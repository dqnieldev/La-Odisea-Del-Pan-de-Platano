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
        [Tooltip("Opcional (nivel del horno): la barra mide el horneado en vez del camino.")]
        [SerializeField] private BakeTimer bake;
        [Tooltip("Opcional, con el horneado: el jefe del final. La barra se parte en dos fases (horneado y jefe) y el " +
                 "100 % es vencerlo.")]
        [SerializeField] private Health boss;
        [SerializeField] private string bossName = "Polifemo";
        [Tooltip("Lo que ocupa el horneado en la barra (el resto es el jefe).")]
        [SerializeField, Range(0.1f, 0.9f)] private float bakeShare = 0.6f;
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
            if (Phased)
                CreatePhaseMarks();
            best = Progress();
        }

        private bool Phased => bake != null && boss != null;

        // La línea que separa las dos fases y el nombre de cada una dentro de la barra.
        private void CreatePhaseMarks()
        {
            var track = (RectTransform)progressFill.transform.parent;
            var line = new GameObject("Fin del horneado", typeof(RectTransform), typeof(Image));
            var lineRect = (RectTransform)line.transform;
            lineRect.SetParent(track, false);
            lineRect.anchorMin = new Vector2(bakeShare, 0f);
            lineRect.anchorMax = new Vector2(bakeShare, 1f);
            lineRect.sizeDelta = new Vector2(6f, 10f);
            line.GetComponent<Image>().color = PhaseColor;
            line.GetComponent<Image>().raycastTarget = false;
            // Ulises queda encima de la línea.
            progressMarker.SetAsLastSibling();
            PhaseLabel(track, "HORNEADO", 0f, bakeShare);
            PhaseLabel(track, bossName.ToUpperInvariant(), bakeShare, 1f);
        }

        private static readonly Color PhaseColor = new(0.36f, 0.22f, 0.13f);

        private void PhaseLabel(RectTransform track, string text, float from, float to)
        {
            var go = new GameObject(text, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(track, false);
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(to, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = progressLabel.font;
            label.text = text;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(PhaseColor.r, PhaseColor.g, PhaseColor.b, 0.9f);
            label.raycastTarget = false;
            rect.SetSiblingIndex(progressMarker.GetSiblingIndex());
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
            if (Phased)
            {
                // Horneado de 0 a bakeShare; después, la vida que le quitó al jefe hasta el 100 %.
                if (bake.Progress < 1f || boss.Max <= 0)
                    return bake.Progress * bakeShare;
                return bakeShare + (1f - bakeShare) * (1f - (float)boss.Current / boss.Max);
            }
            if (bake != null)
                return bake.Progress;
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
            progressGroup.SetActive(goal != null || bake != null);
            progressFill.fillAmount = 0f;
            progressLabel.text = ProgressText();
            // Sin punto de control, Reintentar ya empieza desde el principio: Reiniciar sobra y la salida sube a su lugar.
            bool hasCheckpoint = Checkpoints.TryGet(out _);
            restartButton.gameObject.SetActive(hasCheckpoint);
            if (!hasCheckpoint)
                ((RectTransform)exitButton.transform).anchoredPosition = ((RectTransform)restartButton.transform).anchoredPosition;
            shownTime = Time.unscaledTime;
            UiAudio.Play(UiSound.Back);
            if (EventSystem.current != null)
            {
                UiAudio.MuteHoverBriefly();
                EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
            }
        }

        private string ProgressText()
        {
            if (Phased)
            {
                if (best < bakeShare)
                    return $"El pan llevaba el {Mathf.RoundToInt(best / bakeShare * 100f)} % de horneado";
                int bossDone = Mathf.RoundToInt((best - bakeShare) / (1f - bakeShare) * 100f);
                return $"Pan horneado; a {bossName} le quitaste el {bossDone} % de vida";
            }
            return bake != null
                ? $"El pan llevaba el {Mathf.RoundToInt(best * 100f)} % de horneado"
                : $"Llegaste al {Mathf.RoundToInt(best * 100f)} % del camino";
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
