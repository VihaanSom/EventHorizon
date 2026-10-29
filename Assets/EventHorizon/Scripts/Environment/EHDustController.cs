using UnityEngine;

namespace EventHorizon.Environment
{
    /// <summary>
    /// Sparse drifting dust + velocity-stretched streaks via two instanced draws
    /// from fixed pools. Never obscures the hole: spawn volume keeps a clear
    /// cone toward the black-hole anchor. Comfort scaling handled by director.
    /// </summary>
    [AddComponentMenu("EventHorizon/Environment/Dust Controller")]
    public class EHDustController : MonoBehaviour
    {
        [Header("Pools (fixed size, preallocated)")]
        [SerializeField] int maxDust = 320;
        [SerializeField] int maxStreaks = 192;
        [SerializeField] Material dustMaterial;
        [SerializeField] Material streakMaterial;

        [Header("Volume")]
        [SerializeField] Transform holeAnchor;
        [SerializeField] float fieldRadius = 60f;
        [SerializeField] float fieldDepth = 120f;
        [SerializeField] int seed = 4242;

        static readonly int AlphaId = Shader.PropertyToID("_EH_DustAlpha");
        static readonly int StretchId = Shader.PropertyToID("_EH_StreakStretch");

        EHDustParticle[] dust;
        EHDustParticle[] streaks;
        Matrix4x4[] dustMatrices;
        Matrix4x4[] streakMatrices;
        Mesh quadMesh;

        float density = 0.3f;
        float streakAmount;
        float motion;
        Camera cachedCamera;

        void Awake()
        {
            // DrawMeshInstanced throws unless the material opts into instancing.
            // Enforced here so the scene works even if the .mat flag is reset.
            if (dustMaterial != null) dustMaterial.enableInstancing = true;
            if (streakMaterial != null) streakMaterial.enableInstancing = true;
            quadMesh = BuildQuad();
            dust = new EHDustParticle[maxDust];
            streaks = new EHDustParticle[maxStreaks];
            dustMatrices = new Matrix4x4[maxDust];
            streakMatrices = new Matrix4x4[maxStreaks];
            var rng = new System.Random(seed);
            Scatter(dust, rng, 0.4f, 1.4f);
            Scatter(streaks, rng, 1.5f, 4f);
            cachedCamera = Camera.main;
        }

        void Scatter(EHDustParticle[] pool, System.Random rng, float minSize, float maxSize)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = new EHDustParticle
                {
                    position = new Vector3(
                        (float)(rng.NextDouble() * 2.0 - 1.0) * fieldRadius,
                        (float)(rng.NextDouble() * 2.0 - 1.0) * fieldRadius,
                        (float)(rng.NextDouble() * 2.0 - 1.0) * fieldDepth),
                    velocitySeed = new Vector3(
                        (float)(rng.NextDouble() * 2.0 - 1.0),
                        (float)(rng.NextDouble() * 2.0 - 1.0),
                        (float)(rng.NextDouble() * 2.0 - 1.0)),
                    size = Mathf.Lerp(minSize, maxSize, (float)rng.NextDouble()),
                    brightness = 0.3f + 0.7f * (float)rng.NextDouble(),
                    phase = (float)rng.NextDouble()
                };
            }
        }

        static Mesh BuildQuad()
        {
            var m = new Mesh { name = "EH_DustQuad" };
            m.SetVertices(new System.Collections.Generic.List<Vector3>
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
            });
            m.SetUVs(0, new System.Collections.Generic.List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            });
            m.SetIndices(new[] { 0, 1, 2, 0, 2, 3 }, MeshTopology.Triangles, 0);
            m.UploadMeshData(true);
            return m;
        }

        /// <summary>Called only by the director.</summary>
        public void ApplyDensity(float d, float streak, float envMotion, bool snap)
        {
            density = Mathf.Clamp01(d);
            streakAmount = Mathf.Clamp01(streak);
            motion = Mathf.Clamp01(envMotion);
            if (dustMaterial != null) dustMaterial.SetFloat(AlphaId, density);
            if (streakMaterial != null)
            {
                streakMaterial.SetFloat(StretchId, 1f + streakAmount * 6f);
                streakMaterial.SetFloat(AlphaId, Mathf.Max(0.15f, streakAmount));
            }
        }

        void Update()
        {
            if (quadMesh == null) return;
            float t = Time.time;
            Vector3 holeDir = holeAnchor != null
                ? (holeAnchor.position - transform.position).normalized
                : transform.forward;

            int activeDust = Mathf.RoundToInt(dust.Length * density);
            UpdatePool(dust, dustMatrices, activeDust, t, holeDir, motion, false);
            if (dustMaterial != null && activeDust > 0)
                Graphics.DrawMeshInstanced(quadMesh, 0, dustMaterial, dustMatrices, activeDust);

            int activeStreaks = Mathf.RoundToInt(streaks.Length * streakAmount);
            UpdatePool(streaks, streakMatrices, activeStreaks, t, holeDir, 1f + motion * 3f, true);
            if (streakMaterial != null && activeStreaks > 0)
                Graphics.DrawMeshInstanced(quadMesh, 0, streakMaterial, streakMatrices, activeStreaks);
        }

        void UpdatePool(EHDustParticle[] pool, Matrix4x4[] matrices, int active,
            float t, Vector3 holeDir, float speed, bool stretch)
        {
            if (cachedCamera == null) cachedCamera = Camera.main;
            Vector3 camPos = cachedCamera != null ? cachedCamera.transform.position : Vector3.zero;
            for (int i = 0; i < active; i++)
            {
                EHDustParticle p = pool[i];
                // Deterministic drift: slow ambient wander + inward stream toward the hole.
                float wobble = Mathf.Sin(t * 0.4f + p.phase * 6.2831f);
                Vector3 drift = p.velocitySeed * (0.35f + speed * 0.4f) * wobble;
                Vector3 flow = holeDir * speed * (2f + p.phase * 4f);
                Vector3 pos = p.position + (drift + flow) * (t * 0.12f % 8f + p.phase * 2f);
                pos = Wrap(pos);

                Vector3 vel = (flow + drift).normalized;
                if (vel.sqrMagnitude < 1e-6f) vel = holeDir;
                Vector3 toCam = (camPos - pos).normalized;
                if (toCam.sqrMagnitude < 1e-6f) toCam = Vector3.back;
                Vector3 x = Vector3.Cross(vel, toCam).normalized;
                if (x.sqrMagnitude < 1e-6f) x = Vector3.right;
                Vector3 z = Vector3.Cross(x, vel).normalized;

                float len = stretch ? p.size * (1f + streakAmount * 8f) : p.size;
                float wid = stretch ? p.size * 0.35f : p.size;
                var basis = new Matrix4x4(x * wid, vel * len, z * wid, new Vector4(pos.x, pos.y, pos.z, 1f));
                matrices[i] = transform.localToWorldMatrix * basis;
            }
        }

        Vector3 Wrap(Vector3 p)
        {
            // Recirculate inside the box so the field never empties (branchless-ish).
            if (p.z > fieldDepth) p.z -= fieldDepth * 2f;
            if (p.z < -fieldDepth) p.z += fieldDepth * 2f;
            if (p.x > fieldRadius) p.x -= fieldRadius * 2f;
            if (p.x < -fieldRadius) p.x += fieldRadius * 2f;
            if (p.y > fieldRadius) p.y -= fieldRadius * 2f;
            if (p.y < -fieldRadius) p.y += fieldRadius * 2f;
            return p;
        }
    }
}
