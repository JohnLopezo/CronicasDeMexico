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

## 2026-11-11 — sensación del combate (E1) y apuntado asistido (E2)

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `bb6ab68`, con los cambios de los controles táctiles aún sin commit.
- **Pedido del autor:** un juego más profesional y cómodo, inspirado en Tunic, Eastward, Octopath Traveler II y Hyper Light Drifter. Plan de 7 etapas (E1–E7); el autor autorizó E1 y E2.
- **Hecho:**
  - `PlayerAttack`:
    - Combo de 3 golpes con avance. El tercero remata: daño ×1,5, más empuje y 0,45 s de espera.
    - Las pulsaciones se guardan 0,15 s.
    - Pausa breve al acertar (hitstop, con escala 0,05 y no 0 para que no cuente como pausa) y sacudida de cámara.
    - El apuntado asistido (`AimAssist`) gira hacia el enemigo vivo más cercano a menos de 3 m y dentro de ±60°. Lo usan el golpe y la habilidad especial.
    - `Attack()` es el golpe con entrada; `Strike()` sigue igual para las pruebas.
  - `PlayerMovement`: dos esquivas encadenadas y después la espera; la pulsación de esquiva se guarda 0,15 s; `Face(dir)`. La esquiva reinicia el combo.
  - `Core/Feedback/ScreenShake`: la piden `PlayerAttack` y `PlayerDamageResponse` (al recibir daño) y la aplica `IsometricCameraFollow`. Tiene `Enabled` para la futura opción de accesibilidad (E5).
  - Sin cambios en prefabs ni escenas: los campos nuevos toman sus valores por defecto.
- **Pruebas:** EditMode 139/139 y PlayMode 128/128. Nuevas: `Attack_ChainsAThreeHitCombo_WhoseFinisherHitsHarder` y `Attack_TurnsTowardANearbyEnemyInsideTheCone_ButNotBehind`. La de la esquiva ahora comprueba la segunda carga. **No se ha jugado en el Editor ni en el teléfono.**
- Commits `fc7211c` (controles táctiles del S24+) y `8e4cb45` (E1 y E2), con la aprobación del autor. Sin push.
- **Hecho (E3, bloqueo y esquiva perfectos; GDD §115 pide "esquive perfecto con recompensa"):**
  - `PlayerDefense`: un golpe que llega en los primeros 0,2 s de defensa se anula. Aturde 1,2 s al atacante (`IStaggerable`: `WildEnemy` y `EnemyChaser`; los jefes no, porque sus ataques son corrutinas) y da pausa breve, sacudida y destello. Los proyectiles solo se anulan. La ventana se rearma a los 0,6 s, así que pulsar sin parar no sirve.
  - `PlayerMovement`: un golpe que llega en los primeros 0,2 s de una esquiva es una esquiva perfecta. Da cámara lenta (×0,35 durante 0,45 s reales), recarga las esquivas y un destello; una cada 1,5 s como máximo.
  - `Health`: `Intercept` (decide antes del daño) y `Evaded` (golpe ignorado por invulnerabilidad). `DamageFlash.Flash()` es pública.
  - `Combat/TimeDilation` reúne el hitstop y la cámara lenta. Nunca usa escala 0 y, si el plazo vence con el menú abierto, espera a que se cierre. Antes, el hitstop de E1 podía dejar el juego a 0,05 si vencía con el menú abierto.
- **Pruebas (E3):** EditMode 139/139 y PlayMode 129/129.
  - Nueva: `PerfectBlock_NegatesTheHit_StaggersTheAttacker_AndCannotBeSpammed`.
  - La de la esquiva comprueba la esquiva perfecta y la cámara lenta.
  - La de la defensa del Jaguar espera a que pase la ventana.
- E3 en el commit `72d848a`. No se ha jugado.
- **Hecho (E4, sistema de Ruptura; detalle y tabla de debilidades en `Docs/Sistemas/Ruptura.md`):**
  - `Combat/BreakGauge` lleva el escudo y las debilidades. Los golpes del jugador con una afinidad débil y los bloqueos perfectos quitan un punto.
  - Sin escudo, el enemigo queda roto: ×1,5 de daño (`Health.Vulnerability`) y aturdido si es `IStaggerable`. Después el escudo vuelve.
  - Los golpes llevan la afinidad del Guardián (`PlayerAttack.Element`); el golpe de tierra, siempre Tierra.
  - `EnemyData` tiene `shieldPoints`, `weaknesses` y `breakSeconds` (escritos en los 7 `ED_*.asset` y en `EnemyCharacterAssets`).
  - Espíritu corrompido, eco, mini-jefe y jefe llevan sus valores en código (`shortcut:`).
  - HUD: panel `target` bajo la barra del jefe (nombre, rombos, debilidades en color y «¡RUPTURA!»), alimentado por `Core/Feedback/BreakReadout`. No se regeneró ninguna escena.
  - **La tabla de debilidades es una propuesta de Claude; falta la aprobación del autor.**
