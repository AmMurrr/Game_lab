using GameLab.Core;
using GameLab.Utilities;
using UnityEngine;

namespace GameLab.UI
{
    public sealed class WinScreen : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject screenRoot;
        [SerializeField] private GameObject nextLevelButtonRoot;
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private bool hideNextLevelButtonWhenUnavailable = true;
        [SerializeField] private bool drawFallbackGuiWhenScreenRootMissing = true;

        private CanvasGroup canvasGroup;
        private GameSession subscribedSession;
        private bool isVisible;

        private void Awake()
        {
            if (screenRoot == null && IsUiObject(gameObject))
            {
                screenRoot = gameObject;
            }

            ResolveCanvasGroup();
            RefreshNextLevelButton();

            if (hideOnStart)
            {
                SetVisible(false);
            }
        }

        private void OnEnable()
        {
            SubscribeToSession();
            RefreshFromSessionState();
        }

        private void Start()
        {
            SubscribeToSession();
            RefreshFromSessionState();
        }

        private void OnDisable()
        {
            UnsubscribeFromSession();
        }

        private void OnGUI()
        {
            if (!isVisible || screenRoot != null || !drawFallbackGuiWhenScreenRootMissing)
            {
                return;
            }

            DrawFallbackGui();
        }

        public void Show()
        {
            RefreshNextLevelButton();
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void RestartLevel()
        {
            GameSession session = ResolveGameSession();
            if (session != null)
            {
                session.RestartLevel();
                return;
            }

            SceneLoader.RestartActiveScene();
        }

        public void LoadNextLevel()
        {
            GameSession session = ResolveGameSession();
            if (session != null)
            {
                session.LoadNextLevel();
                return;
            }

            SceneLoader.LoadNextScene();
        }

        public void ExitGame()
        {
            GameSession session = ResolveGameSession();
            if (session != null)
            {
                session.ExitGame();
                return;
            }

            SceneLoader.QuitApplication();
        }

        private void RefreshFromSessionState()
        {
            GameSession session = ResolveGameSession();
            if (session != null && session.IsLevelComplete)
            {
                Show();
            }
            else if (hideOnStart)
            {
                Hide();
            }
        }

        private void RefreshNextLevelButton()
        {
            if (nextLevelButtonRoot == null || !hideNextLevelButtonWhenUnavailable)
            {
                return;
            }

            nextLevelButtonRoot.SetActive(CanLoadNextLevel());
        }

        private void SetVisible(bool isVisible)
        {
            this.isVisible = isVisible;

            if (screenRoot == null)
            {
                return;
            }

            if (canvasGroup != null)
            {
                screenRoot.SetActive(true);
                canvasGroup.alpha = isVisible ? 1f : 0f;
                canvasGroup.interactable = isVisible;
                canvasGroup.blocksRaycasts = isVisible;
                return;
            }

            screenRoot.SetActive(isVisible);
        }

        private void DrawFallbackGui()
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            float panelWidth = Mathf.Clamp(Screen.width - 32f, 220f, 360f);
            float panelHeight = Mathf.Clamp(Screen.height - 32f, 220f, 250f);
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28
            };

            GUILayout.BeginArea(panelRect, GUI.skin.window);
            GUILayout.Space(14f);
            GUILayout.Label("Level Complete", titleStyle, GUILayout.Height(42f));
            GUILayout.Space(18f);

            if (GUILayout.Button("Restart", GUILayout.Height(40f)))
            {
                RestartLevel();
            }

            bool wasEnabled = GUI.enabled;
            GUI.enabled = CanLoadNextLevel();

            if (GUILayout.Button("Next Level", GUILayout.Height(40f)))
            {
                LoadNextLevel();
            }

            GUI.enabled = wasEnabled;

            if (GUILayout.Button("Exit", GUILayout.Height(40f)))
            {
                ExitGame();
            }

            GUILayout.EndArea();
        }

        private bool CanLoadNextLevel()
        {
            GameSession session = ResolveGameSession();
            return session != null
                ? session.CanLoadNextLevel()
                : SceneLoader.CanLoadNextScene();
        }

        private void SubscribeToSession()
        {
            GameSession session = ResolveGameSession();
            if (session == null || session == subscribedSession)
            {
                return;
            }

            UnsubscribeFromSession();
            subscribedSession = session;
            subscribedSession.LevelStarted += Hide;
            subscribedSession.LevelCompleted += Show;
        }

        private void UnsubscribeFromSession()
        {
            if (subscribedSession == null)
            {
                return;
            }

            subscribedSession.LevelStarted -= Hide;
            subscribedSession.LevelCompleted -= Show;
            subscribedSession = null;
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

        private void ResolveCanvasGroup()
        {
            if (screenRoot == null)
            {
                return;
            }

            canvasGroup = screenRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null && screenRoot == gameObject)
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
