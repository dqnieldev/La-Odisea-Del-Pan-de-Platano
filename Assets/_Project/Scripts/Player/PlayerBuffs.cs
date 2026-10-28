using System;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Ventajas temporales de Ulises que dan los ítems: escudo de hoja de plátano (detiene el siguiente golpe)
    /// y café de olla (corre más rápido). La curación (pescado zarandeado) va directo a <see cref="Health.Heal"/>.
    /// El HUD lee <see cref="ShieldLeft"/> y <see cref="SpeedLeft"/> para dibujar el tiempo que queda.
    /// </summary>
    public class PlayerBuffs : MonoBehaviour, IDamageBlocker
    {
        [SerializeField] private PlayerController controller;
        [Tooltip("Dibujo del escudo sobre Ulises (hijo); se muestra mientras dura.")]
        [SerializeField] private SpriteRenderer shieldVisual;
        [Tooltip("Estela detrás de Ulises mientras corre con el café.")]
        [SerializeField] private SpriteRenderer speedVisual;
        [Tooltip("Último tramo del escudo y del café en que su dibujo parpadea, para avisar que se acaba.")]
        [SerializeField, Min(0f)] private float warningTime = 1.5f;
        [SerializeField] private SoundEffect shieldBreak = new();

        [Header("Tomar café")]
        [Tooltip("Lo que dura el jarrito dibujado por código (solo si Ulises no tiene su animación de tomar café).")]
        [SerializeField, Min(0f)] private float drinkTime = 0.25f;
        [Tooltip("Jarrito que se empina frente a la boca (hijo). Vacío si la animación de Ulises ya lo trae dibujado.")]
        [SerializeField] private SpriteRenderer drinkProp;
        [Tooltip("Dónde va el jarrito, mirando a la derecha (la x se voltea con Ulises).")]
        [SerializeField] private Vector2 drinkPropOffset = new(0.32f, 0.55f);

        private float shieldTimer, shieldDuration;
        private float speedTimer, speedDuration, speedMultiplier = 1f;
        private float drinkTimer;

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

        /// <summary>Empieza o termina un efecto (el HUD muestra u oculta su ícono).</summary>
        public event Action Changed;

        // Solo apaga los dibujos: el controlador puede no haber hecho su Awake todavía (no tiene Rigidbody aún).
        private void Awake()
        {
            if (shieldVisual != null)
                shieldVisual.enabled = false;
            if (speedVisual != null)
                speedVisual.enabled = false;
            if (drinkProp != null)
                drinkProp.enabled = false;
        }

        // Las animaciones de los ítems no detienen a Ulises: si se mueve, el Animator las corta (ArtImportBuilder).

        /// <summary>Se toma el café de un trago y corre más rápido desde ya.</summary>
        public void DrinkCoffee(float multiplier, float seconds)
        {
            ApplySpeed(multiplier, seconds);
            drinkTimer = drinkTime;
            Drank?.Invoke();
        }

        /// <summary>Se come el pescado (solo la animación: la vida la da Health).</summary>
        public void Eat() => Ate?.Invoke();

        private void StopDrink()
        {
            drinkTimer = 0f;
            if (drinkProp != null)
                drinkProp.enabled = false;
        }

        // El jarrito sube a la boca y se empina hacia atrás; un saltito al final, como el último trago.
        // Como la animación dibujada, se corta si Ulises echa a andar o salta.
        private void AnimateDrink()
        {
            if (drinkTimer <= 0f)
                return;
            drinkTimer -= Time.deltaTime;
            bool moving = controller != null && (Mathf.Abs(controller.Velocity.x) > 0.1f || !controller.IsGrounded);
            if (drinkTimer <= 0f || moving)
            {
                StopDrink();
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
            Show(shieldVisual, HasShield, shieldTimer);
            // La estela solo mientras corre: quieto no tiene sentido.
            bool running = controller != null && Mathf.Abs(controller.Velocity.x) > 0.5f;
            Show(speedVisual, HasSpeed && running, speedTimer);
        }

        private void Show(SpriteRenderer visual, bool active, float timer)
        {
            if (visual == null)
                return;
            // Al final parpadea cada vez más rápido.
            bool blinkOff = active && timer < warningTime && Mathf.Repeat(timer * (6f + 6f * (1f - timer / warningTime)), 1f) < 0.35f;
            visual.enabled = active && !blinkOff;
        }
    }
}
