using System.Collections.Generic;
using UnityEngine;
using EventHorizon.BlackHole;
using EventHorizon.Environment;
using EventHorizon.Audio;
using EventHorizon.XR;

namespace EventHorizon
{
    /// <summary>
    /// Owns the experience graph and pushes each timeline sample to exactly one
    /// controller per property set. No other component may drive these visuals,
    /// so competing writes cannot occur. The XR camera pose itself is never
    /// touched: head tracking always belongs to the headset.
    /// </summary>
    [AddComponentMenu("EventHorizon/Core/Experience Director")]
    public class EHExperienceDirector : MonoBehaviour
    {
        [Header("Timeline")]
        [SerializeField] EHTimelineController timeline;
        [SerializeField] EHExperienceSettings settings;

        [Header("Controllers (one owner per property set)")]
        [SerializeField] EHBlackHoleController blackHole;
        [SerializeField] EHAccretionDiskController accretionDisk;
        [SerializeField] EHLensingController lensing;
        [SerializeField] EHStarfieldController starfield;
        [SerializeField] EHDustController dust;
        [SerializeField] EHAudioController audioController;
        [SerializeField] EHComfortController comfort;
        [SerializeField] EHInputController input;

        [Header("Presentation")]
        [SerializeField] EHExperienceUI ui;

        EHPhaseSample currentSample;
        bool comfortMode;

        public EHTimelineController Timeline => timeline;
        public bool ComfortMode => comfortMode;

        void Awake()
        {
            if (timeline == null) timeline = GetComponent<EHTimelineController>();
            if (timeline != null && settings != null) timeline.Configure(settings);
            comfortMode = settings != null && settings.comfortModeDefault;

            if (ui == null) ui = GetComponent<EHExperienceUI>();
            if (ui != null) ui.BuildRuntimeUI();

            if (timeline != null)
            {
                timeline.onPhaseChanged.AddListener(OnPhaseChanged);
                timeline.onFinished.AddListener(OnFinished);
            }
            if (input != null)
            {
                input.onStart.AddListener(StartExperience);
                input.onPauseToggle.AddListener(TogglePause);
                input.onRestart.AddListener(RestartExperience);
                input.onExit.AddListener(ExitExperience);
                input.onComfortToggle.AddListener(ToggleComfort);
                input.onSkipToPhase.AddListener(SkipToPhase);
            }
        }

        void Start()
        {
            ApplySample(timeline != null ? timeline.Sample(0f) : default, true);
            ui?.ShowStartPrompt();
        }

        void Update()
        {
            if (timeline == null) return;
            if (timeline.State == EHTimelineState.Playing ||
                timeline.State == EHTimelineState.Finished)
            {
                ApplySample(timeline.SampleCurrent(), false);
            }
        }

        public void StartExperience()
        {
            if (timeline == null) return;
            if (timeline.State == EHTimelineState.Playing) return;
            if (timeline.State == EHTimelineState.Finished) timeline.Restart();
            else if (timeline.State == EHTimelineState.Paused) timeline.Resume();
            else timeline.Play();
            ui?.HideStartPrompt();
        }

        public void TogglePause()
        {
            if (timeline == null) return;
            if (timeline.State == EHTimelineState.Playing) { timeline.Pause(); ui?.ShowPaused(); }
            else if (timeline.State == EHTimelineState.Paused) { timeline.Resume(); ui?.HidePaused(); }
        }

        public void RestartExperience()
        {
            if (timeline == null) return;
            timeline.Restart();
            ui?.HideStartPrompt();
            ui?.HideEndCard();
        }

        public void ExitExperience()
        {
            ui?.BeginFarewellFade(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        public void ToggleComfort()
        {
            comfortMode = !comfortMode;
            ApplySample(currentSample, true);
            ui?.FlashComfortNote(comfortMode);
        }

        public void SkipToPhase(int index)
        {
            timeline?.SkipToPhase(index);
        }

        void OnPhaseChanged(int index)
        {
            if (settings != null && index >= 0 && index < EHExperienceSettings.PhaseNames.Length)
                Debug.Log($"[EH] {EHExperienceSettings.PhaseNames[index]}");
        }

        void OnFinished()
        {
            ui?.ShowEndCard();
            audioController?.BeginFinalFade();
        }

        void ApplySample(in EHPhaseSample s, bool snap)
        {
            currentSample = s;
            float streak = comfortMode ? s.streakAmount * 0.25f : s.streakAmount;
            float motion = comfortMode ? s.environmentMotion * 0.6f : s.environmentMotion;

            blackHole?.ApplyScale(s.blackHoleScale, snap);
            lensing?.ApplyLensing(s.lensingStrength, snap);
            accretionDisk?.ApplyEmission(s.diskEmission, snap);
            starfield?.ApplyBrightness(s.starBrightness, snap);
            dust?.ApplyDensity(s.dustDensity, streak, motion, snap);
            audioController?.ApplyIntensity(s.audioIntensity, snap);
            comfort?.Apply(comfortMode, motion, snap);
            ui?.ApplyFade(s);
        }

        /// <summary>Editor + boot-time wiring check. Returns human-readable issues.</summary>
        public List<string> ValidateWiring()
        {
            var issues = new List<string>();
            if (timeline == null) issues.Add("Director: timeline missing.");
            if (settings == null) issues.Add("Director: settings missing.");
            if (blackHole == null) issues.Add("Director: black-hole controller missing.");
            if (accretionDisk == null) issues.Add("Director: accretion-disk controller missing.");
            if (lensing == null) issues.Add("Director: lensing controller missing.");
            if (starfield == null) issues.Add("Director: starfield controller missing.");
            if (dust == null) issues.Add("Director: dust controller missing.");
            if (audioController == null) issues.Add("Director: audio controller missing.");
            if (comfort == null) issues.Add("Director: comfort controller missing.");
            if (input == null) issues.Add("Director: input controller missing.");
            if (Camera.main == null) issues.Add("Scene: no MainCamera found.");
            return issues;
        }
    }
}
