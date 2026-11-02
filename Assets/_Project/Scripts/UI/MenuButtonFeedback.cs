using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Estados de los botones del menú según el design system:
    /// seleccionado = un tono más claro con rebote; presionado = baja 2 px y se encoge un poco.
    /// Funciona igual con mouse, teclado y mando (al pasar el mouse, el botón se selecciona).
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class MenuButtonFeedback : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler,
        IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        [Tooltip("Parte del botón que se mueve (relleno, borde y texto). La sombra se queda fija.")]
        [SerializeField] private RectTransform face;
        [SerializeField] private Image fill;
        [SerializeField] private Color normalColor = new(0.961f, 0.718f, 0.192f);      // Plátano 500
        [SerializeField] private Color selectedColor = new(0.976f, 0.800f, 0.396f);    // Plátano 300
        [SerializeField] private Color pressedColor = new(0.878f, 0.635f, 0.118f);     // Plátano 600
        [SerializeField] private float selectedScale = 1.06f;
        [SerializeField] private float pressDepth = 3f;
        [Tooltip("Opcional: texto que cambia de color al seleccionar (botones de tinta que se rellenan de café).")]
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color labelNormalColor = new(0.169f, 0.118f, 0.086f);
        [SerializeField] private Color labelSelectedColor = new(0.984f, 0.973f, 0.949f);

        private Selectable selectable;
        private Vector2 faceRestPosition;
        private bool selected;
        private Coroutine scaleRoutine;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            faceRestPosition = face.anchoredPosition;
            Paint(normalColor, false);
        }

        private void OnDisable()
        {
            // Al ocultar el panel el botón no debe quedarse "presionado" o agrandado.
            selected = false;
            face.anchoredPosition = faceRestPosition;
            transform.localScale = Vector3.one;
            Paint(normalColor, false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Paint(selectedColor, true);
            AnimateScale(selectedScale, bounce: true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            Paint(normalColor, false);
            AnimateScale(1f, bounce: false);
        }

        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);

        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);

        public void OnSubmit(BaseEventData eventData)
        {
            if (isActiveAndEnabled)
                StartCoroutine(QuickPress());
        }

        private void SetPressed(bool pressed)
        {
            face.anchoredPosition = faceRestPosition + (pressed ? Vector2.down * pressDepth : Vector2.zero);
            Paint(pressed ? pressedColor : selected ? selectedColor : normalColor, pressed || selected);
            if (pressed)
                AnimateScale(0.96f, bounce: false);
            else
                AnimateScale(selected ? selectedScale : 1f, bounce: false);
        }

        private void Paint(Color fillColor, bool lit)
        {
            fill.color = fillColor;
            if (label != null)
                label.color = lit ? labelSelectedColor : labelNormalColor;
        }

        private IEnumerator QuickPress()
        {
            SetPressed(true);
            yield return new WaitForSecondsRealtime(0.09f);
            SetPressed(false);
        }

        private void AnimateScale(float target, bool bounce)
        {
            if (!isActiveAndEnabled)
            {
                transform.localScale = Vector3.one * target;
                return;
            }
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
                // Back ease-out (≈ cubic-bezier(.34,1.56,.64,1) del design system): se pasa un poco y regresa.
                float eased = bounce ? 1f + 2.7f * Mathf.Pow(p - 1f, 3f) + 1.7f * Mathf.Pow(p - 1f, 2f) : 1f - (1f - p) * (1f - p);
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(start, target, eased);
                yield return null;
            }
            transform.localScale = Vector3.one * target;
            scaleRoutine = null;
        }
    }
}
