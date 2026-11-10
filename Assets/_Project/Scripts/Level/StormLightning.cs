using Odisea.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Odisea.Level
{
    /// <summary>
    /// Tormenta de noche (La Tovara): cada tanto cae un relámpago. La luz global destella dos veces (un golpe fuerte y
    /// uno más débil) y el cielo se aclara un instante con un velo blanco encima; luego vuelve a la luz de noche.
    /// </summary>
    public class StormLightning : MonoBehaviour
    {
        [SerializeField] private Light2D globalLight;
        [Tooltip("Velo blanco encima del cielo (empieza transparente).")]
        [SerializeField] private SpriteRenderer skyFlash;
        [SerializeField] private Color nightColor = new(0.6f, 0.68f, 1f);
        [SerializeField, Min(0f)] private float nightIntensity = 0.72f;
        [SerializeField] private Color flashColor = new(0.9f, 0.93f, 1f);
        [SerializeField, Min(0f)] private float flashIntensity = 1.5f;
        [Tooltip("Segundos entre relámpagos (al azar entre los dos).")]
        [SerializeField] private Vector2 interval = new(6f, 14f);
        [SerializeField, Range(0f, 1f)] private float skyFlashAlpha = 0.4f;
        [Tooltip("Opcional: el trueno.")]
        [SerializeField] private SoundEffect thunder = new();

        private float nextStrike;
        private float strikeAt = -10f;

        private void Start()
        {
            nextStrike = Time.time + Random.Range(2f, interval.x);
            Apply(0f);
        }

        private void Update()
        {
            if (Time.time >= nextStrike)
            {
                strikeAt = Time.time;
                nextStrike = Time.time + Random.Range(interval.x, interval.y);
                thunder.Play();
            }
            // Dos destellos: uno fuerte al empezar y otro más débil a los 0.18 s; se apagan rápido.
            float t = Time.time - strikeAt;
            float flash = Mathf.Max(Pulse(t, 0f, 0.12f), 0.6f * Pulse(t, 0.18f, 0.22f));
            Apply(flash);
        }

        private static float Pulse(float t, float start, float length)
        {
            float local = (t - start) / length;
            return local < 0f || local > 1f ? 0f : 1f - local;
        }

        private void Apply(float flash)
        {
            if (globalLight != null)
            {
                globalLight.color = Color.Lerp(nightColor, flashColor, flash);
                globalLight.intensity = Mathf.Lerp(nightIntensity, flashIntensity, flash);
            }
            if (skyFlash != null)
            {
                Color color = skyFlash.color;
                color.a = skyFlashAlpha * flash;
                skyFlash.color = color;
            }
        }
    }
}
