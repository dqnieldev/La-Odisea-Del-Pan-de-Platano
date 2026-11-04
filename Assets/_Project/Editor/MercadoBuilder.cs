using System.Linq;
using Odisea.Core;
using Odisea.Level;
using UnityEditor;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Nivel 1, El Mercado (Odisea > Niveles > Crear Mercado): nivel vertical sacado de estructura_nivel_1.jpg. Son dos
    /// torres de repisas con un hueco al centro; los pisos completos están en el suelo, a la mitad y arriba.
    /// Las repisas son firmes (no se atraviesan desde abajo): se sube por las escaleras (seguro y lento) o por las
    /// orillas del hueco con doble salto (rápido; si falla, cae varios pisos). Los ingredientes salen en lugares al azar
    /// cada vez que se juega (algunos escondidos o sobre el hueco) y la meta está en la cima (el racimo de plátano). No
    /// hay puntos de control: al morir se empieza desde abajo.
    /// Sin el arte del nivel, tablones, vigas y escaleras quedan de color.
    /// </summary>
    public static class MercadoBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Mercado.unity";
        private const string MusicPath = "Assets/_Project/Audio/Music/musica_nivel1";

        // Medidas del nivel (unidades). El suelo está en y = 0 y cada piso queda FloorGap más arriba.
        private const float FloorGap = 4f;
        private const int TopFloor = 10;
        private const float HalfWidth = 13.5f;
        // Vigas (columnas de las dos torres), como en el boceto.
        private static readonly float[] PostXs = { -11.2f, -4.8f, 5f, 11.1f };

        private static readonly Color Backdrop = new(0.93f, 0.87f, 0.76f);
        private static readonly Color Floor = new(0.545f, 0.353f, 0.169f);
        private static readonly Color Wall = new(0.36f, 0.24f, 0.15f);
        private static readonly Color BackWall = new(0.86f, 0.78f, 0.64f);

        // Repisas de cada piso (de dónde a dónde en x); los pisos 5 y 10 cruzan de lado a lado. Las orillas del hueco se
        // alternan (una más adentro, la de arriba más afuera) para que se pueda subir saltando junto a ellas.
        private static readonly (int floor, float from, float to)[] Shelves =
        {
            (1, -13f, -3f), (1, 3f, 13f),
            (2, -13f, -4.5f), (2, 4.5f, 13f),
            (3, -13f, -2.5f), (3, 3f, 13f),
            (4, -13f, -3.5f), (4, 2.5f, 13f),
            (5, -13f, 13f),
            (6, -13f, -3f), (6, 2.5f, 13f),
            (7, -13f, -4.5f), (7, 4.5f, 13f),
            (8, -13f, -2.5f), (8, 3f, 13f),
            (9, -13f, -4f), (9, 4f, 13f),
            (10, -13f, 13f),
        };
        // En los pisos completos, el tramo sobre el hueco se atraviesa desde abajo (la ruta por saltos sigue).
        private const float GapHalfWidth = 3f;
        // Ancho del tramo de paso encima de cada escalera.
        private const float HatchWidth = 1.6f;

        // Escaleras (x, del piso, al piso), entre las vigas para no quedar encimadas. Ruta: la torre izquierda hasta el piso 5, cruzar, y la derecha hasta arriba.
        // A cada torre le faltan escalones (hay que subir esos pisos con doble salto) y la izquierda de arriba no llega a
        // la cima: hay que saltar el hueco.
        private static readonly (float x, int from, int to)[] Ladders =
        {
            (-6.5f, 0, 1), (-6.5f, 1, 2), (-9f, 2, 3), (-6.5f, 3, 4), (-9f, 4, 5),
            (-6.5f, 5, 6), (-6.5f, 6, 7), (-9f, 8, 9),
            (7f, 0, 1), (7f, 1, 2), (9f, 3, 4), (7f, 4, 5),
            (7f, 5, 6), (9f, 6, 7), (7.5f, 7, 8), (9f, 8, 9), (7.5f, 9, 10),
        };

        // Lugares posibles de los ingredientes (x, piso, altura sobre el piso): en las orillas, sobre el hueco y en la torre
        // que no es la ruta. En cada partida salen IngredientCount de ellos, al azar.
        private static readonly (float x, int floor, float height)[] IngredientSpots =
        {
            (-12f, 1, 1f), (12f, 1, 1f), (0f, 2, 1.6f), (10f, 2, 1f), (-12f, 3, 1f), (12.2f, 3, 1f),
            (-3.8f, 4, 1f), (11.8f, 4, 1f), (0f, 5, 1f), (-12f, 5, 1f), (12f, 6, 1f), (-9f, 6, 1f),
            (0f, 7, 1.6f), (-12f, 7, 1f), (12f, 8, 1f), (-3.6f, 8, 1f), (-12f, 9, 1f), (11.8f, 9, 1f),
        };
        private const int IngredientCount = 8;

        // Lugares posibles del ítem de ayuda (pescado, hoja o café, al azar): sale uno solo por partida.
        private static readonly (float x, int floor)[] HelperSpots = { (10f, 5), (-4f, 3), (11f, 7), (-6f, 8) };
        private static readonly PickupKind[] HelperKinds = { PickupKind.Heal, PickupKind.Shield, PickupKind.Speed };

        private static float Surface(int floor) => floor * FloorGap;

        [MenuItem("Odisea/Niveles/Crear Mercado")]
        public static void Build()
        {
            TutorialBuilder.EnsureCircleSprite();
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Sprite square = level.Square;
            Transform root = level.Root;
            int ground = level.GroundLayer;
            float top = Surface(TopFloor);

            // ----- Suelo, paredes y el fondo de las torres -----
            PlayerSandboxBuilder.CreateBlock("Suelo", root, square, new Vector2(0f, -1f), new Vector2(HalfWidth * 2f, 2f), Floor, ground);
            PlayerSandboxBuilder.CreateBlock("Pared izquierda", root, square, new Vector2(-HalfWidth - 0.5f, top / 2f + 2f),
                new Vector2(1f, top + 8f), Wall, ground);
            PlayerSandboxBuilder.CreateBlock("Pared derecha", root, square, new Vector2(HalfWidth + 0.5f, top / 2f + 2f),
                new Vector2(1f, top + 8f), Wall, ground);
            foreach (float side in new[] { -1f, 1f })
            {
                var back = PlayerSandboxBuilder.CreateBlock(side < 0f ? "Fondo torre izquierda" : "Fondo torre derecha", root, square,
                    new Vector2(side * 8f, top / 2f), new Vector2(10.5f, top), BackWall, 0);
                Object.DestroyImmediate(back.GetComponent<BoxCollider2D>());
                back.GetComponent<SpriteRenderer>().sortingOrder = -10;
            }

            // ----- Repisas, vigas y escaleras -----
            var art = PlayerSandboxBuilder.LevelArt.Load();
            foreach ((int floor, float from, float to) in Shelves)
            {
                bool full = to - from > 20f;
                string side = full ? "completo" : from < 0f ? "izquierda" : "derecha";
                // Pasos: encima de cada escalera que llega a este piso y, en los pisos completos, sobre el hueco. Las
                // orillas que dan al hueco terminan rotas; las de la pared, enteras.
                var passes = Ladders.Where(ladder => ladder.to == floor).Select(ladder => (ladder.x, HatchWidth / 2f)).ToList();
                if (full)
                    passes.Add((0f, GapHalfWidth));
                PlayerSandboxBuilder.CreateFirmPlatform($"Piso {floor} ({side})", root, square, art, from, to, Surface(floor), ground,
                    passes, brokenStart: !full && from > 0f, brokenEnd: !full && to < 0f);
            }
            if (art.Post != null)
            {
                foreach (float x in PostXs)
                    PlayerSandboxBuilder.CreatePost(root, art.Post, new Vector2(x, 0f), top - 0.4f, ground);
            }
            foreach ((float x, int from, int to) in Ladders)
                PlayerSandboxBuilder.CreateLadder($"Escalera {from}-{to}", root, square, art, x, Surface(from), Surface(to), level.LadderLayer);

            // ----- Ingredientes y el ítem de ayuda (en lugares al azar) -----
            var kinds = (PlayerSandboxBuilder.Ingredient[])System.Enum.GetValues(typeof(PlayerSandboxBuilder.Ingredient));
            var candidates = new Object[IngredientSpots.Length];
            for (int i = 0; i < IngredientSpots.Length; i++)
            {
                (float x, int floor, float height) = IngredientSpots[i];
                candidates[i] = PlayerSandboxBuilder.CreateIngredient(level, kinds[i % kinds.Length], new Vector2(x, Surface(floor) + height))
                    .GetComponent<Pickup>();
            }
            var spawner = new GameObject("Ingredientes al azar").AddComponent<RandomPickups>();
            PlayerSandboxBuilder.SetSerialized(spawner, "candidates", candidates);
            PlayerSandboxBuilder.SetSerialized(spawner, "keep", IngredientCount);
            // Ítem de ayuda: los tres tipos en cada lugar posible, y al jugar queda solo uno (tipo y lugar al azar).
            var helpers = new System.Collections.Generic.List<Object>();
            foreach ((float x, int floor) in HelperSpots)
            {
                foreach (PickupKind kind in HelperKinds)
                    helpers.Add(PlayerSandboxBuilder.CreatePickup(level, kind, new Vector2(x, Surface(floor) + 1f)).GetComponent<Pickup>());
            }
            var helperSpawner = new GameObject("Ítem de ayuda al azar").AddComponent<RandomPickups>();
            PlayerSandboxBuilder.SetSerialized(helperSpawner, "candidates", helpers.ToArray());
            PlayerSandboxBuilder.SetSerialized(helperSpawner, "keep", 1);

            // ----- Meta: el racimo de plátano en la cima -----
            CreateGoal(root, new Vector2(0f, top), level.PlayerLayer);

            PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(0f, 1f), "El Mercado", MusicPath,
                showControls: false, background: Backdrop);
            // La cámara sigue hacia arriba sin asomarse fuera de las paredes ni debajo del suelo.
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", new Vector2(-1f, 5f));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", new Vector2(1f, top - 2f));

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Mercado creado en {ScenePath}");
        }

        /// <summary>Meta en la cima: el racimo de plátano dorado (círculos de color mientras llega su dibujo).</summary>
        private static void CreateGoal(Transform parent, Vector2 floor, int playerLayer)
        {
            var goal = new GameObject("Meta (racimo de plátano)");
            goal.transform.SetParent(parent, false);
            goal.transform.position = floor + new Vector2(0f, 1.5f);
            var area = goal.AddComponent<BoxCollider2D>();
            area.size = new Vector2(2f, 3f);

            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            var glow = new GameObject("Brillo").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(goal.transform, false);
            glow.transform.localScale = Vector3.one * 2.6f;
            glow.sprite = circle;
            glow.color = new Color(1f, 0.93f, 0.6f, 0.6f);
            glow.sortingOrder = 4;
            for (int i = 0; i < 3; i++)
            {
                var banana = new GameObject($"Plátano {i + 1}").AddComponent<SpriteRenderer>();
                banana.transform.SetParent(goal.transform, false);
                banana.transform.localPosition = new Vector3((i - 1) * 0.45f, (i == 1 ? 0.2f : 0f), 0f);
                banana.transform.localScale = new Vector3(0.45f, 1.1f, 1f);
                banana.transform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 18f);
                banana.sprite = circle;
                banana.color = new Color(0.961f, 0.718f, 0.192f);
                banana.sortingOrder = 5;
            }

            var component = goal.AddComponent<LevelGoal>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "nextScene", SceneFlow.Map);
            PlayerSandboxBuilder.SetSerialized(component, "completesLocation", GameProgress.Mercado);
            PlayerSandboxBuilder.SetSerialized(component, "completeScreen", MenuScenesBuilder.CreateLevelCompleteScreen(
                "¡Mercado completado!", "Ulises llegó a la cima con el racimo de plátano."));
        }
    }
}
