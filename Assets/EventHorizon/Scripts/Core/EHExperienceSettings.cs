using UnityEngine;

namespace EventHorizon
{
    /// <summary>
    /// One choreographed segment of the five-minute descent. All values are
    /// normalized intensities (0..1 unless noted) except where stated.
    /// </summary>
    [System.Serializable]
    public struct EHPhaseParameters
    {
        [Tooltip("Phase length in seconds. Acts I-V default to 45/75/60/60/60.")]
        public float durationSeconds;

        [Tooltip("Black-hole apparent scale multiplier (1 = reference size).")]
        public float blackHoleScale;

        [Tooltip("Analytic lensing strength multiplier.")]
        public float lensingStrength;

        [Tooltip("Accretion-disk emissive intensity.")]
        public float diskEmission;

        [Tooltip("Starfield brightness multiplier.")]
        public float starBrightness;

        [Tooltip("Ambient dust density multiplier.")]
        public float dustDensity;

        [Tooltip("High-speed streak intensity (0 in calm phases).")]
        public float streakAmount;

        [Tooltip("Overall environmental motion / descent speed cue.")]
        public float environmentMotion;

        [Tooltip("Audio intensity (ambience -> full drone).")]
        public float audioIntensity;

        [Tooltip("Blend curve from the previous phase into this one (local 0..1).")]
        public AnimationCurve transitionCurve;
    }

    /// <summary>
    /// Configurable timeline data for the whole experience. Deterministic:
    /// identical time + settings always yield an identical sample.
    /// </summary>
    [CreateAssetMenu(fileName = "EHExperienceSettings", menuName = "EventHorizon/Experience Settings")]
    public class EHExperienceSettings : ScriptableObject
    {
        [Tooltip("Exactly five phases: Approach, Accretion Disk, Plunge, Event Horizon, Final Descent.")]
        public EHPhaseParameters[] phases = new EHPhaseParameters[5];

        [Tooltip("Developer time multiplier (1 = real time).")]
        public float developerTimeMultiplier = 1f;

        [Tooltip("Angular radius of the lensing field in degrees.")]
        public float lensingRadiusDegrees = 28f;

        [Tooltip("Reference black-hole angular radius in degrees.")]
        public float blackHoleRadiusDegrees = 9f;

        [Tooltip("Comfort mode reduces streaks and motion cues.")]
        public bool comfortModeDefault;

        public static readonly string[] PhaseNames =
        {
            "ACT I — APPROACH",
            "ACT II — ACCRETION DISK",
            "ACT III — THE PLUNGE",
            "ACT IV — EVENT HORIZON",
            "ACT V — FINAL DESCENT"
        };

        public float TotalDuration
        {
            get
            {
                float total = 0f;
                if (phases == null) return 300f;
                for (int i = 0; i < phases.Length; i++) total += Mathf.Max(0f, phases[i].durationSeconds);
                return total;
            }
        }

        void OnValidate()
        {
            if (phases == null || phases.Length != 5)
                System.Array.Resize(ref phases, 5);

            float[] defaults = { 45f, 75f, 60f, 60f, 60f };
            for (int i = 0; i < 5; i++)
            {
                if (phases[i].durationSeconds <= 0f)
                    phases[i].durationSeconds = defaults[i];
                if (phases[i].transitionCurve == null || phases[i].transitionCurve.length == 0)
                    phases[i].transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            developerTimeMultiplier = Mathf.Clamp(developerTimeMultiplier, 0.1f, 32f);
        }

        void Reset()
        {
            phases = new EHPhaseParameters[5];
            float[] durations = { 45f, 75f, 60f, 60f, 60f };
            float[] hole = { 1.0f, 1.6f, 2.6f, 4.2f, 6.5f };
            float[] lens = { 0.35f, 0.6f, 1.0f, 1.35f, 1.7f };
            float[] disk = { 0.55f, 1.0f, 1.25f, 0.9f, 0.45f };
            float[] star = { 1.0f, 1.0f, 0.9f, 0.7f, 0.4f };
            float[] dust = { 0.25f, 0.45f, 1.0f, 0.8f, 0.5f };
            float[] streak = { 0f, 0.1f, 0.7f, 1.0f, 0.55f };
            float[] motion = { 0.15f, 0.35f, 1.0f, 0.8f, 0.6f };
            float[] audio = { 0.2f, 0.45f, 0.8f, 1.0f, 0.6f };
            for (int i = 0; i < 5; i++)
            {
                phases[i] = new EHPhaseParameters
                {
                    durationSeconds = durations[i],
                    blackHoleScale = hole[i],
                    lensingStrength = lens[i],
                    diskEmission = disk[i],
                    starBrightness = star[i],
                    dustDensity = dust[i],
                    streakAmount = streak[i],
                    environmentMotion = motion[i],
                    audioIntensity = audio[i],
                    transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
                };
            }
        }
    }
}
