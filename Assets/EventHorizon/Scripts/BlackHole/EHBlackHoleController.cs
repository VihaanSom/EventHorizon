using UnityEngine;

namespace EventHorizon.BlackHole
{
    /// <summary>
    /// Sole writer of the shared black-hole shader globals (silhouette size,
    /// rim, photon ring). The lensing controller forwards through here so only
    /// one component ever touches these properties. No shadow maps are used:
    /// the interior is unlit near-black by construction.
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
        [Range(0.5f, 3f)] public float baseAngularRadius = 1f;
        [Range(0f, 1f)] public float rimSoftness = 0.35f;
        [Range(0f, 3f)] public float photonRingIntensity = 1.2f;
        [Range(0f, 1f)] public float interiorBlackness = 0.985f;

        [Header("Sky dome (generated once at runtime)")]
        [SerializeField] float skyDomeRadius = 900f;
        [SerializeField] int skyDomeSegments = 48;

        [Header("Smoothing")]
        [SerializeField] float responseSpeed = 2f;

        static readonly int HoleDirId = Shader.PropertyToID("_EH_HoleDir");
        static readonly int HoleRadiusId = Shader.PropertyToID("_EH_HoleRadius");
        static readonly int RimId = Shader.PropertyToID("_EH_RimSoftness");
        static readonly int RingId = Shader.PropertyToID("_EH_PhotonRing");
        static readonly int BlackId = Shader.PropertyToID("_EH_InteriorBlack");

        float currentScale = 1f;
        float displayedScale = 1f;

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
            // Gentle smoothing only; the timeline remains the authority.
            displayedScale = Mathf.Lerp(displayedScale, currentScale,
                1f - Mathf.Exp(-responseSpeed * Time.deltaTime));
            Push(displayedScale);
        }

        void EnsureSkyDome()
        {
            if (skyMaterial == null) return;
            var dome = new GameObject("EH_SkyDome");
            dome.transform.SetParent(transform, false);
            dome.transform.localPosition = Vector3.zero;
            var filter = dome.AddComponent<MeshFilter>();
            var renderer = dome.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildSkyDome(skyDomeRadius, skyDomeSegments);
            renderer.sharedMaterial = skyMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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
            // Explicit bounds: generated meshes default to zero-size and would
            // otherwise be frustum-culled as invisible.
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
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;

            // Keep the region dark: radius is an angle-like factor on the sky.
            Shader.SetGlobalVector(HoleDirId, dir);
            Shader.SetGlobalFloat(HoleRadiusId, baseAngularRadius * scale);
            Shader.SetGlobalFloat(RimId, rimSoftness);
            Shader.SetGlobalFloat(RingId, photonRingIntensity);
            Shader.SetGlobalFloat(BlackId, interiorBlackness);
            if (skyMaterial != null)
            {
                skyMaterial.SetVector(HoleDirId, dir);
                skyMaterial.SetFloat(HoleRadiusId, baseAngularRadius * scale);
            }
        }
    }
}
