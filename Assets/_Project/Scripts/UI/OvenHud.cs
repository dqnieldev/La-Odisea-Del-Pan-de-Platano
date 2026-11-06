using Odisea.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// HUD del nivel 2: la vida del horno (debajo de los corazones, con su ícono, que tiembla al recibir un golpe) y la
    /// barra del horneado arriba al centro, que se va dorando y se desvanece cuando el pan está listo.
    /// </summary>
    public class OvenHud : MonoBehaviour
    {
        [SerializeField] private Oven oven;
        [SerializeField] private BakeTimer bake;
        [SerializeField] private Image ovenFill;
        [SerializeField] private RectTransform ovenIcon;
        [SerializeField] private Image bakeFill;
        [SerializeField] private CanvasGroup bakeGroup;
        [Tooltip("Color de la masa cruda y del pan ya dorado.")]
        [SerializeField] private Color rawColor = new(0.96f, 0.86f, 0.62f);
        [SerializeField] private Color bakedColor = new(0.72f, 0.42f, 0.16f);

        private float shake;

        private void OnEnable()
        {
            if (oven != null)
                oven.Changed += OnOvenChanged;
        }

        private void OnDisable()
        {
            if (oven != null)
                oven.Changed -= OnOvenChanged;
        }

        private void OnOvenChanged(int current, int max)
        {
            float value = max > 0 ? (float)current / max : 0f;
            if (value < ovenFill.fillAmount)
                shake = 0.3f;
            ovenFill.fillAmount = value;
        }

        private void Update()
        {
            if (bake != null)
            {
                bakeFill.fillAmount = bake.Progress;
                bakeFill.color = Color.Lerp(rawColor, bakedColor, bake.Progress);
                bakeGroup.alpha = Mathf.MoveTowards(bakeGroup.alpha, bake.Done ? 0f : 1f, Time.deltaTime * 2f);
            }
            if (shake <= 0f || ovenIcon == null)
                return;
            shake -= Time.deltaTime;
            ovenIcon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 60f) * 12f * Mathf.Clamp01(shake / 0.3f));
            if (shake <= 0f)
                ovenIcon.localRotation = Quaternion.identity;
        }
    }
}
