using GameLab.Core;
using GameLab.Player;
using UnityEngine;

namespace GameLab.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelExit : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private bool completeOnTriggerEnter = true;
        [SerializeField] private bool completeOnTriggerStay;
        [SerializeField] private bool completeOnCollisionEnter = true;
        [SerializeField] private bool disableAfterComplete = true;

        private bool hasCompleted;

        private void Reset()
        {
            Collider2D exitCollider = GetComponent<Collider2D>();
            if (exitCollider != null)
            {
                exitCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            Collider2D exitCollider = GetComponent<Collider2D>();
            if (exitCollider != null && !exitCollider.isTrigger)
            {
                Debug.LogWarning("LevelExit usually works best with a trigger Collider2D.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completeOnTriggerEnter)
            {
                TryComplete(other);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (completeOnTriggerStay)
            {
                TryComplete(other);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (completeOnCollisionEnter)
            {
                TryComplete(collision.collider);
            }
        }

        private void TryComplete(Component other)
        {
            if (hasCompleted || other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            GameSession session = ResolveGameSession();
            if (session == null)
            {
                Debug.LogWarning("LevelExit was reached, but no GameSession was found.", this);
                return;
            }

            hasCompleted = true;
            session.CompleteLevel();

            if (disableAfterComplete)
            {
                enabled = false;
            }
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
