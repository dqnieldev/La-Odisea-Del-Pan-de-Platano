using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Utilities;

namespace Odisea.UI
{
    public enum InputScheme
    {
        Keyboard,
        Xbox,
        PlayStation,
    }

    /// <summary>
    /// Recuerda con qué está jugando la persona (teclado/mouse, mando de Xbox o de PlayStation)
    /// según el último botón presionado, para mostrar los íconos correctos en la ayuda de botones.
    /// </summary>
    public static class InputDeviceTracker
    {
        public static InputScheme Current { get; private set; } = InputScheme.Keyboard;

        public static event Action<InputScheme> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Current = Gamepad.current != null ? Classify(Gamepad.current) : InputScheme.Keyboard;
            InputSystem.onAnyButtonPress.Call(control => SetScheme(Classify(control.device)));
        }

        private static InputScheme Classify(InputDevice device) => device switch
        {
            DualShockGamepad => InputScheme.PlayStation, // Incluye DualSense.
            Gamepad => InputScheme.Xbox,                 // Xbox y mandos genéricos.
            _ => InputScheme.Keyboard,
        };

        private static void SetScheme(InputScheme scheme)
        {
            if (scheme == Current)
                return;
            Current = scheme;
            Changed?.Invoke(scheme);
        }
    }
}
