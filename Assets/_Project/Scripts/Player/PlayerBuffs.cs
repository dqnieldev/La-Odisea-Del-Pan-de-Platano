using System;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Ventajas temporales de Ulises que dan los ítems: escudo de hoja de plátano (detiene el siguiente golpe)
    /// y café de olla (corre más rápido). La curación (pescado zarandeado) va directo a <see cref="Health.Heal"/>.
    /// El HUD lee <see cref="ShieldLeft"/> y <see cref="SpeedLeft"/> para dibujar el tiempo que queda; los efectos
    /// (<see cref="ShieldLeaves"/>, <see cref="SpeedTrail"/>) leen <see cref="ShieldShown"/> y <see cref="SpeedShown"/>.
    /// </summary>
    public class PlayerBuffs : MonoBehaviour, IDamageBlocker
    {
        [SerializeField] private PlayerController controller;
        [Tooltip("Último tramo del escudo y del café en que su efecto parpadea, para avisar que se acaba.")]
        [SerializeField, Min(0f)] private float warningTime = 1.5f;
        [SerializeField] private SoundEffect shieldBreak = new();

        [Header("Tomar café")]
        [Tooltip("Lo que tarda en tomárselo: se detiene y la velocidad empieza al terminar.")]
        [SerializeField, Min(0f)] private float drinkTime = 0.25f;
        [Tooltip("Jarrito que se empina frente a la boca (hijo). Vacío si la animación de Ulises ya lo trae dibujado.")]
        [SerializeField] private SpriteRenderer drinkProp;
        [Tooltip("Dónde va el jarrito, mirando a la derecha (la x se voltea con Ulises).")]
        [SerializeField] private Vector2 drinkPropOffset = new(0.32f, 0.55f);

        [Header("Comer y ponerse la hoja")]
        [Tooltip("Pausa al comerse el pescado (animación \"Eat\"). 0 si Ulises no tiene esa animación dibujada.")]
        [SerializeField, Min(0f)] private float eatTime = 0.25f;
        [Tooltip("Pausa al ponerse la hoja de escudo (animación \"Leaf\"). 0 si no está dibujada.")]
        [SerializeField, Min(0f)] private float leafTime = 0.25f;

        private float shieldTimer, shieldDuration;
        private float speedTimer, speedDuration, speedMultiplier = 1f;
        private float drinkTimer, pendingMultiplier, pendingDuration;

        /// <summary>Empieza a tomar café (PlayerAnimator dispara la animación "Drink", si existe).</summary>
        public event Action Drank;
        /// <summary>Se come el pescado (PlayerAnimator dispara "Eat").</summary>
        public event Action Ate;
        /// <summary>Se pone la hoja de escudo (PlayerAnimator dispara "Leaf").</summary>
        public event Action Shielded;

        public bool HasShield => shieldTimer > 0f;
        public bool HasSpeed => speedTimer > 0f;
        /// <summary>Fracción que queda del escudo (1 = recién tomado, 0 = sin escudo).</summary>
        public float ShieldLeft => HasShield ? shieldTimer / shieldDuration : 0f;
        public float SpeedLeft => HasSpeed ? speedTimer / speedDuration : 0f;
        /// <summary>El efecto del escudo se dibuja en este cuadro (al final parpadea).</summary>
        public bool ShieldShown => HasShield && !BlinkOff(shieldTimer);
        public bool SpeedShown => HasSpeed && !BlinkOff(speedTimer);

        /// <summary>El escudo detuvo un golpe y se rompió (las hojitas salen volando).</summary>
        public event Action ShieldBroken;

        /// <summary>Empieza o termina un efecto (el HUD muestra u oculta su ícono).</summary>
        public event Action Changed;

        // Solo apaga el jarrito: el controlador puede no haber hecho su Awake todavía (no tiene Rigidbody aún).
        private void Awake()
        {
            if (drinkProp != null)
                drinkProp.enabled = false;
        }

        /// <summary>Se detiene a tomarse el café de un trago; al terminar, corre más rápido.</summary>
        public void DrinkCoffee(float multiplier, float seconds)
        {
            pendingMultiplier = multiplier;
            pendingDuration = seconds;
            drinkTimer = drinkTime;
            if (controller != null)
                controller.HoldStill(drinkTime);
            Drank?.Invoke();
            if (drinkTime <= 0f)
                FinishDrink();
        }

        /// <summary>Se come el pescado de un bocado: una pausa más corta que la del café.</summary>
        public void Eat()
        {
            if (controller != null && eatTime > 0f)
                controller.HoldStill(eatTime);
            Ate?.Invoke();
        }

        private void FinishDrink()
        {
            drinkTimer = 0f;
            if (drinkProp != null)
                drinkProp.enabled = false;
            ApplySpeed(pendingMultiplier, pendingDuration);
        }

        // El jarrito sube a la boca y se empina hacia atrás; un saltito al final, como el último trago.
        private void AnimateDrink()
        {
            if (drinkTimer <= 0f)
                return;
            drinkTimer -= Time.deltaTime;
            if (drinkTimer <= 0f)
            {
                FinishDrink();
                return;
            }
            if (drinkProp == null)
                return;
            float t = 1f - drinkTimer / drinkTime;
            int facing = controller != null ? controller.FacingDirection : 1;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
            float tilt = Mathf.SmoothStep(0f, 75f, Mathf.Clamp01((t - 0.2f) / 0.5f));
            float gulp = t > 0.75f ? Mathf.Sin((t - 0.75f) / 0.25f * Mathf.PI) * 0.06f : 0f;
            drinkProp.enabled = true;
            drinkProp.flipX = facing < 0;
            drinkProp.transform.localPosition = new Vector3(drinkPropOffset.x * facing, drinkPropOffset.y - 0.35f * (1f - rise) + gulp, 0f);
            drinkProp.transform.localRotation = Quaternion.Euler(0f, 0f, tilt * facing);
        }

        public void ApplyShield(float seconds)
        {
            shieldDuration = shieldTimer = Mathf.Max(0.01f, seconds);
            if (controller != null && leafTime > 0f)
                controller.HoldStill(leafTime);
            Shielded?.Invoke();
            Refresh();
            Changed?.Invoke();
        }

        public void ApplySpeed(float multiplier, float seconds)
        {
            speedMultiplier = multiplier;
            speedDuration = speedTimer = Mathf.Max(0.01f, seconds);
            Refresh();
            Changed?.Invoke();
        }

        public bool TryBlock(DamageInfo info)
        {
            if (!HasShield)
                return false;
            shieldTimer = 0f;
            shieldBreak.Play();
            Refresh();
            ShieldBroken?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private void Update()
        {
            AnimateDrink();
            bool ended = Tick(ref shieldTimer) | Tick(ref speedTimer);
            Refresh();
            if (ended)
                Changed?.Invoke();
        }

        // true si el efecto se acabó en este cuadro.
        private static bool Tick(ref float timer)
        {
            if (timer <= 0f)
                return false;
            timer -= Time.deltaTime;
            return timer <= 0f;
        }

        private void Refresh()
        {
            if (controller != null)
                controller.SpeedMultiplier = HasSpeed ? speedMultiplier : 1f;
        }

        // Al final parpadea cada vez más rápido.
        private bool BlinkOff(float timer) =>
            timer < warningTime && Mathf.Repeat(timer * (6f + 6f * (1f - timer / warningTime)), 1f) < 0.35f;
    }
}
