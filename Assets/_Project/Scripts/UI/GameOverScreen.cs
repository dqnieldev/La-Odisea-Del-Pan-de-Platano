using System.Collections;
using Odisea.Core;
using Odisea.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Game Over como en Cuphead: al quedarse Ulises sin corazones, después de su animación de muerte, se detiene el
    /// juego y sale la tarjeta con qué tan lejos llegó (del inicio a la meta) y las opciones: Reintentar (desde el
    /// último punto de control), Reiniciar el nivel (desde el principio) y salir (al mapa; al menú en el tutorial de
    /// práctica; a la panadería si aún no termina el tutorial).
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private Health player;
        [Tooltip("Donde empieza el nivel (sin punto de control): el 0 % de la barra.")]
        [SerializeField] private Vector2 levelStart;
        [Tooltip("La meta (el 100 %). Vacío = busca la LevelGoal de la escena.")]
        [SerializeField] private Transform goal;
        [SerializeField, Min(0f)] private float showDelay = 1.5f;

        [Header("Tarjeta")]
        [Tooltip("La tarjeta y el fondo oscuro; empieza apagado.")]
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private RectTransform card;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.35f;
        [SerializeField] private Image progressFill;
        [Tooltip("Ulises sobre la barra, en lo más lejos que llegó.")]
        [SerializeField] private RectTransform progressMarker;
        [SerializeField] private TMP_Text progressLabel;
        [Tooltip("Lo que no aplica sin meta en la escena (la barra).")]
        [SerializeField] private GameObject progressGroup;

        [Header("Botones")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        /// <summary>Ulises murió: ya no se abre la pausa (la tarjeta está por salir o ya salió).</summary>
        public static bool IsActive { get; private set; }

        private float best;
        private bool dead;
        private float shownTime = -1f;
        private float barWidth;

        private void Awake()
        {
            IsActive = false;
            panel.gameObject.SetActive(false);
            if (goal == null)
            {
                LevelGoal levelGoal = FindAnyObjectByType<LevelGoal>();
                if (levelGoal != null)
                    goal = levelGoal.transform;
            }
        }

        private void OnEnable()
        {
            if (player != null)
                player.Died += OnDied;
        }

        private void OnDisable()
        {
            if (player != null)
                player.Died -= OnDied;
        }

        private void Start()
        {
            retryButton.onClick.AddListener(Retry);
            restartButton.onClick.AddListener(Restart);
            exitButton.onClick.AddListener(Exit);
            string exitText = ExitDestination() switch
            {
                SceneFlow.MainMenu => "SALIR AL MENÚ",
                SceneFlow.Bakery => "VOLVER A LA PANADERÍA",
                _ => "VOLVER AL MAPA",
            };
            // El texto y sus copias de color (rojo y cian) de la opción.
            foreach (TMP_Text label in exitButton.GetComponentsInChildren<TMP_Text>(true))
                label.text = exitText;
            barWidth = ((RectTransform)progressFill.transform.parent).rect.width;
            best = Progress();
        }

        private void Update()
        {
            if (!dead)
            {
                best = Mathf.Max(best, Progress());
                return;
            }
            if (shownTime < 0f)
                return;
            float t = Mathf.Clamp01((Time.unscaledTime - shownTime) / appearDuration);
            panel.alpha = t;
            float scale = 0.8f + 0.2f * t + Mathf.Sin(t * Mathf.PI) * 0.08f;
            card.localScale = new Vector3(scale, scale, 1f);
            // La barra se llena hasta donde llegó.
            float fill = best * Mathf.Clamp01((Time.unscaledTime - shownTime - appearDuration) / 0.8f);
            progressFill.fillAmount = fill;
            progressMarker.anchoredPosition = new Vector2(fill * barWidth, progressMarker.anchoredPosition.y);
        }

        private void OnDestroy()
        {
            if (shownTime >= 0f)
                Time.timeScale = 1f;
            IsActive = false;
        }

        // 0 a 1: qué tanto avanzó Ulises del inicio hacia la meta (en línea recta; sirve para niveles horizontales y
        // verticales).
        private float Progress()
        {
            if (goal == null || player == null)
                return 0f;
            Vector2 path = (Vector2)goal.position - levelStart;
            if (path.sqrMagnitude < 0.01f)
                return 0f;
            return Mathf.Clamp01(Vector2.Dot((Vector2)player.transform.position - levelStart, path) / path.sqrMagnitude);
        }

        private void OnDied()
        {
            if (dead)
                return;
            dead = true;
            IsActive = true;
            StartCoroutine(ShowAfterDeath());
        }

        private IEnumerator ShowAfterDeath()
        {
            yield return new WaitForSecondsRealtime(showDelay);
            if (SceneFlow.IsLoading)
                yield break;
            Time.timeScale = 0f;
            panel.alpha = 0f;
            panel.gameObject.SetActive(true);
            progressGroup.SetActive(goal != null);
            progressFill.fillAmount = 0f;
            progressLabel.text = $"Llegaste al {Mathf.RoundToInt(best * 100f)} % del camino";
            // Sin punto de control, Reintentar ya empieza desde el principio: Reiniciar sobra.
            restartButton.gameObject.SetActive(Checkpoints.TryGet(out _));
            shownTime = Time.unscaledTime;
            UiAudio.Play(UiSound.Back);
            if (EventSystem.current != null)
            {
                UiAudio.MuteHoverBriefly();
                EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
            }
        }

        private bool CanChoose => shownTime >= 0f && Time.unscaledTime - shownTime > appearDuration && !SceneFlow.IsLoading;

        // Desde el último punto de control (si hay; si no, desde el principio).
        private void Retry()
        {
            if (!CanChoose)
                return;
            UiAudio.Play(UiSound.Click);
            SceneFlow.ReloadCurrent();
        }

        private void Restart()
        {
            if (!CanChoose)
                return;
            UiAudio.Play(UiSound.Click);
            Checkpoints.Clear();
            SceneFlow.ReloadCurrent();
        }

        private void Exit()
        {
            if (!CanChoose)
                return;
            UiAudio.Play(UiSound.Back);
            Checkpoints.Clear();
            SceneFlow.Load(ExitDestination());
        }

        private static string ExitDestination()
        {
            if (SceneFlow.TutorialFromMenu)
                return SceneFlow.MainMenu;
            return GameProgress.TutorialDone ? SceneFlow.Map : SceneFlow.Bakery;
        }
    }
}
