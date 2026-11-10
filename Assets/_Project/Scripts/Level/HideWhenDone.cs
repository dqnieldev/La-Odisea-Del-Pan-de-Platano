using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Quita el objeto de la escena cuando un lugar del mapa ya está hecho (Don Lupe deja la panadería al terminar
    /// La Tovara: ya está en el festival).
    /// </summary>
    public class HideWhenDone : MonoBehaviour
    {
        [SerializeField] private string location;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(location) && GameProgress.IsDone(location))
                gameObject.SetActive(false);
        }
    }
}
