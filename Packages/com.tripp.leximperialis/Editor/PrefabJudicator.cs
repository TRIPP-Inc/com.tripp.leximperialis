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
            // Start by checking if the accused object is a prefab
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

            // Only create the Judgment if infractions were found
            Judgment judgment = new Judgment
            {
                accused = accused,
                judicator = this,
                infractions = infractions
            };

            return judgment; // Return the populated judgment if infractions were found
        }

        private void CheckParticleSystems(List<Infraction> infractions, GameObject prefab)
        {
            ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                bool passed = false;

                // Check against each preset
                foreach (var preset in presets)
                {
                    if (preset != null)
                    {
                        // Use MatchesPreset to quickly check for general matching
                        if (MatchesPreset(particleSystem, preset))
                        {
                            passed = true;  // If the preset matches, flag as passed
                            break;  // No need to check further presets
                        }
                    }
                }

                if (!passed)
                {
                    // If no matching preset, add infraction to the list
                    infractions.Add(new Infraction
                    {
                        isFixable = false,
                        message = $"{particleSystem.name} does not adhere to preset(s)"
                    });
                }
            }
        }

    }
}
