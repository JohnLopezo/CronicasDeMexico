# Guía visual — Demo artística olmeca

Define el estilo de la escena de demostración `Olmeca_VisualDemo` (aún no creada) y de los recursos del capítulo olmeca. Estado: **etapa 1 (borrador para aprobación)**. Lo que aquí cambia respecto a `Docs/Rendimiento.md` (cámara, escala, sombras) se aplica solo a la demo hasta que se apruebe.

## 1. Referencia

- **Referencia principal:** `Docs/Referencia_Visual_Olmeca.png` (1536×1024). **Segunda hoja:** `Referencia_Visual_Olmeca2.png` (nueva desde el 2026-10-28: más vegetación, objetos de aldea, NPC y una escena integrada).
- Se toma como objetivo de **densidad de vegetación, detalle, profundidad, caminos y composición**. No se copian sus recursos ni sus personajes: todo se produce con el flujo de la sección 10.
- La referencia es una hoja de concepto: sus medidas (tile 64×64, palma 64×128, jaguar 32–40 px) se adoptan; algunos de sus elementos **no** se adoptan por criterio histórico (sección 8).

## 2. Paleta

Paleta maestra de **41 colores en rampas**. Los colores marcados con ★ se extrajeron de la sección 5 de la referencia; el resto, del análisis de color (k-means) de sus paneles de tierra, basalto y palma. Todo sprite se cuantiza a esta paleta (o a un subconjunto de rampas) al final del flujo.

| Rampa | Oscuro → claro | Uso |
|---|---|---|
| Tierra | `#3A2A1A` `#644B2A` `#7B5B33` `#9B713F` `#B6814C` `#C6925A` `#E3B47C` `#F1C891`★ | Caminos, plataformas de arcilla, costados de tiles |
| Basalto | `#1E2420` `#2C302A` `#36382F` `#4A473B` `#615C48` `#7D745B` `#9C8B75` `#C9C0AB` | Columnas, cabeza colosal, altares, rocas |
| Verde cálido | `#1D2B1A` `#23551C` `#387022` `#568B26` `#73AD36` `#92B535` `#AAC652` | Palmas, pasto, arbustos al sol |
| Verde frío | `#0F2A17` `#124524` `#136B35` `#217233` `#328F3E` `#4A993E` | Sombra del follaje, helechos, selva de fondo |
| Musgo | `#3E5F27` `#5A8732` `#84B345` `#899736`★ | Musgo sobre piedra, líquenes |
| Madera | `#48331F` `#6D502E` `#9C7842` `#A28A6D`★ | Troncos, raíces, muelles |
| Agua | `#0B4F4A` `#0C7265` `#168787`★ `#489187`★ `#7FC3B5` | Pantano, río, orillas |
| Acentos | `#B16143`★ `#B24232`★ `#994F66`★ `#F47588`★ | Flores (heliconias), frutos; uso escaso |

Reglas: sin negro puro (el más oscuro es `#0F2A17`/`#1E2420`); contorno exterior con el color más oscuro de la rampa del objeto; los acentos no superan ~3 % de la pantalla para no competir con el jugador.

## 3. Escala de píxeles

- **Proyección isométrica 2:1** (dimétrica): un rombo de suelo mide el doble de ancho que de alto.
- **1 tile = 1 m × 1 m de mundo = rombo de 64×32 px.**
- Escala en pantalla: **45,2548 px por metro** (= 32·√2). En horizontal un metro en diagonal ocupa 32 px; en vertical, un metro de altura ocupa ~39 px.
- **Sprites:** *Pixels Per Unit* **45.2548**, *Filter Mode* **Point**, sin mipmaps, sin compresión en la fuente. Sustituye en la demo a los 32 PPU de los placeholders actuales.
- Un píxel de arte = un píxel de la resolución de referencia. **Nunca** se escalan sprites por factores no enteros ni se rotan.

## 4. Cámara ortográfica

