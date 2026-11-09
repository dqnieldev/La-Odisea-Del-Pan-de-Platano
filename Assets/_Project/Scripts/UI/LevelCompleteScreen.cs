using System;
using Odisea.Core;
using Odisea.Level;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.UI
{
    /// <summary>
    /// Tarjeta de "¡Nivel completado!" que sale al llegar a la meta: detiene el juego, aparece con un rebote y, si el
    /// nivel lleva puntaje, va sumando renglón por renglón (ingredientes o vidas del horno, tiempo, enemigos y sin daño), luego el total
    /// y al final cae el sello del rango. Aceptar (Enter, Espacio, clic, A / Cruz o Start) adelanta la cuenta y, ya
    /// terminada, sigue. La abre <see cref="LevelGoal"/>.
    /// </summary>
    public class LevelCompleteScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.35f;
        [Tooltip("Segundos antes de aceptar el botón, para que un salto que se venía pulsando no la salte.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.6f;
        [Tooltip("La frase del nivel: solo sin puntaje.")]
        [SerializeField] private GameObject message;
        [SerializeField] private GameObject prompt;

        [Header("Puntaje")]
        [SerializeField] private GameObject scoreGroup;
        [Tooltip("Renglones en orden: ingredientes, tiempo, enemigos, sin daño.")]
        [SerializeField] private CanvasGroup[] rows = new CanvasGroup[4];
        [SerializeField] private TMP_Text[] rowDetails = new TMP_Text[4];
        [SerializeField] private TMP_Text[] rowPoints = new TMP_Text[4];
        [Tooltip("Nombre del primer renglón en el nivel del horno (en lugar de los ingredientes).")]
        [SerializeField] private string ovenRowName = "Vida del horno";
        [SerializeField] private CanvasGroup totalGroup;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private RectTransform stamp;
        [SerializeField] private TMP_Text rankLetter;
        [SerializeField] private TMP_Text rankPhrase;
        [SerializeField] private GameObject recordLabel;
        [Tooltip("Color de la letra por rango: C, B, A, S.")]
        [SerializeField] private Color[] rankColors = new Color[4];
        [SerializeField] private string[] rankPhrases = { "Pan quemadito", "Pan decente", "¡Pan de primera!", "¡Empleado del mes!" };

        [Header("Ritmo de la cuenta")]
        [SerializeField, Min(0.05f)] private float rowInterval = 0.45f;
        [SerializeField, Min(0.05f)] private float countDuration = 0.35f;
        [SerializeField, Min(0.05f)] private float totalDuration = 0.6f;
        [SerializeField, Min(0.05f)] private float stampDuration = 0.25f;

        private Action onContinue;
        private float shownTime = -1f;
        private ScoreResult? result;
        private int[] points;
        private float rowsStart, totalStart, stampStart, endTime;
        private bool stamped, ticked;
        private int lastRowTicked;

        public bool IsShowing => shownTime >= 0f;

        private void Awake() => gameObject.SetActive(false);

        public void Show(Action continueAction, ScoreResult? score = null)
        {
            if (IsShowing)
                return;
            onContinue = continueAction;
            result = score;
            gameObject.SetActive(true);
            shownTime = Time.unscaledTime;
            Time.timeScale = 0f; // El iris del cambio de escena lo vuelve a 1.
            group.alpha = 0f;
            UiAudio.Play(UiSound.Click);

            bool hasScore = score.HasValue && scoreGroup != null;
            if (message != null)
                message.SetActive(!hasScore);
            if (scoreGroup != null)
                scoreGroup.SetActive(hasScore);
            if (!hasScore)
            {
                endTime = shownTime + appearDuration;
                prompt.SetActive(true);
                return;
            }
            SetUpScore(score.Value);
        }

        private void SetUpScore(ScoreResult score)
        {
            points = new[] { score.IngredientPoints, score.TimePoints, score.EnemyPoints, score.NoDamagePoints };
            rowDetails[0].text = $"{score.IngredientsFound}/{score.IngredientsTotal}";
            rowDetails[1].text = LevelScore.FormatTime(score.Seconds);
            rowDetails[2].text = score.EnemiesDefeated.ToString();
            rowDetails[3].text = score.NoDamage ? "¡Sí!" : "No";
            // Sin ingredientes en el nivel (el tutorial) no sale ese renglón. En el del horno, ese renglón son las vidas
            // que le quedaron al horno.
            rows[0].gameObject.SetActive(score.IngredientsTotal > 0 || score.OvenMax > 0);
            if (score.OvenMax > 0)
            {
                points[0] = score.OvenPoints;
                rowDetails[0].text = $"{score.OvenLife}/{score.OvenMax}";
                if (rows[0].transform.Find("Nombre") is { } label && label.TryGetComponent(out TMP_Text name))
                {
                    // Es más largo que los otros renglones: se encoge lo justo para no encimarse con el detalle.
                    name.text = ovenRowName;
                    name.fontSizeMax = name.fontSize;
                    name.fontSizeMin = name.fontSize * 0.7f;
                    name.enableAutoSizing = true;
                }
            }
            foreach (CanvasGroup row in rows)
                row.alpha = 0f;
            foreach (TMP_Text text in rowPoints)
                text.text = "";
            totalGroup.alpha = 0f;
            stamp.gameObject.SetActive(false);
            recordLabel.SetActive(false);
            prompt.SetActive(false);
            rankLetter.text = score.Rank.ToString();
            rankLetter.color = rankColors[(int)score.Rank];
            rankPhrase.text = rankPhrases[(int)score.Rank];

            rowsStart = shownTime + appearDuration + 0.2f;
            totalStart = rowsStart + VisibleRows() * rowInterval + 0.15f;
            stampStart = totalStart + totalDuration + 0.3f;
            endTime = stampStart + stampDuration;
            stamped = ticked = false;
            lastRowTicked = -1;
        }

        private int VisibleRows()
        {
            int count = 0;
            foreach (CanvasGroup row in rows)
                if (row.gameObject.activeSelf)
                    count++;
            return count;
        }

        private void Update()
        {
            if (!IsShowing)
                return;
            float now = Time.unscaledTime;
            float elapsed = now - shownTime;
            float t = Mathf.Clamp01(elapsed / appearDuration);
            group.alpha = t;
            // Crece un poco de más y se asienta.
            float scale = 0.8f + 0.2f * t + Mathf.Sin(t * Mathf.PI) * 0.08f;
            card.localScale = new Vector3(scale, scale, 1f);
            if (result.HasValue && scoreGroup != null)
                AnimateScore(now);

            if (onContinue == null || elapsed < inputDelay || !ContinuePressed())
                return;
            if (now < endTime)
            {
                // Primero adelanta la cuenta hasta el final.
                SkipToEnd();
                return;
            }
            UiAudio.Play(UiSound.Click);
            Action action = onContinue;
            onContinue = null;
            action();
        }

        private void AnimateScore(float now)
        {
            ScoreResult score = result.Value;
            int visible = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                if (!rows[i].gameObject.activeSelf)
                    continue;
                float start = rowsStart + visible * rowInterval;
                visible++;
                float p = Mathf.Clamp01((now - start) / countDuration);
                rows[i].alpha = p;
                if (p <= 0f)
                    continue;
                if (i > lastRowTicked)
                {
                    lastRowTicked = i;
                    UiAudio.Play(UiSound.Hover);
                }
                rowPoints[i].text = $"+{Mathf.RoundToInt(points[i] * p)}";
            }

            float total = Mathf.Clamp01((now - totalStart) / totalDuration);
            totalGroup.alpha = Mathf.Clamp01(total * 3f);
            totalText.text = Mathf.RoundToInt(score.Total * total).ToString();
            if (total > 0f && !ticked)
            {
                ticked = true;
                UiAudio.Play(UiSound.Page);
            }

            float s = (now - stampStart) / stampDuration;
            if (s < 0f)
                return;
            if (!stamped)
            {
                stamped = true;
                stamp.gameObject.SetActive(true);
                UiAudio.Play(UiSound.Click);
            }
            // El sello cae grande y se aplasta contra la hoja.
            float k = Mathf.Clamp01(s);
            stamp.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, k * k);
            if (k >= 1f)
            {
                recordLabel.SetActive(score.NewRecord);
                prompt.SetActive(true);
            }
        }

        private void SkipToEnd()
        {
            float shift = endTime - Time.unscaledTime;
            rowsStart -= shift;
            totalStart -= shift;
            stampStart -= shift;
            endTime -= shift;
            lastRowTicked = rows.Length; // Sin el tic de cada renglón al saltar.
            ticked = true;
        }

        private static bool ContinuePressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                                         keyboard.spaceKey.wasPressedThisFrame)) ||
                   (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame)) ||
                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        private void OnDestroy()
        {
            if (IsShowing)
                Time.timeScale = 1f;
        }
    }
}
