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
        // Libro abierto (como el de Cuphead): en la página grande, el título arriba, la viñeta y el texto abajo.
        // Las posiciones son para un dibujo 16:9 con la página grande de ~22 % a ~91 % del ancho.
        private const string BookPath = "Assets/_Project/Art/Comics/Libro.png";
        // Canción de fondo del prólogo (sin extensión).
        private const string PrologueMusicPath = "Assets/_Project/Audio/Music/musica_prologo";
        private static readonly Vector2 PanelSize = new(1440f, 810f);
        private const float PanelOffsetY = 75f;
        private static readonly Vector2 BookPanelCenter = new(125f, 62f);
        private static readonly Vector2 BookPanelSize = new(1000f, 563f);
        private static readonly Color BookTitleInk = new(0.30f, 0.20f, 0.13f);
        private static readonly Color OldPaper = new(0.910f, 0.851f, 0.722f);
        private static readonly Color OldPaperLight = new(0.945f, 0.898f, 0.788f);
        private static readonly Color OldPaperEdge = new(0.749f, 0.635f, 0.459f);
        private static readonly Color Ink = new(0.12f, 0.09f, 0.07f);

        private static readonly string[] PrologueTitles =
        {
            "El Decreto del Viejo Horno", "Un Panadero Terco", "Sin Atajos", "Madame Circe",
            "Lo que Está en Juego", "La Ruta", "Juego Sucio", "Antes de Partir",
        };

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
            ("Circe se marcha y deja su tarjeta sobre la mesa. Don Lupe la mira en silencio: no es solo un concurso, " +
             "es la panadería entera.",
             "Don Lupe sosteniendo la tarjeta dorada del consorcio; por la ventana, anuncios de hoteles y franquicias sobre el puerto."),
            ("Esa noche, a la luz de una vela, Ulises traza su ruta: el Mercado, el Horno del pueblo y los manglares de La Tovara.",
             "Ulises de noche sobre un mapa viejo de San Blas, marcando con lápiz el Mercado, el Horno y La Tovara."),
            ("Mientras tanto, en su cocina de acero, Circe no piensa jugar limpio: sus ayudantes ya se adelantan por la costa.",
             "Cocina industrial cromada; Circe sonriendo y señalando un mapa mientras sus ayudantes salen por la puerta."),
            ("Al amanecer, Don Lupe lo detiene en la puerta: «Antes de irte, muchacho, demuéstrame que no se te olvidó lo básico.»",
             "Amanecer en la puerta de la panadería; Don Lupe le ofrece el rodillo a Ulises, el puerto de fondo."),
        };

        // ---------- Acto I: el Mercado ----------
        // La receta de verdad va en los textos: lo que se junta en el Mercado (plátanos muy maduros, harina, azúcar de
        // caña, canela) y lo que ya hay en la panadería (huevos, mantequilla, bicarbonato, vainilla, sal).

        private static readonly string[] MercadoTitles =
        {
            "El Mercado de San Blas", "La Receta de Don Lupe", "Lo que Hay en la Alacena", "Los Puestos",
            "Los Plátanos de la Cima", "Una Risa en las Vigas",
        };

        private static readonly (string caption, string note)[] MercadoIntro =
        {
            ("Amanece en San Blas. Ulises llega al Mercado Municipal con su morral vacío y la receta de Don Lupe en la cabeza.",
             "Entrada del mercado al amanecer: toldos, papel picado y puestos; Ulises con el morral vacío, mirando hacia arriba."),
            ("«Tres plátanos bien maduros, con pecas negras, que se machaquen solitos», le dijo Don Lupe. «Nada de plátanos verdes.»",
             "Recuerdo en la panadería: Don Lupe muestra tres plátanos con pecas y rechaza uno verde; Ulises escucha."),
            ("En la panadería ya esperan dos huevos, media taza de mantequilla, una cucharadita de bicarbonato, un chorrito " +
             "de vainilla y una pizca de sal. Lo demás hay que conseguirlo aquí.",
             "Ulises en la alacena: huevos, mantequilla, bicarbonato, vainilla y sal; al lado, frascos vacíos y una canasta."),
            ("Dos tazas de harina, tres cuartos de taza de azúcar de caña y una cucharadita de canela… y los plátanos más " +
             "dulces de la costa. Todo está en algún rincón del Mercado.",
             "Pasillos del mercado: costales de harina, piloncillo y caña, canela en rama y racimos maduros; Ulises huele la canela."),
            ("Pero los plátanos que pide la receta no están en un puesto: un racimo dorado cuelga en lo más alto, bajo el " +
             "tragaluz. Para llegar hay que escalar.",
             "Desde abajo: las torres de repisas y escaleras suben hasta el tragaluz; arriba brilla el racimo dorado."),
            ("Desde las vigas, una risa conocida: Madame Circe se columpia sobre el hueco, rodeada de cajas y cáscaras de " +
             "plátano. Sus ayudantes ya patrullan las repisas.",
             "Circe en su columpio entre las vigas, riéndose, con cajas y cáscaras; abajo sus dos ayudantes; Ulises de espaldas."),
        };

        private static readonly string[] MercadoOutroTitles = { "El Racimo Dorado", "Circe Escapa", "Todo Listo en la Mesa" };

        private static readonly (string caption, string note)[] MercadoOutro =
        {
            ("En la cima, Ulises arranca el racimo de plátanos más maduro y dulce de toda la costa. Con eso, el morral queda completo.",
             "Ulises bajo el tragaluz levantando el racimo dorado; del morral asoman la harina, el azúcar y la canela."),
            ("Afuera, Madame Circe se sube a su camioneta y huye entre bocinazos: «¡Esto no se ha terminado, panadero!»",
             "Circe huye en su camioneta azul con sus ayudantes colgados atrás; Ulises se despide con el racimo en la mano."),
            ("De vuelta en el taller, todo cabe en la mesa: plátanos, harina, azúcar de caña, canela, huevos, mantequilla, " +
             "bicarbonato, vainilla y sal. Don Lupe sonríe: «Ahora viene lo difícil: hornearlo.»",
             "La mesa de la panadería con todos los ingredientes en fila; Ulises los presenta y Don Lupe sonríe de brazos cruzados."),
        };

        [MenuItem("Odisea/Menús/Crear escena de historietas")]
        public static void Build()
        {
            // Primero la escena: abrirla descarga los assets recién creados y sus referencias quedarían vacías.
            var scene = MenuScenesBuilder.NewScene(ScenePath);
            ComicSequence prologue = EnsureStory(SceneFlow.PrologueStory, "Prólogo", Prologue, PrologueTitles);
            // Canción del prólogo (si diseño no le puso otra).
            if (prologue.music == null)
            {
                prologue.music = GameSoundsBuilder.LoadMusic(PrologueMusicPath);
                EditorUtility.SetDirty(prologue);
            }
            // Las del Mercado van con la misma canción de cuento (si diseño no les puso otra).
            foreach (ComicSequence act in new[]
                     {
                         EnsureStory(SceneFlow.MercadoStory, "Acto I · El Mercado", MercadoIntro, MercadoTitles),
                         EnsureStory(SceneFlow.MercadoOutroStory, "Acto I · El racimo dorado", MercadoOutro, MercadoOutroTitles),
                     })
            {
                if (act.music != null)
                    continue;
                act.music = GameSoundsBuilder.LoadMusic(PrologueMusicPath);
                EditorUtility.SetDirty(act);
            }
            AssetDatabase.SaveAssets();
            ComicSequence[] stories = AssetDatabase.FindAssets("t:ComicSequence", new[] { StoriesFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ComicSequence>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

            MenuScenesBuilder.CreateCamera(MenuScenesBuilder.Cafe);
            Transform canvas = MenuScenesBuilder.CreateCanvas("Historieta");
            MenuScenesBuilder.CreateUiAudio();
            // Música sin canción: el visor pone la de la historieta (las que no traen, van en silencio).
            var musicObject = new GameObject("Música de la historieta", typeof(AudioSource));
            var storyMusic = musicObject.AddComponent<SceneMusic>();

            Sprite book = File.Exists(BookPath) ? MenuScenesBuilder.LoadUiSprite(BookPath) : null;
            RectTransform paper;
            RectTransform panel;
            Image art;
            TMP_Text note, caption, counter = null, title = null, nextLabel;
            if (book != null)
            {
                // El libro llena la pantalla; todo va en la página grande, como una página de cuento.
                paper = (RectTransform)canvas;
                MenuScenesBuilder.CreateImage("Libro", paper, book, Color.white, Vector2.zero, Vector2.zero, stretch: true);
                (panel, art, note, caption) = CreatePanel(paper, BookPanelCenter, BookPanelSize, 38,
                    UiAssetsBuilder.LoadFont(UiAssetsBuilder.ImFellEnglish), storybook: true);
                // El título va dentro de la viñeta para que entre y salga junto con ella.
                title = MenuScenesBuilder.CreateText("Título", panel, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RyeRegular), 62,
                    BookTitleInk, position: new Vector2(0f, BookPanelCenter.y + BookPanelSize.y / 2f + 62f),
                    rectSize: new Vector2(BookPanelSize.x + 80f, 90f));
                nextLabel = CreatePromptBar(paper, new Vector2(450f, -482f));
            }
            else
            {
                paper = CreatePaper((RectTransform)canvas);
                (panel, art, note, caption) = CreatePanel(paper, new Vector2(0f, PanelOffsetY), PanelSize, 38,
                    UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium));
                counter = MenuScenesBuilder.CreateText("Contador", paper, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 28,
                    MenuScenesBuilder.TintaClara, position: new Vector2(-560f, -478f), rectSize: new Vector2(640f, 50f),
                    alignment: TextAlignmentOptions.MidlineLeft);
                nextLabel = CreatePromptBar(paper, new Vector2(560f, -478f));
            }

            var viewer = new GameObject("Visor de historietas").AddComponent<ComicViewer>();
            PlayerSandboxBuilder.SetSerialized(viewer, "stories", stories.Cast<Object>().ToArray());
            MenuScenesBuilder.SetField(viewer, "fallback", prologue);
            MenuScenesBuilder.SetField(viewer, "music", storyMusic);
            MenuScenesBuilder.SetField(viewer, "panel", panel);
            MenuScenesBuilder.SetField(viewer, "panelGroup", panel.GetComponent<CanvasGroup>());
            MenuScenesBuilder.SetField(viewer, "art", art);
            MenuScenesBuilder.SetField(viewer, "placeholder", note);
            MenuScenesBuilder.SetField(viewer, "caption", caption);
            if (counter != null)
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
        /// Si ya existe, conserva sus textos (diseño los puede corregir en el Inspector) y solo agrega las viñetas nuevas
        /// que falten al final.
        /// </summary>
        private static ComicSequence EnsureStory(string name, string title, (string caption, string note)[] panels,
            string[] panelTitles = null)
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
            else if (story.panels.Length < panels.Length)
            {
                story.panels = story.panels.Concat(panels.Skip(story.panels.Length)
                    .Select(p => new ComicSequence.Panel { caption = p.caption, placeholderNote = p.note })).ToArray();
            }
            for (int i = 0; i < story.panels.Length; i++)
            {
                // Solo llena los títulos vacíos: los que diseño haya cambiado se quedan.
                if (panelTitles != null && i < panelTitles.Length && string.IsNullOrEmpty(story.panels[i].title))
                    story.panels[i].title = panelTitles[i];
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
        // En el libro (storybook) el marco es de papel claro con una línea fina, como una estampa pegada en la hoja.
        private static (RectTransform, Image, TMP_Text, TMP_Text) CreatePanel(RectTransform paper, Vector2 center, Vector2 panelSize,
            float captionSize, TMP_FontAsset captionFont, bool storybook = false)
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
            Image art;
            if (storybook)
            {
                shadow.color = new Color(0.25f, 0.16f, 0.08f, 0.25f);
                MenuScenesBuilder.CreateImage("Borde", frameRect, null, OldPaperEdge, Vector2.zero, Vector2.zero, stretch: true);
                MenuScenesBuilder.CreateImage("Paspartú", frameRect, null, OldPaperLight, Vector2.zero, Vector2.zero, stretch: true, inset: 3f);
                MenuScenesBuilder.CreateImage("Línea", frameRect, null, OldPaperEdge, Vector2.zero, Vector2.zero, stretch: true, inset: 15f);
                art = MenuScenesBuilder.CreateImage("Dibujo", frameRect, null, Color.white, Vector2.zero, Vector2.zero, stretch: true, inset: 17f);
            }
            else
            {
                MenuScenesBuilder.CreateImage("Borde", frameRect, null, Ink, Vector2.zero, Vector2.zero, stretch: true);
                art = MenuScenesBuilder.CreateImage("Dibujo", frameRect, null, Color.white, Vector2.zero, Vector2.zero, stretch: true, inset: 10f);
            }
            TMP_Text note = MenuScenesBuilder.CreateText("Descripción (sin dibujo)", frameRect, "",
                UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 40, Ink, stretch: true);
            ((RectTransform)note.transform).offsetMin = new Vector2(120f, 80f);
            ((RectTransform)note.transform).offsetMax = new Vector2(-120f, -80f);
            note.fontStyle = FontStyles.Italic;

            TMP_Text caption = MenuScenesBuilder.CreateText("Texto", rect, "", captionFont, captionSize,
                Ink, position: new Vector2(0f, center.y - panelSize.y / 2f - (storybook ? 92f : 72f)),
                rectSize: new Vector2(storybook ? panelSize.x + 60f : panelSize.x, storybook ? 150f : 110f),
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
