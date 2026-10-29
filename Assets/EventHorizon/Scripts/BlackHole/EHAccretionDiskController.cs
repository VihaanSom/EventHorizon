using UnityEngine;

namespace EventHorizon.BlackHole
{
    /// <summary>
    /// Builds a static annular mesh once (never regenerated per frame) and
    /// drives only the disk material's emission/flow properties. Emissive
    /// shading carries the detail; no lights or shadow maps are involved.
    /// </summary>
    [AddComponentMenu("EventHorizon/BlackHole/Accretion Disk Controller")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class EHAccretionDiskController : MonoBehaviour
    {
        [Header("Annulus (mesh rebuilt only when these change in the editor)")]
        [SerializeField] float innerRadius = 14f;
        [SerializeField] float outerRadius = 42f;
        [SerializeField] int radialSegments = 24;
        [SerializeField] int angularSegments = 180;
        [SerializeField, Range(0f, 45f)] float tiltDegrees = 18f;

        [Header("Look")]
        [SerializeField] Material diskMaterial;
        [SerializeField, Range(0f, 4f)] float emissionGain = 1f;
        [SerializeField, Range(0f, 3f)] float flowSpeed = 0.35f;

        static readonly int EmissionId = Shader.PropertyToID("_EH_DiskEmission");
        static readonly int FlowId = Shader.PropertyToID("_EH_FlowTime");

        float currentEmission = 1f;
        float flowTime;

        public Material DiskMaterial => diskMaterial;

        void Awake() => EnsureMesh();

        void OnValidate()
        {
            radialSegments = Mathf.Clamp(radialSegments, 4, 64);
            angularSegments = Mathf.Clamp(angularSegments, 24, 360);
            innerRadius = Mathf.Max(1f, innerRadius);
            outerRadius = Mathf.Max(innerRadius + 1f, outerRadius);
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
            // Flow time is shader animation input, not a mesh rebuild.
            flowTime += Time.deltaTime * flowSpeed;
            diskMaterial.SetFloat(FlowId, flowTime);
            float shown = diskMaterial.GetFloat(EmissionId);
            diskMaterial.SetFloat(EmissionId,
                Mathf.Lerp(shown, currentEmission, 1f - Mathf.Exp(-2f * Time.deltaTime)));
        }

        void EnsureMesh()
        {
            // Self-provision rendering components so hand-authored scenes and
            // prefabs only need the controller itself.
            var filter = GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = diskMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (filter.sharedMesh != null && filter.sharedMesh.name == MeshName())
                return;
            filter.sharedMesh = BuildAnnulus(innerRadius, outerRadius, radialSegments, angularSegments);
        }

        string MeshName() => $"EH_Disk_{innerRadius}_{outerRadius}_{radialSegments}_{angularSegments}";

        /// <summary>Deterministic static relativistic warped accretion mesh; UV.x = angle 0..1 (seamless), UV.y = radius 0..1.</summary>
        public static Mesh BuildAnnulus(float inner, float outer, int radial, int angular)
        {
            var mesh = new Mesh { name = $"EH_Disk_{inner}_{outer}_{radial}_{angular}" };
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

            // 1. Equatorial Annulus (Direct Primary Disk)
            AddGrid((r, c) =>
            {
                float fr = (float)r / radial;
                float fa = (float)(c % angular) / angular;
                float radius = Mathf.Lerp(inner, outer, fr);
                float ang = fa * Mathf.PI * 2f;
                return (new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius), new Vector2(fa, fr));
            }, radial, angular);

            // 2. Upper Lensed Halo Arch (Gravitational deflection bending over the black hole shadow)
            int archRadial = Mathf.Max(6, radial / 2);
            int archAngular = Mathf.Max(24, angular / 2);
            float rArchInner = inner * 1.02f;
            float rArchOuter = inner * 1.02f + (outer - inner) * 0.62f;
            AddGrid((r, c) =>
            {
                float fr = (float)r / archRadial;
                float fa = (float)c / archAngular;
                float theta = fa * Mathf.PI;
                float radius = Mathf.Lerp(rArchInner, rArchOuter, fr);
                float x = -Mathf.Cos(theta) * radius;
                float y = Mathf.Sin(theta) * radius;
                float z = Mathf.Sin(theta) * inner * 0.22f;
                return (new Vector3(x, y, z), new Vector2(fa, fr));
            }, archRadial, archAngular);

            // 3. Lower Lensed Halo Arch (Gravitational deflection bending underneath the shadow)
            float rArchLowerOuter = inner * 1.02f + (outer - inner) * 0.45f;
            AddGrid((r, c) =>
            {
                float fr = (float)r / archRadial;
                float fa = (float)c / archAngular;
                float theta = fa * Mathf.PI;
                float radius = Mathf.Lerp(rArchInner, rArchLowerOuter, fr);
                float x = -Mathf.Cos(theta) * radius;
                float y = -Mathf.Sin(theta) * radius * 0.68f;
                float z = Mathf.Sin(theta) * inner * 0.22f;
                return (new Vector3(x, y, z), new Vector2(fa, fr));
            }, archRadial, archAngular);

            // 4. Relativistic Inner ISCO Collar (Photon Sphere ring collar)
            AddGrid((r, c) =>
            {
                float fr = (float)r;
                float fa = (float)(c % angular) / angular;
                float ang = fa * Mathf.PI * 2f;
                float y = (fr - 0.5f) * (inner * 0.08f);
                return (new Vector3(Mathf.Cos(ang) * inner, y, Mathf.Sin(ang) * inner), new Vector2(fa, 0.02f));
            }, 1, angular);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * outer * 3.0f);
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
