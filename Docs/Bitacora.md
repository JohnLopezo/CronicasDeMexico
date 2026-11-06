# Bitácora de sesiones

Registro corto de cada sesión de trabajo, para retomar en cualquiera de las dos PC (`Docs/DesarrolloMultiPC.md`). La memoria de Claude Code es local de cada PC; lo que deba saberse en la otra va aquí, en `CLAUDE.md` o en el resto de `Docs/`.

**Cómo se usa:**

- **Al empezar:** leer `Docs/Estado_y_Plan.md` (recopilación y plan) y la última entrada, `git pull` y comprobar que el último commit coincide con el anotado.
- **Al terminar:** añadir una entrada **arriba** (la más reciente primero) y subirla en el mismo push que el trabajo. Así la otra PC ve el estado real al hacer `git pull`.
- Cada entrada es breve; el detalle técnico vive en los documentos de cada área (por ejemplo, `Docs/Arte_Olmeca_Demo.md` §12) y en los commits.

Plantilla:

```markdown
## AAAA-MM-DD — PC (i5-6500 / i7-10700)

- **Rama / último commit:** `rama` @ `abc1234`
- **Hecho:** …
- **Estado al cerrar:** pruebas (EditMode x/x, PlayMode x/x), push hecho o no, cambios locales sin subir.
- **Siguiente:** …
```

---

## 2026-11-06 — i7-10700: historia, afinidades y atributos de los guerreros (etapa 4)

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `cb8ae9c` (etapa 3). EditMode 97/97, PlayMode 74/74. Subido junto con las etapas 1 a 3.
- **Decisiones del autor:** son guerreros humanos ligados al espíritu de su animal; mandan las afinidades de las fichas; atributos propios sin estilos de combate.
- **Hecho:** biblia (§1 premisa, §2, §14, §16, §17) y `Protagonista.md`; reflejo, figura espiritual, artesano, anciana y niños; `Docs` de misiones, objetos y progresión. Afinidades: Balam tierra, Naira viento, Itzel luz (el águila deja el fuego). Atributos con presupuesto igual (`Sistema_Progresion.md` §2), visibles en la pantalla de elección. Una marca por Guardián: `RewardService` retira del paquete las de otro.
- **Regenerado:** `Olmeca_Ciudad` y los tres pisos (el reflejo y la pista del basalto viven en la escena); `Olmeca_Selva` no hacía falta.
- **Cambio de comportamiento:** los `DLG_*` de `CreateCharacter` se rehacen siempre desde el código; editarlos a mano se perdería al generar. Solo cambiaron los 6 previstos.
- **Sin probar:** equilibrio jugando: a 8 de ataque (Xali, Ocel) un espíritu de 30 cae en 4 golpes y no en 3, e Itzel aguanta 4 golpes de 10. Partidas viejas: Balam pasa a 12 de ataque y 80 de energía. La línea de atributos del panel no se capturó.
- **Fuera de alcance:** estilos de combate distintos; muertes de Naira, Ameyali y Ocel; quitar el arte del jaguar jugable; textos del GDD (`Diseno.md`), al que la biblia prevalece.

---

## 2026-11-06 — i7-10700: elección de Guardián (etapa 3)

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `563d882` (etapa 2). EditMode 92/92, PlayMode 74/74. Sin push.
- **Hecho:** Nueva partida pasa por un panel con los 7 guerreros (`StartMenuController`): tocar una tarjeta la describe y "Empezar con X" inicia; la partida guardada no se borra hasta ese momento. `Progression.Profile` guarda y restaura el elegido por `guardianId`, sin cambiar el formato de la partida; `PlayerStats` le pone sus sprites al jugador. Catálogo en `Resources/Personajes/CatalogoGuardianes`, que rehace `GuerreroCharacterAssets`.
- **Partidas viejas:** traen `jaguar` o nada: siguen como Balam. Un id desconocido también, con un aviso en el log.
- **Comprobado:** captura del panel a 1920×1080 y a 1280×576, sin desbordes (con una prueba temporal, ya borrada).
- **Sin probar:** con mando, táctil en un dispositivo real, y jugando en un build.
- **Pendiente (etapa 4):** diálogos que llaman "jaguar" al jugador; atributos iguales en los 7; `OBJ_marca_jaguar` no da nada a los demás (`guardianOnly: jaguar`) y hay que revisar `BEN_instinto_jaguar`; afinidades de Naira, Itzel y Balam.
- **Siguiente:** que el autor decida si cambia la regla de la forma animal (`Docs/Personajes/Protagonista.md`).

---

## 2026-11-06 — i7-10700: Balam reemplaza al jaguar jugable (etapa 2)

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `31e2f4d` (etapa 1). Sin push.
- **Hecho:** `GuerreroCharacterAssets` crea 35 `DSS_Guerrero_*` y 7 `GUA_` con todas sus animaciones. `PlayerPrefabUpgrade` pone a Balam en `Jugador.prefab` (cápsula 1,1 m). Balam conserva el id `jaguar`: partidas guardadas y `OBJ_marca_jaguar` siguen valiendo. Las escenas no se regeneraron: heredan del prefab.
- **Cambio frente a la etapa 1:** los guerreros van a **48 PPU**, no 32: el jugador no escala su raíz y a 32 mediría 1,69 m frente a 1,125 m de los NPC.
- **Capturas:** la herramienta no mostraba al jugador en ninguna (en modo edición el billboard no gira); corregido. Nuevo menú **Crónicas → Capturar guerreros (pasarela)**.
- **Pruebas:** EditMode 87/87, PlayMode 72/72 (PlayMode antes de tocar la herramienta de captura, que es solo de editor).
- **Pendiente de probar:** jugarlo; rendimiento en i5 y Android sin cambios esperados. Los 3 `caido()` se ven mal en la pasarela.
- **Incoherencia conocida hasta la etapa 4:** los diálogos siguen llamando "jaguar" al jugador; atributos iguales en los 7.
- **Siguiente:** etapa 3 (pantalla de selección en Nueva partida, guardar `guardianId`, aplicar al cargar).

