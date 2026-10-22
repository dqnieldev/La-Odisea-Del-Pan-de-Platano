# Plan: Base de desarrollo profesional para "La Odisea del Pan de Plátano"

> **Para retomar en otra sesión:** pídele a Claude *"lee `Docs/Plan-de-trabajo.md` y continúa con el Paso C"*.
>
> **Estado actual (21-oct-2026):** Paso A ✅ (carpetas + reglas LFS + `Assets/Welcome` eliminada, ya en `main`). Ramas `main`, `dev` y `qa` creadas; la rama temporal `claude/pensive-pascal-l6lzhc` se eliminó. El trabajo ahora es **local** (Windows, `D:\Proyectos Unity\La Odisea Del Pan de Platano`). Paso B ⏳ (el usuario copia sus referencias en una rama `docs/referencias` creada desde `dev`). Paso C pendiente.

## Contexto
Proyecto escolar, equipo de 5, juego **arcade multi-género 2D** (estilo cartoon *rubber hose*) en Unity. El repo ya tiene un proyecto **Unity 6 (6000.6.6f1), plantilla 2D URP** con Input System, 2D Animation, PSD/Aseprite importer, Tilemap, Timeline y `.gitattributes` con Git LFS. Solo hay un commit ("Initial check-in"). El contenido creativo (historia, personajes, storyboards) está en PDFs.
Entregables: build jugable, GDD/documentación, evidencia de Git, presentación/video.
Objetivo de este primer paso: dejar la **estructura, las reglas y la documentación** para que el equipo y yo trabajemos de forma uniforme desde el día 1.

## Decisiones tomadas
| Fecha | Decisión |
|---|---|
| 22-oct-2026 | **MVP = 2 niveles jugables + 3 escenarios**: (0) **Tutorial**, escenario donde se enseñan movimiento y mecánicas; (1) "El Mercado Vertical" (plataformas verticales); (2) "Carrera por La Tovara" (side-scroller). El tower defense de la panadería (Acto II) es **extra** solo si sobra tiempo. |
| 22-oct-2026 | **Entrega final: antes del 12-nov-2026.** Build congelada el 10-nov. |
| 22-oct-2026 | **Ulises sí golpea** (ataque cuerpo a cuerpo con el rodillo). |
| 22-oct-2026 | **En La Tovara el jugador controla la velocidad** (no es runner automático). Los dos niveles comparten el mismo controlador de plataformas; La Tovara agrega cronómetro, lodo, deslizarse y power-ups. |
| 22-oct-2026 | **Equipo:** Daniel y Eduardo (desarrollo), Emmanuel y Jorge (diseño), Axel (pendiente de rol). |

## Roadmap del MVP (22-oct → 11-nov)
| Semana | Desarrollo | Diseño |
|---|---|---|
| **1 · 22–28 oct**: Base jugable | Controlador de Ulises (correr, saltar, doble salto, golpe, recibir daño, vidas), cámara, flujo de escenas (`GameManager`), menú principal, escena Tutorial en graybox | Ulises con fondo transparente y frames alineados (idle, correr, saltar, caer, golpe, daño, agacharse/deslizar); íconos de ingredientes |
| **2 · 29 oct–4 nov**: Nivel 1 | Mercado: plataformas, proyectiles de Circe, ingredientes, Polifemo; HUD (vidas, puntos, tiempo); Game Over y Victoria; Tutorial con arte | Sprites jugables de Circe, piezas del mercado en PNG transparente, UI (botones, paneles, corazones) |
| **3 · 5–11 nov**: La Tovara + entrega | Manglares: cocodrilos, lodo, ramas bajas (deslizarse), aves, hoja de plátano (escudo), café de olla (velocidad), cronómetro; audio; pulido; build `.exe`, tag `v1.0` | Arte de manglares (fondo con capas, plataformas, enemigos), pantallas de victoria |

Propuesta de reparto (a confirmar): **Daniel**: Ulises, física y Nivel 1. **Eduardo**: menús, HUD, flujo de escenas, Tutorial y La Tovara. **Emmanuel**: personajes y animaciones. **Jorge**: escenarios y UI. **Axel**: QA, documentación, bitácora y video.

