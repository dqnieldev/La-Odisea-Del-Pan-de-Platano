using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Escena "Creditos" (la abre el menú principal): el fondo del menú oscurecido y el texto de Text/Creditos.txt
    /// subiendo despacio (<see cref="CreditsScreen"/>). La música del menú sigue sonando.
    /// Menú: Odisea > Menús > Crear créditos.
    /// </summary>
    public static class CreditsBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Menus/Creditos.unity";
        private const string TextPath = "Assets/_Project/Text/Creditos.txt";
        // Su propio fondo (Ulises contra Polifemo); si no está, el del menú.
        private const string BackgroundPath = "Assets/_Project/Art/UI/FondoCreditos.jpeg";
        private const string MenuBackgroundPath = "Assets/_Project/Art/UI/FondoMenu.jpg";

        [MenuItem("Odisea/Menús/Crear créditos")]
        public static void Build()
        {
            var scene = MenuScenesBuilder.NewScene(ScenePath);
            MenuScenesBuilder.CreateCamera(MenuScenesBuilder.Cafe);
            Transform canvas = MenuScenesBuilder.CreateCanvas("Créditos");
            MenuScenesBuilder.CreateEventSystem();
            MenuScenesBuilder.CreateUiAudio();
            // Con su canción propia; si no está, sigue la del menú.
            if (GameSoundsBuilder.LoadMusic(MenuScenesBuilder.CreditsMusicPath) != null)
                GameSoundsBuilder.CreateMusic("Música de créditos", MenuScenesBuilder.CreditsMusicPath);
            else
                MenuScenesBuilder.CreateMenuMusic();
            var canvasRect = (RectTransform)canvas;

            Sprite background = MenuScenesBuilder.LoadUiSprite(BackgroundPath);
            bool own = background != null;
            background ??= AssetDatabase.LoadAssetAtPath<Sprite>(MenuBackgroundPath);
            if (background != null)
                MenuScenesBuilder.CreateImage("Fondo", canvasRect, background, Color.white, Vector2.zero, Vector2.zero, stretch: true);
            // Con su fondo propio, un velo más ligero para que se vea el dibujo (el texto sigue leyéndose).
            MenuScenesBuilder.CreateImage("Oscurecer", canvasRect, null, new Color(0.11f, 0.075f, 0.055f, own ? 0.72f : 0.85f), Vector2.zero,
                Vector2.zero, stretch: true);

            // El texto empieza justo abajo de la pantalla (pivote arriba, anclado abajo) y sube.
            TMP_Text text = MenuScenesBuilder.CreateText("Texto", canvasRect, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 40,
                MenuScenesBuilder.Crema, anchor: new Vector2(0.5f, 0f), rectSize: new Vector2(1400f, 4000f),
                alignment: TextAlignmentOptions.Top);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.lineSpacing = 12f;

            CreatePromptBar(canvasRect);

            var screen = new GameObject("Pantalla de créditos").AddComponent<CreditsScreen>();
            MenuScenesBuilder.SetField(screen, "source", AssetDatabase.LoadAssetAtPath<TextAsset>(TextPath));
            MenuScenesBuilder.SetField(screen, "text", text);
            MenuScenesBuilder.SetField(screen, "scroll", text.rectTransform);

            EditorSceneManager.SaveScene(scene);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Créditos creados en {ScenePath}.");
        }

        // Abajo a la derecha: [Aceptar] Adelantar  [Atrás] Volver.
        private static void CreatePromptBar(RectTransform canvas)
        {
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            var go = new GameObject("Ayuda de botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 28f);
            rect.sizeDelta = new Vector2(520f, 60f);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            Image fast = MenuScenesBuilder.CreatePromptIcon(rect);
            Light(MenuScenesBuilder.CreatePromptLabel(rect, font, "Adelantar"));
            MenuScenesBuilder.CreatePromptSpacer(rect);
            Image back = MenuScenesBuilder.CreatePromptIcon(rect);
            Light(MenuScenesBuilder.CreatePromptLabel(rect, font, "Volver"));
            fast.color = back.color = MenuScenesBuilder.Crema;

            var bar = go.AddComponent<ButtonPromptBar>();
            MenuScenesBuilder.SetField(bar, "confirmIcon", fast);
            MenuScenesBuilder.SetField(bar, "backIcon", back);
            MenuScenesBuilder.SetPromptSet(bar, "keyboard", "kb_enter", "kb_esc", "kb_q", "kb_e");
            MenuScenesBuilder.SetPromptSet(bar, "xbox", "xbox_a", "xbox_b", "xbox_lb", "xbox_rb");
            MenuScenesBuilder.SetPromptSet(bar, "playStation", "ps_cross", "ps_circle", "ps_l1", "ps_r1");
        }

        private static void Light(TMP_Text label)
        {
            label.color = MenuScenesBuilder.Crema;
            label.fontSize = 30;
        }
    }
}
