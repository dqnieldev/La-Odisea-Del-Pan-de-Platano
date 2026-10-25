using System.Collections;
using Odisea.Core;
using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Música del menú principal: entra con un fundido suave, se repite en loop, sigue el volumen de música
    /// de Opciones y se desvanece en cuanto empieza un cambio de escena (Jugar, Tutorial, Ir al título),
    /// mientras se cierra el iris.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MenuMusic : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float fadeInDuration = 1.2f;
        [Tooltip("Debe ser parecido a lo que tarda en cerrarse el iris para que la música termine junto con la imagen.")]
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.9f;

        private AudioSource source;
        // Multiplicador del fundido (0 a 1); el volumen final es fundido × volumen de música.
        private float fade;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            fade = 0f;
            ApplyVolume();
        }

        private void OnEnable()
        {
            GameSettings.MusicVolumeChanged += OnMusicVolumeChanged;
            SceneFlow.LoadStarted += OnLoadStarted;
        }

        private void OnDisable()
        {
            GameSettings.MusicVolumeChanged -= OnMusicVolumeChanged;
            SceneFlow.LoadStarted -= OnLoadStarted;
        }

        private void Start()
        {
            if (source.clip == null)
                return;
            source.Play();
            FadeTo(1f, fadeInDuration);
        }

        private void OnMusicVolumeChanged(float _) => ApplyVolume();

        private void OnLoadStarted(string sceneName) => FadeTo(0f, fadeOutDuration);

        private void FadeTo(float target, float duration)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(target, duration));
        }

        private IEnumerator FadeRoutine(float target, float duration)
        {
            float from = fade;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                fade = Mathf.Lerp(from, target, t / duration);
                ApplyVolume();
                yield return null;
            }
            fade = target;
            ApplyVolume();
            if (target <= 0f)
                source.Stop();
            fadeRoutine = null;
        }

        private void ApplyVolume() => source.volume = fade * GameSettings.MusicVolume;
    }
}
