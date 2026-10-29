using System;
using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Una historieta: título y sus viñetas, que <see cref="ComicViewer"/> muestra una por una. El texto de cada viñeta
    /// va en el juego y no en el dibujo, para poder corregirlo sin redibujar. Se busca por el nombre del asset
    /// (SceneFlow.PlayStory("Prologo", ...)).
    /// </summary>
    [CreateAssetMenu(menuName = "Odisea/Historieta", fileName = "Historieta")]
    public class ComicSequence : ScriptableObject
    {
        [Serializable]
        public class Panel
        {
            [Tooltip("Título arriba de la viñeta; vacío usa el de la historieta.")]
            public string title;
            [Tooltip("Dibujo de la viñeta (horizontal, 16:9). Sin dibujo se ve un recuadro con la descripción.")]
            public Sprite art;
            [Tooltip("Texto que se lee debajo del dibujo.")]
            [TextArea(2, 4)] public string caption;
            [Tooltip("Qué debe dibujarse (se ve mientras no hay dibujo).")]
            [TextArea(2, 4)] public string placeholderNote;
        }

        [Tooltip("Título de las viñetas que no tienen uno propio, p. ej. \"El Decreto del Viejo Horno\".")]
        public string title;
        public Panel[] panels = Array.Empty<Panel>();
    }
}