| Ajuste | Valor |
|---|---|
| Proyección | **Ortográfica** |
| Rotación | X = **30°**, Y = **45°**, Z = 0 (da exactamente la relación 2:1) |
| Tamaño ortográfico | 360 / (2 × 45,2548) = **3,977** a la resolución de referencia |
| Rotación en juego | **Fija.** Los sprites están prerenderizados desde este ángulo, así que la cámara no gira. |
| Seguimiento | Cinemachine con seguimiento suave; la posición final se ajusta a la rejilla de píxeles para evitar que el arte tiemble |
| Ejes del mundo | +X del mundo va a la derecha-abajo en pantalla; +Y, a la derecha-arriba |

## 5. Resolución de referencia

- **640×360 px** de arte, escalado **entero** con filtro Point: ×2 = 1280×720, ×3 = 1920×1080, ×4 = 2560×1440.
- Pantallas más anchas (Android 20:9, p. ej. 2400×1080 → ×3): se mantienen 360 px de alto y se amplía el ancho visible (800×360), sin deformar.
- Implementación prevista: cámara renderizando a una RenderTexture de baja resolución y escalado Point a pantalla (etapa de la escena; aún no implementado).
- La UI se dibuja aparte a la resolución nativa.

## 6. Tamaño de sprites y tiles

| Recurso | Lienzo (px) | Mundo | Pivote |
|---|---|---|---|
| Tile de suelo | 64×64 (rombo 64×32 + 10 px de grosor) | 1×1 m | Centro de la cara superior |
| Columnas de basalto | 64×96 y 64×64 por módulo | ~1,1 m de cerca | Centro de la base |
| Palma | 64×128 | ~2,2 m de tronco + copa | Base del tronco |
| Tile de pasto | 64×64 (como el de camino) | 1×1 m | Centro de la cara superior |
| Tile de agua | 64×64; superficie 6 cm bajo el suelo; 4 cuadros en bucle; patrón periódico de 1 m (encaja entre tiles) | 1×1 m | Centro del tile a nivel del suelo |
| Transición camino/pasto | 64×64 por máscara de vecinos | 1×1 m | Centro de la cara superior |
| Orilla (pasto sobre agua) | 64×64 por máscara; transparente donde hay agua; se superpone al tile de agua animado | 1×1 m | Centro de la cara superior |
| Matorral de selva | 64×64 | ~1 m de diámetro, 0,5 m de alto | Base |
| Pasto alto | 32×48 | 0,25–0,35 m | Base |
| Sombra de dosel | 160×96, semitransparente (copa fuera de cuadro) | ~2,5 m | Centro de la sombra |
| Arbusto, heliconia, helecho | 48×48 | 0,5–1 m | Base |
| Roca de basalto | 48×48 | 0,2–0,5 m | Base |
| Nenúfar | 32×32 | — | Centro, sobre la superficie del agua |
| Tule (orilla) | 32×64 | ~0,7 m | Base |
| Jaguar (Guardián) | Lienzo 64×48 por cuadro; cuerpo de 32–40 px de alto | ~0,7 m de altura a la cruz | Entre las patas |
| NPC humano | Lienzo 48×64; 48–56 px de alto (proporciones algo compactas) | — | Entre los pies |
| Cabeza colosal | ~96×96 a 128×128 | ~2,5–3 m de altura | Base |
| Altar-trono | 136×120 (sombra aparte de 160×88) | 2,0 × 1,3 × 1,1 m (Altar 4 real: ~3,2 × 2 × 1,6 m, reducido para el juego) | Centro de la base |
| Estela lisa | 32×64 (pequeña) y 64×88 (grande; 3 variantes) | 0,55 × 0,22 × 1,1 m y 0,9 × 0,3 × 1,35 m | Centro de la base |
| Rampa de arcilla | 136×112 (2 variantes: lado -Y y lado +X) | 2 × 1 m, sube 1 m (~27°) | Centro de su huella |
| Casa de bajareque | 160×152 (sombra aparte de 208×112; 3 variantes) | Muros de 2,0 × 1,4 m, cumbrera a ~2,4 m, basamento de 0,2 m | Centro de la base |
| Fogón con vasijas | 48×48 | ~0,8 m | Centro del fogón |
| Escombros de basalto | 48×48 (3 variantes) | 0,3–0,6 m | Centro |
| Ceiba | 192×208 (sombra aparte de 256×128) | Altura comprimida a ~3,4 m, copa de ~3 m | Base del tronco |
| Tronco caído / tocón | 64×48 (2 variantes) | 1,1 m / 0,35 m | Centro |
| Vasijas grandes | 48×48 (2 variantes) | 0,2–0,45 m | Centro del grupo |
| Enramada | 160×152 (sombra aparte de 208×112) | 2,5 × 1,9 m de techo | Centro de la huella |
| Muelle de madera | 64×64 por módulo de 1 m (variante 1 a lo largo de X, 2 a lo largo de Y) | Tablero a 0,12 m sobre el suelo | Centro del módulo |
| Canoa monóxila | 64×48 | 1,4 m | Centro, a la altura del agua (z = -0,06) |
| Plataforma de arcilla | 64×96 por módulo y máscara de vecinos; se apila (nivel superior en Z = 1 m) | 1×1 m, 1 m de alto | Centro del tile a nivel del pie |

