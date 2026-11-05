using Odisea.Core;
using Odisea.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Barra de vida de Polifemo, arriba al centro: aparece al empezar la pelea, baja poco a poco con cada golpe y se
    /// desvanece después de que cae. Las rayitas marcan las rondas.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private PolifemoBoss boss;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image fill;
        [Tooltip("La parte que se queda atrás un momento y luego alcanza a la vida (para ver cuánto se perdió).")]
        [SerializeField] private Image trail;
        [SerializeField, Min(0.01f)] private float fadeTime = 0.4f;
        [SerializeField, Min(0f)] private float hideDelay = 1.5f;

        private float shown;
        private float trailValue = 1f;
        private float trailWait;
        private float hideTimer = -1f;

        private void Awake()
        {
            group.alpha = 0f;
            fill.fillAmount = trail.fillAmount = 1f;
        }

        private void OnEnable()
        {
            if (boss == null)
                return;
            boss.FightStarted += OnStarted;
            boss.Fell += OnFell;
            boss.Health.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            if (boss == null)
                return;
            boss.FightStarted -= OnStarted;
            boss.Fell -= OnFell;
            if (boss.Health != null)
                boss.Health.HealthChanged -= OnHealthChanged;
        }

        private void OnStarted() => shown = 1f;

        private void OnFell() => hideTimer = hideDelay;

        private void OnHealthChanged(int current, int max)
        {
            fill.fillAmount = max > 0 ? (float)current / max : 0f;
            trailWait = 0.4f;
        }

        private void Update()
        {
            if (hideTimer >= 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer < 0f)
                    shown = 0f;
            }
            group.alpha = Mathf.MoveTowards(group.alpha, shown, Time.deltaTime / fadeTime);
            if (trailWait > 0f)
                trailWait -= Time.deltaTime;
            else
                trailValue = Mathf.MoveTowards(trailValue, fill.fillAmount, Time.deltaTime * 0.8f);
            trail.fillAmount = trailValue;
        }
    }
}
