using GameLab.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLab.UI
{
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;

        [Header("Start Game")]
#if UNITY_EDITOR
        [SerializeField] private SceneAsset firstLevelSceneAsset;
#endif
        [SerializeField] private string firstLevelSceneName;
        [SerializeField] private bool loadNextSceneWhenFirstLevelMissing = true;

        public void StartGame()
        {
            Time.timeScale = 1f;

            if (HasText(firstLevelSceneName))
            {
                SceneLoader loader = ResolveSceneLoader();
                if (loader != null)
                {
                    loader.LoadConfiguredScene(firstLevelSceneName);
                    return;
                }

                SceneLoader.LoadSceneByName(firstLevelSceneName);
                return;
            }

            if (loadNextSceneWhenFirstLevelMissing)
            {
                SceneLoader loader = ResolveSceneLoader();
                if (loader != null)
                {
                    loader.LoadNextLevel();
                    return;
                }

                SceneLoader.LoadNextScene();
                return;
            }

            Debug.LogWarning("Main menu does not have a first level scene configured.", this);
        }

        public void LoadScene(string sceneName)
        {
            if (!HasText(sceneName))
            {
                Debug.LogWarning("Cannot load an empty scene name.", this);
                return;
            }

            Time.timeScale = 1f;

            SceneLoader loader = ResolveSceneLoader();
            if (loader != null)
            {
                loader.LoadConfiguredScene(sceneName);
                return;
            }

            SceneLoader.LoadSceneByName(sceneName);
        }

        public void ExitGame()
        {
            Time.timeScale = 1f;

            SceneLoader loader = ResolveSceneLoader();
            if (loader != null)
            {
                loader.ExitGame();
                return;
            }

            SceneLoader.QuitApplication();
        }

        private SceneLoader ResolveSceneLoader()
        {
            if (sceneLoader == null)
            {
                sceneLoader = SceneLoader.Instance != null
                    ? SceneLoader.Instance
                    : FindSceneObject<SceneLoader>();
            }

            return sceneLoader;
        }

        private static bool HasText(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Trim().Length > 0;
        }

        private static T FindSceneObject<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>();
#else
            return FindObjectOfType<T>();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (firstLevelSceneAsset != null)
            {
                firstLevelSceneName = firstLevelSceneAsset.name;
            }
        }
#endif
    }
}
