using System;
using GameLab.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLab.Core
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private bool keepBetweenScenes;
        [SerializeField, Min(0)] private int startingDeathCount;

        [Header("Level Flow")]
        [SerializeField] private bool pauseOnLevelComplete = true;
        [SerializeField] private bool resumeTimeScaleOnLevelStart = true;
        [SerializeField] private SceneLoader sceneLoader;

        private int deathCount;
        private bool isLevelComplete;
        private bool isPrimaryInstance;

        public event Action<int> DeathCountChanged;
        public event Action LevelStarted;
        public event Action LevelCompleted;

        public int DeathCount => deathCount;
        public bool IsLevelComplete => isLevelComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one GameSession exists in the scene. The newest one will still work if referenced directly.", this);
                return;
            }

            Instance = this;
            isPrimaryInstance = true;
            deathCount = startingDeathCount;
            BeginLevel();
            SceneManager.sceneLoaded += HandleSceneLoaded;

            if (keepBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (isPrimaryInstance)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterDeath()
        {
            deathCount++;
            DeathCountChanged?.Invoke(deathCount);
        }

        public void ResetDeathCount()
        {
            deathCount = 0;
            DeathCountChanged?.Invoke(deathCount);
        }

        public void CompleteLevel()
        {
            if (isLevelComplete)
            {
                return;
            }

            isLevelComplete = true;

            if (pauseOnLevelComplete)
            {
                Time.timeScale = 0f;
            }

            LevelCompleted?.Invoke();
        }

        public void RestartLevel()
        {
            ResumeGameTime();
            isLevelComplete = false;

            SceneLoader loader = ResolveSceneLoader();
            if (loader != null)
            {
                loader.RestartCurrentScene();
                return;
            }

            SceneLoader.RestartActiveScene();
        }

        public void LoadNextLevel()
        {
            ResumeGameTime();

            SceneLoader loader = ResolveSceneLoader();
            if (loader != null)
            {
                loader.LoadNextLevel();
                return;
            }

            SceneLoader.LoadNextScene();
        }

        public void ExitGame()
        {
            ResumeGameTime();

            SceneLoader loader = ResolveSceneLoader();
            if (loader != null)
            {
                loader.ExitGame();
                return;
            }

            SceneLoader.QuitApplication();
        }

        public bool CanLoadNextLevel()
        {
            SceneLoader loader = ResolveSceneLoader();
            return loader != null
                ? loader.CanLoadNextLevel()
                : SceneLoader.CanLoadNextScene();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BeginLevel();
        }

        private void BeginLevel()
        {
            isLevelComplete = false;

            if (resumeTimeScaleOnLevelStart)
            {
                ResumeGameTime();
            }

            LevelStarted?.Invoke();
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

        private static void ResumeGameTime()
        {
            Time.timeScale = 1f;
        }

        private static T FindSceneObject<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>();
#else
            return FindObjectOfType<T>();
#endif
        }
    }
}
