using System.Collections;
using GameLab.Core;
using GameLab.Utilities;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameLab.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private bool allowPauseAfterLevelComplete;

        private CanvasGroup canvasGroup;
        private bool isPaused;
        private Coroutine exitRoutine;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            if (menuRoot == null && IsUiObject(gameObject))
            {
                menuRoot = gameObject;
            }

            ResolveCanvasGroup();

            if (hideOnStart)
            {
                SetVisible(false);
            }
        }

        private void Update()
        {
            if (WasPausePressed())
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
                return;
            }

            PauseGame();
        }

        public void PauseGame()
        {
            if (!allowPauseAfterLevelComplete && IsLevelComplete())
            {
                return;
            }

            isPaused = true;
            Time.timeScale = 0f;
            SetVisible(true);
        }

        public void ResumeGame()
        {
            if (!isPaused)
            {
                SetVisible(false);
                return;
            }

            isPaused = false;
            Time.timeScale = 1f;
            SetVisible(false);
        }

        public void RestartLevel()
        {
            isPaused = false;
            SetVisible(false);

            GameSession session = ResolveGameSession();
            if (session != null)
            {
                session.RestartLevel();
                return;
            }

            SceneLoader.RestartActiveScene();
        }

        public void ExitGame()
        {
            if (exitRoutine != null)
            {
                return;
            }

            isPaused = false;
            Time.timeScale = 1f;
            SetVisible(false);

            exitRoutine = StartCoroutine(ExitGameAfterUiEvent());
        }

        private IEnumerator ExitGameAfterUiEvent()
        {
            yield return null;

            GameSession session = ResolveGameSession();
            if (session != null)
            {
                session.ExitGame();
                yield break;
            }

            SceneLoader.QuitApplication();
        }

        private bool WasPausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private bool IsLevelComplete()
        {
            GameSession session = ResolveGameSession();
            return session != null && session.IsLevelComplete;
        }

        private GameSession ResolveGameSession()
        {
            if (gameSession == null)
            {
                gameSession = GameSession.Instance != null
                    ? GameSession.Instance
                    : FindSceneObject<GameSession>();
            }

            return gameSession;
        }

        private void SetVisible(bool isVisible)
        {
            if (menuRoot == null)
            {
                return;
            }

            if (canvasGroup != null)
            {
                menuRoot.SetActive(true);
                canvasGroup.alpha = isVisible ? 1f : 0f;
                canvasGroup.interactable = isVisible;
                canvasGroup.blocksRaycasts = isVisible;
                return;
            }

            menuRoot.SetActive(isVisible);
        }

        private void ResolveCanvasGroup()
        {
            if (menuRoot == null)
            {
                return;
            }

            canvasGroup = menuRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null && menuRoot == gameObject)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private static bool IsUiObject(GameObject candidate)
        {
            return candidate.GetComponent<RectTransform>() != null
                || candidate.GetComponent<CanvasGroup>() != null;
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
