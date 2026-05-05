using UnityEngine;

namespace GameLab.Core
{
    public sealed class RespawnPoint : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.85f, 1f, 0.9f);
        [SerializeField, Min(0.05f)] private float gizmoRadius = 0.35f;

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.right * gizmoRadius * 1.5f);
        }
    }
}
