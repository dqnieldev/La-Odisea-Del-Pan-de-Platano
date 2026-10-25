using System.Collections;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Reacción visual al recibir daño: destello de color, "aplastón" y temblor.
    /// Opcionalmente se encoge y desaparece al morir (enemigos).
    /// Mueve solo el <see cref="visual"/>, así la física y los colliders no se alteran.
    /// </summary>
    public class DamageFeedback : MonoBehaviour
    {
        [SerializeField] private Health health;
        [Tooltip("Transform que se aplasta y tiembla. Debe ser un hijo con el sprite, no el objeto con la física.")]
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer[] renderers;

        [Header("Destello")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(0f)] private float flashDuration = 0.1f;

        [Header("Aplastón y temblor")]
        [Tooltip("Cuánto se ensancha y aplasta al recibir el golpe (0.25 = 25%).")]
        [SerializeField, Min(0f)] private float punchAmount = 0.25f;
        [SerializeField, Min(0f)] private float shakeAmount = 0.08f;
        [SerializeField, Min(0.01f)] private float punchDuration = 0.2f;

        [Header("Mando")]
        [Tooltip("Vibra el mando al recibir daño (solo para Ulises; respeta el ajuste Vibración de Opciones).")]
        [SerializeField] private bool rumble;

        [Header("Muerte")]
        [Tooltip("Al morir se encoge, se desvanece y se destruye el objeto.")]
        [SerializeField] private bool shrinkAndDestroyOnDeath;
        [SerializeField, Min(0.01f)] private float deathDuration = 0.3f;

        private Color[] originalColors;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private Coroutine hitRoutine;
        private Coroutine flashRoutine;

        private void Awake()
        {
            if (visual == null)
                visual = transform;
            baseScale = visual.localScale;
            basePosition = visual.localPosition;

            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalColors[i] = renderers[i].color;
        }

        private void OnEnable()
        {
            if (health == null)
                return;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health == null)
                return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void OnDamaged(DamageInfo info)
        {
            Restart(ref flashRoutine, Flash());
            Restart(ref hitRoutine, PunchAndShake());
            if (rumble)
                GamepadRumble.Play(0.35f, 0.75f, 0.2f);
        }

        private void OnDied()
        {
            if (!shrinkAndDestroyOnDeath)
                return;

            // Evita más golpes mientras se desvanece.
            foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
                col.enabled = false;
            Restart(ref hitRoutine, ShrinkAndDestroy());
        }

        private void Restart(ref Coroutine routine, IEnumerator next)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(next);
        }

        private IEnumerator Flash()
        {
            SetColors(_ => flashColor);
            yield return new WaitForSeconds(flashDuration);
            SetColors(i => originalColors[i]);
            flashRoutine = null;
        }

        private IEnumerator PunchAndShake()
        {
            for (float t = 0f; t < punchDuration; t += Time.deltaTime)
            {
                float strength = 1f - t / punchDuration;
                float punch = punchAmount * strength;
                visual.localScale = new Vector3(baseScale.x * (1f + punch), baseScale.y * (1f - punch), baseScale.z);
                visual.localPosition = basePosition + (Vector3)(Random.insideUnitCircle * shakeAmount * strength);
                yield return null;
            }
            visual.localScale = baseScale;
            visual.localPosition = basePosition;
            hitRoutine = null;
        }

        private IEnumerator ShrinkAndDestroy()
        {
            yield return new WaitForSeconds(flashDuration);
            for (float t = 0f; t < deathDuration; t += Time.deltaTime)
            {
                float progress = t / deathDuration;
                visual.localScale = baseScale * (1f - progress);
                visual.localRotation = Quaternion.Euler(0f, 0f, 90f * progress);
                SetColors(i =>
                {
                    Color c = originalColors[i];
                    c.a *= 1f - progress;
                    return c;
                });
                yield return null;
            }
            Destroy(gameObject);
        }

        private void SetColors(System.Func<int, Color> colorFor)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = colorFor(i);
            }
        }
    }
}
