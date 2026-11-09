using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Sombra ovalada en el piso debajo de un personaje, para que se vea que pisa el suelo del escenario. Busca el piso
    /// hacia abajo: al saltar o volar la sombra se queda en el piso y se hace más chica y más clara. Es un objeto aparte
    /// (no hijo), así no se voltea ni se encoge con el personaje; se borra con él.
    /// </summary>
    public class GroundShadow : MonoBehaviour
    {
        [Tooltip("Un punto difuso (se estira a óvalo).")]
        [SerializeField] private Sprite sprite;
        [Tooltip("Sin luces (Sprite-Unlit): si no, la luz del fuego la aclara hasta que no se ve.")]
        [SerializeField] private Material material;
        [Tooltip("Color de la sombra; lo transparente va en la textura (Sombra.png): con alfa aquí casi no se ve.")]
        [SerializeField] private Color color = new(0.12f, 0.06f, 0.02f, 1f);
        [Tooltip("Ancho y alto de la sombra con el personaje en el piso.")]
        [SerializeField] private Vector2 size = new(1.2f, 0.3f);
        [SerializeField] private Vector2 offset;
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("A esta altura sobre el piso (o más) la sombra ya no se ve.")]
        [SerializeField, Min(0.1f)] private float maxHeight = 5f;
        [Tooltip("Lo que se achica en lo más alto (0 = nada, 1 = desaparece).")]
        [SerializeField, Range(0f, 1f)] private float shrink = 0.5f;
        [SerializeField] private int sortingOrder = -2;
        [Tooltip("Opcional: al morir, la sombra se desvanece.")]
        [SerializeField] private Health health;

        private SpriteRenderer shadow;
        private float fade = 1f;

        private void Awake()
        {
            if (sprite == null)
                return;
            shadow = new GameObject($"Sombra de {name}").AddComponent<SpriteRenderer>();
            shadow.sprite = sprite;
            if (material != null)
                shadow.sharedMaterial = material;
            shadow.sortingOrder = sortingOrder;
            shadow.color = new Color(color.r, color.g, color.b, 0f);
        }

        private void OnEnable()
        {
            if (shadow != null)
                shadow.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (shadow != null)
                shadow.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (shadow != null)
                Destroy(shadow.gameObject);
        }

        private void LateUpdate()
        {
            if (shadow == null)
                return;
            if (health != null && health.IsDead)
                fade = Mathf.MoveTowards(fade, 0f, Time.deltaTime / 0.4f);
            // Desde un poco arriba de los pies, para no empezar dentro del piso.
            Vector2 from = (Vector2)transform.position + offset + Vector2.up * 0.5f;
            RaycastHit2D hit = Physics2D.Raycast(from, Vector2.down, maxHeight + 0.5f, groundLayers);
            if (!hit)
            {
                shadow.enabled = false;
                return;
            }
            shadow.enabled = true;
            float height = Mathf.Clamp01((hit.distance - 0.5f) / maxHeight);
            float scale = 1f - shrink * height;
            shadow.transform.position = new Vector3(from.x, hit.point.y, 0f);
            Vector2 spriteSize = sprite.bounds.size;
            shadow.transform.localScale = new Vector3(size.x * scale / spriteSize.x, size.y * scale / spriteSize.y, 1f);
            shadow.color = new Color(color.r, color.g, color.b, color.a * (1f - height) * fade);
        }
    }
}
