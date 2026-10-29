using UnityEngine;

namespace EventHorizon.XR
{
    /// <summary>
    /// Comfort support. Never rotates the camera, never bobs the head: the
    /// tracked pose is sacred. Comfort mode eases a camera-attached vignette
    /// in during high-motion phases and asks the director (already applied)
    /// to damp streaks and motion cues.
    /// </summary>
    [AddComponentMenu("EventHorizon/XR/Comfort Controller")]
    public class EHComfortController : MonoBehaviour
    {
        [SerializeField] Material vignetteMaterial;
        [SerializeField, Range(0f, 1f)] float maxVignette = 0.45f;
        [SerializeField] float fadeSpeed = 1.2f;

        GameObject vignetteQuad;
        float target;
        float shown;

        void Awake()
        {
            var cam = Camera.main;
            if (cam == null || vignetteMaterial == null) return;
            vignetteQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vignetteQuad.name = "EH_ComfortVignette";
            Destroy(vignetteQuad.GetComponent<Collider>());
            vignetteQuad.GetComponent<MeshRenderer>().sharedMaterial = vignetteMaterial;
            vignetteQuad.transform.SetParent(cam.transform, false);
            vignetteQuad.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            vignetteQuad.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            vignetteQuad.layer = cam.gameObject.layer;
            SetAlpha(0f);
        }

        /// <summary>Called only by the director.</summary>
        public void Apply(bool comfortMode, float motion, bool snap)
        {
            target = comfortMode ? Mathf.Clamp01(motion) * maxVignette : 0f;
            if (snap)
            {
                shown = target;
                SetAlpha(shown);
            }
        }

        void Update()
        {
            shown = Mathf.MoveTowards(shown, target, fadeSpeed * Time.deltaTime * 0.3f);
            SetAlpha(shown);
        }

        void SetAlpha(float a)
        {
            if (vignetteMaterial != null)
                vignetteMaterial.SetFloat("_EH_VignetteAlpha", a);
            if (vignetteQuad != null)
                vignetteQuad.SetActive(a > 0.003f);
        }
    }
}