- **Pruebas (E4):** PlayMode 131/131 y EditMode 139/139.
  - `BreakGauge_WeakHitsBreakTheShield_WhichStunsAndRaisesDamage_ThenRecovers`.
  - `Bat_BreaksWithLight_AndTheHudShowsItsShieldAndWeaknesses`, en el HUD real del piso 1.
  - El bloqueo perfecto quita un punto.
- E4 en el commit `55852a3`.
- **Hecho (E5, comodidad y UI):**
  - **Cuadro de diálogo** (`UI/Hud/MessageBox`): placa con el nombre, retrato y texto letra a letra.
    - El retrato es el sprite del NPC que habla, por `Core/Feedback/SpeakerPortrait`, que fija `DialogueInteractable`.
    - Las letras que faltan van con `<alpha=#00>`, así el texto no se recoloca al escribirse.
    - La primera pulsación de Hablar completa el texto (`MessageReveal`, en `PlayerInteractor`) y aparece ▼.
  - **Números de daño** (`UI/Hud/DamageNumberLayer`, 12 etiquetas reutilizadas): los informa `Health` por `Core/Feedback/DamageNumbers`. Blancos al enemigo, dorados si está roto y rojos al jugador. Los objetos de vida 1 (vasijas) no muestran número.
  - **Barra del jefe:** el daño recibido deja una estela clara que baja despacio.
  - **Opciones de comodidad** (`Core/Feedback/ComfortSettings`, en PlayerPrefs como la calidad): sacudida de cámara, números de daño y velocidad del texto (lento, normal, rápido, al instante). Aparecen en el panel de opciones del juego y en el del menú de inicio (`UI/Hud/ComfortOptionsView`). `ScreenShake.Enabled` se sustituyó por `ComfortSettings.ScreenShake`.
  - No se regeneró ninguna escena (`Hud.uxml` y `Hud.uss` editados a mano).
- **Pruebas (E5):** PlayMode 132/132 y EditMode 142/142.
  - `Hud_TypesDialogueWithASpeakerPlate_AndShowsDamageNumbers`, en el HUD real del piso 1.
  - `ComfortSettingsTests` (3).
  - **Falta:** ver el cuadro y los números en pantalla (Editor y S24+), y probar que el retrato se ve nítido. La vibración se descartó: en Android `Handheld.Vibrate` es un zumbido largo y fijo.
- E5 en el commit `7f72125`.
- **Hecho (E6, Códice de la Memoria; detalle en `Docs/Sistemas/Codice.md`):**
  - 8 páginas escondidas, al estilo de las del manual de Tunic: 5 enseñan mecánicas y 3 son culturales, con su origen D/I/F.
  - **Cómo se revelan:** con avisos y banderas que ya existían (el reflejo del río, la entrada al piso 1, las ofrendas ocultas tras el basalto, el altar-trono y el sello del Guardián). No se regeneró ninguna escena.
  - `Core/Narrative/CodexService` guarda lo encontrado como banderas `codice.<id>`. Se registra en `GameBootstrapper` y en el guardado, después de StoryState. Al cargar recupera en silencio las páginas cuyo disparador es una bandera que ya se cumplía.
  - `Data/Narrative/CodexCatalog` tiene su asset en `Resources/Narrativa/CatalogoCodice`, creado por `CodexAssets` (menú Crónicas) solo si falta.
  - Pestaña **Códice** en el menú (encontradas con texto; las que faltan, «??? — pista») y aviso emergente `RewardNoticeKind.CodexPage`.
  - **Aviso cultural:** el Códice es un objeto ficticio. No se conservan códices olmecas, y las páginas no dicen que los olmecas los escribieran. **Los textos culturales los debe contrastar el autor.**
- **Pruebas (E6):** EditMode 145/145 y PlayMode 132/132. Nuevas: `CodexTests` (3), que incluyen la del catálogo real en Resources. No se ha visto la pestaña en pantalla.
- **Siguiente:**
  - Que el autor juegue y ajuste en el Inspector del jugador (Combo, Sensación del impacto, Apuntado asistido, Bloqueo perfecto y Esquiva perfecta).
  - Falta arte o VFX propio para el bloqueo y la esquiva perfectos (hoy es el destello blanco del daño).
  - Fijar objetivo (pendiente de E2).
  - E4–E7.

---

## 2026-11-10 — i7-10700: controles táctiles en el Galaxy S24+

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `bb6ab68`.
- **Primera prueba en un teléfono real (S24+, SM-S926B):** el juego instala, arranca y va a 30 FPS en la ciudad, pero **el joystick y los botones no respondían**, y se veían textos de PC.
- **Causa:** el EventSystem propio del prefab `Controles_Tactiles` se guardó sin sus acciones de UI. `CreateEventSystem` crea referencias que se conservan en una escena pero no en un prefab. Ese EventSystem era el activo en la partida y no recibía toques. Se encontró con un build de diagnóstico de toques (temporal, borrado). Detalle en `Docs/Controles_Tactiles.md` §3.
- **Hecho:**
  - Los controles ya no llevan EventSystem propio; usan el de cada escena.
  - Botón nuevo **Usar** (cruceta izquierda, el consumible más útil).
  - Con pantalla táctil, sin textos de teclado:
    - el aviso dice «Hablar» y no «[E] Hablar»;
    - se oculta «Bolsa (I)», porque el menú se abre con Menú;
    - el título de Consumibles cambia a «el botón Usar gasta el más útil»;
    - las pistas «moverse» y «atacar» del tutorial tienen texto táctil (`TutorialHint.TouchText`);
    - el texto de la Bendición de la Tierra nombra el botón Tierra.
  - El FPS va bajo el botón Menú.
  - Regenerados el prefab, Boot y los tres pisos de la mazmorra (en estos, solo se renumeran los fileID y se añade el texto táctil).
