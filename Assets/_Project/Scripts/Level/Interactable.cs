using Odisea.Core;
using Odisea.Player;
using Odisea.UI;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Algo con lo que Ulises interactúa al pararse enfrente (Don Lupe, una puerta): arriba sale un globito con el
    /// botón de interactuar (E / ↑, B o Círculo, según el control) y al presionarlo se llama <see cref="Interact"/>.
    /// Su collider (trigger) es el área donde se puede interactuar.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayers;

        [Header("Globito")]
        [Tooltip("Globito con el botón (se encoge y crece al entrar y salir del área).")]
        [SerializeField] private Transform bubble;
        [SerializeField] private SpriteRenderer bubbleIcon;
        [SerializeField] private Sprite keyboardIcon;
        [SerializeField] private Sprite xboxIcon;
        [SerializeField] private Sprite playStationIcon;

        private PlayerInputReader player;
        private Vector3 bubbleScale = Vector3.one;
        private float bubbleShown;

        /// <summary>Ulises, mientras está en el área.</summary>
        protected PlayerInputReader Player => player;

        protected virtual void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (bubble != null)
            {
                bubbleScale = bubble.localScale;
                bubble.localScale = Vector3.zero;
            }
        }

        private void OnEnable()
        {
            InputDeviceTracker.Changed += ApplyIcon;
            ApplyIcon(InputDeviceTracker.Current);
        }

        private void OnDisable() => InputDeviceTracker.Changed -= ApplyIcon;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
            var reader = other.GetComponentInParent<PlayerInputReader>();
            if (reader == null)
                return;
            player = reader;
            // Lo que se presionó lejos de aquí no cuenta.
            player.ConsumeInteractPressed();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (player != null && other.GetComponentInParent<PlayerInputReader>() == player)
                player = null;
        }

        private void Update()
        {
            bool canInteract = player != null && !DialogueBox.IsOpen && !SceneFlow.IsLoading && Time.timeScale > 0f;
            if (canInteract && player.ConsumeInteractPressed())
                Interact(player);

            AnimateBubble(canInteract);
            WhileNear(player);
        }

        /// <summary>Se presionó interactuar con Ulises enfrente.</summary>
        protected abstract void Interact(PlayerInputReader reader);

        /// <summary>Cada cuadro, con Ulises en el área o null (para voltear hacia él, por ejemplo).</summary>
        protected virtual void WhileNear(PlayerInputReader reader) { }

        // Aparece con un rebote y flota arriba y abajo.
        private void AnimateBubble(bool visible)
        {
            if (bubble == null)
                return;
            bubbleShown = Mathf.MoveTowards(bubbleShown, visible ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            float pop = bubbleShown < 1f && visible ? Mathf.Sin(bubbleShown * Mathf.PI) * 0.25f : 0f;
            bubble.localScale = bubbleScale * (bubbleShown + pop);
            if (bubbleIcon != null)
                bubbleIcon.transform.localPosition = new Vector3(0f, Mathf.Sin(Time.unscaledTime * 4f) * 0.06f, 0f);
        }

        private void ApplyIcon(InputScheme scheme)
        {
            if (bubbleIcon == null)
                return;
            Sprite icon = scheme switch
            {
                InputScheme.Xbox => xboxIcon,
                InputScheme.PlayStation => playStationIcon,
                _ => keyboardIcon,
            };
            if (icon != null)
                bubbleIcon.sprite = icon;
        }
    }
}
