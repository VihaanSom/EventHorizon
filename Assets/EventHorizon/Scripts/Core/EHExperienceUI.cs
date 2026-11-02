using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EventHorizon
{
    /// <summary>
    /// Builds its own VR-compatible WorldSpace Canvas at runtime (start prompt, pause note,
    /// comfort flash, farewell fade, end card). Keeps the hand-authored scenes
    /// free of fragile UI prefab wiring.
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
            
            var cam = Camera.main;
            if (cam != null)
            {
                root.transform.SetParent(cam.transform, false);
                root.transform.localPosition = new Vector3(0f, 0f, 2.2f);
                root.transform.localRotation = Quaternion.identity;
            }
            else
            {
                root.transform.SetParent(transform, false);
                root.transform.position = new Vector3(0f, 0f, 2.2f);
            }
            root.transform.localScale = Vector3.one * 0.002f;

            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            
            var rt = root.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(1000f, 600f);

            root.AddComponent<GraphicRaycaster>();
            canvasGroup = root.AddComponent<CanvasGroup>();

            // Fade image: starts with alpha 0 so it never obscures the view if unmanaged
            var fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(root.transform, false);
            fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.rectTransform.anchorMin = Vector2.zero;
            fadeImage.rectTransform.anchorMax = Vector2.one;
            fadeImage.rectTransform.offsetMin = new Vector2(-500f, -500f);
            fadeImage.rectTransform.offsetMax = new Vector2(500f, 500f);
            fadeImage.color = new Color(0f, 0f, 0f, 0f);

            centerText = MakeText(root.transform, "Center", 28);
            endText = MakeText(root.transform, "EndCard", 20);
            endText.gameObject.SetActive(false);
        }

        Text MakeText(Transform parent, string name, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                try { font = Font.CreateDynamicFontFromOSFont("Arial", size); }
                catch (System.Exception) { }
            }
            if (font != null) t.font = font;

            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.9f, 0.94f, 1f, 1f);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.05f, 0.15f);
            rt.anchorMax = new Vector2(0.95f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return t;
        }

        public void ShowStartPrompt()
        {
            if (centerText != null)
            {
                centerText.gameObject.SetActive(true);
                centerText.text = "EVENT HORIZON\n\nLook around freely.\n\nPress any Trigger / Button to begin the descent";
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
                centerText.text = "PAUSED\n\nPress again to resume";
            }
        }

        public void HidePaused() => HideStartPrompt();

        public void FlashComfortNote(bool on)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash(on ? "Comfort mode: ON" : "Comfort mode: OFF"));
        }

        IEnumerator Flash(string msg)
        {
            if (centerText == null) yield break;
            centerText.gameObject.SetActive(true);
            string prev = centerText.text;
            centerText.text = msg;
            yield return new WaitForSecondsRealtime(2f);
            centerText.text = prev;
            if (prev == string.Empty || prev.StartsWith("EVENT") || prev.StartsWith("PAUSED"))
                centerText.gameObject.SetActive(prev.StartsWith("PAUSED"));
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
            // Final descent closes toward near-black
            if (fadeImage != null && s.phaseIndex == 4)
            {
                var c = fadeImage.color;
                c.a = Mathf.Clamp01(s.phaseLocalT * 0.75f);
                fadeImage.color = c;
            }
        }

        public void BeginFarewellFade(System.Action onDone)
        {
            StartCoroutine(Farewell(onDone));
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
