using UnityEngine;

namespace Odisea.Core
{
    public enum SurfaceKind
    {
        Arena,
        Madera,
    }

    /// <summary>
    /// De qué está hecho un suelo, para que los pasos suenen distinto (PlayerSounds). Un suelo sin este componente
    /// cuenta como arena.
    /// </summary>
    public class GroundSurface : MonoBehaviour
    {
        [SerializeField] private SurfaceKind kind = SurfaceKind.Arena;

        public SurfaceKind Kind => kind;

        /// <summary>La superficie de un collider (la de su objeto o la de un padre); arena si no tiene.</summary>
        public static SurfaceKind Of(Collider2D ground)
        {
            var surface = ground != null ? ground.GetComponentInParent<GroundSurface>() : null;
            return surface != null ? surface.kind : SurfaceKind.Arena;
        }
    }
}
