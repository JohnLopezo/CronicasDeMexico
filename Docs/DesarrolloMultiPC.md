# Desarrollo en dos computadoras

Procedimiento y requisitos técnicos para desarrollar Crónicas de México indistintamente en las dos PC sin perder avances. Complementa a `CLAUDE.md` (reglas para Claude Code) y a `Docs/Rendimiento.md` (perfiles de calidad y presupuestos gráficos).

## 1. Equipos

| | PC 1 — HP ProDesk 600 G2 | PC 2 — Desarrollo principal |
|---|---|---|
| CPU | Intel Core i5-6500 (4 núcleos / 4 hilos) | Intel Core i7-10700 (8 núcleos / 16 hilos) |
| RAM | 12 GB | 16 GB |
| GPU | Intel HD Graphics 530 (integrada) | Intel UHD Graphics 630 (integrada) |
| Disco | SSD 1 TB | SSD M.2 ~930 GB |
| Sistema | Windows | Windows 10 Pro |
| Papel | **Referencia de rendimiento** (lo que corre bien aquí corre en ambas) | Desarrollo diario, horneado de iluminación y builds de Android |

Las dos GPU son integradas de la misma generación y potencia parecida, y comparten la RAM del sistema. Por eso **el límite gráfico es el mismo en ambas**: el i7 compila, importa y hornea más rápido, pero no dibuja mucho más. Toda escena y efecto se valida en **Bajo** en el i5-6500.

## 2. Software obligatorio (idéntico en ambas PC)

| Componente | Versión / nota |
|---|---|
| Unity Editor | **6000.0.85f1** exactamente (`ProjectSettings/ProjectVersion.txt`). Instalar desde Unity Hub en la ruta por defecto `C:\Program Files\Unity\Hub\Editor\6000.0.85f1`, porque `Tools/setup-git.ps1` y `CLAUDE.md` la usan. **No actualizar el Editor** en una sola PC; un cambio de versión se decide, se hace en ambas y va en su propio commit. |
| Módulos de Unity | **Android Build Support** (con OpenJDK y Android SDK & NDK Tools). **iOS Build Support** es opcional: permite cambiar de plataforma y revisar ajustes, pero compilar para iOS requiere una Mac con Xcode. |
| Paquetes | Los fija `Packages/manifest.json` + `packages-lock.json` (URP 17.0.4, Input System, Cinemachine 3, ProBuilder…). No instalar ni actualizar paquetes desde una sola PC sin hacer commit de ambos archivos. |
| Git + Git LFS | Git for Windows con LFS. Después de clonar, ejecutar `Tools/setup-git.ps1` (configura LFS, UnityYAMLMerge, `core.autocrlf false`, `pull.rebase true`). |
| IDE | Visual Studio o Rider; los `.sln`/`.csproj` no se versionan y cada PC los regenera. |
| Claude Code | Abrirlo **desde la carpeta del proyecto** para que lea `CLAUDE.md`. La memoria de Claude Code es local de cada PC; lo que deba saberse en ambas va en `CLAUDE.md` o en `Docs/`. |

## 3. Qué viaja por Git y qué no

- **Viaja:** `Assets/` (con sus `.meta`), `Packages/manifest.json` y `packages-lock.json`, `ProjectSettings/`, `Docs/`, `Tools/`, `CLAUDE.md`. Los binarios (imágenes, audio, modelos, fuentes, `.exr` de lightmaps) por **Git LFS**.
- **No viaja (cada PC lo regenera):** `Library/`, `Temp/`, `Logs/`, `UserSettings/` (layout del Editor, vista de escena), `Builds/`, `.sln`/`.csproj`, APK/AAB.
- La primera apertura en una PC importa todo el proyecto y genera `Library/` (puede tardar varios minutos, más en el i5). Es normal.
- `EditorUserBuildSettings` (la plataforma activa) vive en `Library/`: cada PC puede tener activa una plataforma distinta sin conflicto, pero cambiar de plataforma reimporta las texturas.

## 4. Primera vez en una PC

1. Instalar Unity 6000.0.85f1 con Android Build Support desde Unity Hub.
2. Instalar Git for Windows (incluye Git LFS).
3. Clonar: `git clone https://github.com/JohnLopezo/CronicasDeMexico.git`
4. En la carpeta del proyecto: `powershell -ExecutionPolicy Bypass -File Tools\setup-git.ps1`
5. `git lfs pull` (comprobar que las imágenes no son archivos de texto de 1 KB).
6. Cambiar a la rama de trabajo (`git switch develop` o la `feature/` en curso).
7. Abrir el proyecto con Unity Hub y esperar la importación. Comprobar que la consola no tiene errores y que la calidad del Editor está en **Bajo**.
8. Aplicar los ajustes del Editor de la sección 7.

## 5. Rutina diaria

**Regla de oro: una sola PC trabaja en una rama a la vez, y nunca se cambia de PC sin hacer push.** El proyecto lo desarrolla una persona, así que los conflictos solo aparecen si se olvida subir o bajar cambios.

**Al empezar (en cualquier PC):**

1. Cerrar Unity si está abierto.
2. `git status` → debe estar limpio. Si no lo está, son cambios sin subir de una sesión anterior en esta PC: revisarlos antes de seguir.
3. `git pull` (con rebase, por `setup-git.ps1`).
4. Abrir Unity y esperar la importación.

**Al terminar (o antes de cambiar de PC):**

