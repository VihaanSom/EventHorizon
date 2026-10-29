using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace EventHorizon.XR
{
    /// <summary>
    /// Start / pause / restart / exit / comfort inputs. Works with XR
    /// controller buttons when an XR rig is present and falls back to
    /// keyboard + tap so the timeline is testable on desktop. Emits events;
    /// the director subscribes. No camera control lives here.
    /// </summary>
    [AddComponentMenu("EventHorizon/XR/Input Controller")]
    public class EHInputController : MonoBehaviour
    {
        [Header("XR button paths (optional; ignored when absent)")]
        [SerializeField] string startButtonPath = "<XRController>{RightHand}/triggerPressed";
        [SerializeField] string pauseButtonPath = "<XRController>{RightHand}/menuPressed";

        public UnityEvent onStart;
        public UnityEvent onPauseToggle;
        public UnityEvent onRestart;
        public UnityEvent onExit;
        public UnityEvent onComfortToggle;
        public UnityEvent<int> onSkipToPhase;

        InputAction startAction;
        InputAction pauseAction;
        InputAction comfortAction;
        InputAction restartAction;

        void OnEnable()
        {
            startAction = MakeButton("EH_Start", "<Keyboard>/space", "<Gamepad>/start");
            pauseAction = MakeButton("EH_Pause", "<Keyboard>/p", "<Gamepad>/start");
            comfortAction = MakeButton("EH_Comfort", "<Keyboard>/c", "<Gamepad>/leftStickPress");
            restartAction = MakeButton("EH_Restart", "<Keyboard>/r", "<Gamepad>/select");

            startAction.performed += _ => onStart?.Invoke();
            pauseAction.performed += _ => onPauseToggle?.Invoke();
            comfortAction.performed += _ => onComfortToggle?.Invoke();
            restartAction.performed += _ => onRestart?.Invoke();
        }

        InputAction MakeButton(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string b in bindings)
            {
                try { action.AddBinding(b); }
                catch (System.Exception) { /* binding path unavailable on this device */ }
            }
            // Optional XR bindings: silently skipped when no XR layout exists.
            try
            {
                if (name == "EH_Start") action.AddBinding(startButtonPath);
                if (name == "EH_Pause") action.AddBinding(pauseButtonPath);
            }
            catch (System.Exception) { }
            action.Enable();
            return action;
        }

        void Update()
        {
            // Touchscreen tap = start/advance (mobile fallback).
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                onStart?.Invoke();
            // Escape / Back = exit on device.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                onExit?.Invoke();
            // Developer skip-to-phase: keys 1..5 jump to Acts I..V.
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) onSkipToPhase?.Invoke(0);
                if (Keyboard.current.digit2Key.wasPressedThisFrame) onSkipToPhase?.Invoke(1);
                if (Keyboard.current.digit3Key.wasPressedThisFrame) onSkipToPhase?.Invoke(2);
                if (Keyboard.current.digit4Key.wasPressedThisFrame) onSkipToPhase?.Invoke(3);
                if (Keyboard.current.digit5Key.wasPressedThisFrame) onSkipToPhase?.Invoke(4);
            }
        }

        void OnDisable()
        {
            startAction?.Disable();
            pauseAction?.Disable();
            comfortAction?.Disable();
            restartAction?.Disable();
        }
    }
}
