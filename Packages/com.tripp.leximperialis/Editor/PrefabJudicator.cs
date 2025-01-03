using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using System;

namespace TRIPP.LexImperialis.Editor
{
    [CreateAssetMenu(fileName = "PrefabJudicator", menuName = "ScriptableObjects/LexImperialis/PrefabJudicator")]
    public class PrefabJudicator : ImporterJudicator
    {
        // Adjudicate method to analyze the prefab and identify any infractions
        public override Judgment Adjudicate(UnityEngine.Object accused)
        {
            // Ensure the accused object is a prefab
            GameObject prefab = accused as GameObject;
            if (prefab == null)
            {
                return null; // Return null if not a GameObject prefab
            }

            // Check for ParticleSystems in the prefab and gather infractions
            List<Infraction> infractions = CheckParticleSystems(prefab);

            // If no infractions are found, return null
            if (infractions == null || infractions.Count == 0)
            {
                return null;
            }

            // Create and return a Judgment object with identified infractions
            return new Judgment
            {
                accused = prefab,
                judicator = this,
                infractions = infractions
            };
        }

        // Method to check all ParticleSystems within the prefab for infractions
        private List<Infraction> CheckParticleSystems(GameObject prefab)
        {
            // Retrieve all ParticleSystems within the prefab
            ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems == null || particleSystems.Length == 0)
            {
                return null; // Return null if no ParticleSystems are found
            }

            if (presets == null || presets.Count == 0)
            {
                return null; // Return null if no presets are available for comparison
            }

            List<Infraction> infractions = new List<Infraction>();

            // Loop through each ParticleSystem in the prefab
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                // Get the path to the ParticleSystem for reporting
                string particleSystemPath = AnimationUtility.CalculateTransformPath(particleSystem.transform, prefab.transform);

                // Compare each ParticleSystem to the presets
                foreach (var preset in presets)
                {
                    if (preset == null)
                    {
                        continue; // Skip null presets
                    }

                    // Get infractions for the current preset
                    List<Infraction> presetInfractions = GetPresetInfractions(particleSystem, preset);

                    if (presetInfractions != null)
                    {
                        // Append the ParticleSystem path to each infraction message
                        foreach (var infraction in presetInfractions)
                        {
                            infraction.message = $"{particleSystemPath}: {infraction.message}";
                        }

                        // Add the infractions to the main list
                        infractions.AddRange(presetInfractions);
                    }
                }
            }

            return infractions;
        }

        // Method to apply a fix to a specific infraction
        public override string ServitudeImperpituis(Judgment judgment, Infraction infraction)
        {
            string result = "Failed to apply preset.";

            if (judgment == null || infraction == null)
            {
                return result; // Return early if judgment or infraction is null
            }

            // Split the infraction message to extract path, property, and value information
            string[] parts = infraction.message.Split(':');
            if (parts.Length < 3)
            {
                return "The infraction message format is invalid."; // Return if the message format is incorrect
            }

            string particleSystemPath = parts[0].Trim(); // Extract the ParticleSystem path
            string propertyName = parts[1].Trim(); // Extract the property name
            string valueInfo = parts[2].Trim(); // Extract expected and found values

            // Extract the expected value from the message
            string expectedValueString = "";
            if (valueInfo.Contains("expected"))
            {
                int expectedIndex = valueInfo.IndexOf("expected") + 8; // The word "expected" has a length of 8 characters. Adding 8 moves the starting position to the first character after the word "expected"
                expectedValueString = valueInfo.Substring(expectedIndex).Split(',')[0].Trim();
            }

            GameObject prefab = judgment.accused as GameObject;
            if (prefab == null)
            {
                return "The accused is not a prefab."; 
            }

            // Locate the specific ParticleSystem using the path
            Transform targetTransform = prefab.transform.Find(particleSystemPath);
            if (targetTransform == null)
            {
                return $"Failed to find the ParticleSystem at path: {particleSystemPath}.";
            }

            ParticleSystem targetParticleSystem = targetTransform.GetComponent<ParticleSystem>();
            if (targetParticleSystem == null)
            {
                return $"Failed to find a ParticleSystem at path: {particleSystemPath}.";
            }

            // Access the serialized object of the ParticleSystem
            SerializedObject serializedParticleSystem = new SerializedObject(targetParticleSystem);
            SerializedProperty property = serializedParticleSystem.FindProperty(propertyName);

            if (property == null)
            {
                return $"The property {propertyName} was not found on the ParticleSystem.";
            }

            // Apply the fix based on the property's type
            bool applied = false;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    if (float.TryParse(expectedValueString, out float floatValue))
                    {
                        property.floatValue = floatValue;
                        applied = true;
                        result = $"Updated {propertyName} to {floatValue}.";
                    }
                    break;

                case SerializedPropertyType.Integer:
                    if (int.TryParse(expectedValueString, out int intValue))
                    {
                        property.intValue = intValue;
                        applied = true;
                        result = $"Updated {propertyName} to {intValue}.";
                    }
                    break;

                case SerializedPropertyType.Boolean:
                    if (bool.TryParse(expectedValueString, out bool boolValue))
                    {
                        property.boolValue = boolValue;
                        applied = true;
                        result = $"Updated {propertyName} to {boolValue}.";
                    }
                    break;

                case SerializedPropertyType.String:
                    property.stringValue = expectedValueString;
                    applied = true;
                    result = $"Updated {propertyName} to {expectedValueString}.";
                    break;

                case SerializedPropertyType.Color:
                    if (ColorUtility.TryParseHtmlString(expectedValueString, out Color colorValue))
                    {
                        property.colorValue = colorValue;
                        applied = true;
                        result = $"Updated {propertyName} to {colorValue}.";
                    }
                    break;

                default:
                    return $"The property type {property.propertyType} is not supported.";
            }

            if (applied)
            {
                // Save changes to the prefab
                serializedParticleSystem.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetParticleSystem);
                PrefabUtility.SavePrefabAsset(prefab);

                // Remove the fixed infraction from the judgment
                judgment.infractions?.Remove(infraction);

                if (judgment.infractions?.Count == 0)
                {
                    result = $"Successfully fixed all infractions for {judgment.accused.name}.";
                }
            }

            return result;
        }
    }
}
