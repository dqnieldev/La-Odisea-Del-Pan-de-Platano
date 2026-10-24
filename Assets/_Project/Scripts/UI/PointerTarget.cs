using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Indica a dónde debe apuntar la manícula cuando este elemento está seleccionado
    /// (por ejemplo, al texto de la opción o a la etiqueta de una casilla). Sin él, apunta al propio elemento.
    /// </summary>
    public class PointerTarget : MonoBehaviour
    {
        [SerializeField] private RectTransform target;

        public RectTransform Target => target != null ? target : (RectTransform)transform;
    }
}
