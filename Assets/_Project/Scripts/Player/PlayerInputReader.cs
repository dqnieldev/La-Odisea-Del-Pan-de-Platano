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
        private InputAction interact;

        // Las pulsaciones se guardan desde el callback "performed" hasta que alguien las consume.
        // Así no se pierden toques muy rápidos ni dependen de en qué ciclo de Unity llegó el evento.
        private bool jumpQueued;
        private bool attackQueued;
        private bool throwQueued;
        private bool interactQueued;

        /// <summary>
        /// Movimiento dictado por código en lugar del jugador: cinemáticas (Ulises camina solo) y pruebas.
        /// null = usar los controles normales.
        /// </summary>
        public Vector2? ScriptedMove { get; set; }

        /// <summary>
        /// Ulises no se mueve, ni salta, ni ataca (mientras habla o mientras se cierra una puerta). Interactuar sí
        /// funciona, para pasar las líneas del diálogo.
        /// </summary>
        public bool InputLocked { get; set; }

        public Vector2 Move => InputLocked ? Vector2.zero : ScriptedMove ?? (move != null ? move.ReadValue<Vector2>() : Vector2.zero);
        /// <summary>Si tiene valor, reemplaza al botón de salto mantenido (pruebas o escenas guiadas), como ScriptedMove.</summary>
        public bool? ScriptedJumpHeld { get; set; }

        public bool JumpHeld => !InputLocked && (ScriptedJumpHeld ?? (jump != null && jump.IsPressed()));
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
            interact = map.FindAction("Interact");
        }

        private void OnEnable()
        {
            if (map == null)
                return;
            jump.performed += OnJumpPerformed;
            attack.performed += OnAttackPerformed;
            throwAction.performed += OnThrowPerformed;
            if (interact != null)
                interact.performed += OnInteractPerformed;
            map.Enable();
        }

        private void OnDisable()
        {
            if (map == null)
                return;
            jump.performed -= OnJumpPerformed;
            attack.performed -= OnAttackPerformed;
            throwAction.performed -= OnThrowPerformed;
            if (interact != null)
                interact.performed -= OnInteractPerformed;
            map.Disable();
            jumpQueued = false;
            attackQueued = false;
            throwQueued = false;
            interactQueued = false;
        }

        /// <summary>true una sola vez por cada pulsación de salto.</summary>
        public bool ConsumeJumpPressed()
        {
            bool pressed = jumpQueued && !InputLocked;
            jumpQueued = false;
            return pressed;
        }

        /// <summary>true una sola vez por cada pulsación de golpe.</summary>
        public bool ConsumeAttackPressed()
        {
            bool pressed = attackQueued && !InputLocked;
            attackQueued = false;
            return pressed;
        }

        /// <summary>true una sola vez por cada pulsación de lanzar.</summary>
        public bool ConsumeThrowPressed()
        {
            bool pressed = throwQueued && !InputLocked;
            throwQueued = false;
            return pressed;
        }

        /// <summary>true una sola vez por cada pulsación de interactuar (hablar, abrir una puerta).</summary>
        public bool ConsumeInteractPressed()
        {
            bool pressed = interactQueued;
            interactQueued = false;
            return pressed;
        }

        private void OnJumpPerformed(InputAction.CallbackContext _) => jumpQueued = true;

        private void OnAttackPerformed(InputAction.CallbackContext _) => attackQueued = true;

        private void OnThrowPerformed(InputAction.CallbackContext _) => throwQueued = true;

        private void OnInteractPerformed(InputAction.CallbackContext _) => interactQueued = true;
    }
}
