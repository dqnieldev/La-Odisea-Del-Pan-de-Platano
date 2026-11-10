using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// El letrero de inicio de cada nivel (<see cref="LevelIntro"/>): "¿Listo?" y la frase del nivel, con la letra Rye en
    /// crema, contorno café y sombra. Los builders de cada nivel lo agregan; el menú lo pone en los niveles ya hechos sin
    /// tener que reconstruirlos. Menú: Odisea > Niveles > Agregar letrero de inicio.
    /// </summary>
    public static class LevelIntroBuilder
    {
        private const string CanvasName = "Inicio del nivel";
        private const string MaterialPath = "Assets/_Project/Art/UI/Rye_Letrero.mat";

        // Escena y frase de cada nivel.
        private static readonly (string scene, string phrase)[] Levels =
        {
            (TutorialBuilder.ScenePath, "¡A practicar!"),
            (MercadoBuilder.ScenePath, "¡A subir!"),
            (HornoBuilder.ScenePath, "¡Protege ese horneado!"),
            (TovaraBuilder.ScenePath, "¡No caigas al agua!"),
        };

        // Objetivo de cada nivel (el Horno ya trae el suyo, con el horno que parpadea): título y una línea de ayuda.
        private static readonly (string scene, string title, string hint)[] Objectives =
        {
            (MercadoBuilder.ScenePath, "¡LLEGA A LA CIMA!", "Recolecta los ingredientes del pan de plátano en el camino"),
            (TovaraBuilder.ScenePath, "¡LLEGA AL FESTIVAL!", "Cruza los manglares de panga en panga sin caer al agua"),
        };

        private const string ObjectiveName = "Letrero de objetivo";
        private static readonly Color ObjectiveYellow = new(1f, 0.84f, 0.25f);

        /// <summary>
        /// Pone el letrero del objetivo en la escena abierta, igual al del Horno: arriba, sobre una franja café, el título
        /// en amarillo y la ayuda debajo; entra con un rebote y se va subiendo (<see cref="LevelIntroBanner"/>). Sale al
        /// arrancar el juego, después del "¿Listo?". Sin objetivo para esa escena, no pone nada.
        /// </summary>
        public static void AddObjective(string scenePath)
        {
            if (GameObject.Find(ObjectiveName) is { } old)
                Object.DestroyImmediate(old);
            foreach ((string scene, string title, string hint) in Objectives)
            {
                if (scene != scenePath)
                    continue;
                Transform canvas = MenuScenesBuilder.CreateCanvas(ObjectiveName);
                canvas.GetComponent<Canvas>().sortingOrder = 5;
                canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
                var group = canvas.gameObject.AddComponent<CanvasGroup>();
                group.interactable = group.blocksRaycasts = false;
                group.alpha = 0f;

                var content = (RectTransform)new GameObject("Letrero", typeof(RectTransform)).transform;
                content.SetParent(canvas, false);
                content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
                content.anchoredPosition = new Vector2(0f, 330f);
                content.sizeDelta = new Vector2(1160f, 200f);
                var band = content.gameObject.AddComponent<UnityEngine.UI.Image>();
                band.sprite = UiAssetsBuilder.EnsureRoundedSprite();
                band.type = UnityEngine.UI.Image.Type.Sliced;
                band.color = new Color(PlayerSandboxBuilder.Cafe.r, PlayerSandboxBuilder.Cafe.g, PlayerSandboxBuilder.Cafe.b, 0.82f);
                band.raycastTarget = false;

                TMP_Text Label(string name, string text, Vector2 position, Vector2 size, float fontSize)
                {
                    var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
                    rect.SetParent(content, false);
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = position;
                    rect.sizeDelta = size;
                    var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
                    label.text = text;
                    label.font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
                    label.fontSize = fontSize;
                    label.characterSpacing = 4f;
                    label.alignment = TextAlignmentOptions.Center;
                    label.color = PlayerSandboxBuilder.Crema;
                    label.outlineWidth = 0.25f;
                    label.outlineColor = PlayerSandboxBuilder.Cafe;
                    label.raycastTarget = false;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = fontSize * 0.6f;
                    label.fontSizeMax = fontSize;
                    return label;
                }
                TMP_Text heading = Label("Título", title, new Vector2(0f, 28f), new Vector2(1120f, 124f), 88f);
                heading.color = ObjectiveYellow;
                heading.outlineWidth = 0.3f;
                TMP_Text help = Label("Ayuda", hint, new Vector2(0f, -58f), new Vector2(1120f, 54f), 40f);
                help.characterSpacing = 1f;

                var banner = canvas.gameObject.AddComponent<LevelIntroBanner>();
                PlayerSandboxBuilder.SetSerialized(banner, "group", group);
                PlayerSandboxBuilder.SetSerialized(banner, "content", content);
            }
        }

        public static string PhraseFor(string scenePath)
        {
            foreach ((string scene, string phrase) in Levels)
            {
                if (scene == scenePath)
                    return phrase;
            }
            return "¡A jugar!";
        }

        /// <summary>Pone el letrero en la escena abierta (quita el anterior si ya había uno).</summary>
        public static void Add(string phrase)
        {
            if (GameObject.Find(CanvasName) is { } old)
                Object.DestroyImmediate(old);
            Transform canvas = MenuScenesBuilder.CreateCanvas(CanvasName);
            // Encima del HUD y debajo de la pausa y los diálogos.
            canvas.GetComponent<Canvas>().sortingOrder = 15;
            canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RyeRegular);
            TMP_Text text = MenuScenesBuilder.CreateText("Letrero", canvas, "¿Listo?", font, 190, MenuScenesBuilder.Crema,
                rectSize: new Vector2(1800f, 400f));
            text.fontSharedMaterial = EnsureMaterial(font);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.characterSpacing = 4f;
            // Las frases largas ("¡Protege ese horneado!") se achican solas para caber a lo ancho.
            text.enableAutoSizing = true;
            text.fontSizeMin = 70f;
            text.fontSizeMax = 190f;
            text.rectTransform.sizeDelta = new Vector2(1600f, 400f);

            var intro = canvas.gameObject.AddComponent<LevelIntro>();
            MenuScenesBuilder.SetField(intro, "text", text);
            MenuScenesBuilder.SetField(intro, "group", group);
            var serialized = new SerializedObject(intro);
            SerializedProperty lines = serialized.FindProperty("lines");
            lines.arraySize = 2;
            lines.GetArrayElementAtIndex(0).stringValue = "¿Listo?";
            lines.GetArrayElementAtIndex(1).stringValue = phrase;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameSoundsBuilder.SetSound(intro, "popSound", new[] { UiAssetsBuilder.LoadUiSound("ui_click") }, 0.6f);
        }

        // Material de la letra Rye con contorno café y sombra (un asset propio: no cambia el de los demás textos).
        private static Material EnsureMaterial(TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(font.material) { name = "Rye_Letrero" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = font.material.shader;
            material.SetTexture(ShaderUtilities.ID_MainTex, font.material.GetTexture(ShaderUtilities.ID_MainTex));
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.24f, 0.13f, 0.07f));
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.55f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.7f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.7f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        [MenuItem("Odisea/Niveles/Agregar letrero de inicio")]
        public static void AddToAllLevels()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach ((string scene, string phrase) in Levels)
            {
                if (!System.IO.File.Exists(scene))
                    continue;
                var opened = EditorSceneManager.OpenScene(scene);
                Add(phrase);
                EditorSceneManager.SaveScene(opened);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Odisea] Letrero de inicio agregado a los niveles.");
        }
    }
}
