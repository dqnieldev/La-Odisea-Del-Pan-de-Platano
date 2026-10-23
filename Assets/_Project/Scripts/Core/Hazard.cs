using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Hace daño a lo que lo toca: espinas, cocodrilos, cáscaras, proyectiles.
    /// Funciona con colliders sólidos o triggers.
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;
        [Tooltip("Solo daña objetos en estas capas.")]
        [SerializeField] private LayerMask targetLayers = ~0;
        [Tooltip("Destruye este objeto al dañar a alguien (proyectiles).")]
        [SerializeField] private bool destroyOnHit;

        private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);

        private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        private void TryDamage(Collider2D other)
        {
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target == null)
                return;

            bool applied = target.TakeDamage(new DamageInfo(damage, transform.position, gameObject));
            if (applied && destroyOnHit)
                Destroy(gameObject);
        }
    }
}
