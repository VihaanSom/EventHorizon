using UnityEngine;

namespace EventHorizon.Audio
{
    /// <summary>
    /// Fully procedural ambience: two deterministically synthesized loops
    /// (low drone + airy shimmer) crossfaded by timeline intensity, plus a
    /// restrained sub layer. No audio assets, no streaming, smooth fades only —
    /// no loud impacts. Restrained low end by design (modest sub gain).
    /// </summary>
    [AddComponentMenu("EventHorizon/Audio/Audio Controller")]
    public class EHAudioController : MonoBehaviour
    {
        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] float masterGain = 0.8f;
        [SerializeField, Range(0f, 0.5f)] float subGain = 0.22f;
        [SerializeField] float fadeSpeed = 1.5f;
        [SerializeField] int seed = 9001;

        AudioSource droneSource;
        AudioSource shimmerSource;
        AudioSource subSource;

        float targetIntensity;
        float shownIntensity;
        float finalFade = 1f;
        bool fadingOut;

        void Awake()
        {
            droneSource = MakeLoopSource("EH_Drone", MakeDroneClip(), 0.5f, 0f);
            shimmerSource = MakeLoopSource("EH_Shimmer", MakeShimmerClip(), 0f, 0.3f);
            subSource = MakeLoopSource("EH_Sub", MakeSubClip(), 0f, 0f);
        }

        AudioSource MakeLoopSource(string name, AudioClip clip, float volume, float stereoPan)
        {
            var src = new GameObject(name).AddComponent<AudioSource>();
            src.transform.SetParent(transform, false);
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = volume;
            src.panStereo = stereoPan;
            src.spatialBlend = 0f; // 2D bed; head-tracked cues stay subtle in VR
            src.Play();
            return src;
        }

        /// <summary>Called only by the director.</summary>
        public void ApplyIntensity(float intensity, bool snap)
        {
            targetIntensity = Mathf.Clamp01(intensity);
            if (snap)
            {
                shownIntensity = targetIntensity;
                ApplyMix();
            }
        }

        public void BeginFinalFade() => fadingOut = true;

        void Update()
        {
            shownIntensity = Mathf.MoveTowards(shownIntensity, targetIntensity, fadeSpeed * Time.deltaTime * 0.25f);
            if (fadingOut) finalFade = Mathf.MoveTowards(finalFade, 0f, Time.deltaTime / 6f);
            ApplyMix();
        }

        void ApplyMix()
        {
            float m = masterGain * finalFade;
            if (droneSource != null)
            {
                droneSource.volume = m * (0.25f + 0.45f * shownIntensity);
                droneSource.pitch = 1f - shownIntensity * 0.12f; // descent deepens the drone
            }
            if (shimmerSource != null)
                shimmerSource.volume = m * shownIntensity * 0.35f;
            if (subSource != null)
                subSource.volume = m * subGain * shownIntensity;
        }

        // ---- Deterministic synthesis (seeded, baked once at startup) ----

        AudioClip MakeDroneClip()
        {
            const int rate = 22050;
            const float seconds = 8f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(seed);
            double p1 = 0, p2 = 0, p3 = 0;
            double lp = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / rate;
                p1 += 2.0 * System.Math.PI * 55.0 / rate;
                p2 += 2.0 * System.Math.PI * 82.5 / rate;
                p3 += 2.0 * System.Math.PI * 110.0 / rate;
                double lfo = 0.75 + 0.25 * System.Math.Sin(2.0 * System.Math.PI * 0.125 * t + 1.0);
                double s = (System.Math.Sin(p1) * 0.5 + System.Math.Sin(p2) * 0.3 + System.Math.Sin(p3) * 0.2) * lfo;
                s += ((rng.NextDouble() * 2.0 - 1.0) * 0.02);
                lp += 0.08 * (s - lp); // gentle low-pass so it stays smooth
                // Crossfade loop ends to avoid clicks.
                double edge = System.Math.Min(1.0, System.Math.Min(i, n - i) / (rate * 0.5));
                data[i] = (float)(lp * 0.5 * edge);
            }
            var clip = AudioClip.Create("EH_Drone", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        AudioClip MakeShimmerClip()
        {
            const int rate = 22050;
            const float seconds = 8f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(seed + 7);
            double lp = 0;
            for (int i = 0; i < n; i++)
            {
                double noise = rng.NextDouble() * 2.0 - 1.0;
                lp += 0.02 * (noise - lp); // dark airy wash
                double t = (double)i / rate;
                double swell = 0.6 + 0.4 * System.Math.Sin(2.0 * System.Math.PI * 0.09 * t);
                double edge = System.Math.Min(1.0, System.Math.Min(i, n - i) / (rate * 0.5));
                data[i] = (float)(lp * 0.6 * swell * edge);
            }
            var clip = AudioClip.Create("EH_Shimmer", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        AudioClip MakeSubClip()
        {
            const int rate = 22050;
            const float seconds = 8f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            double p = 0;
            for (int i = 0; i < n; i++)
            {
                p += 2.0 * System.Math.PI * 36.75 / rate;
                double edge = System.Math.Min(1.0, System.Math.Min(i, n - i) / (rate * 0.5));
                data[i] = (float)(System.Math.Sin(p) * 0.4 * edge);
            }
            var clip = AudioClip.Create("EH_Sub", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