---

## 2026-11-06 — i7-10700: sprites de los 7 guerreros (etapa 1)

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `6e9a5f4`. EditMode 80/80; PlayMode sin ejecutar. Sin push.
- **Decisión del autor:** en vez del jaguar, se podrá elegir entre varios guerreros, y se empieza por el arte.
- **Hecho:** `recortar_guerreros.py` genera 135 sprites en `Sprites/Guerreros` (carpeta aparte: el flujo de Blender borra de las suyas lo que no genera). Ameyali medida de nuevo (el autor rehízo su ficha). Muerte de Balam, Xali, Itzel y Yaret con sus hojas (cuadros Daño, Caída, Desplome). `PixelArtSpriteImporter` trata la carpeta como la de NPC (32 PPU).
- **Provisional:** Naira, Ameyali y Ocel caen con `caido()` (su Daño tumbado, se ve rígido) hasta tener hoja de muerte. Naira y Ameyali, un solo cuadro de ataque. Quedan restos: chispa en Ataque 2 de Itzel, verde en el de Yaret, morado en el de Ocel, hojas en el Daño de Naira.
- **Sin hacer:** aún nada usa estos sprites: el jugador sigue siendo el jaguar. Falta: etapa 2 (sets, `GUA_`, `PlayerPrefabUpgrade`, colisión), etapa 3 (pantalla de selección), etapa 4 (historia y afinidades; el autor no ha decidido si cambia la regla de la forma animal).
- **Siguiente:** hojas de revisión aprobadas por el autor; sigue la etapa 2.

---

## 2026-11-06 — i7-10700: vista 3/4 frontal subida y jugada

- **Rama / último commit:** `feature/prototipo-olmeca` @ `6e9a5f4` (subido). Esta entrada queda sin commit.
- **Hecho:** commit y push de la vista 3/4 frontal; documentos corregidos: 48 PPU el arte de Blender y 32 los NPC y los efectos.
- **Jugado por el autor:** "se siente bien". No detalló qué zonas recorrió.
- **Sin comprobar:** cima del montículo, entrada a la mazmorra, vuelta por la escalera, tirón en la aldea; rendimiento en el i5 y en Android. Las pruebas no se repitieron: solo cambiaron documentos.
- **Push desde Claude Code:** Git no puede preguntar la cuenta, hay dos guardadas; usar `git -c credential.username=JohnLopezo push`.
- **Siguiente:** a decidir por el autor (`Docs/Estado_y_Plan.md` §6).

---

## 2026-11-05 — i7-10700: vista 3/4 frontal aplicada en Unity

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `bc3dce7` (vista aprobada, referencias de mapa y ficha de Ameyali). Lo de abajo se subió el 2026-11-06 en el commit siguiente (unos 840 archivos, casi todos sprites, sus `.meta` y escenas). `UpgradeLog.htm` no debe subirse.
- **Hecho en el flujo de arte:**
  - Terreno de ciudad y selva pintado por píxel (`pintar_terreno`): sin cuadrícula, caminos irregulares, patios, orillas con talud. Los tiles quedan solo para las escenas de prueba.
  - Muro de selva, plantas de borde de camino y orillas **horneados en el suelo** (`decorar_terreno`): sin GameObjects ni colisión. El río sigue fuera de los límites con celdas de agua extra.
  - Selva por grupos (`enriquecer_selva`), enredaderas en la ceiba, lienzos ampliados (sin avisos de recorte) y `--reusar` para rehacer composiciones en 1-2 minutos.
  - Montículo: rampas alineadas de frente (la cima del nivel 3 empieza en Y = 16), sin las cárcavas que parecían estacas.
  - **Girados hacia la cámara:** `Entrada_Estructura` (movida al norte de la explanada), `Escalera_Subida` y `Santuario_Memoria` (al muro del fondo). Cambian los puntos de juego de selva y pisos.
  - Cuadrúpedos (`personajes.GIRO_DIRECCION`): N y S girados 25°, porque de frente o de espaldas el jaguar se veía como un tubo. El Guardián sigue igual.
- **Hecho en Unity (el autor aprobó la muestra):** `IsometricProjection` a 48 px/m y `Euler(41,81°, 90°, 0°)`; sprites reimportados; atlas, ciudad, selva y pisos regenerados; colisiones y portales de las piezas giradas; límites de cámara sin el margen de 7 m del rombo.
- **Pruebas:** EditMode 80/80, PlayMode 72/72. Solo se ajustaron pruebas con valores de la vista anterior (45,25 px, pivote de la palma, entrada de la selva, esquiva que cruzaba el portal nuevo). Nueva: altura de la rampa a la cima.
- **Capturas en Unity revisadas:** plaza, aldea, montículo, despertar, entrada y claros de la selva, llegada del piso 1 y arena del piso 3. La **primera captura de cada tanda** saca un personaje en blanco o negro; el mismo sale bien en la siguiente, así que es del render en batch, no del juego (sin comprobar a mano en Play).
- **Rendimiento en el i7 (una ejecución, `Docs/Rendimiento.md`):** lo más lento en Bajo es la aldea, 4,41 ms (227 FPS). Ciudad, piso 2 y arena no empeoran frente a la última medición; la selva empeora en Alto (278–286 FPS, antes 299–302). Picos de hasta 64 ms en Bajo, en la primera pasada por la aldea; sin distinguir si son de la primera visita o de Bajo.
- **Sin comprobar:** jugar a mano (controles, subir a la cima, entrada a la mazmorra, volver por la escalera); rendimiento en el i5 y en Android.
- **Pendiente de arte:** las cimas del montículo se ven planas; el agua repite su patrón; `Basalto_Agrietado` sigue mirando a +X; decidir qué hacer con `Olmeca_Prototipo`, que conserva su arte isométrico bajo la cámara nueva (sus pruebas pasan).
- **Siguiente:** que el autor lo juegue; aislar los picos de Bajo; medir en el i5 y en Android.

