using TMPro;
using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Texto del pie del menú con el nombre del juego y la versión (la de Project Settings → Version).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class VersionLabel : MonoBehaviour
    {
        // Escrito aquí y no con Application.productName, que no lleva acentos.
        private const string GameName = "La Odisea del Pan de Plátano";

        private void Awake() => GetComponent<TMP_Text>().text = $"{GameName}  ·  v{Application.version}";
    }
}
