using UnityEngine;

namespace Odisea.Core
{
    /// <summary>Datos de un golpe: cuánto daño hace y de dónde viene (para empujar al herido).</summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly Vector2 SourcePosition;
        public readonly GameObject Source;

        public DamageInfo(int amount, Vector2 sourcePosition, GameObject source)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            Source = source;
        }
    }

    /// <summary>Cualquier cosa que puede recibir daño: Ulises, enemigos, objetos rompibles.</summary>
    public interface IDamageable
    {
        /// <returns>true si el daño se aplicó (false si era invulnerable o ya estaba muerto).</returns>
        bool TakeDamage(DamageInfo info);
    }
}
