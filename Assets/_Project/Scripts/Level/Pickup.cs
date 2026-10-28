using Odisea.Core;
using Odisea.Player;
using TMPro;
using UnityEngine;

namespace Odisea.Level
{
    public enum PickupKind
    {
        /// <summary>Pescado zarandeado: recupera vida.</summary>
        Heal,
        /// <summary>Hoja de plátano: escudo que detiene el siguiente golpe.</summary>
        Shield,
        /// <summary>Café de olla: corre más rápido un rato.</summary>
        Speed,
    }

    /// <summary>
    /// Ítem que flota en el nivel y Ulises recoge al tocarlo: cura o da una ventaja (<see cref="PlayerBuffs"/>).
    /// Al recogerlo suena, sale su nombre flotando y desaparece. La curación no se gasta con la vida llena:
    /// se queda ahí para cuando haga falta. Al morir, la escena se recarga y los ítems vuelven.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupKind kind;
        [Tooltip("Curación: vidas que recupera. Velocidad: multiplicador (1.5 = 50 % más rápido).")]
        [SerializeField, Min(0f)] private float amount = 1f;
        [Tooltip("Segundos que dura el escudo o la velocidad.")]
        [SerializeField, Min(0f)] private float duration = 8f;
        [SerializeField] private LayerMask playerLayers;
        [Tooltip("El dibujo (hijo): flota arriba y abajo.")]
        [SerializeField] private Transform visual;
        [SerializeField] private SoundEffect sound = new();

        [Header("Texto al recogerlo")]
        [SerializeField] private string label = "+1 vida";
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Color labelColor = new(0.169f, 0.118f, 0.086f);

        [Header("Flotar")]
        [SerializeField, Min(0f)] private float bobHeight = 0.12f;
        [SerializeField, Min(0f)] private float bobSpeed = 2.2f;

        private Vector3 visualRest;
        private bool taken;

        public PickupKind Kind => kind;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (visual != null)
                visualRest = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null)
                return;
            // Cada ítem con su propio ritmo (según dónde está), para que no floten todos al mismo tiempo.
            float phase = Time.time * bobSpeed + transform.position.x;
            visual.localPosition = visualRest + Vector3.up * (Mathf.Sin(phase) * bobHeight);
        }

        // Stay y no Enter: la curación que no se pudo tomar con vida llena se toma al perder una sin salir de ahí.
        private void OnTriggerStay2D(Collider2D other)
        {
            if (taken || (playerLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
            var health = other.GetComponentInParent<Health>();
            if (health == null || health.IsDead || !Apply(health))
                return;

            taken = true;
            sound.Play();
            ShowLabel();
            Destroy(gameObject);
        }

        private bool Apply(Health health)
        {
            if (kind == PickupKind.Heal)
            {
                if (health.Current >= health.Max)
                    return false;
                health.Heal(Mathf.Max(1, Mathf.RoundToInt(amount)));
                return true;
            }

            var buffs = health.GetComponent<PlayerBuffs>();
            if (buffs == null)
                return false;
            if (kind == PickupKind.Shield)
                buffs.ApplyShield(duration);
            else
                buffs.ApplySpeed(amount, duration);
            return true;
        }

        private void ShowLabel()
        {
            if (string.IsNullOrEmpty(label) || font == null)
                return;
            var go = new GameObject($"Texto {label}", typeof(RectTransform), typeof(TextMeshPro));
            go.transform.position = (visual != null ? visual.position : transform.position) + Vector3.up * 0.6f;
            var text = go.GetComponent<TextMeshPro>();
            text.text = label;
            text.font = font;
            text.fontSize = 5f;
            text.color = labelColor;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.sortingOrder = 20;
            go.AddComponent<FloatingLabel>();
        }
    }
}
