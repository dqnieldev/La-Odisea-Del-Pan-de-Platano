using System.Collections;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Costal de harina de la pelea con Polifemo. Si Ulises le pega (con el rodillo o la bolita de masa), revienta y suelta
    /// una nube de harina: si Polifemo está dentro de la nube mientras dura, se marea (<see cref="PolifemoBoss.Daze"/>).
    /// Queda vacío y tirado; se rellena al empezar otra ronda o, si no, después de un rato.
    /// Cuadros del dibujo: 0 lleno, 1 le pegan, 2 revienta, 3-5 la nube (cada vez más tenue), 6 vacío.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FlourSack : MonoBehaviour, IDamageable
    {
        [SerializeField] private SpriteRenderer drawing;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private PolifemoBoss boss;
        [Tooltip("Lo que dura cada cuadro del reventón (le pegan, revienta).")]
        [SerializeField, Min(0.01f)] private float burstFrameTime = 0.1f;
        [Tooltip("Lo que dura cada cuadro de la nube (3-5): mientras, marea a Polifemo.")]
        [SerializeField, Min(0.01f)] private float cloudFrameTime = 0.35f;
        [Tooltip("Centro de la nube respecto a la base del costal, y qué tan lejos alcanza.")]
        [SerializeField] private Vector2 cloudCenter = new(0f, 1.6f);
        [SerializeField, Min(0f)] private float cloudRadius = 2.6f;
        [Tooltip("Si no empieza otra ronda, se rellena solo después de esto.")]
        [SerializeField, Min(0f)] private float refillDelay = 8f;
        [SerializeField] private SoundEffect burstSound = new();

        private Collider2D area;
        private bool burst;
        private bool refillNow;

        private void Awake()
        {
            area = GetComponent<Collider2D>();
            area.isTrigger = true;
            if (frames != null && frames.Length > 0)
                drawing.sprite = frames[0];
        }

        private void OnEnable()
        {
            if (boss != null)
                boss.RoundStarted += OnRoundStarted;
        }

        private void OnDisable()
        {
            if (boss != null)
                boss.RoundStarted -= OnRoundStarted;
        }

        private void OnRoundStarted() => refillNow = true;

        public bool TakeDamage(DamageInfo info)
        {
            // Mientras Polifemo no ha empezado a pelear, o ya cayó, el costal no hace nada.
            if (burst || boss == null || !boss.Started || boss.Defeated)
                return false;
            StartCoroutine(Burst());
            return true;
        }

        private IEnumerator Burst()
        {
            burst = true;
            refillNow = false;
            burstSound.Play();
            for (int i = 1; i < frames.Length - 1; i++)
            {
                drawing.sprite = frames[i];
                bool cloud = i >= 3;
                float duration = cloud ? cloudFrameTime : burstFrameTime;
                for (float t = 0f; t < duration; t += Time.deltaTime)
                {
                    if (cloud && InCloud())
                        boss.Daze();
                    yield return null;
                }
            }
            drawing.sprite = frames[^1];

            float waited = 0f;
            while (!refillNow && waited < refillDelay)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            // Un costal nuevo: aparece con un saltito.
            drawing.sprite = frames[0];
            Vector3 scale = drawing.transform.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
            {
                drawing.transform.localScale = new Vector3(scale.x, scale.y * Mathf.Lerp(0.2f, 1f, t) * (1f + Mathf.Sin(t * Mathf.PI) * 0.15f), scale.z);
                yield return null;
            }
            drawing.transform.localScale = scale;
            burst = false;
        }

        private bool InCloud()
        {
            Vector2 center = (Vector2)transform.position + cloudCenter;
            // El cuerpo de Polifemo (su centro, a media altura).
            Vector2 target = (Vector2)boss.transform.position + new Vector2(0f, 1.6f);
            return Vector2.Distance(center, target) <= cloudRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere((Vector2)transform.position + cloudCenter, cloudRadius);
        }
    }
}
