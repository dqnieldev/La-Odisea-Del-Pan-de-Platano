using System.Collections.Generic;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Aviso de peligro del horno: cada enemigo vivo que está muy cerca del horno lleva encima un signo "!" rojo que lo
    /// sigue (también los jejenes en el aire). Late para llamar la atención y se quita cuando el enemigo se aleja o cae.
    /// </summary>
    public class OvenAlert : MonoBehaviour
    {
        [SerializeField] private Oven oven;
        [Tooltip("El signo de muestra (apagado): se copia uno para cada enemigo.")]
        [SerializeField] private Transform markTemplate;
        [SerializeField] private LayerMask enemyLayers;
        [Tooltip("Qué tanto más allá de la orilla del horno cuenta como \"muy cerca\".")]
        [SerializeField, Min(0f)] private float distance = 0.9f;
        [Tooltip("Altura que se revisa (desde el piso): incluye a los jejenes que vuelan hacia la boca.")]
        [SerializeField, Min(0f)] private float height = 6f;
        [Tooltip("Cuánto arriba del enemigo va el signo.")]
        [SerializeField, Min(0f)] private float lift = 1f;
        [SerializeField, Min(0f)] private float pulseSpeed = 9f;

        private readonly Collider2D[] hits = new Collider2D[24];
        private readonly Dictionary<Health, Transform> marks = new();
        private readonly HashSet<Health> near = new();
        private readonly Stack<Transform> spare = new();
        private readonly List<Health> gone = new();
        private Vector3 baseScale = Vector3.one;

        private void Awake()
        {
            if (markTemplate == null)
                return;
            baseScale = markTemplate.localScale;
            markTemplate.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            near.Clear();
            if (oven != null && !oven.Burned && markTemplate != null)
            {
                Vector2 center = oven.transform.position;
                float reach = oven.HalfWidth + distance;
                int count = Physics2D.OverlapBox(center + new Vector2(0f, height / 2f), new Vector2(reach * 2f, height), 0f,
                    new ContactFilter2D { useLayerMask = true, layerMask = enemyLayers, useTriggers = true }, hits);
                for (int i = 0; i < count; i++)
                {
                    Health health = hits[i].GetComponentInParent<Health>();
                    if (health == null || health.IsDead || !near.Add(health))
                        continue;
                    if (!marks.TryGetValue(health, out Transform mark))
                        marks[health] = mark = Take();
                    // Justo arriba de lo más alto del enemigo, siguiéndolo.
                    Bounds bounds = hits[i].bounds;
                    mark.position = new Vector3(bounds.center.x, bounds.max.y + lift, mark.position.z);
                }
            }
            // Los que ya no están cerca (o cayeron, o desaparecieron) sueltan su signo.
            gone.Clear();
            foreach (KeyValuePair<Health, Transform> pair in marks)
            {
                if (pair.Key == null || !near.Contains(pair.Key))
                    gone.Add(pair.Key);
            }
            foreach (Health health in gone)
            {
                Transform mark = marks[health];
                marks.Remove(health);
                mark.gameObject.SetActive(false);
                spare.Push(mark);
            }
            Vector3 scale = baseScale * (1f + Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * 0.2f);
            foreach (Transform mark in marks.Values)
                mark.localScale = scale;
        }

        private Transform Take()
        {
            Transform mark = spare.Count > 0 ? spare.Pop() : Instantiate(markTemplate, markTemplate.parent);
            mark.name = "Alerta";
            mark.gameObject.SetActive(true);
            return mark;
        }
    }
}
