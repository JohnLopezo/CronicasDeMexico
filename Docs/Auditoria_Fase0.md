# Auditoría Fase 0 — estado del proyecto frente a Diseno.md

**Fecha:** 2026-10-27 · **Rama:** `feature/prototipo-olmeca` · **Último commit auditado:** `dc50c61`
Auditoría sin cambios (Diseno.md §18). Las correcciones de la Fase 1 se marcan al final.

## 1. Estado verificado

| Área | Estado | Rutas / evidencia |
|---|---|---|
| Git | Ramas `main`, `develop`, `feature/prototipo-olmeca`, `respaldo-*`; remoto GitHub. LFS activo para binarios. | `.gitattributes`, `Tools/setup-git.ps1` |
| Motor | Unity 6000.0.85f1, URP 17.0.4, Input System 1.20, Cinemachine 3.1.7. ProBuilder y AI Navigation instalados sin uso. | `Packages/manifest.json` |
| Escenas | `Boot` (arranque; carga `Olmeca_Prototipo`), `Olmeca_Prototipo` (generada), `Sandbox_HD2D` (prueba). | `Assets/_Project/Scenes/` |
| Android | Módulo con SDK, NDK y OpenJDK. IL2CPP, ARM64, Vulkan + GLES3, API mínima 23. Ningún APK compilado hasta la Fase 1. | `ProjectSettings/ProjectSettings.asset` |
| Calidad | Bajo / Medio / Alto. Windows en Bajo; Android e iOS en Medio. | `ProjectSettings/QualitySettings.asset`, `Docs/Rendimiento.md` |
| Pruebas | 28 EditMode y 18 PlayMode pasando en modo batch. Sin pruebas en dispositivo; FPS sin medir. | `Assets/_Project/Tests/` |

## 2. Sistemas existentes reutilizables

| Sistema | Archivos (`Assets/_Project/Scripts/`) | GDD |
|---|---|---|
| Arranque y servicios | `Core/Boot/GameBootstrapper`, `BootSceneLoader`, `Core/Services/ServiceLocator`, `QualityService` | §15 |
| Eventos | `Core/Events/EventChannel`, `VoidEventChannel`, `StringEventChannel`, `HealthEventChannel` | §15 |
| Movimiento 8 direcciones | `Gameplay/Player/PlayerMovement`, `Gameplay/Movement/PlanarMovement`, `IPlanarMotion`, `DirectionQuantizer` | §8 |
| Sprites 2.5D | `Art/Shaders/HD2D_SpriteLit.shader`, `World/Rendering/SpriteBillboard`, `Gameplay/Player/DirectionalSpriteAnimator`, `Data/Characters/DirectionalSpriteSet` | §5 |
| Cámara | Cinemachine con inclinación fija de 30°, FOV 30, seguimiento suave (creada por el generador) | §5 |
| Interacción y diálogo | `Gameplay/Interaction/IInteractable` (+ `IConversation`), `PlayerInteractor`, `InscriptionInteractable`, `DialogueInteractable` | §12 |
| Combate básico | `Gameplay/Combat/Health`, `HealSource`, `PlayerAttack`, `DamageFlash`, `Gameplay/Player/PlayerDamageResponse` | §8, §9 |
| Enemigo y NPC | `Gameplay/Enemies/EnemyChaser`, `Gameplay/Npc/NpcPatrol` | §10, §12 |
| HUD y opciones | `UI/Hud/HudController`, `QualityOptionsPanel` (UI Toolkit) | §13 |
| Calidad por cámara | `World/Rendering/CameraQualityApplier` | §16 |
| Herramientas | `Editor/SceneGeneration/OlmecaPrototypeGenerator`, `PlaceholderSpriteFactory`, `Editor/Build/AndroidBuild` | — |

## 3. Conflictos con el GDD

