using System.Collections;
using System.Collections.Generic;
using Odisea.Core;
using Odisea.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Corazones del HUD: uno por cada punto de vida que le queda a Ulises (los perdidos no se ven).
    /// Al empezar entran uno por uno. El que se pierde destella, se rompe en trocitos, sacude la fila y desaparece; el
    /// que se recupera vuelve a aparecer con un brinco y un anillo dorado. Con una sola vida el último late y las orillas de la pantalla se
    /// ponen rojas. Todo en tiempo sin escala (sigue con la pausa o el tiempo lento).
    /// En el nivel 2 también muestra la vida del horno (con <see cref="oven"/> en lugar de <see cref="health"/>).
    /// </summary>
    public class HeartsDisplay : MonoBehaviour
    {
        [SerializeField] private Health health;
        [Tooltip("En lugar de una vida (Health): la del horno.")]
        [SerializeField] private Oven oven;
        [SerializeField] private Sprite fullHeart;
        [SerializeField] private Vector2 heartSize = new(72f, 66f);
        [SerializeField, Min(0f)] private float spacing = 12f;
        [Tooltip("Opcional: el hueco que deja cada corazón perdido (transparente = no se ve).")]
        [SerializeField] private Color hollowColor = Color.clear;

        [Header("Efectos")]
        [SerializeField, Min(0.01f)] private float popDuration = 0.25f;
        [SerializeField, Min(0f)] private float popScale = 0.4f;
        [Tooltip("Entre la entrada de un corazón y el siguiente, al empezar el nivel.")]
        [SerializeField, Min(0f)] private float entranceStagger = 0.08f;
        [Tooltip("Trocitos del corazón roto (un círculo); sin sprite no se rompe.")]
        [SerializeField] private Sprite shardSprite;
        [SerializeField] private Color shardColor = new(0.784f, 0.263f, 0.169f);
        [SerializeField, Min(0)] private int shardCount = 5;
        [Tooltip("Anillo que se expande al curar.")]
        [SerializeField] private Sprite ringSprite;
        [SerializeField] private Color ringColor = new(0.961f, 0.718f, 0.192f);
        [SerializeField, Min(0f)] private float shakeStrength = 10f;

        [Header("Última vida")]
        [Tooltip("Orillas rojas de la pantalla (Image a pantalla completa) mientras queda una sola vida.")]
        [SerializeField] private Image lowHealthVignette;
        [SerializeField, Range(0f, 1f)] private float vignetteAlpha = 0.45f;
        [Tooltip("Latidos por segundo del último corazón.")]
        [SerializeField, Min(0.1f)] private float heartbeatRate = 1.1f;

        private readonly List<Image> hearts = new();
        private readonly List<Image> hollows = new();
        private readonly List<float> punch = new();
        // Escala de entrada de cada corazón (0 a 1, con rebote).
        private readonly List<float> appear = new();
        private RectTransform rect;
        private Vector2 home;
        private float shakeUntil;
        private int shownHealth;
        private float vignette;

        private void Start()
        {
            if (health == null && oven == null)
            {
                Debug.LogError($"{name}: HeartsDisplay necesita una referencia a Health.", this);
                enabled = false;
                return;
            }

            rect = (RectTransform)transform;
            home = rect.anchoredPosition;
            int max = health != null ? health.Max : oven.Max;
            int current = health != null ? health.Current : oven.Current;
            BuildHearts(max);
            shownHealth = current;
            Refresh(current, animate: false);
            if (health != null)
                health.HealthChanged += OnHealthChanged;
            else
                oven.Changed += OnHealthChanged;
            for (int i = 0; i < shownHealth; i++)
                StartCoroutine(Entrance(i, i * entranceStagger));
            if (lowHealthVignette != null)
                SetVignette(0f);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.HealthChanged -= OnHealthChanged;
            if (oven != null)
                oven.Changed -= OnHealthChanged;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (max != hearts.Count)
                BuildHearts(max);
            Refresh(current, animate: true);
        }

        private void BuildHearts(int count)
        {
            foreach (Image heart in hearts)
                Destroy(heart.gameObject);
            foreach (Image hollow in hollows)
                Destroy(hollow.gameObject);
            hearts.Clear();
            hollows.Clear();
            punch.Clear();
            appear.Clear();

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Corazon {i + 1}", typeof(RectTransform), typeof(Image));
                var heartRect = (RectTransform)go.transform;
                heartRect.SetParent(transform, false);
                heartRect.anchorMin = heartRect.anchorMax = new Vector2(0f, 1f);
                // Pivote al centro: así brinca y late desde su centro.
                heartRect.pivot = new Vector2(0.5f, 0.5f);
                heartRect.sizeDelta = heartSize;
                heartRect.anchoredPosition = new Vector2(i * (heartSize.x + spacing) + heartSize.x / 2f, -heartSize.y / 2f);
                if (hollowColor.a > 0f)
                {
                    // Detrás del corazón: se ve cuando este se pierde.
                    Image hollow = Effect($"Hueco {i + 1}", fullHeart, hollowColor, heartRect.anchoredPosition, heartSize);
                    hollow.preserveAspect = true;
                    hollow.transform.SetSiblingIndex(heartRect.GetSiblingIndex());
                    hollows.Add(hollow);
                }

                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                hearts.Add(image);
                punch.Add(0f);
                appear.Add(1f);
            }
        }

        private void Refresh(int current, bool animate)
        {
            for (int i = 0; i < hearts.Count; i++)
            {
                bool full = i < current;
                hearts[i].sprite = fullHeart;
                if (!animate)
                {
                    appear[i] = full ? 1f : 0f;
                    continue;
                }

                // Solo se animan los corazones que cambiaron.
                bool changed = (i < shownHealth) != full;
                if (!changed)
                    continue;
                if (full)
                {
                    StartCoroutine(Entrance(i, 0f));
                    StartCoroutine(Gain(hearts[i].rectTransform));
                }
                else
                {
                    Lose(i);
                }
            }
            shownHealth = current;
        }

        private void Update()
        {
            // Sacudida de la fila al perder un corazón.
            float shake = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / 0.3f);
            rect.anchoredPosition = home + (shake > 0f ? Random.insideUnitCircle * shakeStrength * shake : Vector2.zero);

            // Última vida: el corazón late (doble pulso, como un corazón de verdad) y la pantalla se enrojece.
            bool lastLife = shownHealth == 1 && hearts.Count > 1;
            float beat = lastLife ? Heartbeat(Time.unscaledTime * heartbeatRate) : 0f;
            for (int i = 0; i < hearts.Count; i++)
            {
                punch[i] = Mathf.MoveTowards(punch[i], 0f, Time.unscaledDeltaTime * 4f);
                float scale = appear[i] * (1f + punch[i] + (i == 0 ? beat * 0.14f : 0f));
                hearts[i].rectTransform.localScale = Vector3.one * scale;
            }
            if (lowHealthVignette != null)
            {
                vignette = Mathf.MoveTowards(vignette, lastLife ? 1f : 0f, Time.unscaledDeltaTime * 2f);
                SetVignette(vignette * vignetteAlpha * (0.75f + beat * 0.25f));
            }
        }

        // 0 a 1: dos golpes seguidos ("pum-pum") y una pausa.
        private static float Heartbeat(float t)
        {
            float phase = t - Mathf.Floor(t);
            float first = Mathf.Max(0f, 1f - Mathf.Abs(phase - 0.08f) / 0.08f);
            float second = Mathf.Max(0f, 1f - Mathf.Abs(phase - 0.28f) / 0.09f) * 0.7f;
            return Mathf.Max(first, second);
        }

        private void SetVignette(float alpha)
        {
            Color color = lowHealthVignette.color;
            color.a = alpha;
            lowHealthVignette.color = color;
            lowHealthVignette.enabled = alpha > 0.001f;
        }

        // ---------- Efectos ----------

        private IEnumerator Entrance(int index, float delay)
        {
            appear[index] = 0f;
            for (float t = 0f; t < delay; t += Time.unscaledDeltaTime)
                yield return null;
            for (float t = 0f; t < popDuration && index < appear.Count; t += Time.unscaledDeltaTime)
            {
                float p = t / popDuration;
                appear[index] = p + Mathf.Sin(p * Mathf.PI) * popScale;
                yield return null;
            }
            if (index < appear.Count)
                appear[index] = 1f;
        }

        // Destello blanco, trocitos que saltan y caen, y la fila se sacude.
        private void Lose(int index)
        {
            RectTransform heart = hearts[index].rectTransform;
            punch[index] = popScale;
            shakeUntil = Time.unscaledTime + 0.3f;
            StartCoroutine(Flash(heart));
            StartCoroutine(Vanish(index));
            if (shardSprite == null)
                return;
            for (int i = 0; i < shardCount; i++)
                StartCoroutine(Shard(heart.anchoredPosition, i));
        }

        // Se encoge hasta desaparecer (deja de ocupar su lugar en la fila: los que quedan son los de la izquierda).
        private IEnumerator Vanish(int index)
        {
            const float duration = 0.22f;
            for (float t = 0f; t < duration && index < appear.Count; t += Time.unscaledDeltaTime)
            {
                appear[index] = 1f - t / duration;
                yield return null;
            }
            if (index < appear.Count)
                appear[index] = 0f;
        }

        private IEnumerator Gain(RectTransform heart)
        {
            int index = hearts.FindIndex(h => h.rectTransform == heart);
            if (index >= 0)
                punch[index] = popScale;
            if (ringSprite == null)
                yield break;
            Image ring = Effect("Anillo", ringSprite, ringColor, heart.anchoredPosition, heartSize * 0.8f);
            const float duration = 0.45f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = t / duration;
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.2f, 1f - (1f - p) * (1f - p));
                ring.color = new Color(ringColor.r, ringColor.g, ringColor.b, ringColor.a * (1f - p));
                yield return null;
            }
            Destroy(ring.gameObject);
        }

        private IEnumerator Flash(RectTransform heart)
        {
            Image flash = Effect("Destello", fullHeart, Color.white, heart.anchoredPosition, heartSize);
            flash.preserveAspect = true;
            const float duration = 0.25f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = t / duration;
                flash.color = new Color(1f, 1f, 1f, 1f - p);
                flash.rectTransform.localScale = Vector3.one * (1f + p * 0.5f);
                yield return null;
            }
            Destroy(flash.gameObject);
        }

        private IEnumerator Shard(Vector2 origin, int index)
        {
            float size = Random.Range(0.16f, 0.26f) * heartSize.x;
            Image shard = Effect("Trocito", shardSprite, shardColor, origin, new Vector2(size, size * 0.8f));
            // Salen en abanico hacia arriba y caen con gravedad, girando.
            float angle = Mathf.Lerp(30f, 150f, (index + Random.Range(0.2f, 0.8f)) / shardCount) * Mathf.Deg2Rad;
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(220f, 380f);
            float spin = Random.Range(-540f, 540f);
            const float duration = 0.7f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                velocity.y -= 1400f * dt;
                shard.rectTransform.anchoredPosition += velocity * dt;
                shard.rectTransform.Rotate(0f, 0f, spin * dt);
                float fade = 1f - Mathf.Clamp01((t - duration * 0.5f) / (duration * 0.5f));
                shard.color = new Color(shardColor.r, shardColor.g, shardColor.b, fade);
                yield return null;
            }
            Destroy(shard.gameObject);
        }

        private Image Effect(string effectName, Sprite sprite, Color color, Vector2 position, Vector2 size)
        {
            var go = new GameObject(effectName, typeof(RectTransform), typeof(Image));
            var effect = (RectTransform)go.transform;
            effect.SetParent(transform, false);
            effect.anchorMin = effect.anchorMax = new Vector2(0f, 1f);
            effect.pivot = new Vector2(0.5f, 0.5f);
            effect.anchoredPosition = position;
            effect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