- **Pruebas:** nueva `RealTouch_InAGameScene_MovesTheStick_AndPressesAttack`: un toque simulado de verdad, por el EventSystem de la ciudad. Las anteriores llamaban al botón directamente y por eso no lo vieron. En modo batch hace falta `IgnoreFocus` para que el Input System no descarte los toques simulados.
- **Estado:** EditMode 139/139 (antes del botón Usar) y PlayMode 126/126. APK de desarrollo instalado en el S24+. **Sin commit ni push.** El target del proyecto quedó en Android: volver con `-buildTarget Win64`.
- **Siguiente:** que el autor confirme en el teléfono que todo responde y diga si los tamaños le acomodan; después, commit con su aprobación.

---

## 2026-11-10 — i7-10700: controles táctiles

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `d99bf16`.
- **Distribución aprobada por el autor:** joystick abajo a la izquierda; abajo a la derecha, Atacar grande con Esquivar, Defender y Especial alrededor y Tierra y Hablar encima; Menú arriba a la derecha.
- **Hecho:** `UI/Touch/TouchControls` y `SafeArea`; `TouchControlsSetup` crea el prefab y lo pone en Boot. Son `OnScreenStick` y `OnScreenButton` del Input System: un mando virtual que el juego ya entiende (no usa `PlayerInput` ni esquemas fijos), así que no cambia nada del juego. Se ven solo con pantalla táctil y se ocultan con el menú (pausa); usan su propio EventSystem solo si no hay otro.
- **Arreglo:** Atacar estaba ligado a tocar cualquier parte de la pantalla (`<Touchscreen>/primaryTouch/tap`): con los controles, cada toque del joystick habría atacado. Quitado.
- **Pruebas:** `TouchControlsTests` (4): visibilidad, que Atacar pulsa el botón Oeste del mando, que el joystick mueve el stick y que cada control tiene una acción del jugador. Captura real revisada (prueba temporal, borrada).
- **Estado:** EditMode 139/139, PlayMode 125/125. Subido.
- **Falta:** probarlos en un Android real (build de desarrollo y ajuste de tamaños). **No se ha jugado en el Editor.**

---

## 2026-11-10 — i7-10700: revisión de las animaciones de los enemigos

- **Pedido del autor:** que las animaciones de los enemigos se vean completas.
- **Revisado:** el recorte no avisa de ningún corte y no quedan cortes rectos en los 46 sprites; nada toca el borde ni se sale del pivote. Nueva `EnemyAnimations_HaveEveryFrame_InEveryDirection` (`EnemyAssetsTests`): cada animación con sus cuadros, en orden, igual en las cinco direcciones y con su duración. En el juego real, los 46 llegan a la pantalla (a 5 les «faltan» 1–2 px casi negros que se confunden con el fondo de la captura); el Espíritu del Jaguar ×2, completo.
- **Límite del arte:** una imagen por pose y solo de lado.
- **Estado:** EditMode 15/15 en `EnemyAssetsTests`. Subido.

---

## 2026-11-10 — i7-10700: pasadizo secreto y cámara del tesoro (D3)

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `e1b5f48`.
- **Hecho:** en el piso 1, la alcoba tras el basalto agrietado se alarga en un pasadizo al norte hasta una cámara del tesoro con el estrado (colisión nueva en `PixelArtSceneBuilder`), la ofrenda oculta a su pie, braseros, cristales y esteras. Solo se entra con el golpe de tierra (tras el Guardián).
- **Flujo de Blender:** opción nueva `--solo-piso N` (`exportar_piso`, `datos_region`) que exporta un solo piso sin tocar el resto. Hizo falta porque la exportación completa sobrescribiría casi todos los sprites, y porque las losas de `Salida` eran viejas: el primer intento dio al piso 1 un suelo de losas sueltas con huecos. Se renderizaron las losas actuales (`--reusar --solo-mazmorra`) y el suelo volvió a ser continuo; el piso 3, reexportado como prueba, salía 3 px distinto, así que solo cambia el piso 1.
- **Pruebas:** `Threshold_HasTheSecretPassage_ToTheTreasureRoom` (suelo del pasadizo, estrado y ofrenda en la cámara) y una prueba temporal que camina de la alcoba al estrado (pasa; borrada). Capturas reales revisadas.
- **Estado:** EditMode 132/132, PlayMode 121/121. `Floors_Load_..._VoidBlocked` usaba (0, 8) como vacío, que ahora es la cámara del tesoro: usa (-6, 8). Subido.

---

