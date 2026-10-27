using System;
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
        /// <summary>Logo del juego antes del título. Es la primera escena y a donde lleva "Ir al título".</summary>
        public const string Splash = "Splash";
        public const string Title = "Title";
        public const string MainMenu = "MainMenu";
        // Mientras no exista el nivel 1, Jugar abre la escena de prueba del jugador.
        public const string Play = "PlayerSandbox";
        public const string Tutorial = "Tutorial";

        public static bool IsLoading => ScreenFader.Instance.IsBusy;

        /// <summary>
        /// Avisa que empieza un cambio de escena (con el nombre de la escena destino), antes de que se cierre el iris.
        /// La música del menú lo usa para desvanecerse.
        /// </summary>
        public static event Action<string> LoadStarted;

        /// <summary>Cierra la pantalla en iris, carga la escena y la vuelve a abrir.</summary>
        /// <param name="viewportCenter">Punto de la pantalla (0-1) hacia donde se cierra el iris; por defecto el centro.</param>
        public static void Load(string sceneName, Vector2? viewportCenter = null)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Odisea] La escena '{sceneName}' no está en la lista de escenas del build.");
                return;
            }
            if (ScreenFader.Instance.IsBusy)
                return;
            LoadStarted?.Invoke(sceneName);
            ScreenFader.Instance.FadeAndLoad(sceneName, viewportCenter);
        }

        /// <summary>
        /// Cambia de escena al instante, sin iris. Para cuando la pantalla ya quedó oscura por su cuenta
        /// (el logo se desvanece y el título empieza sobre el mismo fondo café).
        /// </summary>
        public static void LoadWithoutTransition(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Odisea] La escena '{sceneName}' no está en la lista de escenas del build.");
                return;
            }
            Time.timeScale = 1f;
            LoadStarted?.Invoke(sceneName);
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        /// <summary>Vuelve a cargar la escena actual (reiniciar nivel).</summary>
        /// <param name="viewportCenter">Punto de la pantalla (0-1) hacia donde se cierra el iris (p. ej. Ulises al morir).</param>
        public static void ReloadCurrent(Vector2? viewportCenter = null) =>
            Load(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, viewportCenter);

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
