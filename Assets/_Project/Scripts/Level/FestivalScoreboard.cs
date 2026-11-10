using Odisea.Core;
using TMPro;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// El Tablero de Honor del festival: el mejor puntaje y rango de cada nivel de la partida (lo que guarda
    /// <see cref="GameProgress"/>) y el total. Los niveles sin jugar salen con guiones.
    /// </summary>
    public class FestivalScoreboard : MonoBehaviour
    {
        [SerializeField] private TMP_Text names;
        [SerializeField] private TMP_Text points;
        [SerializeField] private TMP_Text ranks;
        [SerializeField] private TMP_Text total;

        private static readonly (string id, string name)[] Levels =
        {
            (GameProgress.Mercado, "El Mercado"),
            (GameProgress.Horno, "El Horno"),
            (GameProgress.Tovara, "La Tovara"),
        };

        private void Start()
        {
            string nameText = "", pointText = "", rankText = "";
            int sum = 0;
            foreach ((string id, string name) in Levels)
            {
                int best = GameProgress.BestScore(id);
                int rank = GameProgress.BestRank(id);
                sum += best;
                nameText += name + "\n";
                pointText += (best > 0 ? best.ToString("N0") : "—") + "\n";
                rankText += (rank >= 0 ? ((ScoreRank)rank).ToString() : "—") + "\n";
            }
            if (names != null)
                names.text = nameText.TrimEnd();
            if (points != null)
                points.text = pointText.TrimEnd();
            if (ranks != null)
                ranks.text = rankText.TrimEnd();
            if (total != null)
                total.text = $"Total: {sum:N0}";
        }
    }
}