## 2026-11-10 — i7-10700: piezas colocadas en la mazmorra (D4)

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `3a8b248`.
- **Hecho:** `OlmecaDungeonGenerator.BuildProps` coloca trampas, vasijas, braseros, cristales, esteras y una cabeza en los tres pisos, con las posiciones de `Tools/ArtPipeline/colocar_piezas.py` (reglas en `Docs/Mazmorras/Olmeca.md`). El piso 1 se queda sin cerca ni cabeza: no hay sitio fuera del pasillo. Pisos regenerados.
- **Pruebas:** `OlmecaPiezasSceneTests` (3): las piezas de cada piso, sobre suelo, las de suelo por debajo de los personajes, la placa con sus dos pinchos y las vasijas con botín. Capturas reales revisadas (prueba temporal, borrada).
- **Rendimiento (i7, piso 2):** Bajo 256–284, Medio 334–342, Alto 290–296 (`Docs/Rendimiento.md`).
- **Estado:** EditMode 132/132, PlayMode 120/120. Subido.
- **Pendiente:** D3 (sala del tesoro y pasadizo secreto). **No se ha jugado en el Editor.**

---

## 2026-11-10 — i7-10700: mecánicas de la mazmorra (D2)

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `eca8d0e`.
- **Hecho (`Gameplay/Dungeon`):** `SpikeTrap` (avisa parpadeando 0,7 s, sube 1,1 s y daña a quien esté encima; periódica con desfase o activada por placa), `PressurePlate` (se hunde, enciende el glifo y activa sus trampas), `StakeFence` (daña al tocarla; el sólido va aparte) y `BreakablePot` (1 de vida en el layer Enemigo: la rompen el ataque y el golpe de tierra; botín con `EnemyReward` y la tabla nueva `BOT_vasija`, sin experiencia).
- **`DungeonPropAssets`** (editor) monta cada pieza para los pisos: pinchos y placa con el material de los avisos (encima del suelo y debajo de los personajes, para no taparles los pies); braseros y cristales animados con `SpriteFrameCycler`, sin luces. Todavía no se usa en ninguna escena (D4).
- **Pruebas:** `DungeonTrapsTests` (4). EditMode 132/132, PlayMode 117/117. Subido.

---

## 2026-11-10 — i7-10700: piezas de la mazmorra (D1) y personajes «mordidos»

- **Plan aprobado por el autor** para la lámina «Mazmorras ancestrales» (`Docs/Diseño de mazmorra`): rehacer sus piezas en Blender en la vista 3/4 (la lámina es isométrica), con trampas, vasijas rompibles, ambiente y salas nuevas. Estandartes con el sol → esteras tejidas; cofres europeos → estrado con ofrenda; cristales azules → jade (ficción, la memoria).
- **D1 hecho:** 9 piezas en `generar_olmeca.py` (16 sprites): brasero (3 cuadros de fuego), cristal de memoria (2), pinchos (bajados/levantados), placa de presión (arriba/hundida), cerca de estacas, vasija (entera/rota), cabeza de piedra, estrado de la ofrenda y esteras (jade/jaguar). Aprobadas por el autor ("se ven bien"). En `Sprites/Objetos` con sus pivotes; todavía no se colocan en ninguna escena.
- **Ojo con el flujo:** el `pivotes.json` de `Salida` estaba incompleto y `--reusar` rehízo todo el arte, con píxeles distintos (el render no es idéntico). Se exportaron a mano solo las piezas nuevas y se restauró `Salida`. No usar el export completo sin revisar el diff.
- **Personajes «mordidos» (reportado por el autor):** medido en el juego real, Unity dibuja todos los píxeles de cada sprite y la malla los cubre (`SpriteMeshCoverageTests`, nueva). La causa eran cajas de recorte justas en las láminas: la cola de la serpiente al atacar y varias poses de jefes. `recortar_enemigos.py` avisa ahora cuando una caja corta la figura y admite zonas a excluir (el adorno del marco junto a la serpiente). Corregidas siete; quedan cuatro poses de jefe que llegan hasta el marco del panel (cola del perro, sus llamas, las llamas moradas del Señor de los Muertos): hay que limpiarlas a mano en la lámina.
- **Vasija rota:** se cortaba por abajo; lienzo de 40×48.
- **Para D2:** pinchos y placa deben dibujarse como suelo, por debajo de los personajes, o le taparán los pies al jugador.
- **Estado:** EditMode 132/132, PlayMode 113/113. Subido.

---

## 2026-11-10 — i7-10700: revisión de animaciones y muertes

- **Pedido del autor:** buscar fallos de animación, personajes que no se vean completos y muertes que falten.
- **Análisis automático** de los sprites de guerreros, NPC, enemigos y jefes (figura que toca el borde del lienzo, pies fuera del pivote, restos sueltos). Encontró:
  - 9 cuadros de caminata (Xali y 8 NPC) que **flotaban 1 px**: al subir un pie, el otro no llegaba al suelo. `pasos_frente()` baja el cuadro hasta apoyarlo (con autocomprobación).
  - El **ataque del guardia** tocaba el borde superior (la lanza cortada) y se veía más grande que el reposo; el del guerrero, algo mayor. Factores 3,4 y 5,6 (antes 3,0 y 5,0).
  - Tras corregirlo: 0 problemas.
