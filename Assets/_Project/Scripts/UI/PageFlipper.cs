using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Odisea.UI
{
    /// <summary>
    /// Hoja que da vuelta en el recetario. Reproduce solo el tramo del giro del video
    /// (de <see cref="startFrame"/> a <see cref="endFrame"/>) y avisa cuando la hoja ya pasó,
    /// para cambiar el contenido de la página justo ahí. El negro del video lo quita el shader LumaKey.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class PageFlipper : MonoBehaviour
    {
        [SerializeField] private RawImage display;
        [Tooltip("Cuadro del video donde empieza a levantarse la esquina.")]
        [SerializeField, Min(0)] private int startFrame = 20;
        [Tooltip("Cuadro en el que la hoja ya tapó la página derecha: ahí se cambia el contenido.")]
        [SerializeField, Min(0)] private int turnedFrame = 50;
        [Tooltip("Cuadro en el que la hoja terminó de caer a la izquierda.")]
        [SerializeField, Min(0)] private int endFrame = 82;
        [Tooltip("Velocidad del video. 2 = el giro dura la mitad (≈1 s).")]
        [SerializeField, Range(0.5f, 4f)] private float speed = 2f;
        [Tooltip("Fundido al aparecer y desaparecer la hoja.")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.12f;

        private VideoPlayer video;
        private RenderTexture target;
        private bool ready;

        public bool IsFlipping { get; private set; }

        private void Awake()
        {
            video = GetComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = false;
            video.skipOnDrop = true;
            video.audioOutputMode = VideoAudioOutputMode.None;
            // El menú de pausa detiene el tiempo del juego; la hoja debe girar igual.
            video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            video.renderMode = VideoRenderMode.RenderTexture;
            target = new RenderTexture(1920, 1080, 0) { name = "Hoja que da vuelta" };
            video.targetTexture = target;

            display.texture = target;
            SetAlpha(0f);
            display.enabled = false;
        }

        private void OnEnable()
        {
            // Se prepara al abrir Opciones para que el primer giro no tenga retraso.
            ready = false;
            video.prepareCompleted += OnPrepared;
            video.Prepare();
        }

        private void OnDisable()
        {
            video.prepareCompleted -= OnPrepared;
            StopAllCoroutines();
            video.Stop();
            IsFlipping = false;
            display.enabled = false;
        }

        private void OnDestroy()
        {
            if (target != null)
                target.Release();
        }

        /// <summary>
        /// Da vuelta la hoja. <paramref name="onPageTurned"/> se llama cuando la hoja tapa la página derecha
        /// (cambiar el contenido ahí) y <paramref name="onFinished"/> al terminar.
        /// Si el video no está listo, cambia el contenido sin animación.
        /// </summary>
        public void Flip(Action onPageTurned, Action onFinished)
        {
            if (IsFlipping)
                return;
            StartCoroutine(FlipRoutine(onPageTurned, onFinished));
        }

        private void OnPrepared(VideoPlayer player) => ready = true;

        private IEnumerator FlipRoutine(Action onPageTurned, Action onFinished)
        {
            IsFlipping = true;

            for (float waited = 0f; !ready && waited < 1f; waited += Time.unscaledDeltaTime)
                yield return null;
            if (!ready)
            {
                onPageTurned?.Invoke();
                IsFlipping = false;
                onFinished?.Invoke();
                yield break;
            }

            video.Pause();
            video.frame = startFrame;
            // Espera a que el cuadro inicial esté en la textura para no mostrar el final del giro anterior.
            for (float waited = 0f; video.frame != startFrame && waited < 0.5f; waited += Time.unscaledDeltaTime)
                yield return null;
            yield return null;

            display.enabled = true;
            yield return FadeTo(1f);

            // Normalmente se reproduce con Play() a la velocidad configurada. Si Unity mantiene el video en pausa
            // (lo hace cuando la ventana no tiene el foco), se avanza cuadro por cuadro con StepForward según el tiempo.
            video.playbackSpeed = speed;
            video.Play();
            float framesPerSecond = Mathf.Max(1f, (float)video.frameRate) * speed;
            // Tiempo máximo por si el video se traba: el giro siempre termina.
            float maxDuration = (endFrame - startFrame) / framesPerSecond * 2f + 0.5f;
            bool turned = false;
            for (float elapsed = 0f; video.frame < endFrame && elapsed < maxDuration; elapsed += Time.unscaledDeltaTime)
            {
                long targetFrame = startFrame + (long)(elapsed * framesPerSecond);
                if (!video.isPlaying && video.frame < targetFrame)
                    video.StepForward();
                if (!turned && video.frame >= turnedFrame)
                {
                    turned = true;
                    onPageTurned?.Invoke();
                }
                yield return null;
            }
            if (!turned)
                onPageTurned?.Invoke();
            video.Pause();

            yield return FadeTo(0f);
            display.enabled = false;
            IsFlipping = false;
            onFinished?.Invoke();
        }

        private IEnumerator FadeTo(float alpha)
        {
            float from = display.color.a;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, alpha, t / fadeDuration));
                yield return null;
            }
            SetAlpha(alpha);
        }

        private void SetAlpha(float alpha)
        {
            Color c = display.color;
            c.a = alpha;
            display.color = c;
        }
    }
}
