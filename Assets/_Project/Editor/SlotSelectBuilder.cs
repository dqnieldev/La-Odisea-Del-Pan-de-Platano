using Odisea.Core;
using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Escena "Partidas" (la abre Jugar): el fondo del menú oscurecido, tres tarjetas de partida con su avance
    /// (un punto por lugar del mapa), la pregunta para borrar una y la ayuda de botones.
    /// </summary>
    public static class SlotSelectBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Menus/Partidas.unity";
        private const string BackgroundPath = "Assets/_Project/Art/UI/FondoMenu.jpg";
        private static readonly string[] LocationNames = { "Tutorial", "Mercado", "Horno", "Tovara" };
        private static readonly Vector2 CardSize = new(500f, 580f);
        private const float CardSpacing = 580f;

        [MenuItem("Odisea/Menús/Crear pantalla de partidas")]
        public static void Build()
        {
            var scene = MenuScenesBuilder.NewScene(ScenePath);
            MenuScenesBuilder.CreateCamera(MenuScenesBuilder.Cafe);
            Transform canvas = MenuScenesBuilder.CreateCanvas("Partidas");
            MenuScenesBuilder.CreateEventSystem();
            MenuScenesBuilder.CreateUiAudio();
            MenuScenesBuilder.CreateMenuMusic();
            var canvasRect = (RectTransform)canvas;

            TMP_FontAsset titleFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            TMP_FontAsset bodyFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            TMP_FontAsset buttonFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();

            var background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (background != null)
                MenuScenesBuilder.CreateImage("Fondo", canvasRect, background, Color.white, Vector2.zero, Vector2.zero, stretch: true);
            MenuScenesBuilder.CreateImage("Oscurecer", canvasRect, null, new Color(0.11f, 0.075f, 0.055f, 0.72f), Vector2.zero,
                Vector2.zero, stretch: true);
            MenuScenesBuilder.CreateText("Título", canvasRect, "Elige tu partida", titleFont, 84, MenuScenesBuilder.Crema,
                position: new Vector2(0f, 420f), rectSize: new Vector2(1400f, 120f));

            // ----- Tarjetas -----
            var cards = new Button[GameProgress.SlotCount];
            var serializedCards = new (Button button, TMP_Text summary, Image[] dots, Button delete)[GameProgress.SlotCount];
            for (int i = 0; i < GameProgress.SlotCount; i++)
            {
                float x = (i - (GameProgress.SlotCount - 1) / 2f) * CardSpacing;
                serializedCards[i] = CreateCard(i + 1, canvasRect, new Vector2(x, 0f), rounded, titleFont, bodyFont, buttonFont);
                cards[i] = serializedCards[i].button;
            }
            // Izquierda y derecha entre tarjetas (Borrar va aparte: tecla Q / Y / Triángulo).
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? cards[i - 1] : null,
                    selectOnRight = i + 1 < cards.Length ? cards[i + 1] : null,
                };
            }

            // ----- Confirmar borrar -----
            var confirm = new GameObject("Confirmar borrar", typeof(RectTransform));
            var confirmRect = (RectTransform)confirm.transform;
            confirmRect.SetParent(canvasRect, false);
            MenuScenesBuilder.Stretch(confirmRect);
            MenuScenesBuilder.CreateImage("Oscurecer", confirmRect, null, new Color(0.11f, 0.075f, 0.055f, 0.7f), Vector2.zero,
                Vector2.zero, stretch: true).raycastTarget = true;
            RectTransform dialog = MenuScenesBuilder.CreateCard("Tarjeta", confirmRect, rounded, new Vector2(900f, 400f), MenuScenesBuilder.Crema);
            TMP_Text question = MenuScenesBuilder.CreateText("Pregunta", dialog, "", bodyFont, 42, MenuScenesBuilder.Cafe,
                position: new Vector2(0f, 70f), rectSize: new Vector2(820f, 160f));
            Button no = MenuScenesBuilder.CreateMenuButton("NO", dialog, rounded, buttonFont, new Vector2(240f, 90f), 44);
            ((RectTransform)no.transform).anchoredPosition = new Vector2(-150f, -105f);
            Button yes = MenuScenesBuilder.CreateMenuButton("SÍ, BORRAR", dialog, rounded, buttonFont, new Vector2(300f, 90f), 40);
            ((RectTransform)yes.transform).anchoredPosition = new Vector2(170f, -105f);
            no.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = yes };
            yes.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = no };

            CreatePromptBar(canvasRect, buttonFont);

            var screen = new GameObject("Pantalla de partidas").AddComponent<SlotSelectScreen>();
            var serialized = new SerializedObject(screen);
            SerializedProperty list = serialized.FindProperty("cards");
            list.arraySize = serializedCards.Length;
            for (int i = 0; i < serializedCards.Length; i++)
            {
                SerializedProperty card = list.GetArrayElementAtIndex(i);
                card.FindPropertyRelative("button").objectReferenceValue = serializedCards[i].button;
                card.FindPropertyRelative("summary").objectReferenceValue = serializedCards[i].summary;
                card.FindPropertyRelative("deleteButton").objectReferenceValue = serializedCards[i].delete;
                SerializedProperty dots = card.FindPropertyRelative("dots");
                dots.arraySize = serializedCards[i].dots.Length;
                for (int d = 0; d < serializedCards[i].dots.Length; d++)
                    dots.GetArrayElementAtIndex(d).objectReferenceValue = serializedCards[i].dots[d];
            }
            serialized.FindProperty("confirm").objectReferenceValue = confirm;
            serialized.FindProperty("confirmText").objectReferenceValue = question;
            serialized.FindProperty("confirmYes").objectReferenceValue = yes;
            serialized.FindProperty("confirmNo").objectReferenceValue = no;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Pantalla de partidas creada en {ScenePath}.");
        }

        // Tarjeta crema (botón: Aceptar entra) con "Partida N", su avance, un punto por lugar y "Borrar" para el mouse.
        private static (Button, TMP_Text, Image[], Button) CreateCard(int slot, RectTransform parent, Vector2 position, Sprite rounded,
            TMP_FontAsset titleFont, TMP_FontAsset bodyFont, TMP_FontAsset buttonFont)
        {
            var root = new GameObject($"Partida {slot}", typeof(RectTransform), typeof(Button));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = CardSize;

            MenuScenesBuilder.CreateImage("Sombra", rect, rounded, MenuScenesBuilder.Cafe, new Vector2(0f, -10f), CardSize, stretch: false);
            var face = new GameObject("Cara", typeof(RectTransform));
            var faceRect = (RectTransform)face.transform;
            faceRect.SetParent(rect, false);
            MenuScenesBuilder.Stretch(faceRect);
            MenuScenesBuilder.CreateImage("Borde", faceRect, rounded, MenuScenesBuilder.Cafe, Vector2.zero, Vector2.zero, stretch: true);
            Image fill = MenuScenesBuilder.CreateImage("Relleno", faceRect, rounded, MenuScenesBuilder.Crema, Vector2.zero, Vector2.zero,
                stretch: true, inset: 6f);
            fill.raycastTarget = true;

            MenuScenesBuilder.CreateText("Nombre", faceRect, $"Partida {slot}", titleFont, 60, MenuScenesBuilder.Cafe,
                position: new Vector2(0f, 200f), rectSize: new Vector2(440f, 90f));
            TMP_Text summary = MenuScenesBuilder.CreateText("Avance", faceRect, "", bodyFont, 34, MenuScenesBuilder.TintaClara,
                position: new Vector2(0f, 110f), rectSize: new Vector2(440f, 60f));

            // Un punto por lugar del mapa, con su nombre abajo.
            var dots = new Image[LocationNames.Length];
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(TutorialBuilder.CirclePath);
            for (int d = 0; d < LocationNames.Length; d++)
            {
                float x = (d - (LocationNames.Length - 1) / 2f) * 105f;
                dots[d] = MenuScenesBuilder.CreateImage($"Punto {LocationNames[d]}", faceRect, circle, MenuScenesBuilder.Cafe,
                    new Vector2(x, 0f), new Vector2(54f, 54f), stretch: false);
                MenuScenesBuilder.CreateText($"Lugar {LocationNames[d]}", faceRect, LocationNames[d], bodyFont, 22,
                    MenuScenesBuilder.TintaClara, position: new Vector2(x, -50f), rectSize: new Vector2(105f, 36f));
            }

            Button delete = MenuScenesBuilder.CreateMenuButton("BORRAR", faceRect, rounded, buttonFont, new Vector2(200f, 64f), 30);
            ((RectTransform)delete.transform).anchoredPosition = new Vector2(0f, -200f);
            delete.navigation = new Navigation { mode = Navigation.Mode.None };

            var button = root.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            // Se agranda y se pinta de plátano clarito al seleccionarla, como los botones del menú.
            var feedback = root.AddComponent<MenuButtonFeedback>();
            MenuScenesBuilder.SetField(feedback, "face", faceRect);
            MenuScenesBuilder.SetField(feedback, "fill", fill);
            PlayerSandboxBuilder.SetSerialized(feedback, "normalColor", MenuScenesBuilder.Crema);
            PlayerSandboxBuilder.SetSerialized(feedback, "selectedColor", new Color(0.988f, 0.894f, 0.659f));
            PlayerSandboxBuilder.SetSerialized(feedback, "pressedColor", new Color(0.976f, 0.800f, 0.396f));
            PlayerSandboxBuilder.SetSerialized(feedback, "selectedScale", 1.04f);
            root.AddComponent<SelectableSounds>();
            return (button, summary, dots, delete);
        }

        // Abajo al centro: [Aceptar] Jugar  [Atrás] Volver  [Q / Y / Triángulo] Borrar.
        private static void CreatePromptBar(RectTransform canvas, TMP_FontAsset font)
        {
            var go = new GameObject("Ayuda de botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.anchoredPosition = new Vector2(0f, -440f);
            rect.sizeDelta = new Vector2(900f, 60f);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            Image confirm = MenuScenesBuilder.CreatePromptIcon(rect);
            Light(MenuScenesBuilder.CreatePromptLabel(rect, font, "Jugar"));
            MenuScenesBuilder.CreatePromptSpacer(rect);
            Image back = MenuScenesBuilder.CreatePromptIcon(rect);
            Light(MenuScenesBuilder.CreatePromptLabel(rect, font, "Volver"));
            MenuScenesBuilder.CreatePromptSpacer(rect);
            Image delete = MenuScenesBuilder.CreatePromptIcon(rect);
            Light(MenuScenesBuilder.CreatePromptLabel(rect, font, "Borrar"));
            foreach (Image icon in new[] { confirm, back, delete })
                icon.color = MenuScenesBuilder.Crema;

            var bar = go.AddComponent<ButtonPromptBar>();
            MenuScenesBuilder.SetField(bar, "confirmIcon", confirm);
            MenuScenesBuilder.SetField(bar, "backIcon", back);
            MenuScenesBuilder.SetField(bar, "pageLeftIcon", delete);
            MenuScenesBuilder.SetPromptSet(bar, "keyboard", "kb_enter", "kb_esc", "kb_q", "kb_e");
            MenuScenesBuilder.SetPromptSet(bar, "xbox", "xbox_a", "xbox_b", "xbox_y", "xbox_rb");
            MenuScenesBuilder.SetPromptSet(bar, "playStation", "ps_cross", "ps_circle", "ps_triangle", "ps_r1");
        }

        private static void Light(TMP_Text label)
        {
            label.color = MenuScenesBuilder.Crema;
            label.fontSize = 32;
        }
    }
}