- **Muertes:** los jefes de la arena solo se desvanecían; ahora caen con dos cuadros generados desde el reposo (`Caida`, `Desplome`, en `recortar_jefes.py`) y `BossData.death`, también en la primera caída del Muro de Cráneos. El eco borrado desaparecía de golpe; ahora se borra en 0,6 s sin collider. Naira, Ameyali y Ocel siguen con su caída provisional (faltan sus hojas). El Espíritu del Jaguar y el Guardián no caen: se liberan.
- **Pruebas:** EditMode 127/127, PlayMode 113/113 (nueva `ErasedEcho_FadesOut_WhenDefeated`; la arena comprueba la caída del jefe).
- **Subido** junto con el Espíritu del Jaguar al doble de tamaño (entrada anterior).

---

## 2026-11-10 — i7-10700: el Jaguar de Piedra pasa a ser el Espíritu del Jaguar

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `54b5d40`.
- **Pedido del autor:** "quita también el Jaguar de Piedra y reemplázalo por un espíritu del jaguar".
- **Hecho:** `BuildStoneJaguar` usa los sets del Espíritu Jaguar de la lámina con el mismo `StoneJaguar` (barra, salto, zarpazo, rugido, `MP_OLM_04`, escombros de la bajada). Nombre en el HUD "Espíritu del Jaguar"; textos de misión, Museo, objeto y victoria ajustados (también en sus assets, que el generador no rehace). Ids y banderas `jaguar_piedra` sin cambios. Pisos regenerados; ninguna escena usa ya los sprites `Jaguar_Piedra_`.
- **Fallo real encontrado por las pruebas:** el golpe de tierra (`PlayerAbility`) buscaba el basalto agrietado en un búfer de 16 colliders de todos los layers; con los enemigos nuevos del piso 1 se llenaba y el basalto no se rompía. Ahora 64.
- **Estado:** EditMode 127/127, PlayMode 112/112.
- **Después, pedido del autor:** agrandarlo para que se vea como jefe. Sprite ×2 (68 px) y cápsula de 0,7 m. `DirectionalSpriteAnimator` voltea conservando el tamaño (antes fijaba la escala X en ±1 y lo habría estirado). Captura real revisada. **No se ha jugado en el Editor.**

---

## 2026-11-10 — i7-10700: fuera el jaguar de Blender; documentación (etapa 6)

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `74dfb9d`.
- **Pedido del autor:** "quita los jaguares que no son de pixel art que te pasé".
- **Hecho:** `CreateCorruptedSpirit` usa el arte del Espíritu Jaguar de la lámina (`EnemyCharacterAssets.Build("EspirituJaguar")`) en lugar de `JaguarCharacterAssets.Corrupted()`. Mismo `EnemyChaser`, salto avisado, banderas, fragmentos e incursiones: no se rompe `MP_OLM_03`. Ciudad, selva y pisos regenerados; ninguna escena referencia ya los sprites `JaguarCorrupto_`. `OlmecaJaguarTests` comprueba el arte nuevo.
- **No se tocó:** el **Jaguar de Piedra** (mini-jefe del Pantano, modelo de Blender), pendiente de decisión del autor. Los sprites del jaguar de Blender siguen en `Sprites/Personajes` (los genera el flujo de Blender) aunque nada los usa.
- **Etapa 6:** `Docs/Estado_y_Plan.md` (§3 y §5), `Docs/Enemigos/Olmeca.md`.
- **Estado:** EditMode 127/127, PlayMode 112/112. Subido.

---

## 2026-11-10 — i7-10700: arena de pruebas de los jefes (etapa 5)

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `4975a8e`.
- **Hecho:** `recortar_jefes.py` (24 sprites, las cuatro poses pequeñas de cada panel), `BossData`, `TrialBoss` (9 formas de habilidad, fases, resurrección), `BossTrial` y `BossTrialPad`, `BossCharacterAssets` y `TrialArenaGenerator`. Escena `Arena_Pruebas_Jefes` sobre el escenario del piso 3, con seis placas en la antesala. Detalle en `Docs/Enemigos/Jefes_Prueba.md`.
- **Cambio en `recortar_enemigos.py`:** el lienzo se calcula desde el centro del cuerpo (en figuras asimétricas se salía). Los sprites de los enemigos se regeneraron: cambia el ancho del lienzo, no la figura.
- **Pruebas:** `TrialBossTests` (12, PlayMode) y `BossAssetsTests` (EditMode). Capturas reales con una prueba temporal (ya borrada): avisos y jefes visibles; los jefes salen algo pequeños.
- **Estado:** EditMode 127/127, PlayMode 112/112. La arena se añade a Build Settings. Subido.
- **Pendiente del autor:** nombres definitivos y arco de cada jefe; aprobar el arte (varias poses especiales son casi solo el efecto). **No se ha jugado en el Editor.**

---