Animación: 5 direcciones (N, NE, E, SE, S) y las 3 del oeste por espejo, como espera `DirectionalSpriteSet`. Jaguar: reposo, caminar/trotar, correr, ataque, recibir daño y derrota.

## 7. Iluminación

- **La luz principal va horneada en los sprites:** sol desde arriba-izquierda de la pantalla. La cara superior es la más clara, la izquierda intermedia y la derecha oscura. Todos los recursos se renderizan con la misma luz y el mismo ambiente (sección 10), y por eso son coherentes entre sí.
- **Sombras:** pintadas en el sprite o como sprite de sombra aparte (multiplicativo) bajo objetos y personajes. **Sin mapas de sombras en tiempo real en la demo**: es lo más caro en GPU integrada y Android, y con sprites prerenderizados no aportan.
- **En Unity:** una luz ambiental o tinte global para la hora del día (amanecer, día, atardecer) y como máximo 2–3 luces puntuales de gameplay (antorchas, poderes). Niebla ligera de color verde-azulado en el fondo para dar profundidad.
- Post-procesado: solo viñeta suave y corrección de color en Medio/Alto; en Bajo, nada.

## 8. Criterios históricos de arquitectura olmeca (La Venta, Tabasco)

Distinción: **D** = documentado arqueológicamente; **I** = interpretación artística; **F** = ficción del juego. Fuentes a consultar: Drucker, Heizer y Squier (1959) *Excavations at La Venta*; González Lauck (INAH) sobre La Venta; Parque-Museo La Venta (Villahermosa).

| Elemento | Criterio | Tipo |
|---|---|---|
| Entorno | Isla en zona pantanosa cerca del río Tonalá; selva tropical húmeda, manglar y humedales | D |
| Arquitectura principal | Montículos y plataformas de **tierra y arcilla** (p. ej. la estructura C-1), con pisos y rellenos de arcillas de colores. **No** templos de piedra labrada ni escalinatas de sillares | D |
| Columnas de basalto | **Columnas naturales de basalto columnar** (prismas de 4–7 caras, sin labrar) que cercaban el patio del Complejo A; también forman la Tumba A. Se representan **sin glifos ni relieves**, a diferencia de la referencia | D |
| Base de la cerca | Muro bajo de adobe o arcilla bajo las columnas | D/I |
| Cabezas colosales | Basalto traído de la sierra de los Tuxtlas; en La Venta hay cuatro (Monumentos 1–4). Rostro con casco ajustado y orejeras. Se colocan en plazas, no sobre escalinatas | D |
| Altares-trono | Bloques rectangulares con un personaje saliendo de un nicho (p. ej. Altar 4) | D |
| Pavimentos | Mosaicos masivos de serpentina, enterrados como ofrenda | D |
| Musgo, raíces y ruina invadida por la selva | El juego muestra el lugar como "memoria" antigua | I |
| Muelles de madera, caminos de tierra con bordes de piedra | Plausibles pero sin evidencia concreta | I/F |
| Flora | Palmas nativas tipo corozo (*Attalea*), coyol o *Sabal*; heliconias, helechos, ceiba, tule y nenúfares. **Sin cocoteros**: el cocotero llegó a la costa del Golfo después del contacto europeo | D |
| No mezclar | Glifos mayas, pirámides escalonadas de piedra, motivos mexicas | — |

