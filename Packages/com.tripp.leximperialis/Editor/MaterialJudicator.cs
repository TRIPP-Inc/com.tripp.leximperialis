using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TRIPP.LexImperialis.Editor
{
    public class MaterialJudicator : Judicator
    {
        public List<MaterialLaw> materialLaws;
        public List<EmptyTextureMapping> emptyTextureMapping;

        public override Judgment Adjudicate(UnityEngine.Object accused)
        {
            Judgment judgment = null;
            Material examineeMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(accused));
            if (accused.name == "Lit")
            {
                return judgment;
            }

            Dictionary<string, List<string>> illegalShaderAndShaderKeywordDictionary = GetIllegalShaderAndShaderKeywordDictionary(this, examineeMaterial);
            if (illegalShaderAndShaderKeywordDictionary.ContainsKey("error"))
            {
                foreach (string error in illegalShaderAndShaderKeywordDictionary["error"])
                {
                    Debug.LogError(error);
                }
            }
            else if (illegalShaderAndShaderKeywordDictionary.ContainsKey(examineeMaterial.shader.name))
            {
                judgment = new Judgment { accused = accused, judicator = this };
                if (illegalShaderAndShaderKeywordDictionary[examineeMaterial.shader.name].Count == 0)
                {
                    CreateOrAddInfractionsToJudgment(new Infraction { message = "Illegal Shader ", isFixable = false}, judgment, accused);
                }
                else if (illegalShaderAndShaderKeywordDictionary[examineeMaterial.shader.name].Count > 0)
                {
                    foreach (string message in illegalShaderAndShaderKeywordDictionary[examineeMaterial.shader.name])
                    {
                        string hereticalKeyword = message.Substring(8, message.Length - 8);
                        string[] shaderKeywords = examineeMaterial.shader.keywordSpace.keywordNames;
                        if (message.Contains("Invalid"))
                        {
                            CreateOrAddInfractionsToJudgment(new Infraction { message = message, isFixable = false }, judgment, accused);
                        }
                        else
                        {
                            CreateOrAddInfractionsToJudgment(new Infraction { message = message, isFixable = true }, judgment, accused);
                        }
                    }
                }
            }

            return judgment;
        }

        private Dictionary<string, List<string>> GetIllegalShaderAndShaderKeywordDictionary(MaterialJudicator materialLegalCode, Material material)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            if (materialLegalCode == null || material == null)
            {
                result.Add("error", new List<string> { "Judge was provided a null or incomplete material legal code or a null material." });
                return result;
            }

            MaterialLaw matchingLaw = materialLegalCode.materialLaws.Find(l => l.shader == material.shader.name);
            if (matchingLaw == null)
            {
                result.Add(material.shader.name, new List<string>());
            }
            else
            {
                List<string> sortedKeywords = material.shaderKeywords.ToList();
                sortedKeywords.Sort();
                if (!matchingLaw.shaderVariants.Where(v => v.variantKeyWords.SequenceEqual(sortedKeywords)).Any())
                {
                    List<List<string>> variants = new List<List<string>>();
                    foreach (ShaderVariant variant in matchingLaw.shaderVariants)
                    {
                        variants.Add(variant.variantKeyWords.ToList());
                    }

                    result.Add(material.shader.name, GetVariantMatchingReport(variants, material));
                }
            }

            return result;
        }

        private List<string> GetVariantMatchingReport(List<List<string>> variants, Material material)
        {
            List<string> result = new List<string>();
            List<string> materialKeywords = material.shaderKeywords.ToList();
            if (!variants.Where(v => v.SequenceEqual(materialKeywords)).Any())
            {
                List<List<string>> variantMissMatches = new List<List<string>>();
                for (int i = 0; i < variants.Count; i++)
                {
                    List<string> missMatch = new List<string>();
                    foreach (string keyword in variants[i])
                    {
                        if (!materialKeywords.Contains(keyword))
                        {
                            missMatch.Add("Missing " + keyword);
                        }
                    }

                    foreach (string keyword in materialKeywords)
                    {
                        if (!variants[i].Contains(keyword))
                        {
                            if (material.shader.keywordSpace.keywordNames.Contains(keyword))
                            {
                                missMatch.Add($"Invalid {keyword}");
                            }
                            else
                            {
                                missMatch.Add("Illegal " + keyword);
                            }
                        }
                    }

                    if (missMatch.Count > 0)
                    {
                        variantMissMatches.Add(missMatch);
                    }
                }

                variantMissMatches.Sort((x, y) => x.Count().CompareTo(y.Count()));
                result = variantMissMatches[0];
            }

            return result;
        }

        public override string ServitudeImperpituis(Judgment judgment, Infraction infraction)
        {
            string infractionMessage = infraction.message;
            string result = "Failed to fix " + infractionMessage;
            if (infractionMessage.Contains("Missing"))
            {
                //Assign missing texture
                Material hereticalMaterial = judgment.accused as Material;
                SetEmptyTexture(hereticalMaterial, emptyTextureMapping.Find(m => m.keyword == infractionMessage.Remove(0, 8)).propertyName);
                RemoveInfraction(judgment, infraction);
                result = "Successfully called SetTexture.";
            }
            else if (infractionMessage.Contains("Illegal"))
            {
                //Remove invalid keywords
                result = PurgeInvalidKeyword(judgment, infraction);
            }

            return result;
        }

        private void SetEmptyTexture(Material hereticalMaterial, string propertyName)
        {
            hereticalMaterial.SetTexture(propertyName, emptyTextureMapping.Find(m => m.propertyName == propertyName).texture);
            EditorUtility.SetDirty(hereticalMaterial);
            AssetDatabase.SaveAssets();
        }

        public string PurgeInvalidKeyword(Judgment judgment, Infraction infraction)
        {
            string result = string.Empty;
            Material hereticalMaterial = judgment.accused as Material;
            List<string> keywords = hereticalMaterial.shaderKeywords.ToList();
            string hereticalKeyword = infraction.message.Substring(8, infraction.message.Length - 8);
            string[] shaderKeywords = hereticalMaterial.shader.keywordSpace.keywordNames;
            if (shaderKeywords.Contains(hereticalKeyword))
            {
                result = $"Failed to purge. The keyword {hereticalKeyword} is a valid keyword.";
            }
            else
            {
                keywords.Remove(hereticalKeyword);
                hereticalMaterial.shaderKeywords = keywords.ToArray();
                EditorUtility.SetDirty(hereticalMaterial);
                AssetDatabase.SaveAssets();
                RemoveInfraction(judgment, infraction);
                result = $"Successfully purged {hereticalKeyword} from {hereticalMaterial.name}'s keywords.";
            }

            return result;
        }
    }

    [Serializable]
    public class MaterialLaw
    {
        public string shader;
        public List<ShaderVariant> shaderVariants;
    }

    [Serializable]
    public class ShaderVariant
    { 
        public string[] variantKeyWords;
    }

    [Serializable]
    public class EmptyTextureMapping
    {
        public string keyword;
        public string propertyName;
        public Texture texture;
    }
}