1. Guardar escenas y proyecto (**File → Save Project**, para que se escriban los `.asset` modificados).
2. Cerrar Unity (así `ProjectSettings/` queda escrito en disco y nada lo sobrescribe después).
3. `git status` y `git diff --stat`: confirmar que solo cambió lo esperado y que cada asset nuevo tiene su `.meta`.
4. Commit con mensaje descriptivo en español y `git push`.
5. En GitHub o con `git log origin/<rama> -1` comprobar que el commit está arriba.

**Al usar Claude Code:** Claude no hace commits ni push sin autorización. Antes de cambiar de PC, pedirle explícitamente el commit y el push, o hacerlo a mano.

## 6. Ramas y conflictos

- `main` estable, `develop` integración, `feature/<nombre>` para cada sistema o fase. Se fusiona a `develop` cuando las pruebas pasan, y a `main` en cada hito.
- **Archivos sensibles a conflicto:** escenas (`.unity`), `ProjectSettings/*.asset`, URP Assets y lightmaps. Para reducirlos:
  - Una escena por región o zona (ver `Docs/Rendimiento.md` §Escenas por región) y contenido en prefabs.
  - **La iluminación se hornea solo en el i7-10700** y se sube junto con sus lightmaps; el i5 no rehornea escenas ajenas.
  - Los ajustes de proyecto y de calidad se cambian con el Editor cerrado en la otra PC, en un commit propio.
- El driver UnityYAMLMerge (configurado por `setup-git.ps1`) resuelve solo la mayoría de fusiones de YAML de Unity. Si aun así queda un conflicto en una escena, conservar una versión completa (`git checkout --ours` o `--theirs` sobre ese archivo) y rehacer a mano el cambio menor; **no editar a mano los marcadores de conflicto en una escena.**
- Nunca mover o renombrar assets fuera del Editor sin mover su `.meta` (se pierde el GUID y se rompen referencias).
- Carpetas vacías con `.gitkeep`.

## 7. Ajustes del Editor para gráficos integrados

Estos ajustes son locales (`UserSettings/` o preferencias) y se aplican a mano en cada PC:

- **Calidad del Editor: Bajo** (ya es el valor por defecto del proyecto). Subir a Medio/Alto solo para revisar cómo se ven.
- **Scene view:** desactivar *Always Refresh* y, al editar geometría, los efectos de escena (*Post Processing*, *Fog*, *Particle Systems*) en el menú de efectos de la vista.
- **Game view:** una resolución fija de prueba (1280×720 o 1920×1080 horizontal) en lugar de *Free Aspect*; no usar *Maximize On Play* en el i5 si baja el rendimiento.
- **Lighting:** *Auto Generate* desactivado; hornear a mano con **Progressive CPU** (el valor actual del proyecto; las GPU integradas no son fiables para el lightmapper de GPU).
- **Una sola vista de juego/escena visible a la vez** al hacer pruebas en el i5; cerrar ventanas pesadas (Profiler, Frame Debugger) cuando no se usan.
- **Enter Play Mode Options** ya está configurado (sin recarga de dominio) para iterar rápido.
- **Preferences → Asset Pipeline:** *Auto Refresh* activado; si una importación grande ralentiza el i5, se puede desactivar temporalmente y refrescar con Ctrl+R.
- Las mediciones de FPS se hacen en un **build de desarrollo**, no en el Editor (`Docs/Rendimiento.md`).

## 8. Builds

- **Android:** preferentemente en el i7-10700 (16 GB; Gradle + Editor consumen mucha memoria). Menú **Crónicas → Build → Android (desarrollo)** o el comando batch de `CLAUDE.md`. El APK queda en `Builds/Android/` (no se versiona).
- **Windows:** cualquiera de las dos PC.
- **iOS:** no se compila en estas PC. El proyecto se mantiene preparado (ver `Docs/Rendimiento.md` §Plataformas) y el build final se hará en una Mac con Xcode.

## 9. Lista de comprobación de compatibilidad

Antes de dar por terminado un cambio que se va a abrir en la otra PC:

- [ ] Misma versión de Unity (`ProjectVersion.txt` sin cambios inesperados).
- [ ] `manifest.json` y `packages-lock.json` en el commit si cambiaron paquetes.
- [ ] Cada asset nuevo con su `.meta`; ningún binario fuera de LFS (`git lfs ls-files`).
- [ ] La escena se ve y rinde bien en **Bajo**.
- [ ] Pruebas EditMode y PlayMode pasan (comandos en `CLAUDE.md`).
- [ ] `git status` limpio después del push.

## 10. Pendientes detectados (no aplicados; requieren autorización)

- **Cambios de la Fase 1 sin commit en el i7-10700** (rama `feature/prototipo-olmeca`, 15 archivos modificados y 3 nuevos al 2026-10-27). Hasta que se haga commit y push, **esos avances solo existen en esta PC**.
- `.gitattributes` trata todos los `*.asset` como texto YAML, pero `LightingData.asset` (y otros datos horneados) son binarios. Antes del primer horneado de luces conviene añadir una regla `LightingData.asset binary` (o enviarlo por LFS) para que Git no lo corrompa al normalizar finales de línea.
- La configuración global de Git en el i7 tiene `core.autocrlf true` y `pull.rebase false`; `setup-git.ps1` lo corrige a nivel de repositorio. En el i5 hay que ejecutarlo igualmente.
