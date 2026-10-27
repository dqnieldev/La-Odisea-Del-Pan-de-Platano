using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Pasa una lista de sprites a un ritmo fijo, sin Animator. Para efectos chicos: la bolita de masa volando
    /// (en loop) o el embarrón al chocar (una vez, se queda un momento desvaneciéndose y se destruye).
    /// </summary>
    public class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float framesPerSecond = 12f;
        [SerializeField] private bool loop = true;
        [Tooltip("Solo sin loop: al terminar, el último cuadro se desvanece en este tiempo y se destruye el objeto.")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.4f;
        [SerializeField] private bool destroyWhenDone;

        private float time;
        private Color baseColor;

        private void Awake()
        {
            if (target == null)
                target = GetComponent<SpriteRenderer>();
            baseColor = target.color;
            if (frames != null && frames.Length > 0)
                target.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0)
                return;

            time += Time.deltaTime;
            int index = Mathf.FloorToInt(time * framesPerSecond);
            if (index < frames.Length)
            {
                target.sprite = frames[index];
                return;
            }
            if (loop)
            {
                target.sprite = frames[index % frames.Length];
                return;
            }

            target.sprite = frames[^1];
            float fade = fadeOutDuration > 0f ? (time - frames.Length / framesPerSecond) / fadeOutDuration : 1f;
            target.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - Mathf.Clamp01(fade)));
            if (fade >= 1f && destroyWhenDone)
                Destroy(gameObject);
        }
    }
}
