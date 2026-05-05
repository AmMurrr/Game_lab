using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLab.Corpses
{
    public sealed class CorpseManager : MonoBehaviour
    {
        public static CorpseManager Instance { get; private set; }

        [Header("Prefab")]
        [SerializeField] private CorpseBehavior corpsePrefab;
        [SerializeField] private Transform corpseParent;

        [Header("Limits")]
        [SerializeField, Min(0)] private int maxActiveCorpses = 6;
        [SerializeField, Min(1)] private int deathsBeforeRotting = 4;
        [SerializeField, Min(1)] private int deathsBeforeSkeletonAfterRotting = 2;
        [SerializeField, Min(0)] private int deathsBeforeDisappearingAfterSkeleton = 2;

        [Header("Corpse Physics")]
        [SerializeField, Min(1f)] private float rottenJumpMultiplier = 1.35f;
        [SerializeField, Range(0f, 1f)] private float inheritedVelocityScale = 0.2f;
        [SerializeField] private Vector2 spawnOffset;
        [SerializeField] private bool copyPlayerRotation;

        private readonly List<CorpseBehavior> corpses = new List<CorpseBehavior>();

        public event Action<int, int> CorpseCountChanged;

        public int MaxActiveCorpses => maxActiveCorpses;

        public int CorpseCount
        {
            get
            {
                PruneMissingCorpses();
                return corpses.Count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one CorpseManager exists in the scene. The newest one will still work if referenced directly.", this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            maxActiveCorpses = Mathf.Max(0, maxActiveCorpses);
            deathsBeforeRotting = Mathf.Max(1, deathsBeforeRotting);
            deathsBeforeSkeletonAfterRotting = Mathf.Max(1, deathsBeforeSkeletonAfterRotting);
            deathsBeforeDisappearingAfterSkeleton = Mathf.Max(0, deathsBeforeDisappearingAfterSkeleton);
            rottenJumpMultiplier = Mathf.Max(1f, rottenJumpMultiplier);
            inheritedVelocityScale = Mathf.Clamp01(inheritedVelocityScale);
        }

        public CorpseBehavior RegisterPlayerDeath(Vector3 playerPosition, Quaternion playerRotation, Vector2 playerVelocity)
        {
            AgeExistingCorpses();

            if (maxActiveCorpses == 0)
            {
                NotifyCorpseCountChanged();
                return null;
            }

            CorpseBehavior corpse = SpawnCorpse(playerPosition, playerRotation, playerVelocity);
            if (corpse != null)
            {
                corpses.Add(corpse);
            }

            EnforceCorpseLimit();
            NotifyCorpseCountChanged();
            return corpse;
        }

        public void ClearAllCorpses()
        {
            for (int i = corpses.Count - 1; i >= 0; i--)
            {
                DestroyCorpse(corpses[i]);
            }

            corpses.Clear();
            NotifyCorpseCountChanged();
        }

        private void AgeExistingCorpses()
        {
            for (int i = corpses.Count - 1; i >= 0; i--)
            {
                CorpseBehavior corpse = corpses[i];
                if (corpse == null)
                {
                    corpses.RemoveAt(i);
                    continue;
                }

                if (!corpse.AgeByDeath())
                {
                    DestroyCorpse(corpse);
                    corpses.RemoveAt(i);
                }
            }
        }

        private CorpseBehavior SpawnCorpse(Vector3 playerPosition, Quaternion playerRotation, Vector2 playerVelocity)
        {
            Vector3 corpsePosition = playerPosition + (Vector3)spawnOffset;
            Quaternion corpseRotation = copyPlayerRotation ? playerRotation : Quaternion.identity;
            Vector2 inheritedVelocity = playerVelocity * inheritedVelocityScale;

            CorpseBehavior corpse = corpsePrefab != null
                ? Instantiate(corpsePrefab, corpsePosition, corpseRotation, corpseParent)
                : CreateRuntimeCorpse(corpsePosition, corpseRotation);

            corpse.Initialize(
                deathsBeforeRotting,
                deathsBeforeSkeletonAfterRotting,
                deathsBeforeDisappearingAfterSkeleton,
                rottenJumpMultiplier,
                inheritedVelocity);

            return corpse;
        }

        private CorpseBehavior CreateRuntimeCorpse(Vector3 position, Quaternion rotation)
        {
            GameObject corpseObject = new GameObject("Corpse");
            corpseObject.transform.SetParent(corpseParent);
            corpseObject.transform.SetPositionAndRotation(position, rotation);

            SpriteRenderer spriteRenderer = corpseObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.white;

            BoxCollider2D collider = corpseObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            Rigidbody2D body = corpseObject.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;

            return corpseObject.AddComponent<CorpseBehavior>();
        }

        private void EnforceCorpseLimit()
        {
            PruneMissingCorpses();

            while (corpses.Count > maxActiveCorpses)
            {
                CorpseBehavior oldestCorpse = corpses[0];
                corpses.RemoveAt(0);
                DestroyCorpse(oldestCorpse);
            }
        }

        private void PruneMissingCorpses()
        {
            for (int i = corpses.Count - 1; i >= 0; i--)
            {
                if (corpses[i] == null)
                {
                    corpses.RemoveAt(i);
                }
            }
        }

        private void DestroyCorpse(CorpseBehavior corpse)
        {
            if (corpse == null)
            {
                return;
            }

            Destroy(corpse.gameObject);
        }

        private void NotifyCorpseCountChanged()
        {
            CorpseCountChanged?.Invoke(CorpseCount, maxActiveCorpses);
        }
    }
}
