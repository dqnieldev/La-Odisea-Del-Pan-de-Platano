using Odisea.Core;
using Odisea.Player;
using Odisea.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.Map
{
    /// <summary>
    /// Ventanita del mapa que sale al acercarse a un lugar: su nombre, si ya se terminó (y la mejor puntuación) y
    /// "Entrar" (arriba o Enter, como una puerta: A / Espacio es saltar), o por qué todavía no se puede ("Bloqueado",
    /// "Próximamente").
    /// </summary>
    public class LocationPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text status;
        [Tooltip("Ícono de arriba y \"Entrar\"; solo si se puede entrar.")]
        [SerializeField] private GameObject enterPrompt;
        [Tooltip("Los controles de Ulises: arriba es entrar, y se le quitan al entrar.")]
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Cuánto hay que empujar hacia arriba para entrar.")]
        [SerializeField, Range(0.1f, 1f)] private float upThreshold = 0.6f;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.25f;

        private MapLocation current;
        private float shownTime;
        private bool entering, upHeld = true;

        private void Awake()
        {
            group.alpha = 0f;
            card.localScale = Vector3.zero;
        }

        public void Show(MapLocation location)
        {
            if (entering)
                return;
            current = location;
            shownTime = Time.unscaledTime;
            title.text = location.DisplayName;
            subtitle.text = location.Subtitle;

            LocationState state = location.State;
            enterPrompt.SetActive(state == LocationState.Available);
            status.text = state switch
            {
                LocationState.Locked => $"Bloqueado: primero termina {location.RequiresName}",
                LocationState.ComingSoon => "Próximamente",
                _ when location.IsDone && GameProgress.BestScore(location.Id) > 0 =>
                    $"¡Terminado!  Récord: {GameProgress.BestScore(location.Id)}",
                _ when location.IsDone => "¡Terminado!",
                _ => "",
            };
            UiAudio.Play(UiSound.Hover);
        }

        public void Hide(MapLocation location)
        {
            if (location == current && !entering)
                current = null;
        }

        private void Update()
        {
            // Aparece con un rebote y se encoge al alejarse.
            float target = current != null ? 1f : 0f;
            float t = Mathf.Clamp01((Time.unscaledTime - shownTime) / appearDuration);
            float scale = current != null ? 0.8f + 0.2f * t + Mathf.Sin(t * Mathf.PI) * 0.08f : Mathf.MoveTowards(card.localScale.x, 0f, Time.unscaledDeltaTime * 6f);
            card.localScale = new Vector3(scale, scale, 1f);
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime * 8f);

            // "Arriba" cuenta al empujarlo, no mientras se mantiene (si llega con arriba presionado, hay que soltarlo).
            bool up = input != null && input.Move.y >= upThreshold;
            bool upPressed = up && !upHeld;
            upHeld = up;
            if (current == null || entering || Time.timeScale == 0f || SceneFlow.IsLoading)
                return;
            if (current.State == LocationState.Available && (upPressed || EnterPressed()))
                Enter();
        }

        private void Enter()
        {
            entering = true;
            // Se queda quieto mientras se cierra el iris.
            if (input != null)
            {
                input.ScriptedMove = Vector2.zero;
                input.ScriptedJumpHeld = false;
            }
            UiAudio.Play(UiSound.Click);
            Vector2? center = null;
            Camera view = Camera.main;
            if (view != null && input != null)
            {
                Vector3 point = view.WorldToViewportPoint(input.transform.position + Vector3.up);
                center = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
            }
            current.Enter(center);
        }

        private static bool EnterPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) ||
                   (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);
        }
    }
}
