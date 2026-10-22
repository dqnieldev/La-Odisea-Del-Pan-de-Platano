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

        private InputAction move;
        private InputAction jump;
        private InputAction attack;
        private InputAction crouch;

        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => jump != null && jump.WasPressedThisFrame();
        public bool JumpHeld => jump != null && jump.IsPressed();
        public bool AttackPressed => attack != null && attack.WasPressedThisFrame();
        public bool CrouchHeld => crouch != null && crouch.IsPressed();

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{name}: falta asignar el InputActionAsset en PlayerInputReader.", this);
                return;
            }

            var map = actions.FindActionMap(actionMap, throwIfNotFound: true);
            move = map.FindAction("Move", throwIfNotFound: true);
            jump = map.FindAction("Jump", throwIfNotFound: true);
            attack = map.FindAction("Attack", throwIfNotFound: true);
            crouch = map.FindAction("Crouch");
        }

        private void OnEnable() => actions?.FindActionMap(actionMap)?.Enable();

        private void OnDisable() => actions?.FindActionMap(actionMap)?.Disable();
    }
}
