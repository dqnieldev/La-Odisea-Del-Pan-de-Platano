using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>Marca un lugar del mapa como hecho al abrir la escena (el Festival: con solo llegar ya cuenta).</summary>
    public class CompleteOnStart : MonoBehaviour
    {
        [SerializeField] private string location;

        private void Start()
        {
            if (!string.IsNullOrEmpty(location) && !GameProgress.IsDone(location))
                GameProgress.Complete(location);
        }
    }
}
