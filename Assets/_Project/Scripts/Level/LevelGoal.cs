using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Meta del nivel: cuando Ulises la toca, el iris se cierra sobre él y se carga la escena siguiente
    /// (por ahora el menú principal; luego el siguiente nivel o la pantalla de victoria).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        [Tooltip("Escena a la que lleva (nombre en Build Settings).")]
        [SerializeField] private string nextScene = SceneFlow.MainMenu;
        [SerializeField] private LayerMask playerLayers;

        private bool reached;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (reached || (playerLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
            reached = true;

            Camera view = Camera.main;
            Vector2? center = null;
            if (view != null)
            {
                Vector3 point = view.WorldToViewportPoint(other.bounds.center);
                center = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
            }
            SceneFlow.Load(nextScene, center);
        }
    }
}
