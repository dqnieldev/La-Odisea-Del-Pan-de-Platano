using System;
using System.IO;
using Odisea.Core;
using Odisea.Player;
using Odisea.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Crea la escena Sandbox/PlayerSandbox para probar a Ulises con placeholders:
    /// suelo, plataformas a distintas alturas, un peligro y un muñeco para golpear.
    /// Menú: Odisea > Sandbox > Crear escena de prueba del jugador.
    /// </summary>
    public static class PlayerSandboxBuilder
    {
        private const string BaseScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/PlayerSandbox.unity";
        private const string SpritePath = "Assets/_Project/Art/Placeholders/Cuadro.png";
        private const string HeartFullPath = "Assets/_Project/Art/Placeholders/CorazonLleno.png";
        private const string HeartEmptyPath = "Assets/_Project/Art/Placeholders/CorazonVacio.png";

        // Corazón en pixel art. X = contorno, o = relleno, w = brillo. La primera fila es la de arriba.
        private static readonly string[] HeartPattern =
        {
            "..XXX.XXX..",
            ".XoooXoooX.",
            "XoowooooooX",
            "XowoooooooX",
            "XoooooooooX",
            ".XoooooooX.",
            "..XoooooX..",
            "...XoooX...",
            "....XoX....",
            ".....X.....",
        };
        private const string MaterialPath = "Assets/_Project/Settings/SinFriccion.physicsMaterial2D";
        private const string ConfigPath = "Assets/_Project/ScriptableObjects/Player/UlisesConfig.asset";
        private const string InputPath = "Assets/Settings/InputSystem_Actions.inputactions";

        // Colores del design system (Docs/Referencias/Paleta).
        private static readonly Color Platano = new(0.961f, 0.718f, 0.192f);
        private static readonly Color Cafe = new(0.169f, 0.118f, 0.086f);
        private static readonly Color Crema = new(0.984f, 0.973f, 0.949f);
        private static readonly Color Manglar = new(0.180f, 0.353f, 0.267f);
        private static readonly Color Chile = new(0.784f, 0.263f, 0.169f);
        private static readonly Color Mar = new(0.184f, 0.561f, 0.627f);
        private static readonly Color BeigeMasa = new(0.910f, 0.878f, 0.824f);
        private static readonly Color Madera = new(0.545f, 0.353f, 0.169f);

        [MenuItem("Odisea/Sandbox/Crear escena de prueba del jugador")]
        public static void Build()
        {
            int groundLayer = EnsureLayer("Ground", 6);
            int playerLayer = EnsureLayer("Player", 7);
            int enemyLayer = EnsureLayer("Enemy", 8);

            EnsurePlaceholderSprite();
            EnsureHeartSprite(HeartFullPath, Chile, Crema);
            EnsureHeartSprite(HeartEmptyPath, BeigeMasa, BeigeMasa);
            EnsureNoFrictionMaterial();
            EnsurePlayerConfig(groundLayer, enemyLayer);
            // Guardar antes de abrir la escena: OpenScene descarga los assets nuevos que no están en disco
            // y las referencias a ellos quedarían vacías.
            AssetDatabase.SaveAssets();

            EnsureFolder(Path.GetDirectoryName(ScenePath));
            if (!AssetDatabase.CopyAsset(BaseScenePath, ScenePath) &&
                AssetDatabase.LoadMainAssetAtPath(ScenePath) == null)
                throw new InvalidOperationException($"No se pudo copiar {BaseScenePath}.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearSceneExcept(scene, "Main Camera", "Global Light 2D");

            Sprite square = LoadRequired<Sprite>(SpritePath);
            var noFriction = LoadRequired<PhysicsMaterial2D>(MaterialPath);
            var config = LoadRequired<PlayerConfig>(ConfigPath);
            var inputActions = LoadRequired<InputActionAsset>(InputPath);
            var fullHeart = LoadRequired<Sprite>(HeartFullPath);
            var emptyHeart = LoadRequired<Sprite>(HeartEmptyPath);

            var level = new GameObject("Nivel").transform;
            CreateBlock("Suelo", level, square, new Vector2(10f, -1f), new Vector2(60f, 2f), Manglar, groundLayer);
            CreateBlock("Pared izquierda", level, square, new Vector2(-20.5f, 4f), new Vector2(1f, 12f), Manglar, groundLayer);
            CreateBlock("Pared derecha", level, square, new Vector2(40.5f, 4f), new Vector2(1f, 12f), Manglar, groundLayer);
            CreateBlock("Plataforma baja", level, square, new Vector2(4f, 2f), new Vector2(4f, 0.5f), Cafe, groundLayer);
            CreateBlock("Plataforma media", level, square, new Vector2(10f, 4.5f), new Vector2(3f, 0.5f), Cafe, groundLayer);
            CreateBlock("Plataforma doble salto", level, square, new Vector2(16f, 8f), new Vector2(3f, 0.5f), Cafe, groundLayer);
            CreateBlock("Escalón", level, square, new Vector2(-8f, 0.75f), new Vector2(3f, 1.5f), Cafe, groundLayer);

            var hazard = CreateBlock("Peligro (resta 1 vida)", level, square, new Vector2(24f, 0.25f), new Vector2(3f, 0.5f), Chile, groundLayer);
            var hazardComponent = hazard.AddComponent<Hazard>();
            SetSerialized(hazardComponent, "targetLayers", 1 << playerLayer);

            CreateDummy(level, square, enemyLayer);

            GameObject player = CreatePlayer(square, noFriction, config, inputActions, playerLayer);
            CreateHud(player.GetComponent<Health>(), fullHeart, emptyHeart);

            var camera = GameObject.Find("Main Camera");
            camera.transform.position = new Vector3(0f, 2f, -10f);
            var cam = camera.GetComponent<Camera>();
            cam.orthographicSize = 7f;
            cam.backgroundColor = Crema;
            var follow = camera.AddComponent<CameraFollow2D>();
            follow.Target = player.transform;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Odisea] Escena de prueba creada en {ScenePath}");
        }

        /// <summary>Punto de entrada para Unity en modo batch (-executeMethod).</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static GameObject CreatePlayer(Sprite square, PhysicsMaterial2D material, PlayerConfig config,
            InputActionAsset inputActions, int playerLayer)
        {
            var player = new GameObject("Ulises") { layer = playerLayer };
            player.transform.position = new Vector3(0f, 1f, 0f);

            var body = player.AddComponent<Rigidbody2D>();
            body.sharedMaterial = material;
            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.6f);

            // El sprite va en un hijo para poder cambiar el placeholder por el arte real sin tocar la física.
            var visual = new GameObject("Visual") { layer = playerLayer };
            visual.transform.SetParent(player.transform, false);
            visual.transform.localScale = new Vector3(0.8f, 1.6f, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = Platano;
            renderer.sortingOrder = 10;

            // "Nariz" para ver hacia dónde mira mientras no hay sprite real.
            var nose = new GameObject("Frente") { layer = playerLayer };
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0.35f, 0.4f, 0f);
            nose.transform.localScale = new Vector3(0.3f, 0.2f, 1f);
            var noseRenderer = nose.AddComponent<SpriteRenderer>();
            noseRenderer.sprite = square;
            noseRenderer.color = Cafe;
            noseRenderer.sortingOrder = 11;

            var groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(player.transform, false);
            groundCheck.localPosition = new Vector3(0f, -0.8f, 0f);

            var input = player.AddComponent<PlayerInputReader>();
            SetSerialized(input, "actions", inputActions);

            var health = player.AddComponent<Health>();
            SetSerialized(health, "maxHealth", 3);
            SetSerialized(health, "invulnerabilityTime", 1.2f);

            var controller = player.AddComponent<PlayerController>();
            SetSerialized(controller, "config", config);
            SetSerialized(controller, "input", input);
            SetSerialized(controller, "groundCheck", groundCheck);
            SetSerialized(controller, "health", health);

            var combat = player.AddComponent<PlayerCombat>();
            SetSerialized(combat, "input", input);

            var animator = player.AddComponent<PlayerAnimator>();
            SetSerialized(animator, "controller", controller);
            SetSerialized(animator, "combat", combat);
            SetSerialized(animator, "health", health);
            SetSerialized(animator, "spriteRenderer", renderer);

            var feedback = player.AddComponent<DamageFeedback>();
            SetSerialized(feedback, "health", health);
            SetSerialized(feedback, "visual", visual.transform);
            SetSerialized(feedback, "renderers", new UnityEngine.Object[] { renderer, noseRenderer });
            SetSerialized(feedback, "flashColor", Chile);

            CreateAttackVisual(player, combat, square, config, playerLayer);
            return player;
        }

        private static void CreateAttackVisual(GameObject player, PlayerCombat combat, Sprite square, PlayerConfig config, int layer)
        {
            // Rodillo: el pivote es la mano; la barra sale hacia adelante y gira hacia abajo.
            var pivot = new GameObject("Rodillo") { layer = layer };
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0.25f, 0.15f, 0f);

            var bar = new GameObject("Barra") { layer = layer };
            bar.transform.SetParent(pivot.transform, false);
            bar.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            bar.transform.localScale = new Vector3(1f, 0.22f, 1f);
            var barRenderer = bar.AddComponent<SpriteRenderer>();
            barRenderer.sprite = square;
            barRenderer.color = Madera;
            barRenderer.sortingOrder = 12;

            // Estela: ocupa exactamente el área que daña el golpe (PlayerConfig.attackOffset / attackSize).
            var trail = new GameObject("Estela") { layer = layer };
            trail.transform.SetParent(player.transform, false);
            trail.transform.localPosition = config.attackOffset;
            trail.transform.localScale = new Vector3(config.attackSize.x, config.attackSize.y, 1f);
            var trailRenderer = trail.AddComponent<SpriteRenderer>();
            trailRenderer.sprite = square;
            trailRenderer.color = new Color(Platano.r, Platano.g, Platano.b, 0.45f);
            trailRenderer.sortingOrder = 9;

            var attackVisual = player.AddComponent<PlayerAttackVisual>();
            SetSerialized(attackVisual, "combat", combat);
            SetSerialized(attackVisual, "swingPivot", pivot.transform);
            SetSerialized(attackVisual, "trail", trailRenderer);
        }

        private static void CreateDummy(Transform parent, Sprite square, int enemyLayer)
        {
            var dummy = new GameObject("Muñeco de práctica") { layer = enemyLayer };
            dummy.transform.SetParent(parent, false);
            dummy.transform.position = new Vector3(-4f, 0.75f, 0f);
            var collider = dummy.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 1.5f);
            collider.isTrigger = true;

            var visual = new GameObject("Visual") { layer = enemyLayer };
            visual.transform.SetParent(dummy.transform, false);
            visual.transform.localScale = new Vector3(1f, 1.5f, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = Mar;
            renderer.sortingOrder = 5;

            var health = dummy.AddComponent<Health>();
            SetSerialized(health, "maxHealth", 3);
            SetSerialized(health, "invulnerabilityTime", 0.2f);
            SetSerialized(health, "destroyOnDeath", false); // Lo destruye DamageFeedback tras la animación.

            var feedback = dummy.AddComponent<DamageFeedback>();
            SetSerialized(feedback, "health", health);
            SetSerialized(feedback, "visual", visual.transform);
            SetSerialized(feedback, "renderers", new UnityEngine.Object[] { renderer });
            SetSerialized(feedback, "shrinkAndDestroyOnDeath", true);
        }

        private static void CreateHud(Health playerHealth, Sprite fullHeart, Sprite emptyHeart)
        {
            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Design system: HUD anclado a 48 px de los bordes, vidas arriba a la izquierda.
            var lives = new GameObject("Vidas", typeof(RectTransform));
            var rect = (RectTransform)lives.transform;
            rect.SetParent(canvasObject.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(48f, -48f);
            rect.sizeDelta = new Vector2(300f, 80f);

            var hearts = lives.AddComponent<HeartsDisplay>();
            SetSerialized(hearts, "health", playerHealth);
            SetSerialized(hearts, "fullHeart", fullHeart);
            SetSerialized(hearts, "emptyHeart", emptyHeart);
        }

        private static GameObject CreateBlock(string name, Transform parent, Sprite sprite, Vector2 position,
            Vector2 size, Color color, int layer)
        {
            var block = new GameObject(name) { layer = layer };
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            block.AddComponent<BoxCollider2D>();
            return block;
        }

        private static void ClearSceneExcept(UnityEngine.SceneManagement.Scene scene, params string[] keep)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (Array.IndexOf(keep, root.name) < 0)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"No se encontró {path}.");
            return asset;
        }

        private static void SetSerialized(UnityEngine.Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"{target.GetType().Name} no tiene el campo '{field}'.");

            if (value is UnityEngine.Object[] references)
            {
                property.arraySize = references.Length;
                for (int i = 0; i < references.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            if (value is UnityEngine.Object reference)
            {
                property.objectReferenceValue = reference;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (new SerializedObject(target).FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException($"No se pudo asignar '{field}' en {target.GetType().Name}.");
                return;
            }

            SetSerializedValue(serialized, property, field, value);
        }

        private static void SetSerializedValue(SerializedObject serialized, SerializedProperty property, string field, object value)
        {
            switch (value)
            {
                case int i: // También cubre LayerMask.
                    property.intValue = i;
                    break;
                case float f:
                    property.floatValue = f;
                    break;
                case bool b:
                    property.boolValue = b;
                    break;
                case Color color:
                    property.colorValue = color;
                    break;
                default:
                    throw new ArgumentException($"Tipo no soportado para '{field}'.");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int EnsureLayer(string layerName, int preferredIndex)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    return i;
            }

            int index = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferredIndex).stringValue)
                ? preferredIndex
                : FirstEmptyUserLayer(layers);
            layers.GetArrayElementAtIndex(index).stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return index;
        }

        private static int FirstEmptyUserLayer(SerializedProperty layers)
        {
            for (int i = 6; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    return i;
            }
            throw new InvalidOperationException("No hay capas libres en Tags and Layers.");
        }

        private static void EnsurePlaceholderSprite()
        {
            if (!File.Exists(SpritePath))
            {
                EnsureFolder(Path.GetDirectoryName(SpritePath));
                var texture = new Texture2D(4, 4);
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SpritePath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 4;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureHeartSprite(string path, Color fill, Color shine)
        {
            if (File.Exists(path))
                return;

            EnsureFolder(Path.GetDirectoryName(path));
            int height = HeartPattern.Length;
            int width = HeartPattern[0].Length;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = HeartPattern[row][x] switch
                    {
                        'X' => Cafe,
                        'o' => fill,
                        'w' => shine,
                        _ => Color.clear,
                    };
                    // Las texturas empiezan abajo; el patrón está escrito de arriba hacia abajo.
                    texture.SetPixel(x, height - 1 - row, pixel);
                }
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureNoFrictionMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath) != null)
                return;

            EnsureFolder(Path.GetDirectoryName(MaterialPath));
            // Sin fricción para que Ulises no se quede pegado a las paredes al saltar contra ellas.
            var material = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        private static void EnsurePlayerConfig(int groundLayer, int enemyLayer)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath);
            if (config == null)
            {
                EnsureFolder(Path.GetDirectoryName(ConfigPath));
                config = ScriptableObject.CreateInstance<PlayerConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            if (config.groundLayers.value == 0)
                config.groundLayers = 1 << groundLayer;
            if (config.attackLayers.value == 0)
                config.attackLayers = 1 << enemyLayer;
            EditorUtility.SetDirty(config);
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
