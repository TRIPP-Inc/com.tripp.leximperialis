using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

namespace TRIPP.LexImperialis.Editor
{
    [CreateAssetMenu(fileName = "PrefabJudicator", menuName = "ScriptableObjects/LexImperialis/PrefabJudicator")]
    public class PrefabJudicator : ImporterJudicator
    {
        public override Judgment Adjudicate(Object accused)
        {
            // Ensure the accused object is a prefab
            GameObject prefab = accused as GameObject;
            if (prefab == null)
            {
                return null; // Return null if not a prefab
            }

            // Prepare a list to collect infractions
            List<Infraction> infractions = new List<Infraction>();

            // Check for Particle Systems in the prefab
            CheckParticleSystems(infractions, prefab);

            // If no infractions were found, return null to suppress default messages
            if (infractions.Count == 0)
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

        private void CheckParticleSystems(List<Infraction> infractions, GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab is null. Cannot check particle systems.");
                return;
            }

            ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems == null || particleSystems.Length == 0)
            {
                Debug.LogWarning("No Particle Systems found in the prefab.");
                return;
            }

            if (presets == null || presets.Count == 0)
            {
                Debug.LogWarning("No presets available for comparison.");
                return;
            }

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem == null)
                {
                    Debug.LogWarning("Encountered a null ParticleSystem component.");
                    continue;
                }

                bool matched = false;
                List<Infraction> collectedInfractions = new List<Infraction>();

                foreach (var preset in presets)
                {
                    if (preset == null)
                    {
                        Debug.LogWarning("Encountered a null preset.");
                        continue;
                    }

                    // Use the inherited method to check infractions for each ParticleSystem
                    List<Infraction> presetInfractions = GetPresetInfractions(particleSystem, preset);

                    // Special case for "Max Particles" property
                    if (presetInfractions != null)
                    {
                        foreach (var infraction in presetInfractions)
                        {
                            // Check if the infraction message is related to "Max Particles"
                            if (infraction.message.Contains("maxNumParticles"))
                            {
                                // Get the Max Particles value from the ParticleSystem
                                var mainModule = particleSystem.main;
                                if (mainModule.maxParticles <= 512)
                                {
                                    // If the value is less than or equal to 512, skip adding this infraction
                                    continue;
                                }
                            }

                            // Add the infraction if it doesn't match the special case
                            collectedInfractions.Add(infraction);
                        }
                    }

                    if (presetInfractions == null || presetInfractions.Count == 0)
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched)
                {
                    continue;
                }

                if (collectedInfractions.Count > 0)
                {
                    infractions.AddRange(collectedInfractions);
                }
                else
                {
                    infractions.Add(new Infraction
                    {
                        isFixable = false,
                        message = $"{particleSystem.name} does not adhere to any of the provided presets."
                    });
                }
            }
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
