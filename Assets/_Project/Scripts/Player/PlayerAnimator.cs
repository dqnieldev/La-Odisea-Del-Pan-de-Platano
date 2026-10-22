using System.Collections.Generic;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Pasa el estado de Ulises al Animator y hace parpadear el sprite mientras es invulnerable.
    /// Parámetros que puede tener el Animator Controller (los que falten se ignoran):
    /// Speed (float), VerticalSpeed (float), IsGrounded (bool), Attack, DoubleJump, Hurt, Die (triggers).
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private static readonly int IsGroundedId = Animator.StringToHash("IsGrounded");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int DoubleJumpId = Animator.StringToHash("DoubleJump");
        private static readonly int HurtId = Animator.StringToHash("Hurt");
        private static readonly int DieId = Animator.StringToHash("Die");

        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Health health;
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
            if (controller != null) controller.DoubleJumped += OnDoubleJumped;
            if (combat != null) combat.Attacked += OnAttacked;
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (controller != null) controller.DoubleJumped -= OnDoubleJumped;
            if (combat != null) combat.Attacked -= OnAttacked;
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
                SetFloat(VerticalSpeedId, controller.Velocity.y);
                SetBool(IsGroundedId, controller.IsGrounded);
            }

            if (spriteRenderer != null && health != null)
            {
                bool visible = !health.IsInvulnerable || health.IsDead || Mathf.Repeat(Time.time * blinkRate, 1f) > 0.5f;
                spriteRenderer.enabled = visible;
            }
        }

        private void OnDoubleJumped() => SetTrigger(DoubleJumpId);
        private void OnAttacked() => SetTrigger(AttackId);
        private void OnDamaged(DamageInfo _) => SetTrigger(HurtId);
        private void OnDied() => SetTrigger(DieId);

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
