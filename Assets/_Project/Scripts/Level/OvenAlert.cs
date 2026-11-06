using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Aviso de peligro del horno: cuando un enemigo vivo está muy cerca del horno, aparece un signo "!" rojo arriba del
    /// horno, del lado por donde viene (izquierda o derecha; los dos si vienen por ambos lados). Late para llamar la
    /// atención y se va cuando ya no hay nadie cerca.
    /// </summary>
    public class OvenAlert : MonoBehaviour
    {
        [SerializeField] private Oven oven;
        [Tooltip("Los signos de cada lado (empiezan apagados).")]
        [SerializeField] private Transform leftMark;
        [SerializeField] private Transform rightMark;
        [SerializeField] private LayerMask enemyLayers;
        [Tooltip("Qué tanto más allá de la orilla del horno cuenta como \"muy cerca\".")]
        [SerializeField, Min(0f)] private float distance = 0.9f;
        [Tooltip("Altura que se revisa (desde el piso): incluye a los jejenes que vuelan hacia la boca.")]
        [SerializeField, Min(0f)] private float height = 6f;
        [SerializeField, Min(0f)] private float pulseSpeed = 9f;

        private readonly Collider2D[] hits = new Collider2D[24];
        private Vector3 leftScale, rightScale;

        private void Awake()
        {
            if (leftMark != null)
            {
                leftScale = leftMark.localScale;
                leftMark.gameObject.SetActive(false);
            }
            if (rightMark != null)
            {
                rightScale = rightMark.localScale;
                rightMark.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            bool left = false, right = false;
            if (oven != null && !oven.Burned)
            {
                Vector2 center = oven.transform.position;
                float reach = oven.HalfWidth + distance;
                var area = new Vector2(reach * 2f, height);
                int count = Physics2D.OverlapBox(center + new Vector2(0f, height / 2f), area, 0f,
                    new ContactFilter2D { useLayerMask = true, layerMask = enemyLayers, useTriggers = true }, hits);
                for (int i = 0; i < count; i++)
                {
                    Health health = hits[i].GetComponentInParent<Health>();
                    if (health == null || health.IsDead)
                        continue;
                    if (hits[i].bounds.center.x < center.x)
                        left = true;
                    else
                        right = true;
                }
            }
            float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * 0.25f;
            Show(leftMark, left, leftScale * pulse);
            Show(rightMark, right, rightScale * pulse);
        }

        private static void Show(Transform mark, bool on, Vector3 scale)
        {
            if (mark == null)
                return;
            if (mark.gameObject.activeSelf != on)
                mark.gameObject.SetActive(on);
            if (on)
                mark.localScale = scale;
        }
    }
}
