using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TRIPP.LexImperialis.Editor
{
    [CreateAssetMenu(fileName = "PresetJudicator", menuName = "ScriptableObjects/LexImperialis/PresetJudicator")]
    public class ImporterJudicator : Judicator
    {
        public string[] phantomProperties;
        public List<Preset> presets;

        public override Judgment Adjudicate(Object accused)
        {
            Judgment result = null;
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(accused));

            List<Infraction> infractions = new List<Infraction>();

            foreach (var preset in presets)
            {
                if (preset == null)
                    continue;

                // Get the list of infractions from MatchesPreset
                List<Infraction> presetInfractions = MatchesPreset(importer, preset);
                if (presetInfractions.Count == 0)
                {
                    // If the preset matches, break out of the loop
                    return null; // No need to create a judgment if everything is fine
                }
                else
                {
                    // Add all infractions to the main list
                    infractions.AddRange(presetInfractions);
                }
            }

            // If there are infractions, create and return a Judgment
            if (infractions.Count > 0)
            {
                result = new Judgment
                {
                    accused = accused,
                    judicator = this,
                    infractions = infractions
                };
            }

            return result;
        }

        protected List<Infraction> MatchesPreset(Object accusedObject, Preset preset)
        {
            List<Infraction> infractions = new List<Infraction>();

            if (preset.DataEquals(accusedObject))
                return infractions; // Return an empty list if the object matches the preset

            Type accusedObjectType = accusedObject.GetType();
            Type presetType = preset.GetType();
            List<string> excluded = new List<string>();
            excluded.AddRange(preset.excludedProperties);
            excluded.AddRange(phantomProperties);

            foreach (PropertyInfo propertyInfo in accusedObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!excluded.Contains(propertyInfo.Name))
                {
                    try
                    {
                        object presetValue = presetType.GetProperty(propertyInfo.Name)?.GetValue(preset, null);
                        object accusedValue = propertyInfo.GetValue(accusedObject, null);

                        if (!object.Equals(presetValue, accusedValue))
                        {
                            // Create an individual infraction for each mismatch
                            infractions.Add(new Infraction
                            {
                                isFixable = false,
                                message = $"{propertyInfo.Name}: expected {presetValue}, found {accusedValue}"
                            });
                        }
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is NotSupportedException)
                    {
                        Debug.LogWarning($"Property {propertyInfo.Name} is not supported and was skipped.");
                    }
                    catch (AmbiguousMatchException)
                    {
                        Debug.LogWarning($"Property {propertyInfo.Name} resulted in an ambiguous match and was skipped.");
                    }
                }
            }

            return infractions; // Return the list of infractions
        }

        public override string ServitudeImperpituis(Judgment judgment, Infraction infraction)
        {
            string result = "Failed to apply preset.";
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(judgment.accused));
            Preset preset = presets[0];
            if (preset.CanBeAppliedTo(importer))
            {
                preset.ApplyTo(importer);
                result = $"Successfully mind-wiped, reprogrammed, and cybernetically-enhanced {judgment.accused.name}.";
                RemoveInfraction(judgment, infraction);
                importer.SaveAndReimport();
            }

            return result;
        }
    }
}