using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Una viñeta en la página: el dibujo (o, sin dibujo, un recuadro con la descripción) y su texto debajo.
    /// Aparece con un rebote. La crea <see cref="ComicViewer"/> copiando una plantilla.
    /// </summary>
    public class ComicPanelView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image art;
        [SerializeField] private TMP_Text placeholder;
        [SerializeField] private TMP_Text caption;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.35f;

        private float shownTime = -1f;

        public void Set(ComicSequence.Panel panel)
        {
            bool hasArt = panel.art != null;
            art.sprite = panel.art;
            art.color = hasArt ? Color.white : new Color(0.886f, 0.690f, 0.494f, 0.45f);
            art.preserveAspect = hasArt;
            placeholder.gameObject.SetActive(!hasArt);
            placeholder.text = panel.placeholderNote;
            caption.text = panel.caption;
            caption.gameObject.SetActive(!string.IsNullOrWhiteSpace(panel.caption));
            group.alpha = 0f;
        }

        public void Appear() => shownTime = Time.unscaledTime;

        public void ShowNow()
        {
            shownTime = Time.unscaledTime - appearDuration;
            Update();
        }

        private void Update()
        {
            if (shownTime < 0f)
                return;
            float t = Mathf.Clamp01((Time.unscaledTime - shownTime) / appearDuration);
            group.alpha = t;
            // Crece un poco de más, se asienta y gira apenas, como si la pegaran en la página.
            float scale = 0.85f + 0.15f * t + Mathf.Sin(t * Mathf.PI) * 0.06f;
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, (1f - t) * -4f);
        }
    }
}
