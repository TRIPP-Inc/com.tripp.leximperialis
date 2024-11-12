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
            bool passed = false;
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(accused));

            foreach (var preset in presets)
            {
                if (preset == null)
                    continue;
                
                if (!MatchesPreset(importer, preset))
                    continue;
                else
                    passed = true;
            }

            if (!passed)
            {
                bool fixable = false;
                if (presets.Count == 1)
                {
                    fixable = true;
                }

                result = new Judgment
                {
                    accused = accused,
                    judicator = this,
                    infractions = new List<Infraction>
                    {
                        new Infraction
                        {
                            isFixable = fixable,
                            message = $"{accused.name} does not adhere to preset(s)"
                        }
                    }
                };
            }
        
            return result;
        }

        protected bool ComponentMatchesPreset(Component component, Preset preset)
        {
            return MatchesPreset(component, preset);
        }

        protected bool MatchesPreset(Object accusedObject, Preset preset)
        {
            if(preset.DataEquals(accusedObject))
                return true;

            var accusedObjectType = accusedObject.GetType();
            var presetType = preset.GetType();
            List<string> excluded = new List<string>();
            excluded.AddRange(preset.excludedProperties);
            excluded.AddRange(phantomProperties);

            foreach (PropertyInfo propertyInfo in accusedObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!excluded.Contains(propertyInfo.Name))
                {                    
                    try
                    {
                        var presetValue = presetType.GetProperty(propertyInfo.Name)?.GetValue(preset, null);
                        var importerValue = propertyInfo.GetValue(accusedObject, null);
                        if (!object.Equals(presetValue, importerValue))
                        {
                            Debug.Log(propertyInfo.Name);
                            return false;
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
            return true;
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