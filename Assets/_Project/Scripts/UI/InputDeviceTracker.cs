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
    /// según el último botón presionado (o el mouse al moverse), para mostrar los íconos correctos en la ayuda de
    /// botones. Con el mando, el cursor del mouse se esconde; vuelve en cuanto se mueve el mouse o se usa el teclado.
    /// </summary>
    public static class InputDeviceTracker
    {
        // Lo que tiene que moverse el mouse (en píxeles, en un cuadro) para volver a mostrar el cursor: así un
        // temblorcito de la mesa no lo hace aparecer.
        private const float MouseMoveThreshold = 2f;

        public static InputScheme Current { get; private set; } = InputScheme.Keyboard;

        public static event Action<InputScheme> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Current = Gamepad.current != null ? Classify(Gamepad.current) : InputScheme.Keyboard;
            InputSystem.onAnyButtonPress.Call(control => SetScheme(Classify(control.device)));
            ApplyCursor();
            var watcher = new GameObject("Mouse (cursor)") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(watcher);
            watcher.AddComponent<MouseWatcher>();
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
            ApplyCursor();
            Changed?.Invoke(scheme);
        }

        private static void ApplyCursor() => Cursor.visible = Current == InputScheme.Keyboard;

        // Mover el mouse no es "presionar un botón": se revisa cada cuadro.
        private class MouseWatcher : MonoBehaviour
        {
            private void Update()
            {
                Mouse mouse = Mouse.current;
                if (Current != InputScheme.Keyboard && mouse != null &&
                    mouse.delta.ReadValue().sqrMagnitude > MouseMoveThreshold * MouseMoveThreshold)
                    SetScheme(InputScheme.Keyboard);
            }

            private void OnApplicationFocus(bool focused)
            {
                // Al volver a la ventana, el sistema puede haberlo mostrado.
                if (focused)
                    ApplyCursor();
            }
        }
    }
}
