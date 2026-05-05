using UnityEngine;

namespace GameLab.Utilities
{
    public sealed class TimeScaleController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float normalTimeScale = 1f;
        [SerializeField, Min(0f)] private float pausedTimeScale;

        public bool IsPaused => Mathf.Approximately(Time.timeScale, pausedTimeScale);

        public void Pause()
        {
            Time.timeScale = pausedTimeScale;
        }

        public void Resume()
        {
            Time.timeScale = normalTimeScale;
        }

        public void SetPaused(bool isPaused)
        {
            Time.timeScale = isPaused ? pausedTimeScale : normalTimeScale;
        }

        public void TogglePaused()
        {
            SetPaused(!IsPaused);
        }
    }
}
