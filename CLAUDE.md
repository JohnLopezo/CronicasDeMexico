# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

**Crónicas de México**: Action RPG de exploración y mazmorras, un jugador, en estilo 2.5D (sprites pixel art sobre escenarios 3D estilizados) con identidad basada en las culturas de México. El primer capítulo es olmeca (La Venta). Unity **6000.0.85f1 (LTS)**, C#, URP 17.0.4. Responder y documentar en español.

**El documento de diseño de referencia es `Docs/Diseno.md` (GDD).** Leerlo antes de proponer cambios y contrastar cada requisito con el repositorio. **La referencia narrativa es `Docs/Historia_Mundo.md`** (biblia del autor; prevalece sobre el GDD en historia, nombres, personajes y capítulos), con sus derivados en `Docs/Historia/`, `Docs/Personajes/`, `Docs/Culturas/`, `Docs/Enemigos/` y `Docs/Mazmorras/`. Plataforma objetivo: **Android en horizontal** (arquitectura preparada para iOS); el PC se usa para desarrollo y pruebas con teclado y ratón.

Se desarrolla en dos PC Windows con gráficos integrados: **i5-6500 / Intel HD 530 / 12 GB** (referencia de rendimiento) e **i7-10700 / Intel UHD 630 / 16 GB** (desarrollo principal, horneado de luces y builds de Android). Ambas usan exactamente Unity 6000.0.85f1 y se sincronizan solo por Git. Requisitos, rutina diaria y reglas de conflicto: **`Docs/DesarrolloMultiPC.md`**. Antes de que el usuario cambie de PC, recordarle que haga commit y push (o pedir autorización para hacerlo). **El estado general y el plan están en `Docs/Estado_y_Plan.md`; el de cada sesión (fecha, PC, rama, qué se hizo y qué sigue), en `Docs/Bitacora.md`:** leer la última entrada al empezar y añadir una al terminar, porque la memoria de Claude Code no viaja entre PC.

## Forma de trabajo

- Proponer primero (análisis, archivos y plan por etapas) y **esperar autorización** antes de modificar. No hacer commits ni push sin aprobación.
- Cambios pequeños; conservar escenas, prefabs y ajustes que funcionan; no añadir paquetes sin necesidad.
- Al terminar: listar archivos modificados, pruebas realmente ejecutadas y lo que queda pendiente de probar en el Editor o en un dispositivo. No declarar éxito sin pruebas.
- Separar en el contenido cultural lo documentado, lo interpretado y lo ficticio; no inventar dioses históricos (Diseno.md §3).

## Comandos

Unity no está en el PATH: `U="/c/Program Files/Unity/Hub/Editor/6000.0.85f1/Editor/Unity.exe"`. **El modo batch falla si el proyecto está abierto en el Editor**, y editar `ProjectSettings/*.asset` con el Editor abierto no sirve: el Editor no los recarga y puede sobrescribirlos al cerrarse.

```bash
# Pruebas (EditMode o PlayMode). No combinar -runTests con -quit.
"$U" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults <ruta>/editmode.xml -logFile <ruta>/editmode.log
# Una sola prueba o clase: añadir  -testFilter "CronicasDeMexico.Tests.ServiceLocatorTests"

# Compilar / reimportar sin abrir el Editor (revisar errores en el log)
"$U" -batchmode -quit -projectPath . -logFile <ruta>/compile.log

# Build de Windows del juego completo (arranca en Boot, con el menú de inicio); queda en Builds/Windows/Juego/
"$U" -batchmode -quit -projectPath . -executeMethod CronicasDeMexico.Editor.Build.WindowsGameBuild.BuildDevelopment -logFile <ruta>/windows.log

# Build Android de desarrollo (cambia el target del proyecto a Android; volver con -buildTarget Win64)
"$U" -batchmode -quit -projectPath . -buildTarget Android -executeMethod CronicasDeMexico.Editor.Build.AndroidBuild.BuildDevelopment -logFile <ruta>/android.log
```

Configuración de Git por PC (LFS + UnityYAMLMerge, guardada en `.git/config`): `Tools/setup-git.ps1`.

## Arquitectura

