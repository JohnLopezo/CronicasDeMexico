# Registro de rendimiento

Referencias (Diseno.md §16):

- **PC 1 (referencia):** HP ProDesk 600 G2, i5-6500, 12 GB, **Intel HD Graphics 530**, calidad **Bajo**. Lo que rinde aquí rinde en ambas PC.
- **PC 2 (desarrollo principal):** i7-10700, 16 GB, **Intel UHD Graphics 630**. GPU integrada de potencia similar a la HD 530: no sirve como referencia "alta".
- **Android de gama media:** objetivo de **30 FPS estables** medidos en un dispositivo real; 60 FPS es opcional.

Procedimiento para trabajar en ambas PC: `Docs/DesarrolloMultiPC.md`.

Se mide en un **build de desarrollo** (no en el Editor), en cada hito. En PC el V-Sync está activo: los FPS no superan la frecuencia del monitor, así que hay que fijarse en los **ms por frame** y en los mínimos. El HUD muestra FPS y ms (esquina superior derecha). **No se anotan resultados sin haberlos medido.**

| Fecha | Hito | Equipo | Calidad | Resolución | Escena | FPS medio | FPS 1 % bajo | ms/frame | Notas |
|---|---|---|---|---|---|---|---|---|---|
| 2026-10-28 | Ciudad jugable, etapa E | i7-10700 / UHD 630 | Bajo | 1920×1080 | Olmeca_Ciudad | 269–282 | 185–197 | 3,6 (p95 4,8) | PixelArtScreen 642×362 ×3; sin V-Sync; 5 puntos |
| 2026-10-28 | Ciudad jugable, etapa E | i7-10700 / UHD 630 | Medio | 1920×1080 | Olmeca_Ciudad | 207–220 | 154–164 | 4,7 (p95 6,0) | Ídem |
| 2026-10-28 | Ciudad jugable, etapa E | i7-10700 / UHD 630 | Alto | 1920×1080 | Olmeca_Ciudad | 106–109 | 70–94 | 9,3 (p95 10,6) | Ídem; Alto paga SSAO, Forward+ y HDR a 1080 p en la cámara de fondo |

**Cómo se mide la ciudad** (`FrameTimeBenchmark`): build de desarrollo de Windows con menú **Crónicas → Build → Windows: medición de la ciudad**, o con `-executeMethod CronicasDeMexico.Editor.Build.WindowsBenchmarkBuild.Build`. Se ejecuta así:

```
Builds\Windows\Ciudad\CronicasDeMexico.exe -benchmark -benchmarkOutput <carpeta> -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -popupwindow
```

- Recorre 5 puntos (plaza, montículo, recinto, aldea y selva) en los 3 niveles, con 6 s por punto, la cámara en movimiento y sin V-Sync.
- Escribe `resumen.txt`, `frames.csv` y una captura por punto, y se cierra sola.
- `-sinPantallaPixel` desactiva PixelArtScreen para comparar. La comparación del 2026-10-28 **no es válida**: la ventana perdió el foco o se cerró durante las ejecuciones (frames de decenas de segundos y otros fijos en 16,7 ms). Los puntos limpios dan una idea, pero no son una medición: Bajo/Plaza 3,3 ms directo frente a 3,6 ms con PixelArtScreen; Medio/Plaza a Recinto 5,2–5,9 ms frente a 4,6–4,8. **La ventaja de PixelArtScreen es sobre todo de imagen**: sin él, Bajo dibuja a 0,75 y reescala con filtro bilineal, y el pixel art sale borroso (capturas del build). En el i7 la escena no está limitada por píxeles.
- Para medir, no usar el PC mientras dura (unos 2 minutos): la ventana de 1920×1080 tapa la pantalla y, si pierde el foco, los datos se estropean.
- **Falta medir en el i5-6500 / HD 530**, que es la referencia, y en Android.

## Calidad por defecto

| Plataforma | Nivel inicial |
|---|---|
| Editor | Bajo (`m_CurrentQuality: 0`) |
| Windows (Standalone) | **Bajo** |
| Android | **Bajo**, hasta medir en un dispositivo real |
| iOS | Medio (sin revisar; iOS no es objetivo inmediato) |

`QualityService` guarda la elección del jugador (Esc / Start → panel de calidad). Es un ajuste, no una partida: puede vivir en PlayerPrefs.

## Perfiles de calidad

