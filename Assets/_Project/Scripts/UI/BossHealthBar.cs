using Odisea.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Barra de vida de un jefe, arriba al centro: aparece cuando el jefe entra a escena (se activa su objeto), baja
    /// poco a poco con cada golpe y se desvanece un momento después de que cae.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Tooltip("El jefe: mientras su objeto está activo y vivo, la barra se ve.")]
        [SerializeField] private Health boss;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image fill;
        [Tooltip("La parte que se queda atrás un momento y luego alcanza a la vida (para ver cuánto se perdió).")]
        [SerializeField] private Image trail;
        [SerializeField, Min(0.01f)] private float fadeTime = 0.4f;
        [SerializeField, Min(0f)] private float hideDelay = 1.5f;

        private float trailValue = 1f;
        private float trailWait;
        private float deadTime = -1f;

        private void Awake()
        {
            group.alpha = 0f;
            fill.fillAmount = trail.fillAmount = 1f;
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
            fill.fillAmount = max > 0 ? (float)current / max : 0f;
            trailWait = 0.4f;
        }

        private void OnDied() => deadTime = Time.time;

        private void Update()
        {
            bool shown = boss != null && boss.gameObject.activeInHierarchy && (deadTime < 0f || Time.time - deadTime < hideDelay);
            group.alpha = Mathf.MoveTowards(group.alpha, shown ? 1f : 0f, Time.deltaTime / fadeTime);
            if (trailWait > 0f)
                trailWait -= Time.deltaTime;
            else
                trailValue = Mathf.MoveTowards(trailValue, fill.fillAmount, Time.deltaTime * 0.8f);
            trail.fillAmount = trailValue;
        }
    }
}
