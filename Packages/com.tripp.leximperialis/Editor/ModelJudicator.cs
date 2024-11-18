using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace TRIPP.LexImperialis.Editor
{
    [CreateAssetMenu(fileName = "ModelJudicator", menuName = "ScriptableObjects/LexImperialis/ModelJudicator")]
    public class ModelJudicator : ImporterJudicator
    {
        public override Judgment Adjudicate(Object accused)
        {
            Judgment judgment = base.Adjudicate(accused);
            ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(accused)) as ModelImporter;
            List<Infraction> infractions = CheckForInfractions(judgment, modelImporter);
            if (infractions != null && infractions.Count > 0)
            {
                if(judgment == null)
                {
                    judgment = new Judgment
                    {
                        accused = accused,
                        judicator = this,
                        infractions = new List<Infraction>()
                    };
                }

                judgment.infractions.AddRange(infractions);
            }

            return judgment;
        }

        private List<Infraction> CheckForInfractions(Judgment judgment, ModelImporter accusedImporter)
        {
            List<Infraction> result = new List<Infraction>();
            List<Mesh> meshes = GetSubMeshes(accusedImporter);
            foreach(Mesh mesh in meshes)
            {
                List<Infraction> uvSetInfractions = CheckForUVSetInfractions(mesh);
                if(uvSetInfractions != null && uvSetInfractions.Count > 0)
                {
                    result.AddRange(uvSetInfractions);
                }
            }

            Infraction meshOriginInfraction = CheckMeshOriginInfraction(accusedImporter);
            if (meshOriginInfraction != null)
            {
                AddToJudgement(judgment, meshOriginInfraction);
            }

            List<Infraction> emptyNodeInfractions = CheckForEmptyNodeInfactions(accusedImporter);
            if(emptyNodeInfractions != null && emptyNodeInfractions.Count > 0)
            {
                result.AddRange(emptyNodeInfractions);
            }

            return result;
        }

        private List<Mesh> GetSubMeshes(ModelImporter modelImporter)
        {
            List<Mesh> subMeshes = new List<Mesh>();
            string assetPath = AssetDatabase.GetAssetPath(modelImporter);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is Mesh mesh)
                {
                    subMeshes.Add(mesh);
                }
            }

            return subMeshes;
        }

        private List<Infraction> CheckForUVSetInfractions(Mesh mesh)
        {
            List<Infraction> result = new List<Infraction>();
            bool hasFlippedUVs1 = HasFlippedUVs(mesh.uv, mesh.triangles, mesh);
            bool hasOverlappingUVs1 = HasOverlappingTriangles(mesh, mesh.uv);
            bool primaryUVsGood = !hasFlippedUVs1 && !hasOverlappingUVs1;
            bool hasSecondaryUVSet = false;
            if (primaryUVsGood)
            {
                hasSecondaryUVSet = HasSecondaryUVSet(mesh);
                if (hasSecondaryUVSet)
                {
                    result.Add(new Infraction 
                    {
                        isFixable = false,
                        message = ($"{mesh.name}'s primary UV set is valid. Secondary UV set should not have been used.") 
                    });
                }
                else
                    return result;
            }
            else
            {
                if (hasSecondaryUVSet)
                {
                    bool hasFlippedUVs2 = HasFlippedUVs(mesh.uv2, mesh.triangles, mesh);
                    bool hasOverlappingUVs2 = HasOverlappingTriangles(mesh, mesh.uv2);
                    result.Add(new Infraction
                    {
                        message = AssembleUVMessage(mesh.name, hasFlippedUVs2, hasOverlappingUVs2),
                        isFixable = false
                    });
                }
                else
                {
                    result.Add(new Infraction{
                        message = AssembleUVMessage(mesh.name, hasFlippedUVs1, hasOverlappingUVs1),
                        isFixable = false
                    });
                }
            }

            return result;
        }

        private string AssembleUVMessage(string objectName, bool hasFlippedUVs, bool hasOverlappingUVs, bool isSecondary = false)
        {
            string uvSetName = isSecondary ? "secondary" : "primary";
            string message = $"{objectName} has issues with the {uvSetName} UV set:";
            if (hasFlippedUVs)
            {
                message += " [ Flipped UVs ]";
            }
            if (hasOverlappingUVs)
            {
                message += " [ Overlapping UVs ]";
            }
            return message;
        }

        private List<Infraction> CheckForEmptyNodeInfactions(ModelImporter modelImporter)
        {
            List<Infraction> result = new List<Infraction>();
            GameObject rootObject = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(modelImporter));
            Transform rigRoot = null;
            SkinnedMeshRenderer skinnedMeshRenderer = rootObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                rigRoot = skinnedMeshRenderer.rootBone;
            }

            List<Transform> rigTransforms = new List<Transform>();
            if (rigRoot != null)
            {
                rigTransforms = rootObject.GetComponentsInChildren<Transform>().ToList();
            }

            foreach (Transform transform in rootObject.GetComponentsInChildren<Transform>())
            {
                if (rigTransforms.Contains(transform))
                    continue;

                if (transform.childCount == 0 && transform.GetComponents<Component>().Count() < 2)
                {
                    result.Add(new Infraction
                    {
                        isFixable = false,
                        message = $"{transform.name} - {transform.GetInstanceID()} is an empty node"
                    });
                }
            }

            return result;
        }

        private Infraction CheckMeshOriginInfraction(ModelImporter modelImporter)
        {
            Infraction result = null;
            GameObject rootObject = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(modelImporter));
            if (rootObject.GetComponents<Component>().Count() > 1)
            {
                if(rootObject.transform.localPosition != Vector3.zero)
                result = new Infraction
                {
                    isFixable = false,
                    message = $"{rootObject.name}: The pivot is not set at the origin (0,0,0)"
                };
            }
            else if(rootObject.GetComponents<Component>().Count() == 1)
            {
                for (int i = 0; i < rootObject.transform.childCount; i++)
                {
                    if (rootObject.transform.GetChild(i).transform.localPosition != Vector3.zero)
                    {
                        result = new Infraction
                        {
                            isFixable = false,
                            message = $"{rootObject.name} - {rootObject.transform.GetChild(i).name}: The pivot is not set at the origin (0,0,0)"
                        };
                    }
                }
            }

            return result;
        }

        private bool HasOverlappingTriangles(Mesh mesh, Vector2[] uvs)
        {
            int[] triangles = mesh.triangles;

            if (uvs.Length == 0 || triangles.Length < 6) // At least two triangles needed
                return false;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (i + 2 >= triangles.Length) break; // Ensure there are enough triangles

                // Get UVs for the first triangle
                Vector2 uv0 = uvs[triangles[i]];
                Vector2 uv1 = uvs[triangles[i + 1]];
                Vector2 uv2 = uvs[triangles[i + 2]];

                // Check against all other triangles
                for (int j = 0; j < triangles.Length; j += 3)
                {
                    if (i == j || j + 2 >= triangles.Length) continue; // Skip the same triangle

                    // Get UVs for the second triangle
                    Vector2 uv3 = uvs[triangles[j]];
                    Vector2 uv4 = uvs[triangles[j + 1]];
                    Vector2 uv5 = uvs[triangles[j + 2]];

                    // Skip if they share any vertices
                    if (SharesVertices(triangles, i, j))
                        continue;

                    // Check for overlap using SAT
                    if (DoTrianglesOverlap(uv0, uv1, uv2, uv3, uv4, uv5))
                    {
                        return true; // Overlapping triangles found
                    }
                }
            }

            return false; // No overlapping triangles found
        }

        private bool SharesVertices(int[] triangles, int indexA, int indexB)
        {
            HashSet<int> verticesA = new HashSet<int>
            {
                triangles[indexA],
                triangles[indexA + 1],
                triangles[indexA + 2]
            };

            // Check if any vertex in triangle B is also in triangle A
            return verticesA.Contains(triangles[indexB]) ||
                   verticesA.Contains(triangles[indexB + 1]) ||
                   verticesA.Contains(triangles[indexB + 2]);
        }

        private bool DoTrianglesOverlap(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 e, Vector2 f)
        {
            // Get the axes for both triangles
            Vector2[] axes = new Vector2[6];
            axes[0] = Perpendicular(b - a); // Edge AB
            axes[1] = Perpendicular(c - b); // Edge BC
            axes[2] = Perpendicular(a - c); // Edge CA
            axes[3] = Perpendicular(e - d); // Edge DE
            axes[4] = Perpendicular(f - e); // Edge EF
            axes[5] = Perpendicular(d - f); // Edge FD

            // Check for overlaps on each axis
            foreach (var axis in axes)
            {
                if (!ProjectAndCheckOverlap(axis, a, b, c, d, e, f))
                {
                    return false; // No overlap found
                }
            }
            return true; // Overlap found
        }

        private Vector2 Perpendicular(Vector2 v)
        {
            return new Vector2(-v.y, v.x); // Perpendicular vector
        }

        private bool ProjectAndCheckOverlap(Vector2 axis, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 e, Vector2 f)
        {
            // Project triangle A's vertices onto the axis
            float minA = float.MaxValue, maxA = float.MinValue;
            ProjectVertex(a, axis, ref minA, ref maxA);
            ProjectVertex(b, axis, ref minA, ref maxA);
            ProjectVertex(c, axis, ref minA, ref maxA);

            // Project triangle B's vertices onto the axis
            float minB = float.MaxValue, maxB = float.MinValue;
            ProjectVertex(d, axis, ref minB, ref maxB);
            ProjectVertex(e, axis, ref minB, ref maxB);
            ProjectVertex(f, axis, ref minB, ref maxB);

            // Check for overlap
            return maxA >= minB && maxB >= minA; // True if overlapping
        }

        private void ProjectVertex(Vector2 vertex, Vector2 axis, ref float min, ref float max)
        {
            float projection = Vector2.Dot(vertex, axis);
            if (projection < min) min = projection;
            if (projection > max) max = projection;
        }

        private bool HasFlippedUVs(Vector2[] uvs, int[] triangles, Mesh mesh)
        {
            if (uvs.Length < 3 || triangles.Length < 3)
                return false;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int index0 = triangles[i];
                int index1 = triangles[i + 1];
                int index2 = triangles[i + 2];

                Vector2 uv0 = uvs[index0];
                Vector2 uv1 = uvs[index1];
                Vector2 uv2 = uvs[index2];

                // Calculate the area of the triangle formed by the UVs
                float area = (uv1.x - uv0.x) * (uv2.y - uv0.y) - (uv2.x - uv0.x) * (uv1.y - uv0.y);

                if (area > 0)
                {
                    return true; // Flipped UV detected
                }
            }

            return false; // No flipped UVs detected
        }

        private bool HasSecondaryUVSet(Mesh mesh)
        {
            return mesh.uv2 != null && mesh.uv2.Length > 0;
        }
    }
}
