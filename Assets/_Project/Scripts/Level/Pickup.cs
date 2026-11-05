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
        /// <summary>Ingrediente del pan de plátano: suma al puntaje del nivel (<see cref="LevelScore"/>).</summary>
        Ingredient,
    }

    /// <summary>
    /// Ítem que flota en el nivel y Ulises recoge al tocarlo: cura, da una ventaja (<see cref="PlayerBuffs"/>) o es un
    /// ingrediente que suma al puntaje.
    /// Al recogerlo suena (cada ítem con su sonido), sale su nombre flotando y desaparece. La curación con la vida
    /// llena da un corazón extra (hasta <see cref="maxHearts"/>). Al morir, la escena se recarga y los ítems vuelven.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupKind kind;
        [Tooltip("Curación: vidas que recupera. Velocidad: multiplicador (1.5 = 50 % más rápido).")]
        [SerializeField, Min(0f)] private float amount = 1f;
        [Tooltip("Segundos que dura el escudo o la velocidad.")]
        [SerializeField, Min(0f)] private float duration = 8f;
        [Tooltip("Curación con la vida llena: suma un corazón hasta este máximo; ya en el máximo, no se recoge.")]
        [SerializeField, Min(1)] private int maxHearts = 5;
        [SerializeField] private LayerMask playerLayers;
        [Tooltip("El dibujo (hijo): flota arriba y abajo.")]
        [SerializeField] private Transform visual;
        [SerializeField] private SoundEffect sound = new();

        [Header("Texto al recogerlo")]
        [SerializeField] private string label = "+1 vida";
        [Tooltip("Curación con la vida llena (corazón nuevo).")]
        [SerializeField] private string extraHeartLabel = "¡Corazón extra!";
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Color labelColor = new(0.169f, 0.118f, 0.086f);

        [Header("Ingrediente")]
        [Tooltip("Rayos detrás del dibujo que giran (solo los ingredientes los tienen).")]
        [SerializeField] private Transform rays;
        [SerializeField] private float raySpeed = 45f;
        [Tooltip("Brillo que late (crece y se encoge).")]
        [SerializeField] private Transform pulse;
        [Tooltip("Al recogerlo: el dibujo salta y se desvanece y salen destellos (PickupBurst).")]
        [SerializeField] private SpriteRenderer burstDrawing;
        [SerializeField] private Sprite sparkle;
        [SerializeField] private Color sparkleColor = new(1f, 0.82f, 0.25f);
        [Tooltip("Ingredientes tomados uno tras otro (con menos de estos segundos entre sí) suenan cada vez más agudo.")]
        [SerializeField, Min(0f)] private float comboWindow = 2.5f;
        [SerializeField, Min(0f)] private float comboPitchStep = 0.06f;
        [SerializeField, Min(0)] private int comboMaxSteps = 6;

        // Racha de ingredientes (compartida por todos).
        private static float lastIngredientTime = -100f;
        private static int combo;

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
            if (rays != null)
                rays.Rotate(0f, 0f, -raySpeed * Time.deltaTime);
            if (pulse != null)
                pulse.localScale = Vector3.one * (1.35f + Mathf.Sin(Time.time * 4f + transform.position.x) * 0.12f);
        }

        // Stay y no Enter: la curación que no se pudo tomar (ya con todos los corazones) se toma al perder una vida
        // sin salir de ahí.
        private void OnTriggerStay2D(Collider2D other)
        {
            if (taken || (playerLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
            var health = other.GetComponentInParent<Health>();
            if (health == null || health.IsDead || !Apply(health, out string shown))
                return;

            taken = true;
            PlaySound();
            if (burstDrawing != null)
                PickupBurst.Spawn(burstDrawing, sparkle, sparkleColor);
            ShowLabel(shown);
            Destroy(gameObject);
        }

        private void PlaySound()
        {
            if (kind != PickupKind.Ingredient)
            {
                sound.Play();
                return;
            }
            combo = Time.time - lastIngredientTime <= comboWindow ? Mathf.Min(combo + 1, comboMaxSteps) : 0;
            lastIngredientTime = Time.time;
            GameAudio.Play(sound, 0f, 1f + combo * comboPitchStep);
        }

        private bool Apply(Health health, out string shown)
        {
            shown = label;
            if (kind == PickupKind.Ingredient)
            {
                if (LevelScore.Current != null)
                    LevelScore.Current.CollectIngredient(this);
                return true;
            }
            var buffs = health.GetComponent<PlayerBuffs>();
            if (kind == PickupKind.Heal)
            {
                int hearts = Mathf.Max(1, Mathf.RoundToInt(amount));
                bool eaten = true;
                if (health.Current < health.Max)
                    health.Heal(hearts);
                else
                {
                    shown = extraHeartLabel;
                    eaten = health.IncreaseMax(hearts, maxHearts);
                }
                if (eaten && buffs != null)
                    buffs.Eat();
                return eaten;
            }

            if (buffs == null)
                return false;
            if (kind == PickupKind.Shield)
                buffs.ApplyShield(duration);
            else
                buffs.DrinkCoffee(amount, duration);
            return true;
        }

        private void ShowLabel(string shown)
        {
            if (string.IsNullOrEmpty(shown) || font == null)
                return;
            var go = new GameObject($"Texto {shown}", typeof(RectTransform), typeof(TextMeshPro));
            go.transform.position = (visual != null ? visual.position : transform.position) + Vector3.up * 0.6f;
            var text = go.GetComponent<TextMeshPro>();
            text.text = shown;
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
