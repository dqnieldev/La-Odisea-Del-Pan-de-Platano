using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Adorno de una meta (el pan horneado): el dibujo flota subiendo y bajando, y a su alrededor aparecen destellos
    /// (estrellitas de cuatro puntas) que crecen girando, suben un poco y se apagan, para luego salir en otro lado.
    /// Solo mueve el dibujo: el área que se toca para ganar se queda quieta.
    /// </summary>
    public class GoalSparkles : MonoBehaviour
    {
        [Header("Flotar")]
        [Tooltip("Lo que sube y baja (el dibujo, no la meta completa).")]
        [SerializeField] private Transform floating;
        [SerializeField, Min(0f)] private float floatHeight = 0.25f;
        [SerializeField, Min(0.1f)] private float floatPeriod = 2.2f;

        [Header("Destellos")]
        [SerializeField] private Sprite sparkle;
        [Tooltip("Sprite-Unlit: las luces de la escena no los apagan.")]
        [SerializeField] private Material material;
        [Tooltip("Lo transparente va en la textura: con alfa en el color casi no se ven.")]
        [SerializeField] private Color color = new(0.72f, 0.42f, 1f);
        [SerializeField, Min(1)] private int count = 14;
        [Tooltip("Medio ancho y medio alto del óvalo donde salen.")]
        [SerializeField] private Vector2 area = new(1.3f, 0.95f);
        [SerializeField] private Vector2 sizeRange = new(0.3f, 0.65f);
        [SerializeField] private Vector2 lifeRange = new(0.7f, 1.4f);
        [Tooltip("Lo que sube cada destello mientras dura.")]
        [SerializeField] private float rise = 0.35f;
        [Tooltip("Orden del dibujo: la mitad de los destellos va detrás y la mitad delante.")]
        [SerializeField] private int drawingOrder = 5;

        private Vector3 floatingStart;
        private float floatStartTime;
        private SpriteRenderer[] sparkles;
        private float[] starts, lives, sizes, spins;
        private Vector3[] origins;

        private void Awake()
        {
            if (floating != null)
                floatingStart = floating.localPosition;
            if (sparkle == null)
                return;
            sparkles = new SpriteRenderer[count];
            starts = new float[count];
            lives = new float[count];
            sizes = new float[count];
            spins = new float[count];
            origins = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var renderer = new GameObject("Destello").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(transform, false);
                renderer.sprite = sparkle;
                renderer.color = color;
                if (material != null)
                    renderer.sharedMaterial = material;
                renderer.sortingOrder = drawingOrder + (i % 2 == 0 ? 1 : -1);
                sparkles[i] = renderer;
            }
        }

        private void OnEnable()
        {
            floatStartTime = Time.time;
            if (sparkles == null)
                return;
            // Escalonados: no salen todos a la vez.
            for (int i = 0; i < sparkles.Length; i++)
                Restart(i, Time.time - Random.Range(0f, lifeRange.y));
        }

        private void Update()
        {
            float now = Time.time;
            if (floating != null)
            {
                float wave = Mathf.Sin((now - floatStartTime) * 2f * Mathf.PI / floatPeriod);
                floating.localPosition = floatingStart + Vector3.up * (wave * floatHeight);
            }
            if (sparkles == null)
                return;
            for (int i = 0; i < sparkles.Length; i++)
            {
                float p = (now - starts[i]) / lives[i];
                if (p >= 1f)
                {
                    Restart(i, now);
                    p = 0f;
                }
                // Crece y se encoge (sin alfa: se ve igual de vivo), girando y subiendo.
                float shine = p <= 0f ? 0f : Mathf.Sin(p * Mathf.PI);
                Transform t = sparkles[i].transform;
                t.localScale = Vector3.one * (sizes[i] * shine);
                t.localPosition = origins[i] + Vector3.up * (rise * Mathf.Max(0f, p));
                t.localRotation = Quaternion.Euler(0f, 0f, spins[i] * Mathf.Max(0f, p));
            }
        }

        private void Restart(int i, float now)
        {
            Vector2 point = Random.insideUnitCircle;
            origins[i] = new Vector3(point.x * area.x, point.y * area.y, 0f);
            starts[i] = now + Random.Range(0f, 0.4f);
            lives[i] = Random.Range(lifeRange.x, lifeRange.y);
            sizes[i] = Random.Range(sizeRange.x, sizeRange.y);
            spins[i] = Random.Range(-90f, 90f);
        }
    }
}
