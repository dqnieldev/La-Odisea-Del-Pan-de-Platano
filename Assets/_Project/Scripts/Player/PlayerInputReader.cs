using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.Player
{
    /// <summary>
    /// Lee el mapa "Player" de InputSystem_Actions y lo expone en forma simple.
    /// El resto de scripts del jugador no conocen el Input System.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMap = "Player";

        private InputActionMap map;
        private InputAction move;
        private InputAction jump;
        private InputAction attack;
        private InputAction throwAction;
        private InputAction crouch;

        // Las pulsaciones se guardan desde el callback "performed" hasta que alguien las consume.
        // Así no se pierden toques muy rápidos ni dependen de en qué ciclo de Unity llegó el evento.
        private bool jumpQueued;
        private bool attackQueued;
        private bool throwQueued;

        /// <summary>
        /// Movimiento dictado por código en lugar del jugador: cinemáticas (Ulises camina solo) y pruebas.
        /// null = usar los controles normales.
        /// </summary>
        public Vector2? ScriptedMove { get; set; }

        public Vector2 Move => ScriptedMove ?? (move != null ? move.ReadValue<Vector2>() : Vector2.zero);
        /// <summary>Si tiene valor, reemplaza al botón de salto mantenido (pruebas o escenas guiadas), como ScriptedMove.</summary>
        public bool? ScriptedJumpHeld { get; set; }

        public bool JumpHeld => ScriptedJumpHeld ?? (jump != null && jump.IsPressed());
        public bool CrouchHeld => crouch != null && crouch.IsPressed();

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{name}: falta asignar el InputActionAsset en PlayerInputReader.", this);
                return;
            }

            map = actions.FindActionMap(actionMap, throwIfNotFound: true);
            move = map.FindAction("Move", throwIfNotFound: true);
            jump = map.FindAction("Jump", throwIfNotFound: true);
            attack = map.FindAction("Attack", throwIfNotFound: true);
            throwAction = map.FindAction("Throw", throwIfNotFound: true);
            crouch = map.FindAction("Crouch");
        }

        private void OnEnable()
        {
            if (map == null)
                return;
            jump.performed += OnJumpPerformed;
            attack.performed += OnAttackPerformed;
            throwAction.performed += OnThrowPerformed;
            map.Enable();
        }

        private void OnDisable()
        {
            if (map == null)
                return;
            jump.performed -= OnJumpPerformed;
            attack.performed -= OnAttackPerformed;
            throwAction.performed -= OnThrowPerformed;
            map.Disable();
            jumpQueued = false;
            attackQueued = false;
            throwQueued = false;
        }

        /// <summary>true una sola vez por cada pulsación de salto.</summary>
        public bool ConsumeJumpPressed()
        {
            bool pressed = jumpQueued;
            jumpQueued = false;
            return pressed;
        }

        /// <summary>true una sola vez por cada pulsación de golpe.</summary>
        public bool ConsumeAttackPressed()
        {
            bool pressed = attackQueued;
            attackQueued = false;
            return pressed;
        }

        /// <summary>true una sola vez por cada pulsación de lanzar.</summary>
        public bool ConsumeThrowPressed()
        {
            bool pressed = throwQueued;
            throwQueued = false;
            return pressed;
        }

        private void OnJumpPerformed(InputAction.CallbackContext _) => jumpQueued = true;

        private void OnAttackPerformed(InputAction.CallbackContext _) => attackQueued = true;

        private void OnThrowPerformed(InputAction.CallbackContext _) => throwQueued = true;
    }
}
