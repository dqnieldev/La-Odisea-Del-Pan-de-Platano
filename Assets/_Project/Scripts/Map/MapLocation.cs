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
    /// Un lugar del mapa (la panadería, el mercado, el horno, La Tovara): su nombre, a qué nivel lleva y si ya se
    /// puede entrar. Se abre al terminar el lugar anterior; si su escena todavía no existe, dice "Próximamente".
    /// Su collider es el área a la que se le da clic (lo maneja <see cref="OverworldMap"/>).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MapLocation : MonoBehaviour
    {
        [Tooltip("Identificador en GameProgress (tutorial, mercado, horno, tovara).")]
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
        [Tooltip("Se oscurecen mientras está bloqueado.")]
        [SerializeField] private SpriteRenderer[] tinted = new SpriteRenderer[0];
        [Tooltip("Se muestra cuando ya se terminó (banderita).")]
        [SerializeField] private GameObject doneMark;

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
