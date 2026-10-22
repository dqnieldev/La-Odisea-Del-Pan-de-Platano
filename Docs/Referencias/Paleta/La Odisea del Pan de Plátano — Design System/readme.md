# La Odisea del Pan de Plátano — Design System

Videojuego arcade multi-género (E10+) ambientado en **San Blas, Nayarit**. Ulises, joven panadero, reinterpreta *La Odisea* en clave laboral: recolecta ingredientes, defiende el horno y corre por los manglares para llevar el pan de plátano definitivo al Festival Gastronómico de la Riviera Nayarit y ganar el título de **Empleado del Mes**. Proyecto escolar del equipo de la Universidad Tecnológica de Nayarit (IDGS), motor Unity, PC (Windows .exe), 1080p / 60 FPS, teclado (WASD / flechas).

Este sistema se usa para generar **todos los assets, objetos, personajes y pantallas** del juego y de su app companion.

## Productos / superficies
1. **Juego (PC 1920×1080)** — Menú principal (Jugar, Opciones, Salir) → Mapa "La Ruta del Pan" (isométrico 2.5D, pergamino, camino de migas) → 3 niveles:
   - **Nivel 1 · El Mercado Vertical** — plataformas vertical tipo Doodle Jump/Donkey Kong. Recolectar harina, huevos, azúcar; clímax: el Plátano Macho. Enemigos: Madame Circe (cáscaras, cajas), Polifemo.
   - **Nivel 2 · El Asedio a la Panadería** — tower defense isométrico; barra de cocción; ratas, jejenes, cocodrilos, cangrejos ermitaños, mapaches, gaviotas, ayudantes de Circe. Ítems: leña, utensilios-trampa.
   - **Nivel 3 · Carrera por La Tovara** — runner side-scroller de perfil. Cocodrilos, lianas, ramas bajas, pelícanos/garzas. Ítems: hoja de plátano (escudo 1 golpe), café de olla (velocidad).
   - Pantallas de Game Over y Victoria.
2. **Companion Wiki (móvil)** — Top Bar + tarjetas de acción (Ver Historia, Personajes, Receta), lore, storyboards.

### Personajes
Ulises (El Panadero, héroe) · Don Lupe (Maestro Panadero, aliado) · Madame Circe (Chef Rival corporativa) · Polifemo (Estibador del Mercado, guardián).

## Fuentes entregadas
- `uploads/Interfaz gráfica.docx` — paleta, tipografías, iconografía, botones, logotipo, layout (fuente de verdad visual).
- `uploads/Historia del videojuego.docx` — prólogo + 3 actos, storyboard, personajes.
- `uploads/DOCUMENTO PROYECTO VIDEOJUEGO.docx` — requisitos, herramientas (Inkscape, VS Code, Unity), género, público, descripción técnica de niveles, hojas de personaje, arte de escenarios.
- `uploads/Documento Oscar.docx` — visión, MVP, requerimientos, historias de usuario HU-01…06, sprints.
- `uploads/La odisea del pan de plátano/odisea-4b-icono.svg`, `odisea-4b-marca.svg` — logotipo vectorial v4b.
- Imágenes extraídas de los .docx en `src_docs/media/` (incluye logos institucionales UT Nayarit / IDGS, no usados en la marca del juego).

---

