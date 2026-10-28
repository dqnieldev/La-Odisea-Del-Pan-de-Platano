using System.Collections.Generic;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Reproduce los efectos de sonido del juego (golpes, mordidas, daño...). Tiene varias fuentes para que los
    /// sonidos se puedan encimar, sigue sonando aunque el objeto que lo pidió se destruya (un enemigo que muere)
    /// y respeta el volumen de Efectos de Opciones (además del general). Se crea solo la primera vez.
    /// Se salta el silencio con el que empiezan muchos archivos, para que el sonido coincida con la acción.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        private const int Voices = 10;
        // Se empieza a reproducir un poco antes del primer sonido audible, para no cortar su ataque.
        private const float PreRoll = 0.01f;
        private const float SilenceThreshold = 0.02f;

        private static GameAudio instance;
        private AudioSource[] sources;
        private int next;
        private readonly Dictionary<AudioClip, float> startTimes = new();

        public static void Play(SoundEffect effect, float delay = 0f)
        {
            if (effect == null || !effect.HasClips)
                return;
            AudioClip clip = effect.NextClip(out float clipVolume);
            if (clip == null)
                return;
            Instance.PlayClip(clip, effect.volume * clipVolume, 1f + Random.Range(-effect.pitchVariation, effect.pitchVariation), delay);
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

        private void PlayClip(AudioClip clip, float volume, float pitch, float delay)
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
            source.time = StartTime(clip);
            if (delay > 0f)
                source.PlayDelayed(delay);
            else
                source.Play();
        }

        // Segundo en que el clip deja de ser silencio (se calcula una vez por clip).
        private float StartTime(AudioClip clip)
        {
            if (startTimes.TryGetValue(clip, out float start))
                return start;
            start = 0f;
            var samples = new float[clip.samples * clip.channels];
            // Solo funciona con clips descomprimidos al cargar (así los importa el generador); si no, empieza en 0.
            if (clip.loadType == AudioClipLoadType.DecompressOnLoad && clip.GetData(samples, 0))
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    if (Mathf.Abs(samples[i]) > SilenceThreshold)
                    {
                        start = Mathf.Max(0f, (float)(i / clip.channels) / clip.frequency - PreRoll);
                        break;
                    }
                }
            }
            startTimes[clip] = start;
            return start;
        }
    }
}
