using Odisea.Core;
using Odisea.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.Map
{
    /// <summary>
    /// Ventanita del mapa que sale al acercarse a un lugar: su nombre, si ya se terminó (y la mejor puntuación) y
    /// "Entrar" (Aceptar), o por qué todavía no se puede ("Bloqueado", "Próximamente").
    /// </summary>
    public class LocationPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text status;
        [Tooltip("Ícono del botón Aceptar y \"Entrar\"; solo si se puede entrar.")]
        [SerializeField] private GameObject enterPrompt;
        [SerializeField] private MapWalker walker;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.25f;

        private MapLocation current;
        private float shownTime;
        private bool entering;

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

            if (current == null || entering || Time.timeScale == 0f || SceneFlow.IsLoading)
                return;
            if (current.State == LocationState.Available && EnterPressed())
                Enter();
        }

        private void Enter()
        {
            entering = true;
            if (walker != null)
                walker.Frozen = true;
            UiAudio.Play(UiSound.Click);
            Vector2? center = null;
            Camera view = Camera.main;
            if (view != null && walker != null)
            {
                Vector3 point = view.WorldToViewportPoint(walker.transform.position);
                center = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
            }
            current.Enter(center);
        }

        private static bool EnterPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                                         keyboard.spaceKey.wasPressedThisFrame)) ||
                   (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        }
    }
}
