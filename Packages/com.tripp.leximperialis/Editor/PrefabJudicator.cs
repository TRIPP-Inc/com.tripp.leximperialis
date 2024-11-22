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
        public override Judgment Adjudicate(UnityEngine.Object accused)
        {
            // Ensure the accused object is a prefab
            GameObject prefab = accused as GameObject;
            if (prefab == null)
            {
                return null; // Return null if not a prefab
            }

            // Check for Particle Systems in the prefab and get infractions
            List<Infraction> infractions = CheckParticleSystems(prefab);

            // If no infractions were found, return null to suppress default messages
            if (infractions == null || infractions.Count == 0)
            {
                return null;
            }

            // Create the Judgment if infractions were found
            Judgment judgment = new Judgment
            {
                accused = accused,
                judicator = this,
                infractions = infractions
            };

            return judgment;
        }

        private List<Infraction> CheckParticleSystems(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab is null. Cannot check particle systems.");
                return null;
            }

            ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems == null || particleSystems.Length == 0)
            {
                // Silently handle cases where no Particle Systems are found.
                return null;
            }

            if (presets == null || presets.Count == 0)
            {
                Debug.LogWarning("No presets available for comparison.");
                return null;
            }

            List<Infraction> infractions = new List<Infraction>();

            foreach (ParticleSystem particleSystem in particleSystems)
            {

                foreach (var preset in presets)
                {
                    if (preset == null)
                    {
                        Debug.LogWarning("Encountered a null preset.");
                        continue;
                    }

                    // Use the inherited method to check infractions for each ParticleSystem
                    List<Infraction> presetInfractions = GetPresetInfractions(particleSystem, preset);

                    if (presetInfractions == null)
                    {
                        break;
                    }

                    // Check maxParticles against the preset
                    ParticleSystem.MainModule mainModule = particleSystem.main;
                    PropertyModification maxParticlesModification = Array.Find(preset.PropertyModifications,
                        modification => modification.propertyPath.Contains("InitialModule.maxNumParticles"));
                    int presetMaxParticles = int.Parse(maxParticlesModification.value);

                    // Remove infractions related to maxParticles if within the limit
                    presetInfractions.RemoveAll(infraction =>
                        infraction.message.Contains("InitialModule.maxNumParticles") &&
                        mainModule.maxParticles <= presetMaxParticles);

                    // Add remaining infractions to the main infractions list
                    infractions.AddRange(presetInfractions);
                }
            }

            return infractions;
        }

        public override string ServitudeImperpituis(Judgment judgment, Infraction infraction)
        {
            string result = "Failed to apply preset.";

            if (judgment == null || infraction == null)
            {
                return result;
            }

            GameObject accusedGameObject = judgment.accused as GameObject;
            if (accusedGameObject == null)
            {
                return result;
            }

            ParticleSystem particleSystem = accusedGameObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return result;
            }

            string[] parts = infraction.message.Split(':');
            if (parts.Length < 2)
            {
                return result;
            }

            string propertyName = parts[0].Trim();
            string expectedValueString = parts[1].Substring(parts[1].IndexOf("expected") + 8).Split(',')[0].Trim();

            SerializedObject serializedParticleSystem = new SerializedObject(particleSystem);
            SerializedProperty property = serializedParticleSystem.FindProperty(propertyName);

            if (property == null)
            {
                return result;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    if (float.TryParse(expectedValueString, out float floatValue))
                    {
                        property.floatValue = floatValue;
                        result = $"Updated {propertyName} to {floatValue}.";
                    }
                    break;

                case SerializedPropertyType.Integer:
                    if (int.TryParse(expectedValueString, out int intValue))
                    {
                        property.intValue = intValue;
                        result = $"Updated {propertyName} to {intValue}.";
                    }
                    break;

                case SerializedPropertyType.Boolean:
                    if (bool.TryParse(expectedValueString, out bool boolValue))
                    {
                        property.boolValue = boolValue;
                        result = $"Updated {propertyName} to {boolValue}.";
                    }
                    break;

                case SerializedPropertyType.String:
                    property.stringValue = expectedValueString;
                    result = $"Updated {propertyName} to {expectedValueString}.";
                    break;

                case SerializedPropertyType.Color:
                    if (ColorUtility.TryParseHtmlString(expectedValueString, out Color colorValue))
                    {
                        property.colorValue = colorValue;
                        result = $"Updated {propertyName} to {colorValue}.";
                    }
                    break;

                default:
                    break;
            }

            serializedParticleSystem.ApplyModifiedProperties();

            if (judgment.infractions != null && judgment.infractions.Contains(infraction))
            {
                judgment.infractions.Remove(infraction);

                if (judgment.infractions.Count == 0)
                {
                    result = $"Successfully mind-wiped, reprogrammed, and cybernetically-enhanced {judgment.accused.name}.";
                }
            }

            return result;
        }

    }
}
