using Odisea.Core;
using Odisea.Player;
using Odisea.UI;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Puerta a otra escena (la playa del tutorial, la salida al mapa). Si falta terminar algo, quien la cuida
    /// (Don Lupe) lo dice y no se abre. Al abrirse, el iris se cierra sobre la puerta.
    /// </summary>
    public class SceneDoor : Interactable
    {
        [Tooltip("Escena a la que lleva (nombre en Build Settings).")]
        [SerializeField] private string targetScene;
        [Tooltip("Lugar terminado que hace falta para abrirla (GameProgress); vacío = siempre abierta.")]
        [SerializeField] private string requiresDone;
        [Tooltip("Quién avisa que todavía no se puede salir, y qué dice.")]
        [SerializeField] private NpcDialogue keeper;
        [SerializeField, TextArea(2, 4)] private string[] lockedLines = { "Todavía no." };
        [Tooltip("Al ir al mapa: lugar donde aparece la ficha de Ulises (GameProgress); vacío = no cambia.")]
        [SerializeField] private string mapLocation;
        [SerializeField] private SoundEffect openSound = new();

        public bool IsLocked => !string.IsNullOrEmpty(requiresDone) && !GameProgress.IsDone(requiresDone);

        protected override void Interact(PlayerInputReader reader)
        {
            if (IsLocked)
            {
                if (keeper != null)
                    keeper.SayOnce(lockedLines, reader);
                else
                    DialogueBox.Say("", lockedLines, reader);
                return;
            }

            openSound.Play();
            reader.InputLocked = true;
            if (!string.IsNullOrEmpty(mapLocation))
                GameProgress.LastMapLocation = mapLocation;
            Vector2? center = null;
            Camera view = Camera.main;
            if (view != null)
            {
                Vector3 point = view.WorldToViewportPoint(transform.position + Vector3.up * 1.5f);
                center = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
            }
            SceneFlow.Load(targetScene, center);
        }
    }
}