## 9. Organización por capas

| # | Capa | Contenido | Orden |
|---|---|---|---|
| 1 | Fondo | Agua, selva lejana, niebla | Primero, sin escribir profundidad |
| 2 | Suelo | Tiles de camino, tierra, pasto, orillas | Por posición en la rejilla |
| 3 | Detalles de suelo | Guijarros, hojas caídas, sombras pintadas | Sobre el suelo |
| 4 | Objetos | Columnas, palmas, ruinas, cabeza colosal, Jaguar, NPC | **Ordenados por su punto de apoyo** (lo más cercano a la cámara se dibuja después) |
| 5 | Primer plano | Copas y follaje delantero | Se vuelven semitransparentes o se disuelven con tramado cuando el Jaguar queda detrás |
| 6 | Efectos | Partículas, interacción | Encima |
| 7 | UI | HUD (UI Toolkit) | Resolución nativa |

Los sprites se colocan en el mundo 3D mirando a la cámara. Las colisiones son geometría 3D invisible, así que se conservan `CharacterController` y los sistemas actuales.

## 10. Flujo de producción (Blender → Pixel Art)

Scripts en `Tools/ArtPipeline/`; se ejecutan sin abrir Blender (`blender --background --python ...`).

1. Modelo 3D procedural o importado, con la escala real en metros.
2. Cámara ortográfica a 30°/45°, la misma luz de sol y el mismo ambiente para todos los recursos.
3. Render con Cycles a **4× la resolución final**, fondo transparente y transformación de vista *Standard*.
4. Reducción a 1× por bloques de 4×4 (cobertura ≥ 50 % = píxel opaco; sin semitransparencias).
5. Cuantización a las rampas de la paleta del recurso (distancia en espacio Lab).
6. Contorno exterior de 1 px con el color oscuro de la rampa. Los tiles de suelo no llevan contorno, para que encajen sin costuras.
7. Se guarda el PNG con su pivote. Se puede retocar a mano después (Aseprite u otro editor) si se aprueba.

Reglas para tiles de suelo (aprendidas en las pruebas):

- Se renderizan un **3 % más grandes** (solapamiento de ~1 px). Sin esto quedan huecos en escalera entre vecinos.
- La **fila trasera del rombo toma el color del píxel interior**, porque si no forma líneas al encajar.
- El relieve se **atenúa hacia los bordes**, que quedan todos a la misma altura, para que no aparezcan surcos.
- Las **transiciones** se generan por máscara de vecinos: 4 bits cardinales y 4 diagonales, y una diagonal solo cuenta si sus dos lados son del mismo terreno. El borde irregular es periódico, así que dos tiles contiguos empalman sin escalón. Hoy solo se generan las máscaras que usa la composición; el juego necesitará el conjunto completo, que se genera igual.

Ventaja: coherencia de luz y escala entre recursos y facilidad para generar variantes. Límite: los personajes animados (Jaguar, NPC) y la cabeza colosal necesitan un modelo o arte de calidad que se preparará aparte.

## 11. Requisitos de rendimiento

- Objetivo: **30 FPS estables** en Android de gama media y en el i5-6500 con HD 530 en calidad **Bajo**.
- Sprites con **recorte alfa** (sin semitransparencias) para que escriban profundidad y no se acumule *overdraw*. La transparencia real solo se usa en el agua, la niebla y el follaje que se desvanece.
- Sprite Atlas por grupo (suelo olmeca, vegetación, ruinas, Jaguar, NPC), de 2048×2048 como máximo. En Android, ASTC 4×4 o sin comprimir si la compresión ensucia el pixel art (se decide comparando a ojo).
- Material compartido por atlas, para que el SRP Batcher agrupe.
- Sin mapas de sombras en tiempo real, sin SSAO y sin profundidad de campo en la demo.
- Mundo dividido en escenas por zona (`Docs/Rendimiento.md` §Escenas por región).
- Pendiente de medir en dispositivo real; no se anotan cifras sin medición.

