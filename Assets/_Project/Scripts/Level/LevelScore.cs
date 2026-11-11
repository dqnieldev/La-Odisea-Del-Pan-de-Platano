using System;
using System.Collections.Generic;
using Odisea.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Odisea.Level
{
    /// <summary>Rango del nivel, de peor a mejor.</summary>
    public enum ScoreRank
    {
        C,
        B,
        A,
        S,
    }

    /// <summary>Desglose del puntaje al llegar a la meta (lo muestra la tarjeta de resultados).</summary>
    public readonly struct ScoreResult
    {
        public readonly int IngredientsFound, IngredientsTotal, IngredientPoints;
        public readonly float Seconds;
        public readonly int TimePoints;
        public readonly int EnemiesDefeated, EnemyPoints;
        public readonly bool NoDamage;
        public readonly int NoDamagePoints;
        /// <summary>Nivel del horno: las vidas que le quedaron al horno (OvenMax 0 = el nivel no tiene horno).</summary>
        public readonly int OvenLife, OvenMax, OvenPoints;
        public readonly int Total;
        public readonly ScoreRank Rank;
        public readonly bool NewRecord;
        /// <summary>El tiempo cuenta para el puntaje (en el nivel del horno no: el horneado siempre dura lo mismo).</summary>
        public readonly bool TimeScored;

        public ScoreResult(int ingredientsFound, int ingredientsTotal, int ingredientPoints, float seconds, int timePoints,
            int enemiesDefeated, int enemyPoints, bool noDamage, int noDamagePoints, ScoreRank rank, bool newRecord,
            int ovenLife = 0, int ovenMax = 0, int ovenPoints = 0, bool timeScored = true)
        {
            TimeScored = timeScored;
            IngredientsFound = ingredientsFound;
            IngredientsTotal = ingredientsTotal;
            IngredientPoints = ingredientPoints;
            Seconds = seconds;
            TimePoints = timePoints;
            EnemiesDefeated = enemiesDefeated;
            EnemyPoints = enemyPoints;
            NoDamage = noDamage;
            NoDamagePoints = noDamagePoints;
            OvenLife = ovenLife;
            OvenMax = ovenMax;
            OvenPoints = ovenPoints;
            Total = ingredientPoints + timePoints + enemyPoints + noDamagePoints + ovenPoints;
            Rank = rank;
            NewRecord = newRecord;
        }
    }

    /// <summary>
    /// Puntaje del nivel: ingredientes recogidos (o, en el nivel del horno, las vidas que le quedaron al horno), tiempo,
    /// enemigos derrotados y si Ulises no recibió daño. Al llegar a
    /// la meta (<see cref="LevelGoal"/>) se calcula el total y el rango (S, A, B o C según qué tanto del máximo posible
    /// logró) y se guarda el récord de la partida.
    /// Al reintentar desde un punto de control la carrera sigue: el tiempo se acumula, los ingredientes recogidos y
    /// los enemigos derrotados no vuelven a aparecer, y ya no hay bono sin daño. Reiniciar el nivel empieza de cero.
    /// </summary>
    public class LevelScore : MonoBehaviour
    {
        [SerializeField] private Health player;

        [Header("Puntos")]
        [SerializeField, Min(0)] private int pointsPerIngredient = 100;
        [SerializeField, Min(0)] private int pointsPerEnemy = 50;
        [Tooltip("Bono completo si termina en este tiempo o menos (segundos); baja hasta 0 al triple.")]
        [SerializeField, Min(1f)] private float parTime = 120f;
        [Tooltip("Apágalo si el nivel siempre dura lo mismo (el horno): el tiempo no da puntos ni sale en la tarjeta.")]
        [SerializeField] private bool scoreTime = true;
        [SerializeField, Min(0)] private int timeBonus = 1000;
        [SerializeField, Min(0)] private int noDamageBonus = 500;
        [Tooltip("Opcional (nivel del horno): cada vida que le quede al horno al terminar suma estos puntos.")]
        [SerializeField] private Oven oven;
        [SerializeField, Min(0)] private int pointsPerOvenLife = 200;

        [Header("Rango (fracción del máximo posible)")]
        [SerializeField, Range(0f, 1f)] private float rankS = 0.9f;
        [SerializeField, Range(0f, 1f)] private float rankA = 0.7f;
        [SerializeField, Range(0f, 1f)] private float rankB = 0.45f;
        [Tooltip("Nivel del horno: la vida que le debe quedar al horno (fracción) para poder sacar B, A y S. Con menos, " +
                 "el rango baja aunque los puntos den para más.")]
        [SerializeField] private Vector3 ovenLifeForBAS = new(0.4f, 0.6f, 0.8f);

        /// <summary>El puntaje del nivel abierto (null fuera de un nivel).</summary>
        public static LevelScore Current { get; private set; }

        /// <summary>Cambió algo que muestra el HUD (ingredientes).</summary>
        public event Action Changed;

        public int IngredientsFound => run.Ingredients.Count;
        public int IngredientsTotal { get; private set; }
        public int EnemiesDefeated => run.Enemies.Count;
        public float Seconds => run.Seconds;
        public bool Finished { get; private set; }

        // Lo que sobrevive a la recarga de la escena al reintentar desde un punto de control.
        private class Run
        {
            public string Scene;
            public float Seconds;
            public bool Damaged;
            public readonly HashSet<string> Ingredients = new();
            public readonly HashSet<string> Enemies = new();
        }

        private static Run run = new();
        private int enemiesTotal;
        private readonly HashSet<Health> discounted = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            run = new Run();
            Current = null;
        }

        private void Awake()
        {
            Current = this;
            string scene = SceneManager.GetActiveScene().name;
            // Sigue la misma carrera solo si reaparece en un punto de control de este nivel.
            if (run.Scene != scene || !Checkpoints.TryGet(out _))
                run = new Run { Scene = scene };
        }

        private void Start()
        {
            if (player != null)
            {
                player.Damaged += OnPlayerDamaged;
                player.Died += OnPlayerDied;
            }

            foreach (Pickup pickup in FindObjectsByType<Pickup>(FindObjectsInactive.Include))
            {
                if (pickup.Kind != PickupKind.Ingredient)
                    continue;
                IngredientsTotal++;
                if (run.Ingredients.Contains(KeyOf(pickup.transform)))
                    Destroy(pickup.gameObject);
            }
            foreach (Health health in FindObjectsByType<Health>(FindObjectsInactive.Include))
            {
                if (health != player)
                    Register(health);
            }
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.Damaged -= OnPlayerDamaged;
                player.Died -= OnPlayerDied;
            }
            if (Current == this)
                Current = null;
        }

        private void Update()
        {
            // Con el juego en pausa o en el Game Over (timeScale 0) el reloj no avanza.
            if (!Finished && (player == null || !player.IsDead))
                run.Seconds += Time.deltaTime;
        }

        /// <summary>Enemigo que cuenta para el puntaje (los que aparecen después del inicio se registran solos aquí).</summary>
        public void Register(Health enemy)
        {
            string key = KeyOf(enemy.transform);
            if (run.Enemies.Contains(key))
            {
                // Ya lo había derrotado antes de reintentar: no vuelve.
                Destroy(enemy.gameObject);
                enemiesTotal++;
                return;
            }
            enemiesTotal++;
            enemy.Died += () =>
            {
                if (!discounted.Contains(enemy))
                    run.Enemies.Add(key);
            };
        }

        /// <summary>
        /// Este enemigo no cuenta como derrotado por Ulises (se llama antes de matarlo: p. ej. los que caen solos al
        /// vencer a Polifemo). Tampoco cuenta para el máximo posible.
        /// </summary>
        public void Discount(Health enemy)
        {
            if (enemy != null && discounted.Add(enemy))
                enemiesTotal = Mathf.Max(0, enemiesTotal - 1);
        }

        /// <summary>Lo llama el ingrediente al recogerlo.</summary>
        public void CollectIngredient(Pickup ingredient)
        {
            run.Ingredients.Add(KeyOf(ingredient.transform));
            Changed?.Invoke();
        }

        /// <summary>
        /// Llegó a la meta: calcula el desglose y, si cuenta para la partida, guarda el récord y el rango del lugar
        /// (GameProgress) que terminó.
        /// </summary>
        public ScoreResult Finish(string location, bool save)
        {
            Finished = true;
            int ingredientPoints = IngredientsFound * pointsPerIngredient;
            int enemyPoints = EnemiesDefeated * pointsPerEnemy;
            float late = Mathf.Max(0f, run.Seconds - parTime) / (parTime * 2f);
            int timePoints = scoreTime ? Mathf.RoundToInt(timeBonus * Mathf.Clamp01(1f - late)) : 0;
            bool noDamage = !run.Damaged;
            int noDamagePoints = noDamage ? noDamageBonus : 0;
            int ovenLife = oven != null ? oven.Current : 0, ovenMax = oven != null ? oven.Max : 0;
            int ovenPoints = ovenLife * pointsPerOvenLife;
            int total = ingredientPoints + enemyPoints + timePoints + noDamagePoints + ovenPoints;

            int max = IngredientsTotal * pointsPerIngredient + enemiesTotal * pointsPerEnemy + (scoreTime ? timeBonus : 0) + noDamageBonus +
                      ovenMax * pointsPerOvenLife;
            float ratio = max > 0 ? (float)total / max : 1f;
            ScoreRank rank = ratio >= rankS ? ScoreRank.S : ratio >= rankA ? ScoreRank.A : ratio >= rankB ? ScoreRank.B : ScoreRank.C;
            if (ovenMax > 0)
            {
                // Lo que importa en el horno es defenderlo: con poca vida no hay rango alto.
                float life = (float)ovenLife / ovenMax + 0.001f;
                ScoreRank cap = life >= ovenLifeForBAS.z ? ScoreRank.S : life >= ovenLifeForBAS.y ? ScoreRank.A :
                    life >= ovenLifeForBAS.x ? ScoreRank.B : ScoreRank.C;
                if (rank > cap)
                    rank = cap;
            }

            bool newRecord = false;
            if (save)
            {
                newRecord = GameProgress.SubmitScore(location, total);
                GameProgress.SubmitRank(location, (int)rank);
            }
            return new ScoreResult(IngredientsFound, IngredientsTotal, ingredientPoints, run.Seconds, timePoints,
                EnemiesDefeated, enemyPoints, noDamage, noDamagePoints, rank, newRecord, ovenLife, ovenMax, ovenPoints, scoreTime);
        }

        /// <summary>Tiempo como "1:05".</summary>
        public static string FormatTime(float seconds)
        {
            int whole = Mathf.FloorToInt(seconds);
            return $"{whole / 60}:{whole % 60:00}";
        }

        private void OnPlayerDamaged(DamageInfo info) => run.Damaged = true;

        // Las muertes se cuentan en toda la partida y salen en el Tablero de Honor del festival.
        private void OnPlayerDied() => GameProgress.AddDeath();

        // Cada cosa del nivel se reconoce por dónde empieza (no se mueven antes del Start).
        private static string KeyOf(Transform target)
        {
            Vector3 position = target.position;
            return $"{target.name}@{position.x:0.0},{position.y:0.0}";
        }
    }
}
