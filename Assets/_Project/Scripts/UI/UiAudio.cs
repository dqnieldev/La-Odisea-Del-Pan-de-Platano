using UnityEngine;

namespace Odisea.UI
{
    public enum UiSound
    {
        Hover,
        Click,
        Back,
        Page,
    }

    /// <summary>
    /// Sonidos de la interfaz (hover, click, volver). Va una vez por escena de menú;
    /// los botones lo usan con <see cref="Play"/>. Respeta el volumen general porque suena por el AudioListener.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class UiAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip hover;
        [SerializeField] private AudioClip click;
        [Tooltip("Opcional. Si falta, Volver usa el sonido de click.")]
        [SerializeField] private AudioClip back;
        [Tooltip("Opcional. Hoja que da vuelta en el recetario de Opciones (ui_clickOptions); si falta, usa el click.")]
        [SerializeField] private AudioClip page;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [Tooltip("Tiempo mínimo entre dos hovers, para que no se amontonen al mover el slider o el mouse rápido.")]
        [SerializeField, Min(0f)] private float hoverCooldown = 0.06f;

        private static UiAudio current;
        private AudioSource source;
        private float hoverMutedUntil;
        private float lastHoverTime = -1f;

        private void Awake()
        {
            current = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            // Al abrir el menú se selecciona un botón automáticamente; eso no debe sonar.
            MuteHoverBriefly();
        }

        private void OnDestroy()
        {
            if (current == this)
                current = null;
        }

        public static void Play(UiSound sound)
        {
            if (current != null)
                current.PlaySound(sound);
        }

        /// <summary>Silencia el hover un instante. Llamarlo antes de seleccionar un botón por código.</summary>
        public static void MuteHoverBriefly()
        {
            if (current != null)
                current.hoverMutedUntil = Time.unscaledTime + 0.1f;
        }

        private void PlaySound(UiSound sound)
        {
            float now = Time.unscaledTime;
            if (sound == UiSound.Hover)
            {
                if (now < hoverMutedUntil || now - lastHoverTime < hoverCooldown)
                    return;
                lastHoverTime = now;
            }

            AudioClip clip = sound switch
            {
                UiSound.Hover => hover,
                UiSound.Back => back != null ? back : click,
                UiSound.Page => page != null ? page : click,
                _ => click,
            };
            if (clip != null)
                source.PlayOneShot(clip, volume);
        }
    }
}
