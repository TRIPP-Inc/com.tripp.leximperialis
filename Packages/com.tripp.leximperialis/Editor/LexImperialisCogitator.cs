using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.Presets;

namespace TRIPP.LexImperialis.Editor
{
    public class LexImperialisCogitator : EditorWindow
    {
        [MenuItem("Tools/TRIPP TA Tools/Lex Imperialis")]
        public static void ShowWindow()
        {
            GetWindow<LexImperialisCogitator>("Lex Imperialis");

        }

        private LexImperialisMachineSpirit machineSpirit;
        private List<Judgment> judgments;
        private Vector2 _scrollPosition;
        private int _toolbarIndex;
        private readonly string[] _toolbarOptions = { "Arbites Judge", "Legislator" };
        private string _message;
        private GUIStyle _messageStyle;
        private bool _randomMessageIsSet;
        private Dictionary<JudicatorFilter, bool> filterDictionary = new Dictionary<JudicatorFilter, bool>();
        private Dictionary<JudicatorFilter, bool> judicatorExpanded = new Dictionary<JudicatorFilter, bool>();
        private Dictionary<JudicatorFilter, Dictionary<Preset, bool>> presetStates = new Dictionary<JudicatorFilter, Dictionary<Preset, bool>>();
        private Dictionary<Object, bool> infractionFoldouts = new Dictionary<Object, bool>();
        private bool showJudicators = true; // Toggles visibility of all Judicators

