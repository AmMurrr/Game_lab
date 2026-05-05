using UnityEngine;

namespace GameLab.Corpses
{
    public enum CorpseState
    {
        Fresh,
        Rotten,
        Skeleton
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CorpseBehavior : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite freshSprite;
        [SerializeField] private Sprite rottenSprite;
        [SerializeField] private Sprite skeletonSprite;
        [SerializeField] private Color freshColor = Color.white;
        [SerializeField] private Color rottenColor = new Color(0.62f, 0.78f, 0.28f, 1f);
        [SerializeField] private Color skeletonColor = new Color(1f, 1f, 1f, 0.45f);

        [Header("Physics")]
        [SerializeField] private Collider2D solidCollider;
        [SerializeField, Min(1f)] private float rottenJumpMultiplier = 1.35f;

        private Rigidbody2D body;
        private Collider2D[] colliders;
        private Sprite initialSprite;
        private CorpseState state;
        private int deathAge;
        private int deathsBeforeRotting = 4;
        private int deathsBeforeSkeletonAfterRotting = 2;
        private int deathsBeforeDisappearingAfterSkeleton = 2;

        public CorpseState State => state;
        public int DeathAge => deathAge;
        public bool IsSolid => state != CorpseState.Skeleton;
        public float JumpMultiplier => state == CorpseState.Rotten ? rottenJumpMultiplier : 1f;

        private int RottingDeathAge => Mathf.Max(1, deathsBeforeRotting);
        private int SkeletonDeathAge => RottingDeathAge + Mathf.Max(1, deathsBeforeSkeletonAfterRotting);
        private int DisappearDeathAge => SkeletonDeathAge + Mathf.Max(0, deathsBeforeDisappearingAfterSkeleton);

        public bool ShouldDisappear =>
            deathsBeforeDisappearingAfterSkeleton > 0 && deathAge >= DisappearDeathAge;

        private void Awake()
        {
            CacheComponents();
        }

        private void Reset()
        {
            CacheComponents();
        }

        public void Initialize(
            int deathsBeforeRotting,
            int deathsBeforeSkeletonAfterRotting,
            int deathsBeforeDisappearingAfterSkeleton,
            float rottenJumpMultiplier,
            Vector2 inheritedVelocity)
        {
            CacheComponents();

            this.deathsBeforeRotting = Mathf.Max(1, deathsBeforeRotting);
            this.deathsBeforeSkeletonAfterRotting = Mathf.Max(1, deathsBeforeSkeletonAfterRotting);
            this.deathsBeforeDisappearingAfterSkeleton = Mathf.Max(0, deathsBeforeDisappearingAfterSkeleton);
            this.rottenJumpMultiplier = Mathf.Max(1f, rottenJumpMultiplier);

            deathAge = 0;
            state = CorpseState.Fresh;

            if (freshSprite == null)
            {
                freshSprite = initialSprite;
            }

            ConfigureBody(inheritedVelocity);
            ApplyState();
        }

        public bool AgeByDeath()
        {
            deathAge++;

            CorpseState nextState = CalculateState();
            if (nextState != state)
            {
                state = nextState;
                ApplyState();
            }

            return !ShouldDisappear;
        }

        private void CacheComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (solidCollider == null)
            {
                solidCollider = GetComponent<Collider2D>();
            }

            body = GetComponent<Rigidbody2D>();
            colliders = GetComponents<Collider2D>();

            if (spriteRenderer != null && initialSprite == null)
            {
                initialSprite = spriteRenderer.sprite;
            }
        }

        private CorpseState CalculateState()
        {
            if (deathAge >= SkeletonDeathAge)
            {
                return CorpseState.Skeleton;
            }

            if (deathAge >= RottingDeathAge)
            {
                return CorpseState.Rotten;
            }

            return CorpseState.Fresh;
        }

        private void ConfigureBody(Vector2 inheritedVelocity)
        {
            if (body == null)
            {
                return;
            }

            body.bodyType = RigidbodyType2D.Dynamic;
            body.simulated = true;
            body.freezeRotation = true;
            body.angularVelocity = 0f;
            SetVelocity(inheritedVelocity);
        }

        private void ApplyState()
        {
            switch (state)
            {
                case CorpseState.Fresh:
                    SetView(freshSprite, freshColor);
                    SetCollisionEnabled(true);
                    SetBodySimulated(true);
                    break;

                case CorpseState.Rotten:
                    SetView(rottenSprite != null ? rottenSprite : freshSprite, rottenColor);
                    SetCollisionEnabled(true);
                    SetBodySimulated(true);
                    break;

                case CorpseState.Skeleton:
                    SetView(skeletonSprite != null ? skeletonSprite : freshSprite, skeletonColor);
                    SetVelocity(Vector2.zero);
                    SetCollisionEnabled(false);
                    SetBodySimulated(false);
                    break;
            }
        }

        private void SetView(Sprite sprite, Color color)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            spriteRenderer.color = color;
        }

        private void SetCollisionEnabled(bool isEnabled)
        {
            if (solidCollider != null)
            {
                solidCollider.enabled = isEnabled;
                return;
            }

            if (colliders == null || colliders.Length == 0)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = isEnabled;
                }
            }
        }

        private void SetBodySimulated(bool isSimulated)
        {
            if (body == null)
            {
                return;
            }

            body.simulated = isSimulated;
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
    }
}
