using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Odisea.UI
{
    /// <summary>
    /// Manícula (manita que señala, como en los libros antiguos) junto a la opción seleccionada del recetario.
    /// Se desliza a la nueva opción y se mece un poco, como si apuntara. Solo aparece para elementos dentro del libro.
    /// </summary>
    public class SelectionPointer : MonoBehaviour
    {
        [SerializeField] private RectTransform pointer;
        [Tooltip("Solo se señalan elementos dentro de esta zona (el libro).")]
        [SerializeField] private RectTransform area;
        [Tooltip("Separación entre la punta del dedo y el texto.")]
        [SerializeField] private float gap = 14f;
        [SerializeField] private float swayAmount = 6f;
        [SerializeField] private float swaySpeed = 5f;
        [Tooltip("Qué tan rápido se desliza a la nueva opción.")]
        [SerializeField] private float followSpeed = 18f;

        private CanvasGroup pointerGroup;
        private Transform lastTarget;

        private void Awake()
        {
            pointerGroup = pointer.GetComponent<CanvasGroup>();
            if (pointerGroup == null)
                pointerGroup = pointer.gameObject.AddComponent<CanvasGroup>();
            pointerGroup.blocksRaycasts = false;
        }

        private void LateUpdate()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null || !selected.activeInHierarchy || !selected.transform.IsChildOf(area))
            {
                pointerGroup.alpha = Mathf.MoveTowards(pointerGroup.alpha, 0f, Time.unscaledDeltaTime * 8f);
                lastTarget = null;
                return;
            }

            RectTransform target = selected.TryGetComponent(out PointerTarget custom) ? custom.Target : (RectTransform)selected.transform;
            Vector3 tip = LeftEdgeWorld(target);

            // La punta del dedo queda a la izquierda del texto, centrada en su altura.
            Vector3 local = pointer.parent.InverseTransformPoint(tip);
            float halfWidth = pointer.rect.width * 0.5f;
            float sway = Mathf.Sin(Time.unscaledTime * swaySpeed) * swayAmount;
            Vector2 desired = new(local.x - gap - halfWidth - Mathf.Abs(sway), local.y);

            bool jumped = lastTarget != selected.transform && pointerGroup.alpha < 0.05f;
            pointer.anchoredPosition = jumped
                ? desired
                : Vector2.Lerp(pointer.anchoredPosition, desired, 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));
            pointerGroup.alpha = Mathf.MoveTowards(pointerGroup.alpha, 1f, Time.unscaledDeltaTime * 8f);
            lastTarget = selected.transform;
        }

        /// <summary>Borde izquierdo del contenido visible: el texto real si es TextMeshPro, si no el rectángulo.</summary>
        private static Vector3 LeftEdgeWorld(RectTransform target)
        {
            if (target.TryGetComponent(out TMP_Text text) && text.textInfo != null && text.textInfo.characterCount > 0)
            {
                Bounds bounds = text.textBounds;
                return target.TransformPoint(new Vector3(bounds.min.x, bounds.center.y, 0f));
            }

            Rect rect = target.rect;
            return target.TransformPoint(new Vector3(rect.xMin, rect.center.y, 0f));
        }
    }
}
