using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Nombres de las escenas del juego y cambio entre ellas con fundido.
    /// Si una escena cambia de nombre o de destino, se cambia solo aquí.
    /// Las escenas deben estar en File > Build Profiles (Scene List).
    /// </summary>
    public static class SceneFlow
    {
        public const string Title = "Title";
        public const string MainMenu = "MainMenu";
        // Mientras no existan los niveles, Jugar y Tutorial abren la escena de prueba del jugador.
        public const string Play = "PlayerSandbox";
        public const string Tutorial = "PlayerSandbox";

        public static bool IsLoading => ScreenFader.Instance.IsBusy;

        /// <summary>Cierra la pantalla en iris, carga la escena y la vuelve a abrir.</summary>
        /// <param name="viewportCenter">Punto de la pantalla (0-1) hacia donde se cierra el iris; por defecto el centro.</param>
        public static void Load(string sceneName, Vector2? viewportCenter = null)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Odisea] La escena '{sceneName}' no está en la lista de escenas del build.");
                return;
            }
            ScreenFader.Instance.FadeAndLoad(sceneName, viewportCenter);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
