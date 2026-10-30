using System;
using Odisea.Core;
using Odisea.Player;
using Odisea.UI;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Personaje que platica (Don Lupe): al interactuar dice las líneas que tocan según el avance de la partida
    /// (la última conversación cuyo lugar requerido ya se terminó). Voltea a ver a Ulises cuando está cerca.
    /// </summary>
    public class NpcDialogue : Interactable
    {
        [Serializable]
        public class Conversation
        {
            [Tooltip("Lugar terminado que hace falta (GameProgress: tutorial, mercado…); vacío = desde el principio.")]
            public string requiresDone;
            [TextArea(2, 4)] public string[] lines = Array.Empty<string>();
        }

        [SerializeField] private string speakerName = "Don Lupe";
        [Tooltip("En orden: se usa la última cuyo requisito ya se cumplió.")]
        [SerializeField] private Conversation[] conversations = Array.Empty<Conversation>();
        [Tooltip("El dibujo (hijo): se voltea hacia Ulises. Por defecto mira a la derecha.")]
        [SerializeField] private Transform visual;

        private Vector3 visualScale = Vector3.one;

        protected override void Awake()
        {
            base.Awake();
            if (visual != null)
                visualScale = visual.localScale;
        }

        /// <summary>Las líneas que tocan ahora.</summary>
        public string[] CurrentLines
        {
            get
            {
                string[] lines = Array.Empty<string>();
                foreach (Conversation conversation in conversations)
                {
                    if (string.IsNullOrEmpty(conversation.requiresDone) || GameProgress.IsDone(conversation.requiresDone))
                        lines = conversation.lines;
                }
                return lines;
            }
        }

        /// <summary>Dice algo fuera de su conversación (p. ej. cuando una puerta no se puede abrir todavía).</summary>
        public void SayOnce(string[] lines, PlayerInputReader reader) => DialogueBox.Say(speakerName, lines, reader);

        protected override void Interact(PlayerInputReader reader) => DialogueBox.Say(speakerName, CurrentLines, reader);

        protected override void WhileNear(PlayerInputReader reader)
        {
            if (visual == null || reader == null)
                return;
            float side = Mathf.Sign(reader.transform.position.x - transform.position.x);
            visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * side, visualScale.y, visualScale.z);
        }
    }
}
