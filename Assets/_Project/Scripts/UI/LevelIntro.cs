using System.Collections;
using Odisea.Core;
using Odisea.Player;
using TMPro;
using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Letrero al empezar un nivel, como en las caricaturas viejas: "¿Listo?" y luego la frase del nivel ("¡A subir!"),
    /// en grande al centro, cada uno salta al aparecer, tiembla un poquito (como película vieja) y se va. Mientras dura,
    /// el juego está congelado y Ulises no se mueve; al terminar, todo arranca. Corre con tiempo sin escala.
    /// </summary>
    public class LevelIntro : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private string[] lines = { "¿Listo?", "¡A jugar!" };
        [Tooltip("Espera antes del primer letrero (para que termine de aclarar la pantalla).")]
        [SerializeField, Min(0f)] private float startDelay = 0.6f;
        [SerializeField, Min(0.1f)] private float holdTime = 0.85f;
        [SerializeField] private SoundEffect popSound = new();

        private bool running = true;
        private PlayerInputReader[] readers = new PlayerInputReader[0];

        private void Start()
        {
            readers = FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None);
            foreach (PlayerInputReader reader in readers)
                reader.InputLocked = true;
            group.alpha = 0f;
            StartCoroutine(Play());
        }

        private void Update()
        {
            // Congelado mientras sale el letrero (la transición de entrada puede volver a poner el tiempo en 1).
            if (running && !GamePause.IsPaused)
                Time.timeScale = 0f;
        }

        private IEnumerator Play()
        {
            yield return Wait(startDelay);
            for (int i = 0; i < lines.Length; i++)
                yield return Show(lines[i], i == lines.Length - 1);
            Finish();
        }

        // Un letrero: salta al aparecer (de grande a su tamaño con un rebote), tiembla mientras está y se va.
        private IEnumerator Show(string line, bool last)
        {
            text.text = line;
            popSound.Play();
            const float popTime = 0.22f, outTime = 0.2f;
            float hold = last ? holdTime * 1.15f : holdTime;
            for (float t = 0f; t < popTime + hold + outTime; t += Time.unscaledDeltaTime)
            {
                float scale, alpha;
                if (t < popTime)
                {
                    float p = t / popTime;
                    // De 1.6 a 1 con un rebote al final.
                    scale = Mathf.LerpUnclamped(1.6f, 1f, 1f - Mathf.Pow(1f - p, 3f)) + Mathf.Sin(p * Mathf.PI) * 0.06f;
                    alpha = p;
                }
                else if (t < popTime + hold)
                {
                    scale = 1f + Mathf.Sin((t - popTime) * 3f) * 0.015f;
                    alpha = 1f;
                }
                else
                {
                    float p = (t - popTime - hold) / outTime;
                    scale = 1f + p * (last ? 0.35f : 0.12f);
                    alpha = 1f - p;
                }
                // Tembleque de película vieja: se mueve un poquito a saltos.
                float step = Mathf.Floor(Time.unscaledTime * 14f);
                var jitter = new Vector2(Mathf.Sin(step * 12.9898f) * 3f, Mathf.Cos(step * 78.233f) * 3f);
                text.rectTransform.anchoredPosition = jitter;
                text.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(step * 4.1f) * 0.8f);
                text.rectTransform.localScale = Vector3.one * scale;
                group.alpha = alpha;
                yield return null;
            }
            group.alpha = 0f;
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
                yield return null;
        }

        private void Finish()
        {
            running = false;
            if (!GamePause.IsPaused)
                Time.timeScale = 1f;
            foreach (PlayerInputReader reader in readers)
            {
                if (reader != null)
                    reader.InputLocked = false;
            }
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // Si se sale de la escena a la mitad, el tiempo no se queda congelado.
            if (running && !GamePause.IsPaused)
                Time.timeScale = 1f;
        }
    }
}
