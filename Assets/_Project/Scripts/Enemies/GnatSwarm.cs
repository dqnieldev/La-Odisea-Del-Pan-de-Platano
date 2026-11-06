using Odisea.Core;
using Odisea.Level;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Enjambre de jejenes del nivel 2: vuela ondulando desde un lado hacia la boca del horno. Si llega, le pica la masa
    /// (le quita vida al horno) y se dispersa; Ulises lo deshace de un golpe (hay que saltar). Daña a Ulises al tocarlo.
    /// </summary>
    public class GnatSwarm : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField, Min(0f)] private float speed = 2f;
        [SerializeField, Min(0f)] private float waveHeight = 0.5f;
        [SerializeField, Min(0f)] private float waveSpeed = 3f;
        [SerializeField, Min(1)] private int ovenDamage = 1;
        [SerializeField] private SoundEffect stingSound = new();

        private Oven oven;
        private Vector2 start;
        private float travelled;
        private float phase;
        private bool dead;

        public void Setup(Oven target) => oven = target;

        private void OnEnable()
        {
            if (health != null)
                health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnDied;
        }

        private void Start()
        {
            if (oven == null)
                oven = FindAnyObjectByType<Oven>();
            start = transform.position;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (dead || oven == null || oven.Burned)
                return;
            Vector2 target = oven.Mouth;
            float total = Mathf.Max(0.01f, Mathf.Abs(target.x - start.x));
            travelled = Mathf.Min(total, travelled + speed * Time.deltaTime);
            float t = travelled / total;
            float x = Mathf.Lerp(start.x, target.x, t);
            // Ondula, y la onda se apaga al llegar a la boca.
            float y = Mathf.Lerp(start.y, target.y, t) + Mathf.Sin(Time.time * waveSpeed + phase) * waveHeight * (1f - t);
            transform.position = new Vector3(x, y, transform.position.z);
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(target.x - start.x);
            transform.localScale = scale;
            if (t < 1f)
                return;
            // Llegó: pica la masa y se dispersa.
            dead = true;
            stingSound.Play();
            oven.Hit(ovenDamage);
            Destroy(gameObject);
        }

        private void OnDied() => dead = true;
    }
}
