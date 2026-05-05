using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using GameLab.Corpses;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameLab.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float coyoteTime = 0.12f;

        [Header("Fast Fall")]
        [SerializeField] private float fastFallSpeed = 18f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
        [SerializeField] private float groundCheckRadius = 0.18f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Effects")]
        [SerializeField] private ParticleSystem runParticles;
        [SerializeField, Min(0f)] private float runParticleMinSpeed = 0.1f;
        [SerializeField] private bool clearRunParticlesOnStop;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip idleAnimation;
        [SerializeField] private AnimationClip runAnimation;
        [SerializeField] private AnimationClip jumpAnimation;
        [SerializeField] private AnimationClip fallAnimation;

        private readonly Collider2D[] groundHits = new Collider2D[8];

        private enum PlayerAnimationState
        {
            None,
            Idle,
            Run,
            Jump,
            Fall
        }

        private Rigidbody2D body;
        private Collider2D[] ownColliders;
        private PlayableGraph animationGraph;
        private AnimationMixerPlayable animationMixer;
        private Playable currentAnimationPlayable;
        private PlayerAnimationState currentAnimationState = PlayerAnimationState.None;
        private float horizontalInput;
        private float coyoteCounter;
        private float currentGroundJumpMultiplier = 1f;
        private bool jumpPressed;
        private bool fastFallHeld;
        private bool isFacingRight = true;
        private bool isGrounded;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ownColliders = GetComponents<Collider2D>();
            ResolveSpriteRenderer();
            ResolveRunParticles();
            isFacingRight = spriteRenderer == null || !spriteRenderer.flipX;
            body.freezeRotation = true;
            InitializeAnimationGraph();
            StopRunParticles(true);
        }

        private void OnEnable()
        {
            if (animationGraph.IsValid())
            {
                animationGraph.Play();
                currentAnimationState = PlayerAnimationState.None;
            }
        }

        private void Update()
        {
            ReadInput();
            UpdateFacingDirection();

            isGrounded = CheckGrounded();
            coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;

            if (jumpPressed && coyoteCounter > 0f)
            {
                Jump();
            }

            jumpPressed = false;
            UpdateRunParticles();
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            Vector2 velocity = GetVelocity();
            velocity.x = horizontalInput * moveSpeed;

            if (fastFallHeld && !isGrounded)
            {
                velocity.y = Mathf.Min(velocity.y, -fastFallSpeed);
            }

            SetVelocity(velocity);
            UpdateRunParticles();
            UpdateAnimation();
        }

        private void OnDisable()
        {
            horizontalInput = 0f;
            jumpPressed = false;
            fastFallHeld = false;
            currentGroundJumpMultiplier = 1f;

            if (animationGraph.IsValid())
            {
                animationGraph.Stop();
            }

            StopRunParticles(true);
        }

        private void OnDestroy()
        {
            if (animationGraph.IsValid())
            {
                animationGraph.Destroy();
            }
        }

        public void ResetMotion()
        {
            horizontalInput = 0f;
            jumpPressed = false;
            fastFallHeld = false;
            coyoteCounter = 0f;
            currentGroundJumpMultiplier = 1f;
            isGrounded = false;
            SetVelocity(Vector2.zero);
            currentAnimationState = PlayerAnimationState.None;
            StopRunParticles(true);
            UpdateAnimation();
        }

        private void ReadInput()
        {
            horizontalInput = 0f;
            fastFallHeld = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    horizontalInput -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    horizontalInput += 1f;
                }

                jumpPressed = keyboard.spaceKey.wasPressedThisFrame
                    || keyboard.wKey.wasPressedThisFrame
                    || keyboard.upArrowKey.wasPressedThisFrame;

                fastFallHeld = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            horizontalInput = Input.GetAxisRaw("Horizontal");
            jumpPressed = Input.GetButtonDown("Jump");
            fastFallHeld = Input.GetAxisRaw("Vertical") < -0.5f;
