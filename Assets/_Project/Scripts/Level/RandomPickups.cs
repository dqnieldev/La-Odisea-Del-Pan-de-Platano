using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Al cargar el nivel deja solo <see cref="keep"/> de los ítems de la lista, elegidos al azar, y quita los demás:
    /// cada vez que se juega (o se reintenta), los ingredientes salen en otros lugares. Va antes que el puntaje
    /// (<see cref="LevelScore"/>), que cuenta los ingredientes en su Start.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class RandomPickups : MonoBehaviour
    {
        [Tooltip("Lugares posibles: cada uno con su ítem ya puesto.")]
        [SerializeField] private Pickup[] candidates;
        [SerializeField, Min(0)] private int keep = 8;

        private void Awake()
        {
            // Mezcla (Fisher-Yates) y se quedan los primeros.
            var shuffled = (Pickup[])candidates.Clone();
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            // Inmediato y no Destroy: el puntaje los cuenta en este mismo cuadro.
            for (int i = keep; i < shuffled.Length; i++)
            {
                if (shuffled[i] != null)
                    DestroyImmediate(shuffled[i].gameObject);
            }
        }
    }
}