| # | Conflicto | GDD | Prioridad | Estado |
|---|---|---|---|---|
| 1 | El protagonista es una figura humana provisional; el GDD pide un Guardián animal (Jaguar recomendado). | §4 | Alta (Fase 3) | Pendiente |
| 2 | El enemigo "espíritu jaguar" choca con el Guardián Jaguar y con el mini-jefe "Jaguar de Piedra"; conviene tematizarlo como criatura de El Olvido. | §4, §7.1, §10 | Media | Pendiente de decisión |
| 3 | Al morir se reaparece en el inicio con toda la vida; el GDD pide volver al último checkpoint con penalización moderada configurable. | §9 | Alta (Fases 5 y 7) | Pendiente de decisión |
| 4 | `Health.Restore()` curaba sin origen. | §9 | Alta | **Resuelto en Fase 1** (`Heal` + `HealSource`, `ReviveFull`) |
| 5 | Los enemigos reaparecen a los 8 s; no encaja con salas y pisos. | §7 | Media (Fase 5) | Pendiente |
| 6 | `EnemyChaser` daña por contacto, sin Telegraph → Attack → Recover. | §10 | Alta (Fase 3/5) | Pendiente |
| 7 | `Olmeca_Prototipo` mezcla hub y enemigos; el hub debe ser zona segura. | §6 | Media (Fase 4) | Pendiente |
| 8 | Android permitía orientación vertical. | §13 | Alta | **Resuelto en Fase 1** |
| 9 | Solo hay controles de PC (Esc para opciones, aviso con la tecla E, sin controles táctiles). | §8, §13 | Alta (Fase 3) | Pendiente |
| 10 | `companyName` y `applicationIdentifier` por defecto (plantilla de Unity). | — | Alta | **Resuelto en Fase 1** (`com.johnlopezo.cronicasdemexico`) |

## 4. Faltante (por sección del GDD)

- §4 Datos de Guardianes, selección y nombre del jugador.
- §8 Combo, ataque pesado, esquive y esquive perfecto, tres habilidades, definitiva, energía; elementos y estados; hitbox/hurtbox explícitos.
- §10 Arquetipos y máquina de estados de enemigos; mini-jefes y jefe con fases.
- §7 Salas, pisos, transiciones y estado de incursión.
- §9 Santuarios (Vida, Memoria, Espiritual, Sabiduría, Artesano) y usos por incursión; **guardado versionado con respaldo** (no existe).
- §11 Bendiciones, XP y evolución espiritual.
- §12 IDs persistentes de NPC, árboles de diálogo con condiciones, misiones.
- §13 Menú principal, pausa, selección de Guardián, inventario, mapa, Códice, controles táctiles.
- §14 AudioManager y mezcladores (carpetas de audio vacías).
- §5 Sprite Atlas, transparencia por oclusión, transiciones entre escenas y eras.

## 5. Requisitos

- **Confirmados:** la vida no se regenera; Android horizontal y PC para pruebas; partidas sin PlayerPrefs; 30 FPS estables en Android de gama media; vertical slice antes de producir capítulos; primeras mazmorras hechas a mano; separar documentado, interpretado y ficticio.
- **Propuestas ajustables:** lista de Guardianes, pisos por capítulo, ruta de capítulos, estructura de carpetas de §15, Bendiciones olmecas.
- **Pendientes:** Guardián del slice; ángulo y resolución definitivos; penalización por muerte y vida al reaparecer; qué persiste en una incursión; balance.

**Estructura de carpetas (§15):** se mantiene la de assemblies (`Core`, `Data`, `Gameplay`, `World`, `UI`, `Editor`) y los sistemas nuevos se añaden como carpetas dentro de ella, sin reorganización masiva.

## 6. Fase 1 — estabilización (resultado)

| Punto | Cambio |
|---|---|
| Proteger el trabajo | Commit `dc50c61` con la iteración 2 y `Docs/Diseno.md`. |
| Escena congelada | `OlmecaPrototypeGenerator` se niega a sobrescribir `Olmeca_Prototipo`. |
| Documentación | `Docs/Rendimiento.md`, `CLAUDE.md` y este informe. |
| Android | Solo horizontal; calidad inicial Bajo; `companyName` JohnLopezo e identificador `com.johnlopezo.cronicasdemexico`. |
| Build Android | `Editor/Build/AndroidBuild` (menú **Crónicas → Build → Android (desarrollo)**). Primer build de desarrollo **correcto**: 0 errores, 0 avisos, APK de 45 MB en 8 min (i7-10700). Aún no instalado ni probado en un dispositivo. Al cerrarse, Unity registra un `Gradle build failed` del comando `gradle --stop` (puerto ocupado): no afecta al APK. |
| Limpieza | Eliminadas las carpetas vacías `lfs/` y `state/` de la raíz; `/.utmp/` (temporales de Gradle/CMake) añadido a `.gitignore`. |
| Pruebas | 28 EditMode y 21 PlayMode pasando (dos ejecuciones PlayMode seguidas). Se corrigió una prueba de ataque que dependía del timing de la física y, al fallar, dejaba objetos que rompían otras pruebas. |
| Regla de vida | `Health.Heal(cantidad, HealSource)`, `ReviveFull()` solo tras morir y prueba de no regeneración. |
