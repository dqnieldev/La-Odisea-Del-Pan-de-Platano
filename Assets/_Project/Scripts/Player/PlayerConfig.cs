using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Valores de ajuste de Ulises. Diseño puede cambiarlos en el Inspector sin tocar código
    /// (Create > Odisea > Configuración del jugador).
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Odisea/Configuración del jugador")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movimiento horizontal")]
        [Min(0f)] public float moveSpeed = 8f;
        [Tooltip("Cuánto hay que inclinar el stick para caminar (0-1). Pasándolo corre a toda velocidad, aunque vaya " +
                 "en diagonal: así W+D o el stick un poco arriba no lo hacen más lento.")]
        [Range(0f, 0.9f)] public float moveDeadzone = 0.3f;
        [Tooltip("Qué tan rápido alcanza la velocidad máxima (unidades/s²).")]
        [Min(0f)] public float acceleration = 70f;
        [Tooltip("Qué tan rápido frena al soltar la dirección (unidades/s²).")]
        [Min(0f)] public float deceleration = 90f;
        [Tooltip("Multiplica aceleración y frenado en el aire (1 = igual que en el suelo).")]
        [Range(0f, 1f)] public float airControl = 0.75f;

        [Header("Salto")]
        [Tooltip("Altura del primer salto, en unidades.")]
        [Min(0f)] public float jumpHeight = 3.2f;
        [Tooltip("Altura del doble salto, en unidades.")]
        [Min(0f)] public float doubleJumpHeight = 2.4f;
        [Tooltip("Saltos extra en el aire (1 = doble salto, 0 = sin doble salto).")]
        [Min(0)] public int extraJumps = 1;
        [Tooltip("Tiempo para saltar después de salir de una orilla.")]
        [Min(0f)] public float coyoteTime = 0.1f;
        [Tooltip("Tiempo que se recuerda el botón de salto pulsado justo antes de tocar el suelo.")]
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Header("Gravedad")]
        [Min(0f)] public float gravityScale = 3f;
        [Tooltip("Gravedad extra al caer, para que el salto se sienta firme.")]
        [Min(1f)] public float fallGravityMultiplier = 1.7f;
        [Tooltip("Gravedad extra al soltar el salto antes de tiempo (salto corto).")]
        [Min(1f)] public float jumpCutGravityMultiplier = 2.5f;
        [Min(0f)] public float maxFallSpeed = 20f;

        [Header("Escaleras")]
        [Tooltip("Velocidad al trepar, en unidades por segundo.")]
        [Min(0f)] public float climbSpeed = 4.5f;
        [Tooltip("Qué tanto hay que inclinar arriba/abajo (0-1) para agarrar la escalera.")]
        [Range(0.1f, 1f)] public float climbInputThreshold = 0.5f;
        [Tooltip("Qué tan rápido se centra Ulises en la escalera al agarrarla.")]
        [Min(0f)] public float ladderSnapSpeed = 12f;
        public LayerMask ladderLayers;

        [Header("Detección de suelo")]
        public LayerMask groundLayers;
        public Vector2 groundCheckSize = new(0.7f, 0.1f);

        [Header("Golpe con el rodillo")]
        [Min(1)] public int attackDamage = 1;
        [Min(0f)] public float attackCooldown = 0.35f;
        [Tooltip("Centro del área de golpe, relativo a Ulises mirando a la derecha.")]
        public Vector2 attackOffset = new(0.8f, 0.1f);
        public Vector2 attackSize = new(1.1f, 1f);
        public LayerMask attackLayers;

        [Header("Lanzar bolita de masa (proyectil)")]
        [Min(1)] public int projectileDamage = 1;
        [Tooltip("Velocidad del proyectil. Bajo a propósito: es un ataque lento.")]
        [Min(0.1f)] public float projectileSpeed = 6f;
        [Min(0f)] public float projectileCooldown = 0.7f;
        [Tooltip("Segundos antes de que el proyectil desaparezca si no choca con nada.")]
        [Min(0.1f)] public float projectileLifetime = 2.5f;
        [Tooltip("Dónde aparece, relativo a Ulises mirando a la derecha.")]
        public Vector2 projectileSpawnOffset = new(0.6f, 0.3f);

        [Header("Al recibir daño")]
        [Tooltip("Empuje al recibir daño: x = hacia atrás, y = hacia arriba.")]
        public Vector2 hurtKnockback = new(6f, 7f);
        [Tooltip("Segundos sin control después de recibir daño.")]
        [Min(0f)] public float hurtControlLock = 0.25f;

        /// <summary>Velocidad vertical necesaria para alcanzar <paramref name="height"/> con la gravedad actual.</summary>
        public float JumpVelocityFor(float height) =>
            Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * gravityScale * height);
    }
}
