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
