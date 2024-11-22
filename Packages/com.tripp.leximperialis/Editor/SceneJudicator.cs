using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TRIPP.LexImperialis.Editor
{
    [CreateAssetMenu(fileName = "SceneJudicator", menuName = "ScriptableObjects/LexImperialis/SceneJudicator")]
    public class SceneJudicator : Judicator
    {
        public PlatformStandards[] platformStandards;

        [Serializable]
        public class PlatformStandards
        {
            public string qualityLevelName;
            public int maxTriangleCount = 500000;
            public int maxDrawCalls = 500;
            [Header("Camera Settings")]
            public int fieldOfView = 110;
            public float aspectRatio = 1.0f;
            public bool cameraRotation = true;
            public int cameraRotationInterval = 90;
        }

        public override Judgment Adjudicate(Object accused)
        {
            Judgment result = null;
            string scenePath = AssetDatabase.GetAssetPath(accused);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera mainCamera = Camera.main;
            foreach(PlatformStandards platformStandard in platformStandards)
            {
                int qualityIndex = Array.FindIndex(QualitySettings.names, name => name == platformStandard.qualityLevelName);
                if (qualityIndex != -1)
                {
                    QualitySettings.SetQualityLevel(qualityIndex);
                }
                else
                {
                    Debug.LogError($"Quality level {platformStandard.qualityLevelName} not found in this project's Quality Settings");
                    continue;
                }

                mainCamera.fieldOfView = platformStandard.fieldOfView;
                mainCamera.aspect = platformStandard.aspectRatio;
                if (mainCamera == null)
                {
                    Debug.LogError($"No main camera found in scene {accused.name}");
                    continue;
                }

                if (platformStandard.cameraRotation)
                {
                    bool bottomChecked = false;
                    bool topChecked = false;
                    for (int x = 0; x < 360; x += platformStandard.cameraRotationInterval)
                    {
                        for (int y = -90; y <= 90; y += platformStandard.cameraRotationInterval)
                        {
                            if (y == -90 && bottomChecked)
                            {
                                continue;
                            }
                            else if (y == 90 && topChecked)
                            {
                                continue;
                            }

                            mainCamera.transform.rotation = Quaternion.Euler(y, x, 0);
                            result = CheckForInfractions(platformStandard, mainCamera, result, accused, x, y);

                            if (y == -90)
                            {
                                bottomChecked = true;
                            }
                            else if (y == 90)
                            {
                                topChecked = true;
                            }
                        }
                    }
                }
                else
                {
                    result = CheckForInfractions(
                        platformStandard, 
                        mainCamera, 
                        result, 
                        accused, 
                        mainCamera.transform.eulerAngles.x, 
                        mainCamera.transform.eulerAngles.y);
                }
            }

            return result;
        }

        public override string ServitudeImperpituis(Judgment judgment, Infraction infraction)
        {
            throw new System.NotImplementedException();
        }
        private Judgment CheckForInfractions(PlatformStandards platformStandard, Camera mainCamera, Judgment result, Object accused, float x, float y)
        {
            int triangleCount = UnityStats.triangles;
            int drawCalls = UnityStats.drawCalls;
            if (triangleCount > platformStandard.maxTriangleCount)
            {
                Infraction newInfraction = new Infraction
                {
                    message = $"Maximum triangle count exceeded at camera angle {new Vector2(x, y)}, Triangle Count: {triangleCount}",
                    isFixable = false
                };

                result = CreateOrAddInfractionsToJudgment(newInfraction, result, accused);
            }

            if (drawCalls > platformStandard.maxDrawCalls)
            {
                Infraction newInfraction = new Infraction
                {
                    message = $"Maximum draw call count exceeded at camera angle {new Vector2(x, y)}, Draw Call Count: {drawCalls}",
                    isFixable = false
                };

                result = CreateOrAddInfractionsToJudgment(newInfraction, result, accused);
            }

            return result;
        }
    }
}
