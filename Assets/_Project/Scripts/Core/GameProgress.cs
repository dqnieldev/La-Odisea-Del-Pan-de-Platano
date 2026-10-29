using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Avance guardado del jugador (PlayerPrefs): qué lugares del mapa terminó, su mejor puntuación y en cuál estaba
    /// por última vez (para aparecer ahí al volver al mapa). Los lugares se identifican con <see cref="Tutorial"/>,
    /// <see cref="Mercado"/>, <see cref="Horno"/> y <see cref="Tovara"/>.
    /// </summary>
    public static class GameProgress
    {
        public const string Tutorial = "tutorial";
        public const string Mercado = "mercado";
        public const string Horno = "horno";
        public const string Tovara = "tovara";

        private const string Prefix = "Odisea.Progreso.";
        private const string KnownKey = Prefix + "Conocidos";
        private const string LastLocationKey = Prefix + "UltimoLugar";

        /// <summary>Ya terminó el tutorial: "Jugar" lleva directo al mapa.</summary>
        public static bool TutorialDone => IsDone(Tutorial);

        public static bool IsDone(string id) => !string.IsNullOrEmpty(id) && PlayerPrefs.GetInt(DoneKey(id), 0) == 1;

        public static void Complete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;
            Remember(id);
            PlayerPrefs.SetInt(DoneKey(id), 1);
            PlayerPrefs.Save();
        }

        /// <summary>Mejor puntuación del lugar (0 si no hay).</summary>
        public static int BestScore(string id) => PlayerPrefs.GetInt(ScoreKey(id), 0);

        /// <summary>Guarda la puntuación si supera la anterior; true si es récord.</summary>
        public static bool SubmitScore(string id, int score)
        {
            if (string.IsNullOrEmpty(id) || score <= BestScore(id))
                return false;
            Remember(id);
            PlayerPrefs.SetInt(ScoreKey(id), score);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Lugar del mapa del que salió por última vez (vacío: el inicio).</summary>
        public static string LastMapLocation
        {
            get => PlayerPrefs.GetString(LastLocationKey, "");
            set
            {
                PlayerPrefs.SetString(LastLocationKey, value ?? "");
                PlayerPrefs.Save();
            }
        }

        /// <summary>Borra todo el avance (no las opciones de audio y pantalla).</summary>
        public static void ResetAll()
        {
            foreach (string id in Known())
            {
                PlayerPrefs.DeleteKey(DoneKey(id));
                PlayerPrefs.DeleteKey(ScoreKey(id));
            }
            PlayerPrefs.DeleteKey(KnownKey);
            PlayerPrefs.DeleteKey(LastLocationKey);
            PlayerPrefs.Save();
        }

        private static string DoneKey(string id) => $"{Prefix}{id}.Terminado";
        private static string ScoreKey(string id) => $"{Prefix}{id}.Puntos";

        // Lista de lugares con algo guardado, para poder borrarlos sin tocar el resto de PlayerPrefs.
        private static IEnumerable<string> Known() =>
            PlayerPrefs.GetString(KnownKey, "").Split(',').Where(id => id.Length > 0);

        private static void Remember(string id)
        {
            var known = new HashSet<string>(Known());
            if (known.Add(id))
                PlayerPrefs.SetString(KnownKey, string.Join(",", known));
        }
    }
}
