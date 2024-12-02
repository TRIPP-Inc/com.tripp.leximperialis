using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TRIPP.LexImperialis.Editor
{
    [CreateAssetMenu(fileName = "PresetJudicator", menuName = "ScriptableObjects/LexImperialis/PresetJudicator")]
    public class ImporterJudicator : Judicator
    {
        public int floatComparisonPrecision = 4;
        public List<string> propertyPathsToIgnore;
        public List<Preset> presets;

        public override Judgment Adjudicate(Object accused)
        {
            Judgment result = null;
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(accused));
            if (importer == null)
                return result;

            List<Infraction> infractions = null;
            foreach (var preset in presets)
            {
                if (preset == null)
                    continue;

                List<Infraction> presetInfractions = GetPresetInfractions(importer, preset);
                if (presetInfractions == null || presetInfractions.Count == 0)
                {
                    infractions = null;
                    break;
                }
                else
                {
                    if (infractions == null)
                        infractions = new List<Infraction>();

                    infractions.AddRange(presetInfractions);
                }
            }

            if (infractions != null)
            {
                result = new Judgment
                {
                    accused = accused,
                    infractions = infractions,
                    judicator = this
                };
            }

            return result;
        }

        protected List<Infraction> GetPresetInfractions(Object accusedObject, Preset preset)
        {
            List<Infraction> infractions = null;

            if(preset == null || accusedObject == null)
                return infractions;

            if(preset.DataEquals(accusedObject))
                return infractions;

            SerializedObject accusedSerializedObject = new SerializedObject(accusedObject);
            foreach (PropertyModification propertyModification in preset.PropertyModifications)
            {
                if(propertyPathsToIgnore.Contains(propertyModification.propertyPath))
                    continue;

                SerializedProperty accusedProperty = accusedSerializedObject.FindProperty(propertyModification.propertyPath);
                
                if (accusedProperty == null)
                {
                    Debug.LogWarning($"Property {propertyModification.propertyPath} not found in {accusedObject.name}");
                    continue;
                }

                ComparisonStrings comparisonStrings = ValueToString(accusedProperty, propertyModification.value);
                if (comparisonStrings.presetValue != comparisonStrings.accusedValue)
                {
                    if (infractions == null)
                        infractions = new List<Infraction>();

                    infractions.Add(new Infraction
                    {
                        isFixable = true,
                        message = $"{accusedProperty.propertyPath} : expected {comparisonStrings.presetValue}, found {comparisonStrings.accusedValue}"
                    });
                }
            }

            return infractions;
        }

        private class ComparisonStrings
        { 
            public string presetValue;
            public string accusedValue;
        }

        private ComparisonStrings ValueToString(SerializedProperty property, string propertyModificationValue)
        {
            ComparisonStrings result = new ComparisonStrings
            {
                presetValue = propertyModificationValue
            };

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    result.presetValue = propertyModificationValue == string.Empty ? "Empty" : propertyModificationValue;
                    result.accusedValue = property.stringValue == string.Empty ? "Empty" : property.stringValue;
                    break;
                case SerializedPropertyType.Integer:
                    result.accusedValue = property.intValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    result.presetValue = propertyModificationValue == "0" ? "False" : "True";
                    result.accusedValue = property.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    float presetFloat = float.Parse(propertyModificationValue, new System.Globalization.CultureInfo("en-EN"));
                    result.presetValue = presetFloat.ToString($"F{floatComparisonPrecision}");
                    result.accusedValue = property.floatValue.ToString($"F{floatComparisonPrecision}");
                    break;
                case SerializedPropertyType.Enum:
                    result.accusedValue = property.enumNames[property.enumValueIndex];
                    break;
                case SerializedPropertyType.ObjectReference:
                    result.presetValue = propertyModificationValue == "" ? "None" : propertyModificationValue;
                    result.accusedValue = property.objectReferenceValue != null ? property.objectReferenceValue.name : "None";
                    break;
                case SerializedPropertyType.ArraySize:
                    result.accusedValue = property.intValue.ToString();
                    break;
                case SerializedPropertyType.Character:
                    result.accusedValue = property.stringValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    result.accusedValue = property.animationCurveValue.ToString();
                    break;
                case SerializedPropertyType.Bounds:
                    result.accusedValue = property.boundsValue.ToString();
                    break;
                case SerializedPropertyType.Color:
                    result.accusedValue = property.colorValue.ToString();
                    break;
                case SerializedPropertyType.Gradient:
                    result.accusedValue = property.gradientValue.ToString();
                    break;
                case SerializedPropertyType.LayerMask:
                    result.accusedValue = property.intValue.ToString();
                    break;
                case SerializedPropertyType.Quaternion:
                    result.accusedValue = property.quaternionValue.ToString();
                    break;
                case SerializedPropertyType.Rect:
                    result.accusedValue = property.rectValue.ToString();
                    break;
                case SerializedPropertyType.Vector2:
                    result.accusedValue = property.vector2Value.ToString();
                    break;
                case SerializedPropertyType.Vector3:
                    result.accusedValue = property.vector3Value.ToString();
                    break;
                case SerializedPropertyType.Vector4:
                    result.accusedValue = property.vector4Value.ToString();
                    break;
                case SerializedPropertyType.ExposedReference:
                    result.accusedValue = property.exposedReferenceValue != null ? property.exposedReferenceValue.name : "None";
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    result.accusedValue = property.fixedBufferSize.ToString();
                    break;
                case SerializedPropertyType.Generic:
                    result.accusedValue = "Generic";
                    break;
                default:
                    result.accusedValue = "Unrecognized Type";
                    break;
            }

            return result;
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