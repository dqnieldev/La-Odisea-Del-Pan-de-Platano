using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Opción de menú solo de texto, estilo Cuphead: apagada en gris cálido; al seleccionarla
    /// se ilumina, crece con un rebote y aparece un leve desfase rojo/cian como de película vieja.
    /// Funciona con mouse, teclado y mando (al pasar el mouse, la opción se selecciona).
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class TextMenuItemFeedback : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler,
        IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        [SerializeField] private TMP_Text label;
        [Tooltip("Copias del texto desplazadas (roja y cian) que solo se ven en la opción seleccionada.")]
        [SerializeField] private TMP_Text[] ghosts;
        [Tooltip("Color de las opciones no seleccionadas: gris cálido que combina con el fondo madera (#9C8A74).")]
        [SerializeField] private Color normalColor = new(0.612f, 0.541f, 0.455f);
        [SerializeField] private Color selectedColor = new(0.98f, 0.97f, 0.93f);
        [SerializeField] private float selectedScale = 1.1f;
        [Tooltip("Cuánto tiemblan las copias de color, en píxeles, para que se sienta vivo.")]
        [SerializeField, Min(0f)] private float ghostJitter = 1f;

        private Selectable selectable;
        private Vector2[] ghostRestPositions;
        private bool selected;
        private Coroutine scaleRoutine;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            ghostRestPositions = new Vector2[ghosts.Length];
            for (int i = 0; i < ghosts.Length; i++)
                ghostRestPositions[i] = ghosts[i].rectTransform.anchoredPosition;
            ApplyState(instant: true);
        }

        private void OnDisable()
        {
            selected = false;
            ApplyState(instant: true);
        }

        private void Update()
        {
            if (!selected || ghostJitter <= 0f)
                return;
            // Temblor sutil y desordenado, como el de un proyector.
            float t = Time.unscaledTime * 18f;
            for (int i = 0; i < ghosts.Length; i++)
            {
                float jitter = (Mathf.PerlinNoise(t, i * 7.3f) - 0.5f) * 2f * ghostJitter;
                ghosts[i].rectTransform.anchoredPosition = ghostRestPositions[i] + new Vector2(jitter, 0f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            ApplyState(instant: false);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            ApplyState(instant: false);
        }

        public void OnPointerDown(PointerEventData eventData) => AnimateScale(selectedScale * 0.92f, bounce: false);

        public void OnPointerUp(PointerEventData eventData) => AnimateScale(selected ? selectedScale : 1f, bounce: false);

        public void OnSubmit(BaseEventData eventData)
        {
            if (isActiveAndEnabled)
                StartCoroutine(QuickPress());
        }

        private IEnumerator QuickPress()
        {
            AnimateScale(selectedScale * 0.92f, bounce: false);
            yield return new WaitForSecondsRealtime(0.08f);
            AnimateScale(selectedScale, bounce: true);
        }

        private void ApplyState(bool instant)
        {
            label.color = selected ? selectedColor : normalColor;
            foreach (TMP_Text ghost in ghosts)
                ghost.enabled = selected;
            for (int i = 0; i < ghosts.Length; i++)
                ghosts[i].rectTransform.anchoredPosition = ghostRestPositions[i];

            float target = selected ? selectedScale : 1f;
            if (instant || !isActiveAndEnabled)
                transform.localScale = Vector3.one * target;
            else
                AnimateScale(target, bounce: selected);
        }

        private void AnimateScale(float target, bool bounce)
        {
            if (!isActiveAndEnabled)
                return;
            if (scaleRoutine != null)
                StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(ScaleTo(target, bounce));
        }

        private IEnumerator ScaleTo(float target, bool bounce)
        {
            const float duration = 0.16f;
            float start = transform.localScale.x;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = t / duration;
                // Back ease-out: se pasa un poco y regresa (rebote del design system).
                float eased = bounce ? 1f + 2.7f * Mathf.Pow(p - 1f, 3f) + 1.7f * Mathf.Pow(p - 1f, 2f) : 1f - (1f - p) * (1f - p);
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(start, target, eased);
                yield return null;
            }
            transform.localScale = Vector3.one * target;
            scaleRoutine = null;
        }
    }
}
