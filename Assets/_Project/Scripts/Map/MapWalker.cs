using Odisea.Core;
using Odisea.Player;
using UnityEngine;

namespace Odisea.Map
{
    /// <summary>
    /// Ulises en el mapa de San Blas, visto desde arriba: camina en 8 direcciones a velocidad constante (también en
    /// diagonal y con el stick a medias), sin gravedad. Aparece junto al último lugar del que salió.
    /// Mientras no haya dibujo de Ulises caminando desde arriba, usa sus animaciones de lado (Idle/Run).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader))]
    public class MapWalker : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int IsGroundedId = Animator.StringToHash("IsGrounded");

        [SerializeField, Min(0f)] private float speed = 5.5f;
        [Tooltip("Cuánto hay que inclinar el stick para caminar (0-1).")]
        [SerializeField, Range(0f, 0.9f)] private float deadzone = 0.3f;
        [Tooltip("El dibujo (hijo): se voltea al caminar a la izquierda.")]
        [SerializeField] private Transform visual;
        [SerializeField] private Animator animator;
        [SerializeField] private SoundEffect footstep = new();
        [SerializeField, Min(0.05f)] private float footstepInterval = 0.32f;

        private Rigidbody2D body;
        private PlayerInputReader input;
        private float stepTimer;

        /// <summary>Quieto (al entrar a un lugar, con la pausa abierta...).</summary>
        public bool Frozen { get; set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            PlaceAtLastLocation();
        }

        // Al volver de un nivel, aparece frente al lugar del que salió.
        private void PlaceAtLastLocation()
        {
            string last = GameProgress.LastMapLocation;
            if (string.IsNullOrEmpty(last))
                return;
            foreach (MapLocation location in FindObjectsByType<MapLocation>(FindObjectsSortMode.None))
            {
                if (location.Id != last)
                    continue;
                body.position = location.ArrivalPoint;
                transform.position = location.ArrivalPoint;
                return;
            }
        }

        private void FixedUpdate()
        {
            Vector2 move = Frozen ? Vector2.zero : input.Move;
            // Solo importa hacia dónde: pasando el margen camina a toda velocidad.
            Vector2 direction = move.magnitude >= deadzone ? move.normalized : Vector2.zero;
            body.linearVelocity = direction * speed;
        }

        private void Update()
        {
            Vector2 velocity = body.linearVelocity;
            bool walking = velocity.sqrMagnitude > 0.01f;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool(IsGroundedId, true);
                animator.SetFloat(SpeedId, velocity.magnitude);
            }
            if (visual != null && Mathf.Abs(velocity.x) > 0.05f)
            {
                Vector3 scale = visual.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(velocity.x);
                visual.localScale = scale;
            }

            stepTimer -= Time.deltaTime;
            if (walking && stepTimer <= 0f)
            {
                footstep.Play();
                stepTimer = footstepInterval;
            }
        }
    }
}
