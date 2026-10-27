using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Va en Ulises: al cargar el nivel, si hay un punto de control guardado (murió después de pasarlo),
    /// lo pone ahí en vez de en el inicio. La cámara lo alcanza de golpe en su Start.
    /// </summary>
    public class CheckpointSpawn : MonoBehaviour
    {
        [Tooltip("Altura sobre el suelo del punto de control a la que aparece (cae al piso, como al empezar).")]
        [SerializeField] private float height = 1f;

        private void Awake()
        {
            if (!Checkpoints.TryGet(out Vector2 point))
                return;
            Vector2 spawn = point + Vector2.up * height;
            transform.position = spawn;
            if (TryGetComponent(out Rigidbody2D body))
                body.position = spawn;
        }
    }
}
