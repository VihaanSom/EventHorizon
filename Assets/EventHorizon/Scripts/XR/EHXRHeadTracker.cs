using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
#endif

namespace EventHorizon.XR
{
    /// <summary>
    /// Ensures smooth, rock-solid 6DOF head tracking on Meta Quest and PC VR.
    /// Uses OpenXR native late-latching and TrackedPoseDriver. Strictly avoids
    /// manual polling overrides in Update() which cause dual-writer camera shake.
    /// </summary>
    [AddComponentMenu("EventHorizon/XR/XR Head Tracker")]
    [RequireComponent(typeof(Camera))]
    public class EHXRHeadTracker : MonoBehaviour
    {
        void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            var driver = GetComponent<TrackedPoseDriver>();
            if (driver == null)
            {
                driver = gameObject.AddComponent<TrackedPoseDriver>();
                driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            }
#endif
        }

#if UNITY_EDITOR
        // Desktop mouse look fallback for rapid PC testing when no VR headset is worn
        void Update()
        {
            if (!UnityEngine.XR.XRSettings.enabled && Input.GetMouseButton(1))
            {
                float mx = Input.GetAxis("Mouse X") * 2.5f;
                float my = -Input.GetAxis("Mouse Y") * 2.5f;
                transform.localEulerAngles += new Vector3(my, mx, 0f);
            }
        }
#endif
    }
}