- **Todo el contenido propio vive en `Assets/_Project/`.** Los assets de terceros van en `Assets/ThirdParty/` y `Assets/Plugins/`. `Docs/` y `Tools/` están fuera de `Assets`.
- **Assembly Definitions** en `Assets/_Project/Scripts/` (dependencias en un solo sentido):
  `Core` (sin dependencias) ← `Data` ← `World` (URP), `Gameplay` (Input System, Cinemachine), `UI` (UI Toolkit, Input System) ← `Editor` (solo editor).
  Pruebas en `Assets/_Project/Tests/{EditMode,PlayMode}`.
- **Organización por funciones** (Diseno.md §15) dentro de los assemblies existentes, sin reorganizar en masa: `Gameplay/{Movement,Player,Combat,Enemies,Npc,Interaction}`; los sistemas nuevos (`Dungeon`, `Blessings`, `SaveSystem`, `Dialogue`…) se crean como carpetas en el assembly que corresponda.
- **Flujo de arranque:** la escena `Scenes/Boot/Boot.unity` (índice 0 en Build Settings) contiene `GameBootstrapper`, que registra los servicios en `ServiceLocator`, y el **menú de inicio** (`UI/Hud/StartMenuController`, en `Menu_Inicio`).
  - Si Boot arranca sola, `GameBootstrapper.StartMenuRequested` muestra el menú:
    - **Continuar** (`ContinueSavedGame`) aparece en el santuario de Memoria de la partida guardada.
    - **Nueva partida** (`StartNewGame`) borra la partida, tras confirmar si existe, y carga `initialScene` (`Olmeca_Ciudad`) de forma **aditiva**.
    - También tiene **Opciones** (calidad) y **Salir**.
  - El menú se crea con **Crónicas → Crear menú de inicio en Boot** (`StartMenuSetup`) y los **controles táctiles** con **Crónicas → Crear controles táctiles en Boot** (`TouchControlsSetup`, prefab `Prefabs/UI/Controles_Tactiles`); es lo único de Boot que se genera.
  - **Controles táctiles** (`UI/Touch/TouchControls`): joystick y botones de uGUI (`OnScreenStick`, `OnScreenButton`) que hacen de mando virtual, con los bindings de mando que ya tiene el juego. Solo con pantalla táctil (o `forceVisible`) y ocultos con el juego en pausa; respetan `Screen.safeArea`. Atacar ya no responde a tocar cualquier parte de la pantalla. **No llevan EventSystem propio**: los toques llegan por el de cada escena, así que toda escena de juego debe traer uno. Un EventSystem de `CreateEventSystem` guardado en un prefab pierde sus acciones de UI y no recibe toques; pasó en el S24+. Con pantalla táctil (`Touchscreen.current`) los textos no nombran teclas (`TutorialHint.TouchText`, aviso de `PlayerInteractor`). Distribución, funcionamiento y pruebas en el teléfono: `Docs/Controles_Tactiles.md`.
  - Su cámara de fondo y su EventSystem son hijos inactivos que solo se activan con el menú a la vista.
  - Boot permanece cargada toda la partida. `BootSceneLoader` (`RuntimeInitializeOnLoadMethod.BeforeSceneLoad`) añade Boot si se pulsa Play desde otra escena; en ese caso Boot se carga **después** de esa escena, así que los componentes de juego no deben depender de servicios en `Awake`/`Start`. Toda escena cargada por nombre debe estar en Build Settings.
