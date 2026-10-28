using TMPro;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>Texto en el mundo que sube, se desvanece y se destruye (lo que dio un ítem al recogerlo).</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class FloatingLabel : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float lifetime = 1.1f;
        [SerializeField] private float rise = 1.2f;

        private TMP_Text text;
        private Vector3 start;
        private float age;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            start = transform.position;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            // Sube rápido al principio y se frena; se desvanece en la segunda mitad.
            transform.position = start + Vector3.up * (rise * (1f - (1f - t) * (1f - t)));
            text.alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
