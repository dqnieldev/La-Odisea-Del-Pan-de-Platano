using System.Text;
using Odisea.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.UI
{
    /// <summary>
    /// Créditos: el texto de Text/Creditos.txt sube despacio por la pantalla. Las líneas con "# " son títulos de
    /// sección (en plátano y más grandes); las que empiezan con "//" se ignoran. Mantener Aceptar los adelanta; Atrás
    /// (o al terminar) regresa al menú.
    /// </summary>
    public class CreditsScreen : MonoBehaviour
    {
        [SerializeField] private TextAsset source;
        [SerializeField] private TMP_Text text;
        [Tooltip("Lo que sube: el texto (su ancla, abajo de la pantalla).")]
        [SerializeField] private RectTransform scroll;
        [SerializeField, Min(1f)] private float speed = 70f;
        [SerializeField, Min(1f)] private float fastSpeed = 320f;
        [SerializeField] private Color sectionColor = new(0.961f, 0.718f, 0.192f);
        [SerializeField, Min(1f)] private float sectionScale = 1.35f;

        private bool leaving;
        private float endY;

        private void Start()
        {
            text.text = Format(source != null ? source.text : "");
            text.ForceMeshUpdate();
            // Termina cuando la última línea salió por arriba.
            var canvas = (RectTransform)scroll.parent;
            endY = text.preferredHeight + canvas.rect.height;
            scroll.anchoredPosition = Vector2.zero;
        }

        private string Format(string raw)
        {
            string color = ColorUtility.ToHtmlStringRGB(sectionColor);
            var builder = new StringBuilder();
            foreach (string rawLine in raw.Replace("\r", "").Split('\n'))
            {
                string line = rawLine.TrimEnd();
                if (line.StartsWith("//"))
                    continue;
                line = line.Replace("{version}", Application.version);
                if (line.StartsWith("# "))
                    builder.Append($"\n<size={sectionScale * 100f:0}%><color=#{color}>{line.Substring(2)}</color></size>\n");
                else
                    builder.Append(line).Append('\n');
            }
            return builder.ToString().Trim('\n');
        }

        private void Update()
        {
            if (leaving || SceneFlow.IsLoading)
                return;
            if (BackPressed())
            {
                Leave();
                return;
            }
            float current = HoldingFast() ? fastSpeed : speed;
            scroll.anchoredPosition += Vector2.up * current * Time.deltaTime;
            if (scroll.anchoredPosition.y >= endY)
                Leave();
        }

        private void Leave()
        {
            leaving = true;
            UiAudio.Play(UiSound.Back);
            SceneFlow.Load(SceneFlow.MainMenu);
        }

        private static bool BackPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                   (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
        }

        private static bool HoldingFast()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && (keyboard.enterKey.isPressed || keyboard.spaceKey.isPressed)) ||
                   (gamepad != null && gamepad.buttonSouth.isPressed) ||
                   (mouse != null && mouse.leftButton.isPressed);
        }
    }
}
