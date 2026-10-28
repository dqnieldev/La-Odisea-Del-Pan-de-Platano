using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Efecto del café de olla: mientras corre deja siluetas color sepia que se desvanecen detrás de él y
    /// polvo en los pies; corriendo o parado, le sale vapor de la cabeza, como al jarrito.
    /// </summary>
    public class SpeedTrail : MonoBehaviour
    {
        [SerializeField] private PlayerBuffs buffs;
        [SerializeField] private PlayerController controller;
        [Tooltip("El dibujo de Ulises: las siluetas copian su cuadro actual.")]
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Círculo suave para el polvo y el vapor.")]
        [SerializeField] private Sprite puff;
        [SerializeField] private int playerSortingOrder = 10;

        [Header("Siluetas")]
        [SerializeField, Min(0.01f)] private float ghostInterval = 0.06f;
        [SerializeField, Min(0.01f)] private float ghostLifetime = 0.25f;
        [SerializeField] private Color ghostColor = new(0.85f, 0.6f, 0.3f, 0.45f);

        [Header("Polvo y vapor")]
        [SerializeField, Min(0.01f)] private float dustInterval = 0.12f;
        [SerializeField] private Color dustColor = new(0.85f, 0.75f, 0.6f, 0.6f);
        [SerializeField, Min(0.01f)] private float steamInterval = 0.28f;
        [SerializeField] private Color steamColor = new(1f, 1f, 1f, 0.6f);
        [Tooltip("De dónde sale el vapor, respecto al centro de Ulises (la x se voltea con él).")]
        [SerializeField] private Vector2 headOffset = new(0.1f, 0.95f);
        [Tooltip("Dónde están los pies, respecto al centro de Ulises.")]
        [SerializeField] private float feetHeight = -0.75f;

        private float ghostTimer, dustTimer, steamTimer;

        private void Update()
        {
            if (buffs == null || controller == null || !buffs.SpeedShown)
                return;
            bool running = Mathf.Abs(controller.Velocity.x) > 0.5f;
            if (running && Every(ref ghostTimer, ghostInterval))
                Ghost();
            if (running && controller.IsGrounded && Every(ref dustTimer, dustInterval))
                Dust();
            if (Every(ref steamTimer, steamInterval))
                Steam();
        }

        private static bool Every(ref float timer, float interval)
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
                return false;
            timer += interval;
            if (timer < 0f)
                timer = interval;
            return true;
        }

        // Copia del cuadro actual, en el mismo lugar; como Ulises sigue corriendo, se queda atrás.
        private void Ghost()
        {
            if (body == null || body.sprite == null)
                return;
            FadingSprite ghost = FadingSprite.Spawn(body.sprite, body.transform.position, ghostColor, playerSortingOrder - 1, ghostLifetime);
            ghost.GetComponent<SpriteRenderer>().flipX = body.flipX;
            ghost.transform.localScale = body.transform.lossyScale;
        }

        // Nubecita que se levanta detrás de los pies y se abre.
        private void Dust()
        {
            if (puff == null)
                return;
            float back = -Mathf.Sign(controller.Velocity.x);
            Vector3 feet = transform.position + new Vector3(back * 0.3f, feetHeight, 0f);
            FadingSprite dust = FadingSprite.Spawn(puff, feet, dustColor, playerSortingOrder + 1, 0.35f)
                .Move(new Vector2(back * Random.Range(0.5f, 1.2f), Random.Range(0.3f, 0.7f)))
                .Grow(1.2f);
            dust.transform.localScale = Vector3.one * Random.Range(0.25f, 0.35f);
        }

        // Rizo de vapor que sube de la cabeza meciéndose.
        private void Steam()
        {
            if (puff == null)
                return;
            int facing = controller.FacingDirection;
            Vector3 head = transform.position + new Vector3(headOffset.x * facing + Random.Range(-0.15f, 0.15f), headOffset.y, 0f);
            FadingSprite steam = FadingSprite.Spawn(puff, head, steamColor, playerSortingOrder + 1, 0.7f)
                .Move(new Vector2(-facing * 0.2f, 0.9f))
                .Grow(1.5f, sideWobble: 0.15f);
            steam.transform.localScale = Vector3.one * Random.Range(0.12f, 0.18f);
        }
    }
}
