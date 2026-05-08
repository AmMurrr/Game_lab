using UnityEngine;

namespace GameLab.UI
{
    public sealed class MenuMusicToggle : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip musicClip;

        [Header("Playback")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool restartWhenEnabled;
        [SerializeField] private bool stopWhenDisabled;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private bool isMusicEnabled;
        private bool isPaused;

        public bool IsMusicEnabled => isMusicEnabled;

        private void Awake()
        {
            ResolveAudioSource();
            ConfigureAudioSource();
        }

        private void Start()
        {
            SetMusicEnabled(playOnStart);
        }

        public void ToggleMusic()
        {
            SetMusicEnabled(!isMusicEnabled);
        }

        public void PlayMusic()
        {
            SetMusicEnabled(true);
        }

        public void PauseMusic()
        {
            SetMusicEnabled(false);
        }

        public void SetMusicEnabled(bool shouldPlay)
        {
            isMusicEnabled = shouldPlay;

            if (musicSource == null)
            {
                return;
            }

            if (shouldPlay)
            {
                if (musicSource.clip == null)
                {
                    Debug.LogWarning("Menu music cannot play because AudioSource has no AudioClip.", this);
                    return;
                }

                if (restartWhenEnabled)
                {
                    musicSource.Stop();
                    isPaused = false;
                }

                if (!musicSource.isPlaying)
                {
                    if (isPaused)
                    {
                        musicSource.UnPause();
                    }
                    else
                    {
                        musicSource.Play();
                    }
                }

                isPaused = false;
                return;
            }

            if (stopWhenDisabled)
            {
                musicSource.Stop();
                isPaused = false;
                return;
            }

            isPaused = musicSource.isPlaying;
            musicSource.Pause();
        }

        private void ResolveAudioSource()
        {
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void ConfigureAudioSource()
        {
            if (musicSource == null)
            {
                return;
            }

            if (musicClip != null)
            {
                musicSource.clip = musicClip;
            }

            musicSource.playOnAwake = false;
            musicSource.loop = loop;
            musicSource.volume = volume;
            musicSource.spatialBlend = 0f;
        }

        private void OnValidate()
        {
            if (musicSource != null)
            {
                ConfigureAudioSource();
            }
        }
    }
}
