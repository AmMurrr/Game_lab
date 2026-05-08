using GameLab.Utilities;
using UnityEngine;

namespace GameLab.UI
{
    public sealed class FinalScreenMenu : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;

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

        private static T FindSceneObject<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>();
#else
            return FindObjectOfType<T>();
#endif
        }
    }
}
