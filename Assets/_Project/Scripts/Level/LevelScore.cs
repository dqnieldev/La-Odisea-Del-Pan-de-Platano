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
        public readonly int Total;
        public readonly ScoreRank Rank;
        public readonly bool NewRecord;

        public ScoreResult(int ingredientsFound, int ingredientsTotal, int ingredientPoints, float seconds, int timePoints,
            int enemiesDefeated, int enemyPoints, bool noDamage, int noDamagePoints, ScoreRank rank, bool newRecord)
        {
            IngredientsFound = ingredientsFound;
            IngredientsTotal = ingredientsTotal;
            IngredientPoints = ingredientPoints;
            Seconds = seconds;
            TimePoints = timePoints;
            EnemiesDefeated = enemiesDefeated;
            EnemyPoints = enemyPoints;
            NoDamage = noDamage;
            NoDamagePoints = noDamagePoints;
            Total = ingredientPoints + timePoints + enemyPoints + noDamagePoints;
            Rank = rank;
            NewRecord = newRecord;
        }
    }

    /// <summary>
    /// Puntaje del nivel: ingredientes recogidos, tiempo, enemigos derrotados y si Ulises no recibió daño. Al llegar a
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
        [SerializeField, Min(0)] private int timeBonus = 1000;
        [SerializeField, Min(0)] private int noDamageBonus = 500;

        [Header("Rango (fracción del máximo posible)")]
        [SerializeField, Range(0f, 1f)] private float rankS = 0.9f;
        [SerializeField, Range(0f, 1f)] private float rankA = 0.7f;
        [SerializeField, Range(0f, 1f)] private float rankB = 0.45f;

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
                player.Damaged += OnPlayerDamaged;

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
                player.Damaged -= OnPlayerDamaged;
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
            enemy.Died += () => run.Enemies.Add(key);
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
            int timePoints = Mathf.RoundToInt(timeBonus * Mathf.Clamp01(1f - late));
            bool noDamage = !run.Damaged;
            int noDamagePoints = noDamage ? noDamageBonus : 0;
            int total = ingredientPoints + enemyPoints + timePoints + noDamagePoints;

            int max = IngredientsTotal * pointsPerIngredient + enemiesTotal * pointsPerEnemy + timeBonus + noDamageBonus;
            float ratio = max > 0 ? (float)total / max : 1f;
            ScoreRank rank = ratio >= rankS ? ScoreRank.S : ratio >= rankA ? ScoreRank.A : ratio >= rankB ? ScoreRank.B : ScoreRank.C;

            bool newRecord = false;
            if (save)
            {
                newRecord = GameProgress.SubmitScore(location, total);
                GameProgress.SubmitRank(location, (int)rank);
            }
            return new ScoreResult(IngredientsFound, IngredientsTotal, ingredientPoints, run.Seconds, timePoints,
                EnemiesDefeated, enemyPoints, noDamage, noDamagePoints, rank, newRecord);
        }

        /// <summary>Tiempo como "1:05".</summary>
        public static string FormatTime(float seconds)
        {
            int whole = Mathf.FloorToInt(seconds);
            return $"{whole / 60}:{whole % 60:00}";
        }

        private void OnPlayerDamaged(DamageInfo info) => run.Damaged = true;

        // Cada cosa del nivel se reconoce por dónde empieza (no se mueven antes del Start).
        private static string KeyOf(Transform target)
        {
            Vector3 position = target.position;
            return $"{target.name}@{position.x:0.0},{position.y:0.0}";
        }
    }
}
