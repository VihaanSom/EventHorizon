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
    /// controller per property set. Seamlessly moves the player rig along the
    /// 5-act gravitational plunge into the black hole singularity while preserving
    /// local headset tracking on the camera.
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

        [Header("Player Descent Rig")]
        [SerializeField] Transform playerRig;

        [Header("Presentation")]
        [SerializeField] EHExperienceUI ui;

        EHPhaseSample currentSample;
        bool comfortMode;

        public EHTimelineController Timeline => timeline;
        public bool ComfortMode => comfortMode;

        void Awake()
        {
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<EHXRHeadTracker>() == null)
            {
                cam.gameObject.AddComponent<EHXRHeadTracker>();
            }

            if (playerRig == null)
            {
                var rigObj = GameObject.Find("EH_PlayerRig");
                if (rigObj != null) playerRig = rigObj.transform;
                else if (cam != null && cam.transform.parent != null)
                    playerRig = cam.transform.parent;
            }

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
            StartCoroutine(AutoStartRoutine());
        }

        System.Collections.IEnumerator AutoStartRoutine()
        {
            yield return new WaitForSeconds(3.5f);
            if (timeline != null && timeline.State == EHTimelineState.Stopped)
            {
                StartExperience();
            }
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
            UpdatePlayerDescent(true);
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
            UpdatePlayerDescent(true);
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

            UpdatePlayerDescent(snap);
        }

        void UpdatePlayerDescent(bool snap)
        {
            if (playerRig == null || timeline == null) return;
            float total = Mathf.Max(1f, timeline.TotalDuration);
            float u = Mathf.Clamp01(timeline.TimeSeconds / total);

            Vector3 holePos = blackHole != null ? blackHole.transform.position : new Vector3(0f, 2f, 150f);

            // 5-Act smooth descent trajectory:
            // Act I (0.00 - 0.15): Approach from deep space (280m -> 130m)
            // Act II (0.15 - 0.40): Transition into accretion disk plane (130m -> 42m)
            // Act III (0.40 - 0.60): Gravitational plunge into ISCO (42m -> 12m)
            // Act IV (0.60 - 0.80): Event horizon crossing (12m -> 1.5m)
            // Act V (0.80 - 1.00): Singularity plunge (1.5m -> 0.05m)
            Vector3 targetPos;
            if (u < 0.15f)
            {
                float tPhase = u / 0.15f;
                Vector3 p0 = holePos + new Vector3(0f, 18f, -280f);
                Vector3 p1 = holePos + new Vector3(0f, 7f, -130f);
                targetPos = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0f, 1f, tPhase));
            }
            else if (u < 0.40f)
            {
                float tPhase = (u - 0.15f) / 0.25f;
                Vector3 p0 = holePos + new Vector3(0f, 7f, -130f);
                Vector3 p1 = holePos + new Vector3(0f, 2.2f, -42f);
                targetPos = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0f, 1f, tPhase));
            }
            else if (u < 0.60f)
            {
                float tPhase = (u - 0.40f) / 0.20f;
                Vector3 p0 = holePos + new Vector3(0f, 2.2f, -42f);
                Vector3 p1 = holePos + new Vector3(0f, 0.8f, -12f);
                targetPos = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0f, 1f, tPhase));
            }
            else if (u < 0.80f)
            {
                float tPhase = (u - 0.60f) / 0.20f;
                Vector3 p0 = holePos + new Vector3(0f, 0.8f, -12f);
                Vector3 p1 = holePos + new Vector3(0f, 0.2f, -1.5f);
                targetPos = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0f, 1f, tPhase));
            }
            else
            {
                float tPhase = (u - 0.80f) / 0.20f;
                Vector3 p0 = holePos + new Vector3(0f, 0.2f, -1.5f);
                Vector3 p1 = holePos + new Vector3(0f, 0.0f, -0.05f);
                targetPos = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0f, 1f, tPhase));
            }

            if (snap)
            {
                playerRig.position = targetPos;
            }
            else
            {
                playerRig.position = Vector3.Lerp(playerRig.position, targetPos, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
            }
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
