using System.Collections.Generic;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Pasa el estado de Ulises al Animator y hace parpadear el sprite mientras es invulnerable.
    /// Parámetros que puede tener el Animator Controller (los que falten se ignoran):
    /// Speed (float), VerticalSpeed (float), RunSpeed (float: velocidad de la animación de correr, sube con el café), ClimbSpeed (float: 1 si sube o baja por la escalera, 0 si está quieto),
    /// IsGrounded (bool), IsClimbing (bool),
    /// Attack, Throw, DoubleJump, Hurt, Die, Drink, Eat, Leaf (triggers; Drink al tomar café, Eat al comer el pescado,
    /// Leaf al ponerse la hoja de escudo), Slip (trigger: se resbaló con una cáscara).
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private static readonly int ClimbSpeedId = Animator.StringToHash("ClimbSpeed");
        private static readonly int RunSpeedId = Animator.StringToHash("RunSpeed");
        private static readonly int IsGroundedId = Animator.StringToHash("IsGrounded");
        private static readonly int IsClimbingId = Animator.StringToHash("IsClimbing");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int ThrowId = Animator.StringToHash("Throw");
        private static readonly int DoubleJumpId = Animator.StringToHash("DoubleJump");
        private static readonly int HurtId = Animator.StringToHash("Hurt");
        private static readonly int DieId = Animator.StringToHash("Die");
        private static readonly int DrinkId = Animator.StringToHash("Drink");
        private static readonly int EatId = Animator.StringToHash("Eat");
        private static readonly int LeafId = Animator.StringToHash("Leaf");
        private static readonly int SlipId = Animator.StringToHash("Slip");

        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Health health;
        [Tooltip("Opcional: para las animaciones de los ítems (triggers Drink, Eat y Leaf).")]
        [SerializeField] private PlayerBuffs buffs;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Parpadeos por segundo mientras Ulises es invulnerable.")]
        [SerializeField, Min(0f)] private float blinkRate = 12f;

        private readonly HashSet<int> availableParameters = new();

        private void Awake()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                availableParameters.Add(parameter.nameHash);
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.DoubleJumped += OnDoubleJumped;
                controller.Slipped += OnSlipped;
            }
            if (buffs != null)
            {
                buffs.Drank += OnDrank;
                buffs.Ate += OnAte;
                buffs.Shielded += OnShielded;
            }
            if (combat != null)
            {
                combat.Attacked += OnAttacked;
                combat.Thrown += OnThrown;
            }
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.DoubleJumped -= OnDoubleJumped;
                controller.Slipped -= OnSlipped;
            }
            if (buffs != null)
            {
                buffs.Drank -= OnDrank;
                buffs.Ate -= OnAte;
                buffs.Shielded -= OnShielded;
            }
            if (combat != null)
            {
                combat.Attacked -= OnAttacked;
                combat.Thrown -= OnThrown;
            }
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (controller != null)
            {
                SetFloat(SpeedId, Mathf.Abs(controller.Velocity.x));
                SetFloat(RunSpeedId, controller.SpeedMultiplier);
                SetFloat(VerticalSpeedId, controller.Velocity.y);
                SetBool(IsGroundedId, controller.IsGrounded);
                SetBool(IsClimbingId, controller.IsClimbing);
                SetFloat(ClimbSpeedId, controller.IsClimbing && Mathf.Abs(controller.Velocity.y) > 0.05f ? 1f : 0f);
            }

            if (spriteRenderer != null && health != null)
            {
                bool visible = !health.IsInvulnerable || health.IsDead || Mathf.Repeat(Time.time * blinkRate, 1f) > 0.5f;
                spriteRenderer.enabled = visible;
            }
        }

        private void OnDoubleJumped() => SetTrigger(DoubleJumpId);
        private void OnSlipped() => SetTrigger(SlipId);
        private void OnAttacked() => SetTrigger(AttackId);
        private void OnThrown() => SetTrigger(ThrowId);
        private void OnDrank() => SetTrigger(DrinkId);
        private void OnAte() => SetTrigger(EatId);
        private void OnShielded() => SetTrigger(LeafId);
        private void OnDamaged(DamageInfo _) => SetTrigger(HurtId);
        private void OnDied()
        {
            // El último golpe también avisa "daño": se cancela para que no interrumpa la muerte.
            if (availableParameters.Contains(HurtId))
                animator.ResetTrigger(HurtId);
            SetTrigger(DieId);
        }

        private void SetFloat(int id, float value)
        {
            if (availableParameters.Contains(id)) animator.SetFloat(id, value);
        }

        private void SetBool(int id, bool value)
        {
            if (availableParameters.Contains(id)) animator.SetBool(id, value);
        }

        private void SetTrigger(int id)
        {
            if (availableParameters.Contains(id)) animator.SetTrigger(id);
        }
    }
}