| Ajuste | Bajo | Medio | Alto |
|---|---|---|---|
| URP Asset | `URP_Bajo` | `URP_Medio` | `URP_Alto` |
| Renderer | `Renderer_Rendimiento` (Forward, sin SSAO) | `Renderer_Rendimiento` | `Renderer_Calidad` (Forward+, SSAO) |
| Render Scale | 0.75 | 1 | 1 |
| HDR | **No** | Sí | Sí |
| Depth Texture | **No** | Sí | Sí |
| Opaque Texture | No | No | Sí |
| Post-procesado de la cámara | **Apagado** (`CameraQualityApplier`) | Sí | Sí |
| Sombra principal | 1024, 1 cascada, 30 m, duras | 2048, 2 cascadas, 40 m, suaves (baja) | 2048, 4 cascadas, 50 m, suaves (alta) |
| Sombras de luces adicionales | No | No | Sí |
| Filtrado anisotrópico | Por textura | Forzado | Forzado |

Notas:

- En Bajo, sin HDR, el bloom (umbral 1) apenas se vería; por eso `CameraQualityApplier` apaga el post-procesado de la cámara en ese nivel y lo reactiva al subir.
- El volumen global usa `Volume_HD2D_Base`: Tonemapping, Bloom y Viñeta (Motion Blur desactivado). No hay profundidad de campo por ahora.
- Los sprites con destello de daño usan un MaterialPropertyBlock: esos renderers salen del SRP Batcher mientras lo tienen asignado. Con pocos personajes en pantalla no es relevante; revisarlo si hay muchos enemigos.
- Build Android de desarrollo: menú **Crónicas → Build → Android (desarrollo)** (APK en `Builds/Android/`).

## Presupuestos para gráficos integrados

Valores **orientativos** para escenas en **Bajo** en el i5-6500 y en Android de gama media. Son el punto de partida; se ajustan con mediciones reales (Profiler y Frame Debugger sobre un build de desarrollo) y se anotan aquí cuando se midan.

| Recurso | Presupuesto inicial |
|---|---|
| Luces en tiempo real | 1 direccional (sol/luna) con sombras + máx. 2 luces adicionales por objeto (`pixelLightCount: 2`), sin sombras en Bajo y Medio |
| Draw calls / SetPass | Vigilar en el Frame Debugger; mantener materiales compartidos para que el SRP Batcher agrupe |
| Texturas | Atlas de sprites de 2048×2048 como máximo; texturas de entorno de 1024 o menos salvo justificación |
| Transparencias | Pocas capas de partículas o follaje transparente superpuestas a pantalla completa (el *overdraw* es lo que más castiga a las GPU integradas) |
| Asignaciones en `Update` | 0 B por frame en código de juego |

## Pixel Art isométrico 2.5D: ajustes técnicos

El estilo es **Pixel Art detallado en 2.5D con cámara isométrica/semisométrica** (Diseno.md §5): personajes y elementos en sprites pixel art sobre escenarios 3D estilizados. La cámara actual es en **perspectiva con FOV 30** y ángulo elevado; el ángulo definitivo sigue abierto (Diseno.md §19). La calidad artística no se sacrifica entre perfiles: lo que cambia entre Bajo, Medio y Alto es la iluminación y el post-procesado, **nunca la resolución de los sprites**.

- **Importación de sprites:** *Filter Mode* **Point**, **32 píxeles por unidad** (valor actual de los placeholders), sin mipmaps, *Compression* **None** en la textura fuente. La compresión se decide en el Sprite Atlas.
- **Tamaño base:** personajes de ~32–48 px de alto (Diseno.md §5), a validar en pruebas.
- **Paleta y legibilidad:** paletas por región; los efectos no deben tapar la silueta del personaje en combate.
- **Render Scale 0.75 en Bajo:** con el filtro de escalado *Auto* puede suavizar los píxeles de los sprites. Revisar a ojo en el i5; si se nota, probar *Point* como filtro de escalado en `URP_Bajo` (pendiente, no aplicado).
- Las sombras de los personajes las proyecta el `SpriteRenderer` con `HD2D_SpriteLit`; el volteo es por escala X (ver `CLAUDE.md`).

## Sprite Atlas

- Un atlas por **grupo que se ve junto**: personaje (todas sus animaciones), enemigos de una región, props de una región, UI. No mezclar regiones en un atlas, para que al descargar una región se libere su memoria.
- Ajustes del atlas: *Filter Mode* Point, sin mipmaps, *Padding* 2–4 px, *Allow Rotation* y *Tight Packing* desactivados.
- Compresión por plataforma en el atlas: **Android ASTC** (formato por defecto del proyecto) con bloque pequeño (4×4 o 5×5) para no perder detalle del pixel art; Windows sin compresión o alta calidad. Comparar a ojo antes de fijar el valor.
- Ubicación y nombre: `Assets/_Project/Art/<Tipo>/Atlases/Atlas_<Grupo>.spriteatlasv2` (los `.spriteatlas`/`.spriteatlasv2` ya están en `.gitattributes`).
- **Pendiente:** el Sprite Packer está **desactivado** (`m_SpritePackerMode: 0` en `EditorSettings`) y no hay ningún atlas todavía. Hay que activar *Sprite Atlas V2* antes de crearlos.

