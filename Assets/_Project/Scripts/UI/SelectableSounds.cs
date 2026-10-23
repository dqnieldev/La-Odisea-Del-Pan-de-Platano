using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Hover al seleccionar (mouse, teclado o mando) y click al presionar.
    /// Funciona en botones y casillas; en un slider solo da el hover (el valor lo suena OptionsPanel).
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class SelectableSounds : MonoBehaviour, ISelectHandler
    {
        [Tooltip("Sonido al presionar. Volver usa Back.")]
        [SerializeField] private UiSound pressSound = UiSound.Click;

        private void Awake()
        {
            if (TryGetComponent(out Button button))
                button.onClick.AddListener(PlayPress);
            else if (TryGetComponent(out Toggle toggle))
                toggle.onValueChanged.AddListener(_ => PlayPress());
        }

        public void OnSelect(BaseEventData eventData) => UiAudio.Play(UiSound.Hover);

        private void PlayPress() => UiAudio.Play(pressSound);
    }
}