## 1. Cómo me pasas el contexto (dónde va cada cosa)
Regla: **lo que es referencia/diseño va en `Docs/`** (Unity no lo importa); **lo que entra al juego va en `Assets/_Project/Art/`**.

| Material | Dónde va |
|---|---|
| PDFs de historia, GDD, storyboards | `Docs/Referencias/PDFs/` |
| Paleta de colores (imagen/PDF/códigos HEX) | `Docs/Referencias/Paleta/` (yo luego creo `Assets/_Project/Art/Palette/` con la paleta como swatch de Unity y `Docs/GDD/05-arte-y-audio.md` con los HEX) |
| Diseños/concept art de personajes (hojas de modelo, bocetos) | `Docs/Referencias/Personajes/<NombrePersonaje>/` |
| Storyboards sueltos (imágenes) | `Docs/Referencias/Storyboards/` |
| **Sprites ya listos para el juego** (PNG/PSD/PSB) | `Assets/_Project/Art/Characters/<NombrePersonaje>/`, `Art/Environment/`, `Art/UI/` según corresponda |
| Si no sabes si un sprite es final | `Docs/Referencias/Sprites-por-revisar/` y yo lo clasifico |

- Se agregan reglas LFS para `*.pdf`, `*.psd`, `*.psb`, `*.png`, `*.jpg` (si no las cubre ya `.gitattributes`) para que el repo no se haga pesado.
- Yo leo todo (texto e imágenes) y lo convierto en Markdown en `Docs/GDD/`. A partir de ahí **el GDD en Markdown es la fuente de verdad**; los PDFs quedan como referencia original.

## 2. Documentos de gestión (Markdown en el repo)
| Archivo | Para qué |
|---|---|
| `AGENTS.md` (+ `CLAUDE.md` que solo lo importa) | Reglas para mí/cualquier IA: arquitectura, convenciones C#, estructura de carpetas, flujo Git, qué no tocar. Se lee automáticamente en cada sesión. |
| `README.md` | Qué es el juego, versión de Unity, cómo clonar (Git LFS), abrir y compilar. |
| `CONTRIBUTING.md` | Flujo de ramas, formato de commits, cómo abrir PR, checklist de revisión, cómo evitar conflictos en escenas. |
| `Docs/GDD/` | `00-resumen.md`, `01-historia.md`, `02-personajes.md`, `03-mecanicas.md`, `04-niveles.md`, `05-arte-y-audio.md`, `06-ui.md` (generados desde los PDFs). |
| `Docs/Roadmap.md` | Milestones (Prototipo → Vertical slice → Alpha → Beta → Entrega) con tareas y responsable. |
| `Docs/Bitacora.md` / `CHANGELOG.md` | Registro por sprint/versión → evidencia para la escuela. |
| `Docs/Pipeline-Arte.md` | Specs de sprites: resolución, PPU, pivotes, nombres, formato PSB para rigging, export de animaciones. |
| `.github/pull_request_template.md`, `.github/ISSUE_TEMPLATE/` | PRs e issues con formato uniforme. |

