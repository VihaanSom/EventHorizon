using UnityEngine;

namespace EventHorizon.BlackHole
{
    /// <summary>
    /// Sole writer of the shared black-hole shader globals (silhouette size,
    /// rim, photon ring). Anchored firmly in world space: the sky dome centers
    /// on the camera position but NEVER rotates with head movement, ensuring
    /// the black hole stays fixed in 3D space when the player turns their head in VR.
    /// </summary>
    [AddComponentMenu("EventHorizon/BlackHole/Black Hole Controller")]
    [ExecuteAlways]
    public class EHBlackHoleController : MonoBehaviour
    {
        [Header("Reference")]
        [Tooltip("Direction from origin toward the black-hole centre (object looks at it).")]
        [SerializeField] Transform holeAnchor;
        [SerializeField] Material skyMaterial;

        [Header("Silhouette")]
        [Range(0.5f, 3f)] public float baseAngularRadius = 1.0f;
        [Range(0f, 1f)] public float rimSoftness = 0.20f;
        [Range(0f, 5f)] public float photonRingIntensity = 1.4f;
        [Range(0f, 1f)] public float interiorBlackness = 1.0f;

        [Header("Sky dome (generated once at runtime)")]
        [SerializeField] float skyDomeRadius = 900f;
        [SerializeField] int skyDomeSegments = 64;

        [Header("Smoothing")]
        [SerializeField] float responseSpeed = 3f;

        static readonly int HoleDirId = Shader.PropertyToID("_EH_HoleDir");
        static readonly int HoleRadiusId = Shader.PropertyToID("_EH_HoleRadius");
        static readonly int RimId = Shader.PropertyToID("_EH_RimSoftness");
        static readonly int RingId = Shader.PropertyToID("_EH_PhotonRing");
        static readonly int BlackId = Shader.PropertyToID("_EH_InteriorBlack");

        float currentScale = 1f;
        float displayedScale = 1f;
        Transform skyDomeTransform;

        void Awake() => EnsureSkyDome();

        void OnEnable() => Push(currentScale);

        void OnValidate()
        {
            if (Application.isPlaying) return;
            Push(currentScale);
        }

        /// <summary>Called only by the director.</summary>
        public void ApplyScale(float scale, bool snap)
        {
            currentScale = Mathf.Max(0.05f, scale);
            if (snap)
            {
                displayedScale = currentScale;
                Push(displayedScale);
            }
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            displayedScale = Mathf.Lerp(displayedScale, currentScale,
                1f - Mathf.Exp(-responseSpeed * Time.deltaTime));
        }

        void LateUpdate()
        {
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

            // Center dome on camera translation, but STRICTLY lock rotation to world identity
            // so turning your head in VR rotates your view relative to the sky, not the sky with your head!
            if (skyDomeTransform != null)
            {
                skyDomeTransform.position = camPos;
                skyDomeTransform.rotation = Quaternion.identity;
            }

            Push(displayedScale);
        }

        void EnsureSkyDome()
        {
            if (skyMaterial == null) return;
            var existing = GameObject.Find("EH_SkyDome");
            if (existing != null)
            {
                skyDomeTransform = existing.transform;
                skyDomeTransform.SetParent(null, false);
                return;
            }

            var dome = new GameObject("EH_SkyDome");
            // Unparented so it lives in world root, completely decoupled from camera rotation
            dome.transform.SetParent(null, false);
            dome.transform.position = Vector3.zero;
            dome.transform.rotation = Quaternion.identity;
            var filter = dome.AddComponent<MeshFilter>();
            var renderer = dome.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildSkyDome(skyDomeRadius, skyDomeSegments);
            renderer.sharedMaterial = skyMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            skyDomeTransform = dome.transform;
        }

        /// <summary>Deterministic inverted sphere; shader culls front faces.</summary>
        public static Mesh BuildSkyDome(float radius, int segments)
        {
            var mesh = new Mesh { name = $"EH_SkyDome_{radius}_{segments}" };
            int rings = segments / 2 + 1;
            int cols = segments + 1;
            var verts = new System.Collections.Generic.List<Vector3>(rings * cols);
            var indices = new System.Collections.Generic.List<int>(segments * segments * 6);
            for (int r = 0; r < rings; r++)
            {
                float theta = Mathf.PI * r / (rings - 1);
                for (int c = 0; c < cols; c++)
                {
                    float phi = 2f * Mathf.PI * c / (cols - 1);
                    verts.Add(new Vector3(
                        radius * Mathf.Sin(theta) * Mathf.Cos(phi),
                        radius * Mathf.Cos(theta),
                        radius * Mathf.Sin(theta) * Mathf.Sin(phi)));
                }
            }
            for (int r = 0; r < rings - 1; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    int i0 = r * cols + c, i1 = i0 + 1, i2 = i0 + cols, i3 = i2 + 1;
                    indices.Add(i0); indices.Add(i1); indices.Add(i2);
                    indices.Add(i1); indices.Add(i3); indices.Add(i2);
                }
            }
            mesh.SetVertices(verts);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
            mesh.UploadMeshData(true);
            return mesh;
        }

        void Push(float scale)
        {
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            Vector3 target = (holeAnchor != null && (holeAnchor.position - transform.position).sqrMagnitude > 0.01f)
                ? holeAnchor.position
                : transform.position;
            Vector3 dir = (target - camPos).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;

            // Physical apparent angular radius based on true world distance
            float dist = Mathf.Max(0.5f, Vector3.Distance(camPos, target));
            float distanceGain = Mathf.Clamp(100f / dist, 0.7f, 8.5f);
            float effectiveScale = scale * distanceGain * 0.50f;

            Shader.SetGlobalVector(HoleDirId, dir);
            Shader.SetGlobalFloat(HoleRadiusId, baseAngularRadius * effectiveScale);
            Shader.SetGlobalFloat(RimId, rimSoftness);
            Shader.SetGlobalFloat(RingId, photonRingIntensity);
            Shader.SetGlobalFloat(BlackId, interiorBlackness);
            if (skyMaterial != null)
            {
                skyMaterial.SetVector(HoleDirId, dir);
                skyMaterial.SetFloat(HoleRadiusId, baseAngularRadius * effectiveScale);
            }
        }
    }
}
