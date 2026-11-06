using System;
using System.Collections;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Cada cierto tiempo cae del cielo un ítem de ayuda al azar (pescado, hoja o café) en un lugar al azar: baja
    /// meciéndose, se queda donde cae (piso o repisa) y, si nadie lo toma, parpadea y desaparece.
    /// </summary>
    public class ItemDropper : MonoBehaviour
    {
        [SerializeField] private GameObject[] items = Array.Empty<GameObject>();
        [Tooltip("Cuánto tarda el primero.")]
        [SerializeField, Min(0f)] private float firstDelay = 12f;
        [Tooltip("Segundos entre uno y otro (al azar entre los dos).")]
        [SerializeField] private Vector2 interval = new(15f, 22f);
        [SerializeField] private float minX = -11f;
        [SerializeField] private float maxX = 11f;
        [Tooltip("Altura de donde caen (arriba de lo que se ve).")]
        [SerializeField] private float spawnY = 12.5f;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField, Min(0.1f)] private float fallSpeed = 2.5f;
        [Tooltip("Lo que dura en el piso antes de desaparecer (los últimos segundos parpadea).")]
        [SerializeField, Min(0f)] private float lifetime = 10f;
        [SerializeField] private Health player;
        [SerializeField] private Oven oven;

        private int dropped;

        private void Start()
        {
            if (items.Length > 0)
                StartCoroutine(Drop());
        }

        private IEnumerator Drop()
        {
            yield return new WaitForSeconds(firstDelay);
            while (true)
            {
                if ((player != null && player.IsDead) || (oven != null && oven.Burned))
                    yield break;
                GameObject prefab = items[UnityEngine.Random.Range(0, items.Length)];
                var position = new Vector3(UnityEngine.Random.Range(minX, maxX), spawnY, 0f);
                GameObject item = Instantiate(prefab, position, Quaternion.identity, transform);
                item.name = $"{prefab.name} (del cielo {++dropped})";
                var fall = item.AddComponent<FallingItem>();
                fall.Setup(groundLayers, fallSpeed, lifetime);
                yield return new WaitForSeconds(UnityEngine.Random.Range(interval.x, interval.y));
            }
        }
    }
}