#endif
        }

        private void Jump()
        {
            Vector2 velocity = GetVelocity();
            velocity.y = jumpForce * currentGroundJumpMultiplier;
            SetVelocity(velocity);
            coyoteCounter = 0f;
            isGrounded = false;
        }

        private bool CheckGrounded()
        {
            Vector2 checkPosition = GetGroundCheckPosition();
            int hitCount = Physics2D.OverlapCircleNonAlloc(checkPosition, groundCheckRadius, groundHits, groundLayer);
            bool hasGround = false;
            float bestJumpMultiplier = 1f;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = groundHits[i];
                if (hit == null || hit.isTrigger || IsOwnCollider(hit))
                {
                    continue;
                }

                hasGround = true;
                CorpseBehavior corpse = hit.GetComponentInParent<CorpseBehavior>();
                if (corpse != null)
                {
                    bestJumpMultiplier = Mathf.Max(bestJumpMultiplier, corpse.JumpMultiplier);
                }
            }

            currentGroundJumpMultiplier = hasGround ? bestJumpMultiplier : 1f;
            return hasGround;
        }

        private void InitializeAnimationGraph()
        {
            if (animator == null)
            {
                TryGetComponent(out animator);
            }

            if (animator == null && HasAnimationClips())
            {
                animator = gameObject.AddComponent<Animator>();
            }

            if (animator == null || !HasAnimationClips())
            {
                return;
            }

            animator.applyRootMotion = false;

            animationGraph = PlayableGraph.Create($"{nameof(PlayerController2D)}Animation");
            animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            animationMixer = AnimationMixerPlayable.Create(animationGraph, 1);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Player Animation", animator);
            output.SetSourcePlayable(animationMixer);
            animationGraph.Play();
        }

        private bool HasAnimationClips()
        {
            return idleAnimation != null
                || runAnimation != null
                || jumpAnimation != null
                || fallAnimation != null;
        }

        private void ResolveSpriteRenderer()
        {
            if (spriteRenderer == null)
            {
                TryGetComponent(out spriteRenderer);
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void ResolveRunParticles()
        {
            if (runParticles == null)
            {
                runParticles = GetComponentInChildren<ParticleSystem>(true);
            }

            if (runParticles != null && !runParticles.gameObject.activeSelf)
            {
                runParticles.gameObject.SetActive(true);
            }
        }

        private void UpdateFacingDirection()
        {
            if (spriteRenderer == null || Mathf.Abs(horizontalInput) <= 0.01f)
            {
                return;
            }

            bool shouldFaceRight = horizontalInput > 0f;
            if (shouldFaceRight == isFacingRight)
            {
                return;
            }

            isFacingRight = shouldFaceRight;
            spriteRenderer.flipX = !isFacingRight;
        }

        private void UpdateAnimation()
        {
            if (!animationGraph.IsValid())
            {
                return;
            }

            PlayerAnimationState nextState = GetAnimationState();
            if (nextState == currentAnimationState)
            {
                return;
            }

            AnimationClip nextClip = GetAnimationClip(nextState);
            if (nextClip == null)
            {
                return;
            }

            PlayAnimation(nextState, nextClip);
        }

        private PlayerAnimationState GetAnimationState()
        {
            Vector2 velocity = GetVelocity();

            if (!isGrounded)
            {
                return fastFallHeld || velocity.y <= 0f
                    ? PlayerAnimationState.Fall
                    : PlayerAnimationState.Jump;
            }

            return Mathf.Abs(horizontalInput) > 0.01f || Mathf.Abs(velocity.x) > 0.01f
                ? PlayerAnimationState.Run
                : PlayerAnimationState.Idle;
        }

        private AnimationClip GetAnimationClip(PlayerAnimationState state)
        {
            switch (state)
            {
                case PlayerAnimationState.Run:
                    return runAnimation != null ? runAnimation : idleAnimation;
                case PlayerAnimationState.Jump:
                    return jumpAnimation != null ? jumpAnimation : fallAnimation;
                case PlayerAnimationState.Fall:
                    return fallAnimation != null ? fallAnimation : jumpAnimation;
                case PlayerAnimationState.Idle:
                    return idleAnimation;
                default:
                    return null;
            }
        }

        private void PlayAnimation(PlayerAnimationState state, AnimationClip clip)
        {
            if (currentAnimationPlayable.IsValid())
            {
                animationGraph.Disconnect(animationMixer, 0);
                currentAnimationPlayable.Destroy();
            }

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(animationGraph, clip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);

            animationGraph.Connect(clipPlayable, 0, animationMixer, 0);
            animationMixer.SetInputWeight(0, 1f);
            currentAnimationPlayable = clipPlayable;
            currentAnimationState = state;
        }

        private void UpdateRunParticles()
        {
            if (runParticles == null)
            {
                return;
            }

            Vector2 velocity = GetVelocity();
            bool shouldPlay = isGrounded
                && Mathf.Abs(horizontalInput) > 0.01f
                && Mathf.Abs(velocity.x) > runParticleMinSpeed;

            if (shouldPlay)
            {
                if (!runParticles.isPlaying)
                {
                    runParticles.Play(true);
                }

                return;
            }

            StopRunParticles(clearRunParticlesOnStop);
        }

        private void StopRunParticles(bool clear)
        {
            if (runParticles == null || (!clear && !runParticles.isPlaying))
            {
                return;
            }

            ParticleSystemStopBehavior stopBehavior = clear
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;

            runParticles.Stop(true, stopBehavior);
        }

        public Vector2 GetCurrentVelocity()
        {
            return GetVelocity();
        }

        public void SetCurrentVelocity(Vector2 velocity)
        {
            SetVelocity(velocity);
        }

        public void ClearGrounding()
        {
            coyoteCounter = 0f;
            isGrounded = false;
            currentGroundJumpMultiplier = 1f;
        }

        private Vector2 GetGroundCheckPosition()
        {
            if (groundCheck != null)
            {
                return groundCheck.position;
            }

            return (Vector2)transform.position + groundCheckOffset;
        }

        private bool IsOwnCollider(Collider2D hit)
        {
            for (int i = 0; i < ownColliders.Length; i++)
            {
                if (ownColliders[i] == hit)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        private void SetVelocity(Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(GetGroundCheckPosition(), groundCheckRadius);
        }
    }
}
