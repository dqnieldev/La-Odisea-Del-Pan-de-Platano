using System;
using System.Collections;
using Odisea.Core;
using Odisea.Enemies;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Oleadas del nivel 2: cada entrada hace aparecer enemigos a cierto segundo del horneado, por la izquierda, la
    /// derecha o un lado al azar. Los que caminan (ratas, cocodrilos) salen al ras del piso y los jejenes en el aire. Cuando
    /// el pan está horneado aparece Polifemo y, mientras camina, de vez en cuando llega un ayudante (una rata o jejenes).
    /// Para que no se sienta un metrónomo: el lado es al azar (nunca más de <see cref="maxSameSide"/> seguidos del mismo),
    /// el tiempo entre uno y otro varía, a veces salen dos casi juntos (ráfaga) y, ya avanzado el horneado, a veces uno
    /// por cada lado a la vez (pinza). Las ráfagas y pinzas cuentan dentro de la oleada: salen los mismos enemigos.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Serializable]
        public class Wave
        {
            [Tooltip("Segundo del horneado en que empieza.")]
            public float time;
            public GameObject prefab;
            [Tooltip("-1 izquierda, 1 derecha, 0 al azar.")]
            public int side;
            [Min(1)] public int count = 1;
            [Tooltip("Segundos entre uno y otro (en promedio: varía con intervalVariation).")]
            [Min(0f)] public float interval = 1.5f;
            [Tooltip("A veces salen dos casi juntos del mismo lado.")]
            public bool bursts;
            [Tooltip("Ya avanzado el horneado, a veces sale uno por cada lado a la vez.")]
            public bool pincers;
        }

        [SerializeField] private Oven oven;
        [SerializeField] private BakeTimer bake;
        [SerializeField] private Wave[] waves = Array.Empty<Wave>();
        [Tooltip("Dónde salen los que caminan (x de cada lado y la altura del piso).")]
        [SerializeField] private float groundX = 12f;
        [SerializeField] private float groundY;
        [Tooltip("Alturas posibles de los que vuelan.")]
        [SerializeField] private Vector2 airHeights = new(3f, 5f);

        [Header("Que no sea predecible")]
        [Tooltip("Cuántos pueden salir seguidos del mismo lado (al azar).")]
        [SerializeField, Min(1)] private int maxSameSide = 3;
        [Tooltip("Cuánto varía el tiempo entre uno y otro (0.4 = ±40 %).")]
        [SerializeField, Range(0f, 0.9f)] private float intervalVariation = 0.4f;
        [Tooltip("Probabilidad de que salgan dos casi juntos (en las oleadas con ráfagas).")]
        [SerializeField, Range(0f, 1f)] private float burstChance = 0.25f;
        [SerializeField, Min(0f)] private float burstGap = 0.35f;
        [Tooltip("Probabilidad de una pinza (en las oleadas con pinzas, desde pincerFrom).")]
        [SerializeField, Range(0f, 1f)] private float pincerChance = 0.2f;
        [Tooltip("Segundo del horneado desde el que puede haber pinzas.")]
        [SerializeField, Min(0f)] private float pincerFrom = 50f;

        [Header("Polifemo")]
        [SerializeField] private PolifemoWalker boss;
        [Tooltip("Mientras camina Polifemo, llega uno de estos cada tanto.")]
        [SerializeField] private GameObject[] bossHelpers = Array.Empty<GameObject>();
        [SerializeField, Min(0.5f)] private float helperInterval = 3.5f;
        [Tooltip("Cada cuánto sale un ayudante cuando Polifemo ya está enojado (a media vida).")]
        [SerializeField, Min(0.5f)] private float rageHelperInterval = 2.5f;

        private int spawned;
        private int lastSide;
        private int sameSideStreak;

        private void Awake()
        {
            if (boss != null)
                boss.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (bake != null)
                bake.Baked += OnBaked;
        }

        private void OnDisable()
        {
            if (bake != null)
                bake.Baked -= OnBaked;
        }

        private void Start()
        {
            foreach (Wave wave in waves)
            {
                if (wave.prefab != null)
                    StartCoroutine(Run(wave));
            }
        }

        private IEnumerator Run(Wave wave)
        {
            while (bake.Elapsed < wave.time)
            {
                if (bake.Done)
                    yield break;
                yield return null;
            }
            int left = wave.count;
            while (left > 0 && !bake.Done && !oven.Burned)
            {
                if (wave.pincers && left >= 2 && bake.Elapsed >= pincerFrom && UnityEngine.Random.value < pincerChance)
                {
                    // Pinza: uno por cada lado a la vez.
                    Spawn(wave.prefab, -1);
                    Spawn(wave.prefab, 1);
                    lastSide = 0;
                    sameSideStreak = 0;
                    left -= 2;
                }
                else
                {
                    int side = wave.side != 0 ? wave.side : PickSide();
                    Spawn(wave.prefab, side);
                    left--;
                    if (wave.bursts && left >= 1 && UnityEngine.Random.value < burstChance)
                    {
                        // Ráfaga: otro casi pegado, del mismo lado.
                        yield return new WaitForSeconds(burstGap);
                        if (bake.Done || oven.Burned)
                            yield break;
                        Spawn(wave.prefab, side);
                        left--;
                    }
                }
                yield return new WaitForSeconds(Vary(wave.interval));
            }
        }

        // Un lado al azar, pero nunca más de maxSameSide seguidos del mismo (ni un lado olvidado, ni injusto).
        private int PickSide()
        {
            int side = UnityEngine.Random.value < 0.5f ? -1 : 1;
            if (side == lastSide && sameSideStreak >= maxSameSide)
                side = -side;
            sameSideStreak = side == lastSide ? sameSideStreak + 1 : 1;
            lastSide = side;
            return side;
        }

        private float Vary(float seconds) =>
            seconds * UnityEngine.Random.Range(1f - intervalVariation, 1f + intervalVariation);

        private void OnBaked()
        {
            if (boss == null)
                return;
            // Sale del lado contrario al que está Ulises, para que tenga que cruzar.
            Transform player = FindAnyObjectByType<Odisea.Player.PlayerController>()?.transform;
            float side = player != null && player.position.x > oven.transform.position.x ? -1f : 1f;
            boss.transform.position = new Vector3(oven.transform.position.x + side * groundX, groundY, 0f);
            boss.gameObject.SetActive(true);
            StartCoroutine(Helpers());
        }

        private IEnumerator Helpers()
        {
            while (boss != null && !boss.Defeated && !oven.Burned)
            {
                yield return new WaitForSeconds(Vary(boss.Enraged ? rageHelperInterval : helperInterval));
                if (boss.Defeated || oven.Burned || bossHelpers.Length == 0)
                    yield break;
                Spawn(bossHelpers[UnityEngine.Random.Range(0, bossHelpers.Length)], PickSide());
            }
        }

        private void Spawn(GameObject prefab, int side)
        {
            bool flies = prefab.GetComponent<GnatSwarm>() != null;
            float y = flies ? UnityEngine.Random.Range(airHeights.x, airHeights.y) : groundY;
            var position = new Vector3(oven.transform.position.x + side * groundX, y, 0f);
            GameObject enemy = Instantiate(prefab, position, Quaternion.identity, transform);
            // Nombre único: el puntaje recuerda a los vencidos por nombre y lugar, y estos salen siempre en el mismo.
            enemy.name = $"{prefab.name} {++spawned}-{UnityEngine.Random.Range(0, 100000)}";
            if (enemy.TryGetComponent(out OvenRaider raider))
                raider.Setup(oven);
            if (enemy.TryGetComponent(out GnatSwarm swarm))
                swarm.Setup(oven);
            if (LevelScore.Current != null && enemy.TryGetComponent(out Health health))
                LevelScore.Current.Register(health);
        }
    }
}
