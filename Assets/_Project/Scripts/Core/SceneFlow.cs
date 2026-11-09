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
        /// <summary>Escena de pruebas del jugador (ya no la abre Jugar; queda para probar mecánicas).</summary>
        public const string Play = "PlayerSandbox";
        public const string Tutorial = "Tutorial";
        /// <summary>Visor de historietas: muestra la que se pidió con <see cref="PlayStory"/> y luego sigue.</summary>
        public const string Story = "Historia";
        /// <summary>Elegir una de las tres partidas (la abre Jugar).</summary>
        public const string Slots = "Partidas";
        /// <summary>Mapa de San Blas: Ulises camina y entra a cada lugar (tutorial y niveles).</summary>
        public const string Map = "Mapa";
        /// <summary>Panadería de Don Lupe: después del prólogo y desde el mapa. Su puerta lleva a la playa (tutorial).</summary>
        public const string Bakery = "Panaderia";
        /// <summary>Créditos (los abre el menú principal).</summary>
        public const string Credits = "Creditos";
        /// <summary>Historieta del principio (antes de la panadería).</summary>
        public const string PrologueStory = "Prologo";
        /// <summary>Historietas del Acto I: al entrar al Mercado (desde el mapa) y al llegar al racimo.</summary>
        public const string MercadoStory = "Mercado";
        public const string MercadoOutroStory = "MercadoSalida";
        /// <summary>Historietas del Acto II: al entrar al Horno (desde el mapa) y después de vencer a Polifemo.</summary>
        public const string HornoStory = "Horno";
        public const string HornoOutroStory = "HornoSalida";
        /// <summary>Nivel 3: la carrera por los manglares hasta el festival.</summary>
        public const string Tovara = "Tovara";
        /// <summary>Historietas del Acto III: al entrar a La Tovara (desde el mapa) y al llegar al festival.</summary>
        public const string TovaraStory = "Tovara";
        public const string TovaraOutroStory = "TovaraSalida";

        /// <summary>Historieta pendiente de mostrar y la escena que sigue después de ella.</summary>
        public static string PendingStory { get; private set; }
        public static string AfterStory { get; private set; }

        /// <summary>
        /// El tutorial se abrió con el botón Tutorial del menú (de práctica): al terminarlo regresa al menú y no
        /// cuenta para ninguna partida.
        /// </summary>
        public static bool TutorialFromMenu { get; private set; }

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

        /// <summary>Jugar: abre la pantalla de partidas.</summary>
        public static void StartGame() => Load(Slots);

        /// <summary>Botón Tutorial del menú: el tutorial de práctica, que al terminar regresa al menú.</summary>
        public static void PlayTutorialFromMenu()
        {
            TutorialFromMenu = true;
            Load(Tutorial);
        }

        /// <summary>
        /// Entra a una partida: si es nueva o no ha terminado el tutorial, la historieta del prólogo y la panadería
        /// (Don Lupe lo manda a la playa, al tutorial); si ya lo terminó, directo al mapa.
        /// </summary>
        public static void PlaySlot(int slot)
        {
            TutorialFromMenu = false;
            GameProgress.Begin(slot);
            if (GameProgress.TutorialDone)
                Load(Map);
            else
                PlayStory(PrologueStory, Bakery);
        }

        /// <summary>
        /// Muestra una historieta (por su nombre, en la escena <see cref="Story"/>) y después carga nextScene.
        /// Sin historieta, carga nextScene directo.
        /// </summary>
        public static void PlayStory(string storyName, string nextScene, Vector2? viewportCenter = null)
        {
            if (string.IsNullOrEmpty(storyName) || !Application.CanStreamedLevelBeLoaded(Story))
            {
                Load(nextScene, viewportCenter);
                return;
            }
            PendingStory = storyName;
            AfterStory = nextScene;
            Load(Story, viewportCenter);
        }

        /// <summary>El visor toma la historieta pendiente (y la borra para que no se repita).</summary>
        public static (string story, string next) TakeStoryRequest()
        {
            var request = (PendingStory, AfterStory);
            PendingStory = AfterStory = null;
            return request;
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
