using System;
using System.Collections;
using Odisea.Core;
using Odisea.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.UI
{
    /// <summary>
    /// Cuadro de diálogo de abajo: el nombre de quien habla en una cinta y su línea, que se escribe letra por letra.
    /// Interactuar (o Aceptar, Espacio, clic) termina de escribir la línea o pasa a la siguiente. Mientras está
    /// abierto, Ulises no se mueve. Hay uno por escena; se usa con <see cref="Say"/>.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text speakerName;
        [SerializeField] private TMP_Text body;
        [Tooltip("Flechita de \"sigue\" (parpadea cuando la línea ya se escribió completa).")]
        [SerializeField] private RectTransform continueMark;
        [SerializeField, Min(1f)] private float charactersPerSecond = 45f;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.2f;
        [Tooltip("Sonidito de cada pocas letras.")]
        [SerializeField] private SoundEffect blip = new();
        [SerializeField, Min(1)] private int lettersPerBlip = 3;
        [SerializeField] private SoundEffect advance = new();

        private static DialogueBox current;

        private string[] lines = Array.Empty<string>();
        private int lineIndex;
        private float typed;
        private int openedFrame;
        private PlayerInputReader player;
        private Action closed;
        private bool open;
        private float openTime;
        private Vector2 continueMarkHome;

        /// <summary>Hay un diálogo en pantalla.</summary>
        public static bool IsOpen => current != null && current.open;

        private void Awake()
        {
            current = this;
            group.alpha = 0f;
            card.localScale = Vector3.zero;
            if (continueMark != null)
                continueMarkHome = continueMark.anchoredPosition;
        }

        private void OnDestroy()
        {
            if (current == this)
                current = null;
        }

        /// <summary>Muestra las líneas de alguien, una tras otra; Ulises (si se pasa) se queda quieto mientras.</summary>
        public static void Say(string speaker, string[] lines, PlayerInputReader player = null, Action onClosed = null)
        {
            if (current == null || lines == null || lines.Length == 0)
            {
                onClosed?.Invoke();
                return;
            }
            current.Open(speaker, lines, player, onClosed);
        }

        private void Open(string speaker, string[] newLines, PlayerInputReader reader, Action onClosed)
        {
            speakerName.text = speaker;
            lines = newLines;
            lineIndex = 0;
            player = reader;
            closed = onClosed;
            if (player != null)
                player.InputLocked = true;
            open = true;
            openTime = Time.unscaledTime;
            openedFrame = Time.frameCount;
            ShowLine();
        }

        private void ShowLine()
        {
            typed = 0f;
            body.text = lines[lineIndex];
            body.maxVisibleCharacters = 0;
            body.ForceMeshUpdate();
        }

        private void Update()
        {
            Animate();
            if (!open || Time.timeScale == 0f)
                return;

            int total = body.textInfo.characterCount;
            int before = Mathf.FloorToInt(typed);
            if (before < total)
            {
                typed = Mathf.Min(total, typed + charactersPerSecond * Time.unscaledDeltaTime);
                int now = Mathf.FloorToInt(typed);
                body.maxVisibleCharacters = now;
                if (now / lettersPerBlip != before / lettersPerBlip)
                    blip.Play();
            }

            // La misma pulsación que abrió el diálogo no lo avanza.
            bool pressed = (player != null && player.ConsumeInteractPressed()) | ConfirmPressed();
            if (!pressed || Time.frameCount == openedFrame)
                return;
            if (typed < total)
            {
                typed = total;
                body.maxVisibleCharacters = total;
                return;
            }
            advance.Play();
            if (++lineIndex < lines.Length)
                ShowLine();
            else
                Close();
        }

        private void Close()
        {
            open = false;
            Action onClosed = closed;
            closed = null;
            if (player != null)
                StartCoroutine(Unlock(player));
            player = null;
            onClosed?.Invoke();
        }

        // Un cuadro después: el Espacio con que se cerró no debe hacer saltar a Ulises.
        private static IEnumerator Unlock(PlayerInputReader reader)
        {
            yield return null;
            if (reader != null && !IsOpen)
                reader.InputLocked = false;
        }

        // Entra con un rebote y sale encogiéndose; la flechita parpadea cuando ya se puede pasar.
        private void Animate()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - openTime) / appearDuration);
            float scale = open ? 0.85f + 0.15f * t + Mathf.Sin(t * Mathf.PI) * 0.05f
                : Mathf.MoveTowards(card.localScale.x, 0f, Time.unscaledDeltaTime * 6f);
            card.localScale = new Vector3(scale, scale, 1f);
            group.alpha = Mathf.MoveTowards(group.alpha, open ? 1f : 0f, Time.unscaledDeltaTime * 8f);
            if (continueMark != null)
            {
                bool done = open && typed >= body.textInfo.characterCount;
                continueMark.gameObject.SetActive(done);
                continueMark.anchoredPosition = continueMarkHome + Vector2.up * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 8f;
            }
        }

        private static bool ConfirmPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                                         keyboard.spaceKey.wasPressedThisFrame)) ||
                   (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) ||
                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
    }
}
