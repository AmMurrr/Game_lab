using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLab.Utilities
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("Scenes")]
#if UNITY_EDITOR
        [SerializeField] private SceneAsset nextSceneAsset;
        [SerializeField] private SceneAsset exitSceneAsset;
#endif
        [SerializeField] private string nextSceneName;
        [SerializeField] private string exitSceneName;
        [SerializeField] private bool quitApplicationWhenExitSceneMissing = true;

        public string NextSceneName => nextSceneName;
        public string ExitSceneName => exitSceneName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            SyncSceneName(nextSceneAsset, ref nextSceneName);
            SyncSceneName(exitSceneAsset, ref exitSceneName);
        }
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one SceneLoader exists in the scene. The newest one will still work if referenced directly.", this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool CanLoadNextLevel()
        {
            return CanLoadNextScene(nextSceneName);
        }

        public void RestartCurrentScene()
        {
            RestartActiveScene();
        }

        public void LoadNextLevel()
        {
            LoadNextScene(nextSceneName);
        }

        public void LoadConfiguredScene(string sceneName)
        {
            LoadSceneByName(sceneName);
        }

        public void ExitGame()
        {
            if (HasText(exitSceneName))
            {
                LoadSceneByName(exitSceneName);
                return;
            }

            if (quitApplicationWhenExitSceneMissing)
            {
                QuitApplication();
            }
        }

        public static void RestartActiveScene()
        {
            ResumeGameTime();

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.name);
        }

        public static bool CanLoadNextScene(string preferredSceneName = null)
        {
            if (HasText(preferredSceneName))
            {
                return CanLoadScene(preferredSceneName);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.buildIndex >= 0
                && activeScene.buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
        }

        public static void LoadNextScene(string preferredSceneName = null)
        {
            ResumeGameTime();

            if (HasText(preferredSceneName))
            {
                LoadSceneByName(preferredSceneName);
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            int nextBuildIndex = activeScene.buildIndex + 1;

            if (activeScene.buildIndex >= 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextBuildIndex);
                return;
            }

            Debug.LogWarning("No next level is available in Build Settings.");
        }

        public static bool LoadSceneByName(string sceneName)
        {
            ResumeGameTime();

            int buildIndex;
            if (!TryGetSceneBuildIndex(sceneName, out buildIndex))
            {
                Debug.LogWarning($"Scene '{sceneName}' is not listed in Build Settings.");
                return false;
            }

            SceneManager.LoadScene(buildIndex);
            return true;
        }

        public static bool CanLoadScene(string sceneName)
        {
            int buildIndex;
            return TryGetSceneBuildIndex(sceneName, out buildIndex);
        }

        private static bool TryGetSceneBuildIndex(string sceneName, out int buildIndex)
        {
            if (!HasText(sceneName))
            {
                buildIndex = -1;
                return false;
            }

            string trimmedSceneName = sceneName.Trim();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameWithoutExtension = Path.GetFileNameWithoutExtension(scenePath);

                if (string.Equals(scenePath, trimmedSceneName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sceneNameWithoutExtension, trimmedSceneName, StringComparison.OrdinalIgnoreCase))
                {
                    buildIndex = i;
                    return true;
                }
            }

            buildIndex = -1;
            return false;
        }

        public static void QuitApplication()
        {
            ResumeGameTime();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void ResumeGameTime()
        {
            Time.timeScale = 1f;
        }

        private static bool HasText(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Trim().Length > 0;
        }

#if UNITY_EDITOR
        private static void SyncSceneName(SceneAsset sceneAsset, ref string sceneName)
        {
            if (sceneAsset != null)
            {
                sceneName = sceneAsset.name;
            }
        }
#endif
    }
}
