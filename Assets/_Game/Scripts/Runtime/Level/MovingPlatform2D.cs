using UnityEngine;

namespace GameLab.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MovingPlatform2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float speed = 2f;
        [SerializeField] private Transform targetPoint;
        [SerializeField] private Vector2 localMoveOffset = new Vector2(3f, 0f);
        [SerializeField, Min(0.001f)] private float arriveDistance = 0.02f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float startDelay;
        [SerializeField, Min(0f)] private float waitAtEnds;
        [SerializeField] private bool moveOnStart = true;

        private Rigidbody2D body;
        private Vector2 origin;
        private Vector2 currentVelocity;
        private bool movingToTarget = true;
        private float waitTimer;

        public Vector2 CurrentVelocity => currentVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ConfigureBody();

            origin = body.position;
            waitTimer = moveOnStart ? startDelay : float.PositiveInfinity;
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            ConfigureBody();
        }

        private void OnEnable()
        {
            currentVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            Vector2 currentPosition = body.position;

            if (waitTimer > 0f)
            {
                waitTimer -= Time.fixedDeltaTime;
                currentVelocity = Vector2.zero;
                return;
            }

            Vector2 destination = movingToTarget ? GetTargetPosition() : origin;
            Vector2 nextPosition = Vector2.MoveTowards(
                currentPosition,
                destination,
                speed * Time.fixedDeltaTime);

            body.MovePosition(nextPosition);
            currentVelocity = (nextPosition - currentPosition) / Time.fixedDeltaTime;

            if (Vector2.Distance(nextPosition, destination) <= arriveDistance)
            {
                movingToTarget = !movingToTarget;
                waitTimer = waitAtEnds;
            }
        }

        public void StartMoving()
        {
            if (float.IsPositiveInfinity(waitTimer))
            {
                waitTimer = startDelay;
            }
        }

        public void StopMoving()
        {
            waitTimer = float.PositiveInfinity;
            currentVelocity = Vector2.zero;
        }

        private Vector2 GetTargetPosition()
        {
            if (targetPoint != null)
            {
                return targetPoint.position;
            }

            return origin + localMoveOffset;
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 start = Application.isPlaying ? (Vector3)origin : transform.position;
            Vector3 end = targetPoint != null
                ? targetPoint.position
                : start + (Vector3)localMoveOffset;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireCube(start, Vector3.one * 0.18f);
            Gizmos.DrawWireCube(end, Vector3.one * 0.18f);
        }
    }
}
