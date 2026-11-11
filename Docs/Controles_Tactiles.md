# Controles táctiles (Android)

Cómo funcionan los controles en pantalla, qué se arregló en la primera prueba en un teléfono real y cómo probarlos. Plataforma objetivo: Android en horizontal (GDD §14).

## 1. Distribución

Aprobada por el autor. Las posiciones están en píxeles de una pantalla de referencia de 1920×1080; `CanvasScaler` las ajusta a cada pantalla (mezcla ancho/alto 0,5) y `SafeArea` las aparta de la muesca y de las esquinas redondeadas.

| Control | Lugar | Tamaño | Botón del mando virtual | Acción del jugador |
|---|---|---|---|---|
| Joystick | abajo a la izquierda (270, 260) | 300 (palanca 140, recorrido 110) | stick izquierdo | Moverse |
| **Atacar** | abajo a la derecha (−230, 230) | 190 | Oeste | Attack |
| Esquivar | (−90, 100) | 120 | Sur | Dodge |
| Defender | (−440, 170) | 120 | LB | Defensa (mantener) |
| Especial | (−410, 340) | 120 | RB | Especial |
| Tierra | (−250, 450) | 110 | Este | Golpe de tierra |
| Hablar | (−90, 330) | 110 | Norte | Interact (examinar, hablar, cerrar mensajes) |
| **Usar** | (−90, 510) | 100 | cruceta izquierda | UseItem (el consumible más útil) |
| **Fijar** | (−430, 510) | 100 | pulsar el stick derecho | LockOn (fijar o soltar objetivo; Tab en el teclado) |
| Menú | arriba a la derecha (−90, −80) | 100 | Select | Menú del juego (bolsa, Bendiciones…) |

Las posiciones de los botones se miden desde la esquina inferior derecha; las del Menú, desde la superior derecha.

## 2. Cómo funcionan

- **Son un mando virtual.** Cada control es un `OnScreenStick` o un `OnScreenButton` del Input System, que mueve un botón de un mando (`Gamepad`) que no existe físicamente. El juego ya tenía esos bindings de mando, así que no hubo que cambiar nada de movimiento ni de combate: tocar Atacar es pulsar el botón Oeste.
- **Dónde viven.** Prefab `Prefabs/UI/Controles_Tactiles` en la escena Boot, que dura toda la partida. Los crea **Crónicas → Crear controles táctiles en Boot** (`TouchControlsSetup`). No se editan a mano: se cambia `TouchControlsSetup` y se regenera.
- **Cuándo se ven** (`UI/Touch/TouchControls`): solo si hay pantalla táctil (en el PC, con `forceVisible`) y el juego no está en pausa. Con un menú abierto (`Time.timeScale = 0`) se ocultan, porque los menús de UI Toolkit ya responden al toque.
- **Cómo les llegan los toques.** Por el EventSystem de la escena que esté a la vista: el del menú de inicio, o el que trae cada escena de juego (ciudad, selva, los tres pisos de la mazmorra y la arena de pruebas). **Los controles no llevan EventSystem propio** (ver §3). Toda escena de juego nueva debe traer el suyo (`OlmecaPrototypeGenerator.CreateEventSystem`).
- **Tocar la pantalla ya no ataca.** Se quitó el binding `<Touchscreen>/primaryTouch/tap` de Attack: si no, cada toque del joystick habría atacado.

### Textos en el teléfono

Con pantalla táctil (`Touchscreen.current != null`) el juego no nombra teclas:

- El aviso de interacción dice «Hablar» o «Examinar», sin «[E]» (`PlayerInteractor`).
- El botón «Bolsa (I)» del HUD se oculta: el menú se abre con Menú y ese botón estorbaba a Esquivar (`GameMenuController`).
- En la bolsa, Consumibles dice «el botón Usar gasta el más útil» en vez de «tecla 1».
- Las pistas del tutorial de la mazmorra tienen un texto táctil (`TutorialHint.TouchText`, rellenado por `OlmecaDungeonGenerator`). Lo llevan las pistas que nombran teclas: «moverse» y «atacar».
- La Bendición de la Tierra dice «tecla C, botón Este o Tierra en pantalla».
- El contador de FPS (solo en desarrollo) va bajo el botón Menú, y no encima de él.

## 3. Lo que falló en el Galaxy S24+ (2026-11-10) y por qué

**Síntoma:** en el teléfono se veían los controles, pero ni el joystick ni los botones hacían nada.

**Diagnóstico:** se instaló un build con registro de toques (temporal, ya borrado). El toque llegaba a la pantalla y caía sobre el joystick, pero el EventSystem activo no lo procesaba, así que el mando virtual seguía en (0, 0). El EventSystem activo era el del prefab de los controles. Sus acciones de UI (Point, Click…) estaban guardadas vacías (`fileID: 0`). `CreateEventSystem` crea esas referencias como objetos de la escena: en una escena se guardan, pero en un prefab se pierden. En el menú de inicio sí funcionaba porque ese EventSystem está en la escena Boot.

**Arreglo:** se quitó el EventSystem propio de los controles; ahora usan el de cada escena.

**Por qué no lo vieron las pruebas:** llamaban al botón directamente (`OnPointerDown`), sin pasar por el EventSystem. La prueba nueva `RealTouch_InAGameScene_MovesTheStick_AndPressesAttack` simula un toque de verdad en la ciudad: arrastra el joystick y toca Atacar. Detalle: en modo batch el Editor no tiene el foco y el Input System descarta los toques simulados. Por eso la prueba pone `backgroundBehavior = IgnoreFocus` mientras dura y lo restaura al final.

## 4. Probar en el teléfono

1. Build de desarrollo: `-buildTarget Android -executeMethod CronicasDeMexico.Editor.Build.AndroidBuild.BuildDevelopment` (ver `CLAUDE.md`). Deja `Builds/Android/CronicasDeMexico-dev.apk` y cambia el target del proyecto a Android; al terminar, volver con `-buildTarget Win64`.
2. Instalar con el adb del Editor (`.../PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe install -r <apk>`). Si adb dice `unauthorized`, aceptar en el teléfono «¿Permitir depuración USB?».
3. Registro del juego: `adb logcat -s Unity`.

## 5. Pendiente

- Confirmar en el S24+ que todo responde y ajustar los tamaños y posiciones con lo que diga el autor.
- Medir FPS en el teléfono durante el combate. En la ciudad marcaba 30 FPS, el objetivo.
- Los controles no cambian de tamaño ni se pueden mover desde las Opciones (no se ha pedido).