- **Viaje entre escenas de región** (`Core/Travel/SceneTravel`, registrado por `GameBootstrapper`; `Gameplay/Travel`): un `ScenePortal` (trigger) pide el viaje; `SceneTravel` funde a negro, descarga la escena del portal, carga la de destino de forma aditiva y la activa. El `SpawnPoint` con el id pedido coloca al jugador en su `Awake` (antes de los `Start`, así la cámara empieza ya mirándolo) y le aplica la vida que traía (`Health.CarryOver`, que nunca la sube). Al pulsar Play en una escena no hay viaje y el `SpawnPoint` no hace nada.
- **Comunicación desacoplada:** canales de eventos como ScriptableObject (`Core/Events`: `EventChannel<T>`, `VoidEventChannel`, `StringEventChannel`, `HealthEventChannel`; assets en `Data/Events`). Los oyentes se suscriben en `OnEnable` y se desuscriben en `OnDisable`. Los datos de diseño van en ScriptableObjects dentro de `Data`.
- **Narrativa y guardado** (`Core/Narrative`, `Core/Save`, `Data/Narrative`): `GameBootstrapper` registra `StoryState` (banderas y avisos), `QuestService`, `PlayerCheckpoint` (escena, punto de aparición y vida del último santuario de Memoria) y `SaveService`. Los avisos tienen la forma de `StoryEvents` (`hablar:id`, `examinar:id`, `vencer:id`, `llegar:id`) y los emiten `DialogueInteractable`, `InscriptionInteractable`, `ReportOnDefeat` y `ReportOnEnter`. Diálogos (`DialogueData`, ramas por banderas), personajes (`NpcData`) y misiones (`QuestData`) son ScriptableObjects; una misión debe estar en el catálogo `Resources/Narrativa/CatalogoMisiones` para recuperarse al cargar. Las banderas `mision.ID.activa` y `mision.ID.completada` las mantiene `QuestService`; las recompensas se dan una sola vez, al completarse y nunca al cargar. La partida es JSON con versión y copia de respaldo en `persistentDataPath` (no PlayerPrefs); al cambiar `SaveData`, subir `SaveService.CurrentVersion` y migrar. Guarda el santuario de Memoria (`MemoryShrine`) y carga Boot al arrancar. Las pruebas que guardan registran servicios propios con una carpeta temporal, para no tocar la partida real.
- **Mazmorra** (`Gameplay/Dungeon`, M2): `MemoryShrine` (punto de control y guardado), `LifeShrine` (cura una vez por incursión), `DefeatReturn` (en cada piso: al caer con punto de control, viaja al santuario y recarga el piso) e `IncursionReset` (en la selva: borra las banderas `incursion.*`). Ids de banderas en `DungeonFlags`.
- **Combate de la mazmorra** (M3):
  - `PlayerMovement` incluye la esquiva (acción `Player/Dodge`, la antigua Jump; se busca en el mapa de la acción de movimiento, así que no hace falta tocar el prefab). Dos cargas encadenadas y pulsación guardada.
  - `PlayerAttack.Attack()` es el golpe con entrada: combo de 3 con remate, avance, hitstop (escala 0,05, nunca 0: 0 es pausa), sacudida (`Core/Feedback/ScreenShake`, la aplica `IsometricCameraFollow`) y apuntado asistido (`AimAssist`, también en `PlayerSpecial`). `Strike()` es el golpe crudo, para pruebas.
  - Los ataques avisados usan `TelegraphMarker`, una forma en el suelo (círculo, anillo o franja) que se desprende de su dueño. Los proyectiles (`Projectile`) los reutiliza quien los lanza.
  - Enemigos: `ErasedEcho` (dispara y salta entre puntos), `StoneJaguar` (mini-jefe con varias poses; cambia `DirectionalSpriteAnimator.SpriteSet`; barra en el HUD por `EVT_Jefe_Nombre` y `EVT_Jefe_Vida`) y `StoneGuardian` (jefe, M4).
  - El Guardián tiene tres fases por vida y ataques en corrutinas; `ForceAttack` permite probar cada uno. El agua de su fase 2 es un objeto de la escena que él activa.
  - `SealedPassage` abre un paso al cerrar su mensaje (bandera).
  - `SlowZone` frena a los `ISlowable` con `OnTriggerStay`, no con salidas: Unity no llama a `OnTriggerExit` si el personaje se teletransporta.
  - `TutorialHint` muestra una pista una sola vez (bandera `tutorial.<id>`).
  - En las pruebas, la invulnerabilidad del jugador tras un golpe dura 1 s.
  - **Enemigos de la lámina** (`Docs/Enemigos/Olmeca.md`): `WildEnemy` guiado por `EnemyData` (`Data/Enemies`); `EnemyCharacterAssets` arma sus sets y los monta (`Create`). Sprites de `recortar_enemigos.py` en `Sprites/Enemigos`. `EnemyChaser` sigue siendo el del jaguar corrupto y las incursiones.
  - **Jefes de prueba** (`Docs/Enemigos/Jefes_Prueba.md`): ecos ficticios en la escena aparte `Arena_Pruebas_Jefes` (`TrialArenaGenerator`), con `TrialBoss` guiado por `BossData`, `BossTrial` y `BossTrialPad`. No se enlazan con el juego.
