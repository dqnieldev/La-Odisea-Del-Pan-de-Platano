using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Sonido de ambiente en bucle (la lluvia de La Tovara): suena todo el nivel con el volumen de efectos, entra
    /// suave al empezar y se calla mientras el juego está en pausa.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbientLoop : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [SerializeField, Min(0.01f)] private float fade = 1.5f;

        private AudioSource source;
        private float level;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        private void OnEnable()
        {
            if (clip != null)
                source.Play();
        }

        private void Update()
        {
            bool on = clip != null && Time.timeScale > 0f;
            level = Mathf.MoveTowards(level, on ? 1f : 0f, Time.unscaledDeltaTime / fade);
            source.volume = volume * level * GameSettings.EffectsVolume;
        }
    }
}
