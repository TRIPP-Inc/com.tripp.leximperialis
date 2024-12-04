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
            List<Judgment> judgements = new List<Judgment>();
            Object[] selection = Selection.objects;

            for (int i = 0; i < selection.Length; i++)
            {
                Object obj = selection[i];
                string assetPath = AssetDatabase.GetAssetPath(obj);

                // Validate asset path
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogWarning($"Invalid asset path for object at index {i}. Skipping.");
                    continue;
                }

                string assetName = Path.GetFileName(assetPath); // Extract the object name
                string currentHash = ComputeAssetHash(obj);

                // Update progress bar with cancel option
                float progress = (float)i / selection.Length;
                bool isCancelled = EditorUtility.DisplayCancelableProgressBar(
                    "Passing Judgment",
                    $"Processing {assetName} ({i + 1}/{selection.Length})",
                    progress
                );

                // Handle cancellation
                if (isCancelled)
                {
                    Debug.Log("Pass Judgment operation canceled by the user.");
                    break;
                }

                // Find the filter for this object
                string objectType = obj.GetType().Name;
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                string importerType = importer != null ? importer.GetType().Name : null;

                JudicatorFilter filter = _lexImperialis.judicatorFilters.Find(f =>
                    f.objectType == objectType && f.importerType.ToString() == importerType);

                if (filter == null)
                {
                    Debug.LogWarning($"No filter found for {objectType}. Skipping {assetName}.");
                    continue;
                }

                if (!filterDictionary.ContainsKey(filter) || !filterDictionary[filter])
                {
                    Debug.Log($"{assetName} skipped (filter disabled).");
                    continue;
                }

                // Perform adjudication
                List<Judgment> newJudgments = AdjudicateAsset(obj);
                judgements.AddRange(newJudgments);

                // Update cache
                UpdateCache(assetPath, currentHash, judgements);
            }

            // Clear progress bar
            EditorUtility.ClearProgressBar();

            return judgements;
        }

        private bool ShouldSkipAsset(string assetPath, string currentHash)
        {
            CachedAsset cached = cache.assets.Find(c => c.assetPath == assetPath);
            return cached != null && cached.hash == currentHash && cached.passed;
        }

        private void UpdateCache(string assetPath, string currentHash, List<Judgment> judgments)
        {
            CachedAsset cached = cache.assets.Find(c => c.assetPath == assetPath);
            if (cached == null)
            {
                cached = new CachedAsset { assetPath = assetPath };
                cache.assets.Add(cached);
            }

            cached.hash = currentHash;
            cached.passed = judgments.All(j => j.infractions == null || j.infractions.Count == 0);

            SaveCache(); // Persist changes to disk
        }

        private void SaveCache()
        {
            using (FileStream stream = new FileStream(cacheFilePath, FileMode.Create))
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(stream, cache);
            }
        }

        private string ComputeAssetHash(Object asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"Cannot compute hash for asset: {asset.name} (Invalid path)");
                return $"{assetPath}_INVALID";
            }

            try
            {
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);

                if (importer == null)
                {
                    Debug.LogWarning($"No importer found for asset: {asset.name}. Using fallback hash.");
                    return $"{assetPath}_NO_IMPORTER";
                }

                SerializedObject serializedImporter = new SerializedObject(importer);
                string serializedData = SerializeImporterProperties(serializedImporter);

                // Return serialized data directly
                return $"{assetPath}_{serializedData}";
            }
            catch (Exception e)
            {
                Debug.LogError($"Error computing hash for asset: {asset.name}. Exception: {e.Message}");
                return $"{assetPath}_ERROR";
            }
        }

        private string SerializeImporterProperties(SerializedObject importer)
        {
            System.Text.StringBuilder serializedData = new System.Text.StringBuilder();

            SerializedProperty property = importer.GetIterator();
            while (property.NextVisible(true)) // Iterate through all visible properties, including child properties
            {
                serializedData.Append($"{property.propertyPath}:");

                // Append the property value based on its type. Each property is handled explicitly to ensure correct serialization.
                switch (property.propertyType)
                {
                    case SerializedPropertyType.String:
                        serializedData.Append(property.stringValue);
                        break;
                    case SerializedPropertyType.Integer:
                        serializedData.Append(property.intValue);
                        break;
                    case SerializedPropertyType.Boolean:
                        serializedData.Append(property.boolValue);
                        break;
                    case SerializedPropertyType.Float:
                        serializedData.Append(property.floatValue.ToString("F4")); // Format to 4 decimal places
                        break;
                    case SerializedPropertyType.Color:
                        serializedData.Append(property.colorValue.ToString());
                        break;
                    case SerializedPropertyType.ObjectReference:
                        serializedData.Append(property.objectReferenceValue != null ? property.objectReferenceValue.name : "None");
                        break;
                    case SerializedPropertyType.Enum:
                        serializedData.Append(property.enumDisplayNames[property.enumValueIndex]);
                        break;
                    case SerializedPropertyType.Vector2:
                        serializedData.Append(property.vector2Value.ToString());
                        break;
                    case SerializedPropertyType.Vector3:
                        serializedData.Append(property.vector3Value.ToString());
                        break;
                    case SerializedPropertyType.Vector4:
                        serializedData.Append(property.vector4Value.ToString());
                        break;
                    case SerializedPropertyType.Rect:
                        serializedData.Append(property.rectValue.ToString());
                        break;
                    case SerializedPropertyType.ArraySize:
                        serializedData.Append(property.intValue);
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        serializedData.Append(property.animationCurveValue != null ? property.animationCurveValue.ToString() : "None");
                        break;
                    case SerializedPropertyType.Bounds:
                        serializedData.Append(property.boundsValue.ToString());
                        break;
                    case SerializedPropertyType.Quaternion:
                        serializedData.Append(property.quaternionValue.ToString());
                        break;
                    default:
                        serializedData.Append("UnsupportedType"); // For any property types not handled above, the function appends a placeholder
                        break;
                }

                serializedData.Append(";"); // Separate properties
            }

            return serializedData.ToString();
        }

        private List<Judgment> AdjudicateAsset(Object obj)
        {
            List<Judgment> judgments = new List<Judgment>();

            // Find the appropriate Judicator filter for this object
            string objectType = obj.GetType().Name;
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
            string importerType = importer != null ? importer.GetType().Name : null;

            JudicatorFilter filter = _lexImperialis.judicatorFilters.Find(f =>
                f.objectType == objectType && f.importerType.ToString() == importerType);

            if (filter != null && filter.judicator is Judicator judicator)
            {
                Judgment judgment = judicator.Adjudicate(obj);
                if (judgment != null)
                {
                    judgments.Add(judgment);
                }
            }

            return judgments;
        }

        public void PrintAllVariantsToLaw(Material material)
        {
            List<List<string>> permutations = GetPermutations(material.shaderKeywords.ToList());

            foreach(List<string> permutation in permutations)
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
        public List<CachedAsset> assets = new List<CachedAsset>(); // List of cached assets
    }

    [Serializable]
    public class CachedAsset
    {
        public string assetPath; // Path to the asset
        public string hash;      // Hash representing the asset's state
        public bool passed;      // Whether the asset passed adjudication
    }
}
