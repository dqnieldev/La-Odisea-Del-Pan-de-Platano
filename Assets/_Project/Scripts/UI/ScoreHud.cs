using Odisea.Level;
using TMPro;
using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// HUD del puntaje, arriba a la derecha: ingredientes recogidos ("3/8", con un saltito al recoger uno) y el
    /// cronómetro. Sin ingredientes en el nivel, solo el cronómetro.
    /// </summary>
    public class ScoreHud : MonoBehaviour
    {
        [SerializeField] private LevelScore score;
        [SerializeField] private GameObject ingredientsGroup;
        [SerializeField] private RectTransform ingredientsIcon;
        [SerializeField] private TMP_Text ingredientsText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField, Min(0.01f)] private float popDuration = 0.3f;
        [Tooltip("Opcional (nivel del horno): el texto del tiempo cuenta lo que falta de horneado en vez del tiempo del nivel.")]
        [SerializeField] private BakeTimer bake;

        private int shownSecond = -1;
        private int shownIngredients = -1;
        private float popTime = -1f;

        private void OnEnable()
        {
            if (score != null)
                score.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (score != null)
                score.Changed -= Refresh;
        }

        private void Refresh()
        {
            ingredientsGroup.SetActive(score.IngredientsTotal > 0);
            if (shownIngredients >= 0 && score.IngredientsFound > shownIngredients)
                popTime = Time.unscaledTime;
            shownIngredients = score.IngredientsFound;
            ingredientsText.text = $"{score.IngredientsFound}/{score.IngredientsTotal}";
        }

        private void Update()
        {
            if (score == null)
                return;
            float shown = bake != null ? Mathf.Ceil(bake.Duration - bake.Elapsed) : score.Seconds;
            int second = Mathf.FloorToInt(shown);
            if (second != shownSecond)
            {
                shownSecond = second;
                timeText.text = LevelScore.FormatTime(shown);
            }
            if (popTime < 0f)
                return;
            float t = (Time.unscaledTime - popTime) / popDuration;
            if (t >= 1f)
            {
                popTime = -1f;
                ingredientsIcon.localScale = Vector3.one;
                return;
            }
            ingredientsIcon.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.35f);
        }
    }
}