---

## 2026-11-04 (cierre) — i7-10700: nueva vista 3/4 frontal, en muestra

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `6d428a1`; subido el 2026-11-05 en el commit siguiente, junto con la ficha de Ameyali (`Docs/Guerreros/`, sin integrar).
- **Petición del autor:** mejorar el mapa y que no se vea tan diagonal, porque en PC hacen falta dos teclas para seguir un camino. Referencias en `Docs/Creacion de mapa/`.
- **Causa:** el movimiento ya va con la pantalla; lo diagonal es que los caminos siguen la rejilla, que la cámara ve girada 45°.
- **Decisiones del autor:**
  - Vista **3/4 frontal**, aunque sus referencias son isométricas: se adapta su estilo, no su ángulo. Las cambia también selva y mazmorra, porque la cámara es única.
  - De las referencias se toma la riqueza (selva densa, flores, musgo, enredaderas, bordes de camino, cercas, agua). Se mantiene la base histórica (`Docs/Arte_Olmeca_Demo.md` §8): montículos de tierra, columnas lisas y palmas nativas; sin pirámides de piedra, cabezas sobre ellas ni cocoteros.
- **Hecho, sin commit:** cámara nueva en `pixelart_comun.py`, `generar_olmeca.py` y `personajes.py`:
  - `PPU = 48`, tile de 48×32 px, `CAM_ROT` X = 48,19°, `DERECHA`, `ARRIBA`, `VISTA` y `HACIA_SOL` nuevos.
  - Orden de dibujo por `-Y`, `GIRO_DIRECCION = (90, 45, 0, -45, -90)`, `TAM_` y `CENTRO_` de ciudad y selva.
  - **Unity no se tocó:** los sprites y escenas siguen siendo los isométricos. **Mientras tanto, ejecutar el flujo solo con `--sin-unity`**, o mezclaría la cámara nueva con el juego sin adaptar.
- **Muestra:** se ve bien y los caminos quedan rectos; **el autor aprobó la vista y la inclinación el 2026-11-05**. Está en la carpeta temporal de la sesión, así que puede perderse. Para rehacerla (unos 10 minutos en el i7): `blender.exe --background --factory-startup --python Tools\ArtPipeline\generar_olmeca.py -- <carpeta> --sin-unity`. Mirar `Ciudad_2600x1960.png`, `Ciudad_Aldea_...` y `Ciudad_Plaza_...`.
- **Sin revisar:** la selva y la mazmorra en la muestra; nada de esto se ha probado dentro de Unity. Las pruebas no se ejecutaron, porque el juego no cambió.
- **Archivos del autor sin commit:** movió `Docs/Referencia_Visual_Olmeca*.png` a `Docs/Creacion de mapa/`; `Docs/Arte_Olmeca_Demo.md` y `generar_olmeca.py` aún citan la ruta antigua. `UpgradeLog.htm` no es del proyecto y no debe subirse.
- **Siguiente:** plan en `Docs/Estado_y_Plan.md` §6, "Vista 3/4 frontal": paso 1, arte con segunda muestra.

---

## 2026-11-04 (noche) — i7-10700: artesano y anciana sin muñecos

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `7c291b4`, subido.
- **Hecho:** a petición del autor, el artesano de jade usa el arte del Comerciante y la anciana el de la Curandera, en la aldea y en el prefab `Artesano`. Mismos id, diálogos, patrullas y misiones. Borrados sus PNG provisionales, `DSS_Artesano`, `DSS_Anciana` y sus paletas.
- **Decisión del autor:** se quedan por ahora el jaguar jugable, sus enemigos, los jefes y la figura espiritual. `Docs/Guerreros/` queda sin integrar.
- **Pruebas:** EditMode 80/80, PlayMode 72/72. Capturas de ambos en `Logs/Capturas`.
- **Sin comprobar:** el prefab `Artesano` visto en `Olmeca_Prototipo` (escena congelada, solo pasan sus pruebas); jugar a mano.
- **Siguiente:** fichas propias del artesano y la anciana, porque hoy se ven repetidos.

---

## 2026-11-04 — i7-10700: NPC con arte final, guardias e incursiones

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `f20a2c6`. Todo este trabajo se subió en un solo commit, por petición del usuario.
- **Hecho:** fichas del autor (`Docs/NPC/`) en el juego. Detalle en `Docs/Personajes/NPC_Olmecas.md` y `Docs/Historia/Misiones.md`.
  - `recortar_npc.py` y `NpcCharacterAssets`: reposo, caminata y ataque. Van en `Sprites/Npc` porque el flujo de Blender borra de sus carpetas los PNG que no genera.
  - 19 personajes con arte, 24 NPC en la ciudad, todos patrullan salvo la figura espiritual.
  - `NpcDefender`, y `EnemyChaser` con objetivo más cercano y marcha.
  - **Cambio de historia del autor:** el guardia (no confundir con los Guardianes) protege la aldea; puede haber uno o muchos.
