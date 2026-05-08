using TMPro;
using UnityEngine;

namespace GameLab.UI
{
    public sealed class CreditsScroller : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private float pixelsPerSecond = 35f;
        [SerializeField] private float startDelay = 0.75f;
        [SerializeField] private float bottomPadding = 80f;
        [SerializeField] private float topPadding = 80f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool loop;

        private float startY;
        private float endY;
        private float delayRemaining;
        private bool initialized;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            Restart();
        }

        private void Start()
        {
            Restart();
        }

        private void Update()
        {
            if (!initialized)
            {
                Restart();
            }

            if (content == null)
            {
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (delayRemaining > 0f)
            {
                delayRemaining -= deltaTime;
                return;
            }

            Vector2 position = content.anchoredPosition;
            position.y += pixelsPerSecond * deltaTime;

            if (position.y >= endY)
            {
                if (loop)
                {
                    position.y = startY;
                    delayRemaining = startDelay;
                }
                else
                {
                    position.y = endY;
                    enabled = false;
                }
            }

            content.anchoredPosition = position;
        }

        public void Restart()
        {
            ResolveReferences();

            if (content == null)
            {
                initialized = false;
                return;
            }

            Canvas.ForceUpdateCanvases();
            ResizeContentToPreferredTextHeight();

            float viewportHeight = ResolveViewportHeight();
            float contentHeight = content.rect.height;
            Vector2 pivot = content.pivot;

            startY = (-viewportHeight * 0.5f) - bottomPadding - (contentHeight * (1f - pivot.y));
            endY = (viewportHeight * 0.5f) + topPadding + (contentHeight * pivot.y);
            delayRemaining = startDelay;
            initialized = true;

            Vector2 position = content.anchoredPosition;
            position.y = startY;
            content.anchoredPosition = position;
        }

        private void ResolveReferences()
        {
            if (content == null)
            {
                content = GetComponent<RectTransform>();
            }

            if (viewport == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    viewport = canvas.GetComponent<RectTransform>();
                }
            }
        }

        private void ResizeContentToPreferredTextHeight()
        {
            TMP_Text text = content.GetComponent<TMP_Text>();
            if (text == null)
            {
                return;
            }

            text.ForceMeshUpdate();

            Vector2 size = content.sizeDelta;
            size.y = Mathf.Max(size.y, text.preferredHeight);
            content.sizeDelta = size;
        }

        private float ResolveViewportHeight()
        {
            if (viewport != null && viewport.rect.height > 0f)
            {
                return viewport.rect.height;
            }

            return Screen.height > 0 ? Screen.height : 600f;
        }
    }
}
