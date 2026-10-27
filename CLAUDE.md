# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

**Crónicas de México**: Action RPG de exploración y mazmorras, un jugador, en estilo 2.5D (sprites pixel art sobre escenarios 3D estilizados) con identidad basada en las culturas de México. El primer capítulo es olmeca (La Venta). Unity **6000.0.85f1 (LTS)**, C#, URP 17.0.4. Responder y documentar en español.

**El documento de diseño de referencia es `Docs/Diseno.md` (GDD).** Leerlo antes de proponer cambios y contrastar cada requisito con el repositorio. Plataforma objetivo: **Android en horizontal** (arquitectura preparada para iOS); el PC se usa para desarrollo y pruebas con teclado y ratón.

Se desarrolla en dos PC Windows con gráficos integrados: **i5-6500 / Intel HD 530 / 12 GB** (referencia de rendimiento) e **i7-10700 / Intel UHD 630 / 16 GB** (desarrollo principal, horneado de luces y builds de Android). Ambas usan exactamente Unity 6000.0.85f1 y se sincronizan solo por Git. Requisitos, rutina diaria y reglas de conflicto: **`Docs/DesarrolloMultiPC.md`**. Antes de que el usuario cambie de PC, recordarle que haga commit y push (o pedir autorización para hacerlo).

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
- **Flujo de arranque:** la escena `Scenes/Boot/Boot.unity` (índice 0 en Build Settings) contiene `GameBootstrapper`, que registra los servicios en `ServiceLocator` y carga `initialScene` (`Olmeca_Prototipo`) de forma **aditiva**. Boot permanece cargada toda la partida. `BootSceneLoader` (`RuntimeInitializeOnLoadMethod.BeforeSceneLoad`) añade Boot si se pulsa Play desde otra escena; en ese caso Boot se carga **después** de esa escena, así que los componentes de juego no deben depender de servicios en `Awake`/`Start`. Toda escena cargada por nombre debe estar en Build Settings.
- **Comunicación desacoplada:** canales de eventos como ScriptableObject (`Core/Events`: `EventChannel<T>`, `VoidEventChannel`, `StringEventChannel`, `HealthEventChannel`; assets en `Data/Events`). Los oyentes se suscriben en `OnEnable` y se desuscriben en `OnDisable`. Los datos de diseño van en ScriptableObjects dentro de `Data`.
- **La recarga de dominio está desactivada** (Enter Play Mode Options = Disable Domain Reload, para iterar rápido en el i5). Todo estado estático debe limpiarse con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, como hace `ServiceLocator`.
- **Personajes:** raíz con `CharacterController` + hijo `Sprite` (`SpriteRenderer` con `HD2D_SpriteLit`, `SpriteBillboard`). `DirectionalSpriteAnimator` lee cualquier `IPlanarMotion` (`PlayerMovement`, `NpcPatrol`, `EnemyChaser`) y voltea con la escala X, no con `flipX` (las sombras de URP no reciben `flipX`).
- **Vida (regla del GDD §9):** la vida no se regenera. Estando vivo solo sube con `Health.Heal(cantidad, HealSource)`; `ReviveFull()` es solo para reaparecer tras morir. Hay una prueba que lo vigila.
- **Layers:** 6 `Interactuable` (triggers que encuentra `PlayerInteractor`), 7 `Enemigo` (lo que golpea `PlayerAttack`). El jugador lleva la etiqueta `Player`.
- **UI:** UI Toolkit (`Art/UI/Hud`: `Hud.uxml`, `Hud.uss`, `HudTheme.tss`; `Settings/UI/PanelSettings_Hud`). TextMesh Pro no tiene importados sus recursos.

## Escena olmeca (congelada)

`Scenes/Regions/Olmeca/Olmeca_Prototipo.unity` la creó `OlmecaPrototypeGenerator` (menú **Crónicas → Generar prototipo olmeca**). **Desde la Fase 1 está congelada**: se edita a mano o con prefabs y el generador se niega a sobrescribirla. Regenerar reemplaza la escena y los prefabs de personajes, así que solo se hace borrando o renombrando antes la escena, a propósito.

## Rendimiento y calidad

- Tres niveles, **Bajo / Medio / Alto**, cada uno con su URP Asset en `Assets/_Project/Settings/Rendering/`. Valores y registro de FPS en `Docs/Rendimiento.md`.
- Editor, Windows y Android arrancan en **Bajo**. En Bajo no hay HDR ni Depth Texture y `CameraQualityApplier` apaga el post-procesado de la cámara. Las funciones nuevas deben verse bien y rendir en Bajo en el i5-6500 y en Android de gama media (objetivo: 30 FPS estables en dispositivo real).
- Estilo: Pixel Art detallado en 2.5D con cámara isométrica/semisométrica. Los perfiles cambian iluminación y post-procesado, nunca la resolución de los sprites (Point, 32 PPU). Presupuestos, Sprite Atlas, iluminación, escenas por región, efectos a evitar y ajustes de Android/iOS: `Docs/Rendimiento.md`.
- Iluminación horneada (solo en el i7) o light probes con pocas luces en tiempo real, Sprite Atlas por grupo/región y materiales compatibles con el SRP Batcher. En `Update`, evitar asignaciones de memoria y llamadas a `GetComponent`/`Find`; usar búferes fijos (`OverlapSphereNonAlloc`) y pooling para proyectiles y VFX.

## Convenciones de Unity y Git

- Asset Serialization: Force Text. Meta files visibles. Al mover o renombrar assets fuera del Editor, mover también su `.meta` para conservar el GUID; si no, se rompen las referencias.
- Las carpetas vacías llevan `.gitkeep`: Git no versiona carpetas vacías y Unity borraría su `.meta` en la otra PC.
- Los binarios (imágenes, audio, modelos, fuentes) van por Git LFS (`.gitattributes`). El YAML de Unity se guarda con LF y se fusiona con UnityYAMLMerge.
- Ramas: `main` (estable), `develop` (integración), `feature/<nombre>`. Evitar editar la misma escena en ambas PC a la vez; preferir prefabs y escenas aditivas.

## Plan de desarrollo (Diseno.md §17 y §18)

Fase 0 auditoría (hecha, `Docs/Auditoria_Fase0.md`) → **Fase 1 estabilización** → 2 arte y cámara → 3 Guardián (movimiento, animaciones, combate, controles táctiles) → 4 mundo/NPC → 5 mazmorra → 6 jefe → 7 persistencia/UI → 8 optimización en Android real. Objetivo intermedio: **vertical slice olmeca** (un Guardián, hub con cuatro NPC, mazmorra de tres pisos, mini-jefe, jefe, una Bendición, guardado). Las referencias culturales van en `Docs/` y no deben mezclarse elementos olmecas con mexicas o mayas.
