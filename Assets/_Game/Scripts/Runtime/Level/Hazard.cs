using GameLab.Player;
using UnityEngine;

namespace GameLab.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hazard : MonoBehaviour
    {
        [SerializeField] private bool killOnTriggerEnter = true;
        [SerializeField] private bool killOnTriggerStay = true;
        [SerializeField] private bool killOnCollisionEnter = true;
        [SerializeField] private bool killOnCollisionStay;
        [SerializeField] private bool deactivateAfterKillingPlayer;

        private bool isSpent;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (killOnTriggerEnter)
            {
                TryKill(other);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (killOnTriggerStay)
            {
                TryKill(other);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (killOnCollisionEnter)
            {
                TryKill(collision.collider);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (killOnCollisionStay)
            {
                TryKill(collision.collider);
            }
        }

        public bool TryKill(Component other)
        {
            if (isSpent)
            {
                return false;
            }

            PlayerDeathHandler deathHandler = other.GetComponentInParent<PlayerDeathHandler>();
            if (deathHandler == null || !deathHandler.TryDie(this))
            {
                return false;
            }

            if (deactivateAfterKillingPlayer)
            {
                isSpent = true;
                gameObject.SetActive(false);
            }

            return true;
        }
    }
}