## CONTENT FUNDAMENTALS
- **Idioma:** español de México. Todo el copy del juego va en español; los nombres propios locales se respetan (San Blas, La Tovara, Riviera Nayarit, huacales, café de olla, jejenes).
- **Tono:** épico-cómico. Mezcla lenguaje de epopeya ("El Decreto del Viejo Horno", "La Cueva del Mercado y el Fruto Prohibido") con humor slapstick y cotidiano ("Empleado del Mes", "¡Se te quemó el pan!"). Nunca violento ni grosero (E10+).
- **Persona:** se habla al jugador de **tú** ("Recolecta…", "Defiende el horno", "¡Que no te arrebaten el pan!"). La narración de lore va en tercera persona, pasado/presente literario.
- **Casing:** títulos en *Sentence case* con mayúscula en nombres propios ("Carrera por La Tovara"). Etiquetas/tags y eyebrows en MAYÚSCULAS con tracking (NIVEL 1 · MERCADO). Exclamaciones con signos de apertura: **¡…!** y **¿…?** siempre.
- **Estructura narrativa:** Prólogo → Acto I/II/III. Niveles se nombran "Nivel N: Nombre".
- **Botones:** verbos cortos en infinitivo o imperativo: *Jugar, Opciones, Salir, Continuar, Reintentar, ← Volver, Ver Historia*.
- **Emoji:** no se usan. Ni en UI ni en copy. Los símbolos son íconos Material (ver Iconografía); los únicos caracteres Unicode vistos en fuentes son "←" y "ⓘ" (se renderizan como íconos `arrow_back` / `info`).
- **Números:** puntuación con separador de miles ("12,450"), tiempo mm:ss ("02:47"), rangos con letra ("Rango S").
- Ejemplos: "La costa no tiene espacio para recetas humildes." · "Hojas de plátano mágicas que otorgan un escudo de un solo golpe." · "¡Empleado del Mes!"

## VISUAL FOUNDATIONS
- **Color:** paleta cálida y artesanal. *Amarillo Plátano* `#F5B731` = énfasis/CTA/estrellas/selección; *Café Panadería* `#2B1E16` reemplaza al negro para texto, íconos y contornos; *Crema Harina* `#FBF8F2` fondo base (nunca blanco puro de fondo); *Verde Manglar* `#2E5A44` secundario (aliados, ingredientes, éxito, enlaces); *Beige Masa* `#E8E0D2` superficies/bordes/inactivos; Top Bar `#EFE7D8`. El logotipo v4b usa **Terracota** `#D2703A` sobre **Corteza** `#1C130E` (el documento describe la versión en plátano sobre `#2B1E16`; ambas se incluyen). Adiciones para gameplay: Chile `#C8432B` (vidas/daño) y Mar `#2F8FA0` (agua/info), tomadas del tono de las ilustraciones.
- **Tipografía:** *Outfit* (geométrica, 700–800) para títulos, HUD y botones; *Rubik* (humanista redondeada, 400–600) para lectura, lore y etiquetas. Títulos con tracking ligeramente negativo; caps con +0.06em.
- **Fondos:** ilustración cartoon pintada, cálida, con textura de pergamino/papel envejecido y bordes quemados (mapa). Hojas de personaje en acuarela + tinta con anotaciones manuscritas. En UI plana: crema liso o café profundo con **arcos concéntricos** (crema → beige → café) como en la portada. Sin gradientes decorativos; solo protección oscura abajo de imágenes con texto.
- **Bordes:** contorno café **2px** (3px en HUD/paneles) "bien marcado" en tarjetas, botones y casillas: look de sticker/cartoon.
- **Sombras:** sombra **sólida desplazada** sin blur (`0 4px 0 #2B1E16`) para todo lo interactivo; sombra difusa (`--shadow-float`) solo para modales flotantes.
- **Radios:** siempre suaves: 10–14px botones, 20px tarjetas, 28px paneles, píldora para chips/tags/Volver, círculo para icon-buttons, squircle 22.5% para el ícono de app.
- **Tarjetas:** fondo blanco, contorno café 2px, radio 20px, sombra pop 4px. Tarjeta de acción: tile café con ícono plátano a la izquierda, título Outfit, subtítulo Rubik, flecha amarilla a la derecha.
- **Estados:** *Hover* = superficie un paso más clara (plátano 300). *Presionado* = fondo ligeramente más oscuro + baja 2px (sombra 4→2px) + escala 0.96. *Inactivo* = beige `#E8E0D2` con texto café 500 y candado, sin sombra.
- **Animación:** rápida y con rebote: 90ms press, 160ms base con `cubic-bezier(.34,1.56,.64,1)` para selección/aparición, 320ms para barras. Sin fades largos.
- **Transparencia/blur:** solo scrim café `rgba(28,19,14,.72)` bajo paneles de Pausa/Game Over y blur del escenario detrás; bandeja de ítems café al 78%.
- **Layout:** Companion: Top Bar fija / scroll de tarjetas / Home Bar. Juego: HUD anclado a 48px de los bordes: vidas + puntos + tiempo + barra de progreso arriba; ítems abajo-izquierda; pausa arriba-derecha. Diseñado a 1920×1080.
- **Imágenes:** cálidas (ocres, turquesas del Pacífico, verdes de manglar), iluminación dorada, sin grano fotográfico; nunca fotos reales.

