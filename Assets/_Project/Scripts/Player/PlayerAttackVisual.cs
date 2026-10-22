using System.Collections;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Muestra el golpe mientras no hay animación real: el rodillo gira frente a Ulises
    /// y una estela marca el área que daña. Cuando llegue el sprite de ataque se puede quitar.
    /// </summary>
    public class PlayerAttackVisual : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        [Tooltip("Pivote del rodillo (la mano de Ulises). Gira en Z durante el golpe.")]
        [SerializeField] private Transform swingPivot;
        [Tooltip("Sprite que marca el área de daño.")]
        [SerializeField] private SpriteRenderer trail;

        [SerializeField] private float startAngle = 110f;
        [SerializeField] private float endAngle = -40f;
        [SerializeField, Min(0.01f)] private float swingDuration = 0.14f;
        [SerializeField, Min(0f)] private float trailFadeDuration = 0.18f;

        private Color trailColor;
        private Coroutine routine;

        private void Awake()
        {
            if (trail != null)
            {
                trailColor = trail.color;
                trail.enabled = false;
            }
            if (swingPivot != null)
                swingPivot.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (combat != null)
                combat.Attacked += OnAttacked;
        }

        private void OnDisable()
        {
            if (combat != null)
                combat.Attacked -= OnAttacked;
        }

        private void OnAttacked()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Swing());
        }

        private IEnumerator Swing()
        {
            swingPivot.gameObject.SetActive(true);
            trail.enabled = true;

            float total = Mathf.Max(swingDuration, trailFadeDuration);
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                float swing = Mathf.Clamp01(t / swingDuration);
                // Ease-out: arranca rápido y frena al final, se siente más como un golpe.
                float eased = 1f - (1f - swing) * (1f - swing);
                swingPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, endAngle, eased));

                Color c = trailColor;
                c.a *= 1f - Mathf.Clamp01(t / trailFadeDuration);
                trail.color = c;
                yield return null;
            }

            swingPivot.gameObject.SetActive(false);
            trail.enabled = false;
            routine = null;
        }
    }
}
