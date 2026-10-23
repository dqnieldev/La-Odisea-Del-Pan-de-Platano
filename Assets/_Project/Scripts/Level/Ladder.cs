using System.Collections.Generic;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Escalera trepable. Su collider (trigger) marca la zona: abajo = suelo donde empieza,
    /// arriba = superficie de la plataforma donde termina.
    /// Las plataformas de un sentido que cruza se atraviesan mientras Ulises trepa.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Ladder : MonoBehaviour
    {
        private BoxCollider2D area;
        private readonly List<Collider2D> passThroughPlatforms = new();

        public float CenterX => area.bounds.center.x;
        public float TopY => area.bounds.max.y;
        public float BottomY => area.bounds.min.y;
        public IReadOnlyList<Collider2D> PassThroughPlatforms => passThroughPlatforms;

        private void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        private void Start()
        {
            // Las plataformas se buscan una vez: el nivel no se mueve.
            // La plataforma donde se apoya la base de la escalera no cuenta: si se atravesara, Ulises se caería al bajar.
            Bounds bounds = area.bounds;
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(bounds.center, bounds.size + new Vector3(0f, 0.1f), 0f))
            {
                bool isOneWayPlatform = !hit.isTrigger && hit.usedByEffector && hit.GetComponent<PlatformEffector2D>() != null;
                if (hit != area && isOneWayPlatform && hit.bounds.max.y > BottomY + 0.05f)
                    passThroughPlatforms.Add(hit);
            }
        }

        private void OnDrawGizmos()
        {
            var box = area != null ? area : GetComponent<BoxCollider2D>();
            if (box == null)
                return;
            Gizmos.color = new Color(0.96f, 0.72f, 0.19f, 0.6f);
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
    }
}
