using UnityEngine;

namespace EventHorizon.BlackHole
{
    /// <summary>
    /// Forwards lensing strength to the black-hole controller (the single
    /// writer of shared globals) and owns only its debug toggle. The MVP
    /// effect is an analytic ray-direction bend, not GR ray tracing — see the
    /// shader header and README for the exact approximation.
    /// </summary>
    [AddComponentMenu("EventHorizon/BlackHole/Lensing Controller")]
    public class EHLensingController : MonoBehaviour
    {
        [SerializeField] EHBlackHoleController blackHole;
        [SerializeField, Tooltip("Debug: bypass the bend entirely.")]
        bool lensingEnabled = true;
        [SerializeField, Tooltip("Angular radius of the lensing field, degrees.")]
        float lensingRadiusDegrees = 28f;

        static readonly int StrengthId = Shader.PropertyToID("_EH_LensingStrength");
        static readonly int RadiusId = Shader.PropertyToID("_EH_LensingRadius");

        float currentStrength;

        public bool LensingEnabled
        {
            get => lensingEnabled;
            set { lensingEnabled = value; Push(); }
        }

        void Awake()
        {
            if (blackHole == null) blackHole = GetComponent<EHBlackHoleController>();
            Push();
        }

        void OnValidate() => Push();

        /// <summary>Called only by the director.</summary>
        public void ApplyLensing(float strength, bool snap)
        {
            currentStrength = Mathf.Max(0f, strength);
            if (snap || !Application.isPlaying) Push();
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            Push();
        }

        void Push()
        {
            float s = lensingEnabled ? currentStrength : 0f;
            Shader.SetGlobalFloat(StrengthId, s);
            Shader.SetGlobalFloat(RadiusId, lensingRadiusDegrees);
        }
    }
}
