using System.Collections;
using Odisea.Core;
using Odisea.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// HUD del nivel 2.
    /// La vida del horno: una tablita con un ladrillo de adobe por punto; con cada golpe el ladrillo de la derecha se
    /// agrieta y se cae en dos pedazos (sin el dibujo, una barra que tiembla).
    /// El horneado: un reloj de cocina cuyo anillo se va llenando, con la aguja girando y el pan del centro pasando de
    /// crudo a dorado; al terminar, el reloj suena y tiembla (sin el dibujo, un relleno que se va dorando).
    /// </summary>
    public class OvenHud : MonoBehaviour
    {
        [SerializeField] private Oven oven;
        [SerializeField] private BakeTimer bake;

        [Header("Vida: ladrillos")]
        [SerializeField] private RectTransform board;
        [Tooltip("De izquierda a derecha; se caen de derecha a izquierda.")]
        [SerializeField] private Image[] bricks;
        [SerializeField] private Sprite brickSprite;
        [SerializeField] private Sprite crackedSprite;
        [SerializeField] private Sprite[] halfSprites;
        [Tooltip("Color del hueco que deja un ladrillo caído.")]
        [SerializeField] private Color emptyColor = new(0.12f, 0.07f, 0.04f, 0.45f);
        [Tooltip("Con esta vida o menos, los ladrillos que quedan laten en rojo.")]
        [SerializeField, Min(0)] private int dangerHealth = 3;

        [Header("Vida: barra (sin dibujo)")]
        [SerializeField] private Image ovenFill;
        [SerializeField] private RectTransform ovenIcon;

        [Header("Horneado")]
        [SerializeField] private Image bakeFill;
        [SerializeField] private RectTransform needle;
        [SerializeField] private Image bakedBread;
        [SerializeField] private RectTransform clock;
        [SerializeField] private SoundEffect ringSound = new();
        [SerializeField] private CanvasGroup bakeGroup;
        [Tooltip("Color de la masa cruda y del pan ya dorado (el relleno).")]
        [SerializeField] private Color rawColor = new(0.96f, 0.86f, 0.62f);
        [SerializeField] private Color bakedColor = new(0.72f, 0.42f, 0.16f);

        private float shake;
        private float ring;
        private int shown = -1;
        private bool rang;

        private bool UsesBricks => bricks != null && bricks.Length > 0 && brickSprite != null;

        private void OnEnable()
        {
            if (oven != null)
                oven.Changed += OnOvenChanged;
        }

        private void OnDisable()
        {
            if (oven != null)
                oven.Changed -= OnOvenChanged;
        }

        private void OnOvenChanged(int current, int max)
        {
            if (UsesBricks)
            {
                int count = Mathf.Clamp(Mathf.RoundToInt((float)current / Mathf.Max(1, max) * bricks.Length), 0, bricks.Length);
                if (shown < 0)
                    shown = bricks.Length;
                for (int i = 0; i < bricks.Length; i++)
                {
                    bool had = i < shown, has = i < count;
                    if (had && !has)
                        StartCoroutine(Break(bricks[i]));
                    else if (has && !had)
                        Restore(bricks[i]);
                }
                if (count < shown)
                    shake = 0.3f;
                shown = count;
                return;
            }
            if (ovenFill == null)
                return;
            float value = max > 0 ? (float)current / max : 0f;
            if (value < ovenFill.fillAmount)
                shake = 0.3f;
            ovenFill.fillAmount = value;
        }

        private void Restore(Image brick)
        {
            brick.sprite = brickSprite;
            brick.color = Color.white;
        }

        // Se agrieta un instante y luego cae en dos pedazos que giran y se desvanecen; queda el hueco.
        private IEnumerator Break(Image brick)
        {
            if (crackedSprite != null)
                brick.sprite = crackedSprite;
            RectTransform rect = brick.rectTransform;
            Vector2 home = rect.anchoredPosition;
            for (float t = 0f; t < 0.14f; t += Time.deltaTime)
            {
                rect.anchoredPosition = home + new Vector2(Mathf.Sin(t * 120f) * 3f, 0f);
                yield return null;
            }
            rect.anchoredPosition = home;
            brick.sprite = brickSprite;
            brick.color = emptyColor;
            if (halfSprites == null)
                yield break;
            var pieces = new RectTransform[halfSprites.Length];
            var velocity = new Vector2[halfSprites.Length];
            var spin = new float[halfSprites.Length];
            Vector2 size = rect.rect.size;
            for (int i = 0; i < halfSprites.Length; i++)
            {
                if (halfSprites[i] == null)
                    continue;
                var piece = new GameObject("Pedazo", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                piece.SetParent(rect.parent, false);
                piece.anchorMin = piece.anchorMax = rect.anchorMin;
                float side = i == 0 ? -1f : 1f;
                piece.sizeDelta = new Vector2(size.x * 0.5f, size.y);
                piece.anchoredPosition = home + new Vector2(side * size.x * 0.25f, 0f);
                var image = piece.GetComponent<Image>();
                image.sprite = halfSprites[i];
                image.preserveAspect = true;
                image.raycastTarget = false;
                pieces[i] = piece;
                velocity[i] = new Vector2(side * Random.Range(60f, 110f), Random.Range(140f, 200f));
                spin[i] = side * -Random.Range(200f, 380f);
            }
            const float life = 0.8f;
            for (float t = 0f; t < life; t += Time.deltaTime)
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    if (pieces[i] == null)
                        continue;
                    velocity[i].y -= 900f * Time.deltaTime;
                    pieces[i].anchoredPosition += velocity[i] * Time.deltaTime;
                    pieces[i].localRotation *= Quaternion.Euler(0f, 0f, spin[i] * Time.deltaTime);
                    pieces[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((t - life * 0.5f) / (life * 0.5f)));
                }
                yield return null;
            }
            foreach (RectTransform piece in pieces)
            {
                if (piece != null)
                    Destroy(piece.gameObject);
            }
        }

        private void Update()
        {
            UpdateBake();
            UpdateDanger();
            RectTransform shaken = UsesBricks ? board : ovenIcon;
            if (shake <= 0f || shaken == null)
                return;
            shake -= Time.deltaTime;
            float amount = Mathf.Clamp01(shake / 0.3f);
            shaken.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 60f) * (UsesBricks ? 3f : 12f) * amount);
            if (shake <= 0f)
                shaken.localRotation = Quaternion.identity;
        }

        // Pocos ladrillos: los que quedan laten en rojo.
        private void UpdateDanger()
        {
            if (!UsesBricks || shown < 0)
                return;
            bool danger = shown > 0 && shown <= dangerHealth;
            float pulse = danger ? Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.45f : 0f;
            var tint = new Color(1f, 1f - pulse, 1f - pulse);
            for (int i = 0; i < shown && i < bricks.Length; i++)
            {
                if (bricks[i].sprite == brickSprite)
                    bricks[i].color = tint;
            }
        }

        private void UpdateBake()
        {
            if (bake == null)
                return;
            float progress = bake.Progress;
            if (bakeFill != null)
            {
                bakeFill.fillAmount = progress;
                if (bakedBread == null)
                    bakeFill.color = Color.Lerp(rawColor, bakedColor, progress);
            }
            if (needle != null)
                needle.localRotation = Quaternion.Euler(0f, 0f, -360f * progress);
            if (bakedBread != null)
                bakedBread.color = new Color(1f, 1f, 1f, progress);
            if (bake.Done && !rang)
            {
                // Suena la campanita: el reloj tiembla un momento.
                rang = true;
                ring = 1.2f;
                ringSound.Play();
            }
            if (clock != null && ring > 0f)
            {
                ring -= Time.deltaTime;
                float amount = Mathf.Clamp01(ring / 1.2f);
                clock.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 50f) * 10f * amount);
                clock.localScale = Vector3.one * (1f + 0.12f * amount);
                if (ring <= 0f)
                {
                    clock.localRotation = Quaternion.identity;
                    clock.localScale = Vector3.one;
                }
            }
            if (bakeGroup != null)
                bakeGroup.alpha = Mathf.MoveTowards(bakeGroup.alpha, bake.Done ? 0f : 1f, Time.deltaTime * 2f);
        }
    }
}
