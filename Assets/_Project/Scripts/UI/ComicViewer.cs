using System.Collections;
using System.Linq;
using Odisea.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Escena de historietas: muestra la que se pidió con <see cref="SceneFlow.PlayStory"/> una viñeta por pantalla,
    /// grande, sobre una hoja de papel viejo (como Cuphead), con su texto debajo. Aceptar pasa a la siguiente (la
    /// actual se va y la nueva llega con un rebote); después de la última carga la escena que sigue. Atrás o Start
    /// la salta.
    /// </summary>
    public class ComicViewer : MonoBehaviour
    {
        [Tooltip("Todas las historietas del juego; se busca la pedida por el nombre del asset.")]
        [SerializeField] private ComicSequence[] stories;
        [Tooltip("La que se muestra si se abre esta escena directo (pruebas en el editor).")]
        [SerializeField] private ComicSequence fallback;
        [SerializeField] private string fallbackNextScene = SceneFlow.MainMenu;

        [Header("Página")]
        [Tooltip("La viñeta (marco, dibujo y texto); es lo que entra y sale.")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Image art;
        [Tooltip("Descripción de lo que va dibujado (solo mientras no hay dibujo).")]
        [SerializeField] private TMP_Text placeholder;
        [SerializeField] private TMP_Text caption;
        [Tooltip("Título de la historieta y \"1 / 4\".")]
        [SerializeField] private TMP_Text counter;
        [Tooltip("Ayuda de botones; dice \"Siguiente\" y en la última viñeta \"Continuar\".")]
        [SerializeField] private TMP_Text nextLabel;

        [Header("Tiempos")]
        [Tooltip("Pausa antes de la primera viñeta (que no se salte con el botón de Jugar).")]
        [SerializeField, Min(0f)] private float startDelay = 0.45f;
        [SerializeField, Min(0.01f)] private float outDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float inDuration = 0.35f;

        private ComicSequence story;
        private string nextScene;
        private int index = -1;
        private bool busy, leaving;

        private void Start()
        {
            (string storyName, string next) = SceneFlow.TakeStoryRequest();
            story = storyName != null ? stories.FirstOrDefault(s => s != null && s.name == storyName) : fallback;
            nextScene = next ?? fallbackNextScene;
            panelGroup.alpha = 0f;
            if (story == null || story.panels.Length == 0)
            {
                if (storyName != null)
                    Debug.LogWarning($"[Odisea] No hay historieta '{storyName}'; se sigue a {nextScene}.");
                Leave();
                return;
            }
            StartCoroutine(ShowNext(startDelay));
        }

        private void Update()
        {
            // Si se pidió salir mientras el iris de llegada seguía abriéndose, se reintenta al terminar.
            if (leaving)
            {
                if (!SceneFlow.IsLoading && UnityEngine.SceneManagement.SceneManager.GetActiveScene() == gameObject.scene)
                    SceneFlow.Load(nextScene);
                return;
            }
            if (story == null || index < 0)
                return;
            if (SkipPressed())
            {
                UiAudio.Play(UiSound.Back);
                Leave();
                return;
            }
            if (busy || !NextPressed())
                return;
            UiAudio.Play(UiSound.Click);
            if (index + 1 < story.panels.Length)
                StartCoroutine(ShowNext(0f));
            else
                Leave();
        }

        // La viñeta actual se va hacia la izquierda; la siguiente llega creciendo con un rebote, como al pasar la hoja.
        private IEnumerator ShowNext(float delay)
        {
            busy = true;
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            if (index >= 0)
            {
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / outDuration)
                {
                    panelGroup.alpha = 1f - t;
                    panel.anchoredPosition = new Vector2(-80f * t * t, 0f);
                    panel.localRotation = Quaternion.Euler(0f, 0f, 3f * t);
                    yield return null;
                }
            }

            index++;
            Fill(story.panels[index]);
            UiAudio.Play(UiSound.Page);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / inDuration)
            {
                Pose(t);
                yield return null;
            }
            Pose(1f);
            busy = false;
        }

        private void Pose(float t)
        {
            panelGroup.alpha = Mathf.Clamp01(t * 2f);
            float scale = 0.92f + 0.08f * t + Mathf.Sin(t * Mathf.PI) * 0.03f;
            panel.localScale = new Vector3(scale, scale, 1f);
            panel.anchoredPosition = new Vector2(60f * (1f - t), 0f);
            panel.localRotation = Quaternion.Euler(0f, 0f, -2f * (1f - t));
        }

        private void Fill(ComicSequence.Panel data)
        {
            bool hasArt = data.art != null;
            art.sprite = data.art;
            // Sin dibujo, un recuadro de color con la descripción de lo que va.
            art.color = hasArt ? Color.white : new Color(0.886f, 0.690f, 0.494f);
            art.preserveAspect = hasArt;
            placeholder.gameObject.SetActive(!hasArt);
            placeholder.text = data.placeholderNote;
            caption.text = data.caption;
            if (counter != null)
                counter.text = $"{story.title}   ·   {index + 1} / {story.panels.Length}";
            if (nextLabel != null)
                nextLabel.text = index + 1 < story.panels.Length ? "Siguiente" : "Continuar";
        }

        private void Leave()
        {
            leaving = true;
            if (!SceneFlow.IsLoading)
                SceneFlow.Load(nextScene);
        }

        private static bool NextPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                                         keyboard.spaceKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)) ||
                   (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) ||
                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        private static bool SkipPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                   (gamepad != null && (gamepad.buttonEast.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame));
        }
    }
}
