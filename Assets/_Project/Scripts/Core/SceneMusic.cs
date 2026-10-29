using System;
using System.Collections;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Música de fondo de una escena (el menú principal o un nivel): entra con un fundido suave, se repite en loop,
    /// sigue el volumen de música de Opciones y se desvanece en cuanto empieza un cambio de escena, mientras se
    /// cierra el iris. En un nivel, opcionalmente se apaga rápido cuando muere Ulises (para que se oiga la muerte).
    /// Si va a una escena de <see cref="continueInto"/> (la del menú: partidas, historia, mapa) sigue sonando sin
    /// cortarse, y esa escena, al traer la misma canción, deja la que ya sonaba.
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
        [Tooltip("Escenas a las que la canción pasa sin cortarse (por nombre). A las demás se desvanece.")]
        [SerializeField] private string[] continueInto = Array.Empty<string>();

        [Header("En pausa: más bajito y como disco viejo")]
        [SerializeField, Range(0f, 1f)] private float pausedVolume = 0.55f;
        [Tooltip("Corta los agudos (Hz): suena apagado, como un radio antiguo.")]
        [SerializeField, Min(100f)] private float pausedLowPass = 1600f;
        [Tooltip("Corta los graves (Hz): suena delgado, como un gramófono.")]
        [SerializeField, Min(10f)] private float pausedHighPass = 450f;
        [SerializeField, Min(0.01f)] private float pauseTransition = 0.35f;

        // La que está sonando ahora (puede venir de una escena anterior).
        private static SceneMusic playing;
        // Ya pasó a otra escena: sobrevive al cambio y se destruye al desvanecerse.
        private bool carried;

        private AudioSource source;
        // Multiplicador del fundido (0 a 1); el volumen final es fundido × volumen de música.
        private float fade;
        private Coroutine fadeRoutine;
        private AudioLowPassFilter lowPass;
        private AudioHighPassFilter highPass;
        // 0 = normal, 1 = en pausa (disco viejo); se mueve suave hacia la meta.
        private float muffle;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            // Ya suena la misma canción desde la escena anterior: esta no hace nada y se quita.
            if (playing != null && playing != this && playing.source.clip == source.clip && source.clip != null)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            highPass = gameObject.AddComponent<AudioHighPassFilter>();
            muffle = GamePause.IsPaused ? 1f : 0f;
            ApplyMuffle();
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
            playing = this;
            source.Play();
            FadeTo(1f, fadeInDuration);
        }

        private void OnDestroy()
        {
            if (playing == this)
                playing = null;
        }

        private void OnMusicVolumeChanged(float _) => ApplyVolume();

        // Con el tiempo detenido de la pausa, por eso en tiempo real.
        private void Update()
        {
            float target = GamePause.IsPaused ? 1f : 0f;
            if (Mathf.Approximately(muffle, target))
                return;
            muffle = Mathf.MoveTowards(muffle, target, Time.unscaledDeltaTime / pauseTransition);
            ApplyMuffle();
            ApplyVolume();
        }

        private void ApplyMuffle()
        {
            if (lowPass == null)
                return;
            // Sin pausa los filtros se apagan del todo (no gastan ni colorean el sonido).
            lowPass.enabled = highPass.enabled = muffle > 0f;
            // El corte baja en escala logarítmica: así el cambio se oye parejo.
            lowPass.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(22000f), Mathf.Log(pausedLowPass), muffle));
            highPass.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(10f), Mathf.Log(pausedHighPass), muffle));
        }

        private void OnLoadStarted(string sceneName)
        {
            if (Array.IndexOf(continueInto, sceneName) >= 0)
            {
                if (!carried)
                {
                    carried = true;
                    transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
                return;
            }
            FadeTo(0f, fadeOutDuration);
        }

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
            fadeRoutine = null;
            if (target <= 0f)
            {
                source.Stop();
                // Venía de otra escena: ya no pertenece a ninguna.
                if (carried)
                    Destroy(gameObject);
            }
        }

        private void ApplyVolume() => source.volume = fade * GameSettings.MusicVolume * Mathf.Lerp(1f, pausedVolume, muffle);
    }
}
