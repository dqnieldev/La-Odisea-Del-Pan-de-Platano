using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Importa el arte de los diseñadores (Docs/Referencias) al juego:
    /// quita el fondo blanco, detecta cada cuadro, lo acomoda en tiras con el pivote en los pies,
    /// las rebana en sprites y arma los clips y el Animator de Ulises y del cocodrilo.
    /// Se puede volver a correr cuando llegue arte nuevo: conserva los IDs de los sprites y los assets.
    /// Menú: Odisea > Arte > Importar arte de la sandbox.
    /// </summary>
    public static class ArtImportBuilder
    {
        private const string SourceFolder = "Docs/Referencias";
        private const string UlisesSheet = "Personajes/sprite en png/ulises/ulises.png";
        private const string UlisesRunSheet = "Personajes/sprite en png/ulises/correrulises.png";
        // Hojas por animación (convención personaje_animacion.png, cuadros en una sola fila).
        // Si alguna no existe se usa el sustituto de ulises.png / correrulises.png.
        private const string UlisesFolderSource = "Personajes/sprite en png/ulises/";
        private const string CrocodileSheet = "Personajes/sprite en png/enemigos/cocodrilo/cocodrilo.png";
        private const string CrocodileWalkSheet = "Personajes/sprite en png/enemigos/cocodrilo/cocodrilo_caminar.png";
        private const string CrocodileBiteSheet = "Personajes/sprite en png/enemigos/cocodrilo/cocodrilo_mordida.png";
        private const string CrocodileHurtSheet = "Personajes/sprite en png/enemigos/cocodrilo/cocodrilo_dano.png";
        private const string CrocodileDeathSheet = "Personajes/sprite en png/enemigos/cocodrilo/cocodrilo_muerte.png";
        private const string Level1Sheet = "Sprites-por-revisar/Niveles/Nivel 1/spritesheets/spritesheet_nivel1.jpg";
        // Tablas, escalera y vigas del tutorial (por ahora, copia de la hoja del nivel 1, con fondo blanco).
        private const string TutorialSheet = "Sprites-por-revisar/Niveles/Tutorial/tutorial_tablas.jpg";
        public const string TutorialFolder = "Assets/_Project/Art/Environment/Tutorial";

        private const string UlisesFolder = "Assets/_Project/Art/Characters/Ulises";
        private const string DoughFolder = "Assets/_Project/Art/VFX/Masa";
        public const string DoughFlyPath = DoughFolder + "/Masa_Vuela.png";
        public const string DoughImpactPath = DoughFolder + "/Masa_Impacto.png";
        private const string CrocodileFolder = "Assets/_Project/Art/Characters/Enemigos/Cocodrilo";
        private const string Level1Folder = "Assets/_Project/Art/Environment/Nivel1";

        public const string UlisesControllerPath = UlisesFolder + "/Ulises.controller";
        // Ficha del mapa: Ulises chibi (ulises_mapa.png), reposo y caminar, sin Animator (lo pasa OverworldMap).
        public const string MapIdlePath = UlisesFolder + "/Ulises_Mapa_Reposo.png";
        public const string MapWalkPath = UlisesFolder + "/Ulises_Mapa_Camina.png";
        // Alto de la ficha en el mapa (unidades), medido en su primer cuadro de reposo.
        private const float MapTokenHeight = 2.4f;
        public const string CrocodileControllerPath = CrocodileFolder + "/Cocodrilo.controller";
        public const string PostPath = Level1Folder + "/Viga.png";
        public const string LadderPath = Level1Folder + "/Escalera.png";
        public const string PlankPath = Level1Folder + "/Tablon.png";
        public const string BrokenPlankPath = Level1Folder + "/TablonRoto1.png";
        public const string BrokenPlank2Path = Level1Folder + "/TablonRoto2.png";
        /// <summary>Tablón roto de los dos lados (se arma con las puntas de los otros dos; ver ComposeBrokenBothEnds).</summary>
        public const string BrokenBothPath = Level1Folder + "/TablonRotoAmbos.png";

        // Tamaños en el juego (unidades). Ulises mide 1.6 de collider; el dibujo corriendo, con pelo, un poco más.
        private const float UlisesRunHeight = 1.8f;
        // Ulises parado. Cada hoja nueva viene a otra escala; se mide su cuadro parado y se lleva a esta altura.
        private const float UlisesStandHeight = 1.95f;
        // En el aire el dibujo se centra en el cuerpo (no en los pies) para que las vueltas no "brinquen":
        // el centro del cuerpo queda a esta altura sobre los pies del collider.
        private const float BodyCenterAboveFeet = 0.95f;
        private const float CrocodileHeight = 0.95f;
        // Ritmo de las patas: 10 fps se veía bien a velocidad 1.5; a 2.6 (la de la sandbox) va en proporción
        // para que no parezca que patina.
        private const float CrocodileWalkFps = 17f;
        // Segundos que se queda boca arriba al final de la muerte, antes de desvanecerse.
        public const float CrocodileDeathHold = 0.5f;
        // Grosor del tablón = grosor de las plataformas de la sandbox.
        private const float PlankHeight = 0.45f;
        // Diámetro de la bolita de masa (su collider mide 0.4).
        private const float DoughBallSize = 0.45f;

        // Fondo blanco: huecos blancos encerrados (entre brazo y cuerpo) de al menos este tamaño también son fondo;
        // los más chicos (blanco de los ojos, dientes) se respetan.
        private const int EnclosedHoleMinArea = 120;
        // Orilla suave: píxeles claros junto al fondo se vuelven semitransparentes en vez de dejar un halo blanco.
        private const int FringeLightness = 150;
        // Borde vacío alrededor de cada dibujo (en píxeles de la hoja); quien acomode piezas lo puede descontar.
        public const int Padding = 4;

        [MenuItem("Odisea/Arte/Importar arte de la sandbox")]
        public static void ImportAll()
        {
            ImportUlises();
            ImportDough();
            ImportCrocodile();
            ImportHenchman(male: true, "secuaz hombre/secuaz_hombre", "ataque_cuchara");
            ImportHenchman(male: false, "secuaz mujer/secuaz_mujer", "ataque_batidor");
            ImportCirce();
            ImportLevelPieces(Level1Sheet, whiteThreshold: 232, Level1Folder, "Nivel 1");
            ImportLevelPieces(TutorialSheet, whiteThreshold: 232, TutorialFolder, "Tutorial");
            ImportTutorialBackground();
            ImportItems();
            ImportIngredients();
            ImportCirceThrowables();
            ImportDonLupe();
            AssetDatabase.SaveAssets();
            Debug.Log("[Odisea] Arte de la sandbox importado.");
        }

        // ---------- Don Lupe ----------

        public const string DonLupeFolder = "Assets/_Project/Art/Characters/DonLupe";
        /// <summary>Tira de Don Lupe en reposo (apoyado en el bastón, respira y parpadea); pivote en los pies.</summary>
        public const string DonLupeIdlePath = DonLupeFolder + "/DonLupe_Reposo.png";
        /// <summary>Tira de Don Lupe hablando (mueve la boca y una mano); pivote en los pies, como la de reposo.</summary>
        public const string DonLupeTalkPath = DonLupeFolder + "/DonLupe_Habla.png";
        private const string DonLupeSource = "Personajes/sprite en png/don lupe/donlupe_reposo.png";
        private const string DonLupeTalkSource = "Personajes/sprite en png/don lupe/donlupe_habla.jfif";
        // Estatura en el juego: un poco más alto que Ulises aunque va encorvado.
        private const float DonLupeHeight = 2.35f;

        /// <summary>
        /// Hojas de una fila con fondo blanco (barba y guantes son blanco hueso, más oscuros que el fondo): reposo y
        /// hablando, cada cuadro con los pies en el mismo punto y a la misma estatura.
        /// </summary>
        [MenuItem("Odisea/Arte/Importar a Don Lupe")]
        public static void ImportDonLupe()
        {
            EnsureFolder(DonLupeFolder);
            ImportDonLupeStrip(DonLupeSource, 248, DonLupeIdlePath, "DonLupe_Reposo");
            // La de hablar es JPG: el fondo trae ruido de compresión, por eso el umbral es un poco más bajo.
            ImportDonLupeStrip(DonLupeTalkSource, 246, DonLupeTalkPath, "DonLupe_Habla");
        }

        private static void ImportDonLupeStrip(string source, int whiteThreshold, string path, string spriteName)
        {
            Sheet sheet = LoadSheet(source, whiteThreshold, warnIfMissing: false);
            if (sheet == null)
                return;
            List<Frame> frames = FindFrames(sheet, coreFraction: 0.3f, attachDistance: 20).SelectMany(r => r)
                .OrderBy(f => f.Core.MinX).ToList();
            float ppu = MedianCoreHeight(frames) / DonLupeHeight;
            WriteStrip(sheet, frames, path, ppu, spriteName);
            Debug.Log($"[Odisea] {spriteName}: {frames.Count} cuadros.");
        }

        // ---------- Ítems ----------

        // Pescado zarandeado (cura), hoja de plátano (escudo) y café de olla (velocidad). Cada uno en su imagen de
        // Docs/Referencias/Sprites-por-revisar/Items, con fondo blanco o transparente; caben en un cuadro de ItemSize.
        public const string ItemsFolder = "Assets/_Project/Art/Items";
        public const string FishPath = ItemsFolder + "/Pescado.png";
        public const string LeafPath = ItemsFolder + "/Hoja.png";
        public const string CoffeePath = ItemsFolder + "/Cafe.png";
        /// <summary>Dibujos de los ingredientes (Canela, Azucar, Harina, Platano .png); mientras no estén, círculos.</summary>
        public const string IngredientsFolder = ItemsFolder + "/Ingredientes";
        private const string ItemsSource = "Sprites-por-revisar/Items/";
        private const float ItemSize = 0.9f;

        private static void ImportItems()
        {
            EnsureFolder(ItemsFolder);
            WriteItem("item_pescado.png", FishPath);
            WriteItem("item_hoja.png", LeafPath);
            WriteItem("item_cafe.png", CoffeePath);
        }

        // Sin la imagen no pasa nada: el juego usa un círculo de color mientras llega el arte.
        private static void WriteItem(string file, string path)
        {
            Sheet sheet = LoadSheet(ItemsSource + file, whiteThreshold: 235, warnIfMissing: false);
            if (sheet == null)
                return;
            RectInt bounds = OpaqueBounds(sheet);
            WriteRegion(sheet, bounds, path, Mathf.Max(bounds.width, bounds.height) / ItemSize, new Vector2(0.5f, 0.5f));
        }

        // ---------- Ingredientes del pan de plátano ----------

        private const string IngredientsSource = "Items/ingredientes.png";
        private static readonly string[] IngredientNames = { "Canela", "Azucar", "Harina", "Platano" };

        /// <summary>
        /// Los 4 ingredientes de una hoja en fila (canela, azúcar, harina, plátano), cada uno a su archivo en
        /// Art/Items/Ingredientes, del mismo tamaño que los demás ítems. El costal de harina es casi blanco: el
        /// umbral del fondo va alto para no borrarlo.
        /// </summary>
        [MenuItem("Odisea/Arte/Importar ingredientes")]
        public static void ImportIngredients()
        {
            Sheet sheet = LoadSheet(IngredientsSource, whiteThreshold: 248, warnIfMissing: false);
            if (sheet == null)
                return;
            List<RectInt> parts = SplitByGaps(sheet, OpaqueBounds(sheet), minGap: 60)
                .Where(part => part.width > 40 && part.height > 40).ToList();
            if (parts.Count != IngredientNames.Length)
            {
                Debug.LogError($"[Odisea] ingredientes.png: se esperaban {IngredientNames.Length} dibujos y hay {parts.Count}.");
                return;
            }
            EnsureFolder(IngredientsFolder);
            for (int i = 0; i < parts.Count; i++)
                WriteRegion(sheet, parts[i], $"{IngredientsFolder}/{IngredientNames[i]}.png",
                    Mathf.Max(parts[i].width, parts[i].height) / ItemSize, new Vector2(0.5f, 0.5f));
        }

        // ---------- Ulises ----------

        private static void ImportUlises()
        {
            Sheet sheet = LoadSheet(UlisesSheet, whiteThreshold: 245);
            Sheet runSheet = LoadSheet(UlisesRunSheet);
            if (sheet == null || runSheet == null)
                return;

            List<List<Frame>> rows = FindFrames(sheet, coreFraction: 0.3f, attachDistance: 30);
            List<List<Frame>> runRows = FindFrames(runSheet, coreFraction: 0.3f, attachDistance: 20);
            Debug.Log($"[Odisea] Ulises: filas {string.Join(", ", rows.Select(r => r.Count))}; carrera {runRows.Sum(r => r.Count)} cuadros.");
            // Filas esperadas: 0 poses de frente (6), 1 carrera (6), 2 golpe (5), 3 salto (7).
            if (rows.Count < 4 || rows[0].Count < 4 || rows[2].Count < 3 || rows[3].Count < 7)
            {
                Debug.LogError("[Odisea] La hoja de Ulises no tiene las filas esperadas; revisa ulises.png.");
                return;
            }

            // Misma escala para todo: la carrera de cada hoja mide lo mismo en el juego.
            float ppu = MedianCoreHeight(rows[1]) / UlisesRunHeight;
            float runPpu = MedianCoreHeight(runRows.SelectMany(r => r).ToList()) / UlisesRunHeight;

            EnsureFolder(UlisesFolder);
            // Sustitutos mientras llega el arte que falta (ver Docs): golpe = puñetazo, lanzar = inicio del puñetazo,
            // daño = pose de frente echándose para atrás, escalera = subida del salto.
            var clips = new Dictionary<string, AnimationClip>
            {
            };

            // Golpe con rodillo (8 cuadros): 0-1 listo, 2 lo levanta, 3 barrida (estela), 4 madrazo, 5 sigue, 6-7 regresa.
            // El daño es inmediato y se puede volver a golpear a los 0.35 s: se usa de la barrida al final a 15 fps
            // (~0.33 s), así el madrazo se ve a ~0.07 s de presionar.
            if (TryLoadRow("ulises_golpe.png", attachDistance: 30, minFrames: 8, out Sheet attackSheet, out List<Frame> attack,
                    removeGroundLine: true))
            {
                float attackPpu = attack[7].Body.Height / UlisesStandHeight;
                clips["Attack"] = StripClip(attackSheet, PickFrames(attack, 3, 4, 5, 6, 7), attackPpu, "Attack", 15f, loop: false);
            }
            else
                clips["Attack"] = StripClip(sheet, Pick(rows, 2, 0, 1, 2), ppu, "Attack", 16f, loop: false, holdLast: 0.06f);

            // Lanzar (6 cuadros): 0 listo, 1 prepara, 2 brazo atrás (aún con la bolita), 3 suelta, 4 brazo estirado, 5 parado.
            // La bolita sale al instante de presionar: se empieza en "suelta" (3) para no ver dos bolitas a la vez,
            // y termina antes de poder volver a lanzar (0.3 s).
            if (TryLoadRow("ulises_lanzar.png", attachDistance: 20, minFrames: 6, out Sheet throwSheet, out List<Frame> toss))
            {
                float throwPpu = toss[5].Body.Height / UlisesStandHeight;
                clips["Throw"] = StripClip(throwSheet, PickFrames(toss, 3, 4, 5), throwPpu, "Throw", 12f, loop: false);
            }
            else
                clips["Throw"] = StripClip(sheet, Pick(rows, 2, 0, 1), ppu, "Throw", 14f, loop: false, holdLast: 0.1f);

            // Reposo: respiración de 3/4 (todos los cuadros están parados).
            // Daño y escalera no tienen un cuadro parado para medirlos; vienen en el mismo lienzo y a la misma escala
            // que el reposo, así que usan su escala.
            float idlePpu = ppu;
            if (TryLoadRow("ulises_idle.png", attachDistance: 20, minFrames: 2, out Sheet idleSheet, out List<Frame> idle))
            {
                idlePpu = MedianCoreHeight(idle) / UlisesStandHeight;
                clips["Idle"] = StripClip(idleSheet, idle, idlePpu, "Idle", 7f, loop: true);
            }
            else
                clips["Idle"] = StripClip(sheet, Pick(rows, 3, 6), ppu, "Idle", 8f, loop: true);

            // Daño (4 cuadros): 0 recibe el golpe, 1 sale volando mareado, 2 trastabilla, 3 se recupera.
            // El golpe quita el control 0.25 s y lo avienta hacia arriba: se usan 0-2 a 12 fps y de ahí pasa a la caída.
            if (TryLoadRow("ulises_dano.png", attachDistance: 40, minFrames: 3, out Sheet hurtSheet, out List<Frame> hurt))
                clips["Hurt"] = StripClip(hurtSheet, PickFrames(hurt, 0, 1, 2), idlePpu, "Hurt", 12f, loop: false);
            else
                clips["Hurt"] = StripClip(sheet, Pick(rows, 0, 3), ppu, "Hurt", 8f, loop: false, holdLast: 0.25f);

            // Escalera (de espaldas). Anclada en los pies (al pie de la escalera queda parado en el suelo);
            // solo avanza mientras sube o baja (parámetro ClimbSpeed del Animator). La hoja trae 4 cuadros, pero el
            // 4.º tiene los tirantes rectos (en los demás van cruzados) y repite al 1.º: se usan los 3 primeros.
            if (TryLoadRow("ulises_escalera.png", attachDistance: 20, minFrames: 4, out Sheet climbSheet, out List<Frame> climb))
                clips["Climb"] = StripClip(climbSheet, PickFrames(climb, 0, 1, 2), idlePpu, "Climb", 8f, loop: true);
            else
                clips["Climb"] = StripClip(sheet, Pick(rows, 3, 2), ppu, "Climb", 8f, loop: true);

            // Ficha del mapa (opcional, ulises_mapa.png con fondo transparente): 2 cuadros de reposo y 4 de caminar.
            if (TryLoadRow("ulises_mapa.png", attachDistance: 20, minFrames: 6, out Sheet mapSheet, out List<Frame> map))
            {
                float mapPpu = map[0].Body.Height / MapTokenHeight;
                WriteStrip(mapSheet, PickFrames(map, 0, 1), MapIdlePath, mapPpu, "Ulises_Mapa_Reposo");
                WriteStrip(mapSheet, PickFrames(map, 2, 3, 4, 5), MapWalkPath, mapPpu, "Ulises_Mapa_Camina");
            }

            // Tomar café (opcional, ulises_cafe.png con fondo transparente): sin la hoja, el juego dibuja un jarrito
            // que Ulises se empina (PlayerBuffs).
            if (TryLoadRow("ulises_cafe.png", attachDistance: 20, minFrames: 2, out Sheet drinkSheet, out List<Frame> drink))
                // Lo sube, trago y sonríe: 0.25 s, lo que dura PlayerBuffs.drinkTime (igual que comer y la hoja).
                clips["Drink"] = StripClip(drinkSheet, drink, idlePpu, "Drink", drink.Count / 0.25f, loop: false);

            // Comer el pescado y ponerse la hoja (opcionales, fondo transparente): más rápidas que el café, ~0.25 s
            // (PlayerBuffs.eatTime y leafTime). Sin la hoja de cuadros, el ítem se toma sin animación ni pausa.
            if (TryLoadRow("ulises_pescado.png", attachDistance: 20, minFrames: 2, out Sheet eatSheet, out List<Frame> eat))
                clips["Eat"] = StripClip(eatSheet, eat, idlePpu, "Eat", eat.Count / 0.25f, loop: false);
            if (TryLoadRow("ulises_hoja.png", attachDistance: 20, minFrames: 2, out Sheet leafSheet, out List<Frame> leaf))
                clips["Leaf"] = StripClip(leafSheet, leaf, idlePpu, "Leaf", leaf.Count / 0.25f, loop: false);

            // Muerte (7 cuadros): golpe, sale volando, gira, mareado de pie (3, sirve de medida), cae y queda tirado.
            // Se queda en el último cuadro; la escena se reinicia aparte.
            if (TryLoadRow("ulises_muerte.png", attachDistance: 30, minFrames: 7, out Sheet deathSheet, out List<Frame> death))
            {
                float deathPpu = death[3].Body.Height / UlisesStandHeight;
                clips["Die"] = StripClip(deathSheet, death, deathPpu, "Die", 10f, loop: false);
            }
            else
                clips["Die"] = clips["Hurt"];

            // Correr: el polvo suelto se descarta (solo se pega lo que toca al cuerpo) para no mezclarlo con el cuadro vecino.
            if (TryLoadRow("ulises_correr.png", attachDistance: 8, minFrames: 4, out Sheet runNewSheet, out List<Frame> run))
            {
                float newRunPpu = MedianCoreHeight(run) / UlisesRunHeight;
                clips["Run"] = StripClip(runNewSheet, run, newRunPpu, "Run", 14f, loop: true);
            }
            else
                clips["Run"] = StripClip(runSheet, runRows.SelectMany(r => r).ToList(), runPpu, "Run", 12f, loop: true);

            // Salto (9 cuadros): 0 agachado, 1-3 subida, 4 punta, 5-6 caída, 7 aterrizaje, 8 parado.
            // El agachado no se usa: el salto del juego es inmediato. La subida dura ~0.2 s, por eso va rápida;
            // la caída empieza en la punta (brazos abiertos).
            if (TryLoadRow("ulises_salto.png", attachDistance: 12, minFrames: 9, out Sheet jumpSheet, out List<Frame> jump))
            {
                float jumpPpu = jump[8].Body.Height / UlisesStandHeight;
                clips["Jump"] = StripClip(jumpSheet, Centered(jump, jumpPpu, 1, 2, 3), jumpPpu, "Jump", 18f, loop: false);
                clips["Fall"] = StripClip(jumpSheet, Centered(jump, jumpPpu, 4, 5, 6), jumpPpu, "Fall", 12f, loop: false);
                clips["Land"] = StripClip(jumpSheet, PickFrames(jump, 7, 8), jumpPpu, "Land", 14f, loop: false);
            }
            else
            {
                clips["Jump"] = StripClip(sheet, Pick(rows, 3, 1, 2), ppu, "Jump", 10f, loop: false);
                clips["Fall"] = StripClip(sheet, Pick(rows, 3, 4), ppu, "Fall", 8f, loop: true);
                clips["Land"] = StripClip(sheet, Pick(rows, 3, 5, 6), ppu, "Land", 14f, loop: false);
            }

            // Doble salto (8 cuadros): 1 encogido, 2-4 vuelta en el aire; 5 parado sirve de medida.
            if (TryLoadRow("ulises_doble_salto.png", attachDistance: 30, minFrames: 6, out Sheet doubleSheet, out List<Frame> spin))
            {
                float spinPpu = spin[5].Body.Height / UlisesStandHeight;
                clips["DoubleJump"] = StripClip(doubleSheet, Centered(spin, spinPpu, 1, 2, 3, 4), spinPpu, "DoubleJump", 16f, loop: false);
            }
            else
                clips["DoubleJump"] = StripClip(sheet, Pick(rows, 3, 3), ppu, "DoubleJump", 10f, loop: false, holdLast: 0.15f);

            BuildUlisesController(clips);
        }

        private static AnimationClip StripClip(Sheet sheet, List<Frame> frames, float ppu, string name, float fps,
            bool loop, float holdLast = 0f)
        {
            string texturePath = $"{UlisesFolder}/Ulises_{name}.png";
            List<Sprite> sprites = WriteStrip(sheet, frames, texturePath, ppu, $"Ulises_{name}");
            return CreateClip($"{UlisesFolder}/Ulises_{name}.anim", sprites, fps, loop, holdLast);
        }

        private static void BuildUlisesController(Dictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = ResetController(UlisesControllerPath);
            foreach (string name in new[] { "Speed", "VerticalSpeed", "ClimbSpeed" })
                controller.AddParameter(name, AnimatorControllerParameterType.Float);
            // Velocidad de la animación de correr: 1 normal, más con el café (PlayerAnimator).
            controller.AddParameter(new AnimatorControllerParameter
                { name = "RunSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            foreach (string name in new[] { "IsGrounded", "IsClimbing" })
                controller.AddParameter(name, AnimatorControllerParameterType.Bool);
            foreach (string name in new[] { "Attack", "Throw", "DoubleJump", "Hurt", "Die" })
                controller.AddParameter(name, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();
            int column = 0;
            foreach (var pair in clips)
            {
                AnimatorState state = machine.AddState(pair.Key, new Vector3(250f * (column % 4), 120f * (column / 4), 0f));
                state.motion = pair.Value;
                states[pair.Key] = state;
                column++;
            }
            machine.defaultState = states["Idle"];

            AnimatorState idle = states["Idle"], run = states["Run"], jump = states["Jump"], fall = states["Fall"],
                climb = states["Climb"], land = states["Land"];
            // Desplazamiento: suelo (reposo / correr), aire (subir / caer) y escalera.
            Link(idle, run, Cond("IsGrounded", true), Cond("Speed", AnimatorConditionMode.Greater, 0.1f));
            Link(run, idle, Cond("Speed", AnimatorConditionMode.Less, 0.1f));
            foreach (AnimatorState ground in new[] { idle, run })
            {
                Link(ground, jump, Cond("IsGrounded", false), Cond("VerticalSpeed", AnimatorConditionMode.Greater, 0.1f));
                Link(ground, fall, Cond("IsGrounded", false), Cond("VerticalSpeed", AnimatorConditionMode.Less, -0.1f));
            }
            Link(jump, fall, Cond("VerticalSpeed", AnimatorConditionMode.Less, 0f));
            // Al tocar el suelo: quieto = aterrizaje (agacharse y pararse); corriendo = sigue corriendo sin frenar.
            foreach (AnimatorState air in new[] { jump, fall })
            {
                Link(air, land, Cond("IsGrounded", true), Cond("Speed", AnimatorConditionMode.Less, 0.1f));
                Link(air, run, Cond("IsGrounded", true), Cond("Speed", AnimatorConditionMode.Greater, 0.1f));
            }
            Link(land, run, Cond("Speed", AnimatorConditionMode.Greater, 0.1f));
            Link(land, jump, Cond("IsGrounded", false), Cond("VerticalSpeed", AnimatorConditionMode.Greater, 0.1f));
            ExitWhenDone(land, idle);
            foreach (AnimatorState any in new[] { idle, run, jump, fall })
                Link(any, climb, Cond("IsClimbing", true));
            Link(climb, idle, Cond("IsClimbing", false));
            // En la escalera la animación solo avanza mientras se mueve (PlayerAnimator pone ClimbSpeed en 0 o 1).
            climb.speedParameterActive = true;
            climb.speedParameter = "ClimbSpeed";
            // Con el café las piernas van tan rápido como él (y sus pasos suenan más seguido).
            run.speedParameterActive = true;
            run.speedParameter = "RunSpeed";

            // Acciones de un momento: desde cualquier estado con su trigger; al terminar regresan a reposo
            // y de ahí el desplazamiento elige (correr, caer...) en el siguiente cuadro.
            // La muerte va primero: gana aunque en el mismo cuadro se haya pedido otra acción. No tiene salida.
            AnyState(machine, states["Die"], "Die", toSelf: false);
            AnyState(machine, states["Attack"], "Attack", toSelf: true);
            AnyState(machine, states["Throw"], "Throw", toSelf: true);
            AnyState(machine, states["Hurt"], "Hurt", toSelf: false);
            AnyState(machine, states["DoubleJump"], "DoubleJump", toSelf: true);
            foreach (string action in new[] { "Attack", "Throw", "Hurt" })
                ExitWhenDone(states[action], idle);
            foreach (string item in new[] { "Drink", "Eat", "Leaf" })
            {
                if (!states.TryGetValue(item, out AnimatorState itemState))
                    continue;
                controller.AddParameter(item, AnimatorControllerParameterType.Trigger);
                AnyState(machine, itemState, item, toSelf: false);
                ExitWhenDone(itemState, idle);
            }
            // Tras la vuelta: si ya va bajando, a caída; si no, a la subida (que luego pasa a caída).
            ExitWhenDone(states["DoubleJump"], fall, Cond("VerticalSpeed", AnimatorConditionMode.Less, 0f));
            ExitWhenDone(states["DoubleJump"], jump);
            EditorUtility.SetDirty(controller);
        }

        // ---------- Bolita de masa ----------

        /// <summary>
        /// masa_volando.png (7 cuadros): 0-2 volando, 3 choca, 4 estalla, 5-6 charco en el suelo. Solo sprites (sin
        /// Animator): los pasa SpriteFlipbook. Centrados en la bolita, para que el embarrón quede donde chocó.
        /// El charco no se usa todavía: la bolita vuela recta y choca en el aire (enemigos, paredes).
        /// </summary>
        private static void ImportDough()
        {
            // Las migajas que salen volando en el estallido son grandes (~17 % del cuadro mayor): con el 30 % se pegan
            // a su cuadro en vez de contar como cuadro propio.
            if (!TryLoadRow("masa_volando.png", attachDistance: 40, minFrames: 7, out Sheet sheet, out List<Frame> frames))
                return;
            float ppu = frames[0].Body.Height / DoughBallSize;
            EnsureFolder(DoughFolder);
            WriteStrip(sheet, PickFrames(frames, 0, 1, 2).Select(f => f.WithCenterAnchor(0)).ToList(), DoughFlyPath, ppu, "Masa_Vuela");
            WriteStrip(sheet, PickFrames(frames, 3, 4).Select(f => f.WithCenterAnchor(0)).ToList(), DoughImpactPath, ppu, "Masa_Impacto");
        }

        // ---------- Cocodrilo ----------

        private static void ImportCrocodile()
        {
            EnsureFolder(CrocodileFolder);
            List<Sprite> walkSprites;
            float ppu;
            float walkLength; // Largo de la cola al hocico en píxeles de la hoja del caminar.

            // Caminar nuevo (2 filas de 6): solo la primera fila, que tiene el mismo tamaño en todos los cuadros;
            // en la segunda el cocodrilo cambia de tamaño entre cuadros y "palpitaría" al caminar.
            Sheet walkSheet = LoadSheet(CrocodileWalkSheet, warnIfMissing: false);
            List<List<Frame>> walkRows = walkSheet != null ? FindFrames(walkSheet, coreFraction: 0.3f, attachDistance: 15) : null;
            if (walkRows != null && walkRows[0].Count >= 4)
            {
                List<Frame> walk = walkRows[0];
                Debug.Log($"[Odisea] cocodrilo_caminar.png: {walk.Count} cuadros (fila de arriba).");
                ppu = MedianCoreHeight(walk) / CrocodileHeight;
                walkLength = Median(walk.Select(f => (float)f.Body.Width).ToList());
                walkSprites = WriteStrip(walkSheet, walk, $"{CrocodileFolder}/Cocodrilo_Walk.png", ppu, "Cocodrilo_Walk");
            }
            else
            {
                Sheet sheet = LoadSheet(CrocodileSheet);
                if (sheet == null)
                    return;
                List<Frame> frames = FindFrames(sheet, coreFraction: 0.3f, attachDistance: 15).SelectMany(r => r).ToList();
                Debug.Log($"[Odisea] Cocodrilo: {frames.Count} cuadros.");
                ppu = MedianCoreHeight(frames) / CrocodileHeight;
                walkLength = Median(frames.Select(f => (float)f.Body.Width).ToList());
                walkSprites = WriteStrip(sheet, frames, $"{CrocodileFolder}/Cocodrilo_Walk.png", ppu, "Cocodrilo_Walk");
            }
            AnimationClip walkClip = CreateClip($"{CrocodileFolder}/Cocodrilo_Walk.anim", walkSprites, CrocodileWalkFps, loop: true);

            AnimatorController controller = ResetController(CrocodileControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState walkState = machine.AddState("Walk");
            walkState.motion = walkClip;
            machine.defaultState = walkState;

            // Mordida (6 cuadros): 0 listo, 1 se levanta con el hocico abierto, 2 se lanza, 3 cierra (aquí daña),
            // 4-5 se recupera. A 12 fps dura 0.5 s y cierra a los 0.25 s (los tiempos de PatrolEnemy).
            // Viene en el mismo lienzo y escala que el caminar: usa su escala.
            Sheet biteSheet = LoadSheet(CrocodileBiteSheet, warnIfMissing: false);
            if (biteSheet != null)
            {
                List<Frame> bite = FindFrames(biteSheet, coreFraction: 0.3f, attachDistance: 25).SelectMany(r => r)
                    .OrderBy(f => f.Core.MinX).ToList();
                Debug.Log($"[Odisea] cocodrilo_mordida.png: {bite.Count} cuadros.");
                if (bite.Count >= 6)
                {
                    List<Sprite> biteSprites = WriteStrip(biteSheet, bite, $"{CrocodileFolder}/Cocodrilo_Bite.png", ppu, "Cocodrilo_Bite");
                    AnimationClip biteClip = CreateClip($"{CrocodileFolder}/Cocodrilo_Bite.anim", biteSprites, 12f, loop: false);
                    controller.AddParameter("Bite", AnimatorControllerParameterType.Trigger);
                    AnimatorState biteState = machine.AddState("Bite");
                    biteState.motion = biteClip;
                    AnyState(machine, biteState, "Bite", toSelf: false);
                    ExitWhenDone(biteState, walkState);
                }
            }

            // Muerte (6 cuadros): se espanta, salta, gira en el aire y cae boca arriba. Mismo lienzo y escala
            // que el caminar. Se alinea al suelo común de la hoja (no a los pies de cada cuadro) para que el salto se vea.
            // Su transición va antes que la de daño: si el golpe mata, gana la muerte.
            Sheet deathSheet = LoadSheet(CrocodileDeathSheet, warnIfMissing: false);
            if (deathSheet != null)
            {
                List<Frame> death = FindFrames(deathSheet, coreFraction: 0.3f, attachDistance: 40).SelectMany(r => r)
                    .OrderBy(f => f.Core.MinX).ToList();
                Debug.Log($"[Odisea] cocodrilo_muerte.png: {death.Count} cuadros.");
                if (death.Count >= 4)
                {
                    int ground = death.Min(f => f.Body.MinY);
                    death = death.Select(f => f.WithGroundAnchor(ground)).ToList();
                    List<Sprite> deathSprites = WriteStrip(deathSheet, death, $"{CrocodileFolder}/Cocodrilo_Die.png", ppu, "Cocodrilo_Die");
                    AnimationClip deathClip = CreateClip($"{CrocodileFolder}/Cocodrilo_Die.anim", deathSprites, 10f, loop: false,
                        holdLast: CrocodileDeathHold);
                    controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
                    AnimatorState deathState = machine.AddState("Die");
                    deathState.motion = deathClip;
                    AnyState(machine, deathState, "Die", toSelf: false);
                }
            }

            // Daño (3 cuadros): grita, se encoge de dolor y se recompone. Esta hoja viene dibujada más grande que
            // el caminar; se escala para que el cocodrilo mida lo mismo de la cola al hocico.
            Sheet hurtSheet = LoadSheet(CrocodileHurtSheet, warnIfMissing: false);
            if (hurtSheet != null)
            {
                List<Frame> hurt = FindFrames(hurtSheet, coreFraction: 0.3f, attachDistance: 60).SelectMany(r => r)
                    .OrderBy(f => f.Core.MinX).ToList();
                Debug.Log($"[Odisea] cocodrilo_dano.png: {hurt.Count} cuadros.");
                if (hurt.Count >= 2)
                {
                    float hurtPpu = ppu * hurt.Max(f => f.Body.Width) / walkLength;
                    int ground = hurt.Min(f => f.Body.MinY);
                    hurt = hurt.Select(f => f.WithGroundAnchor(ground)).ToList();
                    List<Sprite> hurtSprites = WriteStrip(hurtSheet, hurt, $"{CrocodileFolder}/Cocodrilo_Hurt.png", hurtPpu, "Cocodrilo_Hurt");
                    // Lento a propósito (~0.65 s): el golpe tiene que notarse; el cocodrilo se queda quieto ese tiempo.
                    AnimationClip hurtClip = CreateClip($"{CrocodileFolder}/Cocodrilo_Hurt.anim", hurtSprites, 6f, loop: false, holdLast: 0.15f);
                    controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
                    AnimatorState hurtState = machine.AddState("Hurt");
                    hurtState.motion = hurtClip;
                    AnyState(machine, hurtState, "Hurt", toSelf: true);
                    ExitWhenDone(hurtState, walkState);
                }
            }
            EditorUtility.SetDirty(controller);
        }

        // ---------- Madame Circe ----------

        public const string CirceFolder = "Assets/_Project/Art/Characters/Circe";
        public const string CirceControllerPath = CirceFolder + "/Circe.controller";
        /// <summary>Tira de Circe en reposo (el primer cuadro se ve en la escena antes de darle Play).</summary>
        public const string CirceIdlePath = CirceFolder + "/Circe_Reposo.png";
        private const string CirceSource = "Personajes/sprite en png/circe";
        // Estatura con todo y su gorro alto: más alta que los secuaces.
        private const float CirceHeight = 2.7f;
        /// <summary>Cuadros por segundo de "lanzar": la mano suelta en el cuadro 5 (índice 4), a los 0.4 s.</summary>
        public const float CirceThrowFps = 10f;
        public const int CirceReleaseFrame = 4;

        /// <summary>
        /// Madame Circe (Odisea > Arte > Importar a Madame Circe): Idle (6 cuadros, en bucle), Throw (6: echa el brazo
        /// atrás, suelta en el quinto y se recompone) y Laugh (4, se tapa la boca; dos vueltas). Las hojas traen fondo
        /// blanco. Cada una se escala con su primer cuadro (parada) y los pies quedan a la misma altura.
        /// </summary>
        [MenuItem("Odisea/Arte/Importar a Madame Circe")]
        public static void ImportCirce()
        {
            EnsureFolder(CirceFolder);
            ImportCirceRope();
            List<Sprite> idle = ImportCirceStrip("circe_reposo.png", 245, CirceIdlePath, "Circe_Reposo");
            if (idle == null)
                return;
            List<Sprite> toss = ImportCirceStrip("circe_lanzar.png", 245, $"{CirceFolder}/Circe_Lanzar.png", "Circe_Lanzar");
            // La de reír es JPG: el ruido de compresión pide un umbral un poco más bajo.
            List<Sprite> laugh = ImportCirceStrip("circe_reir.jfif", 238, $"{CirceFolder}/Circe_Reir.png", "Circe_Reir");

            AnimatorController controller = ResetController(CirceControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle");
            idleState.motion = CreateClip($"{CirceFolder}/Circe_Idle.anim", idle, 7f, loop: true);
            machine.defaultState = idleState;
            if (toss != null)
            {
                controller.AddParameter("Throw", AnimatorControllerParameterType.Trigger);
                AnimatorState state = machine.AddState("Throw");
                state.motion = CreateClip($"{CirceFolder}/Circe_Throw.anim", toss, CirceThrowFps, loop: false);
                AnyState(machine, state, "Throw", toSelf: false);
                ExitWhenDone(state, idleState);
            }
            if (laugh != null)
            {
                controller.AddParameter("Laugh", AnimatorControllerParameterType.Trigger);
                AnimatorState state = machine.AddState("Laugh");
                state.motion = CreateClip($"{CirceFolder}/Circe_Laugh.anim", laugh.Concat(laugh).ToList(), 8f, loop: false);
                AnyState(machine, state, "Laugh", toSelf: false);
                ExitWhenDone(state, idleState);
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static List<Sprite> ImportCirceStrip(string file, int whiteThreshold, string path, string spriteName)
        {
            Sheet sheet = LoadSheet($"{CirceSource}/{file}", whiteThreshold, warnIfMissing: false);
            if (sheet == null)
                return null;
            List<Frame> frames = FindFrames(sheet, coreFraction: 0.3f, attachDistance: 25).SelectMany(r => r)
                .OrderBy(f => f.Core.MinX).ToList();
            Debug.Log($"[Odisea] {spriteName}: {frames.Count} cuadros.");
            float ppu = frames[0].Body.Height / CirceHeight;
            int ground = frames.Min(f => f.Body.MinY);
            frames = frames.Select(f => f.WithGroundAnchor(ground)).ToList();
            return WriteStrip(sheet, frames, path, ppu, spriteName);
        }

        /// <summary>Soga que cuelga la tablita de Circe: un tramo que se repite a lo alto (Tiled) sin que se note la unión.</summary>
        public const string CirceRopePath = CirceFolder + "/Circe_Soga.png";
        // Grosor de la soga en el juego.
        private const float CirceRopeWidth = 0.16f;

        /// <summary>
        /// Recorta de soga.png un tramo exacto del trenzado: busca cada cuánto se repite (el desfase con el que la soga
        /// se parece más a sí misma) y corta una vuelta del centro de la imagen, sin margen arriba ni abajo.
        /// </summary>
        private static void ImportCirceRope()
        {
            Sheet sheet = LoadSheet($"{CirceSource}/soga.png", 245, warnIfMissing: false);
            if (sheet == null)
                return;
            int minX = sheet.Width, maxX = -1;
            for (int x = 0; x < sheet.Width; x++)
            {
                int filled = 0;
                for (int y = 0; y < sheet.Height; y++)
                    if (sheet.Pixels[y * sheet.Width + x].a > 24)
                        filled++;
                if (filled > sheet.Height / 4)
                {
                    minX = Mathf.Min(minX, x);
                    maxX = x;
                }
            }
            if (maxX < 0)
            {
                Debug.LogError("[Odisea] soga.png: no se encontró la soga.");
                return;
            }
            int width = maxX - minX + 1;
            // Desfase vertical con la menor diferencia; de los que casi empatan (múltiplos), el más chico.
            int maxLag = Mathf.Min(sheet.Height / 3, 600);
            var diffs = new float[maxLag + 1];
            float best = float.MaxValue;
            for (int lag = 20; lag <= maxLag; lag++)
            {
                double sum = 0;
                for (int y = 0; y + lag < sheet.Height; y += 2)
                for (int x = minX; x <= maxX; x += 2)
                {
                    Color32 a = sheet.Pixels[y * sheet.Width + x], b = sheet.Pixels[(y + lag) * sheet.Width + x];
                    sum += Mathf.Abs(a.r + a.g + a.b - b.r - b.g - b.b) + Mathf.Abs(a.a - b.a);
                }
                diffs[lag] = (float)(sum / (sheet.Height - lag));
                best = Mathf.Min(best, diffs[lag]);
            }
            int period = 20;
            for (int lag = 20; lag <= maxLag; lag++)
            {
                if (diffs[lag] > best * 1.15f)
                    continue;
                period = lag;
                // El mínimo local de ese valle.
                while (period < maxLag && diffs[period + 1] < diffs[period])
                    period++;
                break;
            }
            int startY = (sheet.Height - period) / 2;
            var pixels = new Color32[width * period];
            for (int y = 0; y < period; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = sheet.Pixels[(startY + y) * sheet.Width + minX + x];
            WritePng(CirceRopePath, pixels, width, period);
            ConfigureTexture(CirceRopePath, width / CirceRopeWidth, new Vector2(0.5f, 0.5f), Vector4.zero, multiple: false);
            Debug.Log($"[Odisea] Soga de Circe: {width}x{period} px por tramo.");
        }

        /// <summary>Caja entera (vuela girando sobre su centro).</summary>
        public const string CirceCratePath = CirceFolder + "/Caja.png";
        /// <summary>La caja hecha pedazos (se ve un instante al romperse).</summary>
        public const string CirceBrokenCratePath = CirceFolder + "/Caja_Rota.png";
        /// <summary>Cáscara en el aire (gira sobre su centro).</summary>
        public const string CircePeelPath = CirceFolder + "/Cascara.png";
        /// <summary>Cáscara tirada en la repisa: pivote abajo, para apoyarla en el piso.</summary>
        public const string CirceLandedPeelPath = CirceFolder + "/Cascara_Piso.png";
        private const float CrateWidth = 0.8f;
        private const float LandedPeelWidth = 0.95f;

        /// <summary>
        /// Lo que avienta Circe: caja.png (izquierda la caja, derecha sus pedazos) y cascara.png (izquierda volando,
        /// derecha en el piso). Cada hoja usa una sola escala para sus dos dibujos.
        /// </summary>
        [MenuItem("Odisea/Arte/Importar lo que avienta Circe")]
        public static void ImportCirceThrowables()
        {
            EnsureFolder(CirceFolder);
            ImportHalves("caja.png", CirceCratePath, CirceBrokenCratePath, new Vector2(0.5f, 0.5f), (left, _) => left.width / CrateWidth);
            ImportHalves("cascara.png", CircePeelPath, CirceLandedPeelPath, new Vector2(0.5f, 0f),
                (_, right) => right.width / LandedPeelWidth);
        }

        private static void ImportHalves(string file, string leftPath, string rightPath, Vector2 rightPivot,
            Func<RectInt, RectInt, float> pixelsPerUnit)
        {
            Sheet sheet = LoadSheet($"{CirceSource}/{file}", 245, warnIfMissing: false);
            if (sheet == null)
                return;
            int half = sheet.Width / 2;
            RectInt left = OpaqueBounds(sheet, new RectInt(0, 0, half, sheet.Height));
            RectInt right = OpaqueBounds(sheet, new RectInt(half, 0, sheet.Width - half, sheet.Height));
            float ppu = pixelsPerUnit(left, right);
            WriteRegion(sheet, left, leftPath, ppu, new Vector2(0.5f, 0.5f));
            WriteRegion(sheet, right, rightPath, ppu, rightPivot);
        }

        // ---------- Secuaces de Circe (cocinero y cocinera) ----------

        private const string HenchmenSource = "Personajes/sprite en png/enemigos";
        public const string HenchmenFolder = "Assets/_Project/Art/Characters/Enemigos";
        // Estatura con todo y gorro: un poco más altos que Ulises.
        private const float HenchmanHeight = 2.15f;

        /// <summary>Controller del secuaz (hombre = cocinero con cuchara, mujer = cocinera con batidor).</summary>
        public static string HenchmanControllerPath(bool male) => $"{HenchmanFolder(male)}/{HenchmanPrefix(male)}.controller";

        /// <summary>Primer cuadro de su caminar (para que la escena se vea bien antes de darle Play).</summary>
        public static string HenchmanWalkPath(bool male) => $"{HenchmanFolder(male)}/{HenchmanPrefix(male)}_Walk.png";

        /// <summary>Animación del secuaz: Walk, Attack, Hurt o Die.</summary>
        public static string HenchmanClipPath(bool male, string clip) => $"{HenchmanFolder(male)}/{HenchmanPrefix(male)}_{clip}.anim";

        private static string HenchmanFolder(bool male) => $"{HenchmenFolder}/Secuaz {(male ? "hombre" : "mujer")}";
        private static string HenchmanPrefix(bool male) => male ? "SecuazHombre" : "SecuazMujer";

        [MenuItem("Odisea/Arte/Importar secuaces de Circe")]
        public static void ImportHenchmen()
        {
            ImportHenchman(male: true, "secuaz hombre/secuaz_hombre", "ataque_cuchara");
            ImportHenchman(male: false, "secuaz mujer/secuaz_mujer", "ataque_batidor");
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Un secuaz con el mismo esquema que el cocodrilo (lo maneja PatrolEnemy): Walk (6 cuadros), Bite = el ataque
        /// (4 cuadros: prepara, levanta, golpea —aquí daña— y se recompone; a 8 fps dura 0.5 s y pega a los 0.25 s),
        /// Hurt (el gorro sale volando y se marea) y Die (el gorro sale volando y cae de espaldas). Cada hoja tiene su
        /// propio lienzo: se escala para que mida HenchmanHeight parado, y los pies de cada hoja quedan a la misma altura.
        /// </summary>
        private static void ImportHenchman(bool male, string sourcePrefix, string attackName)
        {
            string folder = HenchmanFolder(male), prefix = HenchmanPrefix(male);
            Sheet walkSheet = LoadSheet($"{HenchmenSource}/{sourcePrefix}_caminar.png", warnIfMissing: false);
            if (walkSheet == null)
                return;
            EnsureFolder(folder);
            List<Frame> walk = FindFrames(walkSheet, coreFraction: 0.3f, attachDistance: 15).SelectMany(r => r)
                .OrderBy(f => f.Core.MinX).ToList();
            Debug.Log($"[Odisea] {prefix}: caminar {walk.Count} cuadros.");
            float ppu = MedianCoreHeight(walk) / HenchmanHeight;
            List<Sprite> walkSprites = WriteStrip(walkSheet, walk, $"{folder}/{prefix}_Walk.png", ppu, $"{prefix}_Walk");
            AnimationClip walkClip = CreateClip($"{folder}/{prefix}_Walk.anim", walkSprites, 10f, loop: true);

            AnimatorController controller = ResetController(HenchmanControllerPath(male));
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState walkState = machine.AddState("Walk");
            walkState.motion = walkClip;
            machine.defaultState = walkState;

            // Ataque: la escala sale del último cuadro (parado, con el utensilio abajo).
            Sheet attackSheet = LoadSheet($"{HenchmenSource}/{sourcePrefix}_{attackName}.png", warnIfMissing: false);
            if (attackSheet != null)
            {
                List<Frame> attack = FindFrames(attackSheet, coreFraction: 0.3f, attachDistance: 25).SelectMany(r => r)
                    .OrderBy(f => f.Core.MinX).ToList();
                Debug.Log($"[Odisea] {prefix}: ataque {attack.Count} cuadros.");
                if (attack.Count >= 4)
                {
                    float attackPpu = attack[attack.Count - 1].Core.Height / HenchmanHeight;
                    int ground = attack.Min(f => f.Body.MinY);
                    attack = attack.Select(f => f.WithGroundAnchor(ground)).ToList();
                    List<Sprite> sprites = WriteStrip(attackSheet, attack, $"{folder}/{prefix}_Attack.png", attackPpu, $"{prefix}_Attack");
                    AnimationClip clip = CreateClip($"{folder}/{prefix}_Attack.anim", sprites, 8f, loop: false);
                    controller.AddParameter("Bite", AnimatorControllerParameterType.Trigger);
                    AnimatorState state = machine.AddState("Bite");
                    state.motion = clip;
                    AnyState(machine, state, "Bite", toSelf: false);
                    ExitWhenDone(state, walkState);
                }
            }

            // Golpe y muerte (5 cuadros): 0 el gorro sale volando, 1 mareado, 2-4 cae de espaldas. La escala sale del
            // mareado (parado con el gorro puesto).
            Sheet hitSheet = LoadSheet($"{HenchmenSource}/{sourcePrefix}_golpe_muerte.png", warnIfMissing: false);
            if (hitSheet != null)
            {
                List<Frame> hit = FindFrames(hitSheet, coreFraction: 0.3f, attachDistance: 60).SelectMany(r => r)
                    .OrderBy(f => f.Core.MinX).ToList();
                Debug.Log($"[Odisea] {prefix}: golpe y muerte {hit.Count} cuadros.");
                if (hit.Count >= 5)
                {
                    float hitPpu = hit[1].Core.Height / HenchmanHeight;
                    int ground = hit.Min(f => f.Body.MinY);
                    hit = hit.Select(f => f.WithGroundAnchor(ground)).ToList();
                    // Muerte primero: si el golpe mata, gana la muerte.
                    List<Frame> death = new() { hit[0], hit[2], hit[3], hit[4] };
                    List<Sprite> deathSprites = WriteStrip(hitSheet, death, $"{folder}/{prefix}_Die.png", hitPpu, $"{prefix}_Die");
                    AnimationClip deathClip = CreateClip($"{folder}/{prefix}_Die.anim", deathSprites, 8f, loop: false,
                        holdLast: CrocodileDeathHold);
                    controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
                    AnimatorState deathState = machine.AddState("Die");
                    deathState.motion = deathClip;
                    AnyState(machine, deathState, "Die", toSelf: false);

                    List<Sprite> hurtSprites = WriteStrip(hitSheet, new List<Frame> { hit[0], hit[1] }, $"{folder}/{prefix}_Hurt.png",
                        hitPpu, $"{prefix}_Hurt");
                    AnimationClip hurtClip = CreateClip($"{folder}/{prefix}_Hurt.anim", hurtSprites, 5f, loop: false, holdLast: 0.15f);
                    controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
                    AnimatorState hurtState = machine.AddState("Hurt");
                    hurtState.motion = hurtClip;
                    AnyState(machine, hurtState, "Hurt", toSelf: true);
                    ExitWhenDone(hurtState, walkState);
                }
            }
            EditorUtility.SetDirty(controller);
        }

        // ---------- Piezas del nivel 1 ----------

        /// <summary>
        /// Viga, escalera, tablón y dos tablones rotos de una hoja de nivel. Escribe Viga, Escalera, Tablon,
        /// TablonRoto1 y TablonRoto2 en la carpeta indicada (mismos nombres para cada nivel).
        /// </summary>
        private static void ImportLevelPieces(string sheetPath, int whiteThreshold, string folder, string label)
        {
            Sheet sheet = LoadSheet(sheetPath, whiteThreshold, warnIfMissing: false);
            if (sheet == null)
                return;
            // Cada pieza es su propio "cuadro"; los puntitos sueltos (ruido del JPG) se pegan a la más cercana.
            List<Frame> pieces = FindFrames(sheet, coreFraction: 0.02f, attachDistance: 6).SelectMany(r => r).ToList();
            List<Frame> tall = pieces.Where(p => p.Core.Height > p.Core.Width).OrderBy(p => p.Core.MinX).ToList();
            List<Frame> wide = pieces.Where(p => p.Core.Width >= p.Core.Height).OrderByDescending(p => p.Core.Width).ToList();
            Debug.Log($"[Odisea] {label}: {tall.Count} piezas verticales y {wide.Count} horizontales.");
            if (tall.Count < 2 || wide.Count < 3)
            {
                Debug.LogError($"[Odisea] {label}: la hoja no tiene viga, escalera y 3 tablones; revisa {sheetPath}.");
                return;
            }

            float ppu = wide[0].Body.Height / PlankHeight;
            EnsureFolder(folder);
            // La viga se repite a lo alto conservando la base (abajo) y el remate (arriba).
            int postFoot = Mathf.RoundToInt(tall[0].Core.Height * 0.12f);
            int postCap = Mathf.RoundToInt(tall[0].Core.Height * 0.04f);
            WriteSingle(sheet, tall[0], $"{folder}/Viga.png", ppu, new Vector4(0f, postFoot, 0f, postCap));
            // La escalera se repite a lo alto: los extremos (bordes) no se repiten.
            int ladderCap = Mathf.RoundToInt(tall[1].Core.Height * 0.06f);
            WriteSingle(sheet, tall[1], $"{folder}/Escalera.png", ppu, new Vector4(0f, ladderCap, 0f, ladderCap));
            // Los tablones se repiten a lo ancho: las puntas (bordes) se quedan en las orillas.
            foreach ((Frame piece, string path) in new[] { (wide[0], $"{folder}/Tablon.png"), (wide[1], $"{folder}/TablonRoto1.png"),
                         (wide[2], $"{folder}/TablonRoto2.png") })
            {
                int cap = Mathf.Min(piece.Core.Width / 6, piece.Core.Height * 2);
                WriteSingle(sheet, piece, path, ppu, new Vector4(cap, 0f, cap, 0f));
            }
            ComposeBrokenBothEnds(folder);
        }

        /// <summary>
        /// Arma TablonRotoAmbos.png: la punta rota de TablonRoto2 (izquierda), el centro y la punta rota de TablonRoto1
        /// (derecha), alineados por arriba. Las puntas quedan como bordes, así que se repite a lo ancho sin estirarlas.
        /// </summary>
        public static void ComposeBrokenBothEnds(string folder = Level1Folder)
        {
            string leftPath = $"{folder}/TablonRoto2.png", rightPath = $"{folder}/TablonRoto1.png";
            if (!File.Exists(leftPath) || !File.Exists(rightPath))
                return;
            var leftImporter = (TextureImporter)AssetImporter.GetAtPath(leftPath);
            var rightImporter = (TextureImporter)AssetImporter.GetAtPath(rightPath);
            Texture2D left = LoadPng(leftPath), right = LoadPng(rightPath);
            int rightCapStart = right.width - Mathf.RoundToInt(rightImporter.spriteBorder.z);
            // Se corta en la raya oscura entre dos tablas (después de cada punta) para que la unión no se note.
            int leftCap = SeamColumn(left, Mathf.RoundToInt(leftImporter.spriteBorder.x)) + 1;
            int middleStart = SeamColumn(right, Mathf.RoundToInt(rightImporter.spriteBorder.x)) + 1;

            int width = leftCap + (right.width - middleStart);
            int height = Mathf.Max(left.height, right.height);
            var pixels = new Color32[width * height];
            // Las filas van de abajo hacia arriba: se recorre cada imagen subiéndola para que las caras de arriba coincidan.
            Copy(left, 0, leftCap, 0, height - left.height);
            Copy(right, middleStart, right.width, leftCap, height - right.height);
            WritePng(BrokenBothPathIn(folder), pixels, width, height);
            ConfigureTexture(BrokenBothPathIn(folder), leftImporter.spritePixelsPerUnit, new Vector2(0.5f, 0.5f),
                new Vector4(leftCap, 0f, right.width - rightCapStart, 0f), multiple: false);
            UnityEngine.Object.DestroyImmediate(left);
            UnityEngine.Object.DestroyImmediate(right);

            void Copy(Texture2D source, int fromX, int toX, int targetX, int targetY)
            {
                Color32[] data = source.GetPixels32();
                for (int y = 0; y < source.height; y++)
                {
                    for (int x = fromX; x < toX; x++)
                        pixels[(y + targetY) * width + targetX + x - fromX] = data[y * source.width + x];
                }
            }
        }

        private static string BrokenBothPathIn(string folder) => $"{folder}/TablonRotoAmbos.png";

        // La columna más oscura (la raya entre dos tablas) en la parte de arriba del tablón, poco después de "from".
        private static int SeamColumn(Texture2D texture, int from)
        {
            Color32[] data = texture.GetPixels32();
            int best = from;
            float darkest = float.MaxValue;
            for (int x = from; x < Mathf.Min(from + 90, texture.width); x++)
            {
                float sum = 0f;
                int count = 0;
                // Filas de las tablas de arriba (las filas van de abajo hacia arriba).
                for (int y = texture.height / 2; y < texture.height * 85 / 100; y++)
                {
                    Color32 pixel = data[y * texture.width + x];
                    if (pixel.a < 128)
                        continue;
                    sum += pixel.r + pixel.g + pixel.b;
                    count++;
                }
                if (count > 0 && sum / count < darkest)
                {
                    darkest = sum / count;
                    best = x;
                }
            }
            return best;
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        // ---------- Fondo del tutorial ----------

        private const string TutorialArtSource = "Sprites-por-revisar/Niveles/Tutorial/";
        public const string TutorialBackgroundFolder = TutorialFolder + "/Fondo";
        public const string PaperPath = TutorialBackgroundFolder + "/Papel.png";
        public const string HorizonPath = TutorialBackgroundFolder + "/Horizonte.png";
        public const string SunPath = TutorialBackgroundFolder + "/Sol.png";
        public const string SunControllerPath = TutorialBackgroundFolder + "/Sol.controller";
        public const string SandPath = TutorialBackgroundFolder + "/Arena.png";
        public const string Sand2Path = TutorialBackgroundFolder + "/Arena2.png";
        public const string PierPath = TutorialBackgroundFolder + "/Muelle.png";
        public const string Palm1Path = TutorialBackgroundFolder + "/Palmera1.png";
        public const string Palm2Path = TutorialBackgroundFolder + "/Palmera2.png";
        public const string BoatPath = TutorialBackgroundFolder + "/Panga.png";
        public const string MooringPostPath = TutorialBackgroundFolder + "/Poste.png";
        public const string RocksPath = TutorialBackgroundFolder + "/Rocas.png";
        public const string GrassPath = TutorialBackgroundFolder + "/Pasto.png";
        // Obstáculos de arena (cubo de la caja de salto, torre del doble salto y bordillo del cocodrilo).
        public const string CubePath = TutorialFolder + "/Cubo.png";
        public const string TowerPath = TutorialFolder + "/Torre.png";
        public const string CurbPath = TutorialFolder + "/Bordillo.png";
        // Alto de cada uno hasta su parte de arriba plana (= alto de su collider en el tutorial).
        public const float CubeHeight = 1.8f;
        public const float TowerHeight = 4.5f;
        public const float CurbHeight = 0.9f;
        // Lo que la base (la arena amontonada) se hunde bajo el suelo; la orilla de la arena, dibujada delante,
        // la tapa un poco para que se vea enterrada y no pegada encima.
        public const float ObstacleSink = 0.12f;
        // Letrero de madera: ancho de la tabla. El pivote queda en el centro de su cara clara, y la cara se guarda
        // como borde del sprite para acomodar encima el título y los botones.
        public const string SignPath = TutorialFolder + "/Letrero.png";
        private const float SignWidth = 4.2f;
        // Muñeco de práctica: tira de 3 cuadros (quieto, golpeado, noqueado) con los pies (la arena del palo) en el
        // mismo punto; alto del cuadro quieto. Erizos: franja que hace daño, ancho total del dibujo.
        public const string DummyPath = TutorialFolder + "/Muneco.png";
        public const float DummyHeight = UlisesStandHeight; // De la altura de Ulises.
        public const string UrchinsPath = TutorialFolder + "/Erizos.png";
        private const float UrchinsWidth = 5f;
        // Meta: el carrito de pan de plátano con el letrero "¡META!", alto total (hasta la punta del banderín);
        // su montón de arena se hunde un poco más que los obstáculos para que asome detrás de la orilla.
        public const string GoalPath = TutorialFolder + "/Meta.png";
        private const float GoalHeight = 4.2f;
        public const float GoalSink = 0.3f;

        // Tamaños en el juego (unidades). El papel cubre la vista (la cámara ve ~25 x 14).
        public const float PaperSize = 26f;
        public const float HorizonHeight = 16f;
        // Altura de la línea del mar en la imagen del horizonte (fracción desde abajo): ahí va el pivote.
        private const float HorizonSeaLine = 0.193f;
        // Parte de arriba del horizonte que se desvanece para fundirse con el papel.
        private const float HorizonFade = 0.15f;
        private const float SunHeight = 2.6f;
        private const float SunFps = 3f;
        // La arena llega hasta abajo de la vista (la cámara no baja de y = 2.5 con tamaño 7).
        public const float SandHeight = 4.6f;
        private const float PierHeight = 5f;
        private const float PalmHeight = 6.5f;
        private const float BoatHeight = 2.6f;
        private const float MooringPostHeight = 1.6f;
        private const float RocksHeight = 1.2f;
        private const float GrassHeight = 1.6f;

        /// <summary>
        /// Fondo por capas del tutorial (tutorial_*.png, ya con transparencia): papel, horizonte, sol animado,
        /// arena (con el pivote en la línea donde se camina) y piezas sueltas con el pivote abajo al centro.
        /// </summary>
        private static void ImportTutorialBackground()
        {
            if (ReadImage("tutorial_papel.png") is not { } paper)
                return;
            EnsureFolder(TutorialBackgroundFolder);
            WriteRegion(paper, OpaqueBounds(paper), PaperPath, paper.Width / PaperSize, new Vector2(0.5f, 0.5f));

            if (ReadImage("tutorial_horizonte.png") is { } horizon)
            {
                int fadeRows = Mathf.RoundToInt(horizon.Height * HorizonFade);
                for (int y = horizon.Height - fadeRows; y < horizon.Height; y++)
                {
                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.SmoothStep(1f, 0f, (y - (horizon.Height - fadeRows)) / (float)fadeRows));
                    for (int x = 0; x < horizon.Width; x++)
                        horizon.Pixels[y * horizon.Width + x].a = alpha;
                }
                WriteRegion(horizon, new RectInt(0, 0, horizon.Width, horizon.Height), HorizonPath,
                    horizon.Height / HorizonHeight, new Vector2(0.5f, HorizonSeaLine), padding: 0);
            }

            if (ReadImage("tutorial_sol.png") is { } sun)
                ImportSun(sun);

            Sheet baseSand = null;
            float baseWalk = 0f;
            foreach ((string file, string path) in new[] { ("tutorial_arena.png", SandPath), ("tutorial_arena2.png", Sand2Path) })
            {
                if (ReadImage(file) is not { } sand)
                    continue;
                float walk = WalkLine(sand, OpaqueBounds(sand));
                if (baseSand == null)
                {
                    baseSand = sand;
                    baseWalk = walk;
                }
                else
                {
                    // Las variantes toman el tono de la primera y sus orillas se funden con las de ella:
                    // así quedan pegadas a una tira normal sin que se note la costura.
                    MatchColors(sand, baseSand);
                    BlendEdges(sand, baseSand, Mathf.RoundToInt(walk - baseWalk));
                }
                RectInt bounds = OpaqueBounds(sand);
                // Sin borde vacío a los lados: las tiras van una junto a otra sin costura.
                WriteRegion(sand, bounds, path, bounds.height / SandHeight,
                    new Vector2(0.5f, (walk - bounds.yMin) / bounds.height), padding: 0);
            }

            WriteObstacle("tutorial_cubo.png", CubePath, CubeHeight);
            WriteObstacle("tutorial_torre.png", TowerPath, TowerHeight);
            WriteObstacle("tutorial_bordillo.png", CurbPath, CurbHeight);
            WriteSign();
            WriteDummy();
            if (ReadImage("tutorial_erizos.png") is { } urchins)
            {
                RectInt bounds = OpaqueBounds(urchins);
                // Van delante de la arena (tienen que verse bien: hacen daño), apenas hundidos.
                float ppu = bounds.width / UrchinsWidth;
                WriteRegion(urchins, bounds, UrchinsPath, ppu, new Vector2(0.5f, 0.04f * ppu / bounds.height));
            }

            if (ReadImage("tutorial_meta.png") is { } goal)
            {
                RectInt bounds = OpaqueBounds(goal);
                float ppu = bounds.height / GoalHeight;
                WriteRegion(goal, bounds, GoalPath, ppu, new Vector2(0.5f, GoalSink * ppu / bounds.height));
            }

            WritePiece("tutorial_muelle.png", PierPath, PierHeight);
            WritePiece("tutorial_panga.png", BoatPath, BoatHeight);
            WritePiece("tutorial_poste.png", MooringPostPath, MooringPostHeight);
            WritePiece("tutorial_rocas.png", RocksPath, RocksHeight);
            WritePiece("tutorial_pasto.png", GrassPath, GrassHeight);
            // Las dos palmeras vienen en la misma imagen: se separan por la columna vacía entre ellas.
            if (ReadImage("tutorial_palmeras.png") is { } palms)
            {
                List<RectInt> parts = SplitByGaps(palms, OpaqueBounds(palms));
                float ppu = parts.Max(p => p.height) / PalmHeight;
                foreach ((RectInt part, string path) in parts.Take(2).Zip(new[] { Palm1Path, Palm2Path }, (p, n) => (p, n)))
                    WriteRegion(palms, part, path, ppu, new Vector2(0.5f, 0f));
            }
        }

        // Línea donde se camina: la mediana del borde de arriba (las olitas suben y bajan alrededor).
        private static float WalkLine(Sheet sand, RectInt bounds)
        {
            var tops = new List<float>();
            for (int x = bounds.xMin; x < bounds.xMax; x += 8)
            {
                for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
                {
                    if (sand.Pixels[y * sand.Width + x].a > 128)
                    {
                        tops.Add(y);
                        break;
                    }
                }
            }
            return Median(tops);
        }

        // Funde los extremos de image (una quinta parte de cada lado) con los de reference, desplazada offsetY
        // para que coincidan sus líneas de suelo. Ambas deben medir lo mismo de ancho.
        private static void BlendEdges(Sheet image, Sheet reference, int offsetY)
        {
            if (image.Width != reference.Width)
            {
                Debug.LogWarning("[Odisea] Las tiras de arena no miden lo mismo de ancho; no se funden sus orillas.");
                return;
            }
            int fade = image.Width / 5;
            for (int x = 0; x < image.Width; x++)
            {
                int distance = Mathf.Min(x, image.Width - 1 - x);
                if (distance >= fade)
                    continue;
                float own = Mathf.SmoothStep(0f, 1f, distance / (float)fade);
                for (int y = 0; y < image.Height; y++)
                {
                    int ry = y - offsetY;
                    Color32 other = ry >= 0 && ry < reference.Height ? reference.Pixels[ry * reference.Width + x] : default;
                    int i = y * image.Width + x;
                    image.Pixels[i] = Color32.Lerp(other, image.Pixels[i], own);
                }
            }
        }

        // Ajusta cada canal (media y desviación) de lo visible de image al de reference.
        private static void MatchColors(Sheet image, Sheet reference)
        {
            (Vector3 mean, Vector3 deviation) Stats(Sheet sheet)
            {
                Vector3 sum = Vector3.zero, squares = Vector3.zero;
                int count = 0;
                foreach (Color32 c in sheet.Pixels)
                {
                    if (c.a < 200)
                        continue;
                    var v = new Vector3(c.r, c.g, c.b);
                    sum += v;
                    squares += Vector3.Scale(v, v);
                    count++;
                }
                Vector3 mean = sum / Mathf.Max(1, count);
                Vector3 variance = squares / Mathf.Max(1, count) - Vector3.Scale(mean, mean);
                return (mean, new Vector3(Mathf.Sqrt(variance.x), Mathf.Sqrt(variance.y), Mathf.Sqrt(variance.z)));
            }

            (Vector3 from, Vector3 fromDeviation) = Stats(image);
            (Vector3 to, Vector3 toDeviation) = Stats(reference);
            byte Map(byte value, int channel) => (byte)Mathf.Clamp(Mathf.RoundToInt(
                (value - from[channel]) * toDeviation[channel] / Mathf.Max(1f, fromDeviation[channel]) + to[channel]), 0, 255);
            for (int i = 0; i < image.Pixels.Length; i++)
            {
                Color32 c = image.Pixels[i];
                image.Pixels[i] = new Color32(Map(c.r, 0), Map(c.g, 1), Map(c.b, 2), c.a);
            }
        }

        /// <summary>
        /// Obstáculo que se pisa: la parte de arriba plana (la primera fila que ocupa casi todo el ancho, sin la
        /// banderita) queda a height sobre el suelo, la base un poco hundida, y el pivote en el centro del cuerpo.
        /// </summary>
        private static void WriteObstacle(string file, string path, float height)
        {
            if (ReadImage(file) is not { } image)
                return;
            RectInt bounds = OpaqueBounds(image);
            int Span(int y, out int left)
            {
                left = -1;
                int right = -1;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    if (image.Pixels[y * image.Width + x].a <= 128)
                        continue;
                    if (left < 0)
                        left = x;
                    right = x;
                }
                return left < 0 ? 0 : right - left + 1;
            }

            int top = bounds.yMax - 1;
            while (top > bounds.yMin && Span(top, out _) < bounds.width * 0.4f)
                top--;
            float ppu = (top - bounds.yMin + 1) / (height + ObstacleSink);
            // Centro del cuerpo a media altura (la arena de la base puede estar más de un lado).
            int middle = (top + bounds.yMin) / 2;
            int width = Span(middle, out int middleLeft);
            float centerX = middleLeft + width / 2f - bounds.xMin;
            WriteRegion(image, bounds, path, ppu, new Vector2(centerX / bounds.width, ObstacleSink * ppu / bounds.height));
        }

        // Cuadros del muñeco separados por columnas vacías; cada uno se ancla en el centro de la arena de su palo
        // (la fila de más abajo), así el palo no "brinca" al cambiar de cuadro.
        private static void WriteDummy()
        {
            if (ReadImage("tutorial_muneco.png") is not { } sheet)
                return;
            List<RectInt> frames = SplitByGaps(sheet, OpaqueBounds(sheet), minGap: 20);
            if (frames.Count != 3)
            {
                Debug.LogWarning($"[Odisea] tutorial_muneco.png: se esperaban 3 cuadros y hay {frames.Count}.");
                if (frames.Count == 0)
                    return;
            }

            var anchors = new List<int>();
            foreach (RectInt frame in frames)
            {
                // Centro de lo que ocupa la franja de abajo (la arena), no del cuadro: los brazos lo descuadran.
                int band = Mathf.Max(1, frame.height / 30), left = int.MaxValue, right = int.MinValue;
                for (int y = frame.yMin; y < frame.yMin + band; y++)
                for (int x = frame.xMin; x < frame.xMax; x++)
                {
                    if (sheet.Pixels[y * sheet.Width + x].a <= 128)
                        continue;
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                }
                anchors.Add(left <= right ? (left + right) / 2 : frame.xMin + frame.width / 2);
            }

            int half = frames.Select((f, i) => Mathf.Max(anchors[i] - f.xMin, f.xMax - anchors[i])).Max() + Padding;
            int cellWidth = half * 2;
            int cellHeight = frames.Max(f => f.height) + Padding * 2;
            int stripWidth = cellWidth * frames.Count;
            var pixels = new Color32[stripWidth * cellHeight];
            for (int i = 0; i < frames.Count; i++)
            {
                RectInt frame = frames[i];
                for (int y = 0; y < frame.height; y++)
                for (int x = 0; x < frame.width; x++)
                {
                    int tx = i * cellWidth + half + (frame.xMin + x - anchors[i]);
                    pixels[(Padding + y) * stripWidth + tx] = sheet.Pixels[(frame.yMin + y) * sheet.Width + frame.xMin + x];
                }
            }

            float ppu = frames[0].height / (DummyHeight + ObstacleSink);
            var pivot = new Vector2(0.5f, (Padding + ObstacleSink * ppu) / cellHeight);
            Dictionary<string, GUID> previousIds = TakeSpriteIds(DummyPath);
            WritePng(DummyPath, pixels, stripWidth, cellHeight);
            ConfigureTexture(DummyPath, ppu, pivot, Vector4.zero, frames.Count > 1);
            if (frames.Count > 1)
                SliceGrid(DummyPath, frames.Count, cellWidth, cellHeight, pivot, "Muneco", previousIds);
        }

        // La tabla son las filas que ocupan casi todo el ancho; su cara, la zona clara que rodea su centro.
        private static void WriteSign()
        {
            if (ReadImage("tutorial_letrero.png") is not { } image)
                return;
            RectInt bounds = OpaqueBounds(image);
            int boardBottom = -1, boardTop = -1, boardLeft = bounds.xMax, boardRight = bounds.xMin;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                // Se cuentan los píxeles (no de punta a punta) para que las migajas de la base no parezcan tabla.
                int left = -1, right = -1, filled = 0;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    if (image.Pixels[y * image.Width + x].a <= 128)
                        continue;
                    if (left < 0)
                        left = x;
                    right = x;
                    filled++;
                }
                if (filled < bounds.width * 0.6f)
                    continue;
                if (boardBottom < 0)
                    boardBottom = y;
                boardTop = y;
                boardLeft = Mathf.Min(boardLeft, left);
                boardRight = Mathf.Max(boardRight, right);
            }
            if (boardBottom < 0)
            {
                Debug.LogWarning("[Odisea] tutorial_letrero.png: no se encontró la tabla.");
                return;
            }

            RectInt face = LightArea(image, new Vector2Int((boardLeft + boardRight) / 2, (boardBottom + boardTop) / 2));
            float ppu = (boardRight - boardLeft + 1) / SignWidth;
            var pivot = new Vector2((face.center.x - bounds.xMin) / bounds.width, (face.center.y - bounds.yMin) / bounds.height);
            WriteRegion(image, bounds, SignPath, ppu, pivot, inner: face);
        }

        // Rectángulo de la zona clara (parecida en luz al punto de partida) conectada con seed.
        private static RectInt LightArea(Sheet image, Vector2Int seed)
        {
            static float Light(Color32 c) => (c.r + c.g + c.b) / 3f;
            float threshold = Light(image.Pixels[seed.y * image.Width + seed.x]) - 35f;
            var visited = new bool[image.Pixels.Length];
            var pending = new Stack<Vector2Int>();
            pending.Push(seed);
            visited[seed.y * image.Width + seed.x] = true;
            int minX = seed.x, maxX = seed.x, minY = seed.y, maxY = seed.y;
            while (pending.Count > 0)
            {
                Vector2Int p = pending.Pop();
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                foreach (Vector2Int step in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                {
                    Vector2Int n = p + step;
                    if (n.x < 0 || n.y < 0 || n.x >= image.Width || n.y >= image.Height)
                        continue;
                    int i = n.y * image.Width + n.x;
                    Color32 c = image.Pixels[i];
                    if (visited[i] || c.a <= 128 || Light(c) < threshold)
                        continue;
                    visited[i] = true;
                    pending.Push(n);
                }
            }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void WritePiece(string file, string path, float height)
        {
            if (ReadImage(file) is not { } image)
                return;
            RectInt bounds = OpaqueBounds(image);
            WriteRegion(image, bounds, path, bounds.height / height, new Vector2(0.5f, 0f));
        }

        // Cuadros del sol en una fila: misma celda para todos, centrados, y un clip en bucle con su Animator.
        private static void ImportSun(Sheet sheet)
        {
            List<RectInt> frames = SplitByGaps(sheet, OpaqueBounds(sheet));
            int cellWidth = frames.Max(f => f.width) + Padding * 2;
            int cellHeight = frames.Max(f => f.height) + Padding * 2;
            int stripWidth = cellWidth * frames.Count;
            var pixels = new Color32[stripWidth * cellHeight];
            for (int i = 0; i < frames.Count; i++)
            {
                RectInt frame = frames[i];
                int offsetX = i * cellWidth + (cellWidth - frame.width) / 2;
                int offsetY = (cellHeight - frame.height) / 2;
                for (int y = 0; y < frame.height; y++)
                for (int x = 0; x < frame.width; x++)
                    pixels[(offsetY + y) * stripWidth + offsetX + x] = sheet.Pixels[(frame.yMin + y) * sheet.Width + frame.xMin + x];
            }

            Dictionary<string, GUID> previousIds = TakeSpriteIds(SunPath);
            WritePng(SunPath, pixels, stripWidth, cellHeight);
            var pivot = new Vector2(0.5f, 0.5f);
            ConfigureTexture(SunPath, (cellHeight - Padding * 2) / SunHeight, pivot, Vector4.zero, frames.Count > 1);
            if (frames.Count > 1)
                SliceGrid(SunPath, frames.Count, cellWidth, cellHeight, pivot, "Sol", previousIds);

            AnimationClip clip = CreateClip(TutorialBackgroundFolder + "/Sol.anim", LoadStrip(SunPath), SunFps, loop: true);
            AnimatorController controller = ResetController(SunControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState state = machine.AddState("Sol");
            state.motion = clip;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
        }

        /// <summary>Imagen ya recortada con transparencia (sin quitar fondo ni buscar manchas). null si no existe.</summary>
        private static Sheet ReadImage(string fileName)
        {
            string path = Path.Combine(SourceFolder, TutorialArtSource + fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Odisea] No se encontró {path}.");
                return null;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            var sheet = new Sheet { Width = texture.width, Height = texture.height, Pixels = texture.GetPixels32() };
            UnityEngine.Object.DestroyImmediate(texture);
            return sheet;
        }

        // Rectángulo que encierra lo que no es transparente (dentro de area, o de toda la imagen).
        private static RectInt OpaqueBounds(Sheet sheet, RectInt? area = null)
        {
            RectInt region = area ?? new RectInt(0, 0, sheet.Width, sheet.Height);
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = region.yMin; y < region.yMax; y++)
            {
                for (int x = region.xMin; x < region.xMax; x++)
                {
                    if (sheet.Pixels[y * sheet.Width + x].a <= 24)
                        continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            return maxX < 0 ? region : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // Separa dibujos puestos uno junto a otro por las columnas vacías entre ellos (cada uno, recortado).
        private static List<RectInt> SplitByGaps(Sheet sheet, RectInt bounds, int minGap = 8)
        {
            var parts = new List<RectInt>();
            int start = -1, empty = 0;
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                bool filled = false;
                if (x < bounds.xMax)
                {
                    for (int y = bounds.yMin; y < bounds.yMax && !filled; y++)
                        filled = sheet.Pixels[y * sheet.Width + x].a > 24;
                }
                if (filled)
                {
                    if (start < 0)
                        start = x;
                    empty = 0;
                }
                else if (start >= 0 && (++empty >= minGap || x == bounds.xMax))
                {
                    int end = x - empty + 1;
                    parts.Add(OpaqueBounds(sheet, new RectInt(start, bounds.yMin, end - start, bounds.height)));
                    start = -1;
                }
            }
            return parts;
        }

        // Copia un rectángulo de la imagen a su propio sprite (con borde vacío); pivot relativo al rectángulo.
        // inner (opcional): rectángulo de la imagen que se guarda como borde del sprite (p. ej. la cara del letrero).
        private static void WriteRegion(Sheet sheet, RectInt region, string path, float ppu, Vector2 pivot, int padding = Padding,
            RectInt? inner = null)
        {
            int width = region.width + padding * 2;
            int height = region.height + padding * 2;
            var pixels = new Color32[width * height];
            for (int y = 0; y < region.height; y++)
            for (int x = 0; x < region.width; x++)
                pixels[(y + padding) * width + x + padding] = sheet.Pixels[(region.yMin + y) * sheet.Width + region.xMin + x];
            WritePng(path, pixels, width, height);
            var spritePivot = new Vector2((padding + pivot.x * region.width) / width, (padding + pivot.y * region.height) / height);
            Vector4 border = inner is { } face
                ? new Vector4(padding + face.xMin - region.xMin, padding + face.yMin - region.yMin,
                    padding + region.xMax - face.xMax, padding + region.yMax - face.yMax)
                : Vector4.zero;
            ConfigureTexture(path, ppu, spritePivot, border, multiple: false);
        }

        // ---------- Lectura y limpieza ----------

        private sealed class Sheet
        {
            public int Width;
            public int Height;
            public Color32[] Pixels;
            public int[] Labels;
            public List<Blob> Blobs;
        }

        private sealed class Blob
        {
            public int Id;
            public int Area;
            public int MinX = int.MaxValue, MinY = int.MaxValue, MaxX = int.MinValue, MaxY = int.MinValue;
            public int Width => MaxX - MinX + 1;
            public int Height => MaxY - MinY + 1;
            public float CenterY => (MinY + MaxY) * 0.5f;

            public void Add(int x, int y)
            {
                Area++;
                if (x < MinX) MinX = x;
                if (x > MaxX) MaxX = x;
                if (y < MinY) MinY = y;
                if (y > MaxY) MaxY = y;
            }
        }

        /// <summary>Un cuadro: la mancha principal y las piezas sueltas que le pertenecen (destellos, polvo).</summary>
        private sealed class Frame
        {
            public Blob Core;
            // Cuerpo = la mancha principal más las piezas grandes pegadas (p. ej. piernas que quedaron separadas
            // del torso); sin polvo ni rayitas. Con él se mide la altura y se ponen los pies.
            public readonly Blob Body = new();
            public readonly HashSet<int> BlobIds = new();
            public int MinX, MinY, MaxX, MaxY;
            private int? centerAbovePx;
            private int? groundY;

            // Ancla = pies: centro horizontal y parte de abajo del cuerpo. Centrado: centro del cuerpo menos
            // la distancia a la que deben quedar los pies (así el pivote sigue estando "en los pies").
            public int AnchorX => (Body.MinX + Body.MaxX) / 2;
            public int AnchorY => groundY ?? (centerAbovePx.HasValue ? (Body.MinY + Body.MaxY) / 2 - centerAbovePx.Value : Body.MinY);

            public Frame(Blob core)
            {
                Core = core;
                BlobIds.Add(core.Id);
                MinX = core.MinX; MinY = core.MinY; MaxX = core.MaxX; MaxY = core.MaxY;
                Body.Area = core.Area;
                Body.MinX = MinX; Body.MinY = MinY; Body.MaxX = MaxX; Body.MaxY = MaxY;
            }

            public Frame WithCenterAnchor(int abovePx)
            {
                var copy = (Frame)MemberwiseClone();
                copy.centerAbovePx = abovePx;
                return copy;
            }

            /// <summary>Ancla en una línea de suelo común a toda la hoja (los cuadros en el aire conservan su salto).</summary>
            public Frame WithGroundAnchor(int y)
            {
                var copy = (Frame)MemberwiseClone();
                copy.groundY = y;
                return copy;
            }

            public void Attach(Blob blob)
            {
                BlobIds.Add(blob.Id);
                MinX = Mathf.Min(MinX, blob.MinX); MinY = Mathf.Min(MinY, blob.MinY);
                MaxX = Mathf.Max(MaxX, blob.MaxX); MaxY = Mathf.Max(MaxY, blob.MaxY);
                if (blob.Area >= Core.Area / 4)
                {
                    Body.MinX = Mathf.Min(Body.MinX, blob.MinX); Body.MinY = Mathf.Min(Body.MinY, blob.MinY);
                    Body.MaxX = Mathf.Max(Body.MaxX, blob.MaxX); Body.MaxY = Mathf.Max(Body.MaxY, blob.MaxY);
                }
            }
        }

        private static Sheet LoadSheet(string relativePath, int whiteThreshold = -1, bool warnIfMissing = true,
            bool removeGroundLine = false)
        {
            string path = Path.Combine(SourceFolder, relativePath);
            if (!File.Exists(path))
            {
                if (warnIfMissing)
                    Debug.LogWarning($"[Odisea] No se encontró {path}.");
                return null;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            var sheet = new Sheet { Width = texture.width, Height = texture.height, Pixels = texture.GetPixels32() };
            UnityEngine.Object.DestroyImmediate(texture);
            if (whiteThreshold > 0)
                RemoveWhiteBackground(sheet, whiteThreshold);
            if (removeGroundLine)
                RemoveGroundLine(sheet);
            LabelBlobs(sheet);
            return sheet;
        }

        private static void RemoveWhiteBackground(Sheet sheet, int threshold)
        {
            int width = sheet.Width, height = sheet.Height, count = width * height;
            Color32[] pixels = sheet.Pixels;
            var white = new bool[count];
            for (int i = 0; i < count; i++)
                white[i] = Mathf.Min(pixels[i].r, Mathf.Min(pixels[i].g, pixels[i].b)) >= threshold;

            // Regiones blancas (4 vecinos): son fondo si tocan la orilla de la imagen o si son huecos grandes.
            var background = new bool[count];
            var visited = new bool[count];
            var stack = new Stack<int>();
            var region = new List<int>();
            for (int start = 0; start < count; start++)
            {
                if (!white[start] || visited[start])
                    continue;
                region.Clear();
                bool touchesEdge = false;
                stack.Push(start);
                visited[start] = true;
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    region.Add(i);
                    int x = i % width, y = i / width;
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        touchesEdge = true;
                    TryPush(x - 1, y); TryPush(x + 1, y); TryPush(x, y - 1); TryPush(x, y + 1);
                }
                if (touchesEdge || region.Count >= EnclosedHoleMinArea)
                {
                    foreach (int i in region)
                        background[i] = true;
                }
            }

            void TryPush(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                    return;
                int i = y * width + x;
                if (white[i] && !visited[i])
                {
                    visited[i] = true;
                    stack.Push(i);
                }
            }

            // Orilla: se "despinta" el blanco de los píxeles claros pegados al fondo (hasta 2 px).
            var result = (Color32[])pixels.Clone();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (background[i])
                    {
                        result[i] = new Color32(pixels[i].r, pixels[i].g, pixels[i].b, 0);
                        continue;
                    }
                    if (!NearBackground(background, width, height, x, y, 2))
                        continue;
                    Color32 c = pixels[i];
                    int lightness = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    if (lightness <= FringeLightness)
                        continue;
                    float alpha = Mathf.Clamp01((255f - lightness) / (255f - FringeLightness));
                    result[i] = alpha <= 0.01f
                        ? new Color32(c.r, c.g, c.b, 0)
                        : new Color32(Unmix(c.r, alpha), Unmix(c.g, alpha), Unmix(c.b, alpha), (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            sheet.Pixels = result;
        }

        /// <summary>
        /// Quita una línea de suelo dibujada bajo todos los cuadros (filas que cruzan casi toda la hoja): si no,
        /// une a todos los cuadros en una sola mancha. Los pies que la tocaban pierden solo ese par de píxeles.
        /// </summary>
        private static void RemoveGroundLine(Sheet sheet)
        {
            int width = sheet.Width, height = sheet.Height;
            var lineRows = new List<int>();
            for (int y = 0; y < height; y++)
            {
                int opaque = 0;
                for (int x = 0; x < width; x++)
                {
                    if (sheet.Pixels[y * width + x].a > 24)
                        opaque++;
                }
                // La línea cruza ~95 % del ancho; los personajes juntos llegan a ~50 %.
                if (opaque > width * 0.85f)
                    lineRows.Add(y);
            }
            // Se limpia también un píxel arriba y abajo (la orilla suavizada de la línea).
            foreach (int row in lineRows)
            {
                for (int y = Mathf.Max(0, row - 1); y <= Mathf.Min(height - 1, row + 1); y++)
                {
                    for (int x = 0; x < width; x++)
                        sheet.Pixels[y * width + x].a = 0;
                }
            }
            if (lineRows.Count > 0)
                Debug.Log($"[Odisea] Línea de suelo quitada ({lineRows.Count} filas).");
        }

        private static bool NearBackground(bool[] background, int width, int height, int x, int y, int radius)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < width && ny < height && background[ny * width + nx])
                        return true;
                }
            }
            return false;
        }

        // Quita la mezcla con blanco: el color que, con esa transparencia sobre blanco, daría el píxel original.
        private static byte Unmix(byte channel, float alpha) =>
            (byte)Mathf.Clamp(Mathf.RoundToInt((channel - (1f - alpha) * 255f) / alpha), 0, 255);

        private static void LabelBlobs(Sheet sheet)
        {
            int width = sheet.Width, height = sheet.Height, count = width * height;
            var labels = new int[count];
            for (int i = 0; i < count; i++)
                labels[i] = -1;
            var blobs = new List<Blob>();
            var stack = new Stack<int>();
            for (int start = 0; start < count; start++)
            {
                if (labels[start] >= 0 || sheet.Pixels[start].a <= 24)
                    continue;
                var blob = new Blob { Id = blobs.Count };
                blobs.Add(blob);
                labels[start] = blob.Id;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    int x = i % width, y = i / width;
                    blob.Add(x, y);
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                                continue;
                            int n = ny * width + nx;
                            if (labels[n] < 0 && sheet.Pixels[n].a > 24)
                            {
                                labels[n] = blob.Id;
                                stack.Push(n);
                            }
                        }
                    }
                }
            }
            sheet.Labels = labels;
            sheet.Blobs = blobs;
        }

        /// <summary>
        /// Agrupa las manchas en cuadros (las grandes son cuerpos; las chicas se pegan al cuerpo más cercano)
        /// y los cuadros en filas, de arriba hacia abajo y de izquierda a derecha.
        /// </summary>
        private static List<List<Frame>> FindFrames(Sheet sheet, float coreFraction, int attachDistance)
        {
            int maxArea = sheet.Blobs.Max(b => b.Area);
            var frames = sheet.Blobs.Where(b => b.Area >= maxArea * coreFraction).Select(b => new Frame(b)).ToList();
            // Cada pieza suelta va al cuadro cercano (a attachDistance o menos) cuyo cuerpo tenga el centro más
            // próximo en horizontal: si una pieza toca a dos cuadros encimados (p. ej. las piernas de un cuadro que
            // quedaron separadas del torso), gana el que está realmente encima de ella.
            foreach (Blob blob in sheet.Blobs.Where(b => b.Area < maxArea * coreFraction))
            {
                Frame nearest = null;
                float best = float.MaxValue;
                float blobCenterX = (blob.MinX + blob.MaxX) * 0.5f;
                foreach (Frame frame in frames)
                {
                    int gapX = Mathf.Max(0, Mathf.Max(frame.Core.MinX - blob.MaxX, blob.MinX - frame.Core.MaxX));
                    int gapY = Mathf.Max(0, Mathf.Max(frame.Core.MinY - blob.MaxY, blob.MinY - frame.Core.MaxY));
                    if (Mathf.Max(gapX, gapY) > attachDistance)
                        continue;
                    float distance = Mathf.Abs((frame.Core.MinX + frame.Core.MaxX) * 0.5f - blobCenterX);
                    if (distance < best)
                    {
                        best = distance;
                        nearest = frame;
                    }
                }
                nearest?.Attach(blob);
            }

            float tolerance = 0.5f * Median(frames.Select(f => (float)f.Core.Height).ToList());
            var rows = new List<List<Frame>>();
            foreach (Frame frame in frames.OrderByDescending(f => f.Core.CenterY))
            {
                List<Frame> row = rows.LastOrDefault();
                if (row == null || Mathf.Abs(row[0].Core.CenterY - frame.Core.CenterY) > tolerance)
                    rows.Add(row = new List<Frame>());
                row.Add(frame);
            }
            foreach (List<Frame> row in rows)
                row.Sort((a, b) => a.Core.MinX.CompareTo(b.Core.MinX));
            return rows;
        }

        /// <summary>Hoja de una sola fila (convención nueva). false si no existe o tiene menos cuadros de los esperados.</summary>
        private static bool TryLoadRow(string fileName, int attachDistance, int minFrames, out Sheet sheet, out List<Frame> frames,
            bool removeGroundLine = false, float coreFraction = 0.3f)
        {
            frames = null;
            sheet = LoadSheet(UlisesFolderSource + fileName, warnIfMissing: false, removeGroundLine: removeGroundLine);
            if (sheet == null)
                return false;
            frames = FindFrames(sheet, coreFraction, attachDistance).SelectMany(r => r)
                .OrderBy(f => f.Core.MinX).ToList();
            Debug.Log($"[Odisea] {fileName}: {frames.Count} cuadros.");
            if (frames.Count >= minFrames)
                return true;
            Debug.LogWarning($"[Odisea] {fileName} tiene {frames.Count} cuadros y se esperaban al menos {minFrames}; se usa el sustituto.");
            return false;
        }

        private static List<Frame> PickFrames(List<Frame> frames, params int[] indices) =>
            indices.Select(i => frames[Mathf.Min(i, frames.Count - 1)]).ToList();

        // Cuadros en el aire: el ancla es el centro del cuerpo, puesto a BodyCenterAboveFeet sobre los pies.
        private static List<Frame> Centered(List<Frame> frames, float ppu, params int[] indices)
        {
            int abovePx = Mathf.RoundToInt(BodyCenterAboveFeet * ppu);
            return PickFrames(frames, indices).Select(f => f.WithCenterAnchor(abovePx)).ToList();
        }

        private static List<Frame> Pick(List<List<Frame>> rows, int row, params int[] indices) =>
            indices.Select(i => rows[row][Mathf.Min(i, rows[row].Count - 1)]).ToList();

        private static float MedianCoreHeight(List<Frame> frames) => Median(frames.Select(f => (float)f.Body.Height).ToList());

        private static float Median(List<float> values)
        {
            values.Sort();
            return values.Count == 0 ? 1f : values[values.Count / 2];
        }

        // ---------- Escritura de sprites ----------

        /// <summary>Sprites de una tira, en orden (Nombre_0, Nombre_1...).</summary>
        public static Sprite[] LoadStrip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => s.name.Length).ThenBy(s => s.name, StringComparer.Ordinal).ToArray();

        /// <summary>Tira horizontal de cuadros del mismo tamaño, con los pies de todos en el mismo punto (el pivote).</summary>
        private static List<Sprite> WriteStrip(Sheet sheet, List<Frame> frames, string path, float ppu, string spriteName)
        {
            int left = frames.Max(f => f.AnchorX - f.MinX);
            int right = frames.Max(f => f.MaxX - f.AnchorX);
            int up = Mathf.Max(0, frames.Max(f => f.MaxY - f.AnchorY));
            int down = Mathf.Max(0, frames.Max(f => f.AnchorY - f.MinY));
            int half = Mathf.Max(left, right) + Padding;
            int cellWidth = half * 2;
            int cellHeight = down + up + Padding * 2 + 1;
            int stripWidth = cellWidth * frames.Count;

            var pixels = new Color32[stripWidth * cellHeight];
            for (int i = 0; i < frames.Count; i++)
                CopyFrame(sheet, frames[i], pixels, stripWidth, i * cellWidth + half, Padding + down);

            // Los cortes viejos se quitan antes de reescribir la imagen (si la tira cambió de tamaño quedarían fuera
            // y Unity avisaría); sus IDs se guardan para reutilizarlos y que los clips no pierdan la referencia.
            Dictionary<string, GUID> previousIds = TakeSpriteIds(path);
            WritePng(path, pixels, stripWidth, cellHeight);
            var pivot = new Vector2(0.5f, (Padding + down) / (float)cellHeight);
            ConfigureTexture(path, ppu, pivot, Vector4.zero, frames.Count > 1);
            if (frames.Count > 1)
                SliceGrid(path, frames.Count, cellWidth, cellHeight, pivot, spriteName, previousIds);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => s.name.Length).ThenBy(s => s.name, StringComparer.Ordinal).ToList();
        }

        private static void WriteSingle(Sheet sheet, Frame frame, string path, float ppu, Vector4 border)
        {
            int width = frame.MaxX - frame.MinX + 1 + Padding * 2;
            int height = frame.MaxY - frame.MinY + 1 + Padding * 2;
            var pixels = new Color32[width * height];
            CopyFrame(sheet, frame, pixels, width, frame.AnchorX - frame.MinX + Padding, frame.AnchorY - frame.MinY + Padding);
            WritePng(path, pixels, width, height);
            ConfigureTexture(path, ppu, new Vector2(0.5f, 0.5f), border, multiple: false);
        }

        // Copia solo los píxeles de las manchas del cuadro (no lo que haya de otros cuadros cerca).
        private static void CopyFrame(Sheet sheet, Frame frame, Color32[] target, int targetWidth, int anchorX, int anchorY)
        {
            for (int y = frame.MinY; y <= frame.MaxY; y++)
            {
                for (int x = frame.MinX; x <= frame.MaxX; x++)
                {
                    int source = y * sheet.Width + x;
                    if (!frame.BlobIds.Contains(sheet.Labels[source]))
                        continue;
                    int tx = anchorX + (x - frame.AnchorX);
                    int ty = anchorY + (y - frame.AnchorY);
                    target[ty * targetWidth + tx] = sheet.Pixels[source];
                }
            }
        }

        private static void WritePng(string path, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureTexture(string path, float ppu, Vector2 pivot, Vector4 border, bool multiple)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // Rectángulo completo: hace falta para repetir (Tiled) tablones y escaleras.
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // Rebana la tira en cuadros; conserva el ID de cada sprite para que los clips no pierdan la referencia.
        private static ISpriteEditorDataProvider SpriteProvider(TextureImporter importer)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            return provider;
        }

        /// <summary>IDs de los cortes actuales (por nombre) y los quita de la textura. Vacío si aún no existe.</summary>
        private static Dictionary<string, GUID> TakeSpriteIds(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer || importer.spriteImportMode != SpriteImportMode.Multiple)
                return new Dictionary<string, GUID>();
            ISpriteEditorDataProvider provider = SpriteProvider(importer);
            Dictionary<string, GUID> ids = provider.GetSpriteRects()
                .GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First().spriteID);
            provider.SetSpriteRects(Array.Empty<SpriteRect>());
            provider.Apply();
            return ids;
        }

        private static void SliceGrid(string path, int count, int cellWidth, int cellHeight, Vector2 pivot, string baseName,
            Dictionary<string, GUID> previous)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            ISpriteEditorDataProvider provider = SpriteProvider(importer);

            var rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
            {
                string name = $"{baseName}_{i}";
                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(i * cellWidth, 0, cellWidth, cellHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = previous.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                };
            }
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        // ---------- Animación ----------

        private static AnimationClip CreateClip(string path, IList<Sprite> sprites, float fps, bool loop, float holdLast = 0f)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew)
                clip = new AnimationClip();
            clip.frameRate = fps;

            // Cada cuadro dura 1/fps; la última llave repite el último cuadro para que también dure lo suyo (más la pausa).
            var keys = new List<ObjectReferenceKeyframe>();
            for (int i = 0; i < sprites.Count; i++)
                keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] });
            keys.Add(new ObjectReferenceKeyframe { time = sprites.Count / fps + holdLast, value = sprites[^1] });
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNew)
                AssetDatabase.CreateAsset(clip, path);
            else
                EditorUtility.SetDirty(clip);
            return clip;
        }

        // Reutiliza el asset (y su GUID) si ya existe, vaciándolo; así la escena no pierde la referencia.
        private static AnimatorController ResetController(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                return AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (AnimatorControllerParameter parameter in controller.parameters)
                controller.RemoveParameter(parameter);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
                machine.RemoveAnyStateTransition(transition);
            foreach (ChildAnimatorState child in machine.states)
                machine.RemoveState(child.state);
            return controller;
        }

        private readonly struct Condition
        {
            public readonly string Parameter;
            public readonly AnimatorConditionMode Mode;
            public readonly float Threshold;

            public Condition(string parameter, AnimatorConditionMode mode, float threshold)
            {
                Parameter = parameter;
                Mode = mode;
                Threshold = threshold;
            }
        }

        private static Condition Cond(string parameter, bool value) =>
            new(parameter, value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f);

        private static Condition Cond(string parameter, AnimatorConditionMode mode, float threshold) => new(parameter, mode, threshold);

        // Transición inmediata (los sprites no se mezclan) en cuanto se cumplen las condiciones.
        private static void Link(AnimatorState from, AnimatorState to, params Condition[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            foreach (Condition condition in conditions)
                transition.AddCondition(condition.Mode, condition.Threshold, condition.Parameter);
        }

        private static void AnyState(AnimatorStateMachine machine, AnimatorState to, string trigger, bool toSelf)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = toSelf;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void ExitWhenDone(AnimatorState from, AnimatorState to, params Condition[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
            foreach (Condition condition in conditions)
                transition.AddCondition(condition.Mode, condition.Threshold, condition.Parameter);
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
