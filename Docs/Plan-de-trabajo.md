# Plan: Base de desarrollo profesional para "La Odisea del Pan de Plátano"

> **Para retomar en otra sesión:** pídele a Claude *"lee `Docs/Plan-de-trabajo.md` y continúa con el Paso C"*.
>
> **Estado actual (21-oct-2026):** Paso A ✅ (carpetas + reglas LFS + `Assets/Welcome` eliminada, ya en la rama). Paso B ⏳ (el usuario copia sus referencias; puede hacerlo desde una sesión local de Claude Code en su carpeta de Unity en Windows). Paso C pendiente. Nota: desde la nube `lfs.github.com` está bloqueado; en sesión local no aplica.

## Contexto
Proyecto escolar, equipo de 5, juego de **plataformas 2D ilustrado** en Unity. El repo ya tiene un proyecto **Unity 6 (6000.6.6f1), plantilla 2D URP** con Input System, 2D Animation, PSD/Aseprite importer, Tilemap, Timeline y `.gitattributes` con Git LFS. Solo hay un commit ("Initial check-in"). El contenido creativo (historia, personajes, storyboards) está en PDFs.
Entregables: build jugable, GDD/documentación, evidencia de Git, presentación/video.
Objetivo de este primer paso: dejar la **estructura, las reglas y la documentación** para que el equipo y yo trabajemos de forma uniforme desde el día 1.

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
- Unity: Version Control = Visible Meta Files, Asset Serialization = Force Text (verificar en ProjectSettings), UnityYAMLMerge configurado.

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
- **Scripts (C#)**: los escribo yo completos (movimiento con Input System, cámara, salud, enemigos, checkpoints, diálogos, menús, guardado), con datos en ScriptableObjects para que diseño ajuste valores sin tocar código. Ustedes los prueban en el Editor (aquí no puedo ejecutar Unity) y me reportan errores de consola.
- **Sprites**: no puedo dibujar el arte ilustrado final; sí puedo: definir specs en `Pipeline-Arte.md`, generar sprites placeholder para prototipar, scripts de editor para importación/slicing automático, y configurar Sprite Atlas.
- **Animaciones**: Animator Controllers y máquinas de estado, el script que las maneja, guía de rigging con PSD Importer + 2D Animation (ideal para estilo ilustrado), y Timeline para cinemáticas de los storyboards.
- **Gestión**: crear issues por tarea del roadmap, revisar PRs, mantener bitácora y changelog, ayudar con la presentación final.

## 6. Orden de trabajo (al aprobar el plan)
**Paso A — yo (rápido):** en la rama `claude/pensive-pascal-l6lzhc` creo solo las carpetas vacías (`Docs/Referencias/...` y `Assets/_Project/...` con `.gitkeep`) + reglas LFS, y hago push. Así ya sabes exactamente dónde soltar cada archivo.

**Paso B — tú (Windows, proyecto creado con Unity Hub + GitHub):** la carpeta del proyecto en tu compu YA es el repo (ruta visible en Unity Hub → Projects → ⋯ → Show in Explorer). Cierra Unity → GitHub Desktop *Add local repository* con esa carpeta → Fetch → cambiar a rama `claude/pensive-pascal-l6lzhc` → copiar archivos → commit → push. Luego subes PDFs, paleta, diseños de personajes y sprites a esas carpetas (en esa misma rama, con un commit tipo `docs(referencias): agregar PDFs y arte de personajes`) y me avisas. Yo **no avanzo** hasta que me digas que ya están.

**Paso C — yo:**
1. Leer todo y generar `Docs/GDD/*.md` y `Docs/Pipeline-Arte.md` (con la paleta y specs reales de sus sprites).
2. Crear `AGENTS.md`, `CLAUDE.md`, `README.md`, `CONTRIBUTING.md`, `Docs/Roadmap.md`, `Docs/Bitacora.md`, plantillas de `.github/`.
3. Reorganizar sprites si hace falta, quitar `Assets/Welcome`.
4. Commit + push. Después, con tu permiso: crear `dev` y `qa` desde `main`, abrir PR de mi rama → `dev`, y crear issues del roadmap.

## Verificación
- Abrir el proyecto en Unity 6000.6.6f1 sin errores en consola tras el cambio de carpetas.
- `git lfs ls-files` muestra los PDFs/PNGs grandes.
- Un integrante hace una rama `feature/` de prueba y un PR a `dev` usando la plantilla.
