using Codice.CM.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TRIPP.LexImperialis.Editor
{
    public class LexImperialisMachineSpirit
    {
        // Path to the binary cache file
        private string cacheFilePath = "Library/LexImperialisCache.bin";

        // Cache data structure
        private CacheData cache;

        public LexImperialis _lexImperialis;

        public LexImperialisMachineSpirit()
        {
            // Load LexImperialis ScriptableObject
            _lexImperialis = AssetDatabase.LoadAssetAtPath<LexImperialis>(
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:LexImperialis")[0])
            );

            // Load existing cache or create a new one
            if (File.Exists(cacheFilePath))
            {
                using (FileStream stream = new FileStream(cacheFilePath, FileMode.Open))
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    cache = (CacheData)formatter.Deserialize(stream);
                }
            }
            else
            {
                cache = new CacheData();
            }
        }

        public List<Judgment> PassJudgement(Dictionary<JudicatorFilter, bool> filterDictionary)
        {
            Object[] selections = Selection.objects;
            List<string> dependencyPaths = new List<string>();
            List<Judgment> judgments = null;
            foreach (Object selection in selections)
            {
                string assetPath = AssetDatabase.GetAssetPath(selection);
                if (assetPath == null)
                    continue;

                dependencyPaths.AddRange(AssetDatabase.GetDependencies(assetPath).Where(d => !dependencyPaths.Contains(d)));
            }

            int totalDependencies = dependencyPaths.Count;
            for (int i = 0; i < totalDependencies; i++)
            {
                string dependencyPath = dependencyPaths[i];

                //Update Progress Bar
                float progress = (float)i / totalDependencies;
                bool isCancelled = EditorUtility.DisplayCancelableProgressBar(
                    "Passing Judgment",
                    $"Processing {Path.GetFileName(dependencyPath)} ({i + 1}/{totalDependencies})",
                    progress
                );

                if (isCancelled)
                    break;

                if (dependencyPath == null)
                    continue;

                //Check if the asset has changed since the last adjudication
                AssetImporter importer = AssetImporter.GetAtPath(dependencyPath);
                if (ShouldSkipAsset(dependencyPath, importer.assetTimeStamp))
                    continue;

                //Check if the filter for the asset is active
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(dependencyPath);
                string objectType = asset.GetType().Name;
                JudicatorFilter filter = _lexImperialis.judicatorFilters.Find(f =>
                    f.objectType == objectType && f.importerType.ToString() == importer.GetType().Name);

                if (filter == null)
                    continue;

                if (filter.judicator == null)
                {
                    Debug.LogError($"Judicator for {asset.name} is null.");
                    continue;
                }

                if (!filterDictionary.ContainsKey(filter) || !filterDictionary[filter])
                    continue;

                //Adjudicate the asset
                if (judgments == null)
                    judgments = new List<Judgment>();

                Judgment judgment = filter.judicator.Adjudicate(asset);
                if (judgment != null)
                    judgments.Add(judgment);

                // Update cache
                UpdateCache(importer.assetPath, importer.assetTimeStamp, judgment == null);
            }

            // Clear progress bar
            EditorUtility.ClearProgressBar();

            return judgments;
        }

        private bool ShouldSkipAsset(string assetPath, ulong timeStamp)
        {
            CachedAsset cached = cache.assets.Find(c => c.assetPath == assetPath);
            return cached != null && cached.timeStamp == timeStamp && cached.passed;
        }

        private void UpdateCache(string assetPath, ulong currentTimeStamp, bool passedJudgment)
        {
            CachedAsset cached = cache.assets.Find(c => c.assetPath == assetPath);
            if (cached == null)
            {
                cached = new CachedAsset { assetPath = assetPath };
                cache.assets.Add(cached);
            }

            cached.timeStamp = currentTimeStamp;
            cached.passed = passedJudgment;
            SaveCache();
        }

        private void SaveCache()
        {
            using (FileStream stream = new FileStream(cacheFilePath, FileMode.Create))
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(stream, cache);
            }
        }

        public void PrintAllVariantsToLaw(Material material)
        {
            List<List<string>> permutations = GetPermutations(material.shaderKeywords.ToList());

            foreach (List<string> permutation in permutations)
            {
                Debug.Log(String.Join(" ", permutation));
                PrintKeywordsToLaw(material.shader.name, permutation);
            }
            AssetDatabase.SaveAssets();
        }

        private static List<List<T>> GetPermutations<T>(List<T> list)
        {
            List<List<T>> result = new List<List<T>>();
            int combinationCount = 1 << list.Count;

            for (int i = 0; i < combinationCount; i++)
            {
                List<T> combination = new List<T>();
                for (int j = 0; j < list.Count; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        combination.Add(list[j]);
                    }
                }

                combination.Sort();
                result.Add(combination);
            }

            return result;
        }

        public string PrintMaterialToLaw(Material selectedMaterial)
        {
            string message = null;
            if (!selectedMaterial)
                return message;

            List<string> sortedKeywords = selectedMaterial.shaderKeywords.ToList();
            sortedKeywords.Sort();
            message = PrintKeywordsToLaw(selectedMaterial.shader.name, sortedKeywords);
            AssetDatabase.SaveAssets();
            return message;
        }

        private string PrintKeywordsToLaw(string shaderName, List<string> sortedKeywords)
        {
            string message = string.Empty;
            MaterialJudicator materialJudicator = _lexImperialis.judicatorFilters.Find(j => j.objectType == "Material").judicator as MaterialJudicator;
            if (materialJudicator == null)
            {
                string directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_lexImperialis));
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    AssetDatabase.CreateFolder(directory, "LegalCodes");
                }

                materialJudicator = ScriptableObject.CreateInstance<MaterialJudicator>();
                AssetDatabase.CreateAsset(materialJudicator, directory + "/LegalCodes/MaterialJudicator.asset");
                _lexImperialis.judicatorFilters.Add(new JudicatorFilter { objectType = "Material", judicator = materialJudicator });
            }

            if (materialJudicator.materialLaws == null)
                materialJudicator.materialLaws = new List<MaterialLaw>();

            MaterialLaw matchingLaw = materialJudicator.materialLaws.Find(l => l.shader == shaderName);
            if (matchingLaw == null)
            {
                matchingLaw = new MaterialLaw
                {
                    shader = shaderName
                };
                materialJudicator.materialLaws.Add(matchingLaw);
            }

            if (matchingLaw.shaderVariants == null)
                matchingLaw.shaderVariants = new List<ShaderVariant>();

            if (matchingLaw.shaderVariants.Where(v => v.variantKeyWords.SequenceEqual(sortedKeywords)).Any())
            {
                message = "Shader variant is already recorded in the Lex Imperialus";
            }
            else
            {
                matchingLaw.shaderVariants.Add(new ShaderVariant { variantKeyWords = sortedKeywords.ToArray() });
                message = "Keywords have been sribed to law.";
                EditorUtility.SetDirty(materialJudicator);
            }

            return message;
        }

        public string CreateJudicatorFilter()
        {
            string result = $"Failed to create Judicator Filter for {Selection.activeObject.name}";
            if (Selection.activeObject != null)
            {
                Object section = Selection.activeObject;
                string objectType = section.GetType().Name;
                string importerType = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(section)).GetType().Name;
                bool filterExists = _lexImperialis.judicatorFilters.Any(f => f.objectType == objectType && f.importerType.ToString() == importerType);
                if (!filterExists)
                {
                    JudicatorFilter filter = new JudicatorFilter
                    {
                        importerType = (ImporterType)Enum.Parse(typeof(ImporterType), importerType),
                        objectType = objectType,
                        judicator = null
                    };

                    _lexImperialis.judicatorFilters.Add(filter);
                    result = $"Successfully created Judicator Filter for {Selection.activeObject.name}. " +
                             $"Please assign the corresponding Judicator to the filter in the Lex Imperialis.";
                }
                else
                {
                    result = $"Judicator Filter for {Selection.activeObject.name} already exists.";
                }
            }

            return result;
        }
    }

    public class Judgment
    {
        public Object accused;
        public Judicator judicator;
        public List<Infraction> infractions;
    }

    public class Infraction
    {
        public string message;
        public bool isFixable;
    }

    [Serializable]
    public class CacheData
    {
        public List<CachedAsset> assets = new List<CachedAsset>();
    }

    [Serializable]
    public class CachedAsset
    {
        public string assetPath;
        public ulong timeStamp;
        public bool passed;
    }
}
