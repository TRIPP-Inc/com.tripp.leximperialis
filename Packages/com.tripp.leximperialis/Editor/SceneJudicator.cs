using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using static TRIPP.LexImperialis.Editor.SceneJudicator;
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
            public int memoryUsageLimitMB = 500;
            public int maxTriangleCount = 500000;
            public int maxDrawCalls = 500;
            [Header("Camera Settings")]
            public int fieldOfView = 110;
            public float aspectRatio = 1.0f;
            public bool cameraRotates = true;
            public int cameraRotationInterval = 90;
        }

        public override Judgment Adjudicate(Object accused)
        {
            Judgment result = null;
            string scenePath = AssetDatabase.GetAssetPath(accused);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera mainCamera = Camera.main;
            foreach (PlatformStandards platformStandard in platformStandards)
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

                CreateOrAddInfractionsToJudgment(CheckForCameraBasedInfractions(platformStandard, mainCamera), result, accused);
                CreateOrAddInfractionsToJudgment(CheckForMemoryInfraction(accused, platformStandard.memoryUsageLimitMB), result, accused);
            }

            return result;
        }

        public override string ServitudeImperpituis(Judgment judgment, Infraction infraction)
        {
            throw new System.NotImplementedException();
        }

        private List<Infraction> CheckForMemoryInfraction(Object accused, float memoryLimit)
        {
            List<Infraction> result = null;
            string[] dependencyPaths = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(accused));
            float totalMemory = 0;
            foreach (string dependencyPath in dependencyPaths)
            {
                Object dependency = AssetDatabase.LoadAssetAtPath<Object>(dependencyPath);
                if (dependency == null)
                {
                    continue;
                }

                totalMemory += (Profiler.GetRuntimeMemorySizeLong(dependency) / 1048576.0f);
            }

            if (totalMemory > memoryLimit)
            {
                if(result == null)
                    result = new List<Infraction>();

                result.Add(new Infraction
                {
                    message = $"Memory usage exceeded: {totalMemory}MB (Limit: {memoryLimit}MB)",
                    isFixable = false
                });
            }

            return result;
        }

        private List<Infraction> CheckForCameraBasedInfractions(PlatformStandards platformStandard, Camera mainCamera)
        {
            List<Infraction> result = null;
            if (platformStandard.cameraRotates)
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
                        List<Infraction> infractions = CheckForCameraInfractions(platformStandard);
                        if (infractions != null)
                        {
                            if (result == null)
                                result = new List<Infraction>();

                            result.AddRange(infractions);
                        }

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
                result = CheckForCameraInfractions(platformStandard);
            }

            return result;
        }

        private List<Infraction> CheckForCameraInfractions(PlatformStandards platformStandard)
        {
            List<Infraction> result = null;
            int triangleCount = UnityStats.triangles;
            int drawCalls = UnityStats.drawCalls;
            if (triangleCount > platformStandard.maxTriangleCount)
            {
                if (result == null)
                    result = new List<Infraction>();

                result.Add(new Infraction
                {
                    message = $"Maximum triangle count exceeded at camera angle {Camera.main.transform.localEulerAngles}, Triangle Count: {triangleCount}",
                    isFixable = false
                });
            }

            if (drawCalls > platformStandard.maxDrawCalls)
            {
                if (result == null)
                    result = new List<Infraction>();

                result.Add(new Infraction
                {
                    message = $"Maximum draw call count exceeded at camera angle {Camera.main.transform.localEulerAngles}, Draw Call Count: {drawCalls}",
                    isFixable = false
                });
            }

            return result;
        }
    }
}
