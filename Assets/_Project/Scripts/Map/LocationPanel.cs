using Odisea.Core;
using Odisea.UI;
using TMPro;
using UnityEngine;

namespace Odisea.Map
{
    /// <summary>
    /// Ventanita del mapa con el lugar donde está Ulises: su nombre, si ya se terminó (y la mejor puntuación) y
    /// "Entrar", o por qué todavía no se puede ("Próximamente"). Al dar clic a un lugar bloqueado avisa un momento
    /// qué hay que terminar antes, y al pasar el mouse sobre otro lugar lo muestra mientras siga encima.
    /// Solo dibuja: los controles los lleva <see cref="OverworldMap"/>.
    /// </summary>
    public class LocationPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text status;
        [Tooltip("Ícono de Aceptar y \"Entrar\"; solo si se puede entrar.")]
        [SerializeField] private GameObject enterPrompt;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.25f;
        [Tooltip("Cuánto se ve el aviso de un lugar bloqueado antes de volver al lugar actual.")]
        [SerializeField, Min(0.1f)] private float lockedNoticeTime = 2f;

        private MapLocation shown, home;
        private TMP_Text enterLabel;
        private string enterText;
        private float shownTime, noticeUntil = -1f;

        private void Awake()
        {
            group.alpha = 0f;
            card.localScale = Vector3.zero;
            enterLabel = enterPrompt.GetComponentInChildren<TMP_Text>(true);
            if (enterLabel != null)
                enterText = enterLabel.text;
        }

        /// <summary>
        /// Muestra el lugar elegido con el mando o las flechas (Ulises está en otro): "Ir" en lugar de "Entrar", porque
        /// Aceptar lo lleva caminando hasta allá. Se queda hasta que se elija otro o llegue.
        /// </summary>
        public void PreviewDestination(MapLocation location)
        {
            home = location;
            noticeUntil = -1f;
            Display(location);
            if (enterLabel != null)
                enterLabel.text = "Ir";
        }

        /// <summary>Muestra el lugar donde quedó Ulises.</summary>
        public void Show(MapLocation location)
        {
            home = location;
            noticeUntil = -1f;
            Display(location);
        }

        /// <summary>
        /// Muestra otro lugar sin dejar el de Ulises (el mouse encima, o un lugar que se estrena). Con status, ese texto
        /// en lugar del estado. <see cref="EndPreview"/> regresa al de Ulises.
        /// </summary>
        public void Preview(MapLocation location, string status = null)
        {
            noticeUntil = -1f;
            if (shown != location)
                Display(location);
            if (status != null)
            {
                this.status.text = status;
                enterPrompt.SetActive(false);
            }
        }

        public void EndPreview()
        {
            if (noticeUntil > 0f)
                return;
            if (home != null)
            {
                if (shown != home)
                    Display(home);
            }
            else
            {
                shown = null;
            }
        }

        /// <summary>Oculta la ventanita (mientras Ulises camina).</summary>
        public void Hide()
        {
            home = shown = null;
            noticeUntil = -1f;
        }

        /// <summary>Aviso de un momento sobre un lugar bloqueado; luego vuelve al lugar actual.</summary>
        public void ShowLocked(MapLocation location)
        {
            Display(location);
            noticeUntil = Time.unscaledTime + lockedNoticeTime;
            UiAudio.Play(UiSound.Back);
        }

        private void Display(MapLocation location)
        {
            shown = location;
            shownTime = Time.unscaledTime;
            title.text = location.DisplayName;
            subtitle.text = location.Subtitle;
            LocationState state = location.State;
            enterPrompt.SetActive(state == LocationState.Available);
            if (enterLabel != null)
                enterLabel.text = enterText;
            status.text = state switch
            {
                LocationState.Locked => $"Bloqueado: primero termina {location.RequiresName}",
                LocationState.ComingSoon => "Próximamente",
                _ when location.IsDone && GameProgress.BestScore(location.Id) > 0 =>
                    $"¡Terminado!  Récord: {GameProgress.BestScore(location.Id)}{RankText(location.Id)}",
                _ when location.IsDone => "¡Terminado!",
                _ => "",
            };
        }

        // " · Rango A" (vacío si aún no tiene rango). Los rangos van de C (0) a S (3), como Level.ScoreRank.
        private static string RankText(string id)
        {
            int rank = GameProgress.BestRank(id);
            return rank < 0 ? "" : $" · Rango {"CBAS"[Mathf.Min(rank, 3)]}";
        }

        private void Update()
        {
            if (noticeUntil > 0f && Time.unscaledTime >= noticeUntil)
            {
                noticeUntil = -1f;
                if (home != null)
                    Display(home);
                else
                    shown = null;
            }

            // Aparece con un rebote y se encoge al ocultarse.
            bool visible = shown != null;
            float t = Mathf.Clamp01((Time.unscaledTime - shownTime) / appearDuration);
            float scale = visible ? 0.8f + 0.2f * t + Mathf.Sin(t * Mathf.PI) * 0.08f
                : Mathf.MoveTowards(card.localScale.x, 0f, Time.unscaledDeltaTime * 6f);
            card.localScale = new Vector3(scale, scale, 1f);
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        }
    }
}
