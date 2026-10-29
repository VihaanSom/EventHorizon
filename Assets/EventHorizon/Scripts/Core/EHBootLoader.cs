using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EventHorizon
{
    /// <summary>
    /// Boot scene behaviour: validates the install, then loads the experience
    /// scene asynchronously. Never blocks on a failed load; reports instead.
    /// </summary>
    [AddComponentMenu("EventHorizon/Core/Boot Loader")]
    public class EHBootLoader : MonoBehaviour
    {
        [SerializeField] string experienceSceneName = "EH_Experience";
        [SerializeField] float minSplashSeconds = 1.5f;

        string status = "Initializing…";
        bool failed;

        IEnumerator Start()
        {
            var issues = EHSceneValidator.ValidateProject();
            float t0 = Time.realtimeSinceStartup;

            var load = SceneManager.LoadSceneAsync(experienceSceneName, LoadSceneMode.Single);
            if (load == null)
            {
                status = $"Scene '{experienceSceneName}' not found in Build Settings.";
                failed = true;
                yield break;
            }
            load.allowSceneActivation = false;

            while (!load.isDone)
            {
                status = $"Loading {experienceSceneName}… {(load.progress * 100f):F0}%";
                if (load.progress >= 0.9f &&
                    Time.realtimeSinceStartup - t0 >= minSplashSeconds)
                {
                    load.allowSceneActivation = true;
                }
                yield return null;
            }

            if (issues.Count > 0)
                Debug.LogWarning("[EH] Boot validation notes:\n- " + string.Join("\n- ", issues));
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28
            };
            GUI.Label(new Rect(0, Screen.height / 2 - 80, Screen.width, 60), "EVENT HORIZON", style);
            var sub = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };
            GUI.Label(new Rect(0, Screen.height / 2, Screen.width, 40),
                failed ? status : status, sub);
        }
    }
}
