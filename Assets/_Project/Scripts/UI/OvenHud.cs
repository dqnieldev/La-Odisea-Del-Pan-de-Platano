using Odisea.Core;
using Odisea.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Horneado del nivel 2: un reloj de cocina cuya aguja da la vuelta en lo que dura el horneado; al terminar, el reloj
    /// suena y tiembla (sin el dibujo, un relleno que se va dorando).
    /// La vida del horno no tiene indicador: se ve en su dibujo (<see cref="Odisea.Level.Oven"/>).
    /// </summary>
    public class OvenHud : MonoBehaviour
    {
        [SerializeField] private BakeTimer bake;
        [SerializeField] private Image bakeFill;
        [SerializeField] private RectTransform needle;
        [SerializeField] private RectTransform clock;
        [SerializeField] private SoundEffect ringSound = new();
        [SerializeField] private CanvasGroup bakeGroup;
        [Tooltip("Color de la masa cruda y del pan ya dorado (el relleno, sin el dibujo del reloj).")]
        [SerializeField] private Color rawColor = new(0.96f, 0.86f, 0.62f);
        [SerializeField] private Color bakedColor = new(0.72f, 0.42f, 0.16f);

        private float ring;
        private bool rang;

        private void Update()
        {
            if (bake == null)
                return;
            float progress = bake.Progress;
            if (bakeFill != null)
            {
                bakeFill.fillAmount = progress;
                bakeFill.color = Color.Lerp(rawColor, bakedColor, progress);
            }
            if (needle != null)
                needle.localRotation = Quaternion.Euler(0f, 0f, -360f * progress);
            if (bake.Done && !rang)
            {
                // Suena la campanita: el reloj tiembla un momento.
                rang = true;
                ring = 1.2f;
                ringSound.Play();
            }
            if (clock != null && ring > 0f)
            {
                ring -= Time.deltaTime;
                float amount = Mathf.Clamp01(ring / 1.2f);
                clock.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 50f) * 10f * amount);
                clock.localScale = Vector3.one * (1f + 0.12f * amount);
                if (ring <= 0f)
                {
                    clock.localRotation = Quaternion.identity;
                    clock.localScale = Vector3.one;
                }
            }
            if (bakeGroup != null)
                bakeGroup.alpha = Mathf.MoveTowards(bakeGroup.alpha, bake.Done ? 0f : 1f, Time.deltaTime * 2f);
        }
    }
}