- **Corregido:**
  - Los personajes salían un 18 % más estrechos fuera de Play (capturas y vista de escena): `SpriteBillboard` solo gira en Play. Ahora la escena los guarda ya orientados.
  - **Causa probable de la inestabilidad de `Player_CanWalkUnderShelterRoof`** (anotada el 2026-11-03): cogía una enramada cualquiera, sin ordenar, y fallaba siempre con el mismo valor (0,7056). Ahora usa siempre la misma; no se ha repetido en dos ejecuciones. Al fallar dejaba su escena cargada y por eso caían en cadena `Scenery_IsOnPixelGrid` y `Charge_IsTelegraphed`. No se comprobó qué bloquea el paso en la otra enramada.
- **Pruebas:** EditMode 80/80. PlayMode 72/72, dos ejecuciones seguidas (`NpcDefenderTests` y `Raid_ComesAfterTheJungleQuest_AndGuardsDefeatIt` son nuevas). Medición en `Docs/Rendimiento.md`.
- **Sin comprobar:** jugar a mano la animación y el combate; el guerrero de la selva en una captura; Android y el i5.
- **Fichas nuevas sin integrar:** el autor añadió `Docs/Guerreros/` con 6 fichas: Balam (jaguar), Xali (ajolote), Naira (lobo), Itzel (águila real), Yaret (quetzal) y Ocel (xoloitzcuintle). Solo se revisó la de Balam. Nada de esto está en el juego.
- **Estado al cerrar:** trabajo en `cd1afec`; las fichas y esta nota van en el commit siguiente. Push hecho, sin cambios locales.
- **Siguiente:** decidir con el autor qué hacer con `Docs/Guerreros/` (`Docs/Estado_y_Plan.md` §4, punto 5); después, revisión del autor en el Editor y fichas del artesano, la anciana y la figura espiritual.

---

## 2026-11-03 (tarde) — i7-10700: menú de inicio

- **Rama / último commit:** `feature/prototipo-olmeca` @ `e57e1e3` (aldea A3 y jaguares, subido).
- **Hecho:** etapas 1–4, autorizadas, con Opciones y fondo liso.
  - `GameBootstrapper` ya no continúa ni carga solo: pide el menú (`StartMenuRequested`) y tiene `StartNewGame`.
  - `StartMenuController` y `StartMenu.uss`: Continuar, Nueva partida con confirmación, Opciones (reutiliza `QualityOptionsPanel.SelectLevel`) y Salir.
  - `StartMenuSetup` lo añade a `Boot`.
  - `OlmecaPrototypeGenerator.CreateEventSystem` queda separado para reutilizarlo.
  - Build nuevo `WindowsGameBuild`, que arranca en Boot.
- **Error encontrado en el build:** el menú no se dibujaba porque `StartMenuSetup` cargaba `PanelSettings` y la hoja de estilos antes de `OpenScene`, que los descarga, y la escena guardaba referencias vacías. Corregido. `StartMenu_HasPanelSettings_AndItsStyleSheet` lo vigila.
- **Pruebas:** EditMode 80/80 y PlayMode 67/67. `StartMenuTests` es nueva (5 pruebas).
  - Las pruebas del HUD de `OlmecaPrototypeSceneTests` y `OlmecaCiudadSceneTests` buscan ahora el `HudController`, porque `Boot` ya tiene otro `UIDocument`.
  - Una de cuatro ejecuciones completas falló en `Player_CanWalkUnderShelterRoof` y `Charge_IsTelegraphedAsALine_AndHitsAlongIt`, y en cadena en `Scenery_IsOnPixelGrid_AndAltarsCanBeExamined`. Las otras tres pasaron. Parecen inestables; no se ha aislado la causa.
- **Build comprobado:** muestra el menú con Continuar desactivado (sin partida) y el foco en Nueva partida. La navegación con teclado o mando no se probó en el build, solo la lógica en PlayMode.
- **Siguiente:** el usuario prueba el menú (teclado y mando en el build). Commit y push hechos por petición del usuario.

---

## 2026-11-03 — i7-10700: aldea A3 (referencias visuales 3–5)

- **Rama / último commit:** `feature/prototipo-olmeca` @ `199a8e9`.
  - Al empezar ya había 20 archivos locales sin subir que no estaban en esta bitácora: animación direccional, `EnemyChaser`, `Jugador.prefab`, las 5 escenas y `generar_olmeca.py`. El usuario pidió seguir encima de ellos.
- **Hecho:** etapas A3–D3, autorizadas. Detalle en `Docs/Arte_Olmeca_Demo.md` §12, «Etapa A3».
  - Recursos nuevos de aldea y casas rehechas.
  - Patios, vegetación, fogones, rocas, troncos, bancal y pasto nuevo en la zona de la aldea.
  - Colisiones nuevas en `PixelArtSceneBuilder`.
  - Flujo completo regenerado (unos 10 min en el i7) y `Olmeca_Ciudad` regenerada.
- **Estado al cerrar:** EditMode 80/80 y PlayMode 62/62. Medición en `Docs/Rendimiento.md`: la aldea, 4,35 ms en Bajo.
  - Commit y push de todo, por petición del usuario, en un solo commit: lo que ya había en local y A3.
- **Siguiente:**
  1. El usuario revisa la aldea en el Editor y aprueba A3 o pide ajustes.
  2. Medir en el i5.

---

## 2026-10-30 (fin) — i7-10700: M5, sistema de recompensas

- **Rama / último commit:** `feature/prototipo-olmeca` @ `bf08922` al empezar. M5 se revisó y se subió en un
  solo commit, por autorización del usuario.
