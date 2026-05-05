using UnityEngine;

namespace GameLab.Cameras
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset;
        [SerializeField] private float smoothTime = 0.15f;

        [Header("Bounds")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 minPosition;
        [SerializeField] private Vector2 maxPosition;

        private Vector3 velocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z);

            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minPosition.x, maxPosition.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minPosition.y, maxPosition.y);
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }

        private void OnValidate()
        {
            smoothTime = Mathf.Max(0.01f, smoothTime);
        }
    }
}