- **Cadencia de la caminata:** `DirectionalSpriteAnimator` avanza los cuadros según `velocidad / IPlanarMotion.CruiseSpeed` (`WalkPace`, entre ×0,5 y ×1,5); quien no da `CruiseSpeed` (jefes, ecos) camina a ritmo fijo.
- **La recarga de dominio está desactivada** (Enter Play Mode Options = Disable Domain Reload, para iterar rápido en el i5). Todo estado estático debe limpiarse con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, como hace `ServiceLocator`.
- **Personajes:** raíz con `CharacterController` + hijo `Sprite` (`SpriteRenderer` con `HD2D_SpriteLit`, `SpriteBillboard`). `DirectionalSpriteAnimator` lee cualquier `IPlanarMotion` (`PlayerMovement`, `NpcPatrol`, `EnemyChaser`) y voltea con la escala X, no con `flipX` (las sombras de URP no reciben `flipX`).
- **NPC con arte final:** `Tools/ArtPipeline/recortar_npc.py` recorta las fichas de `Docs/NPC` a `Sprites/Npc` (carpeta aparte: el flujo de Blender borra de las suyas lo que no genera) y `NpcCharacterAssets` arma sus `DirectionalSpriteSet`. `NpcPatrol` va en línea recta y no esquiva: toda ruta nueva debe pasar `Settlers_WalkTheirRoutes_WithWalkingAnimation`.
- **Guerreros jugables (decisión del autor: el protagonista es una persona; estado y mejoras en `Docs/Personajes/Guerreros.md`; por dónde seguir en `Continuar_Guerreros.md`):** `Tools/ArtPipeline/recortar_guerreros.py` recorta las fichas de `Docs/Guerreros` a `Sprites/Guerreros`; `GuerreroCharacterAssets` arma sus `DSS_Guerrero_*` y `GUA_`, y su catálogo; `PlayerPrefabUpgrade` deja a Balam en el prefab del jugador (`-executeMethod ...PlayerPrefabUpgrade.Ensure` rehace todo sin regenerar escenas). Se eligen en Nueva partida (`StartMenuController`) y se cambian en la pestaña Guardián del menú (`GameMenuController`; cambiar no cura ni recupera energía); `Progression.Profile` es el elegido, se guarda en `guardianId` y `PlayerStats` aplica sus sprites al aparecer el jugador en cada escena.
- **El guardia no es un Guardián:** los guardias son habitantes que defienden la aldea (`NpcDefender`, id `guardia`; también lo lleva el joven guerrero en la selva). No tienen vida, para no romper la historia. Los Guardianes son el protagonista y los jefes (`GuardianData`, `StoneGuardian`).
- **Vida (regla del GDD §9):** la vida no se regenera. Estando vivo solo sube con `Health.Heal(cantidad, HealSource)`; `ReviveFull()` es solo para reaparecer tras morir. Hay una prueba que lo vigila.
- **Layers:** 6 `Interactuable` (triggers que encuentra `PlayerInteractor`), 7 `Enemigo` (lo que golpea `PlayerAttack`). El jugador lleva la etiqueta `Player`.
- **UI:** UI Toolkit (`Art/UI/Hud`: `Hud.uxml`, `Hud.uss`, `HudTheme.tss`; `Settings/UI/PanelSettings_Hud`). TextMesh Pro no tiene importados sus recursos.

## Escenas de pixel art generadas

