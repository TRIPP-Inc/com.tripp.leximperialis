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
        private LexImperialis _lexImperialis;

        public LexImperialisMachineSpirit()
        {
            _lexImperialis = AssetDatabase.LoadAssetAtPath<LexImperialis>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t: LexImperialis")[0]));
        }

        public List<Judgment> PassJudgement()
        {
            List<Judgment> judgements = new List<Judgment>();
            Object[] selection = Selection.objects;
            List<string> dependencyPaths = new List<string>();



            foreach (Object obj in selection)
            {
                dependencyPaths.AddRange(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(obj)));
            }

            foreach (string path in dependencyPaths)
            {
                Object accused = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (accused == null)
                    continue;

               
                
                string accusedObjectType = accused.GetType().Name;
                string accusedImporterType = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(accused)).GetType().Name;
                JudicatorFilter filter = _lexImperialis.judicatorFilters.Find(f => accusedObjectType == f.objectType && accusedImporterType == f.importerType.ToString());
                if (filter == null)
                    continue;

                Judicator judicator = filter.judicator as Judicator;
                Judgment accusedJudgment = null;
                if (judicator != null)
                    accusedJudgment = judicator.Adjudicate(accused);

                if (accusedJudgment != null && accusedJudgment.infractions != null)
                {
                    judgements.Add(accusedJudgment);
                }
            }

            return judgements;
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
}