## 12. Estado y siguiente sesión

*Actualizado: 2026-10-27, al cerrar la sesión en el i7-10700.*

### Hecho y aprobado

- Guía visual (este documento), aprobada.
- Flujo Blender → Pixel Art funcionando (`Tools/ArtPipeline/`). Genera 54 sprites en ~50 s:
  - Suelo: tiles de camino, pasto, agua animada, transiciones camino/pasto y orillas.
  - Cerca de basalto y palma corozo.
  - Arbustos, heliconia, matorrales, pasto alto, helechos, rocas, nenúfar y tule.
  - Sombras de dosel.
- Composición de prueba: `Tools/ArtPipeline/Salida/Olmeca/Composicion_1920x1080_x3.png`.
- **Ruinas, etapa 1 (2026-10-28): plataforma de arcilla**, aprobada. Módulos `Plataforma_Arcilla_<máscara>`; prueba en `Prueba_Ruinas_1920x1080_x3.png`.
  - Módulos de 1 m × 1 m × 1 m. Bits 1/2/4/8: lado con talud. Bits 16–128: esquina cóncava. Solo se generan las máscaras que usa una composición.
  - Corte con capas de arcillas de colores (solo colores de la paleta, rojo `#B16143` incluido), cárcavas, pasto en la cima y el labio, y terrones al pie.
  - Los taludes traseros no se ven desde la cámara: se marca el borde con una línea `#1D2B1A`.
  - Límites conocidos: todos los módulos de una misma máscara son iguales, así que las cárcavas se repiten cada metro; sin sombra proyectada al pie (va en la integración con Unity).
- **Ruinas, etapa 2 (2026-10-28): altar-trono** (`Altar_Trono_01` y su sombra), aprobado.
  - Bloque de basalto labrado con cornisa saliente, más pronunciada al frente, y nicho de arco rebajado en la cara -Y, la iluminada.
  - Musgo en manchas en la cornisa y en la base; pasto, helechos y fragmentos al pie. Sin relieves.
  - El nicho queda **vacío**: la figura sigue pendiente de decisión (arte a mano u omitirla).
  - Para Unity: por su tamaño (2 × 1,3 m) conviene ordenarlo por su huella y no solo por el pivote, si hay objetos pegados a sus esquinas.
- **Ruinas, etapa 3 (2026-10-28): estelas lisas**, aprobadas. Sin relieve; musgo en la cima y en la base.
  - `Estela_Lisa_01`.
  - `Estela_Grande_01` en pie, `_02` vencida hacia la cámara con tierra removida y `_03` rota con fractura irregular y el trozo superior caído.
- **Ciudad jugable, etapa A (2026-10-28): recursos de la aldea**, aprobada. El plan completo (A–E) está en «Siguiente».
  - `Casa_Bajareque_01–03`: rectangular, absidal y pequeña redonda. Muros de barro con desconchones que dejan ver las varas, puerta con jambas al lado -Y, techo de palma con vetas y flecos, y basamento de tierra. Casas perecederas: D; la forma concreta: I.
  - `Fogon_01`: tres piedras, ceniza, tecomate (vasija esférica sin cuello) y dos vasijas más.
  - `Escombros_Basalto_01–03`: tramos de columna caídos, montón de fragmentos y tocón con su tramo. Es la antigua etapa 4.
  - `Rampa_Arcilla_01–02`: único acceso a la cima de las plataformas. Límite conocido: la unión con el talud vecino deja un corte algo brusco.

Regenerar todo (desde la carpeta del proyecto):

```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python Tools\ArtPipeline\generar_olmeca.py -- Tools\ArtPipeline\Salida\Olmeca
```

### Siguiente: ciudad olmeca jugable (centro ceremonial y aldea)

Pedida el 2026-10-28; se hace en lugar de las etapas 4–6 de las ruinas.