## 2026-11-10 — i7-10700: enemigos de la lámina, etapa 4

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `e5b34ec`.
- **Hecho:** los 7 enemigos colocados en `Olmeca_Selva` y en los pisos 1 a 3, regenerados. Comprueban el suelo con un rayo antes de colocarse. Nueva tabla `BOT_fauna`. Detalle en `Docs/Enemigos/Olmeca.md`.
- **Pruebas:** `OlmecaEnemigosSceneTests` (4): los de cada escena, sobre suelo firme, con botín y sin reaparecer en la mazmorra. Capturas reales con una prueba temporal (ya borrada): escala correcta frente al jugador; avisos visibles.
- **Rendimiento (i7, selva):** Bajo 236–261 FPS, Medio 298–316, Alto 266–270 (`Docs/Rendimiento.md`). Entre un 10 y un 18 % menos que el 29-10, pero desde entonces también llegaron la vista 3/4 y el terreno más rico, que no se midieron: no todo es de los enemigos. Falta medir en el i5 y en Android.
- **Para el autor:** en El Umbral los murciélagos quedan cerca de los espíritus; en la captura se juntaron 4 enemigos contra el jugador, quizá mucho para el tutorial. **No se ha jugado en el Editor.**
- **Estado:** EditMode 120/120, PlayMode 100/100. `EnemyLoot_GivesExperience_AndLessWhenRepeated` tomaba el primer enemigo con botín del piso 1, que ahora es un murciélago: busca al espíritu corrompido. Subido.
- **Siguiente:** etapa 5, la arena de pruebas de los jefes.

---

## 2026-11-10 — i7-10700: enemigos de la lámina, etapa 3

- **Rama / commit al empezar:** `feature/prototipo-olmeca` @ `4e12eb3`.
- **Hecho:** habilidades especiales en `EnemyData` (`EnemySpecial`, `attackDrains`, `swarm`) y `WildEnemy`: escupitajo que frena, coletazo, zarpazo doble, abanico, graznido y chillido que frenan, mordida que cura y enjambre. El freno es temporal: `WildEnemy` renueva `ISlowable.ApplySlow` cada cuadro mientras dura. `Projectile.Struck` avisa de qué proyectil acertó. `EnemyCharacterAssets` añade el aviso de la habilidad (círculo o anillo) y proyectiles a la serpiente. Tabla en `Docs/Enemigos/Olmeca.md`.
- **Los `ED_` se recrearon** para tomar los valores nuevos (ninguna escena los usaba aún).
- **Pruebas:** 7 nuevas en `WildEnemyTests` (16 en total) y comprobaciones de habilidad en `EnemyAssetsTests`.
- **Estado:** EditMode 120/120, PlayMode 96/96. Subido.
- **Siguiente:** etapa 4, colocar los enemigos en la selva, El Umbral, el Pantano y el piso 3.

---

## 2026-11-10 — i7-10700: enemigos de la lámina, etapas 1 y 2

- **Plan aprobado por el autor:** 7 enemigos comunes en la selva y la mazmorra; los 6 jefes, como ecos ficticios en una arena de pruebas aparte (son de culturas mexica, tolteca y maya; GDD §3).
- **Etapa 1 (arte):** `recortar_enemigos.py` recorta 46 sprites de la lámina a `Sprites/Enemigos` (importador: 48 PPU, pies a 2 px). El espíritu jaguar se separa por su brillo; la serpiente y la guacamaya necesitan tolerancias bajas, porque su verde y su turquesa se parecen al fondo. Página de revisión con los enemigos animados.
- **Etapa 2 (sistema común):** `EnemyData` (datos de diseño), `WildEnemy` (movimiento, ataque avisado, defensa, vuelo, muerte y reaparición) y `EnemyCharacterAssets` (sets, datos y montaje del enemigo para las escenas). Detalle en `Docs/Enemigos/Olmeca.md`.
- **Pruebas:** nuevas `EnemyAssetsTests` (EditMode) y `WildEnemyTests` (9, PlayMode). Fallaron 2 al principio: la defensa y el desvanecido se congelaban mientras el enemigo estaba aturdido; corregido.
- **No está en ninguna escena todavía** (etapa 4). `EnemyCharacterAssets.Create` aún no lo ha usado ningún generador.
- **Estado:** EditMode 120/120, PlayMode 89/89. Subido.
- **Siguiente:** etapa 3, habilidades especiales y grupos.

---

## 2026-11-10 — i7-10700: el jaguar corrupto ya no se ve de pie

- **Etapa 2 de `Continuar_Guerreros.md`:** el enemigo reportado era el jaguar corrupto de la selva (`Docs/Bus/BugCara.png`). Con la cámara 3/4 frontal, sus renders Norte y Sur (girados 65° en `personajes.py`) dejaban el cuerpo casi vertical.
- **Hecho (opción 1, elegida por el autor):** `JaguarCharacterAssets.ArtDirection` hace que Norte y Sur usen los renders diagonales (NE y SE). Sin renderizar en Blender ni regenerar escenas: `-executeMethod ...JaguarCharacterAssets.RebuildAll` rehace los `DSS_JaguarCorrupto_*` y `DSS_Jaguar_*`.
- **Estado:** EditMode 112/112, PlayMode 80/80. Subido.
- **Siguiente:** enemigos y jefes de `Docs/Enemigos y Gefes de mazmorra` (plan aprobado: etapas 1 arte y 2 sistema común).

---

