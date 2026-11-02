using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

namespace EventHorizon.XR
{
    /// <summary>
    /// Ensures rock-solid 6DOF head tracking on Meta Quest and PC VR headsets.
    /// Combines InputSystem TrackedPoseDriver with OpenXR late-latching onBeforeRender.
    /// Guarantees the camera always tracks head movement so celestial objects stay fixed in world space.
    /// </summary>
    [AddComponentMenu("EventHorizon/XR/XR Head Tracker")]
    [RequireComponent(typeof(Camera))]
    public class EHXRHeadTracker : MonoBehaviour
    {
        UnityEngine.XR.InputDevice centerEyeDevice;

        void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            var driver = GetComponent<TrackedPoseDriver>();
            if (driver == null)
                driver = gameObject.AddComponent<TrackedPoseDriver>();

            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            // Explicitly bind to standard XR HMD center eye pose
            var posAction = new InputAction("XRHMD_Position", binding: "<XRHMD>/centerEyePosition");
            var rotAction = new InputAction("XRHMD_Rotation", binding: "<XRHMD>/centerEyeRotation");
            posAction.Enable();
            rotAction.Enable();
            driver.positionInput = new InputActionProperty(posAction);
            driver.rotationInput = new InputActionProperty(rotAction);
#endif
        }

        void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRenderCallback;
            InputDevices.deviceConnected += OnDeviceConnected;
            FindCenterEye();
        }

        void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRenderCallback;
            InputDevices.deviceConnected -= OnDeviceConnected;
        }

        void OnDeviceConnected(UnityEngine.XR.InputDevice dev)
        {
            if ((dev.characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
            {
                centerEyeDevice = dev;
            }
        }

        void FindCenterEye()
        {
            centerEyeDevice = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        }

        void OnBeforeRenderCallback()
        {
            if (!centerEyeDevice.isValid)
            {
                FindCenterEye();
            }

            if (centerEyeDevice.isValid)
            {
                if (centerEyeDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out Quaternion rot) &&
                    rot != Quaternion.identity)
                {
                    transform.localRotation = rot;
                }

                if (centerEyeDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out Vector3 pos) &&
                    pos != Vector3.zero)
                {
                    transform.localPosition = pos;
                }
            }
        }

#if UNITY_EDITOR
        // Desktop mouse look fallback for rapid PC testing when no VR headset is worn
        void Update()
        {
            if (!XRSettings.enabled && Input.GetMouseButton(1))
            {
                float mx = Input.GetAxis("Mouse X") * 2.5f;
                float my = -Input.GetAxis("Mouse Y") * 2.5f;
                transform.localEulerAngles += new Vector3(my, mx, 0f);
            }
        }
#endif
    }
}
