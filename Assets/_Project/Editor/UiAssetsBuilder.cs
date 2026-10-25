using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Crea los font assets de TextMeshPro con las tipografías del design system:
    /// Outfit (títulos, botones, HUD) y Rubik (texto de lectura). Rubik queda como fuente por defecto.
    /// Menú: Odisea > UI > Crear font assets.
    /// </summary>
    public static class UiAssetsBuilder
    {
        private const string FontsFolder = "Assets/_Project/Art/UI/Fonts";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        public const string OutfitBold = "Outfit/Outfit-Bold";
        public const string OutfitExtraBold = "Outfit/Outfit-ExtraBold";
        public const string RubikRegular = "Rubik/Rubik-Regular";
        public const string RubikMedium = "Rubik/Rubik-Medium";

        private static readonly string[] AllFonts = { OutfitBold, OutfitExtraBold, RubikRegular, RubikMedium };

        [MenuItem("Odisea/UI/Crear font assets (TextMeshPro)")]
        public static void BuildFontAssets()
        {
            foreach (string font in AllFonts)
                EnsureFontAsset(font);

            SetDefaultFont(LoadFont(RubikRegular));
            AssetDatabase.SaveAssets();
            Debug.Log("[Odisea] Font assets de TextMeshPro listos.");
        }

        /// <summary>Font asset de TextMeshPro para una de las constantes de esta clase (p. ej. <see cref="OutfitBold"/>).</summary>
        public static TMP_FontAsset LoadFont(string font)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath(font));
            return asset != null ? asset : EnsureFontAsset(font);
        }

        private static TMP_FontAsset EnsureFontAsset(string font)
        {
            string assetPath = AssetPath(font);
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            string ttfPath = $"{FontsFolder}/{font}.ttf";
            var source = AssetDatabase.LoadAssetAtPath<Font>(ttfPath)
                ?? throw new InvalidOperationException($"No se encontró la tipografía {ttfPath}.");

            // Dinámico: los caracteres (acentos, ñ, ¡¿) se agregan al atlas conforme se usan.
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            asset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);

            // El material y el atlas tienen que guardarse dentro del asset o se pierden al recargar.
            asset.material.name = $"{asset.name} Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.atlasTextures[0].name = $"{asset.name} Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void SetDefaultFont(TMP_FontAsset font)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning($"[Odisea] No se encontró {TmpSettingsPath}; importa TMP Essential Resources.");
                return;
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string AssetPath(string font) => $"{FontsFolder}/{font} SDF.asset";

        private const string UiSoundsFolder = "Assets/_Project/Audio/SFX/UI";

        /// <summary>
        /// Sonido de UI por nombre ("ui_hover", "ui_click", "ui_back"), con cualquier extensión de audio.
        /// Se configura para sonidos cortos: mono, descomprimido en memoria y precargado (sin retraso al sonar).
        /// </summary>
        public static AudioClip LoadUiSound(string name)
        {
            string path = System.IO.Directory.Exists(UiSoundsFolder)
                ? System.Linq.Enumerable.FirstOrDefault(System.IO.Directory.GetFiles(UiSoundsFolder, name + ".*"),
                    f => !f.EndsWith(".meta"))
                : null;
            if (path == null)
                return null;
            path = path.Replace('\\', '/');

            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (!importer.forceToMono || settings.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                importer.forceToMono = true;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private const string IrisShaderName = "Odisea/UI/Iris";
        // Va en Resources para que ScreenFader lo cargue en tiempo de ejecución y se incluya en el build.
        private const string IrisMaterialPath = "Assets/_Project/Resources/Transitions/Iris.mat";

        [MenuItem("Odisea/UI/Crear material de transición (iris)")]
        public static Material EnsureIrisMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(IrisMaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find(IrisShaderName)
                ?? throw new InvalidOperationException($"No se encontró el shader {IrisShaderName}.");
            string folder = System.IO.Path.GetDirectoryName(IrisMaterialPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Transitions");
            }

            material = new Material(shader) { name = "Iris" };
            AssetDatabase.CreateAsset(material, IrisMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private const string BrightnessShaderName = "Odisea/UI/Brightness";
        // En Resources para que ScreenOverlay lo cargue al arrancar el juego.
        private const string BrightnessMaterialPath = "Assets/_Project/Resources/UI/Brillo.mat";

        /// <summary>Material de la capa de brillo (ajuste Brillo de Opciones).</summary>
        public static Material EnsureBrightnessMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(BrightnessMaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find(BrightnessShaderName)
                ?? throw new InvalidOperationException($"No se encontró el shader {BrightnessShaderName}.");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/UI"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "UI");
            material = new Material(shader) { name = "Brillo" };
            AssetDatabase.CreateAsset(material, BrightnessMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private const string TransitionSettingsPath = "Assets/_Project/Resources/Transitions/TransitionSettings.asset";

        /// <summary>Asset con duraciones, color y curvas del iris, editable en el Inspector.</summary>
        [MenuItem("Odisea/UI/Crear configuración de transición")]
        public static Odisea.Core.TransitionSettings EnsureTransitionSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<Odisea.Core.TransitionSettings>(TransitionSettingsPath);
            if (settings != null)
                return settings;

            EnsureIrisMaterial(); // Crea también la carpeta Resources/Transitions.
            settings = ScriptableObject.CreateInstance<Odisea.Core.TransitionSettings>();
            AssetDatabase.CreateAsset(settings, TransitionSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private const string LumaKeyMaterialPath = "Assets/_Project/Art/UI/Materiales/QuitarNegro.mat";

        /// <summary>Material que vuelve transparente lo negro de un video (hoja del recetario).</summary>
        public static Material EnsureLumaKeyMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(LumaKeyMaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Odisea/UI/LumaKey")
                ?? throw new InvalidOperationException("No se encontró el shader Odisea/UI/LumaKey.");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/UI/Materiales"))
                AssetDatabase.CreateFolder("Assets/_Project/Art/UI", "Materiales");
            material = new Material(shader) { name = "QuitarNegro" };
            AssetDatabase.CreateAsset(material, LumaKeyMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private const string RoundedSpritePath = "Assets/_Project/Art/UI/Redondeado.png";
        private const int RoundedSize = 64;
        private const int RoundedRadius = 24;

        /// <summary>
        /// Rectángulo blanco de esquinas redondeadas, 9-slice. Con Image tipo Sliced sirve para botones,
        /// paneles y "píldoras" de cualquier tamaño; el color se pone en el Image.
        /// </summary>
        public static Sprite EnsureRoundedSprite()
        {
            if (!System.IO.File.Exists(RoundedSpritePath))
            {
                var texture = new Texture2D(RoundedSize, RoundedSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < RoundedSize; y++)
                {
                    for (int x = 0; x < RoundedSize; x++)
                    {
                        // Distancia al centro de la esquina más cercana; suavizado de 1 px en el borde.
                        float cx = Mathf.Clamp(x + 0.5f, RoundedRadius, RoundedSize - RoundedRadius);
                        float cy = Mathf.Clamp(y + 0.5f, RoundedRadius, RoundedSize - RoundedRadius);
                        float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        float alpha = Mathf.Clamp01(RoundedRadius - distance + 0.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                System.IO.File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(RoundedSpritePath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(RoundedSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(RoundedRadius, RoundedRadius, RoundedRadius, RoundedRadius);
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        }
    }
}
