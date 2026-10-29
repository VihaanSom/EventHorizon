using UnityEngine;

namespace EventHorizon.Environment
{
    /// <summary>
    /// Static, seeded starfield rendered as a single billboard-quad mesh
    /// (one draw call, no per-star behaviours, no per-frame updates).
    /// Brightness tiers + a handful of bright reference stars; no flicker.
    /// Lensing displacement happens in the vertex shader so both eyes agree.
    /// </summary>
    [AddComponentMenu("EventHorizon/Environment/Starfield Controller")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class EHStarfieldController : MonoBehaviour
    {
        [Header("Population (mesh rebuilt only in the editor)")]
        [SerializeField] int starCount = 4200;
        [SerializeField] int brightReferenceStars = 24;
        [SerializeField] float sphereRadius = 800f;
        [SerializeField] int seed = 1337;

        [Header("Look")]
        [SerializeField] Material starMaterial;
        [SerializeField, Range(0f, 2f)] float brightnessGain = 1f;

        static readonly int BrightnessId = Shader.PropertyToID("_EH_StarBrightness");

        float currentBrightness = 1f;

        void Awake() => EnsureMesh();
        void OnValidate()
        {
            starCount = Mathf.Clamp(starCount, 100, 8000);
            EnsureMesh();
        }

        /// <summary>Called only by the director.</summary>
        public void ApplyBrightness(float b, bool snap)
        {
            currentBrightness = Mathf.Max(0f, b) * brightnessGain;
            if (snap && starMaterial != null)
                starMaterial.SetFloat(BrightnessId, currentBrightness);
        }

        void Update()
        {
            if (starMaterial == null || !Application.isPlaying) return;
            float shown = starMaterial.GetFloat(BrightnessId);
            starMaterial.SetFloat(BrightnessId,
                Mathf.Lerp(shown, currentBrightness, 1f - Mathf.Exp(-2f * Time.deltaTime)));
        }

        void EnsureMesh()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = starMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (filter.sharedMesh != null && filter.sharedMesh.name == MeshName())
                return;
            filter.sharedMesh = BuildStarMesh(starCount, brightReferenceStars, sphereRadius, seed);
        }

        string MeshName() => $"EH_Stars_{starCount}_{seed}";

        /// <summary>
        /// One quad per star. position = star centre; uv = corner offset (-1..1);
        /// uv2 = (sizePx-ish, magnitude 0..1, tintVariant, referenceFlag).
        /// </summary>
        public static Mesh BuildStarMesh(int count, int brightOnes, float radius, int randomSeed)
        {
            var rng = new System.Random(randomSeed);
            var mesh = new Mesh { name = $"EH_Stars_{count}_{randomSeed}" };
            var verts = new System.Collections.Generic.List<Vector3>(count * 4);
            var uvs = new System.Collections.Generic.List<Vector2>(count * 4);
            var uv2 = new System.Collections.Generic.List<Vector2>(count * 4); // x: size, y: magnitude
            var uv3 = new System.Collections.Generic.List<Vector2>(count * 4); // x: tint 0..1, y: reference flag
            var indices = new int[count * 6];

            Vector2[] corners = {
                new Vector2(-1f, -1f), new Vector2(1f, -1f),
                new Vector2(1f, 1f), new Vector2(-1f, 1f)
            };

            for (int s = 0; s < count; s++)
            {
                // Uniform on sphere via normal-distributed rejection-free method.
                Vector3 dir;
                do
                {
                    dir = new Vector3(
                        (float)(rng.NextDouble() * 2.0 - 1.0),
                        (float)(rng.NextDouble() * 2.0 - 1.0),
                        (float)(rng.NextDouble() * 2.0 - 1.0));
                } while (dir.sqrMagnitude > 1f || dir.sqrMagnitude < 1e-4f);
                dir.Normalize();

                bool reference = s < brightOnes;
                // Magnitude tiers: mostly dim, few bright; fixed at build time.
                float roll = (float)rng.NextDouble();
                float magnitude = reference
                    ? 1f
                    : roll < 0.55f ? 0.25f + roll * 0.4f
                    : roll < 0.9f ? 0.55f + (roll - 0.55f) * 1.1f
                    : 0.9f;
                float size = reference ? 3.2f : 0.9f + magnitude * 1.4f;
                float tint = (float)rng.NextDouble(); // 0 blue-white .. 1 warm

                Vector3 centre = dir * radius;
                for (int c = 0; c < 4; c++)
                {
                    verts.Add(centre);
                    uvs.Add(corners[c]);
                    uv2.Add(new Vector2(size, magnitude));
                    uv3.Add(new Vector2(tint, reference ? 1f : 0f));
                }
                int b = s * 6, q = s * 4;
                indices[b] = q; indices[b + 1] = q + 1; indices[b + 2] = q + 2;
                indices[b + 3] = q; indices[b + 4] = q + 2; indices[b + 5] = q + 3;
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, uv2);
            mesh.SetUVs(2, uv3);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.5f);
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
