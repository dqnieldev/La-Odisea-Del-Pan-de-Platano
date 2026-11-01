using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Avance guardado del jugador (PlayerPrefs) en <see cref="SlotCount"/> partidas: qué lugares del mapa terminó,
    /// su mejor puntuación y en cuál estaba por última vez (para aparecer ahí al volver al mapa). Todo se lee y se
    /// escribe en la partida elegida (<see cref="Slot"/>). Los lugares se identifican con <see cref="Tutorial"/>,
    /// <see cref="Mercado"/>, <see cref="Horno"/> y <see cref="Tovara"/>.
    /// </summary>
    public static class GameProgress
    {
        public const string Tutorial = "tutorial";
        public const string Mercado = "mercado";
        public const string Horno = "horno";
        public const string Tovara = "tovara";
        /// <summary>El final (el festival): está en el mapa pero no cuenta entre los lugares de la partida.</summary>
        public const string Festival = "festival";
        /// <summary>Lugares del mapa, en orden de la historia.</summary>
        public static readonly string[] Locations = { Tutorial, Mercado, Horno, Tovara };

        public const int SlotCount = 3;

        private const string Prefix = "Odisea.Progreso.";
        private const string SlotKey = Prefix + "Partida";

        /// <summary>Partida en uso (1 a <see cref="SlotCount"/>); se recuerda entre sesiones.</summary>
        public static int Slot
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(SlotKey, 1), 1, SlotCount);
            set
            {
                PlayerPrefs.SetInt(SlotKey, Mathf.Clamp(value, 1, SlotCount));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Ya terminó el tutorial: "Jugar" lleva directo al mapa.</summary>
        public static bool TutorialDone => IsDone(Tutorial);

        public static bool IsDone(string id) => IsDone(Slot, id);

        public static bool IsDone(int slot, string id) =>
            !string.IsNullOrEmpty(id) && PlayerPrefs.GetInt(Key(slot, id, "Terminado"), 0) == 1;

        public static void Complete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;
            Remember(Slot, id);
            PlayerPrefs.SetInt(Key(Slot, id, "Terminado"), 1);
            PlayerPrefs.Save();
        }

        /// <summary>Mejor puntuación del lugar en la partida actual (0 si no hay).</summary>
        public static int BestScore(string id) => PlayerPrefs.GetInt(Key(Slot, id, "Puntos"), 0);

        /// <summary>Guarda la puntuación si supera la anterior; true si es récord.</summary>
        public static bool SubmitScore(string id, int score)
        {
            if (string.IsNullOrEmpty(id) || score <= BestScore(id))
                return false;
            Remember(Slot, id);
            PlayerPrefs.SetInt(Key(Slot, id, "Puntos"), score);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Lugar del mapa del que salió por última vez en la partida actual (vacío: el inicio).</summary>
        public static string LastMapLocation
        {
            get => PlayerPrefs.GetString(Key(Slot, "", "UltimoLugar"), "");
            set
            {
                PlayerPrefs.SetString(Key(Slot, "", "UltimoLugar"), value ?? "");
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Cuántos lugares abiertos ya vio dibujarse en el mapa la partida actual: los que pasen de ahí se
        /// estrenan con su camino dibujándose.
        /// </summary>
        public static int MapRevealed
        {
            get => PlayerPrefs.GetInt(Key(Slot, "", "MapaRevelado"), 0);
            set
            {
                PlayerPrefs.SetInt(Key(Slot, "", "MapaRevelado"), value);
                PlayerPrefs.Save();
            }
        }

        // ---------- Partidas ----------

        /// <summary>La partida ya se empezó (aunque no haya terminado nada).</summary>
        public static bool IsStarted(int slot) => PlayerPrefs.GetInt(Key(slot, "", "Empezada"), 0) == 1;

        /// <summary>Elige la partida y la marca como empezada.</summary>
        public static void Begin(int slot)
        {
            Slot = slot;
            PlayerPrefs.SetInt(Key(slot, "", "Empezada"), 1);
            PlayerPrefs.Save();
        }

        /// <summary>Cuántos lugares del mapa terminó la partida.</summary>
        public static int CompletedCount(int slot) => Locations.Count(id => IsDone(slot, id));

        /// <summary>Borra una partida (queda vacía).</summary>
        public static void ResetSlot(int slot)
        {
            foreach (string id in Known(slot))
            {
                PlayerPrefs.DeleteKey(Key(slot, id, "Terminado"));
                PlayerPrefs.DeleteKey(Key(slot, id, "Puntos"));
            }
            PlayerPrefs.DeleteKey(Key(slot, "", "Conocidos"));
            PlayerPrefs.DeleteKey(Key(slot, "", "UltimoLugar"));
            PlayerPrefs.DeleteKey(Key(slot, "", "MapaRevelado"));
            PlayerPrefs.DeleteKey(Key(slot, "", "Empezada"));
            PlayerPrefs.Save();
        }

        /// <summary>Borra las tres partidas (no las opciones de audio y pantalla).</summary>
        public static void ResetAll()
        {
            for (int slot = 1; slot <= SlotCount; slot++)
                ResetSlot(slot);
        }

        private static string Key(int slot, string id, string field) =>
            string.IsNullOrEmpty(id) ? $"{Prefix}P{slot}.{field}" : $"{Prefix}P{slot}.{id}.{field}";

        // Lista de lugares con algo guardado, para poder borrarlos sin tocar el resto de PlayerPrefs.
        private static IEnumerable<string> Known(int slot) =>
            PlayerPrefs.GetString(Key(slot, "", "Conocidos"), "").Split(',').Where(id => id.Length > 0);

        private static void Remember(int slot, string id)
        {
            var known = new HashSet<string>(Known(slot));
            if (known.Add(id))
                PlayerPrefs.SetString(Key(slot, "", "Conocidos"), string.Join(",", known));
        }
    }
}
