using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Punto de control: al pasar Ulises, guarda este lugar (ver <see cref="Checkpoints"/>) para reaparecer aquí
    /// si muere. En el tutorial son los letreros: el letrero da un saltito y suena un brillo la primera vez.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayers;
        [Tooltip("Dónde reaparece Ulises respecto a este punto (a la izquierda del letrero, para no taparlo).")]
        [SerializeField] private Vector2 respawnOffset = new(-2.6f, 0f);
        [Tooltip("Opcional: lo que da el saltito al activarse (el dibujo del letrero).")]
        [SerializeField] private Transform bounce;
        [SerializeField] private SoundEffect sound = new();
        [SerializeField, Min(0.01f)] private float bounceDuration = 0.35f;

        private bool reached;
        private float bounceTimer;
        private Vector3 restScale;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (bounce != null)
                restScale = bounce.localScale;
            // Si Ulises reapareció en este punto (o más adelante), ya está activo: no vuelve a sonar.
            if (Checkpoints.TryGet(out Vector2 saved) && saved.x >= RespawnPoint.x - 0.01f)
                reached = true;
        }

        private Vector2 RespawnPoint => (Vector2)transform.position + respawnOffset;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Solo Ulises (lo que tiene vida), no sus bolitas de masa, que viven en la misma capa.
            if (reached || (playerLayers.value & (1 << other.gameObject.layer)) == 0 || other.GetComponentInParent<Health>() == null)
                return;
            reached = true;
            Checkpoints.Reach(RespawnPoint);
            sound.Play();
            bounceTimer = bounceDuration;
        }

        private void Update()
        {
            if (bounceTimer <= 0f || bounce == null)
                return;
            bounceTimer -= Time.deltaTime;
            // Se estira hacia arriba y vuelve, como un resorte.
            float t = 1f - Mathf.Max(0f, bounceTimer) / bounceDuration;
            float stretch = Mathf.Sin(t * Mathf.PI) * (1f - t) * 0.35f;
            bounce.localScale = new Vector3(restScale.x * (1f - stretch * 0.5f), restScale.y * (1f + stretch), restScale.z);
        }
    }
}
