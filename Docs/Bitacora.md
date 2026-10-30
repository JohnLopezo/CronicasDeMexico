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
