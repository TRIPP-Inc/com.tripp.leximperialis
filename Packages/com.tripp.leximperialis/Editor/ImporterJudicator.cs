using System;
using System.Collections.Generic;
using System.Linq;
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
            bool passed = false;
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(accused));
            List<Infraction> infractions = new List<Infraction>();

            foreach (var preset in presets)
            {
                if (preset == null)
                    continue;

                // Use MatchesPreset to get a list of mismatch details
                List<string> mismatchDetails = MatchesPreset(importer, preset);
                if (mismatchDetails.Count == 0)
                {
                    passed = true; // Mark as passed if no mismatches
                    break; // No need to check further presets
                }
                else
                {
                    // Add an individual infraction for each mismatch
                    foreach (string detail in mismatchDetails)
                    {
                        infractions.Add(new Infraction
                        {
                            isFixable = false,
                            message = $"{accused.name} mismatch: {detail}"
                        });
                    }
                }
            }

            if (!passed)
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

        protected List<string> MatchesPreset(Object accusedObject, Preset preset)
        {
            var mismatchDetails = new List<string>();

            // Check if the accused object matches the preset data directly
            if (preset.DataEquals(accusedObject))
                return mismatchDetails; // If everything matches, return an empty list

            var accusedObjectType = accusedObject.GetType();

            // Combine excluded properties from the preset and phantom properties into a HashSet
            var excludedSet = new HashSet<string>(
                preset.excludedProperties.Concat(phantomProperties).Select(name => name.ToLower())
            );

            Object tempObject = null;
            try
            {
                // Create a temporary clone of the accused object and apply the preset to it
                tempObject = Object.Instantiate(accusedObject);
                preset.ApplyTo(tempObject);

                // Loop through each property of the accused object
                foreach (PropertyInfo propertyInfo in accusedObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Skip properties that are in the exclusion list
                    if (!excludedSet.Contains(propertyInfo.Name.ToLower()))
                    {
                        // Compare the values of the property between the accused object and the preset-applied temporary object
                        ComparePropertyValues(accusedObject, tempObject, propertyInfo, mismatchDetails);
                    }
                }
            }
            finally
            {
                // Ensure the temporary object is destroyed to avoid memory leaks
                if (tempObject != null)
                    Object.DestroyImmediate(tempObject);
            }

            return mismatchDetails;
        }

        private void ComparePropertyValues(Object accusedObject, Object tempObject, PropertyInfo propertyInfo, List<string> mismatchDetails)
        {
            try
            {
                // Get the value of the property from both the accused object and the preset-applied object
                var accusedValue = propertyInfo.GetValue(accusedObject, null);
                var presetValue = propertyInfo.GetValue(tempObject, null);

                // Check if both values are not null before comparing
                if (accusedValue != null && presetValue != null)
                {
                    // Check if the property has an 'enabled' field using Reflection
                    var enabledProperty = accusedValue.GetType().GetProperty("enabled");
                    if (enabledProperty != null)
                    {
                        // If the property has an 'enabled' field, compare the enabled states
                        bool accusedEnabled = (bool)enabledProperty.GetValue(accusedValue);
                        bool presetEnabled = (bool)enabledProperty.GetValue(presetValue);

                        // If the enabled states are different, add a detailed message
                        if (accusedEnabled != presetEnabled)
                        {
                            string expectedState = presetEnabled ? "On" : "Off";
                            string foundState = accusedEnabled ? "On" : "Off";
                            mismatchDetails.Add($"{propertyInfo.Name}: expected {expectedState}, found {foundState}");
                        }
                    }
                    else
                    {
                        // If there's no 'enabled' property, compare the values directly
                        if (!object.Equals(presetValue, accusedValue))
                        {
                            mismatchDetails.Add($"{propertyInfo.Name}: expected {presetValue}, found {accusedValue}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log a warning if there is an error processing the property
                Debug.LogWarning($"Failed to process {propertyInfo.Name}: {ex.Message}");
            }
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