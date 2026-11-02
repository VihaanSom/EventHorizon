using UnityEngine;

namespace EventHorizon.BlackHole
{
    /// <summary>
    /// Builds a clean, seamless relativistic accretion disk mesh and drives
    /// shader animation parameters. Seamlessly renders the equatorial plasma disk
    /// and the gravitationally lensed halo without harsh intersections.
    /// </summary>
    [AddComponentMenu("EventHorizon/BlackHole/Accretion Disk Controller")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class EHAccretionDiskController : MonoBehaviour
    {
        [Header("Disk Dimensions")]
        [SerializeField] float innerRadius = 14f;
        [SerializeField] float outerRadius = 55f;
        [SerializeField] int radialSegments = 32;
        [SerializeField] int angularSegments = 240;
        [SerializeField, Range(0f, 45f)] float tiltDegrees = 20f;

        [Header("Look")]
        [SerializeField] Material diskMaterial;
        [SerializeField, Range(0f, 4f)] float emissionGain = 1.3f;
        [SerializeField, Range(0f, 3f)] float flowSpeed = 0.40f;

        static readonly int EmissionId = Shader.PropertyToID("_EH_DiskEmission");
        static readonly int FlowId = Shader.PropertyToID("_EH_FlowTime");

        float currentEmission = 1f;
        float flowTime;

        public Material DiskMaterial => diskMaterial;

        void Awake() => EnsureMesh();

        void OnValidate()
        {
            radialSegments = Mathf.Clamp(radialSegments, 8, 64);
            angularSegments = Mathf.Clamp(angularSegments, 36, 360);
            innerRadius = Mathf.Max(1f, innerRadius);
            outerRadius = Mathf.Max(innerRadius + 2f, outerRadius);
            EnsureMesh();
            ApplyStaticTilt();
        }

        void Start() => ApplyStaticTilt();

        void ApplyStaticTilt()
        {
            transform.localRotation = Quaternion.Euler(tiltDegrees, 0f, 0f);
        }

        /// <summary>Called only by the director.</summary>
        public void ApplyEmission(float emission, bool snap)
        {
            currentEmission = Mathf.Max(0f, emission) * emissionGain;
            if (snap && diskMaterial != null)
                diskMaterial.SetFloat(EmissionId, currentEmission);
        }

        void Update()
        {
            if (diskMaterial == null) return;
            flowTime += Time.deltaTime * flowSpeed;
            diskMaterial.SetFloat(FlowId, flowTime);
            float shown = diskMaterial.GetFloat(EmissionId);
            diskMaterial.SetFloat(EmissionId,
                Mathf.Lerp(shown, currentEmission, 1f - Mathf.Exp(-2.5f * Time.deltaTime)));
        }

        void EnsureMesh()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = diskMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (filter.sharedMesh != null && filter.sharedMesh.name == MeshName())
                return;
            filter.sharedMesh = BuildRelativisticAccretionMesh(innerRadius, outerRadius, radialSegments, angularSegments);
        }

        string MeshName() => $"EH_RelativisticDisk_{innerRadius}_{outerRadius}_{radialSegments}_{angularSegments}";

        /// <summary>
        /// Builds a unified relativistic accretion disk:
        /// 1. Primary equatorial disk (smooth continuous plane).
        /// 2. Relativistic upper lensed halo (bent over the black hole shadow).
        /// 3. Relativistic lower lensed halo (bent underneath).
        /// All parts have seamless polar UVs and softened boundaries.
        /// </summary>
        public static Mesh BuildRelativisticAccretionMesh(float inner, float outer, int radial, int angular)
        {
            var mesh = new Mesh { name = $"EH_RelativisticDisk_{inner}_{outer}_{radial}_{angular}" };
            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var indices = new System.Collections.Generic.List<int>();

            void AddGrid(System.Func<int, int, (Vector3 pos, Vector2 uv)> evaluator, int rows, int columns)
            {
                int baseIndex = verts.Count;
                int cols = columns + 1;
                for (int r = 0; r <= rows; r++)
                {
                    for (int c = 0; c <= columns; c++)
                    {
                        var pt = evaluator(r, c);
                        verts.Add(pt.pos);
                        uvs.Add(pt.uv);
                    }
                }
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        int i0 = baseIndex + r * cols + c;
                        int i1 = i0 + 1;
                        int i2 = i0 + cols;
                        int i3 = i2 + 1;
                        indices.Add(i0); indices.Add(i2); indices.Add(i1);
                        indices.Add(i1); indices.Add(i2); indices.Add(i3);
                    }
                }
            }

            // 1. Primary Equatorial Disk
            AddGrid((r, c) =>
            {
                float fr = (float)r / radial;
                float fa = (float)(c % angular) / angular;
                float radius = Mathf.Lerp(inner, outer, fr);
                float ang = fa * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                return (pos, new Vector2(fa, fr));
            }, radial, angular);

            // 2. Relativistic Upper Lensed Halo (Smooth arch curving over the shadow)
            int archRadial = Mathf.Max(6, radial / 2);
            int archAngular = Mathf.Max(32, angular / 2);
            float rHaloInner = inner * 1.05f;
            float rHaloOuter = inner * 1.05f + (outer - inner) * 0.55f;

            AddGrid((r, c) =>
            {
                float fr = (float)r / archRadial;
                float fa = (float)c / archAngular;
                float theta = fa * Mathf.PI;
                float radius = Mathf.Lerp(rHaloInner, rHaloOuter, fr);
                // Arch in XY behind the hole (negative Z offset to place it behind the event horizon)
                float x = -Mathf.Cos(theta) * radius;
                float y = Mathf.Sin(theta) * radius * 0.95f;
                float z = -Mathf.Sin(theta) * (inner * 0.35f);
                return (new Vector3(x, y, z), new Vector2(fa * 0.5f, fr));
            }, archRadial, archAngular);

            // 3. Relativistic Lower Lensed Halo (Smooth arch curving underneath the shadow)
            float rHaloLowerOuter = inner * 1.05f + (outer - inner) * 0.38f;
            AddGrid((r, c) =>
            {
                float fr = (float)r / archRadial;
                float fa = (float)c / archAngular;
                float theta = fa * Mathf.PI;
                float radius = Mathf.Lerp(rHaloInner, rHaloLowerOuter, fr);
                float x = -Mathf.Cos(theta) * radius;
                float y = -Mathf.Sin(theta) * radius * 0.75f;
                float z = -Mathf.Sin(theta) * (inner * 0.35f);
                return (new Vector3(x, y, z), new Vector2(fa * 0.5f + 0.5f, fr));
            }, archRadial, archAngular);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * outer * 3.5f);
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