## ICONOGRAPHY
- **Sistema:** *Material Symbols Rounded*, **Filled** (FILL 1, wght 600), cargado por CDN de Google Fonts (`tokens/fonts.css`). Recomendado explícitamente en *Interfaz gráfica §3* (alternativa: Lucide Solid). Glifos sólidos con vértices suaves; no outline (se pierde sobre fondos texturizados). Outline solo para estados vacíos (corazón perdido).
- Uso: componente `Icon` (`<Icon name="favorite" />`) o clase `.ms`.
- Íconos frecuentes: `favorite` vidas, `star` puntos, `timer`, `local_fire_department` cocción, `bakery_dining` pan/receta, `arrow_back` Volver, `info` ⓘ, `volume_up`, `pause`, `lock`, `egg`, `grain` harina, `nutrition` plátano, `eco` hoja-escudo, `coffee` café de olla.
- **No hay** sprite/PNG de íconos propios en las fuentes; los sprites de ítems in-game deben ilustrarse en el estilo de las hojas de personaje y reemplazar al ícono en `ItemSlot image=…`.
- Sin emoji. Unicode solo "←" y "ⓘ" en textos de documentación.

## Index
- `styles.css` — entrada global (solo `@import`). `tokens/` — `fonts.css`, `colors.css`, `typography.css`, `spacing.css`, `effects.css`, `base.css`.
- `guidelines/` — tarjetas de fundamentos (Colors, Type, Spacing, Brand, Iconography, Imagery).
- `assets/` — `logo.svg` (marca v4b), `logo-icono.svg` (squircle), `logo-app-icon.png` (versión plátano), `esrb-e10.png`, `illustrations/` (mapa, storyboard, nivel 1 mercado, nivel 2 patio-horno, portada), `characters/` (hojas de Ulises, Don Lupe, Madame Circe, Polifemo).
- `ui_kits/juego/` — juego PC: menú, mapa, nivel + HUD, pausa, game over, victoria.
- `ui_kits/companion/` — app wiki móvil.
- `SKILL.md` — skill para Claude Code.

### Components
- core: **Icon**, **Button**, **IconButton**, **BackButton**, **Tag**, **Badge**
- cards: **ActionCard**, **CharacterCard**
- navigation: **TopBar**
- hud: **HeartMeter**, **HudChip**, **ProgressBar**, **ItemSlot**
- feedback: **GamePanel**

Definidos en la fuente (*Interfaz gráfica §4/§6*): ActionCard (tarjetas de acción), BackButton, IconButton, TopBar, Tag (roles/Wiki).
**Intentional additions:** Icon (wrapper del set Material), Button (menú Jugar/Opciones/Salir del MVP), Badge (contadores), CharacterCard (tarjetas de personaje mencionadas en §6), HeartMeter / HudChip / ProgressBar / ItemSlot (HUD exigido por HU-05 y la barra de cocción del Nivel 2), GamePanel (Pausa / Game Over / Victoria, HU-06).

## Caveats
- Fuentes: no se entregaron binarios; Outfit y Rubik se cargan desde Google Fonts (mismas familias especificadas).
- Discrepancia de logo: el .docx describe plátano `#F5B731` sobre `#2B1E16`; los SVG v4b usan terracota `#D2703A` sobre `#1C130E`. Se trata el SVG v4b como marca vigente.
- No hay arte de Nivel 3 ni mockups de la app companion/HUD; los UI kits son reconstrucciones desde la descripción escrita.
