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
            // Initialize the judgment object at the start to ensure it's not null
            Judgment judgment = new Judgment
            {
                accused = accused,
                judicator = this,
                infractions = new List<Infraction>()
            };

            GameObject prefab = accused as GameObject;

            if (prefab == null)
            {
                return null; // Return null if not a prefab
            }

            // Check for Particle Systems in the prefab
            CheckParticleSystems(judgment, prefab);

            // If no infractions were found, return null to suppress default messages
            if (judgment.infractions.Count == 0)
            {
                return null;
            }

            return judgment; // Return the populated judgment if infractions were found
        }

        private void CheckParticleSystems(Judgment judgment, GameObject prefab)
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
                    // If no matching preset, report the infraction
                    AddToJudgement(judgment, new Infraction
                    {
                        isFixable = false,
                        message = $"{particleSystem.name} does not adhere to preset(s)"
                    });

                    // Now perform granular checks for specific properties like Max Particles, Gravity Modifier, etc.
                    foreach (var preset in presets)
                    {
                        if (preset != null)
                        {
                            ComparePresetWithAccused(particleSystem, preset, judgment);
                        }
                    }
                }
            }
        }

        private void ComparePresetWithAccused(ParticleSystem particleSystem, Preset preset, Judgment judgment)
        {
            // Create a temporary Particle System to apply the preset
            var tempGameObject = new GameObject("TempParticleSystem");
            var tempParticleSystem = tempGameObject.AddComponent<ParticleSystem>();

            // Apply the preset to the temporary Particle System
            preset.ApplyTo(tempParticleSystem);

            // Now compare properties of the accused particle system with the preset
            var accusedMain = particleSystem.main;
            var presetMain = tempParticleSystem.main;

            // Get the name of the accused particle system for infraction messages
            string particleSystemName = particleSystem.name;

            // Compare Gravity Modifier
            if (!object.Equals(accusedMain.gravityModifier, presetMain.gravityModifier))
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false,
                    message = $"{particleSystemName} Gravity Modifier is not 0, please fix this."
                });
            }

            // Compare Max Particles
            if (accusedMain.maxParticles > presetMain.maxParticles)
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false, // Removed fixable option
                    message = $"{particleSystemName} Max Particles--Current: {accusedMain.maxParticles}, Expected: {presetMain.maxParticles} or less."
                });
            }

            // Compare Limit Velocity Over Lifetime
            var accusedLimitVelocityModule = particleSystem.limitVelocityOverLifetime;
            var presetLimitVelocityModule = tempParticleSystem.limitVelocityOverLifetime;

            if (!object.Equals(accusedLimitVelocityModule.enabled, presetLimitVelocityModule.enabled))
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false, // Removed fixable option
                    message = $"{particleSystemName} Limit Velocity Over Lifetime--Current: {(accusedLimitVelocityModule.enabled ? "On" : "Off")}, Expected: {(presetLimitVelocityModule.enabled ? "On" : "Off")}"
                });
            }

            // Check for Trails module
            var accusedTrailsModule = particleSystem.trails;
            var presetTrailsModule = tempParticleSystem.trails;

            if (accusedTrailsModule.enabled && accusedTrailsModule.minVertexDistance < 0.2f)
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false, // Removed fixable option
                    message = $"{particleSystemName} Trails -> Minimum Vertex Distance is lower than 0.2. Please consider increasing it."
                });
            }

            // Compare Lights
            var accusedLightsModule = particleSystem.lights;
            var presetLightsModule = tempParticleSystem.lights;

            if (!object.Equals(accusedLightsModule.enabled, presetLightsModule.enabled))
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false, // Removed fixable option
                    message = $"{particleSystemName} Lights--Current: {(accusedLightsModule.enabled ? "On" : "Off")}, Expected: {(presetLightsModule.enabled ? "On" : "Off")}"
                });
            }

            // Clean up the temporary GameObject and Particle System
            Object.DestroyImmediate(tempGameObject); // Cleanup
        }
    }
}