## Iluminación

- **Horneada por defecto** (lightmaps + light probes para personajes y objetos móviles). El proyecto usa *Baked Global Illumination* con **Progressive CPU** y sin GI en tiempo real.
- Tiempo real solo para la luz principal y pocas luces de gameplay (antorchas cercanas, golpes, poderes) con radio corto.
- Hornear **solo en el i7-10700** y subir los lightmaps en el mismo commit que la escena (`Docs/DesarrolloMultiPC.md` §6). *Auto Generate* desactivado.
- Reflection probes horneadas y pocas; nada de reflexiones en tiempo real.
- Ninguna escena tiene lightmaps horneados todavía.

## Escenas por región

- `Boot` (siempre cargada) + **una escena aditiva por región o zona**: `Scenes/Regions/<Cultura>/<Cultura>_<Zona>.unity` (p. ej. `Olmeca_Hub`, `Olmeca_Mazmorra_Piso1`). Cada escena tiene su propia iluminación horneada.
- Al cambiar de zona: cargar la nueva con `SceneManager.LoadSceneAsync(..., LoadSceneMode.Additive)`, descargar la anterior con `UnloadSceneAsync` y llamar a `Resources.UnloadUnusedAssets` durante la transición (Diseno.md §6).
- Escenas pequeñas y prefabs anidados: rinden mejor en gráficos integrados y reducen conflictos de Git.
- Toda escena nueva se añade a Build Settings. Addressables no se usa por ahora (no se añaden paquetes sin necesidad); se reevaluará si el tamaño del APK o la memoria lo exigen.

## Efectos: permitidos y a evitar

| Usar (con moderación) | Evitar en gráficos integrados y Android |
|---|---|
| Bloom, viñeta y tonemapping (Medio y Alto) | Profundidad de campo Bokeh (si se usa DOF: Gaussiano y solo en Alto) |
| Niebla de URP | Motion Blur, reflejos en espacio de pantalla, efectos volumétricos |
| Partículas pequeñas con pooling y pocas por pantalla | Partículas grandes transparentes a pantalla completa |
| Agua estilizada con shader simple (desplazamiento de UV; color por profundidad solo en Medio/Alto) | Refracción con Opaque Texture en Bajo |
| SSAO solo en Alto | Sombras de luces adicionales en Bajo/Medio; más de 2 cascadas fuera de Alto |
| Sombras horneadas + sombra principal | Muchas luces en tiempo real con sombras |

## Plataformas: Android y futura compatibilidad con iOS

Configuración actual (verificada en `ProjectSettings` el 2026-10-27):

| Ajuste | Android | iOS |
|---|---|---|
| Orientación | Horizontal | Horizontal (configuración compartida) |
| Scripting backend | IL2CPP | IL2CPP (obligatorio en iOS) |
| Arquitectura | ARM64 | ARM64 |
| API gráfica | Vulkan + OpenGL ES 3 (manual) | Metal (automático) |
| Compresión de texturas | ASTC | ASTC |
| Versión mínima | API 23 (Android 6.0) | iOS 13.0 |
| Identificador | `com.johnlopezo.cronicasdemexico` | `com.johnlopezo.cronicasdemexico` |
| Calidad inicial | Bajo | Medio (revisar antes del primer build de iOS) |

Reglas para no cerrar la puerta a iOS:

- Código dependiente de plataforma detrás de interfaces (entrada, guardado, ajustes), con `#if UNITY_ANDROID` / `UNITY_IOS` solo en la implementación.
- Guardado en `Application.persistentDataPath`, nunca rutas fijas.
- Controles táctiles con el Input System; la UI respeta `Screen.safeArea`.
- Shaders compatibles con Vulkan, GLES3 y Metal (los de URP y Shader Graph lo son).
- Sin plugins nativos exclusivos de Android.
- El build de iOS requiere una Mac con Xcode; no forma parte del flujo en estas PC.

## Pendientes técnicos (no aplicados; requieren autorización)

- Activar el Sprite Packer en modo *Sprite Atlas V2* y crear los primeros atlas.
- Evaluar el filtro de escalado *Point* en `URP_Bajo` para los sprites con Render Scale 0.75.
- Regla de `.gitattributes` para `LightingData.asset` antes del primer horneado.
- Revisar la calidad inicial de iOS (Medio) cuando iOS pase a ser objetivo.