## 2026-11-10 — i7-10700: tamaño de la pose de Daño

- **Rama / último commit al empezar:** `feature/prototipo-olmeca` @ `1c7ce77`.
- **Pedido del autor:** etapa 1 de `Continuar_Guerreros.md`, "con un mejor renderizado".
- **Causa:** la ficha dibuja el Daño en 3/4 y más grande; las escalas a mano (1,2–1,45) dejaban la cabeza mayor que en el reposo, sobre todo en Ameyali, Yaret y Naira.
- **Hecho:** `recortar_guerreros.py` escala el Daño a 46 px de alto en los siete (`ALTO_DANO`), en lugar de las escalas a mano. Naira quita las hojas de selva pegadas a la capa y la mano (`sin_hojas`, con autocomprobación): en sus demás poses solo se van motas de 1 px. Cambian el Daño y el primer cuadro de muerte de todos, las caídas provisionales de Naira, Ameyali y Ocel, y todas las poses de Naira.
- **Estado al cerrar:** EditMode 112/112 y PlayMode 80/80 tras cada cambio. Brazos aprobados por el autor en la página de revisión ("se ve un poco mejor") y subidos.
- **Siguiente:** ver el golpe final en el juego; etapa 2 (el enemigo reportado).
- **Después, pedido del autor:** "mejora la animación del caminado, que se vea más natural para todos".
  - **Cadencia:** `DirectionalSpriteAnimator.WalkPace` hace que los pasos sigan a la velocidad frente a `IPlanarMotion.CruiseSpeed` (nuevo, con 0 por defecto; lo dan `PlayerMovement`, `NpcPatrol` y `EnemyChaser`). Prueba `WalkPaceTests`.
  - **Frente y espalda:** `pasos_frente()` pasa a `recortar_npc.py` y lo usan los dos scripts: el pie sube según la estatura (3 px en los guerreros) y el cuerpo baja al apoyar. Los NPC pasan de 2 a 4 cuadros; `NpcCharacterAssets.RebuildAll` rehízo 18 `DSS_Npc_` sin tocar escenas.
  - Página de revisión con los sprites animados (privada): https://claude.ai/artifact/NbQSLCHuT66rVRDP2qG2h5
  - Commit `7fa9bc4`, subido.
- **Después, pedido del autor:** "que se muevan sus brazos" al caminar de frente. `pasos_frente()` toma como brazos los px de cada costado del torso (`brazos()`, desde el 32 % de la altura) y, al subir un pie, adelanta el brazo contrario (sube 2 px en los guerreros, deja la mano más corta) y atrasa el otro (baja 1). También se mueven el bastón, el escudo o la lanza de esa mano. La página de revisión compara con y sin brazos.

---

## 2026-11-06 — i7-10700: cierre de la sesión de guerreros

- **Rama / último commit:** `feature/prototipo-olmeca` @ `8a838dc`. Subido junto con `Continuar_Guerreros.md`.
- **Hecho:** prueba temporal en el juego real: las muertes de los siete se reproducen completas y a color en la selva. Encontró un salto de tamaño en la pose de Daño.
- **Siguiente:** `Docs/Personajes/Continuar_Guerreros.md`.

---

## 2026-11-06 — i7-10700: muertes con el espíritu completo

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `4a11457`. EditMode 107/107, PlayMode 80/80. Subido.
- **Pedido del autor:** mejor resultado en las muertes, que perdían detalles.
- **Hecho:** Balam y Xali separan el espíritu por su brillo sobre el fondo oscuro (`brillo_sobre_fondo`): se ven completos el jaguar y el ajolote, en 7 cuadros cada uno. Balam se aclara con `gamma`, que no quema las luces. Naira, Ameyali y Ocel caen en 3 cuadros: daño, inclinada y tumbada.

---

## 2026-11-06 — i7-10700: sprites sin cortar

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `e333b0a`. EditMode 107/107, PlayMode 80/80. Subido.
- **Pedido del autor:** "se ve cortada la animación".
- **Causa:** las cajas de recorte cortaban la figura (caras al correr, pies de Ocel, capas), 68 casos medidos. No afectaba solo a la muerte.
- **Hecho:** `recortar_guerreros.py` recorta con margen y conserva la figura que más ocupa la caja (`figura`, `MARGEN*`). Vistas en lienzo de 64 de ancho. Nuevo menú **Crónicas → Capturar muertes (pasarela)**.
- **Regresión corregida (reportada por el autor en `Docs/Bus`):** el margen metía la piel de la figura vecina en el color de fondo y borraba caras y parte del cuerpo. El fondo se mira ahora en el borde de la caja medida y los huecos solo se quitan desde 250 px de ficha. Muerte de Balam aclarada ×1,5: su hoja es muy oscura.
- **Aviso:** las capturas en lote pueden salir con los personajes en negro o en blanco, también los NPC, y cambia entre ejecuciones: es de la captura, no del juego; repetirla.

---

## 2026-11-06 — i7-10700: más tiempo antes de reaparecer

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `f3cf3d3`. EditMode 107/107, PlayMode 80/80. Subido.
- **Hecho:** a petición del autor, se reaparece a los 3 s en vez de 1,5 (`PlayerDamageResponse`, `DefeatReturn`), y la muerte dura 2,8 s. Pisos de la mazmorra regenerados: guardan `returnDelay`.

