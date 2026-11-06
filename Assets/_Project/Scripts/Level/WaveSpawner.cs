using System;
using System.Collections;
using Odisea.Core;
using Odisea.Enemies;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Oleadas del nivel 2: cada entrada hace aparecer enemigos a cierto segundo del horneado, por la izquierda, la
    /// derecha o alternando. Los que caminan (ratas, cocodrilos) salen al ras del piso y los jejenes en el aire. Cuando el
    /// pan está horneado aparece Polifemo y, mientras camina, de vez en cuando llega un ayudante (una rata o jejenes).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Serializable]
        public class Wave
        {
            [Tooltip("Segundo del horneado en que empieza.")]
            public float time;
            public GameObject prefab;
            [Tooltip("-1 izquierda, 1 derecha, 0 alternando.")]
            public int side;
            [Min(1)] public int count = 1;
            [Tooltip("Segundos entre uno y otro.")]
            [Min(0f)] public float interval = 1.5f;
        }

        [SerializeField] private Oven oven;
        [SerializeField] private BakeTimer bake;
        [SerializeField] private Wave[] waves = Array.Empty<Wave>();
        [Tooltip("Dónde salen los que caminan (x de cada lado y la altura del piso).")]
        [SerializeField] private float groundX = 12f;
        [SerializeField] private float groundY;
        [Tooltip("Alturas posibles de los que vuelan.")]
        [SerializeField] private Vector2 airHeights = new(3f, 5f);

        [Header("Polifemo")]
        [SerializeField] private PolifemoWalker boss;
        [Tooltip("Mientras camina Polifemo, llega uno de estos cada tanto.")]
        [SerializeField] private GameObject[] bossHelpers = Array.Empty<GameObject>();
        [SerializeField, Min(0.5f)] private float helperInterval = 6f;

        private int spawned;
        private int alternate = 1;

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
            for (int i = 0; i < wave.count && !bake.Done && !oven.Burned; i++)
            {
                int side = wave.side != 0 ? wave.side : (alternate = -alternate);
                Spawn(wave.prefab, side);
                yield return new WaitForSeconds(wave.interval);
            }
        }

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
                yield return new WaitForSeconds(helperInterval);
                if (boss.Defeated || oven.Burned || bossHelpers.Length == 0)
                    yield break;
                Spawn(bossHelpers[UnityEngine.Random.Range(0, bossHelpers.Length)], alternate = -alternate);
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
