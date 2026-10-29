using UnityEngine;
using UnityEngine.Events;

namespace EventHorizon
{
    /// <summary>
    /// Central deterministic timeline for the ~300 s descent. The single authority
    /// that advances experience time; the director pushes each sample to the
    /// visual/audio controllers. Never touches transforms or materials directly.
    /// </summary>
    [AddComponentMenu("EventHorizon/Core/Timeline Controller")]
    public class EHTimelineController : MonoBehaviour
    {
        [SerializeField] EHExperienceSettings settings;
        [SerializeField, Tooltip("Runtime time multiplier; multiplies the settings value.")]
        float timeMultiplier = 1f;

        public EHTimelineState State { get; private set; } = EHTimelineState.Stopped;
        public float TimeSeconds { get; private set; }
        public int PhaseIndex { get; private set; }

        public EHExperienceSettings Settings => settings;
        public float TotalDuration => settings != null ? settings.TotalDuration : 300f;

        public UnityEvent<int> onPhaseChanged;
        public UnityEvent onFinished;

        float pausedTimeScale = 1f;

        public void Configure(EHExperienceSettings s)
        {
            settings = s;
            if (settings != null) timeMultiplier = settings.developerTimeMultiplier;
        }

        public void Play()
        {
            if (settings == null)
            {
                Debug.LogError("[EH] Timeline has no settings assigned.", this);
                return;
            }
            TimeSeconds = 0f;
            PhaseIndex = 0;
            State = EHTimelineState.Playing;
            onPhaseChanged?.Invoke(0);
        }

        public void Pause()
        {
            if (State != EHTimelineState.Playing) return;
            pausedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            State = EHTimelineState.Paused;
        }

        public void Resume()
        {
            if (State != EHTimelineState.Paused) return;
            Time.timeScale = pausedTimeScale;
            State = EHTimelineState.Playing;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            Play();
        }

        public void SkipToPhase(int index)
        {
            if (settings == null || settings.phases == null) return;
            index = Mathf.Clamp(index, 0, settings.phases.Length - 1);
            float t = 0f;
            for (int i = 0; i < index; i++) t += Mathf.Max(0f, settings.phases[i].durationSeconds);
            TimeSeconds = t;
            PhaseIndex = index;
            if (State == EHTimelineState.Stopped || State == EHTimelineState.Finished)
                State = EHTimelineState.Playing;
            onPhaseChanged?.Invoke(index);
        }

        public void SetTimeMultiplier(float m)
        {
            timeMultiplier = Mathf.Clamp(m, 0.1f, 32f);
        }

        void Update()
        {
            if (State != EHTimelineState.Playing) return;
            float dt = Time.unscaledDeltaTime * timeMultiplier;
            TimeSeconds += dt;

            int idx = ResolvePhase(TimeSeconds);
            if (idx != PhaseIndex)
            {
                PhaseIndex = idx;
                onPhaseChanged?.Invoke(idx);
            }

            if (TimeSeconds >= TotalDuration)
            {
                TimeSeconds = TotalDuration;
                State = EHTimelineState.Finished;
                onFinished?.Invoke();
            }
        }

        int ResolvePhase(float t)
        {
            if (settings == null || settings.phases == null || settings.phases.Length == 0) return 0;
            float acc = 0f;
            for (int i = 0; i < settings.phases.Length; i++)
            {
                acc += Mathf.Max(0f, settings.phases[i].durationSeconds);
                if (t < acc) return i;
            }
            return settings.phases.Length - 1;
        }

        /// <summary>Pure deterministic sample for any timeline time.</summary>
        public EHPhaseSample Sample(float t)
        {
            var fallback = new EHPhaseSample { phaseIndex = 0 };
            if (settings == null || settings.phases == null || settings.phases.Length == 0)
                return fallback;

            t = Mathf.Clamp(t, 0f, TotalDuration);
            float acc = 0f;
            for (int i = 0; i < settings.phases.Length; i++)
            {
                float dur = Mathf.Max(0.001f, settings.phases[i].durationSeconds);
                if (t <= acc + dur || i == settings.phases.Length - 1)
                {
                    float local = Mathf.Clamp01((t - acc) / dur);
                    EHPhaseParameters cur = settings.phases[i];
                    EHPhaseParameters prev = i > 0 ? settings.phases[i - 1] : cur;
                    float curveT = cur.transitionCurve != null
                        ? Mathf.Clamp01(cur.transitionCurve.Evaluate(local))
                        : local;
                    var from = ToSample(prev, i, 1f);
                    var to = ToSample(cur, i, local);
                    // Follow the phase curve, then cross-blend from the previous
                    // phase across the first 15% so boundaries are seamless.
                    var target = EHPhaseSample.Lerp(from, to, curveT);
                    float cross = Mathf.SmoothStep(0f, 0.15f, local);
                    var s = EHPhaseSample.Lerp(from, target, cross);
                    s.phaseIndex = i;
                    s.phaseLocalT = local;
                    s.totalT = TotalDuration > 0f ? t / TotalDuration : 0f;
                    return s;
                }
                acc += dur;
            }
            return fallback;
        }

        static EHPhaseSample ToSample(in EHPhaseParameters p, int index, float local)
        {
            return new EHPhaseSample
            {
                phaseIndex = index,
                phaseLocalT = local,
                blackHoleScale = p.blackHoleScale,
                lensingStrength = p.lensingStrength,
                diskEmission = p.diskEmission,
                starBrightness = p.starBrightness,
                dustDensity = p.dustDensity,
                streakAmount = p.streakAmount,
                environmentMotion = p.environmentMotion,
                audioIntensity = p.audioIntensity
            };
        }

        public EHPhaseSample SampleCurrent() => Sample(TimeSeconds);
    }
}
