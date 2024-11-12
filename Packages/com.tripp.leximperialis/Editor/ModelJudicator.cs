using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TRIPP.LexImperialis.Editor
{
    [CreateAssetMenu(fileName = "ModelJudicator", menuName = "ScriptableObjects/LexImperialis/ModelJudicator")]
    public class ModelJudicator : ImporterJudicator
    {
        public override Judgment Adjudicate(Object accused)
        {
            // Call the base class method to get the initial judgment
            Judgment judgment = base.Adjudicate(accused);

            // Cast the accused object to GameObject directly
            GameObject gameObject = (GameObject)accused;

            // Retrieve the mesh associated with the GameObject
            Mesh accusedMesh = GetMesh(gameObject);

            // Log an error if no mesh is found and return null if there's no mesh
            if (accusedMesh == null)
            {
                Debug.LogError($"{gameObject.name} has no mesh associated with it.");
                return null; // Return null if there's no mesh
            }

            // If judgment is null, initialize it with details about the accused object
            if (judgment == null)
            {
                judgment = new Judgment
                {
                    accused = accused,      // Set the object being judged
                    judicator = this,       // Reference to the current judicator instance
                    infractions = new List<Infraction>() // Initialize the list for infractions
                };
            }

            // Check the mesh for any UV infractions and add them to the judgment
            CheckForInfractions(judgment, accusedMesh, gameObject);

            // If no infractions were found, return null judgment
            if (judgment.infractions.Count == 0)
            {
                return null; // Return null judgment if no infractions are found
            }

            return judgment; // Return the populated judgment
        }

        private Mesh GetMesh(GameObject gameObject)
        {
            // Attempt to get the MeshFilter component from the provided GameObject
            if (gameObject.TryGetComponent(out MeshFilter meshFilter))
            {
                // If the MeshFilter component is found, return its shared mesh
                return meshFilter.sharedMesh;
            }
            // If no MeshFilter is found, attempt to get the MeshRenderer component
            else if (gameObject.TryGetComponent(out MeshRenderer meshRenderer))
            {
                // If the MeshRenderer is found, retrieve its associated MeshFilter and return its shared mesh
                // Use null-conditional operator to safely access the MeshFilter's shared mesh
                return meshRenderer.GetComponent<MeshFilter>()?.sharedMesh;
            }
            return null;
        }

        private void CheckForInfractions(Judgment judgment, Mesh mesh, GameObject accusedObject)
        {
            string objectName = accusedObject.name;

            // Check for primary UV set issues
            bool hasFlippedUVs1 = HasFlippedUVs(mesh.uv, mesh.triangles, mesh);
            bool hasOverlappingUVs1 = HasOverlappingTriangles(mesh, mesh.uv);

            // Check if the primary UV set is good
            bool primaryUVsGood = !hasFlippedUVs1 && !hasOverlappingUVs1;

            // Check if there is a secondary UV set
            bool hasSecondaryUVSet = HasSecondaryUVSet(mesh);

            // Check if pivot is at (0,0,0)
            bool isPivotAtOrigin = IsMeshPivotAtOrigin(mesh);

            // Check if hierarchy has no empty children
            bool isHierarchyPure = IsHierarchyClean(accusedObject);

            if(!isPivotAtOrigin)
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false,
                    message = $"{objectName}: The pivot is not set at the origin (0,0,0)"
                });
            }

            if (!isHierarchyPure)
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false,
                    message = $"{objectName}: The hierarchy of the object contains empty children"
                });
            }


            // If the primary UV set is valid, log the message and check secondary UV set
            if (primaryUVsGood)
            {
                if (hasSecondaryUVSet)
                {
                    // Notify the user that the second UV set is unnecessary
                    AddToJudgement(judgment, new Infraction
                    {
                        isFixable = true,
                        message = $"{objectName}: The primary UV set is valid, so the secondary UV set is not needed."
                    });
                }
                return; // Exit since primary UV set is good
            }

            // Proceed to check the secondary UV set if the primary UV set is not good
            bool hasFlippedUVs2 = hasSecondaryUVSet && HasFlippedUVs(mesh.uv2, mesh.triangles, mesh);
            bool hasOverlappingUVs2 = hasSecondaryUVSet && HasOverlappingTriangles(mesh, mesh.uv2);

            // Report infractions for the primary UV set
            if (hasFlippedUVs1)
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false,
                    message = $"{objectName} has flipped UVs in the primary UV set."
                });
            }

            if (hasOverlappingUVs1)
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false,
                    message = $"{objectName} has overlapping UVs in the primary UV set."
                });
            }

            // Only check the secondary UV set if it exists
            if (hasSecondaryUVSet)
            {
                if (hasFlippedUVs2)
                {
                    AddToJudgement(judgment, new Infraction
                    {
                        isFixable = false,
                        message = $"{objectName} has flipped UVs in the secondary UV set."
                    });
                }

                if (hasOverlappingUVs2)
                {
                    AddToJudgement(judgment, new Infraction
                    {
                        isFixable = false,
                        message = $"{objectName} has overlapping UVs in the secondary UV set."
                    });
                }
            }
            else
            {
                AddToJudgement(judgment, new Infraction
                {
                    isFixable = false,
                    message = $"{objectName} is missing a secondary UV set for lightmaps."
                });
            }

            // If infractions found, judgment remains populated
        }

        //Returns true if GameObject contains the indicated Component, false otherwise
        private bool HasComponent<T>(GameObject obj)where T:Component
        {
            return obj.GetComponent<T>() != null;
        }

        private bool IsHierarchyClean(GameObject rootObject)
        {
            //Has a skinned mesh renderer been found?
            bool foundRig = false;
            //Transform rigRootBone;
            int rigFoundAt = -1;

            for (int i = 0; i< rootObject.transform.childCount; i++)
            {
                //Populates an array with all the children of the children in the first level
                Transform[] childArray = rootObject.transform.GetChild(i).gameObject.GetComponentsInChildren<Transform>();


                foreach (Transform t in childArray)
                {
                    if (HasComponent<SkinnedMeshRenderer>(t.gameObject))
                    {                      
                        foundRig = true;
                        //rigRootBone = t.GetComponent<SkinnedMeshRenderer>().rootBone;
                        rigFoundAt = i;
                        break;
                    }                                              
                }

                if (foundRig)
                    break;

            }

            for (int i = 0; i < rootObject.transform.childCount; i++)
            {
                if (foundRig && i == rigFoundAt)
                    continue;

                //Populates an array with all the children of the children in the first level
                Transform[] childArray = rootObject.transform.GetChild(i).gameObject.GetComponentsInChildren<Transform>();

                foreach (Transform t in childArray)
                {
                    if (t.childCount == 0 && t.GetComponent<Component>() == null)
                        //Found dirty node
                        return false;
                }

            }


            //Clean hierarchy
            return true;
        }

        private bool IsMeshPivotAtOrigin(Mesh mesh)
        {
            
            Vector3 pivotOffset = mesh.bounds.center;

            if(pivotOffset.x == 0f &&
               pivotOffset.y == 0f &&
               pivotOffset.z == 0f)
            {
                return true;
            }

            return false;
            
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
