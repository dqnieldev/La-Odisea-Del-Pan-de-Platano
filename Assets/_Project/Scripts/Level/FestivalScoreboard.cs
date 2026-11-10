using Odisea.Core;
using TMPro;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// El Tablero de Honor del festival: el mejor puntaje y rango de cada nivel de la partida (lo que guarda
    /// <see cref="GameProgress"/>) y el total. Los niveles sin jugar salen con guiones. Con el tablero dibujado, cada
    /// nivel va en su propia tira (rowNames, rowPoints, rowRanks); si no, en tres listas.
    /// </summary>
    public class FestivalScoreboard : MonoBehaviour
    {
        [SerializeField] private TMP_Text names;
        [SerializeField] private TMP_Text points;
        [SerializeField] private TMP_Text ranks;
        [SerializeField] private TMP_Text total;
        [Tooltip("Cómo se escribe el total ({0} es el número).")]
        [SerializeField] private string totalFormat = "Total: {0}";
        [Header("Una tira por nivel (el tablero dibujado)")]
        [SerializeField] private TMP_Text[] rowNames = new TMP_Text[0];
        [SerializeField] private TMP_Text[] rowPoints = new TMP_Text[0];
        [SerializeField] private TMP_Text[] rowRanks = new TMP_Text[0];

        private static readonly (string id, string name)[] Levels =
        {
            (GameProgress.Mercado, "El Mercado"),
            (GameProgress.Horno, "El Horno"),
            (GameProgress.Tovara, "La Tovara"),
        };

        private void Start() => Refresh();

        /// <summary>Pone los puntajes de la partida (al empezar y cada vez que se abre el tablero en grande).</summary>
        public void Refresh()
        {
            string nameText = "", pointText = "", rankText = "";
            int sum = 0;
            for (int i = 0; i < Levels.Length; i++)
            {
                (string id, string name) = Levels[i];
                int best = GameProgress.BestScore(id);
                int rank = GameProgress.BestRank(id);
                sum += best;
                string point = best > 0 ? best.ToString("N0") : "—";
                string rankName = rank >= 0 ? ((ScoreRank)rank).ToString() : "—";
                nameText += name + "\n";
                pointText += point + "\n";
                rankText += rankName + "\n";
                SetRow(rowNames, i, name);
                SetRow(rowPoints, i, point);
                SetRow(rowRanks, i, rankName);
            }
            if (names != null)
                names.text = nameText.TrimEnd();
            if (points != null)
                points.text = pointText.TrimEnd();
            if (ranks != null)
                ranks.text = rankText.TrimEnd();
            if (total != null)
                total.text = string.Format(totalFormat, sum.ToString("N0"));
        }

        private static void SetRow(TMP_Text[] row, int index, string text)
        {
            if (index < row.Length && row[index] != null)
                row[index].text = text;
        }
    }
}