- **Hecho (fases 1–6):**
  - **Núcleo:** `Core/Progress` (`Inventory`, `Progression`, `RewardService`, `Economy`), `SaveData` v2 con migración
    y los ScriptableObjects de `Data/Rewards`.
  - **Contenido olmeca:** lo genera `OlmecaRewardAssets` en `Data/Rewards/Olmeca` y los catálogos en
    `Resources/Recompensas`. Los iconos de 16 px los dibuja `ItemIconFactory`.
  - **Jugador:** `PlayerStats` (estadísticas y consumibles) y `PlayerAbility` (golpe de tierra) se añaden al prefab
    con `PlayerPrefabUpgrade`.
  - **Mundo:** `EnemyReward`, `RewardChest`, `RewardOnEnter`, `BreakableBasalt`, `DungeonCompletion` (jefe, nota y
    cierre), `ServicePoint` en la ciudad y el `MemoryShrine`, que recarga la energía y abre el Museo.
  - **Interfaz:** `GameMenuController`, con el HUD de energía, nivel y experiencia, los avisos, el menú (Bolsa,
    Bendiciones, Fabricar, Comerciar y Museo) y las pantallas de elección y de resultados.
  - **Arte en Blender:** `Ofrenda_01`–`04` (caja y vasija, cerradas y abiertas) y `Basalto_Agrietado_01`.
  - **Pisos:** una alcoba de 2 × 2 celdas por piso tras el basalto agrietado (sin pisos nuevos) y los puntos de las
    ofrendas en `PISOS`.
  - **Misión `MP_OLM_06`:** con su rama nueva de la anciana (`DLG_anciana` se rehízo conservando el GUID) y el texto
    nuevo del altar.
  - **Escenas regeneradas:** ciudad, selva y pisos 1–3.
  - **Documentos:** `Docs/Sistemas/` (Recompensas, Inventario, Progresión y Fabricación) y `Docs/Contenido/` (tabla,
    catálogo y Bendiciones). La tabla de equilibrio con los valores de antes y después está en
    `Sistema_Progresion.md` §1.
- **Pruebas:** EditMode 80/80 (`RewardSystemTests` es nueva) y PlayMode 59/59 (`OlmecaRecompensasTests` es nueva).
  Tres pruebas de combate antiguas se ajustaron a la escala ×10 y a la defensa.
- **Rendimiento (piso 2, i7):** Bajo unos 245 FPS, Medio 295 y Alto 260. Son unos 0,6 ms más por cuadro que en M3 en los tres niveles; la causa no se ha aislado y sigue lejos del presupuesto. Falta medir en el i5 / HD 530.
- **Siguiente:**
  1. El usuario juega M5 en el Editor y anota lo que se sienta mal.
  2. Decidir el plan de arte de `Docs/Personages.png`.
  3. Conexión al arco II.

## 2026-10-30 (cierre) — i7-10700: M5 en curso

- **Rama / último commit:** `feature/prototipo-olmeca` @ `bf08922` (M4, subido).
- **M5 = sistema integral de recompensas.** La propuesta del usuario es la que se pegó en la sesión del 2026-10-30: botín, inventario, experiencia, fabricación, comercio, Bendiciones, Museo de la Memoria, pantalla de resultados y documentos en `Docs/Sistemas` y `Docs/Contenido`. Se trabaja en las fases 1 a 6 del plan sin pedir aprobación archivo por archivo; solo se para ante decisiones que cambien la jugabilidad.
- **Decisiones del usuario:**
  1. **Escala ×10:** el jugador pasa a 50 de vida y 10 de daño; se ajustan enemigos, jefes, curaciones y defensas. Hay que documentar los valores antes y después, y probar que el combate se siente igual.
  2. **Energía SIN regeneración automática.** Solo se recupera en los santuarios de Memoria, con consumibles y con ciertas recompensas o habilidades.
  3. **Bendición de la Tierra obligatoria** al vencer al Guardián: es el golpe de tierra, que daña y rompe el basalto agrietado. Además se elige una Bendición secundaria entre tres. Hay dos ranuras: la principal, fija e intocable, y la secundaria.
  4. **Moneda «ecos», ficticia.** La documentación debe decir que no es una moneda histórica. También se podrá intercambiar materiales con los NPC.
  5. **Experiencia:** menos experiencia por encima del nivel de la zona y protección contra el farmeo de enemigos reaparecidos. Explorar zonas nuevas debe rendir más. El mínimo y la reducción son configurables.
  6. **Museo de la Memoria:** un panel que se abre desde el santuario de Memoria. Muestra culturas, Guardianes vencidos, reliquias, Bendiciones, fragmentos, coleccionables e información narrativa. La sala física se deja preparada para después.
  7. **Penalización por caer = 0**, configurable pero desactivada.
  - **Condiciones:** no añadir pisos (siguen siendo 3), que todo funcione dentro de Unity, prioridad al rendimiento en la HD 530 y sin commits sin autorización.
- **Hecho hasta ahora:** solo la entrada. En `InputSystem_Actions` se renombraron acciones que nadie usaba:
  - `Crouch` pasa a `Ability` (C / botón Este), para el golpe de tierra.
  - `Previous` pasa a `UseItem` (1 / cruceta izquierda).
  - `Next` pasa a `Menu` (I / Select).
- **Siguiente (plan acordado):**
  1. **Documentos de diseño** en `Docs/Sistemas` y `Docs/Contenido`.
  2. **Núcleo en Core:** `Inventory`, `Progression` y `RewardService`, todos `ISaveParticipant` con `SaveData` v2, más los catálogos en Data con el patrón de `IQuestCatalog`.
  3. **Integración:** botín de enemigos, ofrendas (cofres) que se abren una vez, `PlayerStats` y energía, golpe de tierra y basalto agrietado, consumibles, avisos y barras en el HUD.
  4. **Final de la mazmorra:** recompensas del Guardián, elección de Bendición y resultados C/B/A/S.
  5. **Comunidad:** fabricación con el artesano, comercio con el comerciante, `MP_OLM_06` y el Museo.
  6. **Pruebas y FPS.**

