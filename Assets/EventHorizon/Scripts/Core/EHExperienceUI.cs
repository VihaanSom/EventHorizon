using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EventHorizon
{
    /// <summary>
    /// Builds its own minimal Canvas at runtime (start prompt, pause note,
    /// comfort flash, farewell fade, end card). Keeps the hand-authored scenes
    /// free of fragile UI prefab wiring. Final card distinguishes established
    /// physics from approximation and artistic interpretation.
    /// </summary>
    [AddComponentMenu("EventHorizon/Core/Experience UI")]
    public class EHExperienceUI : MonoBehaviour
    {
        Canvas canvas;
        Text centerText;
        Text endText;
        Image fadeImage;
        CanvasGroup canvasGroup;
        Coroutine flashRoutine;

        const string EndCardText =
            "EVENT HORIZON — what you witnessed\n\n" +
            "Established physics: a non-rotating (Schwarzschild) black hole would " +
            "show a dark shadow ringed by a photon ring, with a hot accretion disk " +
            "doppler-brightened on one side.\n\n" +
            "Approximation: the gravitational lensing here is an analytic bend of " +
            "starlight, not a solution of Einstein's field equations.\n\n" +
            "Artistic interpretation: nothing is known — or can be shown — of an " +
            "interior landscape. The final descent is imagination, not data.";

        public void BuildRuntimeUI()
        {
            if (canvas != null) return;
            var root = new GameObject("EH_UI");
            root.transform.SetParent(transform, false);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();
            canvasGroup = root.AddComponent<CanvasGroup>();

            fadeImage = new GameObject("Fade").AddComponent<Image>();
            fadeImage.transform.SetParent(root.transform, false);
            fadeImage.rectTransform.anchorMin = Vector2.zero;
            fadeImage.rectTransform.anchorMax = Vector2.one;
            fadeImage.rectTransform.offsetMin = Vector2.zero;
            fadeImage.rectTransform.offsetMax = Vector2.zero;
            fadeImage.color = new Color(0f, 0f, 0f, 1f);

            centerText = MakeText(root.transform, "Center", 26);
            endText = MakeText(root.transform, "EndCard", 20);
            endText.gameObject.SetActive(false);

            StartCoroutine(FadeFromBlack(2f));
        }

        Text MakeText(Transform parent, string name, int size)
        {
            var t = new GameObject(name).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.85f, 0.9f, 1f, 1f);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.2f);
            rt.anchorMax = new Vector2(0.9f, 0.8f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return t;
        }

        public void ShowStartPrompt()
        {
            if (centerText != null)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = "EVENT HORIZON\n\nLook around freely. You will not be steered.\n\nPress Start / Space / Tap to begin the descent";
            }
        }

        public void HideStartPrompt()
        {
            if (centerText != null) centerText.gameObject.SetActive(false);
        }

        public void ShowPaused()
        {
            if (centerText != null)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = "Paused — press again to resume";
            }
        }

        public void HidePaused() => HideStartPrompt();

        public void FlashComfortNote(bool on)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash(on ? "Comfort mode: ON (streaks reduced)" : "Comfort mode: OFF"));
        }

        IEnumerator Flash(string msg)
        {
            if (centerText == null) yield break;
            centerText.gameObject.SetActive(true);
            string prev = centerText.text;
            centerText.text = msg;
            yield return new WaitForSecondsRealtime(2f);
            centerText.text = prev;
            if (prev == string.Empty || prev.StartsWith("EVENT") || prev.StartsWith("Paused"))
                centerText.gameObject.SetActive(prev.StartsWith("Paused"));
        }

        public void ShowEndCard()
        {
            if (endText != null)
            {
                endText.gameObject.SetActive(true);
                endText.text = EndCardText;
            }
        }

        public void HideEndCard()
        {
            if (endText != null) endText.gameObject.SetActive(false);
        }

        public void ApplyFade(in EHPhaseSample s)
        {
            // Final descent closes toward near-black; the fade quad eases it.
            if (fadeImage != null && s.phaseIndex == 4)
            {
                var c = fadeImage.color;
                c.a = Mathf.Clamp01(s.phaseLocalT * 0.55f);
                fadeImage.color = c;
            }
        }

        public void BeginFarewellFade(System.Action onDone)
        {
            StartCoroutine(Farewell(onDone));
        }

        IEnumerator FadeFromBlack(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                if (fadeImage != null)
                {
                    var c = fadeImage.color;
                    c.a = 1f - Mathf.Clamp01(t / seconds);
                    fadeImage.color = c;
                }
                yield return null;
            }
        }

        IEnumerator Farewell(System.Action onDone)
        {
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.unscaledDeltaTime;
                if (fadeImage != null)
                {
                    var c = fadeImage.color;
                    c.a = Mathf.Clamp01(t / 1.5f);
                    fadeImage.color = c;
                }
                yield return null;
            }
            onDone?.Invoke();
        }
    }
}
