using UnityEngine;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

namespace EventHorizon.XR
{
    /// <summary>
    /// Ensures 6DOF / 3DOF head tracking on Meta Quest and PC VR headsets.
    /// Uses Input System TrackedPoseDriver with seamless XR subsystem fallback.
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
            {
                driver = gameObject.AddComponent<TrackedPoseDriver>();
                driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            }
#endif
        }

        void OnEnable()
        {
            UnityEngine.XR.InputDevices.deviceConnected += OnDeviceConnected;
            FindCenterEye();
        }

        void OnDisable()
        {
            UnityEngine.XR.InputDevices.deviceConnected -= OnDeviceConnected;
        }

        void OnDeviceConnected(UnityEngine.XR.InputDevice dev)
        {
            if ((dev.characteristics & UnityEngine.XR.InputDeviceCharacteristics.HeadMounted) != 0)
            {
                centerEyeDevice = dev;
            }
        }

        void FindCenterEye()
        {
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.HeadMounted | UnityEngine.XR.InputDeviceCharacteristics.EyeTracking, devices);
            if (devices.Count == 0)
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count > 0)
                centerEyeDevice = devices[0];
            else
                centerEyeDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye);
        }

        void Update()
        {
            if (!centerEyeDevice.isValid)
            {
                FindCenterEye();
            }

            if (centerEyeDevice.isValid)
            {
                if (centerEyeDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out Quaternion rot) ||
                    centerEyeDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out rot))
                {
                    if (rot != Quaternion.identity)
                        transform.localRotation = rot;
                }

                if (centerEyeDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out Vector3 pos) ||
                    centerEyeDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out pos))
                {
                    transform.localPosition = pos;
                }
            }
        }
    }
}
