using Odisea.Core;
using Odisea.UI;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Meta del nivel: cuando Ulises la toca, sale la tarjeta de nivel completado (si hay) y, al aceptar, el iris
    /// se cierra sobre él y se carga la escena siguiente (el mapa). Marca el lugar como terminado.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        [Tooltip("Escena a la que lleva (nombre en Build Settings).")]
        [SerializeField] private string nextScene = SceneFlow.MainMenu;
        [SerializeField] private LayerMask playerLayers;
        [Tooltip("Lugar del mapa que queda terminado al llegar (GameProgress: tutorial, mercado, horno, tovara).")]
        [SerializeField] private string completesLocation;
        [Tooltip("Opcional: tarjeta de \"¡Nivel completado!\" que se muestra antes de irse.")]
        [SerializeField] private LevelCompleteScreen completeScreen;

        private bool reached;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // La bolita de masa vive en la capa del jugador: solo cuenta Ulises (lo que tiene vida), no sus proyectiles.
            if (reached || (playerLayers.value & (1 << other.gameObject.layer)) == 0 || other.GetComponentInParent<Health>() == null)
                return;
            reached = true;

            Camera view = Camera.main;
            Vector2? center = null;
            if (view != null)
            {
                Vector3 point = view.WorldToViewportPoint(other.bounds.center);
                center = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
            }
            Checkpoints.Clear();
            GameProgress.Complete(completesLocation);
            if (completeScreen != null)
                completeScreen.Show(() => SceneFlow.Load(nextScene, center));
            else
                SceneFlow.Load(nextScene, center);
        }
    }
}
