using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Odisea.Core;
using UnityEditor;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Efectos de sonido del juego: carpetas por personaje, nombres esperados y carga de variantes
    /// (golpe_rodillo, golpe_rodillo_1, golpe_rodillo_2...). Los generadores de escenas los asignan con
    /// <see cref="SetSound"/>; un sonido que todavía no existe queda vacío y no suena.
    /// </summary>
    public static class GameSoundsBuilder
    {
        public const string UlisesFolder = "Assets/_Project/Audio/SFX/Ulises";
        public const string CrocodileFolder = "Assets/_Project/Audio/SFX/Cocodrilo";
        public const string ItemsFolder = "Assets/_Project/Audio/SFX/Items";
        /// <summary>Madame Circe: circe_risa (su carcajada), caja_golpe (la caja se rompe) y resbalon (la cáscara).</summary>
        public const string CirceFolder = "Assets/_Project/Audio/SFX/Circe";
        private static readonly string[] Extensions = { ".wav", ".ogg", ".mp3" };

        private const string UlisesNotes =
            "Efectos de sonido de Ulises. Nombres (wav, ogg o mp3; variantes con _1, _2...: golpe_rodillo_1.wav):\n" +
            "  golpe_rodillo   al golpear con el rodillo (el movimiento)\n" +
            "  golpe_acierta   cuando el rodillo le da a un enemigo (pon 2 o más: golpe_acierta_1, _2...; no se repite seguido)\n" +
            "  golpe_remate    el golpe que derrota al enemigo (si no está, suena golpe_acierta)\n" +
            "  lanzar_masa     al lanzar la bolita de masa\n" +
            "  masa_impacto    cuando la bolita choca\n" +
            "  salto           al saltar\n" +
            "  doble_salto     la vuelta en el aire (si no está, suena salto)\n" +
            "  aterrizar       al caer de un salto (no en escalones)\n" +
            "  paso            cada pisada al correr (bajito; pon 2 o 3: paso_1, paso_2...)\n" +
            "  dano            al recibir daño\n" +
            "  muerte          al morir\n" +
            "Después: Odisea > Sandbox > Crear escena de prueba del jugador.\n";

        private const string CrocodileNotes =
            "Efectos de sonido del cocodrilo. Nombres (wav, ogg o mp3; variantes con _1, _2...):\n" +
            "  mordida         al cerrar el hocico (justo cuando hace daño)\n" +
            "  mordida_abre    opcional: al empezar la mordida\n" +
            "  dano            al recibir daño\n" +
            "  muerte          al morir\n" +
            "Después: Odisea > Sandbox > Crear escena de prueba del jugador.\n";

        private const string ItemsNotes =
            "Efectos de sonido de los ítems. Nombres (wav, ogg o mp3; variantes con _1, _2...):\n" +
            "  recoger_pescado al tomar el pescado zarandeado\n" +
            "  recoger_hoja    al tomar la hoja de plátano\n" +
            "  recoger_cafe    al tomar el café de olla (un sorbo)\n" +
            "  recoger_ingrediente al tomar un ingrediente (canela, azúcar, harina, plátano); seguidos suenan más agudo\n" +
            "  recoger         el de cualquier ítem que no tenga el suyo (si tampoco está, suena el brillo del logo)\n" +
            "  romper_escudo   cuando el escudo de hoja de plátano detiene un golpe\n" +
            "Después: Odisea > Niveles > Crear tutorial y Odisea > Sandbox > Crear escena de prueba del jugador.\n";

        /// <summary>Crea las carpetas con una nota de los nombres que se esperan.</summary>
        public static void EnsureFolders()
        {
            WriteNotes(UlisesFolder, UlisesNotes);
            WriteNotes(CrocodileFolder, CrocodileNotes);
            WriteNotes(ItemsFolder, ItemsNotes);
        }

        private static void WriteNotes(string folder, string text)
        {
            Directory.CreateDirectory(folder);
            string path = $"{folder}/LEEME.txt";
            if (!File.Exists(path) || File.ReadAllText(path) != text)
            {
                File.WriteAllText(path, text);
                AssetDatabase.ImportAsset(path);
            }
        }

        /// <summary>El sonido "nombre" y sus variantes "nombre_1", "nombre_2"... de la carpeta (vacío si no hay).</summary>
        public static AudioClip[] Load(string folder, string name)
        {
            if (!Directory.Exists(folder))
                return new AudioClip[0];
            var pattern = new Regex($"^{Regex.Escape(name)}(_\\d+)?$", RegexOptions.IgnoreCase);
            return Directory.GetFiles(folder)
                .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()) &&
                            pattern.IsMatch(Path.GetFileNameWithoutExtension(f)))
                .OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase)
                .Select(f => LoadClip(f.Replace('\\', '/')))
                .Where(c => c != null)
                .ToArray();
        }

        // Cortos y frecuentes: mono y descomprimidos al cargar, para que suenen sin retraso.
        private static AudioClip LoadClip(string path)
        {
            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
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
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>
        /// Crea el objeto de música de una escena con <see cref="SceneMusic"/>. basePath sin extensión
        /// (busca .ogg, .mp3 o .wav). Devuelve null si no existe el archivo.
        /// </summary>
        public static SceneMusic CreateMusic(string name, string basePath)
        {
            AudioClip clip = LoadMusic(basePath);
            if (clip == null)
            {
                Debug.LogWarning($"[Odisea] No está {basePath} (.ogg, .mp3 o .wav); la escena queda sin música.");
                return null;
            }

            var go = new GameObject(name, typeof(AudioSource));
            go.GetComponent<AudioSource>().clip = clip;
            return go.AddComponent<SceneMusic>();
        }

        /// <summary>
        /// Canción por su ruta sin extensión (.ogg, .mp3 o .wav), configurada para música; null si no existe.
        /// </summary>
        public static AudioClip LoadMusic(string basePath)
        {
            string path = new[] { ".ogg", ".mp3", ".wav" }.Select(ext => basePath + ext).FirstOrDefault(File.Exists);
            if (path == null)
                return null;

            // Música larga: se lee del disco mientras suena (Streaming) para no ocupar memoria, en estéreo.
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.Streaming)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>
        /// Qué tan fuerte se oye un clip: el tramo de 50 ms más intenso (RMS). Mejor que el pico, porque casi todos
        /// los archivos vienen normalizados al máximo aunque unos suenen la mitad de fuertes que otros.
        /// </summary>
        public static float Loudness(AudioClip clip)
        {
            if (clip == null || clip.samples == 0 || !clip.LoadAudioData() && clip.loadState != AudioDataLoadState.Loaded)
                return 0f;
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0))
                return 0f;
            int window = Mathf.Max(1, clip.frequency * clip.channels / 20);
            double sum = 0;
            float loudest = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i] * data[i];
                if ((i + 1) % window == 0)
                {
                    loudest = Mathf.Max(loudest, Mathf.Sqrt((float)(sum / window)));
                    sum = 0;
                }
            }
            return loudest;
        }

        /// <summary>Asigna los clips (y el volumen) a un campo SoundEffect de un componente.</summary>
        public static void SetSound(Object target, string field, AudioClip[] clips, float volume = 1f)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty($"{field}.clips")
                ?? throw new System.InvalidOperationException($"{target.GetType().Name} no tiene el sonido '{field}'.");
            list.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            // Las variantes se emparejan con la más bajita: así ninguna se pierde ni sobresale.
            float[] loudness = clips.Select(Loudness).ToArray();
            float quietest = loudness.Length > 0 ? loudness.Min() : 1f;
            SerializedProperty gains = serialized.FindProperty($"{field}.clipVolumes");
            gains.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                gains.GetArrayElementAtIndex(i).floatValue = loudness[i] > 0f ? Mathf.Clamp01(quietest / loudness[i]) : 1f;
            serialized.FindProperty($"{field}.volume").floatValue = volume;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