## 2026-10-30 (noche) — i7-10700

- **Rama / último commit al empezar:** `feature/prototipo-olmeca` @ `cb81ef3` (M3).
- **Decisiones del usuario (M4):**
  - La arena va en la misma escena del piso 3.
  - El Guardián se modela en Blender.
  - El agua de la fase 2 son charcos en el borde de la arena.
  - M4 termina con la liberación; las recompensas son de M5.
- **Hecho (M4):**
  - **Avisos:** `TelegraphMarker` tiene ahora tres formas (círculo, anillo y franja), con sus sprites en `EffectSpriteFactory`.
  - **Componentes nuevos:** `SealedPassage` (la losa que se abre al cerrar su mensaje) y `StoneGuardian`.
    - El Guardián tiene tres fases por vida y siete ataques en corrutinas (zarpazo, embestida con aturdimiento, golpe de tierra, ondas, fragmentos, rugido y Furia), con invulnerabilidad al cambiar de fase.
    - `ForceAttack` sirve para las pruebas.
  - **Arte en Blender:** el Guardián (figura felina geométrica con tocado y disco de la Primera Memoria) en 7 poses. La arena es circular, con columnas, raíces y un borde que se inunda.
  - **Escena:** piso 3 regenerado con la losa, la arena, el agua de la fase 2 (objeto inactivo con `SlowZone`), el Guardián, la figura liberada y `StartQuestOnLoad` de `MP_OLM_05`.
  - **Misión:** `MP_OLM_05` («La Piedra Viva»), con los pasos abrir la losa y vencer al Guardián.
  - **Herramientas:** build de medición de la arena (`BuildArena`) y su vista en las capturas.
- **Estado:**
  - Pruebas: EditMode 57/57 y PlayMode 54/54. Las nuevas están en `OlmecaGuardianTests`: losa, fases y agua, embestida, ondas, Furia, y liberación con misión sin que reaparezca.
  - Capturas de Unity revisadas a ojo; el tocado se estrechó y se quitó musgo tras verlas.
  - FPS de la arena en `Docs/Rendimiento.md`.
  - **No se ha jugado en el Editor.** Commit y push al cerrar la sesión.
- **Siguiente:**
  - Jugar la mazmorra completa en el Editor y ajustar la dificultad del Guardián.
  - Aprobar el arte de M1 a M4.
  - Plan de M5: fragmento de la Primera Memoria, habilidad de tierra, Bendición, `MP_OLM_06` y conexión al arco II.

## 2026-10-30 (tarde) — i7-10700

- **Rama / último commit al empezar:** `feature/prototipo-olmeca` @ `276d10d` (M2).
- **Decisiones del usuario (M3):**
  - Se añade la esquiva.
  - El Jaguar de Piedra se modela en Blender como escultura rígida.
  - Los pecaríes se quedan para la selva.
  - El eco borrado entra en M3.
- **Hecho (M3, pisos 1 y 2):**
  - **Combate:**
    - Esquiva en `PlayerMovement`. La acción `Jump` pasa a llamarse `Dodge`.
    - Componentes nuevos: `TelegraphMarker` (aviso en el suelo con su material `M_PixelArt_Aviso`), `Projectile`, `SlowZone`/`ISlowable` y la barra del jefe en el HUD.
  - **Enemigos:** `ErasedEcho` (sprite provisional con franjas borradas) y `StoneJaguar` (zarpazo, salto y rugido avisados). `EnemyChaser` también se frena en el agua.
  - **Arte en Blender:** el Jaguar en 5 poses × 5 direcciones más la pose liberada (`Sprites/Personajes`), agua baja en el Pantano y trazados nuevos de los pisos 1 y 2.
  - **Escenas:** El Umbral con `TutorialHint`. El Pantano con charcos, ecos y la arena con la bajada tapada hasta vencer al Jaguar. Pisos y selva regenerados.
  - **Misión:** `MP_OLM_04`. Se recreó `DLG_guerrero_joven_entrada`, que ahora la inicia.
- **Corregido durante las pruebas:**
  - El Jaguar llamaba a `Move` con el controller apagado al empezar el salto.
  - Las zonas lentas dependían de `OnTriggerExit`, que Unity no envía al teletransportarse. El jugador podía quedar frenado tras reaparecer; ahora frenan con `OnTriggerStay`.
- **Estado:**
  - Pruebas: EditMode 57/57 y PlayMode 48/48. Las nuevas están en `OlmecaMazmorraCombatTests` (esquiva, pista del tutorial, agua baja, eco borrado, el Jaguar con la misión y que no reaparezca), y `OlmecaMazmorraSceneTests` está ajustado.
  - Capturas reales de Unity revisadas a ojo; el Jaguar se agrandó tras verlas.
  - FPS del Pantano en `Docs/Rendimiento.md`.
  - **No se ha jugado en el Editor.** Commit y push al cerrar la sesión.
- **Siguiente:**
  - Jugar la mazmorra en el Editor: dificultad del Jaguar y de los ecos, y sensación de la esquiva.
  - Aprobar el arte de M1, M2 y M3.
  - Plan de M4: la arena del Guardián de la Piedra Viva.

## 2026-10-30 — i7-10700

