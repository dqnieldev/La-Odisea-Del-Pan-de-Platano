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
        [Tooltip("Canción de fondo mientras se ve; vacío = en silencio.")]
        public AudioClip music;
        [Tooltip("La canción sigue sonando, sin cortarse, en la escena que viene después (p. ej. la del nivel, que trae la misma).")]
        public bool musicContinues;
        [Tooltip("Volumen de la canción (0 a 1), para las que vienen más fuertes que las demás.")]
        [Range(0f, 1f)] public float musicVolume = 1f;
        public Panel[] panels = Array.Empty<Panel>();
    }
}
