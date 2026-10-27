using System.Collections;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Música de fondo de una escena (el menú principal o un nivel): entra con un fundido suave, se repite en loop,
    /// sigue el volumen de música de Opciones y se desvanece en cuanto empieza un cambio de escena, mientras se
    /// cierra el iris. En un nivel, opcionalmente se apaga rápido cuando muere Ulises (para que se oiga la muerte).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SceneMusic : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float fadeInDuration = 1.2f;
        [Tooltip("Debe ser parecido a lo que tarda en cerrarse el iris para que la música termine junto con la imagen.")]
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.9f;
        [Tooltip("Opcional (niveles): la vida del jugador. Al morir, la música se apaga en deathFadeOut segundos.")]
        [SerializeField] private Health fadeOnDeath;
        [SerializeField, Min(0.01f)] private float deathFadeOut = 0.5f;

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
            if (fadeOnDeath != null)
                fadeOnDeath.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameSettings.MusicVolumeChanged -= OnMusicVolumeChanged;
            SceneFlow.LoadStarted -= OnLoadStarted;
            if (fadeOnDeath != null)
                fadeOnDeath.Died -= OnPlayerDied;
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

        private void OnPlayerDied() => FadeTo(0f, deathFadeOut);

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
