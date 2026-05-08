using UnityEngine;

namespace GameLab.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class FlyingPatrol2D : MonoBehaviour
    {
        [Header("Patrol")]
        [SerializeField, Min(0f)] private float speed = 2f;
        [SerializeField] private Transform targetPoint;
        [SerializeField] private Vector2 localPatrolOffset = new Vector2(3f, 0f);
        [SerializeField, Min(0.01f)] private float arriveDistance = 0.05f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipSpriteToMovement = true;
        [SerializeField] private bool spriteFacesRightByDefault;

        private Rigidbody2D body;
        private Vector2 origin;
        private bool movingToTarget = true;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ResolveSpriteRenderer();
            origin = body.position;
        }

        private void Reset()
        {
            Rigidbody2D resetBody = GetComponent<Rigidbody2D>();
            resetBody.gravityScale = 0f;
            resetBody.freezeRotation = true;
            ResolveSpriteRenderer();
        }

        private void FixedUpdate()
        {
            Vector2 destination = movingToTarget ? GetTargetPosition() : origin;
            Vector2 currentPosition = body.position;
            Vector2 nextPosition = Vector2.MoveTowards(
                currentPosition,
                destination,
                speed * Time.fixedDeltaTime);

            body.MovePosition(nextPosition);

            Vector2 movement = nextPosition - currentPosition;
            UpdateFacing(movement);

            if (Vector2.Distance(nextPosition, destination) <= arriveDistance)
            {
                movingToTarget = !movingToTarget;
            }
        }

        private Vector2 GetTargetPosition()
        {
            if (targetPoint != null)
            {
                return targetPoint.position;
            }

            return origin + localPatrolOffset;
        }

        private void UpdateFacing(Vector2 movement)
        {
            if (!flipSpriteToMovement || spriteRenderer == null || Mathf.Abs(movement.x) <= 0.001f)
            {
                return;
            }

            spriteRenderer.flipX = spriteFacesRightByDefault
                ? movement.x < 0f
                : movement.x > 0f;
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

        private void OnDrawGizmosSelected()
        {
            Vector3 start = Application.isPlaying ? (Vector3)origin : transform.position;
            Vector3 end = targetPoint != null
                ? targetPoint.position
                : start + (Vector3)localPatrolOffset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.12f);
            Gizmos.DrawWireSphere(end, 0.12f);
        }
    }
}
