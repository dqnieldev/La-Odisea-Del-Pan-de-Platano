using Odisea.Player;
using Odisea.UI;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>El tablerito del festival: al interactuar abre el Tablero de Honor en grande (<see cref="ScoreboardModal"/>).</summary>
    public class ScoreboardTotem : Interactable
    {
        [SerializeField] private ScoreboardModal modal;

        protected override void Interact(PlayerInputReader reader)
        {
            if (modal != null)
                modal.Show(reader);
        }
    }
}
