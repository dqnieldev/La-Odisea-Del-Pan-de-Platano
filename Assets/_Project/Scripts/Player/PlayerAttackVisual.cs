using System.Collections;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Estela del golpe: una media luna que barre hacia adelante y se desvanece, del tamaño exacto del área
    /// que daña (su orilla de enfrente es hasta donde llega el golpe). Sin arte de ataque, además gira
    /// un rodillo de placeholder frente a Ulises.
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
        [SerializeField, Min(0f)] private float trailFadeDuration = 0.22f;
        [Tooltip("Ancho con el que empieza la estela (1 = área completa); crece hasta 1 para que se vea el barrido.")]
        [SerializeField, Range(0.1f, 1f)] private float trailStartWidth = 0.55f;
        [Tooltip("Segundos que tarda la estela en barrer hasta el borde del área.")]
        [SerializeField, Min(0.01f)] private float trailSweepDuration = 0.06f;

        private Color trailColor;
        private Vector3 trailScale;
        private Coroutine routine;

        private void Awake()
        {
            if (trail != null)
            {
                trailColor = trail.color;
                trailScale = trail.transform.localScale;
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

                // La estela crece desde Ulises hasta el borde del área (pivote a la izquierda) y luego se apaga.
                float sweep = Mathf.Clamp01(t / trailSweepDuration);
                float sweepEased = 1f - (1f - sweep) * (1f - sweep);
                Vector3 scale = trailScale;
                scale.x *= Mathf.Lerp(trailStartWidth, 1f, sweepEased);
                trail.transform.localScale = scale;

                // Completa mientras barre; después se desvanece hasta trailFadeDuration.
                float fade = Mathf.InverseLerp(trailSweepDuration, trailFadeDuration, t);
                Color c = trailColor;
                c.a *= 1f - fade * fade;
                trail.color = c;
                yield return null;
            }

            swingPivot.gameObject.SetActive(false);
            trail.enabled = false;
            trail.transform.localScale = trailScale;
            routine = null;
        }
    }
}
