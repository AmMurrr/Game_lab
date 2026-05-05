using System;
using UnityEngine;

namespace GameLab.Core
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private bool keepBetweenScenes;
        [SerializeField, Min(0)] private int startingDeathCount;

        private int deathCount;

        public event Action<int> DeathCountChanged;

        public int DeathCount => deathCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one GameSession exists in the scene. The newest one will still work if referenced directly.", this);
                return;
            }

            Instance = this;
            deathCount = startingDeathCount;

            if (keepBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
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
    }
}
