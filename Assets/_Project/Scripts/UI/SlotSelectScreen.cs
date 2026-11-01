using System;
using Odisea.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Elegir partida (la abre Jugar): tres tarjetas con su avance (cuántos lugares del mapa lleva). Aceptar entra
    /// (nueva: historieta y tutorial; empezada: el mapa). Borrar (Q / Y / Triángulo)
    /// pregunta antes de vaciarla. Atrás vuelve al menú.
    /// </summary>
    public class SlotSelectScreen : MonoBehaviour
    {
        [Serializable]
        public class SlotCard
        {
            public Button button;
            public TMP_Text summary;
            [Tooltip("Un punto por lugar del mapa (en orden); se rellena el que ya terminó.")]
            public Image[] dots = Array.Empty<Image>();
        }

        [SerializeField] private SlotCard[] cards = Array.Empty<SlotCard>();
        [SerializeField] private Color dotDone = new(0.961f, 0.718f, 0.192f);
        [SerializeField] private Color dotPending = new(0.169f, 0.118f, 0.086f, 0.2f);

        [Header("Confirmar borrar")]
        [SerializeField] private GameObject confirm;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button confirmYes;
        [SerializeField] private Button confirmNo;

        private int pendingDelete;
        private GameObject lastSelected;

        private void Start()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int slot = i + 1;
                cards[i].button.onClick.AddListener(() => Play(slot));
            }
            confirmYes.onClick.AddListener(Delete);
            confirmNo.onClick.AddListener(CancelDelete);
            confirm.SetActive(false);
            Refresh();
            // Empieza en la última partida usada.
            Select(cards[Mathf.Clamp(GameProgress.Slot - 1, 0, cards.Length - 1)].button.gameObject);
        }

        private void Refresh()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int slot = i + 1;
                SlotCard card = cards[i];
                bool started = GameProgress.IsStarted(slot);
                int done = GameProgress.CompletedCount(slot);
                card.summary.text = !started ? "Nueva partida"
                    : done == GameProgress.Locations.Length ? "¡Historia completa!"
                    : $"{done} de {GameProgress.Locations.Length} lugares";
                for (int d = 0; d < card.dots.Length && d < GameProgress.Locations.Length; d++)
                    card.dots[d].color = GameProgress.IsDone(slot, GameProgress.Locations[d]) ? dotDone : dotPending;
            }
        }

        private void Update()
        {
            if (SceneFlow.IsLoading)
                return;
            if (confirm.activeSelf)
            {
                if (BackPressed())
                {
                    UiAudio.Play(UiSound.Back);
                    CancelDelete();
                }
                return;
            }
            if (BackPressed())
            {
                UiAudio.Play(UiSound.Back);
                SceneFlow.Load(SceneFlow.MainMenu);
                return;
            }
            int selected = SelectedSlot();
            if (selected > 0 && DeletePressed() && GameProgress.IsStarted(selected))
                AskDelete(selected);

            // Si un clic en el vacío quita la selección, el teclado y el mando no deben quedarse sin tarjeta.
            EventSystem events = EventSystem.current;
            if (events == null)
                return;
            if (events.currentSelectedGameObject == null && lastSelected != null && lastSelected.activeInHierarchy)
                events.SetSelectedGameObject(lastSelected);
            else
                lastSelected = events.currentSelectedGameObject;
        }

        private void Play(int slot)
        {
            if (confirm.activeSelf || SceneFlow.IsLoading)
                return;
            SceneFlow.PlaySlot(slot);
        }

        private void AskDelete(int slot)
        {
            pendingDelete = slot;
            confirmText.text = $"¿Borrar la Partida {slot}?\nSe pierde todo su avance.";
            confirm.SetActive(true);
            UiAudio.Play(UiSound.Click);
            Select(confirmNo.gameObject);
        }

        private void Delete()
        {
            GameProgress.ResetSlot(pendingDelete);
            UiAudio.Play(UiSound.Back);
            CloseConfirm();
            Refresh();
        }

        private void CancelDelete() => CloseConfirm();

        private void CloseConfirm()
        {
            confirm.SetActive(false);
            Select(cards[Mathf.Clamp(pendingDelete - 1, 0, cards.Length - 1)].button.gameObject);
        }

        private int SelectedSlot()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].button.gameObject == selected)
                    return i + 1;
            }
            return 0;
        }

        private void Select(GameObject target)
        {
            lastSelected = target;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        private static bool BackPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                   (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
        }

        private static bool DeletePressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && (keyboard.qKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame)) ||
                   (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);
        }
    }
}
