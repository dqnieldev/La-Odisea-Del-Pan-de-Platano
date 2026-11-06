using System;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// El tiempo de horneado del nivel 2: avanza mientras Ulises está vivo y, al llegar a su duración, el pan está
    /// listo (<see cref="Baked"/>): ahí sale Polifemo.
    /// </summary>
    public class BakeTimer : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float duration = 100f;
        [SerializeField] private Health player;

        public float Elapsed { get; private set; }
        public float Duration => duration;
        /// <summary>0 a 1.</summary>
        public float Progress => Mathf.Clamp01(Elapsed / duration);
        public bool Done { get; private set; }
        public event Action Baked;

        private void Update()
        {
            if (Done || (player != null && player.IsDead))
                return;
            Elapsed += Time.deltaTime;
            if (Elapsed < duration)
                return;
            Elapsed = duration;
            Done = true;
            Baked?.Invoke();
        }
    }
}
