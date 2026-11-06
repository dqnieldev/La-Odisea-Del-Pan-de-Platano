using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Sonido en bucle de un enemigo mientras se mueve (los pasitos de la rata, el zumbido de los jejenes): sube cuando se
    /// mueve, baja cuando se detiene y se apaga al morir o al estar el juego en pausa. Sigue el volumen de efectos. Suena
    /// más fuerte cerca del centro de la pantalla, para que muchos a la vez no saturen.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LoopingSound : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [Tooltip("Opcional: al morir se apaga.")]
        [SerializeField] private Health health;
        [Tooltip("Solo suena mientras se mueve (más rápido que esto, en unidades por segundo).")]
        [SerializeField, Min(0f)] private float minSpeed = 0.2f;
        [SerializeField, Min(0.01f)] private float fade = 0.15f;
        [Tooltip("A esta distancia horizontal de la cámara ya no se oye.")]
        [SerializeField, Min(1f)] private float hearingDistance = 16f;
        [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.08f;

        private AudioSource source;
        private Vector3 lastPosition;
        private float level;
        private float lastMoved = -1f;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            if (clip == null)
                return;
            // Cada uno empieza en otro punto del sonido, para que varios a la vez no suenen idénticos.
            source.time = Random.Range(0f, clip.length);
            source.Play();
        }

        private void Update()
        {
            // Los que se mueven con física (FixedUpdate) no cambian de lugar en cada cuadro: se toma como "moviéndose" si
            // avanzó en el último momento.
            float moved = (transform.position - lastPosition).magnitude;
            if (Time.deltaTime > 0f && moved / Mathf.Max(Time.deltaTime, Time.fixedDeltaTime) > minSpeed)
                lastMoved = Time.time;
            lastPosition = transform.position;
            bool moving = lastMoved >= 0f && Time.time - lastMoved < 0.12f;
            bool on = clip != null && moving && (health == null || !health.IsDead) && Time.timeScale > 0f;
            level = Mathf.MoveTowards(level, on ? 1f : 0f, Time.unscaledDeltaTime / fade);
            float distanceFactor = 1f;
            if (Camera.main != null)
                distanceFactor = 1f - Mathf.Clamp01(Mathf.Abs(transform.position.x - Camera.main.transform.position.x) / hearingDistance) * 0.6f;
            source.volume = volume * level * distanceFactor * GameSettings.EffectsVolume;
            if (health != null && health.IsDead && level <= 0f && source.isPlaying)
                source.Stop();
        }
    }
}
