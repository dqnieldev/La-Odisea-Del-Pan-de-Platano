using UnityEngine;
using UnityEngine.SceneManagement;

namespace Odisea.Core
{
    /// <summary>
    /// Recuerda el último punto de control que tocó Ulises en el nivel actual. Al morir la escena se recarga
    /// completa (enemigos y muñecos vuelven) y Ulises aparece ahí en vez de en el inicio.
    /// Se olvida al ir a otra escena o al reiniciar a mano (R o Reiniciar en la pausa).
    /// </summary>
    public static class Checkpoints
    {
        private static string scene;
        private static Vector2 position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            scene = null;
            SceneFlow.LoadStarted -= OnLoadStarted;
            SceneFlow.LoadStarted += OnLoadStarted;
        }

        /// <summary>Punto en el suelo donde reaparece Ulises.</summary>
        public static void Reach(Vector2 groundPoint)
        {
            scene = SceneManager.GetActiveScene().name;
            position = groundPoint;
        }

        public static bool TryGet(out Vector2 groundPoint)
        {
            groundPoint = position;
            return scene != null && scene == SceneManager.GetActiveScene().name;
        }

        public static void Clear() => scene = null;

        private static void OnLoadStarted(string next)
        {
            if (next != scene)
                Clear();
        }
    }
}
