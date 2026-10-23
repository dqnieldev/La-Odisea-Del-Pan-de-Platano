using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Ajustes del jugador (volumen general y pantalla completa), guardados en PlayerPrefs
    /// y aplicados automáticamente al arrancar el juego.
    /// </summary>
    public static class GameSettings
    {
        private const string VolumeKey = "odisea.volumenGeneral";
        private const string FullscreenKey = "odisea.pantallaCompleta";

        /// <summary>Volumen general de 0 a 1. Afecta a todo el audio, incluido el video de inicio.</summary>
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set
            {
                float volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, volume);
                AudioListener.volume = volume;
            }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                ApplyFullscreen(value);
            }
        }

        /// <summary>Escribe los ajustes a disco. Llamarlo al salir de Opciones.</summary>
        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            AudioListener.volume = MasterVolume;
            ApplyFullscreen(Fullscreen);
        }

        private static void ApplyFullscreen(bool fullscreen)
        {
            // En el Editor la ventana de juego no se puede poner en pantalla completa.
            if (Application.isEditor)
                return;
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }
}
