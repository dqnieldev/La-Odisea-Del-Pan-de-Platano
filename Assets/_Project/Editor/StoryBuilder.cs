using System.IO;
using System.Linq;
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
    /// Escena de historietas (Historia) y las historietas del juego. Cada historieta es un asset en
    /// ScriptableObjects/Comics; sus dibujos se ponen en Art/Comics/&lt;Nombre&gt;/&lt;Nombre&gt;_1.png, _2.png...
    /// y se asignan al volver a generar. Los textos se pueden corregir en el asset (no se sobrescriben).
    /// </summary>
    public static class StoryBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Menus/Historia.unity";
        private const string StoriesFolder = "Assets/_Project/ScriptableObjects/Comics";
        private const string ArtFolder = "Assets/_Project/Art/Comics";
        // Papel viejo opcional (toda la hoja) y la viñeta 16:9 grande, un poco arriba para dejar lugar al texto.
        private const string PaperPath = "Assets/_Project/Art/Comics/Papel.png";
        // Libro abierto (como el de Cuphead): la viñeta en la página derecha; título y número en la izquierda.
        // Las posiciones son para un dibujo 16:9 con la página derecha de ~22 % a ~91 % del ancho.
        private const string BookPath = "Assets/_Project/Art/Comics/Libro.png";
        private static readonly Vector2 PanelSize = new(1440f, 810f);
        private const float PanelOffsetY = 75f;
        private static readonly Vector2 BookPanelCenter = new(125f, 75f);
        private static readonly Vector2 BookPanelSize = new(1120f, 630f);
        private const float BookLeftPageX = -765f;
        private static readonly Color OldPaper = new(0.910f, 0.851f, 0.722f);
        private static readonly Color OldPaperLight = new(0.945f, 0.898f, 0.788f);
        private static readonly Color OldPaperEdge = new(0.749f, 0.635f, 0.459f);
        private static readonly Color Ink = new(0.12f, 0.09f, 0.07f);

        private static readonly (string caption, string note)[] Prologue =
        {
            ("Don Lupe reúne a sus panaderos: «Quien hornee el pan de plátano definitivo y lo lleve al Festival " +
             "será Empleado del Mes… y guardián de la pala de oro.»",
             "Don Lupe, cansado, frente al viejo horno de piedra de su panadería en San Blas, hablándoles a sus panaderos."),
            ("Ulises, tan hábil con la masa como terco, se ata el delantal: esa receta será suya.",
             "Ulises atándose el delantal gastado, decidido, con el rodillo a la mano. Don Lupe lo mira de reojo."),
            ("Pero no hay atajos de supermercado: plátano macho real, azúcar de caña, canela en rama y la mejor harina.",
             "Ulises imaginando los ingredientes a su alrededor: racimo de plátano macho, caña de azúcar, canela, costal de harina."),
            ("Entonces aparece Madame Circe, la chef del consorcio rival: «La costa no tiene espacio para recetas humildes.»",
             "Madame Circe, elegante y burlona, con su batidora cromada, frente a Ulises que frunce el ceño."),
        };

        [MenuItem("Odisea/Menús/Crear escena de historietas")]
        public static void Build()
        {
            // Primero la escena: abrirla descarga los assets recién creados y sus referencias quedarían vacías.
            var scene = MenuScenesBuilder.NewScene(ScenePath);
            ComicSequence prologue = EnsureStory(SceneFlow.PrologueStory, "Prólogo: El Decreto del Viejo Horno", Prologue);
            AssetDatabase.SaveAssets();
            ComicSequence[] stories = AssetDatabase.FindAssets("t:ComicSequence", new[] { StoriesFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ComicSequence>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

            MenuScenesBuilder.CreateCamera(MenuScenesBuilder.Cafe);
            Transform canvas = MenuScenesBuilder.CreateCanvas("Historieta");
            MenuScenesBuilder.CreateUiAudio();
            MenuScenesBuilder.CreateMenuMusic();

            Sprite book = File.Exists(BookPath) ? MenuScenesBuilder.LoadUiSprite(BookPath) : null;
            RectTransform paper;
            RectTransform panel;
            Image art;
            TMP_Text note, caption, counter, title = null, nextLabel;
            if (book != null)
            {
                // El libro llena la pantalla; la página izquierda lleva el título y el número de viñeta.
                paper = (RectTransform)canvas;
                MenuScenesBuilder.CreateImage("Libro", paper, book, Color.white, Vector2.zero, Vector2.zero, stretch: true);
                (panel, art, note, caption) = CreatePanel(paper, BookPanelCenter, BookPanelSize, 34);
                title = MenuScenesBuilder.CreateText("Título", paper, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold), 46,
                    Ink, position: new Vector2(BookLeftPageX, 150f), rectSize: new Vector2(300f, 460f));
                counter = MenuScenesBuilder.CreateText("Contador", paper, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 40,
                    MenuScenesBuilder.TintaClara, position: new Vector2(BookLeftPageX, -330f), rectSize: new Vector2(300f, 60f));
                nextLabel = CreatePromptBar(paper, new Vector2(560f, -470f));
            }
            else
            {
                paper = CreatePaper((RectTransform)canvas);
                (panel, art, note, caption) = CreatePanel(paper, new Vector2(0f, PanelOffsetY), PanelSize, 38);
                counter = MenuScenesBuilder.CreateText("Contador", paper, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 28,
                    MenuScenesBuilder.TintaClara, position: new Vector2(-560f, -478f), rectSize: new Vector2(640f, 50f),
                    alignment: TextAlignmentOptions.MidlineLeft);
                nextLabel = CreatePromptBar(paper, new Vector2(560f, -478f));
            }

            var viewer = new GameObject("Visor de historietas").AddComponent<ComicViewer>();
            PlayerSandboxBuilder.SetSerialized(viewer, "stories", stories.Cast<Object>().ToArray());
            MenuScenesBuilder.SetField(viewer, "fallback", prologue);
            MenuScenesBuilder.SetField(viewer, "panel", panel);
            MenuScenesBuilder.SetField(viewer, "panelGroup", panel.GetComponent<CanvasGroup>());
            MenuScenesBuilder.SetField(viewer, "art", art);
            MenuScenesBuilder.SetField(viewer, "placeholder", note);
            MenuScenesBuilder.SetField(viewer, "caption", caption);
            MenuScenesBuilder.SetField(viewer, "counter", counter);
            if (title != null)
                MenuScenesBuilder.SetField(viewer, "title", title);
            MenuScenesBuilder.SetField(viewer, "nextLabel", nextLabel);

            EditorSceneManager.SaveScene(scene);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Escena de historietas creada en {ScenePath} ({stories.Length} historietas).");
        }

        /// <summary>
        /// Crea la historieta si no existe (con sus textos) y le asigna los dibujos que haya en Art/Comics/&lt;nombre&gt;.
        /// Si ya existe, conserva sus textos: diseño los puede corregir en el Inspector.
        /// </summary>
        private static ComicSequence EnsureStory(string name, string title, (string caption, string note)[] panels)
        {
            PlayerSandboxBuilder.EnsureFolder(StoriesFolder);
            string path = $"{StoriesFolder}/{name}.asset";
            var story = AssetDatabase.LoadAssetAtPath<ComicSequence>(path);
            if (story == null)
            {
                story = ScriptableObject.CreateInstance<ComicSequence>();
                story.title = title;
                story.panels = panels.Select(p => new ComicSequence.Panel { caption = p.caption, placeholderNote = p.note }).ToArray();
                AssetDatabase.CreateAsset(story, path);
            }
            for (int i = 0; i < story.panels.Length; i++)
            {
                Sprite art = LoadPanelArt(name, i + 1);
                if (art != null)
                    story.panels[i].art = art;
            }
            EditorUtility.SetDirty(story);
            return story;
        }

        private static Sprite LoadPanelArt(string story, int number)
        {
            string folder = $"{ArtFolder}/{story}";
            if (!Directory.Exists(folder))
                return null;
            string file = Directory.GetFiles(folder).FirstOrDefault(f =>
                string.Equals(Path.GetFileNameWithoutExtension(f), $"{story}_{number}", System.StringComparison.OrdinalIgnoreCase) &&
                new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(f).ToLowerInvariant()));
            return file != null ? MenuScenesBuilder.LoadUiSprite(file.Replace('\\', '/')) : null;
        }

        // Hoja de papel viejo que llena la pantalla: con Art/Comics/Papel.png si existe; si no, papel amarillento con
        // la orilla más oscura, como quemada por el tiempo.
        private static RectTransform CreatePaper(RectTransform canvas)
        {
            var paper = new GameObject("Papel", typeof(RectTransform));
            var rect = (RectTransform)paper.transform;
            rect.SetParent(canvas, false);
            rect.sizeDelta = new Vector2(1880f, 1040f);
            Sprite texture = AssetDatabase.LoadAssetAtPath<Sprite>(PaperPath) ??
                             (File.Exists(PaperPath) ? MenuScenesBuilder.LoadUiSprite(PaperPath) : null);
            if (texture != null)
            {
                MenuScenesBuilder.CreateImage("Textura", rect, texture, Color.white, Vector2.zero, Vector2.zero, stretch: true);
                return rect;
            }
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            MenuScenesBuilder.CreateImage("Orilla", rect, rounded, OldPaperEdge, Vector2.zero, Vector2.zero, stretch: true);
            MenuScenesBuilder.CreateImage("Hoja", rect, rounded, OldPaper, Vector2.zero, Vector2.zero, stretch: true, inset: 22f);
            MenuScenesBuilder.CreateImage("Centro", rect, rounded, OldPaperLight, Vector2.zero, Vector2.zero, stretch: true, inset: 70f);
            return rect;
        }

        // La viñeta grande (16:9) con marco negro y su texto debajo; todo junto entra y sale.
        private static (RectTransform, Image, TMP_Text, TMP_Text) CreatePanel(RectTransform paper, Vector2 center, Vector2 panelSize,
            float captionSize)
        {
            var root = new GameObject("Viñeta", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)root.transform;
            rect.SetParent(paper, false);
            rect.sizeDelta = panelSize;
            rect.anchoredPosition = new Vector2(center.x, 0f);
            // El grupo se mueve desde el centro del dibujo (que está más arriba que el centro de la hoja).
            rect.pivot = new Vector2(0.5f, 0.5f);

            var frame = new GameObject("Marco", typeof(RectTransform));
            var frameRect = (RectTransform)frame.transform;
            frameRect.SetParent(rect, false);
            frameRect.anchoredPosition = new Vector2(0f, center.y);
            frameRect.sizeDelta = panelSize;
            Image shadow = MenuScenesBuilder.CreateImage("Sombra", frameRect, null, new Color(0f, 0f, 0f, 0.3f), Vector2.zero,
                Vector2.zero, stretch: true);
            shadow.rectTransform.offsetMin = shadow.rectTransform.offsetMax = new Vector2(10f, -12f);
            MenuScenesBuilder.CreateImage("Borde", frameRect, null, Ink, Vector2.zero, Vector2.zero, stretch: true);
            Image art = MenuScenesBuilder.CreateImage("Dibujo", frameRect, null, Color.white, Vector2.zero, Vector2.zero, stretch: true, inset: 10f);
            TMP_Text note = MenuScenesBuilder.CreateText("Descripción (sin dibujo)", frameRect, "",
                UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 40, Ink, stretch: true);
            ((RectTransform)note.transform).offsetMin = new Vector2(120f, 80f);
            ((RectTransform)note.transform).offsetMax = new Vector2(-120f, -80f);
            note.fontStyle = FontStyles.Italic;

            TMP_Text caption = MenuScenesBuilder.CreateText("Texto", rect, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), captionSize,
                Ink, position: new Vector2(0f, center.y - panelSize.y / 2f - 72f), rectSize: new Vector2(panelSize.x, 110f),
                alignment: TextAlignmentOptions.Top);
            caption.lineSpacing = -8f;
            return (rect, art, note, caption);
        }

        // Abajo a la derecha: [Aceptar] Siguiente  [Atrás] Saltar.
        private static TMP_Text CreatePromptBar(RectTransform paper, Vector2 position)
        {
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            var go = new GameObject("Ayuda de botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(paper, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(560f, 56f);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            Image confirm = MenuScenesBuilder.CreatePromptIcon(rect);
            TMP_Text next = MenuScenesBuilder.CreatePromptLabel(rect, font, "Siguiente");
            MenuScenesBuilder.CreatePromptSpacer(rect);
            Image back = MenuScenesBuilder.CreatePromptIcon(rect);
            MenuScenesBuilder.CreatePromptLabel(rect, font, "Saltar");

            var bar = go.AddComponent<ButtonPromptBar>();
            MenuScenesBuilder.SetField(bar, "confirmIcon", confirm);
            MenuScenesBuilder.SetField(bar, "backIcon", back);
            MenuScenesBuilder.SetPromptSet(bar, "keyboard", "kb_enter", "kb_esc", "kb_q", "kb_e");
            MenuScenesBuilder.SetPromptSet(bar, "xbox", "xbox_a", "xbox_b", "xbox_lb", "xbox_rb");
            MenuScenesBuilder.SetPromptSet(bar, "playStation", "ps_cross", "ps_circle", "ps_l1", "ps_r1");
            return next;
        }
    }
}
