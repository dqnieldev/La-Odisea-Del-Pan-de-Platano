using Odisea.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Barra de vida de un jefe (abajo al centro), con su cara y su nombre:
    /// <list type="bullet">
    /// <item>Entra cuando el jefe aparece (se activa su objeto): sube desde abajo y la vida se llena de 0 a completa.</item>
    /// <item>Con cada golpe parpadea en blanco, da un salto y la cara se encoge; lo perdido se queda un momento en
    /// amarillo y luego baja.</item>
    /// <item>Al llegar a <see cref="rageAt"/> de vida (cuando se enoja) cambia el nombre, la vida late en rojo intenso
    /// y la cara tiembla.</item>
    /// <item>Al caer, tiembla fuerte, se vacía y se va hacia abajo desvaneciéndose.</item>
    /// </list>
    /// Todo lo opcional (cara, destello, nombre) puede faltar.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Tooltip("El jefe: mientras su objeto está activo y vivo, la barra se ve.")]
        [SerializeField] private Health boss;
        [SerializeField] private CanvasGroup group;
        [Tooltip("Lo que entra, salta y tiembla (la barra completa con la cara).")]
        [SerializeField] private RectTransform content;
        [SerializeField] private Image fill;
        [Tooltip("La parte que se queda atrás un momento y luego alcanza a la vida (para ver cuánto se perdió).")]
        [SerializeField] private Image trail;
        [Tooltip("Opcional: blanco encima de la vida, para el parpadeo de cada golpe.")]
        [SerializeField] private Image flash;
        [SerializeField] private RectTransform portrait;
        [SerializeField] private TMP_Text nameText;

        [Header("Enojo")]
        [Tooltip("Fracción de vida a la que se enoja (la marca de la barra).")]
        [SerializeField, Range(0f, 1f)] private float rageAt = 0.5f;
        [SerializeField] private string calmName = "POLIFEMO";
        [SerializeField] private string ragedName = "¡POLIFEMO ENOJADO!";
        [SerializeField] private Color calmColor = new(0.82f, 0.24f, 0.16f);
        [SerializeField] private Color ragedColor = new(0.95f, 0.12f, 0.1f);
        [SerializeField] private Color ragedPulse = new(1f, 0.45f, 0.25f);

        [Header("Tiempos")]
        [SerializeField, Min(0.01f)] private float fadeTime = 0.4f;
        [SerializeField, Min(0.01f)] private float introDuration = 1.2f;
        [Tooltip("Desde abajo de la pantalla hasta su lugar.")]
        [SerializeField] private float introRise = 160f;
        [SerializeField, Min(0f)] private float hideDelay = 1.5f;
        [SerializeField, Min(0f)] private float trailDelay = 0.6f;

        private Vector2 home;
        private float shownTime = -1f;
        private float deadTime = -1f;
        private float target = 1f;
        private float trailValue = 1f;
        private float trailWait;
        private float hitTime = -10f;
        private bool raged;

        private void Awake()
        {
            group.alpha = 0f;
            if (content != null)
                home = content.anchoredPosition;
            fill.fillAmount = trail.fillAmount = 0f;
            trailValue = 0f;
            if (flash != null)
                flash.color = new Color(1f, 1f, 1f, 0f);
            if (nameText != null)
                nameText.text = calmName;
            fill.color = calmColor;
        }

        private void OnEnable()
        {
            if (boss == null)
                return;
            boss.HealthChanged += OnHealthChanged;
            boss.Died += OnDied;
        }

        private void OnDisable()
        {
            if (boss == null)
                return;
            boss.HealthChanged -= OnHealthChanged;
            boss.Died -= OnDied;
        }

        private void OnHealthChanged(int current, int max)
        {
            float value = max > 0 ? (float)current / max : 0f;
            if (value < target)
            {
                hitTime = Time.time;
                trailWait = trailDelay;
            }
            target = value;
            if (!raged && current > 0 && value <= rageAt + 0.0001f)
                Rage();
        }

        private void Rage()
        {
            raged = true;
            if (nameText != null)
                nameText.text = ragedName;
        }

        private void OnDied()
        {
            deadTime = Time.time;
            hitTime = Time.time;
            target = 0f;
        }

        private void Update()
        {
            float now = Time.time;
            bool active = boss != null && boss.gameObject.activeInHierarchy;
            if (active && shownTime < 0f)
                shownTime = now;
            bool shown = active && (deadTime < 0f || now - deadTime < hideDelay);
            group.alpha = Mathf.MoveTowards(group.alpha, shown ? 1f : 0f, Time.deltaTime / fadeTime);
            if (shownTime < 0f)
                return;

            // Entrada: sube desde abajo y la vida se llena; después sigue a la vida real.
            float intro = Mathf.Clamp01((now - shownTime) / introDuration);
            float eased = 1f - (1f - intro) * (1f - intro);
            float value = intro < 1f ? Mathf.Min(target, eased) : target;
            fill.fillAmount = value;
            if (intro < 1f)
                trailValue = value;
            else if (trailWait > 0f)
                trailWait -= Time.deltaTime;
            else
                trailValue = Mathf.MoveTowards(trailValue, value, Time.deltaTime * 0.6f);
            trail.fillAmount = Mathf.Max(trailValue, value);

            // Golpe: destello blanco, salto y la cara se encoge un instante.
            float hit = Mathf.Clamp01(1f - (now - hitTime) / 0.25f);
            if (flash != null)
            {
                flash.fillAmount = value;
                flash.color = new Color(1f, 1f, 1f, hit * 0.85f);
            }
            Vector2 offset = new(0f, -introRise * (1f - eased));
            float shake = hit * 7f;
            if (deadTime >= 0f)
            {
                // Al caer: tiembla fuerte y se va hacia abajo.
                float gone = Mathf.Clamp01((now - deadTime) / hideDelay);
                shake = 10f * (1f - gone);
                offset.y -= introRise * gone * gone;
            }
            else if (raged)
                shake = Mathf.Max(shake, 1.5f);
            if (shake > 0f)
                offset += Random.insideUnitCircle * shake;
            if (content != null)
                content.anchoredPosition = home + offset;
            if (portrait != null)
            {
                float squash = 1f - 0.15f * hit;
                portrait.localScale = new Vector3(squash, squash, 1f);
                portrait.localRotation = raged && deadTime < 0f
                    ? Quaternion.Euler(0f, 0f, Mathf.Sin(now * 30f) * 4f)
                    : Quaternion.identity;
            }

            // Enojado: la vida late en rojo intenso.
            fill.color = raged ? Color.Lerp(ragedColor, ragedPulse, (Mathf.Sin(now * 9f) + 1f) * 0.5f * 0.6f) : calmColor;
        }
    }
}
