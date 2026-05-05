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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (killOnTriggerEnter)
            {
                Kill(other);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (killOnTriggerStay)
            {
                Kill(other);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (killOnCollisionEnter)
            {
                Kill(collision.collider);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (killOnCollisionStay)
            {
                Kill(collision.collider);
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