---

## 2026-11-06 — i7-10700: muerte con espíritu y golpe visible

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `4aa62ae`. EditMode 107/107, PlayMode 80/80. Subido.
- **Pedido del autor:** poner la animación de muerte y la de golpe, que no se apreciaban.
- **Causa:** la muerte solo tenía 3 cuadros, sin el espíritu; la pose de golpe duraba 0,2 s y el destello blanco tapaba 0,12.
- **Hecho:** muerte con espíritu desde sus hojas (Balam 5 cuadros; Xali, Itzel y Yaret 7), en 1,4 s antes de reaparecer, lienzo 128×96 y pivote propio. Golpe de 0,4 s. Naira, Ameyali y Ocel siguen con la muerte provisional.
- **Límite:** el espíritu de Balam y el de Xali salen incompletos; una hoja con transparencia lo resolvería.

---

## 2026-11-06 — i7-10700: Habilidad especial de Balam y caminar de frente

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `16b480b`. EditMode 99/99, PlayMode 80/80 (PlayMode antes de corregir el recorte de la pose). Subido.
- **Hecho:** `PlayerSpecial` (acción `Special`: F, RB): 25 de energía, 5 s de espera, 2,5 × ataque enfrente, quieto 0,7 s; solo Balam, con el panel de su ficha, donde el jaguar va fundido con el cuerpo. `pasos_frente` da 4 cuadros de frente y de espalda a los siete. El daño en área pasa a `PlayerAttack.StrikeArea`, que usa también el Golpe de tierra.
- **Distinto de lo propuesto:** el Golpe de tierra se queda en C y la habilidad nueva va en F, para no cambiar lo que ya enseñan los textos.
- **Pendiente:** cuadros reales de frente y de espalda; la habilidad de los otros seis; hoja con el jaguar aparte para animarlo.

---

## 2026-11-06 — i7-10700: Defensa y Victoria de Balam (piloto)

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `52d0f12`. EditMode 98/98, PlayMode 79/79. Subido.
- **Aprobado por el autor:** las propuestas de `Auditoria_Animaciones.md` §8.
- **Hecho:** `PlayerDefense` (acción `Defense`: Q, clic derecho, LB; quieto, sin atacar, mitad de daño) para los siete; poses y `Victoria` solo de Balam, recortadas de su ficha. La victoria dura 1,5 s antes de los resultados, y los demás pasan directo. Pista en el tutorial de El Umbral; pisos regenerados.
- **Pendiente:** táctil (decisión del autor: después del piloto); arte de la Habilidad especial y hoja nueva de Balam.

---

## 2026-11-06 — i7-10700: bloqueo de la entrada y selección al principio

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `f042af4`. EditMode 97/97, PlayMode 78/78. Subido.
- **Bloqueo arreglado (MP_OLM_03):** la selva está abierta desde el principio y un fragmento recogido antes de la misión no contaba, así que la entrada a la mazmorra no se abría nunca. Ahora el fragmento espera a que la misión esté activa, y la franja del primer paso cruza todo el mapa. Prueba: `SpiritsDefeatedBeforeTheQuest_KeepTheirFragmentsUntilItStarts`, que falló antes del arreglo. Las partidas que ya perdieron fragmentos siguen bloqueadas. `Olmeca_Selva` regenerada.
- **Selección al principio:** sin partida guardada se empieza eligiendo; también al pulsar Play en una escena de juego, en pausa. Elegir al empezar da la vida llena; cambiar a mitad de partida sigue sin curar.
- **Sigue igual:** al pulsar Play en `Olmeca_Selva` no hay misión, así que la entrada no se abre: para llegar a la mazmorra, empezar en Boot.
- **Pendiente:** el problema con un enemigo que reportó el autor; falta saber cuál.

---

## 2026-11-06 — i7-10700: pestaña Guardián en el menú de juego

- **Rama / último commit:** `feature/prototipo-olmeca`, sobre `fb83a44`. EditMode 97/97, PlayMode 75/75. Subido.
- **Documentación:** nueva `Docs/Personajes/Guerreros.md` (estado, cómo mejorarlos y mejoras previstas); GDD, glosario, plan y README alineados con que el Guardián es una persona; `Fichas/Jaguar.md` queda como histórica.
- **Decisión del autor:** se puede cambiar de guerrero a mitad de partida, desde cualquier escena (menú con I o el botón Bolsa).
- **Hecho:** pestaña Guardián en `GameMenuController`. Cambiar conserva nivel, objetos y Bendiciones y no cura ni recupera energía: `SetMaxHealth(keepCurrent)` y `SelectGuardian(fillEnergy: false)`, porque sumar la diferencia de vida máxima permitía curarse cambiando.
- **Límite conocido:** la marca de la ofrenda oculta es del guerrero con que se abrió; al cambiar, la nueva no se recibe y la anterior no da bonificación. Si se pulsa Play en una escena, Boot se carga después; hasta guardar en un santuario, el cambio se pierde al cerrar.

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
