using GameLab.Player;
using UnityEngine;

namespace GameLab.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DeathZone : MonoBehaviour
    {
        [SerializeField] private bool killOnStay = true;

        private void Reset()
        {
            Collider2D zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            Collider2D zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider != null && !zoneCollider.isTrigger)
            {
                Debug.LogWarning("DeathZone should use a trigger Collider2D.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Kill(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (killOnStay)
            {
                Kill(other);
            }
        }

        private void Kill(Component other)
        {
            PlayerDeathHandler deathHandler = other.GetComponentInParent<PlayerDeathHandler>();
            if (deathHandler != null)
            {
                deathHandler.Die(this);
            }
        }
    }
}
