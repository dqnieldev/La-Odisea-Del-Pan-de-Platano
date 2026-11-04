using Odisea.Core;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Madame Circe en el Mercado: flota en el hueco del centro a la altura de Ulises (lo sigue al subir y al bajar) y
    /// cada tantos segundos le avienta algo en un arco bajo, para no chocar con la repisa de arriba: una caja directo
    /// a él o una cáscara en la repisa por donde va. Antes de aventar se estira (aviso). No se le puede vencer; deja
    /// de aventar al llegar Ulises a la cima o al morir.
    /// </summary>
    public class CirceThrower : MonoBehaviour
    {
        [SerializeField] private Health player;
        [SerializeField] private ThrownItem cratePrefab;
        [SerializeField] private ThrownItem peelPrefab;
        [Tooltip("Lo que se mueve al aventar (el dibujo): se estira hacia arriba como aviso.")]
        [SerializeField] private Transform visual;
        [Tooltip("De dónde sale lo que avienta (la mano).")]
        [SerializeField] private Transform hand;

        [Header("Movimiento")]
        [Tooltip("Qué tan arriba del centro de Ulises se pone.")]
        [SerializeField] private float heightAbovePlayer = 0.6f;
        [SerializeField] private float minHeight = 4f;
        [Tooltip("La cima: de ahí no pasa, y si Ulises llega ya no avienta.")]
        [SerializeField] private float maxHeight = 36f;
        [SerializeField, Min(0.1f)] private float climbSpeed = 4f;
        [Tooltip("Se mece de lado a lado en el hueco (ancho y velocidad).")]
        [SerializeField] private float swayWidth = 1.5f;
        [SerializeField] private float swaySpeed = 0.6f;

        [Header("Aventar")]
        [Tooltip("Ulises tiene que estar arriba de esta altura para que empiece.")]
        [SerializeField] private float startHeight = 3f;
        [SerializeField] private Vector2 throwInterval = new(2.5f, 4f);
        [SerializeField, Range(0f, 1f)] private float peelChance = 0.5f;
        [SerializeField, Min(0.05f)] private float windUp = 0.45f;
        [Tooltip("Velocidad de lado de lo que avienta; el vuelo dura lo que tarde en llegar (entre min y max segundos).")]
        [SerializeField, Min(1f)] private float throwSpeed = 12f;
        [Tooltip("Duración del vuelo: con más, el arco sube más (y puede chocar con la repisa de arriba).")]
        [SerializeField] private Vector2 flightTime = new(0.35f, 0.7f);
        [Tooltip("Qué tan adelante de Ulises cae la cáscara.")]
        [SerializeField] private Vector2 peelLead = new(1.5f, 3.5f);
        [SerializeField] private SoundEffect throwSound = new();

        private float throwTimer;
        private float windUpTimer = -1f;
        private Vector3 visualScale = Vector3.one;

        private void Start()
        {
            if (visual != null)
                visualScale = visual.localScale;
            throwTimer = Random.Range(throwInterval.x, throwInterval.y);
            transform.position = new Vector3(transform.position.x, TargetHeight(), transform.position.z);
        }

        private void Update()
        {
            if (player == null)
                return;
            Vector3 position = transform.position;
            float height = Mathf.MoveTowards(position.y, TargetHeight(), climbSpeed * Time.deltaTime);
            float x = Mathf.Sin(Time.time * swaySpeed) * swayWidth;
            transform.position = new Vector3(x, height, position.z);

            if (!CanThrow())
            {
                windUpTimer = -1f;
                ResetPose();
                return;
            }
            if (windUpTimer >= 0f)
            {
                windUpTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(windUpTimer / windUp);
                if (visual != null)
                    visual.localScale = new Vector3(visualScale.x * (1f - 0.1f * t), visualScale.y * (1f + 0.15f * t), visualScale.z);
                if (windUpTimer <= 0f)
                {
                    windUpTimer = -1f;
                    ResetPose();
                    Throw();
                }
                return;
            }
            throwTimer -= Time.deltaTime;
            if (throwTimer <= 0f)
            {
                throwTimer = Random.Range(throwInterval.x, throwInterval.y);
                windUpTimer = windUp;
            }
        }

        private float TargetHeight() => Mathf.Clamp(player.transform.position.y + heightAbovePlayer, minHeight, maxHeight);

        private bool CanThrow()
        {
            float playerY = player.transform.position.y;
            return !player.IsDead && playerY > startHeight && playerY < maxHeight - 1f && !SceneFlow.IsLoading;
        }

        private void ResetPose()
        {
            if (visual != null)
                visual.localScale = visualScale;
        }

        private void Throw()
        {
            bool peel = peelPrefab != null && Random.value < peelChance;
            ThrownItem prefab = peel ? peelPrefab : cratePrefab;
            if (prefab == null)
                return;
            Vector2 from = hand != null ? (Vector2)hand.position : (Vector2)transform.position;
            Vector2 target = player.transform.position;
            if (peel)
            {
                // Cae un poco adelante de Ulises, hacia donde va (si está quieto, hacia el centro).
                var body = player.GetComponent<Rigidbody2D>();
                float direction = body != null && Mathf.Abs(body.linearVelocity.x) > 0.1f
                    ? Mathf.Sign(body.linearVelocity.x)
                    : -Mathf.Sign(player.transform.position.x);
                target.x += direction * Random.Range(peelLead.x, peelLead.y);
            }
            ThrownItem item = Instantiate(prefab, from, Quaternion.identity);
            item.Throw(LaunchVelocity(from, target, item));
            throwSound.Play();
        }

        // Velocidad para llegar al blanco con la gravedad del objeto; el vuelo dura según la distancia.
        private Vector2 LaunchVelocity(Vector2 from, Vector2 to, ThrownItem item)
        {
            Vector2 delta = to - from;
            float time = Mathf.Clamp(Mathf.Abs(delta.x) / throwSpeed, flightTime.x, flightTime.y);
            return new Vector2(delta.x / time, delta.y / time + 0.5f * item.Gravity * time);
        }
    }
}
