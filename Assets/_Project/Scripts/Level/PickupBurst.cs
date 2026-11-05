using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Lo que queda al recoger un ingrediente: su dibujo salta, crece y se desvanece, y alrededor salen destellos
    /// dorados que se abren y se apagan. Se destruye solo.
    /// </summary>
    public class PickupBurst : MonoBehaviour
    {
        private const float Lifetime = 0.5f;
        private const int Sparkles = 8;
        // Por delante de Ulises (10) y detrás del texto que sale (20).
        private const int SortingOrder = 15;

        private SpriteRenderer drawing;
        private Transform[] sparkles;
        private SpriteRenderer[] sparkleRenderers;
        private Vector3 start;
        private float baseScale;
        private float age;

        public static void Spawn(SpriteRenderer source, Sprite sparkle, Color color)
        {
            var burst = new GameObject("Ingrediente recogido").AddComponent<PickupBurst>();
            burst.transform.position = source.transform.position;
            burst.start = source.transform.position;
            burst.baseScale = source.transform.lossyScale.x;

            burst.drawing = new GameObject("Dibujo").AddComponent<SpriteRenderer>();
            burst.drawing.transform.SetParent(burst.transform, false);
            burst.drawing.sprite = source.sprite;
            burst.drawing.color = source.color;
            burst.drawing.sortingOrder = SortingOrder + 1;

            if (sparkle == null)
                return;
            burst.sparkles = new Transform[Sparkles];
            burst.sparkleRenderers = new SpriteRenderer[Sparkles];
            for (int i = 0; i < Sparkles; i++)
            {
                var renderer = new GameObject("Destello").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(burst.transform, false);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, i * 360f / Sparkles);
                renderer.sprite = sparkle;
                renderer.color = color;
                renderer.sortingOrder = SortingOrder;
                burst.sparkles[i] = renderer.transform;
                burst.sparkleRenderers[i] = renderer;
            }
            burst.Update();
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / Lifetime;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float ease = 1f - (1f - t) * (1f - t);
            transform.position = start + Vector3.up * (ease * 0.7f);
            drawing.transform.localScale = Vector3.one * (baseScale * Mathf.Lerp(1f, 1.5f, ease));
            drawing.color = WithAlpha(drawing.color, 1f - t * t);

            if (sparkles == null)
                return;
            for (int i = 0; i < sparkles.Length; i++)
            {
                // Cada destello es una gota alargada que sale del centro hacia afuera y se encoge.
                Vector3 direction = sparkles[i].localRotation * Vector3.up;
                sparkles[i].localPosition = direction * Mathf.Lerp(0.25f, 1.3f, ease);
                sparkles[i].localScale = new Vector3(0.12f, 0.45f, 1f) * (1f - t);
                sparkleRenderers[i].color = WithAlpha(sparkleRenderers[i].color, 1f - t);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