| Etapa | Contenido |
|---|---|
| A | Recursos de la aldea: casas, fogón, escombros y rampas (aprobada) |
| B | Trazado en Python (`planificar_ciudad`), aprobado. Detalle debajo |
| C | Importación en Unity, aprobada. Detalle debajo |
| D | Escena `Olmeca_Ciudad`, aprobada. Detalle debajo |
| E | Rendimiento, aprobado. Detalle debajo y en `Docs/Rendimiento.md` |
| A2 | Recursos de la segunda referencia, hechos el 2026-10-28 y pendientes de aprobación. Detalle debajo |

**Etapa B: trazado de la ciudad.** Mapa de 51×47 m. El norte apunta hacia la cámara (-Y), así que el Gran Montículo (Complejo C, al sur) queda al fondo y el recinto de columnas (Complejo A, al norte) en primer plano: el orden se respeta (I).

- Elementos:
  - Gran Montículo de 3 niveles (3 m) con 3 rampas encadenadas.
  - Plaza de tierra con 2 altares y 2 estelas al pie, y estelas lisas a lo largo de la calzada.
  - Recinto de 11×11 m cercado por columnas, con entrada hacia el montículo, una plataforma baja, estelas y escombros.
  - Aldea de 10 casas con fogones, caminos, río al este con tule y nenúfares, y pantano.
  - Selva más densa al oeste y en los bordes.
- Nuevo recurso: `Cerca_Basalto_Y_01–03`, la cerca girada para los lados este y oeste.
- Vista previa: `Ciudad_3264x1800.png` y recortes ×3 `Ciudad_{Plaza,Montículo,Recinto,Aldea}_1920x1080_x3.png`.
- Datos para Unity: `ciudad.json` (suelo, objetos con x, y, z; niveles de plataforma, rampas, celdas de agua y límites para las colisiones).
- Límites conocidos:
  - En las cimas grandes de las plataformas se nota la rejilla de módulos.
  - El borde del mapa es un rombo recortado; en Unity habrá que limitar la cámara o cerrar el borde con selva.
  - La referencia `Referencia_Visual_Olmeca2.png` (nueva, 2026-10-28) propone recursos para una etapa A2: ceiba, troncos, vasijas grandes, enramada, muelle y braseros.

**Etapa C: importación en Unity.**

- **Exportación:** `generar_olmeca.py` termina copiando los sprites del juego a `Assets/_Project/Art/Environment/Olmeca/Sprites/`:
  - `Objetos/`: todo lo que no es suelo.
  - `Agua/`: los 4 cuadros.
  - `Suelo/`: el suelo de la ciudad horneado en trozos de hasta 1024 px, con sombras de dosel y de objetos y sin el agua.
  - También escribe `pivotes_unity.json` y `Assets/_Project/Data/World/Olmeca/Ciudad.json`.
  - Conserva los `.meta` de lo que sigue existiendo. `--sin-unity` omite este paso.
- **Importación:** `PixelArtSpriteImporter` aplica PPU 45.2548, Point, sin mipmaps ni compresión y el pivote del flujo. Malla Tight en objetos y FullRect en el suelo.
- **Shader** `Cronicas/Pixel Art Unlit`: sin iluminación (la luz va horneada), con recorte alfa.
- **Materiales** en `Materials/PixelArt/`:
  - `M_PixelArt_Agua`: cola 1900, sin profundidad.
  - `M_PixelArt_Suelo`: cola 1950, sin profundidad.
  - `M_PixelArt_Objeto`: cola 2450, con profundidad. Un sprite orientado al plano de la cámara escribe la profundidad de su pivote, lo que reproduce el orden por punto de apoyo sin ordenar los renderers.
- **Atlas** `Atlas_Olmeca.spriteatlasv2`, con objetos y agua. El empaquetador del proyecto pasa a Sprite Atlas V2 (`EditorSettings`).
- Menú **Crónicas → Pixel Art → Crear materiales y atlas**, o `-executeMethod CronicasDeMexico.Editor.PixelArt.PixelArtAssets.CreateAll`.
- **Pruebas:** `PixelArtImportTests` (8). EditMode 36/36 y PlayMode 21/21, en batch en el i7 el 2026-10-28.
- **Ejes:** el mundo del flujo (x, y, z) pasa a Unity como (y, z, -x) con la cámara en rotación (30°, 45°, 0°).

