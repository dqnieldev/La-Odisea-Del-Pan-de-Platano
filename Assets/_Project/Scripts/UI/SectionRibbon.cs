using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Listón separador del recetario: cuelga desde el borde de arriba del libro y baja hasta la sección abierta
    /// en el índice, como la cinta que marca la receta. Al cambiar de sección se estira o recoge suavemente.
    /// El sprite es 9-slice: solo se estira el tramo recto y la punta en V no se deforma.
    /// </summary>
    public class SectionRibbon : MonoBehaviour
    {
        [SerializeField] private RectTransform ribbon;
        [Tooltip("Cuánto baja la punta por debajo del centro del texto de la sección.")]
        [SerializeField] private float tipBelowText = 34f;
        [Tooltip("Largo cuando no marca ninguna sección (asoma un poco del libro).")]
        [SerializeField] private float retractedLength = 70f;
        [SerializeField] private float smoothTime = 0.18f;

        private RectTransform target;
        private float velocity;

        /// <summary>Marca una sección del índice. Con <paramref name="instant"/> se coloca sin animación (al abrir el libro).</summary>
        public void PointTo(RectTransform sectionItem, bool instant = false)
        {
            target = sectionItem;
            if (instant)
                SetLength(TargetLength());
        }

        /// <summary>Recoge el listón (por ejemplo, en la portada de la pausa).</summary>
        public void Retract(bool instant = false)
        {
            target = null;
            if (instant)
                SetLength(retractedLength);
        }

        private void LateUpdate()
        {
            float length = Mathf.SmoothDamp(ribbon.sizeDelta.y, TargetLength(), ref velocity, smoothTime,
                Mathf.Infinity, Time.unscaledDeltaTime);
            SetLength(length);
        }

        private float TargetLength()
        {
            if (target == null)
                return retractedLength;
            // Distancia vertical desde el borde superior del listón hasta el centro de la sección, en el espacio del listón.
            Vector3 targetCenter = ribbon.parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
            return Mathf.Max(retractedLength, ribbon.anchoredPosition.y - targetCenter.y + tipBelowText);
        }

        private void SetLength(float length) => ribbon.sizeDelta = new Vector2(ribbon.sizeDelta.x, length);
    }
}
