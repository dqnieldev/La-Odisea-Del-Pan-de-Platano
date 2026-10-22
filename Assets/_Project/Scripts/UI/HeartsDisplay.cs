using System.Collections;
using System.Collections.Generic;
using Odisea.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Corazones del HUD: uno por cada punto de vida máximo de Ulises.
    /// El corazón que se pierde brinca y se vacía; el que se recupera brinca y se llena.
    /// </summary>
    public class HeartsDisplay : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Sprite fullHeart;
        [SerializeField] private Sprite emptyHeart;
        [SerializeField] private Vector2 heartSize = new(72f, 66f);
        [SerializeField, Min(0f)] private float spacing = 12f;
        [SerializeField, Min(0.01f)] private float popDuration = 0.25f;
        [SerializeField, Min(0f)] private float popScale = 0.4f;

        private readonly List<Image> hearts = new();
        private int shownHealth;

        private void Start()
        {
            if (health == null)
            {
                Debug.LogError($"{name}: HeartsDisplay necesita una referencia a Health.", this);
                enabled = false;
                return;
            }

            BuildHearts(health.Max);
            shownHealth = health.Current;
            Refresh(health.Current, animate: false);
            health.HealthChanged += OnHealthChanged;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.HealthChanged -= OnHealthChanged;
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
            hearts.Clear();

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Corazon {i + 1}", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = heartSize;
                rect.anchoredPosition = new Vector2(i * (heartSize.x + spacing), 0f);

                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                hearts.Add(image);
            }
        }

        private void Refresh(int current, bool animate)
        {
            for (int i = 0; i < hearts.Count; i++)
            {
                bool full = i < current;
                hearts[i].sprite = full ? fullHeart : emptyHeart;

                // Solo brincan los corazones que cambiaron.
                bool changed = (i < shownHealth) != full;
                if (animate && changed)
                    StartCoroutine(Pop(hearts[i].rectTransform));
            }
            shownHealth = current;
        }

        private IEnumerator Pop(RectTransform heart)
        {
            for (float t = 0f; t < popDuration; t += Time.unscaledDeltaTime)
            {
                float wave = Mathf.Sin(t / popDuration * Mathf.PI);
                heart.localScale = Vector3.one * (1f + popScale * wave);
                yield return null;
            }
            heart.localScale = Vector3.one;
        }
    }
}
