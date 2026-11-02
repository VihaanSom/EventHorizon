using UnityEngine;

namespace EventHorizon.BlackHole
{
    /// <summary>
    /// Builds an authentic Interstellar (Gargantua) black hole and accretion disk:
    /// 1. Central Event Horizon Sphere (solid black, depth-writing; guarantees the core is never a fireball).
    /// 2. Primary Equatorial Disk (horizontal glowing plasma ring crossing in front of the shadow).
    /// 3. Lensed Halo Arch (vertical glowing halo wrapping over and under the black sphere).
    /// Avoids CreatePrimitive/DestroyImmediate to ensure clean execution during OnValidate.
    /// </summary>
    [AddComponentMenu("EventHorizon/BlackHole/Accretion Disk Controller")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class EHAccretionDiskController : MonoBehaviour
    {
        [Header("Gargantua Scale")]
        [SerializeField] float horizonRadius = 14.0f;
        [SerializeField] float diskInnerRadius = 14.3f;
        [SerializeField] float diskOuterRadius = 52.0f;
        [SerializeField] float haloOuterRadius = 36.0f;
        [SerializeField, Range(0f, 45f)] float tiltDegrees = 25.0f;

        [Header("Look")]
        [SerializeField] Material diskMaterial;
        [SerializeField, Range(0f, 3f)] float emissionGain = 1.0f;
        [SerializeField, Range(0f, 2f)] float flowSpeed = 0.30f;

        static readonly int EmissionId = Shader.PropertyToID("_EH_DiskEmission");
        static readonly int FlowId = Shader.PropertyToID("_EH_FlowTime");

        float currentEmission = 1f;
        float flowTime;
        GameObject horizonSphere;
        static Mesh sharedSphereMesh;

        public Material DiskMaterial => diskMaterial;

        void Awake()
        {
            EnsureMesh();
            EnsureHorizonSphere();
            ApplyStaticTilt();
        }

        void OnValidate()
        {
            horizonRadius = Mathf.Max(1f, horizonRadius);
            diskInnerRadius = Mathf.Max(horizonRadius + 0.1f, diskInnerRadius);
            diskOuterRadius = Mathf.Max(diskInnerRadius + 5f, diskOuterRadius);
            haloOuterRadius = Mathf.Clamp(haloOuterRadius, diskInnerRadius + 2f, diskOuterRadius);
            EnsureMesh();
            EnsureHorizonSphere();
            ApplyStaticTilt();
        }

        void Start()
        {
            EnsureHorizonSphere();
            ApplyStaticTilt();
        }

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

        void EnsureHorizonSphere()
        {
            var existing = transform.Find("EH_HorizonSphere");
            if (existing != null)
            {
                horizonSphere = existing.gameObject;
                horizonSphere.transform.localScale = Vector3.one * (horizonRadius * 2.0f);
                return;
            }

            // Build GameObject directly without Collider to prevent DestroyImmediate errors in OnValidate
            horizonSphere = new GameObject("EH_HorizonSphere");
            horizonSphere.transform.SetParent(transform, false);
            horizonSphere.transform.localPosition = Vector3.zero;
            horizonSphere.transform.localScale = Vector3.one * (horizonRadius * 2.0f);

            var filter = horizonSphere.AddComponent<MeshFilter>();
            var sphereRenderer = horizonSphere.AddComponent<MeshRenderer>();

            if (sharedSphereMesh == null)
            {
                sharedSphereMesh = BuildSphereMesh(0.5f, 32, 48);
            }
            filter.sharedMesh = sharedSphereMesh;

            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
            var blackMat = new Material(unlitShader);
            blackMat.name = "EH_BlackHoleCore_Mat";
            blackMat.color = Color.black;
            if (blackMat.HasProperty("_Surface")) blackMat.SetFloat("_Surface", 0); // Opaque
            blackMat.renderQueue = 2000; // Geometry queue (writes to depth buffer)
            sphereRenderer.sharedMaterial = blackMat;
            sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sphereRenderer.receiveShadows = false;
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
            filter.sharedMesh = BuildInterstellarMesh(diskInnerRadius, diskOuterRadius, haloOuterRadius);
        }

        /// <summary>
        /// Builds procedural sphere mesh without any collider components.
        /// </summary>
        public static Mesh BuildSphereMesh(float radius, int rings, int cols)
        {
            var mesh = new Mesh { name = "EH_ProceduralSphere" };
            var verts = new System.Collections.Generic.List<Vector3>();
            var indices = new System.Collections.Generic.List<int>();

            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float theta = v * Mathf.PI;
                for (int c = 0; c <= cols; c++)
                {
                    float u = (float)c / cols;
                    float phi = u * Mathf.PI * 2f;
                    float x = Mathf.Sin(theta) * Mathf.Cos(phi) * radius;
                    float y = Mathf.Cos(theta) * radius;
                    float z = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;
                    verts.Add(new Vector3(x, y, z));
                }
            }

            int colCount = cols + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int i0 = r * colCount + c;
                    int i1 = i0 + 1;
                    int i2 = i0 + colCount;
                    int i3 = i2 + 1;
                    indices.Add(i0); indices.Add(i2); indices.Add(i1);
                    indices.Add(i1); indices.Add(i2); indices.Add(i3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>
        /// Builds the authentic Interstellar (Gargantua) accretion disk geometry:
        /// 1. Equatorial Primary Disk: flat horizontal ring in the XZ plane.
        /// 2. Upper Lensed Halo Arch: light from the back of the disk bent up over the black hole (Z >= 3.5m, Y >= 0).
        /// 3. Lower Lensed Halo Arch: light from the back of the disk bent down under the black hole (Z >= 3.5m, Y <= 0).
        /// Placed behind the central Event Horizon sphere so it naturally occludes the inner region,
        /// leaving the pure black silhouette of Gargantua with zero perpendicular intersecting geometry.
        /// </summary>
        public static Mesh BuildInterstellarMesh(float inner, float outer, float haloOuter)
        {
            var mesh = new Mesh { name = "EH_InterstellarGargantuaDisk" };
            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var indices = new System.Collections.Generic.List<int>();

            void AddAnnulus(System.Func<float, float, Vector3> posEvaluator, int rows, int cols, float uvYOffset, float uvYScale, float uvXOffset, float uvXScale)
            {
                int baseIndex = verts.Count;
                int colCount = cols + 1;
                for (int r = 0; r <= rows; r++)
                {
                    float fr = (float)r / rows;
                    for (int c = 0; c <= cols; c++)
                    {
                        float fa = (float)c / cols;
                        Vector3 pos = posEvaluator(fr, fa);
                        verts.Add(pos);
                        uvs.Add(new Vector2(uvXOffset + fa * uvXScale, uvYOffset + fr * uvYScale));
                    }
                }
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int i0 = baseIndex + r * colCount + c;
                        int i1 = i0 + 1;
                        int i2 = i0 + colCount;
                        int i3 = i2 + 1;
                        // Double-sided faces
                        indices.Add(i0); indices.Add(i2); indices.Add(i1);
                        indices.Add(i1); indices.Add(i2); indices.Add(i3);
                        indices.Add(i0); indices.Add(i1); indices.Add(i2);
                        indices.Add(i1); indices.Add(i3); indices.Add(i2);
                    }
                }
            }

            // 1. Equatorial Primary Disk (XZ plane, 360 degrees)
            AddAnnulus((fr, fa) =>
            {
                float radius = Mathf.Lerp(inner, outer, fr);
                float angle = fa * Mathf.PI * 2f;
                return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }, 24, 180, 0f, 1f, 0f, 1f);

            // 2. Upper Lensed Halo Arch (Top half, seamlessly branching from flanks and curving behind the black hole)
            AddAnnulus((fr, fa) =>
            {
                float radius = Mathf.Lerp(inner, haloOuter, fr);
                float angle = fa * Mathf.PI; // 0 to PI
                float s = Mathf.Sin(angle);
                float x = -Mathf.Cos(angle) * radius;
                float y = s * radius;
                float z = s * (3.5f + 0.15f * radius * s);
                return new Vector3(x, y, z);
            }, 18, 120, 0f, 1f, 0f, 1f);

            // 3. Lower Lensed Halo Arch (Bottom half, seamlessly branching from flanks and curving behind the black hole)
            AddAnnulus((fr, fa) =>
            {
                float radius = Mathf.Lerp(inner, haloOuter, fr);
                float angle = Mathf.PI + fa * Mathf.PI; // PI to 2*PI
                float s = -Mathf.Sin(angle); // positive 0 to 1
                float x = -Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius * 0.65f;
                float z = s * (3.5f + 0.12f * radius * s);
                return new Vector3(x, y, z);
            }, 16, 120, 0f, 1f, 0f, 1f);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * outer * 4.0f);
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
