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

        private float shieldTimer, shieldDuration;
        private float speedTimer, speedDuration, speedMultiplier = 1f;

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
        }

        public void ApplyShield(float seconds)
        {
            shieldDuration = shieldTimer = Mathf.Max(0.01f, seconds);
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
