# Referencias

Material original de diseño. Unity **no** importa esta carpeta. Lo que va a entrar al juego se pone en `Assets/_Project/Art/`.

| Material | Carpeta |
|---|---|
| PDFs (historia, GDD, storyboards) | `Docs/Referencias/PDFs/` |
| Paleta de colores (imagen, PDF o códigos HEX) | `Docs/Referencias/Paleta/` |
| Diseños / concept art de personajes | `Docs/Referencias/Personajes/<NombrePersonaje>/` |
| Storyboards sueltos (imágenes) | `Docs/Referencias/Storyboards/` |
| Sprites listos para el juego (PNG/PSD/PSB) | `Assets/_Project/Art/Characters/<NombrePersonaje>/`, `Art/Environment/` o `Art/UI/` |
| Sprites que no sabes si son finales | `Docs/Referencias/Sprites-por-revisar/` |

Los PDFs, imágenes y archivos de arte se suben con **Git LFS**. Antes de clonar o de subir archivos, instálalo una vez con `git lfs install`.

Para el commit usa un mensaje como: `docs(referencias): agregar PDFs y arte de personajes`.