**Etapa D: escena `Scenes/Regions/Olmeca/Olmeca_Ciudad.unity`.**

- **Generación:** menú **Crónicas → Generar ciudad olmeca**, o `-executeMethod CronicasDeMexico.Editor.SceneGeneration.OlmecaCityGenerator.Generate`. Se deriva de `Ciudad.json` y regenerarla la reemplaza entera. `Olmeca_Prototipo` no se toca.
- **Capas:**
  - Suelo horneado: 6 trozos, cada uno con su material en `Materials/PixelArt/Suelo/`.
  - Agua: 266 tiles animados por un solo `SpriteFrameCycler`.
  - 1076 objetos y rampas orientados al plano de la cámara y alineados a la rejilla de píxeles.
- **Colisiones (cajas invisibles):** suelo, bordes del mapa, agua, los niveles del montículo y del recinto (1 m cada uno), losas inclinadas para las 3 rampas, casas, altares, estelas, cercas, palmas, rocas, escombros y fogones. La vegetación baja se atraviesa.
- **Interacción:** inscripciones en los 2 altares y las 10 estelas (`InscriptionInteractable`). El texto del altar dice que el nicho está vacío.
- **Cámara** (`IsometricCameraFollow`): ortográfica, sigue con suavizado y se limita al mapa con 7 m de margen. No hay post-procesado. Desde la etapa E dibuja a la resolución del arte mediante `PixelArtScreen` (ver más abajo).
- **Jugador:** el prefab actual escalado ×0,707 (32 → 45,25 px por unidad), con el sprite orientado al plano de la cámara (`CameraPlane`) y `PixelGridSnap`. Lo ilumina un sol sin sombras; el escenario es sin luz.
- **Errores encontrados con capturas reales (`OlmecaCityCapture`, menú Crónicas → Capturar ciudad olmeca):**
  - Con *GPU instancing* el jugador salía blanco. Se desactivó en los materiales.
  - Con un material compartido, los 6 trozos de suelo se dibujaban con la textura del primero. Ahora cada trozo tiene su material y una prueba lo vigila.
- **Pruebas:** EditMode 42/42 (`IsometricProjectionTests`, nueva) y PlayMode 25/25 (`OlmecaCiudadSceneTests`, nueva: carga, colisiones de cada nivel y rampa, subida por la primera rampa, alineación a píxeles, inscripciones, agua animada y texturas del suelo).
- **Pendiente:**
  - Probarla en el Editor con teclado.
  - Medir FPS (etapa E).
  - El jugador provisional desentona con el arte y no tiene sombra pintada.
  - Faltan NPC y Jaguar.

**Etapa E: rendimiento y presentación nítida.**

- **Defecto de la etapa D corregido:** el nivel Bajo (URP Render Scale 0,75 con filtro bilineal) dibujaba el pixel art borroso en Play. Las capturas en batch no lo mostraban porque dibujan en una RenderTexture, donde no se aplica el Render Scale.
- **`PixelArtScreen`:**
  - La cámara del escenario dibuja en una RenderTexture de unos 640×360 (642×362 a 1080 p, con un píxel de margen por lado) con filtro Point.
  - Un panel de UI Toolkit por debajo del HUD (`Settings/UI/PanelSettings_PixelArt.asset`) la amplía ×3.
  - La cámara avanza de píxel del arte en píxel del arte y el resto se compensa desplazando la imagen en píxeles de pantalla, para que el scroll no dé saltos.
  - Una cámara de fondo limpia la pantalla.
- **Medición** (i7-10700 / UHD 630, 1080 p, sin V-Sync, build de desarrollo): Bajo 3,6 ms (unos 275 FPS), Medio 4,7 ms y Alto 9,3 ms. Las capturas del build (`Build_*.png`) confirman que la imagen sale nítida.
- **Herramientas:** `FrameTimeBenchmark` (argumento `-benchmark`) y `WindowsBenchmarkBuild`.
- **Pruebas:** EditMode 42/42 y PlayMode 26/26 (nueva: `Camera_RendersAtArtResolution_AndPanelScalesItByAnInteger`).
- **Pendiente:**
  - Medir en el i5 / HD 530 y en Android.
  - En Alto, la cámara de fondo paga SSAO y HDR a 1080 p sin dibujar nada; se puede optimizar si hace falta.

