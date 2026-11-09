using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Odisea.Level
{
    /// <summary>
    /// Anochece mientras se hornea el pan (nivel 2): la luz global pasa del atardecer a un azul de noche, el patio de noche
    /// se va mezclando encima del de día y el fuego del horno alumbra cada vez más (titilando). Cuando el pan está listo
    /// ya es de noche.
    /// </summary>
    public class Nightfall : MonoBehaviour
    {
        [SerializeField] private BakeTimer bake;
        [SerializeField] private Light2D globalLight;
        [Tooltip("Opcional: el patio de noche, encima del de día (empieza transparente).")]
        [SerializeField] private SpriteRenderer nightBackdrop;
        [Tooltip("Opcional: la luz del fuego del horno (empieza apagada).")]
        [SerializeField] private Light2D ovenLight;
        [Tooltip("En qué parte del horneado empieza a oscurecer y cuándo ya es de noche (0 a 1).")]
        [SerializeField] private Vector2 span = new(0.1f, 0.9f);
        [SerializeField] private Color dayColor = Color.white;
        [SerializeField, Min(0f)] private float dayIntensity = 1f;
        [SerializeField] private Color nightColor = new(0.62f, 0.68f, 1f);
        [SerializeField, Min(0f)] private float nightIntensity = 0.7f;
        [SerializeField, Min(0f)] private float ovenLightIntensity = 1.6f;
        [SerializeField, Range(0f, 1f)] private float flicker = 0.18f;
        [Tooltip("Opcional: resplandor cálido alrededor del horno (un dibujo difuso detrás de él), que aparece al anochecer.")]
        [SerializeField] private SpriteRenderer ovenHalo;
        [SerializeField, Range(0f, 1f)] private float haloAlpha = 0.55f;

        private void Update()
        {
            if (bake == null)
                return;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(span.x, span.y, bake.Progress));
            if (globalLight != null)
            {
                globalLight.color = Color.Lerp(dayColor, nightColor, t);
                globalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
            }
            if (nightBackdrop != null)
            {
                Color color = nightBackdrop.color;
                color.a = t;
                nightBackdrop.color = color;
            }
            float noise = Mathf.PerlinNoise(Time.time * 6f, 0.3f) - 0.5f;
            float flame = 1f + noise * flicker * 2f;
            if (ovenLight != null)
                ovenLight.intensity = ovenLightIntensity * Mathf.Lerp(0.15f, 1f, t) * flame;
            if (ovenHalo != null)
            {
                Color color = ovenHalo.color;
                color.a = haloAlpha * t * flame;
                ovenHalo.color = color;
            }
        }
    }
}
