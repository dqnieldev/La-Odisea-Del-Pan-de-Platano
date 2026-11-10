using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Espuma alrededor de una panga: manchas blancas suaves a lo largo de la línea de flotación que crecen y se
    /// desvanecen cada una a su ritmo. Va en la superficie del agua (no sube y baja con la panga). Con
    /// <see cref="Splash"/> (cuando Ulises cae encima) la espuma se abre y saltan gotas.
    /// </summary>
    public class BoatFoam : MonoBehaviour
    {
        [Tooltip("Las manchas de espuma (hijos con SpriteRenderer).")]
        [SerializeField] private SpriteRenderer[] blobs = new SpriteRenderer[0];
        [Tooltip("Las gotas del chapuzón (hijos con SpriteRenderer, apagados hasta que salpica).")]
        [SerializeField] private SpriteRenderer[] drops = new SpriteRenderer[0];
        [SerializeField, Min(0.1f)] private float period = 1.6f;
        [SerializeField, Range(0f, 1f)] private float alpha = 0.55f;
        [SerializeField, Min(0.1f)] private float splashTime = 0.6f;

        private Vector3[] blobScale;
        private Vector3[] blobPosition;
        private Vector2[] dropVelocity;
        private float splashAt = -10f;

        private void Awake()
        {
            blobScale = new Vector3[blobs.Length];
            blobPosition = new Vector3[blobs.Length];
            for (int i = 0; i < blobs.Length; i++)
            {
                blobScale[i] = blobs[i].transform.localScale;
                blobPosition[i] = blobs[i].transform.localPosition;
            }
            dropVelocity = new Vector2[drops.Length];
            foreach (SpriteRenderer drop in drops)
                drop.enabled = false;
        }

        /// <summary>Se abre la espuma y saltan gotas hacia los lados.</summary>
        public void Splash()
        {
            splashAt = Time.time;
            for (int i = 0; i < drops.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                dropVelocity[i] = new Vector2(side * Random.Range(1f, 2.6f), Random.Range(2.5f, 4.5f));
                drops[i].transform.localPosition = new Vector3(side * Random.Range(0.6f, 1.4f), 0.05f, 0f);
                drops[i].enabled = true;
            }
        }

        private void Update()
        {
            float t = Time.time;
            float splash = Mathf.Clamp01(1f - (t - splashAt) / splashTime);
            for (int i = 0; i < blobs.Length; i++)
            {
                // Cada mancha late a su ritmo: crece, se desvanece y se corre un poquito de lado.
                float wave = Mathf.Sin(t * 2f * Mathf.PI / period + i * 1.7f) * 0.5f + 0.5f;
                float grow = 0.8f + 0.35f * wave + 0.5f * splash;
                blobs[i].transform.localScale = new Vector3(blobScale[i].x * grow, blobScale[i].y * (0.9f + 0.2f * wave), 1f);
                float spread = Mathf.Sign(blobPosition[i].x) * 0.35f * splash;
                blobs[i].transform.localPosition = blobPosition[i] + new Vector3(Mathf.Sin(t * 0.9f + i) * 0.08f + spread, 0f, 0f);
                Color color = blobs[i].color;
                color.a = alpha * (0.55f + 0.45f * wave) + 0.3f * splash;
                blobs[i].color = color;
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < drops.Length; i++)
            {
                if (!drops[i].enabled)
                    continue;
                dropVelocity[i] += Vector2.down * 14f * dt;
                drops[i].transform.localPosition += (Vector3)(dropVelocity[i] * dt);
                Color color = drops[i].color;
                color.a = splash;
                drops[i].color = color;
                // Al volver al agua (o al acabarse el chapuzón) desaparece.
                if (drops[i].transform.localPosition.y < 0f || splash <= 0f)
                    drops[i].enabled = false;
            }
        }
    }
}
