using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.Core
{
    /// <summary>
    /// Vibración corta del mando, respetando el ajuste Vibración de Opciones.
    /// Uso: <c>GamepadRumble.Play(0.3f, 0.7f, 0.2f)</c>. Sin mando conectado no hace nada.
    /// </summary>
    public class GamepadRumble : MonoBehaviour
    {
        private static GamepadRumble instance;
        private Coroutine routine;
        private Gamepad rumbling;

        /// <param name="low">Motor grave (0 a 1): golpe sordo.</param>
        /// <param name="high">Motor agudo (0 a 1): zumbido.</param>
        /// <param name="duration">Segundos (sin importar la pausa).</param>
        public static void Play(float low, float high, float duration)
        {
            Gamepad gamepad = Gamepad.current;
            if (!GameSettings.Vibration || gamepad == null)
                return;

            if (instance == null)
            {
                var go = new GameObject("Vibración del mando");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<GamepadRumble>();
            }
            instance.Rumble(gamepad, low, high, duration);
        }

        /// <summary>Corta cualquier vibración en curso (por ejemplo, al desactivarla en Opciones).</summary>
        public static void Stop()
        {
            if (instance != null)
                instance.StopRumble();
        }

        private void Rumble(Gamepad gamepad, float low, float high, float duration)
        {
            StopRumble();
            rumbling = gamepad;
            gamepad.SetMotorSpeeds(Mathf.Clamp01(low), Mathf.Clamp01(high));
            routine = StartCoroutine(StopAfter(duration));
        }

        private IEnumerator StopAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            routine = null;
            StopRumble();
        }

        private void StopRumble()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            rumbling?.ResetHaptics();
            rumbling = null;
        }

        // Que el mando no se quede vibrando si se cierra el juego o se detiene Play a media vibración.
        private void OnDisable() => StopRumble();
    }
}
