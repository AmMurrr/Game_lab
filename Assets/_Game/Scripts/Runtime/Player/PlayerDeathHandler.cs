using System.Collections;
using GameLab.Core;
using GameLab.Corpses;
using UnityEngine;

namespace GameLab.Player
{
    [RequireComponent(typeof(PlayerController2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerDeathHandler : MonoBehaviour
    {
        [Header("Respawn")]
        [SerializeField] private RespawnPoint respawnPoint;
        [SerializeField, Min(0f)] private float respawnDelay;
        [SerializeField, Min(0f)] private float respawnInvulnerabilityTime = 0.15f;
        [SerializeField] private bool resetRotationOnRespawn = true;

        [Header("Corpse")]
        [SerializeField] private CorpseManager corpseManager;
        [SerializeField] private bool spawnCorpseOnDeath = true;

        [Header("Audio")]
        [SerializeField] private AudioSource deathAudioSource;
        [SerializeField] private AudioClip deathClip;
        [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;

        private PlayerController2D controller;
        private Rigidbody2D body;
        private Collider2D[] colliders;
        private SpriteRenderer[] renderers;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool isDead;
        private float invulnerableUntil;
        private Coroutine deathRoutine;

        public bool IsDead => isDead;

        private void Awake()
        {
            controller = GetComponent<PlayerController2D>();
            body = GetComponent<Rigidbody2D>();
            colliders = GetComponents<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            ResolveSceneReferences();
            ResolveDeathAudioSource();
            ConfigureDeathAudioSource();
            PreloadDeathClip();
        }

        private void OnEnable()
        {
            ResolveSceneReferences();
        }

        public void Die()
        {
            Die(null);
        }

        public void Die(Component source)
        {
            TryDie(source);
        }

        public bool TryDie()
        {
            return TryDie(null);
        }

        public bool TryDie(Component source)
        {
            if (!CanDie())
            {
                return false;
            }

            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
            }

            deathRoutine = StartCoroutine(DeathRoutine());
            return true;
        }

        private bool CanDie()
        {
            return enabled
                && gameObject.activeInHierarchy
                && !isDead
                && Time.time >= invulnerableUntil;
        }

        private IEnumerator DeathRoutine()
        {
            isDead = true;
            PlayDeathSound();

            Vector3 deathPosition = transform.position;
            Quaternion deathRotation = transform.rotation;
            Vector2 deathVelocity = GetVelocity();

            if (spawnCorpseOnDeath)
            {
                ResolveCorpseManager();
                if (corpseManager != null)
                {
                    corpseManager.RegisterPlayerDeath(deathPosition, deathRotation, deathVelocity);
                }
                else
                {
                    Debug.LogWarning("Player died, but no CorpseManager was found. No corpse was spawned.", this);
                }
            }

            GameSession session = GameSession.Instance;
            if (session != null)
            {
                session.RegisterDeath();
            }

            SetPlayerSimulation(false);

            if (respawnDelay > 0f)
            {
                yield return new WaitForSeconds(respawnDelay);
            }
            else
            {
                yield return null;
            }

            Respawn();
            SetPlayerSimulation(true);

            invulnerableUntil = Time.time + respawnInvulnerabilityTime;
            isDead = false;
            deathRoutine = null;
        }

        private void Respawn()
        {
            ResolveRespawnPoint();

            Vector3 respawnPosition = respawnPoint != null ? respawnPoint.Position : initialPosition;
            Quaternion respawnRotation = respawnPoint != null ? respawnPoint.Rotation : initialRotation;

            if (!resetRotationOnRespawn)
            {
                respawnRotation = transform.rotation;
            }

            transform.SetPositionAndRotation(respawnPosition, respawnRotation);
            body.angularVelocity = 0f;
            SetVelocity(Vector2.zero);
            controller.ResetMotion();
        }

        private void SetPlayerSimulation(bool isEnabled)
        {
            if (controller != null)
            {
                controller.enabled = isEnabled;
                if (!isEnabled)
                {
                    controller.ClearGrounding();
                }
            }

            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].enabled = isEnabled;
                    }
                }
            }

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].enabled = isEnabled;
                    }
                }
            }

            if (body != null)
            {
                SetVelocity(Vector2.zero);
                body.angularVelocity = 0f;
                body.simulated = isEnabled;
            }
        }

        private void ResolveSceneReferences()
        {
            ResolveRespawnPoint();
            ResolveCorpseManager();
        }

        private void PlayDeathSound()
        {
            if (deathClip == null)
            {
                return;
            }

            ResolveDeathAudioSource();
            ConfigureDeathAudioSource();
            PreloadDeathClip();

            if (deathAudioSource != null)
            {
                deathAudioSource.PlayOneShot(deathClip, deathVolume);
            }
        }

        private void ResolveDeathAudioSource()
        {
            if (deathAudioSource == null)
            {
                deathAudioSource = GetComponent<AudioSource>();
            }

            if (deathAudioSource == null && deathClip != null)
            {
                deathAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void ConfigureDeathAudioSource()
        {
            if (deathAudioSource == null)
            {
                return;
            }

            deathAudioSource.playOnAwake = false;
            deathAudioSource.loop = false;
            deathAudioSource.spatialBlend = 0f;
        }

        private void PreloadDeathClip()
        {
            if (deathClip != null && deathClip.loadState == AudioDataLoadState.Unloaded)
            {
                deathClip.LoadAudioData();
            }
        }

        private void ResolveRespawnPoint()
        {
            if (respawnPoint == null)
            {
                respawnPoint = FindSceneObject<RespawnPoint>();
            }
        }

        private void ResolveCorpseManager()
        {
            if (corpseManager == null)
            {
                corpseManager = CorpseManager.Instance != null
                    ? CorpseManager.Instance
                    : FindSceneObject<CorpseManager>();
            }
        }

        private static T FindSceneObject<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>();
#else
            return FindObjectOfType<T>();
#endif
        }

        private Vector2 GetVelocity()
        {
            if (body == null)
            {
                return Vector2.zero;
            }

#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        private void SetVelocity(Vector2 velocity)
        {
            if (body == null)
            {
                return;
            }

#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }

        private void OnValidate()
        {
            ConfigureDeathAudioSource();
        }
    }
}
