using Odisea.Core;
using UnityEngine;

namespace Odisea.Map
{
    /// <summary>
    /// En el mapa, Ulises aparece frente al último lugar del que salió (al volver de un nivel); la primera vez, donde
    /// está puesto en la escena.
    /// </summary>
    public class MapArrival : MonoBehaviour
    {
        private void Awake()
        {
            string last = GameProgress.LastMapLocation;
            if (string.IsNullOrEmpty(last))
                return;
            foreach (MapLocation location in FindObjectsByType<MapLocation>(FindObjectsSortMode.None))
            {
                if (location.Id != last)
                    continue;
                Vector2 point = location.ArrivalPoint;
                transform.position = point;
                if (TryGetComponent(out Rigidbody2D body))
                    body.position = point;
                return;
            }
        }
    }
}
