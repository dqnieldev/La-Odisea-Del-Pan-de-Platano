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
        // Tamaño de diseño de una viñeta (4:3); el visor la agranda o achica para llenar la página.
        private static readonly Vector2 PanelSize = new(560f, 420f);

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

            // Página de papel crema con marco café, como el recetario.
            var canvasRect = (RectTransform)canvas;
            RectTransform paper = MenuScenesBuilder.CreateCard("Página", canvasRect, UiAssetsBuilder.EnsureRoundedSprite(),
                new Vector2(1840f, 1010f), MenuScenesBuilder.Crema);
            TMP_Text title = MenuScenesBuilder.CreateText("Título", paper, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold),
                56, MenuScenesBuilder.Cafe, position: new Vector2(0f, 445f), rectSize: new Vector2(1700f, 80f));

            var page = new GameObject("Viñetas", typeof(RectTransform));
            var pageRect = (RectTransform)page.transform;
            pageRect.SetParent(paper, false);
            pageRect.anchoredPosition = new Vector2(0f, -10f);
            pageRect.sizeDelta = new Vector2(1720f, 800f);

            ComicPanelView template = CreatePanelTemplate(pageRect);
            TMP_Text nextLabel = CreatePromptBar(paper);

            var viewer = new GameObject("Visor de historietas").AddComponent<ComicViewer>();
            PlayerSandboxBuilder.SetSerialized(viewer, "stories", stories.Cast<Object>().ToArray());
            MenuScenesBuilder.SetField(viewer, "fallback", prologue);
            MenuScenesBuilder.SetField(viewer, "title", title);
            MenuScenesBuilder.SetField(viewer, "page", pageRect);
            MenuScenesBuilder.SetField(viewer, "template", template);
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

        // Marco café, el dibujo (o la descripción mientras no hay dibujo) y el texto en un cartucho crema abajo.
        private static ComicPanelView CreatePanelTemplate(RectTransform page)
        {
            var root = new GameObject("Viñeta (plantilla)", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)root.transform;
            rect.SetParent(page, false);
            rect.sizeDelta = PanelSize;

            Image shadow = MenuScenesBuilder.CreateImage("Sombra", rect, null, new Color(0f, 0f, 0f, 0.25f), Vector2.zero,
                Vector2.zero, stretch: true);
            shadow.rectTransform.offsetMin = shadow.rectTransform.offsetMax = new Vector2(8f, -10f);
            MenuScenesBuilder.CreateImage("Marco", rect, null, MenuScenesBuilder.Cafe, Vector2.zero, Vector2.zero, stretch: true);
            Image art = MenuScenesBuilder.CreateImage("Dibujo", rect, null, Color.white, Vector2.zero, Vector2.zero, stretch: true, inset: 7f);

            TMP_Text note = MenuScenesBuilder.CreateText("Descripción (sin dibujo)", rect, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium),
                26, MenuScenesBuilder.TintaClara, stretch: true);
            ((RectTransform)note.transform).offsetMin = new Vector2(40f, 120f);
            ((RectTransform)note.transform).offsetMax = new Vector2(-40f, -30f);
            note.fontStyle = FontStyles.Italic;

            // Cartucho del texto: ocupa la franja de abajo del dibujo.
            var box = new GameObject("Cartucho", typeof(RectTransform));
            var boxRect = (RectTransform)box.transform;
            boxRect.SetParent(rect, false);
            boxRect.anchorMin = new Vector2(0f, 0f);
            boxRect.anchorMax = new Vector2(1f, 0.27f);
            boxRect.offsetMin = new Vector2(7f, 7f);
            boxRect.offsetMax = new Vector2(-7f, 0f);
            MenuScenesBuilder.CreateImage("Fondo", boxRect, null, new Color(MenuScenesBuilder.Crema.r, MenuScenesBuilder.Crema.g,
                MenuScenesBuilder.Crema.b, 0.94f), Vector2.zero, Vector2.zero, stretch: true);
            TMP_Text caption = MenuScenesBuilder.CreateText("Texto", boxRect, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium),
                24, MenuScenesBuilder.Cafe, stretch: true);
            var captionRect = (RectTransform)caption.transform;
            captionRect.offsetMin = new Vector2(16f, 6f);
            captionRect.offsetMax = new Vector2(-16f, -6f);
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 14f;
            caption.fontSizeMax = 26f;

            var view = root.AddComponent<ComicPanelView>();
            MenuScenesBuilder.SetField(view, "group", root.GetComponent<CanvasGroup>());
            MenuScenesBuilder.SetField(view, "art", art);
            MenuScenesBuilder.SetField(view, "placeholder", note);
            MenuScenesBuilder.SetField(view, "caption", caption);
            return view;
        }

        // Abajo a la derecha: [Aceptar] Siguiente  [Atrás] Saltar.
        private static TMP_Text CreatePromptBar(RectTransform paper)
        {
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            var go = new GameObject("Ayuda de botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(paper, false);
            rect.anchoredPosition = new Vector2(560f, -458f);
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
