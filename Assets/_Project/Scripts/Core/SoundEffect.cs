using System;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Un efecto de sonido del juego: una o varias variantes (se elige una al azar) y un poco de variación de tono,
    /// para que el mismo golpe no suene idéntico cada vez. Sin clips no suena (no es error).
    /// </summary>
    [Serializable]
    public class SoundEffect
    {
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Cuánto puede subir o bajar el tono al azar (0.06 = ±6 %).")]
        [Range(0f, 0.3f)] public float pitchVariation = 0.06f;

        public bool HasClips => clips != null && clips.Length > 0;

        public void Play() => GameAudio.Play(this);

        /// <summary>Suena dentro de unos segundos (sigue sonando aunque quien lo pidió se destruya).</summary>
        public void PlayAfter(float delay) => GameAudio.Play(this, delay);
    }
}
