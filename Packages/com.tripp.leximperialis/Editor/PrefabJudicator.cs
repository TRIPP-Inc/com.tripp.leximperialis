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
                bool matched = false;

                // Check against each preset
                foreach (var preset in presets)
                {
                    if (preset != null)
                    {
                        // Get the list of infractions from MatchesPreset
                        List<Infraction> presetInfractions = MatchesPreset(particleSystem, preset);
                        if (presetInfractions.Count == 0)
                        {
                            // If the preset matches, mark as matched and break
                            matched = true;
                            break;
                        }
                        else
                        {
                            // Add all infractions to the main list
                            infractions.AddRange(presetInfractions);
                        }
                    }
                }

                if (!matched)
                {
                    // If no presets matched, add a general infraction
                    infractions.Add(new Infraction
                    {
                        isFixable = false,
                        message = $"{particleSystem.name} does not adhere to any of the provided presets."
                    });
                }
            }
        }

    }
}
