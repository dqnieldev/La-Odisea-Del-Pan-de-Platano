using System;
using Odisea.Core;
using Odisea.Player;
using Odisea.UI;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Personaje que platica (Don Lupe): al interactuar dice las líneas que tocan según el avance de la partida
    /// (la última conversación cuyo lugar requerido ya se terminó). Voltea a ver a Ulises cuando está cerca y, si
    /// tiene sus cuadros, mueve la boca mientras se escribe su línea (si no, respira en reposo).
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

        [Header("Animación (opcional)")]
        [SerializeField] private SpriteRenderer drawing;
        [SerializeField] private Sprite[] idleFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] private float idleFps = 6f;
        [SerializeField] private Sprite[] talkFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] private float talkFps = 8f;
        [Tooltip("Opcional: suena una vez al empezar a platicar (su risa).")]
        [SerializeField] private SoundEffect talkSound = new();

        private Vector3 visualScale = Vector3.one;
        private bool speaking;
        private float frameTime;
        private bool wasTalking;

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
        public void SayOnce(string[] lines, PlayerInputReader reader)
        {
            speaking = true;
            DialogueBox.Say(speakerName, lines, reader);
        }

        protected override void Interact(PlayerInputReader reader)
        {
            // La risa solo al empezar la plática (en cada línea se enciman).
            if (!speaking)
                talkSound.Play();
            speaking = true;
            DialogueBox.Say(speakerName, CurrentLines, reader);
        }

        // Reposo o hablando (mientras se escribe su línea); al cambiar, empieza desde el primer cuadro.
        private void LateUpdate()
        {
            if (speaking && !DialogueBox.IsOpen)
                speaking = false;
            bool talking = speaking && DialogueBox.IsTyping && talkFrames.Length > 0;
            if (talking != wasTalking)
            {
                wasTalking = talking;
                frameTime = 0f;
            }
            if (drawing == null)
                return;
            frameTime += Time.deltaTime;
            Sprite[] frames = talking ? talkFrames : idleFrames;
            if (frames.Length > 0)
                drawing.sprite = frames[(int)(frameTime * (talking ? talkFps : idleFps)) % frames.Length];
        }

        protected override void WhileNear(PlayerInputReader reader)
        {
            if (visual == null || reader == null)
                return;
            float side = Mathf.Sign(reader.transform.position.x - transform.position.x);
            visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * side, visualScale.y, visualScale.z);
        }
    }
}
