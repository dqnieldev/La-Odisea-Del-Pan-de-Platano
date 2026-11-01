using System.Collections;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Map
{
    public enum LocationState
    {
        Available,
        Locked,
        ComingSoon,
    }

    /// <summary>
    /// Un lugar del mapa (la panadería, el mercado, el horno, La Tovara, el festival): su nombre, a qué nivel lleva y
    /// si ya se puede entrar. Se abre al terminar el lugar anterior; si su escena todavía no existe, dice
    /// "Próximamente". Mientras no se abre se ve en blanco y negro (una copia en gris encima del dibujo); al abrirse,
    /// el color vuelve como pintura que se extiende desde su base (<see cref="RevealColor"/>).
    /// Su collider es el área a la que se le da clic (lo maneja <see cref="OverworldMap"/>).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MapLocation : MonoBehaviour
    {
        [Tooltip("Identificador en GameProgress (tutorial, mercado, horno, tovara, festival).")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Frase corta debajo del nombre.")]
        [SerializeField] private string subtitle;
        [Tooltip("Escena del nivel (nombre en Build Settings).")]
        [SerializeField] private string sceneName;
        [Tooltip("Historieta antes del nivel (nombre del asset); vacío = directo al nivel.")]
        [SerializeField] private string introStory;
        [Tooltip("Lugar que hay que terminar para que este se abra; vacío = siempre abierto.")]
        [SerializeField] private string requires;
        [SerializeField] private string requiresName;

        [Header("Dibujo en el mapa")]
        [Tooltip("Se oscurecen mientras está bloqueado (opcional).")]
        [SerializeField] private SpriteRenderer[] tinted = new SpriteRenderer[0];
        [Tooltip("Se muestra cuando ya se terminó (banderita).")]
        [SerializeField] private GameObject doneMark;

        [Header("Blanco y negro")]
        [Tooltip("Copia en gris del lugar, encima del dibujo, mientras no se abre.")]
        [SerializeField] private SpriteRenderer grayOverlay;
        [Tooltip("Forma de la mancha que borra el gris (un círculo).")]
        [SerializeField] private Sprite revealShape;
        [Tooltip("Aro que acompaña la orilla de la mancha (opcional).")]
        [SerializeField] private Sprite revealRing;
        [SerializeField] private Color ringColor = new(0.961f, 0.718f, 0.192f, 0.9f);
        [SerializeField, Min(0.1f)] private float revealDuration = 1.5f;
        [Tooltip("Base dorada de \"alcanzado\" sobre la de piedra (aparece al abrirse).")]
        [SerializeField] private GameObject reachedMark;

        public string Id => id;
        public string DisplayName => displayName;
        public string Subtitle => subtitle;
        public string RequiresName => requiresName;
        public bool IsDone => GameProgress.IsDone(id);
        public bool IsLocked => State == LocationState.Locked;

        public LocationState State
        {
            get
            {
                if (!string.IsNullOrEmpty(requires) && !GameProgress.IsDone(requires))
                    return LocationState.Locked;
                return Application.CanStreamedLevelBeLoaded(sceneName) ? LocationState.Available : LocationState.ComingSoon;
            }
        }

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void Start()
        {
            // Bloqueado se oscurece sobre su propio color.
            if (IsLocked)
            {
                foreach (SpriteRenderer renderer in tinted)
                {
                    if (renderer != null)
                        renderer.color *= new Color(0.55f, 0.52f, 0.5f);
                }
            }
            if (doneMark != null)
                doneMark.SetActive(IsDone);
        }

        /// <summary>En color (con su base dorada) o en blanco y negro, sin animación.</summary>
        public void ShowColor(bool colored)
        {
            if (grayOverlay != null)
                grayOverlay.gameObject.SetActive(!colored);
            if (reachedMark != null)
                reachedMark.SetActive(colored);
        }

        /// <summary>
        /// El color vuelve: una mancha que crece desde la base borra el gris, con un aro dorado en su orilla, y al
        /// final brinca la base dorada.
        /// </summary>
        public IEnumerator RevealColor()
        {
            if (grayOverlay == null || revealShape == null || !grayOverlay.gameObject.activeSelf)
            {
                ShowColor(true);
                yield break;
            }

            // La máscara solo toca a esta copia en gris (su propio orden), no a la de otros lugares.
            var mask = new GameObject("Mancha de color").AddComponent<SpriteMask>();
            mask.transform.SetParent(transform, false);
            mask.sprite = revealShape;
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = mask.backSortingLayerID = grayOverlay.sortingLayerID;
            mask.frontSortingOrder = grayOverlay.sortingOrder;
            mask.backSortingOrder = grayOverlay.sortingOrder - 1;
            grayOverlay.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            SpriteRenderer ring = null;
            if (revealRing != null)
            {
                ring = new GameObject("Aro").AddComponent<SpriteRenderer>();
                ring.transform.SetParent(transform, false);
                ring.sprite = revealRing;
                ring.sortingLayerID = grayOverlay.sortingLayerID;
                ring.sortingOrder = grayOverlay.sortingOrder + 20;
            }

            // Hasta la esquina más lejana de la copia en gris.
            Bounds bounds = grayOverlay.bounds;
            Vector2 from = transform.position;
            float radius = 0f;
            foreach (Vector2 corner in new[]
                     {
                         (Vector2)bounds.min, (Vector2)bounds.max, new Vector2(bounds.min.x, bounds.max.y),
                         new Vector2(bounds.max.x, bounds.min.y),
                     })
                radius = Mathf.Max(radius, Vector2.Distance(from, corner));

            for (float t = 0f; t < revealDuration; t += Time.deltaTime)
            {
                float p = t / revealDuration;
                float eased = 1f - (1f - p) * (1f - p) * (1f - p);
                float diameter = radius * 2f * eased;
                mask.transform.localScale = Vector3.one * (diameter / revealShape.bounds.size.x);
                if (ring != null)
                {
                    // El aro del sprite está a ~82 % de su radio.
                    ring.transform.localScale = Vector3.one * (diameter / (revealRing.bounds.size.x * 0.82f));
                    ring.color = new Color(ringColor.r, ringColor.g, ringColor.b, ringColor.a * (1f - p));
                }
                yield return null;
            }

            Destroy(mask.gameObject);
            if (ring != null)
                Destroy(ring.gameObject);
            grayOverlay.maskInteraction = SpriteMaskInteraction.None;
            ShowColor(true);
            if (reachedMark != null)
                yield return Pop(reachedMark.transform, 0.35f);
        }

        private static IEnumerator Pop(Transform target, float duration)
        {
            Vector3 size = target.localScale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                target.localScale = size * (p + Mathf.Sin(p * Mathf.PI) * 0.4f);
                yield return null;
            }
            target.localScale = size;
        }

        /// <summary>Entra: recuerda el lugar (para volver aquí) y abre su historieta y su nivel.</summary>
        public void Enter(Vector2? viewportCenter = null)
        {
            if (State != LocationState.Available)
                return;
            GameProgress.LastMapLocation = id;
            SceneFlow.PlayStory(introStory, sceneName, viewportCenter);
        }
    }
}
