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
        private const string Level1Sheet = "Sprites-por-revisar/Niveles/Nivel 1/spritesheets/spritesheet_nivel1.jpg";
        // Mismas piezas solo con contorno (estilo boceto) para el tutorial.
        private const string TutorialSheet = "Sprites-por-revisar/Niveles/Tutorial/spritesheet_nivel1BN.png";
        public const string TutorialFolder = "Assets/_Project/Art/Environment/Tutorial";

        private const string UlisesFolder = "Assets/_Project/Art/Characters/Ulises";
        private const string DoughFolder = "Assets/_Project/Art/VFX/Masa";
        public const string DoughFlyPath = DoughFolder + "/Masa_Vuela.png";
        public const string DoughImpactPath = DoughFolder + "/Masa_Impacto.png";
        private const string CrocodileFolder = "Assets/_Project/Art/Characters/Enemigos/Cocodrilo";
        private const string Level1Folder = "Assets/_Project/Art/Environment/Nivel1";

        public const string UlisesControllerPath = UlisesFolder + "/Ulises.controller";
        public const string CrocodileControllerPath = CrocodileFolder + "/Cocodrilo.controller";
        public const string PostPath = Level1Folder + "/Viga.png";
        public const string LadderPath = Level1Folder + "/Escalera.png";
        public const string PlankPath = Level1Folder + "/Tablon.png";
        public const string BrokenPlankPath = Level1Folder + "/TablonRoto1.png";
        public const string BrokenPlank2Path = Level1Folder + "/TablonRoto2.png";

        // Tamaños en el juego (unidades). Ulises mide 1.6 de collider; el dibujo corriendo, con pelo, un poco más.
        private const float UlisesRunHeight = 1.8f;
        // Ulises parado. Cada hoja nueva viene a otra escala; se mide su cuadro parado y se lleva a esta altura.
        private const float UlisesStandHeight = 1.95f;
        // En el aire el dibujo se centra en el cuerpo (no en los pies) para que las vueltas no "brinquen":
        // el centro del cuerpo queda a esta altura sobre los pies del collider.
        private const float BodyCenterAboveFeet = 0.95f;
        private const float CrocodileHeight = 0.95f;
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
            ImportLevelPieces(Level1Sheet, whiteThreshold: 232, Level1Folder, "Nivel 1");
            ImportLevelPieces(TutorialSheet, whiteThreshold: -1, TutorialFolder, "Tutorial");
            AssetDatabase.SaveAssets();
            Debug.Log("[Odisea] Arte de la sandbox importado.");
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

            // Escalera (4 cuadros, de espaldas). Anclada en los pies (al pie de la escalera queda parado en el suelo);
            // solo avanza mientras sube o baja (parámetro ClimbSpeed del Animator).
            if (TryLoadRow("ulises_escalera.png", attachDistance: 20, minFrames: 4, out Sheet climbSheet, out List<Frame> climb))
                clips["Climb"] = StripClip(climbSheet, PickFrames(climb, 0, 1, 2, 3), idlePpu, "Climb", 8f, loop: true);
            else
                clips["Climb"] = StripClip(sheet, Pick(rows, 3, 2), ppu, "Climb", 8f, loop: true);

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

            // Caminar nuevo (2 filas de 6): solo la primera fila, que tiene el mismo tamaño en todos los cuadros;
            // en la segunda el cocodrilo cambia de tamaño entre cuadros y "palpitaría" al caminar.
            Sheet walkSheet = LoadSheet(CrocodileWalkSheet, warnIfMissing: false);
            List<List<Frame>> walkRows = walkSheet != null ? FindFrames(walkSheet, coreFraction: 0.3f, attachDistance: 15) : null;
            if (walkRows != null && walkRows[0].Count >= 4)
            {
                List<Frame> walk = walkRows[0];
                Debug.Log($"[Odisea] cocodrilo_caminar.png: {walk.Count} cuadros (fila de arriba).");
                ppu = MedianCoreHeight(walk) / CrocodileHeight;
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
                walkSprites = WriteStrip(sheet, frames, $"{CrocodileFolder}/Cocodrilo_Walk.png", ppu, "Cocodrilo_Walk");
            }
            AnimationClip walkClip = CreateClip($"{CrocodileFolder}/Cocodrilo_Walk.anim", walkSprites, 10f, loop: true);

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

            // Ancla = pies: centro horizontal y parte de abajo del cuerpo. Centrado: centro del cuerpo menos
            // la distancia a la que deben quedar los pies (así el pivote sigue estando "en los pies").
            public int AnchorX => (Body.MinX + Body.MaxX) / 2;
            public int AnchorY => centerAbovePx.HasValue ? (Body.MinY + Body.MaxY) / 2 - centerAbovePx.Value : Body.MinY;

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
