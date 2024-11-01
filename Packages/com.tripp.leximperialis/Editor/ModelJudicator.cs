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
            Judgment judgment = base.Adjudicate(accused);
            Mesh accusedMesh = null;
            Debug.Log("Look here");
            // Check if the accused is a mesh
            if (accused is MeshFilter meshFilter)
            {
                accusedMesh = meshFilter.sharedMesh;
            }
            else if (accused is MeshRenderer meshRenderer)
            {
                accusedMesh = meshRenderer.GetComponent<MeshFilter>()?.sharedMesh;
            }

            if (accusedMesh != null)
            {
                if(judgment ==  null) 
                    judgment = new Judgment { accused = accused, judicator = this, infractions = new List<Infraction>() };

                // Check for overlapping UVs
                if (HasOverlappingUVs(accusedMesh))
                {
                    AddToJudgement(judgment, new Infraction
                    {
                        isFixable = false,
                        message = $"{accused.name} has overlapping UVs."
                    });
                }

                // Check for flipped UVs
                if (HasFlippedUVs(accusedMesh))
                {
                    AddToJudgement(judgment, new Infraction
                    {
                        isFixable = false,
                        message = $"{accused.name} has flipped UVs."
                    });
                }

                // Check if a secondary UV set exists for lightmaps
                if (!HasSecondaryUVSet(accusedMesh))
                {
                    AddToJudgement(judgment, new Infraction
                    {
                        isFixable = false,
                        message = $"{accused.name} is missing a secondary UV set for lightmaps."
                    });
                }

                if (judgment.infractions.Count == 0)
                {
                    judgment = null; // No infractions found
                }
            }

            return judgment;
        }

        private bool HasOverlappingUVs(Mesh mesh)
        {
            Vector2[] uvs = mesh.uv;
            if (uvs.Length == 0) return false;

            HashSet<Vector2> uvSet = new HashSet<Vector2>();
            foreach (var uv in uvs)
            {
                if (!uvSet.Add(uv))
                {
                    // Overlapping UV found
                    return true;
                }
            }
            return false;
        }

        private bool HasFlippedUVs(Mesh mesh)
        {
            Vector2[] uvs = mesh.uv;
            if (uvs.Length < 3) return false;

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector2 uv0 = uvs[mesh.triangles[i]];
                Vector2 uv1 = uvs[mesh.triangles[i + 1]];
                Vector2 uv2 = uvs[mesh.triangles[i + 2]];

                // Compute the signed area of the UV triangle (negative area = flipped)
                float area = (uv1.x - uv0.x) * (uv2.y - uv0.y) - (uv1.y - uv0.y) * (uv2.x - uv0.x);
                if (area < 0)
                {
                    return true; // Flipped UV detected
                }
            }

            return false;
        }

        private bool HasSecondaryUVSet(Mesh mesh)
        {
            // Check if a secondary UV set exists (usually for lightmaps)
            return mesh.uv2 != null && mesh.uv2.Length > 0;
        }

        public override string ServitudeImperpituis(Judgment judgment, Infraction infraction)
        {
            // No automatic fixes provided for UV issues
            return $"Manual fix required for {infraction.message} in {judgment.accused.name}.";
        }
    }
}
