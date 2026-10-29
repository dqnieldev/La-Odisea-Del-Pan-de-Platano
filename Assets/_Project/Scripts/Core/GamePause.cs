using System;

namespace Odisea.Core
{
    /// <summary>
    /// Avisa cuando se abre o se cierra la pausa (el recetario en el mapa o en un nivel). La música lo usa para
    /// bajar y sonar como un disco viejo mientras está en pausa.
    /// </summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        public static event Action<bool> Changed;

        public static void Set(bool paused)
        {
            if (IsPaused == paused)
                return;
            IsPaused = paused;
            Changed?.Invoke(paused);
        }
    }
}
