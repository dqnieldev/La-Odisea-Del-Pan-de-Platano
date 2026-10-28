using System;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Un efecto de sonido del juego: una o varias variantes (se elige una al azar, sin repetir la anterior) y un poco de variación de tono,
    /// para que el mismo golpe no suene idéntico cada vez. Sin clips no suena (no es error).
    /// </summary>
    [Serializable]
    public class SoundEffect
    {
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Cuánto puede subir o bajar el tono al azar (0.06 = ±6 %).")]
        [Range(0f, 0.3f)] public float pitchVariation = 0.06f;
        [Tooltip("Volumen de cada variante (mismo orden que clips; 1 si falta), para que todas suenen igual de fuerte.")]
        public float[] clipVolumes = Array.Empty<float>();

        public bool HasClips => clips != null && clips.Length > 0;

        // Variante que sonó la última vez: con dos o más, no se repite la misma dos veces seguidas.
        [NonSerialized] private int lastClip = -1;

        /// <summary>Una variante al azar, distinta de la anterior si hay de dónde escoger, y su volumen propio.</summary>
        public AudioClip NextClip(out float clipVolume)
        {
            clipVolume = 1f;
            if (!HasClips)
                return null;
            int index = UnityEngine.Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == lastClip)
                index = (index + UnityEngine.Random.Range(1, clips.Length)) % clips.Length;
            lastClip = index;
            if (clipVolumes != null && index < clipVolumes.Length)
                clipVolume = clipVolumes[index];
            return clips[index];
        }

        public void Play() => GameAudio.Play(this);

        /// <summary>Suena dentro de unos segundos (sigue sonando aunque quien lo pidió se destruya).</summary>
        public void PlayAfter(float delay) => GameAudio.Play(this, delay);
    }
}
