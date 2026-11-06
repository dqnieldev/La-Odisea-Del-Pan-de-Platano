using System;
using System.Collections;
using Odisea.Core;
using Odisea.Level;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Polifemo en el nivel 2: cuando el pan ya está horneado sale de un lado y camina despacio hacia el horno. Cada
    /// golpe de Ulises (rodillo o masa) lo hace retroceder un poco y lo detiene un instante; si llega al horno, se
    /// quema el pan. Al acabarse su vida cae y aparece el premio (el pan horneado, la meta). Daña a Ulises al tocarlo.
    /// El controlador de Polifemo trae los estados Idle, Walk, Hurt y Fall; se ponen con Animator.Play. Su dibujo mira
    /// a la derecha y se voltea con <see cref="visual"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PolifemoWalker : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Hazard contact;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visual;
        [SerializeField] private Oven oven;
        [SerializeField, Min(0f)] private float speed = 0.8f;
        [Tooltip("Lo que retrocede con cada golpe.")]
        [SerializeField, Min(0f)] private float pushBack = 0.6f;
        [Tooltip("Lo que se queda quieto después de un golpe.")]
        [SerializeField, Min(0f)] private float hurtPause = 0.45f;
        [Tooltip("Hasta dónde llega (además del medio ancho del horno) antes de quemarlo.")]
        [SerializeField] private float reach = 0.7f;
        [Tooltip("Lo que aparece al vencerlo (el pan horneado: la meta). Se activa con un saltito.")]
        [SerializeField] private GameObject reward;
        [SerializeField, Min(0f)] private float rewardDelay = 1.2f;
        [SerializeField] private SoundEffect fallSound = new();

        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int HurtState = Animator.StringToHash("Hurt");
        private static readonly int FallState = Animator.StringToHash("Fall");

        private Rigidbody2D body;
        private float pauseTimer;
        private bool done;

        public bool Defeated { get; private set; }
        public Health Health => health;
        public event Action Fell;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            if (reward != null)
                reward.SetActive(false);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
            Play(WalkState);
        }

        private void OnDisable()
        {
            if (health == null)
                return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void FixedUpdate()
        {
            if (done || oven == null)
                return;
            if (oven.Burned)
            {
                done = true;
                Play(IdleState);
                return;
            }
            float dx = oven.transform.position.x - body.position.x;
            Face(dx);
            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.fixedDeltaTime;
                if (pauseTimer <= 0f)
                    Play(WalkState);
                return;
            }
            if (Mathf.Abs(dx) <= oven.HalfWidth + reach)
            {
                done = true;
                Play(IdleState);
                oven.Burn();
                return;
            }
            body.MovePosition(body.position + new Vector2(Mathf.Sign(dx) * speed * Time.fixedDeltaTime, 0f));
        }

        private void OnDamaged(DamageInfo info)
        {
            if (health.IsDead || oven == null)
                return;
            // Retrocede, alejándose del horno.
            float away = -Mathf.Sign(oven.transform.position.x - body.position.x);
            body.position += new Vector2(away * pushBack, 0f);
            pauseTimer = hurtPause;
            Play(HurtState);
        }

        private void OnDied()
        {
            Defeated = true;
            done = true;
            if (contact != null)
                contact.enabled = false;
            Play(FallState);
            fallSound.Play();
            Fell?.Invoke();
            if (reward != null)
                StartCoroutine(ShowReward());
        }

        private IEnumerator ShowReward()
        {
            yield return new WaitForSeconds(rewardDelay);
            reward.SetActive(true);
            Transform shown = reward.transform;
            Vector3 scale = shown.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
            {
                // Crece con un rebote.
                float s = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
                shown.localScale = scale * s;
                yield return null;
            }
            shown.localScale = scale;
        }

        private void Face(float dx)
        {
            if (visual == null || Mathf.Abs(dx) < 0.05f)
                return;
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dx);
            visual.localScale = scale;
        }

        private void Play(int state)
        {
            if (animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled)
                animator.Play(state, 0, 0f);
        }
    }
}
