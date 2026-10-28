using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Efecto del escudo de hoja de plátano: hojitas que giran alrededor de Ulises en un óvalo, como si fuera en 3D
    /// (por detrás de él pasan detrás del dibujo y por delante, enfrente). Al romperse el escudo salen volando
    /// y caen girando.
    /// </summary>
    public class ShieldLeaves : MonoBehaviour
    {
        [SerializeField] private PlayerBuffs buffs;
        [Tooltip("Hojitas (hijos). Se reparten parejo alrededor del óvalo.")]
        [SerializeField] private SpriteRenderer[] leaves = new SpriteRenderer[0];
        [Tooltip("Centro del óvalo respecto a Ulises (el centro de su cuerpo).")]
        [SerializeField] private Vector2 center = new(0f, 0.2f);
        [Tooltip("Ancho y alto del óvalo (el alto es la inclinación: cuánto sube y baja al dar la vuelta).")]
        [SerializeField] private Vector2 radius = new(0.85f, 0.3f);
        [SerializeField] private float degreesPerSecond = 200f;
        [Tooltip("Tamaño de cada hojita (escala del dibujo de la hoja).")]
        [SerializeField, Min(0.01f)] private float leafSize = 0.4f;
        [Tooltip("Orden de dibujo de Ulises: las hojitas van una abajo (detrás) o una arriba (delante).")]
        [SerializeField] private int playerSortingOrder = 10;

        private float angle;

        private void Awake() => Show(false);

        private void OnEnable()
        {
            if (buffs != null) buffs.ShieldBroken += Scatter;
        }

        private void OnDisable()
        {
            if (buffs != null) buffs.ShieldBroken -= Scatter;
        }

        private void LateUpdate()
        {
            bool shown = buffs != null && buffs.ShieldShown;
            Show(shown);
            if (!shown)
                return;
            angle += degreesPerSecond * Time.deltaTime;
            for (int i = 0; i < leaves.Length; i++)
            {
                float a = (angle + 360f * i / leaves.Length) * Mathf.Deg2Rad;
                float depth = Mathf.Sin(a); // > 0: de este lado (delante de Ulises).
                Transform leaf = leaves[i].transform;
                leaf.localPosition = new Vector3(center.x + Mathf.Cos(a) * radius.x, center.y - depth * radius.y, 0f);
                // Más chica atrás y más grande adelante, y se mece hacia donde va.
                leaf.localScale = Vector3.one * (leafSize * Mathf.Lerp(0.8f, 1.1f, (depth + 1f) * 0.5f));
                leaf.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(a * 2f) * 25f - Mathf.Cos(a) * 20f);
                leaves[i].sortingOrder = depth > 0f ? playerSortingOrder + 1 : playerSortingOrder - 1;
            }
        }

        private void Show(bool shown)
        {
            foreach (SpriteRenderer leaf in leaves)
                leaf.enabled = shown;
        }

        // Cada hojita sale disparada hacia afuera de donde estaba y cae girando, como hoja seca.
        private void Scatter()
        {
            foreach (SpriteRenderer leaf in leaves)
            {
                Vector3 position = leaf.transform.position;
                Vector2 outward = ((Vector2)(position - transform.position) - center).normalized;
                FadingSprite.Spawn(leaf.sprite, position, leaf.color, playerSortingOrder + 1, 0.8f)
                    .Move(outward * 3f + Vector2.up * 2.5f, gravityAcceleration: 9f, spinSpeed: Random.Range(-540f, 540f))
                    .transform.localScale = leaf.transform.lossyScale;
            }
            Show(false);
        }
    }
}