        private void OnGUI()
        {
            if (machineSpirit == null)
            {
                machineSpirit = new LexImperialisMachineSpirit();
                foreach (JudicatorFilter jf in machineSpirit._lexImperialis.judicatorFilters)
                {
                    filterDictionary.Add(jf, true);
                    judicatorExpanded.Add(jf, false);
                    if (jf.judicator is ImporterJudicator importerJudicator && importerJudicator.presets != null)
                    {
                        presetStates[jf] = importerJudicator.presets.ToDictionary(p => p, _ => true);
                    }
                }
            }

            if (_messageStyle == null)
                _messageStyle = new GUIStyle();

            if (EditorGUILayout.LinkButton("Standards"))
            {
                Application.OpenURL("https://trippinc.atlassian.net/wiki/spaces/GIFT/pages/615448596/Art+Standards");
            }

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _toolbarIndex = GUILayout.Toolbar(_toolbarIndex, _toolbarOptions);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Move Select/Deselect All button below the toolbar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select/Deselect All", GUILayout.Width(120)))
            {
                bool enableAll = !filterDictionary.Values.Any(enabled => enabled);
                foreach (var key in filterDictionary.Keys.ToList())
                {
                    filterDictionary[key] = enableAll;
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            switch (_toolbarIndex)
            {
                case 0:
                    DisplayAbitesJudge();
                    break;
                case 1:
                    DisplayLegislator();
                    break;
            }
            EditorGUILayout.EndScrollView();

            if (Selection.count == 0)
            {
                _message = "Please make a selection.";
                _randomMessageIsSet = false;
            }

            if (_message != null)
            {
                EditorGUILayout.BeginHorizontal("Box");
                if (_message.Contains("Success"))
                {
                    GUI.color = Color.green;
                }
                else if (_message.Contains("Fail"))
                {
                    GUI.color = Color.red;
                }
                else
                {
                    GUI.color = Color.white;
                }
                EditorGUILayout.SelectableLabel(_message);
                GUILayout.EndHorizontal();
            }
        }



        private void DisplayAbitesJudge()
        {


            if (Selection.count != 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (machineSpirit != null)
                {

                    EditorGUILayout.BeginVertical("Box");

                    foreach (JudicatorFilter dct in filterDictionary.Keys.ToList())
                    {
                        if (dct != null && dct.judicator != null)
                            filterDictionary[dct] = EditorGUILayout.ToggleLeft(dct.judicator.name, filterDictionary[dct]);
                    }

                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("Pass Judgment"))
                {
                    judgments = machineSpirit.PassJudgement(filterDictionary); // Using the original method
                }
            }

            if (judgments != null)
            {
                if (judgments.Count > 0)
                {
                    foreach (Judgment judgment in judgments)
                    {
                        if (judgment != null && judgment.infractions != null && judgment.infractions.Count > 0)
                        {
                            // Ensure foldout state exists for the judgment
                            if (!infractionFoldouts.ContainsKey(judgment.accused))
                            {
                                infractionFoldouts[judgment.accused] = false;
                            }

                            EditorGUILayout.BeginVertical("Box");

                            // Begin Horizontal Layout
                            EditorGUILayout.BeginHorizontal();

                            // Foldout arrow
                            infractionFoldouts[judgment.accused] = EditorGUILayout.Foldout(
                                infractionFoldouts[judgment.accused],
                                GUIContent.none,
                                true,
                                EditorStyles.foldout
                            );

                            // Adjust spacing to bring the icon and name closer to the arrow
                            GUILayout.Space(-730); // Negative space to bring the elements closer

                            // ObjectField for Icon and Name
                            EditorGUILayout.ObjectField(judgment.accused, typeof(Object), false, GUILayout.ExpandWidth(true));

                            // End Horizontal Layout
                            EditorGUILayout.EndHorizontal();

                            // If expanded, show infractions
                            if (infractionFoldouts[judgment.accused])
                            {
                                EditorGUILayout.BeginVertical("Box");
                                for (int i = 0; i < judgment.infractions.Count; i++)
                                {
                                    Infraction infraction = judgment.infractions[i];
                                    if (infraction != null)
                                    {
                                        EditorGUILayout.BeginHorizontal("Box");
                                        EditorGUILayout.LabelField(infraction.message);
                                        if (infraction.isFixable)
                                        {

                                            ImporterJudicator importerJudicator = judgment.judicator as ImporterJudicator;
                                            if (importerJudicator != null &&
                                                importerJudicator.presets != null &&
                                                importerJudicator.presets.Count > 1)
                                            {
                                                // Display multiple fix buttons
                                                foreach (var preset in importerJudicator.presets)
                                                {
                                                    if (preset == null) continue;

                                                    if (GUILayout.Button($"Fix with {preset.name}"))
                                                    {
                                                        _message = importerJudicator.ServitudeImperpituis(judgment, infraction, preset);
                                                        _randomMessageIsSet = false;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // fallback single fix
                                                if (GUILayout.Button("Fix"))
                                                {
                                                    _message = judgment.judicator.ServitudeImperpituis(judgment, infraction);
                                                    _randomMessageIsSet = false;
                                                }
                                            }

                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                }
                                EditorGUILayout.EndVertical();
                            }

                            EditorGUILayout.EndVertical();
                        }
                    }
                }
                else
                {
                    _message = "Successfully cleansed the heritics";
                }
            }
            else
            {
                if (!_randomMessageIsSet)
                {
                    _message = SetRandomMessage();
                    _randomMessageIsSet = true;
                }
            }


        }

        private string SetRandomMessage()
        {
            string[] messages =
            {
                "There is no such thing as innocence, only degrees of guilt.",
                "Ave imperator.",
                "Success is measured in blood; yours or your enemy's.",
                "An open mind is like a fortress with its gates unbarred and unguarded."
            };

            return messages[Random.Range(0, messages.Length - 1)];
        }

        private void DisplayLegislator()
        {
            if (Selection.count != 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (Selection.count > 0)
                {
                    if (GUILayout.Button("Get Importer Type"))
                    {
                        _message = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Selection.activeObject)).GetType().Name;
                    }

                    if (GUILayout.Button("Get Type"))
                    {
                        _message = Selection.activeObject.GetType().Name;
                    }
                }

                if (Selection.activeObject.GetType() == typeof(Material))
                {
                    if (GUILayout.Button("Add Single Variant"))
                    {
                        if (machineSpirit == null)
                        {
                            machineSpirit = new LexImperialisMachineSpirit();
                        }

                        _message = machineSpirit.PrintMaterialToLaw(Selection.activeObject as Material);
                    }

                    if (GUILayout.Button("Add All Permutations"))
                    {
                        if (machineSpirit == null)
                        {
                            machineSpirit = new LexImperialisMachineSpirit();
                        }

                        machineSpirit.PrintAllVariantsToLaw(Selection.activeObject as Material);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}