`Olmeca_Ciudad`, `Olmeca_Selva` y los pisos `Olmeca_Mazmorra_P1` a `P3` se generan desde `Data/World/Olmeca/<Región>.json`, que exporta el flujo de Blender (`Tools/ArtPipeline/generar_olmeca.py`), con `OlmecaCityGenerator`, `OlmecaJungleGenerator` y `OlmecaDungeonGenerator` (menú **Crónicas → Generar ciudad / selva / mazmorra olmeca**). Si cambia el JSON de la selva o un generador que usa, hay que regenerar también esa escena. Las piezas comunes están en `PixelArtSceneBuilder`. **No se editan a mano: se regeneran.** **Ojo al exportar desde Blender:** el render no sale idéntico entre ejecuciones y `Tools/ArtPipeline/Salida` no coincide con lo exportado, así que la exportación completa cambiaría cientos de sprites. Para un piso que cambia, `--solo-piso N` exporta solo su JSON y sus trozos de suelo (antes, `--reusar --solo-mazmorra` renderiza las losas actuales; las de `Salida` eran viejas). Los objetos que dependen de la historia (espíritus, fragmentos, el guerrero) se muestran u ocultan con `ActiveWhenFlags`, que va en el padre del objeto.

## Escena olmeca (congelada)

`Scenes/Regions/Olmeca/Olmeca_Prototipo.unity` la creó `OlmecaPrototypeGenerator` (menú **Crónicas → Generar prototipo olmeca**). **Desde la Fase 1 está congelada**: se edita a mano o con prefabs y el generador se niega a sobrescribirla. Regenerar reemplaza la escena y los prefabs de personajes, así que solo se hace borrando o renombrando antes la escena, a propósito.

## Rendimiento y calidad

- Tres niveles, **Bajo / Medio / Alto**, cada uno con su URP Asset en `Assets/_Project/Settings/Rendering/`. Valores y registro de FPS en `Docs/Rendimiento.md`.
- Editor, Windows y Android arrancan en **Bajo**. En Bajo no hay HDR ni Depth Texture y `CameraQualityApplier` apaga el post-procesado de la cámara. Las funciones nuevas deben verse bien y rendir en Bajo en el i5-6500 y en Android de gama media (objetivo: 30 FPS estables en dispositivo real).
- Estilo: Pixel Art detallado en 2.5D con cámara isométrica/semisométrica. Los perfiles cambian iluminación y post-procesado, nunca la resolución de los sprites (Point; 48 PPU el arte de Blender y los guerreros, 32 PPU las fichas de NPC y los efectos). Presupuestos, Sprite Atlas, iluminación, escenas por región, efectos a evitar y ajustes de Android/iOS: `Docs/Rendimiento.md`.
- Iluminación horneada (solo en el i7) o light probes con pocas luces en tiempo real, Sprite Atlas por grupo/región y materiales compatibles con el SRP Batcher. En `Update`, evitar asignaciones de memoria y llamadas a `GetComponent`/`Find`; usar búferes fijos (`OverlapSphereNonAlloc`) y pooling para proyectiles y VFX.

## Convenciones de Unity y Git

- Asset Serialization: Force Text. Meta files visibles. Al mover o renombrar assets fuera del Editor, mover también su `.meta` para conservar el GUID; si no, se rompen las referencias.
- Las carpetas vacías llevan `.gitkeep`: Git no versiona carpetas vacías y Unity borraría su `.meta` en la otra PC.
- Los binarios (imágenes, audio, modelos, fuentes) van por Git LFS (`.gitattributes`). El YAML de Unity se guarda con LF y se fusiona con UnityYAMLMerge.
- Ramas: `main` (estable), `develop` (integración), `feature/<nombre>`. Evitar editar la misma escena en ambas PC a la vez; preferir prefabs y escenas aditivas.

## Plan de desarrollo (Diseno.md §17 y §18)

Fase 0 auditoría (hecha, `Docs/Auditoria_Fase0.md`) → **Fase 1 estabilización** → 2 arte y cámara → 3 Guardián (movimiento, animaciones, combate, controles táctiles) → 4 mundo/NPC → 5 mazmorra → 6 jefe → 7 persistencia/UI → 8 optimización en Android real. Objetivo intermedio: **vertical slice olmeca** (un Guardián, hub con cuatro NPC, mazmorra de tres pisos, mini-jefe, jefe, una Bendición, guardado). Las referencias culturales van en `Docs/` y no deben mezclarse elementos olmecas con mexicas o mayas.
