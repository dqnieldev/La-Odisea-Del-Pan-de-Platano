using System;
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

        /// <summary>Le hizo daño a alguien (el golpe sí entró).</summary>
        public event Action Hit;

        private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);

        private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        private void TryDamage(Collider2D other)
        {
            // Unity avisa de los choques aunque el componente esté apagado: apagado = no hace daño
            // (p. ej. el cocodrilo lo apaga mientras muerde, para que solo dañe el cierre del hocico).
            if (!isActiveAndEnabled || (targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target == null)
                return;

            bool applied = target.TakeDamage(new DamageInfo(damage, transform.position, gameObject));
            if (applied)
                Hit?.Invoke();
            if (applied && destroyOnHit)
                Destroy(gameObject);
        }
    }
}