- **Rama / último commit al empezar:** `feature/prototipo-olmeca` @ `a8759bc` (al día con origin, árbol limpio salvo `Docs/Desarrollo juego México.html` y su carpeta, una conversación guardada que no se tocó).
- **Decisiones del usuario (M2):**
  - Al caer se conservan la historia, las misiones y los jefes vencidos, y los enemigos normales reaparecen.
  - El santuario de Vida cura el 50 %, una vez por santuario y por incursión, y se reenciende al salir de la mazmorra.
  - Boot continúa la partida guardada directamente, sin menú todavía.
  - El arte nuevo va dentro de M2.
- **Hecho (M2, base de la mazmorra):**
  - **Arte en Blender:** losas, muros de columnas, santuarios de Memoria y de Vida (dos estados cada uno) y escaleras de subida y bajada.
  - **Trazado:** `planificar_piso` exporta `Mazmorra_P1–3.json`, con las celdas de vacío. La opción `--solo-mazmorra` sirve para revisar sin exportar.
  - **Escenas:** `OlmecaDungeonGenerator` crea `Olmeca_Mazmorra_P1–3`. Selva regenerada con el portal al piso 1 (abre con `MP_OLM_03` completada) e `IncursionReset`.
  - **Sistemas:** `PlayerCheckpoint` (participante del guardado), `MemoryShrine`, `LifeShrine`, `DefeatReturn` y `DungeonFlags`.
  - **Cambios en lo existente:** `StoryState.ClearFlagsWithPrefix`, `PlayerDamageResponse.RespawnInPlace`, `EnemyChaser.respawns` y `GameBootstrapper.ContinueSavedGame`.
  - **Menús nuevos:** Crónicas → Generar mazmorra / Capturar mazmorra / Borrar partida guardada / Build de medición de la mazmorra.
- **Estado:**
  - Pruebas: EditMode 57/57 y PlayMode 42/42. Las nuevas son `Checkpoint_IsSavedAndRestored_WithTheStory`, `StoryState_ClearFlagsWithPrefix_RemovesOnlyThoseFlags` y las 6 de `OlmecaMazmorraSceneTests`: pisos, viaje selva ↔ piso 3, santuario y derrota, derrota sin punto de control, santuario de Vida e incursión, y continuar desde Boot.
  - Capturas reales de Unity revisadas a ojo.
  - FPS del piso 2 anotados en `Docs/Rendimiento.md`.
  - **No se ha jugado en el Editor.** Commit y push al cerrar la sesión.
- **Siguiente:**
  - Jugar el arco y la mazmorra en el Editor, y aprobar el arte de M1 y M2.
  - Plan de M3: El Umbral, El Pantano y el Jaguar de Piedra.

## 2026-10-29 (tarde) — i7-10700

- **Rama / último commit al empezar:** `feature/prototipo-olmeca` @ `cdccc66` (al día con origin, árbol limpio).
- **Hecho:** Boot arranca en `Olmeca_Ciudad`.
- **Hecho (M1, la selva):**
  - **Viaje entre escenas:** `SceneTravel`, `ScenePortal`, `SpawnPoint` y `Health.CarryOver`.
  - **Arte en Blender:** entrada a la estructura, grietas del olvido y fragmento de memoria; rampas de luz propia Olvido y Jade. Sendero oeste en la ciudad, sin mover ningún objeto.
  - **Escenas:** `planificar_selva` y la escena `Olmeca_Selva`. `PixelArtSceneBuilder` reúne lo común a `OlmecaCityGenerator` y `OlmecaJungleGenerator`.
  - **Misión `MP_OLM_03`:** el guerrero la encadena al cerrar `MP_OLM_02` y espera en la entrada. Tres espíritus corrompidos que no reaparecen y dejan fragmentos (`ActiveWhenFlags`, `MemoryFragmentInteractable`, `CorruptionPulse`).
  - **Herramientas:** capturas y medición para la selva.
- **Decisiones del usuario:** la selva va en una escena aparte; el guerrero espera en la entrada; 3 espíritus y 3 fragmentos.
- **Estado:** EditMode 55/55, PlayMode 36/36. Las nuevas son `CarryOver_LowersToTheCarriedHealth_ButNeverRaisesIt` y, en `OlmecaSelvaSceneTests`, la escena, la misión y el viaje ciudad → selva → ciudad. Las capturas reales de Unity se revisaron a ojo. FPS de la selva en el i7, en `Docs/Rendimiento.md`. **No se ha jugado en el Editor.** Commit y push al cerrar la sesión.
- **Siguiente:**
  - Jugar el arco en el Editor y aprobar el arte de M1.
  - Plan de M2, la base de la mazmorra.

## 2026-10-29 — i7-10700

- **Rama / último commit al empezar:** `feature/prototipo-olmeca` @ `1d3eb5c` (al día con origin).
- **Hecho:** se crea esta bitácora, enlazada desde `CLAUDE.md` y como paso de la rutina diaria (`Docs/DesarrolloMultiPC.md` §5). Se corrigen avisos desactualizados en `Docs/DesarrolloMultiPC.md` §10 y `Docs/Arte_Olmeca_Demo.md` §12 (trabajo "sin commit en el i7" y la decisión sobre versionar `Salida/`, que ya va por LFS).
  Commit `88c918c` (bitácora y avisos); se descartó una regeneración sin cambios de `Olmeca_Ciudad.unity`.
- **Hecho (cámara de fondo):** usa el renderer sin efectos (índice 1 añadido a los tres URP Assets) y no pide HDR, MSAA, sombras ni texturas. Escena regenerada. En el i7, Alto pasa de 99–114 a 286–300 FPS y Medio de 202–222 a 271–349; Bajo igual (`Docs/Rendimiento.md`). La medición ahora activa `runInBackground` y ya no se pausa al perder el foco.
  Commit `6144f3f`.
