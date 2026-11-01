using TMPro;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Ajustes de la transición entre escenas (iris). El asset vive en Resources/Transitions
    /// para que ScreenFader lo cargue solo; los cambios se notan en la siguiente transición, incluso en Play.
    /// </summary>
    [CreateAssetMenu(fileName = "TransitionSettings", menuName = "Odisea/Configuración de transición")]
    public class TransitionSettings : ScriptableObject
    {
        /// <summary>Nombre para Resources.Load (sin extensión).</summary>
        public const string ResourcePath = "Transitions/TransitionSettings";

        [Header("Duración (segundos)")]
        [Tooltip("Lo que tarda el círculo en cerrarse.")]
        [Min(0.01f)] public float closeDuration = 0.7f;
        [Tooltip("Pausa con la pantalla cubierta antes de abrir.")]
        [Min(0f)] public float holdDuration = 0.18f;
        [Tooltip("Lo que tarda el círculo en abrirse en la escena nueva.")]
        [Min(0.01f)] public float openDuration = 0.7f;

        [Header("Aspecto")]
        [Tooltip("Color que cubre la pantalla. Por defecto, Café Panadería.")]
        public Color color = new(0.169f, 0.118f, 0.086f, 1f);
        [Tooltip("Qué tan difuminado es el borde del círculo: 0.004 casi nítido, 0.05 muy suave.")]
        [Range(0f, 0.1f)] public float edgeSoftness = 0.004f;

        [Header("Signo de carga (abajo a la derecha, con la pantalla cubierta)")]
        [Tooltip("Cuadros de Ulises caminando. Sin cuadros no se muestra el signo.")]
        public Sprite[] loadingFrames = new Sprite[0];
        [Min(0.1f)] public float loadingFps = 9f;
        [Tooltip("Alto del dibujo, en píxeles de una pantalla de 1080.")]
        [Min(1f)] public float loadingHeight = 160f;
        [Tooltip("Letra del \"Cargando…\"; sin letra solo se ve Ulises.")]
        public TMP_FontAsset loadingFont;
        public Color loadingTextColor = new(0.96f, 0.91f, 0.80f, 1f);
        [Tooltip("Lo menos que se ve el signo, para que no parpadee en las cargas rápidas.")]
        [Min(0f)] public float minLoadingTime = 0.6f;

        [Header("Aceleración")]
        [Tooltip("Avance del cierre en el tiempo (0 = abierto, 1 = cerrado). Por defecto empieza lento y acelera.")]
        public AnimationCurve closeCurve = new(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 3f, 3f));
        [Tooltip("Avance de la apertura en el tiempo (0 = cerrado, 1 = abierto). Por defecto arranca rápido y frena.")]
        public AnimationCurve openCurve = new(new Keyframe(0f, 0f, 3f, 3f), new Keyframe(1f, 1f, 0f, 0f));
    }
}