**Etapa A2: recursos adaptados de `Referencia_Visual_Olmeca2.png`.** Prueba en `Prueba_Aldea_1920x1080_x3.png`.

- **Recursos:**
  - `Ceiba_01`: árbol sagrado mesoamericano y nativo de Tabasco (D). Tronco gris con raíces tabulares y copa en pisos con núcleos rellenos.
  - `Tronco_Caido_01–02`: tronco con musgo y hongos, y tocón con raíces.
  - `Vasijas_01–02`: olla, tecomate y cántaro; tinaja oscura con un cajete blanco.
  - `Enramada_01`: techo de palma sobre cuatro postes, con petate, vasijas y metate (I).
  - `Muelle_Madera_01–02`: módulo de 1 m con pilotes que acaban en la superficie del agua (I/F).
  - `Canoa_01`: monóxila (D); solo se modela lo que asoma del agua.
- **No adaptado:**
  - Glifos en columnas y plataformas de sillares (van contra §8).
  - Palmas tipo cocotero.
  - Braseros: no hay evidencia olmeca clara.
  - Personajes y enemigos: arte a mano.
- Ya se exportan a Unity (`Sprites/Objetos` y el atlas), pero todavía no están en el trazado de la ciudad.

### Ruinas olmecas (opción b)

Etapas 4–6: escombros, mosaico (opcional) y composición. Crear con el mismo flujo, añadiendo constructores a `generar_olmeca.py` y a la composición. Todo sin labrar con glifos y con los criterios de §8:

| Recurso | Idea | Lienzo previsto | Tipo (§8) |
|---|---|---|---|
| Plataforma o montículo de arcilla | Bloque de tierra con taludes inclinados, capas de arcillas de colores en el corte, erosión y pasto encima. Modular en tiles de 1 m. | 64×64 por módulo (y de esquina) | D |
| Altar-trono | Bloque rectangular de basalto con un nicho frontal y una cornisa superior. La figura que sale del nicho queda **pendiente** (requiere arte a mano o decidir si se omite). | 96×96 | D (figura: I) |
| Estelas lisas | Losas verticales de basalto, rectangulares y algo redondeadas, sin relieve; una de ellas rota o inclinada. | 32×64 y 48×80 | D (sin relieve: I) |
| Mosaico de serpentina (opcional) | Fragmento de pavimento verde-azulado expuesto entre el pasto, como hallazgo. | Tile 64×64 | D/I |
| Escombros de basalto | Columnas caídas y fragmentos sueltos, para ambientar alrededor. | 48×48 | I |

Después: una composición que integre las ruinas con el entorno ya aprobado y la revisión a ojo frente a la referencia.

### Pendientes (necesitan autorización)

1. **Commit y push.** Todo el trabajo (Fase 1, documentación, flujo de arte y salidas) **solo existe en el i7-10700**. En el i5 no aparecerá hasta subirlo (`Docs/DesarrolloMultiPC.md` §5).
2. Borrar `Tools/ArtPipeline/Salida/Pruebas/` (duplica la ronda anterior) y `Docs/Referencia_Visual_Olmeca2.png` (copia exacta de la referencia).
3. Decidir si `Tools/ArtPipeline/Salida/` se versiona (por LFS) o se regenera en cada PC. El i5 necesitaría Blender 5.2 para regenerar.
4. Detalles menores: la variante 3 de la cerca toca 1 px el borde del lienzo, y el conjunto completo de transiciones (~47 por tipo) se genera cuando se integre en Unity.
5. Después de las ruinas: opción c, integrar en Unity (`Olmeca_VisualDemo.unity`, cámara ortográfica y captura real de la pestaña Game).
6. Siguen pendientes de ti la cabeza colosal, el Jaguar animado y el NPC.