- **Hecho (narrativa, fase 1):** la biblia del autor queda en `Docs/Historia_Mundo.md` (texto íntegro y decisiones de integración al inicio). Derivados del arco olmeca: `Docs/Historia/` (Capitulos, Misiones, Glosario), `Docs/Personajes/` (Protagonista, NPC_Olmecas), `Docs/Culturas/Olmeca.md`, `Docs/Enemigos/Olmeca.md`, `Docs/Mazmorras/Olmeca.md`. `Diseno.md` §2, §3, §4, §7.1 y §11 llevan notas de alineación (lo anterior se conserva como antecedente) y `CLAUDE.md` apunta a la biblia.
- **Decisiones:** nombres de la biblia; protagonista sin género; Mariposa monarca como Guardián y Vaquita a futuro; cinco arcos con las culturas del GDD dentro; vertical slice de 3 pisos y meta de 8; jefe = fragmento + habilidad fija + acceso + Bendición a elegir.
  Commit `6f74768`. Después el usuario decidió: **Guardián del vertical slice = Jaguar**; el prólogo del GDD se sustituye por **tutorial en los primeros pisos olmecas**.
- **Abierto:** epiclásico y tolteca dentro del arco III; afinidades de los arcos III–V.
- **Hecho (fase narrativa 2, N1–N3):** `StoryState` (banderas y avisos), `NpcData` y `DialogueData` con ramas; `DialogueInteractable` las usa (conserva las líneas fijas); `QuestData`, `QuestCatalog` y `QuestService`; `InscriptionInteractable` con id, `ReportOnDefeat` y `ReportOnEnter`; objetivo de misión en el HUD; `SaveService` (JSON con versión y respaldo). Boot registra los tres servicios; nada guarda ni carga aún en juego.
  Commit `210265a`.
- **Hecho (N4):** artesano, anciana, joven guerrero y comerciante en `Olmeca_Ciudad` con sprites provisionales (misma silueta, paletas propias), `NpcData` y `DialogueData`; misión `MP_OLM_02` jugable de principio a fin; catálogo de misiones en `Resources`; estelas con texto alternativo tras la misión. Los assets narrativos los crea `OlmecaNarrativeAssets` solo si faltan (después se editan en el Inspector). `OlmecaCityCapture` añade vistas de los habitantes.
- **Estado:** EditMode 55/55, PlayMode 31/31 (nuevas: `Settlers_StandOnGround_WithTheirIdentityAndDialogue`, `SettlementQuest_IsCompletedByTalkingInOrder_AndChangesTheStelae`). Capturas revisadas a ojo; el diálogo y el HUD no se han probado jugando en el Editor.
  Commit `58da8ea`.
- **Hecho (N5):** despertar en la orilla noreste del río. El jugador empieza ahí; `MP_OLM_01` arranca sola (`StartQuestOnLoad`): asomarse al agua (recuerdos y reflejo), escuchar a la figura espiritual (juramento) y hablar con el artesano, que ahora exige el juramento para empezar `MP_OLM_02`. `GuardianData` y `GUA_jaguar` (afinidad inicial oscuridad, propuesta). Se recreó `DLG_artesano_jade` (no tenía ediciones a mano).
- **Estado:** EditMode 55/55, PlayMode 32/32 (nueva: `Awakening_StartsOnLoad_AndLeadsToTheSettlementQuest`). Capturas revisadas a ojo; no se ha jugado en el Editor.
- **Siguiente:** jugar el arco en el Editor (abrir `Olmeca_Ciudad`: Boot sigue cargando el prototipo); decidir si Boot arranca en la ciudad; arte del Jaguar y de los NPC (autor); después, plan de selva y mazmorra (`MP_OLM_03`+); escombros de basalto; medir en Android (corresponde a esta PC). Benchmark del i5 cuando se trabaje allí.

## 2026-10-28 — i7-10700

- **Rama / último commit:** `feature/prototipo-olmeca` @ `1d3eb5c` (push hecho).
- **Hecho:** ruinas olmecas (plataforma, altar, estelas) y recursos de la aldea en el flujo de pixel art de Blender; trazado `Ciudad.json` y escena jugable `Olmeca_Ciudad` (etapas A–E); `PixelArtScreen` a la resolución del arte y medición de rendimiento; ceibas, enramadas, vasijas, troncos, muelle y canoas en la ciudad; canoa rehecha y enramada que se cruza por debajo (`DepthAnchors`, `RoofOverhead`).
- **Estado al cerrar:** EditMode 42/42, PlayMode 27/27. FPS del i7 anotados en `Docs/Rendimiento.md`.
- **Siguiente:** benchmark en el i5-6500 (referencia), Android, cámara de fondo en Alto, escombros y mosaico de las ruinas.

## 2026-10-27 — i7-10700

- **Rama / último commit:** `feature/prototipo-olmeca` @ `8401228` (push hecho).
- **Hecho:** animación direccional e interacción; NPC con diálogo, combate básico y `Docs/Diseno.md`; estabilización del proyecto (Fase 1), documentación de dos PC (`Docs/DesarrolloMultiPC.md`, `Docs/Rendimiento.md`) y cambio de dirección de arte a pixel art isométrico con el flujo de Blender (`Tools/ArtPipeline/`).
- **Siguiente:** ruinas olmecas y ciudad jugable.

## 2026-10-26 y anteriores

- Sin registro de PC. Commits: base técnica (Hito 1.0, `setup-git.ps1`) y prototipo jugable olmeca HD-2D (`7d0471b`), fusionado a `main` por PR #1 y #3.
