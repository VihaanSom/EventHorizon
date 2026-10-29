using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EventHorizon
{
    /// <summary>
    /// Static project/scene sanity checks usable from the boot scene, the
    /// editor menu, and future automated tests. Pure queries: no side effects.
    /// </summary>
    [AddComponentMenu("EventHorizon/Core/Scene Validator")]
    public class EHSceneValidator : MonoBehaviour
    {
        [SerializeField] bool validateOnAwake = true;

        void Awake()
        {
            if (!validateOnAwake) return;
            var issues = ValidateProject();
            issues.AddRange(ValidateScene(gameObject.scene));
            foreach (string issue in issues)
                Debug.LogWarning("[EH] " + issue, this);
        }

        public static List<string> ValidateProject()
        {
            var issues = new List<string>();

            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null)
                issues.Add("No SRP asset assigned (expected a URP asset).");
            else if (!(rp is UniversalRenderPipelineAsset))
                issues.Add($"Render pipeline is {rp.GetType().Name}, expected URP.");

#if UNITY_ANDROID && !UNITY_EDITOR
            // Device-side checks only make sense in a device build.
#endif
            if (QualitySettings.antiAliasing < 2)
                issues.Add("MSAA is below 2x; Quest stereo edges will shimmer.");
            if (Camera.main == null)
                issues.Add("No camera tagged MainCamera in the active scene.");

            return issues;
        }

        public static List<string> ValidateScene(UnityEngine.SceneManagement.Scene scene)
        {
            var issues = new List<string>();
            if (!scene.IsValid()) return issues;
            foreach (var root in scene.GetRootGameObjects())
            {
                var director = root.GetComponentInChildren<EHExperienceDirector>(true);
                if (director != null)
                    issues.AddRange(director.ValidateWiring());
            }
            return issues;
        }
    }
}
