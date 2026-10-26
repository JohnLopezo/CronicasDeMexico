# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

**Crónicas de México**: RPG de acción y aventura en estilo HD-2D (sprites pixel art sobre escenarios 3D, inspirado en Octopath Traveler) con identidad basada en las culturas de México. La primera región es olmeca (La Venta). Unity **6000.0.85f1 (LTS)**, C#, URP 17.0.4. Responder y documentar en español.

Se desarrolla en dos PC Windows: **i5-6500** (referencia de rendimiento) e **i7-10700**. Ambas deben usar exactamente la misma versión del Editor.

## Comandos

Unity no está en el PATH: `U="/c/Program Files/Unity/Hub/Editor/6000.0.85f1/Editor/Unity.exe"`. El modo batch falla si el proyecto está abierto en el Editor.

```bash
# Pruebas (EditMode o PlayMode). No combinar -runTests con -quit.
"$U" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults <ruta>/editmode.xml -logFile <ruta>/editmode.log
# Una sola prueba o clase: añadir  -testFilter "CronicasDeMexico.Tests.ServiceLocatorTests"

# Compilar / reimportar sin abrir el Editor (revisar errores en el log)
"$U" -batchmode -quit -projectPath . -logFile <ruta>/compile.log
```

Configuración de Git por PC (LFS + UnityYAMLMerge, guardada en `.git/config`): `Tools/setup-git.ps1`.

## Arquitectura

- **Todo el contenido propio vive en `Assets/_Project/`.** Los assets de terceros van en `Assets/ThirdParty/` y `Assets/Plugins/`. `Docs/` y `Tools/` están fuera de `Assets`.
- **Assembly Definitions** en `Assets/_Project/Scripts/` (dependencias en un solo sentido):
  `Core` (sin dependencias) ← `Data` ← `World`, `Gameplay` (Input System, Cinemachine), `UI` (UGUI/TMP) ← `Editor` (solo editor).
  Las pruebas están en `Assets/_Project/Tests/{EditMode,PlayMode}` y solo referencian `Core` por ahora; añadir referencias según se necesite.
  `Data`, `World`, `Gameplay` y `UI` aún no tienen scripts, así que Unity muestra el aviso "will not be compiled". Es esperado.
- **Flujo de arranque:** la escena `Scenes/Boot/Boot.unity` (índice 0 en Build Settings) contiene `GameBootstrapper`, que registra los servicios en `ServiceLocator` y carga `initialScene` (`Sandbox_HD2D`) de forma **aditiva**. Boot permanece cargada toda la partida. `BootSceneLoader` (`RuntimeInitializeOnLoadMethod.BeforeSceneLoad`) añade Boot si se pulsa Play desde otra escena; en ese caso el bootstrapper no carga la escena inicial. Toda escena cargada por nombre debe estar en Build Settings.
- **Comunicación desacoplada:** canales de eventos como ScriptableObject (`Core/Events`: `EventChannel<T>`, `VoidEventChannel`). Los oyentes se suscriben en `OnEnable` y se desuscriben en `OnDisable`. Los datos de diseño (stats, ítems, habilidades) van en ScriptableObjects dentro de `Data`.
- **La recarga de dominio está desactivada** (Enter Play Mode Options = Disable Domain Reload, para iterar rápido en el i5). Todo estado estático debe limpiarse con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, como hace `ServiceLocator`.

## Rendimiento y calidad

- Tres niveles de calidad, **Bajo / Medio / Alto**, cada uno con su URP Asset en `Assets/_Project/Settings/Rendering/`. Bajo y Medio usan `Renderer_Rendimiento` (Forward, sin SSAO); Alto usa `Renderer_Calidad` (Forward+, SSAO). Los valores están en `Docs/Rendimiento.md`, que sirve también de registro de FPS por hito.
- El editor trabaja en **Bajo**; los builds arrancan en Medio. Las funciones nuevas deben verse bien y rendir en Bajo en el i5-6500.
- En HD-2D: usar DOF **Gaussiano** (no Bokeh), iluminación horneada o light probes con pocas luces en tiempo real, Sprite Atlas, y materiales compatibles con el SRP Batcher. En `Update`, evitar asignaciones de memoria y llamadas a `GetComponent`/`Find`; usar pooling para proyectiles y VFX.

## Convenciones de Unity y Git

- Asset Serialization: Force Text. Meta files visibles. Al mover o renombrar assets fuera del Editor, mover también su `.meta` para conservar el GUID; si no, se rompen las referencias.
- Las carpetas vacías llevan `.gitkeep`: Git no versiona carpetas vacías y Unity borraría su `.meta` en la otra PC.
- Los binarios (imágenes, audio, modelos, fuentes) van por Git LFS (`.gitattributes`). El YAML de Unity se guarda con LF y se fusiona con UnityYAMLMerge.
- Ramas: `main` (estable), `develop` (integración), `feature/<nombre>`. Evitar editar la misma escena en ambas PC a la vez; preferir prefabs y escenas aditivas.

## Plan de desarrollo (fase 1: prototipo olmeca)

1.0 base técnica (hecho) → 1.1 cámara y estilo HD-2D (Cinemachine, billboards, bloom, viñeta, DOF tilt-shift, niebla; el perfil base es `Volume_HD2D_Base`) → 1.2 personaje jugable → 1.3 combate básico → 1.4 zona olmeca (blockout con ProBuilder) → 1.5 pulido y profiling en el i5. Las referencias culturales van en `Docs/` y no deben mezclarse elementos olmecas con mexicas o mayas.