## 3. Flujo de ramas (main / dev / qa / feature)
```
feature/<area>-<descripcion>  ──PR──▶  dev  ──PR──▶  qa  ──PR──▶  main (+ tag vX.Y)
fix/<descripcion>             ──PR──▶  dev
hotfix/<descripcion>          ──PR──▶  main y dev
```
- `main`: solo versiones entregables, cada merge lleva tag (`v0.1-prototipo`…).
- `dev`: integración diaria. `qa`: congelado para pruebas antes de entregar.
- Protección de ramas en GitHub (lo configura el dueño del repo): PR obligatorio + 1 aprobación en `dev`/`qa`/`main`.
- Commits **Conventional Commits en español**: `feat(player): agregar salto doble`, `fix(ui): …`, `art(sprites): …`, `docs(gdd): …`.
- Regla anti-conflictos: **una persona por escena a la vez**; todo lo reutilizable va en **prefabs**; cada quien prueba en su propia escena `Assets/_Project/Scenes/Sandbox/<Nombre>.unity`.
- Unity: Version Control = Visible Meta Files, Asset Serialization = Force Text (✅ verificado, `m_SerializationMode: 2`), UnityYAMLMerge configurado.
- **UnityYAMLMerge es por computadora** (vive en `.git/config`, no se sube). Cada integrante lo configura una vez, con la ruta de su instalación de Unity:
  ```bash
  git config merge.unityyamlmerge.name "Unity SmartMerge (UnityYamlMerge)"
  git config merge.unityyamlmerge.driver "\"<RutaUnity>/Editor/Data/Tools/UnityYAMLMerge.exe\" merge -h -p --force --fallback none %O %B %A %A"
  git config merge.unityyamlmerge.recursive binary
  ```

## 4. Estructura de carpetas de Unity
```
Assets/_Project/
  Art/ (Characters/, Environment/, UI/, VFX/)
  Animations/ (Controllers/, Clips/)
  Audio/ (Music/, SFX/)
  Prefabs/  Scenes/ (Levels/, Menus/, Sandbox/)
  Scripts/ (Core/, Player/, Enemies/, Level/, UI/, Audio/)  + .asmdef
  ScriptableObjects/  Settings/
```
Se elimina la carpeta `Assets/Welcome` de la plantilla.

## 5. Cómo te ayudo con cada cosa
- **Scripts (C#)**: los escribo yo completos (movimiento con Input System, cámara, salud, enemigos, checkpoints, diálogos, menús, guardado), con datos en ScriptableObjects para que diseño ajuste valores sin tocar código. En sesión local puedo usar el Editor abierto (vía Unity CLI) para crear objetos y leer la consola; ustedes prueban el juego y me reportan lo que no se sienta bien.
- **Sprites**: no puedo dibujar el arte ilustrado final; sí puedo: definir specs en `Pipeline-Arte.md`, generar sprites placeholder para prototipar, scripts de editor para importación/slicing automático, y configurar Sprite Atlas.
- **Animaciones**: Animator Controllers y máquinas de estado, el script que las maneja, guía de rigging con PSD Importer + 2D Animation (ideal para estilo ilustrado), y Timeline para cinemáticas de los storyboards.
- **Gestión**: crear issues por tarea del roadmap, revisar PRs, mantener bitácora y changelog, ayudar con la presentación final.

## 6. Orden de trabajo (al aprobar el plan)
**Paso A — ✅ hecho:** carpetas vacías (`Docs/Referencias/...` y `Assets/_Project/...`), reglas LFS, `Assets/Welcome` eliminada, ramas `dev` y `qa` creadas desde `main`.

**Paso B — tú:** en tu computadora, desde la rama `dev`, crea la rama `docs/referencias`. Copia PDFs, paleta, diseños de personajes y sprites a las carpetas de la sección 1. Haz commit `docs(referencias): agregar PDFs y arte de personajes`, push y PR → `dev` (o pídeme que lo haga yo). Yo **no avanzo** con el Paso C hasta que me digas que ya están.

**Paso C — yo** (en una rama `docs/base-proyecto` desde `dev`):
1. Leer todo y generar `Docs/GDD/*.md` y `Docs/Pipeline-Arte.md` (con la paleta y specs reales de sus sprites).
2. Crear `AGENTS.md`, `CLAUDE.md`, `README.md`, `CONTRIBUTING.md`, `Docs/Roadmap.md`, `Docs/Bitacora.md`, plantillas de `.github/`.
3. Reorganizar sprites si hace falta.
4. Commit + push + PR → `dev`. Después, con tu permiso, crear issues del roadmap.

## Verificación
- Abrir el proyecto en Unity 6000.6.6f1 sin errores en consola tras el cambio de carpetas.
- `git lfs ls-files` muestra los PDFs/PNGs grandes.
- Un integrante hace una rama `feature/` de prueba y un PR a `dev` usando la plantilla.
