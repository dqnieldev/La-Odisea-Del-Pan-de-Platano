using System.Collections.Generic;
using System.Linq;
using Odisea.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.UI
{
    /// <summary>
    /// Escena de historietas: muestra la que se pidió con <see cref="SceneFlow.PlayStory"/> en una página, viñeta
    /// por viñeta (Aceptar = siguiente), y al terminar carga la escena que sigue. Atrás o Start la salta.
    /// Las viñetas se acomodan solas: 2×2 si son 4, en fila si son 3 o menos.
    /// </summary>
    public class ComicViewer : MonoBehaviour
    {
        [Tooltip("Todas las historietas del juego; se busca la pedida por el nombre del asset.")]
        [SerializeField] private ComicSequence[] stories;
        [Tooltip("La que se muestra si se abre esta escena directo (pruebas en el editor).")]
        [SerializeField] private ComicSequence fallback;
        [SerializeField] private string fallbackNextScene = SceneFlow.MainMenu;
        [SerializeField] private TMP_Text title;
        [Tooltip("Área de la página donde van las viñetas.")]
        [SerializeField] private RectTransform page;
        [Tooltip("Viñeta de plantilla (apagada); se copia una por viñeta.")]
        [SerializeField] private ComicPanelView template;
        [Tooltip("Ayuda de botones; la palabra cambia de \"Siguiente\" a \"Continuar\" en la última viñeta.")]
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private Vector2 gap = new(48f, 36f);
        [Tooltip("Pausa antes de la primera viñeta y antes de aceptar botones (que no se salte con el clic de Jugar).")]
        [SerializeField, Min(0f)] private float startDelay = 0.5f;
        [Tooltip("Tiempo mínimo entre viñetas, para no saltarse varias con un solo toque.")]
        [SerializeField, Min(0f)] private float minInterval = 0.25f;

        private readonly List<ComicPanelView> views = new();
        private string nextScene;
        private int shown;
        private float startTime, lastStep;
        private bool leaving;

        private void Start()
        {
            (string storyName, string next) = SceneFlow.TakeStoryRequest();
            ComicSequence story = storyName != null ? stories.FirstOrDefault(s => s != null && s.name == storyName) : fallback;
            nextScene = next ?? fallbackNextScene;
            if (story == null || story.panels.Length == 0)
            {
                if (storyName != null)
                    Debug.LogWarning($"[Odisea] No hay historieta '{storyName}'; se sigue a {nextScene}.");
                Leave();
                return;
            }

            title.text = story.title;
            template.gameObject.SetActive(false);
            BuildPanels(story);
            startTime = Time.unscaledTime;
            lastStep = -minInterval;
        }

        private void BuildPanels(ComicSequence story)
        {
            int count = story.panels.Length;
            int columns = count == 4 ? 2 : Mathf.Min(count, 3);
            int rows = Mathf.CeilToInt(count / (float)columns);
            Vector2 area = page.rect.size;
            // Celdas iguales que llenan la página; la viñeta conserva su forma dentro de la celda.
            var cell = new Vector2((area.x - gap.x * (columns - 1)) / columns, (area.y - gap.y * (rows - 1)) / rows);
            Vector2 templateSize = ((RectTransform)template.transform).sizeDelta;
            float fit = Mathf.Min(cell.x / templateSize.x, cell.y / templateSize.y);
            // Las viñetas van juntas (separadas solo por gap), centradas en la página.
            Vector2 size = templateSize * fit;

            for (int i = 0; i < count; i++)
            {
                ComicPanelView view = Instantiate(template, page);
                view.name = $"Viñeta {i + 1}";
                view.gameObject.SetActive(true);
                var rect = (RectTransform)view.transform;
                rect.sizeDelta = size;
                int column = i % columns, row = i / columns;
                // La última fila, si queda incompleta, se centra.
                int inRow = row == rows - 1 ? count - row * columns : columns;
                float rowWidth = inRow * size.x + (inRow - 1) * gap.x;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(
                    -rowWidth / 2f + size.x / 2f + column * (size.x + gap.x),
                    (rows - 1) * (size.y + gap.y) / 2f - row * (size.y + gap.y));
                view.Set(story.panels[i]);
                views.Add(view);
            }
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
            if (views.Count == 0)
                return;
            float now = Time.unscaledTime;
            if (now - startTime < startDelay)
                return;
            if (shown == 0)
            {
                Step();
                return;
            }
            if (SkipPressed())
            {
                UiAudio.Play(UiSound.Back);
                Leave();
                return;
            }
            if (now - lastStep < minInterval || !NextPressed())
                return;
            UiAudio.Play(UiSound.Click);
            if (shown < views.Count)
                Step();
            else
                Leave();
        }

        private void Step()
        {
            views[shown].Appear();
            shown++;
            lastStep = Time.unscaledTime;
            UiAudio.Play(UiSound.Page);
            if (nextLabel != null)
                nextLabel.text = shown < views.Count ? "Siguiente" : "Continuar";
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
                                         keyboard.spaceKey.wasPressedThisFrame)) ||
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
