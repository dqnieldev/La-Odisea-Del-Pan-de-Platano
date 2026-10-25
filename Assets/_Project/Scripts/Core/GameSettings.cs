using System;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>Cómo se muestra la ventana del juego. El orden es el del selector en Opciones.</summary>
    public enum WindowMode
    {
        Fullscreen,
        Borderless,
        Windowed,
    }

    /// <summary>
    /// Ajustes del jugador (volúmenes, pantalla, brillo, FPS y vibración), guardados en PlayerPrefs
    /// y aplicados automáticamente al arrancar el juego.
    /// </summary>
    public static class GameSettings
    {
        private const string VolumeKey = "odisea.volumenGeneral";
        private const string MusicKey = "odisea.volumenMusica";
        private const string WindowModeKey = "odisea.modoVentana";
        // Ajuste viejo (sí/no); solo se lee para respetar lo que ya eligió quien jugó antes.
        private const string LegacyFullscreenKey = "odisea.pantallaCompleta";
        private const string BrightnessKey = "odisea.brillo";
        private const string FrameRateKey = "odisea.limiteFps";
        private const string ShowFpsKey = "odisea.mostrarFps";
        private const string VibrationKey = "odisea.vibracion";

        /// <summary>Opciones del límite de FPS; 0 = sin límite.</summary>
        public static readonly int[] FrameRateOptions = { 30, 60, 120, 0 };

        /// <summary>Se llama al cambiar el volumen de música, para que la música que ya suena se ajuste.</summary>
        public static event Action<float> MusicVolumeChanged;
        public static event Action<float> BrightnessChanged;
        public static event Action<bool> ShowFpsChanged;

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

        /// <summary>Volumen de la música de 0 a 1. Se multiplica por el volumen general.</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
            set
            {
                float volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicKey, volume);
                MusicVolumeChanged?.Invoke(volume);
            }
        }

        public static WindowMode WindowMode
        {
            get
            {
                if (PlayerPrefs.HasKey(WindowModeKey))
                    return (WindowMode)Mathf.Clamp(PlayerPrefs.GetInt(WindowModeKey), 0, 2);
                return PlayerPrefs.GetInt(LegacyFullscreenKey, 1) == 1 ? WindowMode.Borderless : WindowMode.Windowed;
            }
            set
            {
                PlayerPrefs.SetInt(WindowModeKey, (int)value);
                ApplyWindowMode(value);
            }
        }

        /// <summary>Brillo de 0 a 1; 0.5 es la imagen tal cual.</summary>
        public static float Brightness
        {
            get => PlayerPrefs.GetFloat(BrightnessKey, 0.5f);
            set
            {
                float brightness = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(BrightnessKey, brightness);
                BrightnessChanged?.Invoke(brightness);
            }
        }

        /// <summary>Límite de cuadros por segundo; 0 = sin límite.</summary>
        public static int FrameRateLimit
        {
            get => PlayerPrefs.GetInt(FrameRateKey, 60);
            set
            {
                PlayerPrefs.SetInt(FrameRateKey, Mathf.Max(0, value));
                ApplyFrameRate(value);
            }
        }

        public static bool ShowFps
        {
            get => PlayerPrefs.GetInt(ShowFpsKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ShowFpsKey, value ? 1 : 0);
                ShowFpsChanged?.Invoke(value);
            }
        }

        /// <summary>Si el mando vibra (al recibir daño, por ejemplo).</summary>
        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set => PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
        }

        /// <summary>Escribe los ajustes a disco. Llamarlo al salir de Opciones.</summary>
        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            AudioListener.volume = MasterVolume;
            ApplyWindowMode(WindowMode);
            ApplyFrameRate(FrameRateLimit);
        }

        private static void ApplyWindowMode(WindowMode mode)
        {
            // En el Editor la ventana de juego no se puede poner en pantalla completa.
            if (Application.isEditor)
                return;
            int width = Display.main.systemWidth;
            int height = Display.main.systemHeight;
            switch (mode)
            {
                case WindowMode.Fullscreen:
                    Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
                    break;
                case WindowMode.Borderless:
                    Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
                    break;
                default:
                    // Ventana de 1280 × 720, o más chica si la pantalla no da para tanto.
                    float scale = Mathf.Min(1f, width * 0.85f / 1280f, height * 0.85f / 720f);
                    Screen.SetResolution(Mathf.RoundToInt(1280 * scale), Mathf.RoundToInt(720 * scale), FullScreenMode.Windowed);
                    break;
            }
        }

        private static void ApplyFrameRate(int limit)
        {
            // Con la sincronización vertical activa Unity ignora targetFrameRate; el límite manda.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = limit > 0 ? limit : -1;
        }
    }
}
