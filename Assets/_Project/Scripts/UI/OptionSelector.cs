using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Selector "‹ Valor ›" para Opciones: izquierda / derecha (teclado, mando o clic en cada mitad) cambian
    /// de opción, y Aceptar avanza a la siguiente. Al llegar al final vuelve a empezar.
    /// </summary>
    public class OptionSelector : Selectable, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private RectTransform leftArrow;
        [SerializeField] private RectTransform rightArrow;

        private string[] options = Array.Empty<string>();
        private Coroutine nudge;
        private RectTransform nudged;
        private Vector2 nudgedStart;

        /// <summary>Se llama con el índice nuevo cuando la persona cambia la opción.</summary>
        public event Action<int> Changed;

        public int Index { get; private set; }

        public void SetOptions(string[] labels)
        {
            options = labels ?? Array.Empty<string>();
            SetIndexWithoutNotify(Index);
        }

        public void SetIndexWithoutNotify(int index)
        {
            Index = options.Length == 0 ? 0 : Mathf.Clamp(index, 0, options.Length - 1);
            if (valueText != null)
                valueText.text = options.Length > 0 ? options[Index] : "";
        }

        public override void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    Step(-1);
                    eventData.Use();
                    break;
                case MoveDirection.Right:
                    Step(1);
                    eventData.Use();
                    break;
                default:
                    base.OnMove(eventData);
                    break;
            }
        }

        public void OnSubmit(BaseEventData eventData) => Step(1);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable() || eventData.button != PointerEventData.InputButton.Left)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position,
                eventData.pressEventCamera, out Vector2 local);
            Step(local.x < ((RectTransform)transform).rect.center.x ? -1 : 1);
        }

        private void Step(int direction)
        {
            if (!IsInteractable() || options.Length < 2)
                return;
            SetIndexWithoutNotify((Index + direction + options.Length) % options.Length);
            UiAudio.Play(UiSound.Click);
            Nudge(direction < 0 ? leftArrow : rightArrow, direction);
            Changed?.Invoke(Index);
        }

        // La flecha del lado elegido da un saltito hacia afuera.
        private void Nudge(RectTransform arrow, int direction)
        {
            if (arrow == null)
                return;
            if (nudge != null)
            {
                // Si se interrumpe a medio salto, la flecha regresa a su lugar antes del siguiente.
                StopCoroutine(nudge);
                nudged.anchoredPosition = nudgedStart;
            }
            nudged = arrow;
            nudgedStart = arrow.anchoredPosition;
            nudge = StartCoroutine(NudgeRoutine(arrow, direction, nudgedStart));
        }

        private IEnumerator NudgeRoutine(RectTransform arrow, int direction, Vector2 start)
        {
            const float duration = 0.18f;
            const float distance = 10f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                arrow.anchoredPosition = start + new Vector2(direction * distance * Mathf.Sin(Mathf.PI * t / duration), 0f);
                yield return null;
            }
            arrow.anchoredPosition = start;
            nudge = null;
        }
    }
}
