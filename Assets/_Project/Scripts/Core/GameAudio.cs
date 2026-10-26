using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Reproduce los efectos de sonido del juego (golpes, mordidas, daño...). Tiene varias fuentes para que los
    /// sonidos se puedan encimar, sigue sonando aunque el objeto que lo pidió se destruya (un enemigo que muere)
    /// y respeta el volumen de Efectos de Opciones (además del general). Se crea solo la primera vez.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        private const int Voices = 10;

        private static GameAudio instance;
        private AudioSource[] sources;
        private int next;

        public static void Play(SoundEffect effect)
        {
            if (effect == null || !effect.HasClips)
                return;
            AudioClip clip = effect.clips[Random.Range(0, effect.clips.Length)];
            if (clip == null)
                return;
            Instance.PlayClip(clip, effect.volume, 1f + Random.Range(-effect.pitchVariation, effect.pitchVariation));
        }

        private static GameAudio Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                var go = new GameObject("Efectos de sonido");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<GameAudio>();
                instance.sources = new AudioSource[Voices];
                for (int i = 0; i < Voices; i++)
                {
                    AudioSource source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    instance.sources[i] = source;
                }
                return instance;
            }
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            // Una fuente libre; si todas suenan, se reusa la que lleva más tiempo (la más vieja).
            AudioSource source = null;
            for (int i = 0; i < Voices && source == null; i++)
            {
                AudioSource candidate = sources[(next + i) % Voices];
                if (!candidate.isPlaying)
                    source = candidate;
            }
            source ??= sources[next];
            next = (next + 1) % Voices;

            source.clip = clip;
            source.volume = volume * GameSettings.EffectsVolume;
            source.pitch = pitch;
            source.Play();
        }
    }
}
