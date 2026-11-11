# Crónicas de México — Estado del juego y mejoras deseadas

11 de noviembre de 2026 · Copia en el repositorio del documento editable de Claude Docs: https://claude.ai/artifact/CbftrfDrPqf185tsVA91KP. Si se edita allá, hay que volver a exportarlo aquí (o al revés) para que no se separen.

## Cómo usar este documento

Este documento describe el estado de *Crónicas de México* al 11 de noviembre de 2026 y deja un espacio para que el autor escriba las mejoras que quiere. Sirve para dos lectores:

- **El autor:** para revisar qué ya existe y escribir en la sección «Lo que quiero» qué cambiar, quitar o agregar.
- **Otra IA** (Claude Code u otra): para entender el proyecto sin leer todo el repositorio y hacer el trabajo que pida el autor, respetando las reglas de la sección «Reglas de trabajo».

**Cómo pedir una mejora:** en «Lo que quiero», copia la plantilla, llena qué quieres, por qué, cómo sabrás que quedó bien y la prioridad. Mientras más concreto seas (números, lugares del juego, ejemplos de otros juegos), mejor lo entenderá la IA.

**Fuente de verdad:** si algo de aquí choca con el repositorio, manda el repositorio. La historia y los nombres los manda `Docs/Historia_Mundo.md`; el diseño, `Docs/Diseno.md`; el estado de cada sesión, `Docs/Bitacora.md`.

## Resumen del proyecto

*Crónicas de México* es un RPG de acción y mazmorras para un jugador, en 2.5D (sprites de pixel art sobre escenarios con profundidad), inspirado en las culturas de México. Su primer capítulo es olmeca (La Venta) y ya se puede jugar de principio a fin: ciudad, selva, una mazmorra de 3 pisos, un minijefe y un jefe.

| Dato | Valor |
| --- | --- |
| Motor | Unity 6000.0.85f1 (LTS), C#, URP 17.0.4 |
| Plataforma objetivo | Android en horizontal (probado en un Galaxy S24+); iOS preparado; PC para desarrollo |
| Objetivo de rendimiento | 30 FPS estables en Android de gama media; en el S24+ va a 30 FPS en la ciudad |
| Estilo | Pixel art en vista isométrica fija; arte producido con scripts de Blender (`Tools/ArtPipeline`) |
| Inspiraciones de jugabilidad | Tunic, Eastward, Octopath Traveler II y Hyper Light Drifter |
| Repositorio | GitHub, rama de trabajo `feature/prototipo-olmeca` (último commit `80b7e73`) |
| Pruebas automáticas | EditMode 145/145 y PlayMode 139/139 pasan |
| Desarrollo | Dos PC Windows con gráficos integrados (i5-6500 y i7-10700), sincronizadas solo por Git |

**Qué se tomó de cada inspiración:**

- **Hyper Light Drifter:** combo de 3 golpes con avance, pausa al impactar (hitstop), sacudida de cámara, esquivas encadenadas.
- **Tunic:** apuntado asistido, fijar objetivo, bloqueo perfecto y páginas escondidas que enseñan mecánicas (el Códice).
- **Octopath Traveler II:** sistema de Ruptura (escudo y debilidades por afinidad) y acciones de senda con los habitantes.
- **Eastward:** cuadro de diálogo con retrato y texto letra por letra, y cocina con ingredientes.

**Estado honesto:** casi todo lo de la sesión del 11 de noviembre está probado por pruebas automáticas, pero **no se ha jugado a mano** ni en el Editor ni en el celular. El arte de varios personajes y enemigos es provisional.

## Lo que ya está hecho

El arco olmeca se juega completo: despertar en la ciudad, selva, mazmorra de 3 pisos, minijefe, jefe y recompensas. Abajo, cada sistema con lo que hace y dónde se ajusta.

### Mundo y escenas

| Escena | Qué hay | Cómo se crea |
| --- | --- | --- |
| `Boot` | Menú de inicio (Continuar, Nueva partida con elección de Guardián, Opciones, Salir) y controles táctiles; dura toda la partida | Generada en parte (`StartMenuSetup`, `TouchControlsSetup`) |
| `Olmeca_Ciudad` | Centro ceremonial, aldea, 20 habitantes que caminan y hablan, guardias, taller del artesano, puesto del comerciante, fogón de la curandera, sendero a la selva | Generada desde `Ciudad.json` (`OlmecaCityGenerator`); no se edita a mano |
| `Olmeca_Selva` | Sendero, 3 claros, espíritus corrompidos, fauna hostil, entrada a la estructura | Generada (`OlmecaJungleGenerator`) |
| `Olmeca_Mazmorra_P1` a `P3` | El Umbral (tutorial), El Pantano (agua que frena, minijefe), antesala y arena del jefe; trampas, vasijas, basalto agrietado, ofrendas ocultas, santuarios | Generadas (`OlmecaDungeonGenerator`) |
| `Arena_Pruebas_Jefes` | 6 jefes de prueba para futuros arcos; no enlazada con el juego | Generada (`TrialArenaGenerator`) |
| `Olmeca_Prototipo` | Prototipo viejo, congelado | Se edita a mano |

### Historia y misiones

- Seis misiones principales encadenadas: `MP_OLM_01` (El despertar) a `MP_OLM_06` (llevar el Fragmento de la Primera Memoria al altar-trono).
- Diálogos con ramas según banderas de la historia, NPC con identidad (`NpcData`), misiones con pasos por avisos (`hablar:`, `examinar:`, `vencer:`, `llegar:`).
- Siete Guardianes jugables, humanos ligados a su animal: Balam (jaguar, Tierra), Xali (ajolote, Agua), Naira (lobo, Viento), Ameyali (mariposa, Luz), Itzel (águila, Luz), Yaret (quetzal, Viento) y Ocel (xoloitzcuintle, Oscuridad).

### Combate del jugador

| Mecánica | Cómo funciona | Valores actuales |
| --- | --- | --- |
| Combo | 3 golpes seguidos con avance; el tercero remata | Remate ×1,5 de daño; ventana de 0,5 s entre golpes |
| Pulsación guardada | Atacar o esquivar un poco antes de tiempo no se pierde | 0,15 s |
| Hitstop y sacudida | Pausa breve y sacudida de cámara al acertar y al recibir daño | 0,05 s de pausa; desactivable la sacudida |
| Apuntado asistido | El golpe gira hacia el enemigo vivo más cercano delante | 3 m, ±60° |
| Fijar objetivo | Elige un enemigo; los golpes y el especial van a él; mira dorada; la cámara se recorre hacia él | Fija a 8 m, suelta a 12 m; cámara 35 % con tope de 2,5 m |
| Esquiva | Dos esquivas encadenadas y luego espera | 0,6 s de espera |
| Esquiva perfecta | Esquivar justo al llegar el golpe da cámara lenta y recarga las esquivas | Ventana 0,2 s; ×0,35 durante 0,45 s |
| Defensa | Mantener: quieto y recibe la mitad del daño | 50 % |
| Bloqueo perfecto | Defender justo antes del golpe lo anula y aturde al atacante (no a los jefes) | Ventana 0,2 s; aturde 1,2 s; se rearma a los 0,6 s |
| Habilidad especial | Área enfrente; gasta energía | 25 de energía, 5 s de espera |
| Golpe de tierra | Bendición de la Tierra: área alrededor, rompe basalto agrietado | Afinidad Tierra siempre |

La vida **no se regenera** (regla del diseño): solo sube con santuarios y consumibles. La energía tampoco se regenera sola.

### Enemigos y jefes

- Siete enemigos de la lámina del autor con movimiento, ataque avisado, defensa y habilidad propios: serpiente, guacamaya, mono, avispa, murciélago, cocodrilo y espíritu jaguar (`WildEnemy` + `EnemyData`).
- Espíritus corrompidos y ecos borrados; minijefe Espíritu del Jaguar (El Pantano); jefe Guardián de la Piedra Viva en 3 fases (piso 3).
- Los ataques avisan con una marca en el suelo (círculo, anillo o franja).

### Sistema de Ruptura (Octopath)

Cada enemigo tiene un escudo de rombos y debilidades de afinidad. Cada golpe con una debilidad, y cada bloqueo perfecto, quita un rombo. Sin escudo, el enemigo queda roto 2,5 s (3 s los jefes): recibe ×1,5 de daño y los comunes quedan aturdidos. El panel de arriba muestra nombre, rombos, debilidades y «¡RUPTURA!». Tabla completa en `Docs/Sistemas/Ruptura.md`; **las debilidades son una propuesta de Claude pendiente del autor**.

### Progresión y economía

- Nivel y experiencia (con menos experiencia sobre el nivel de la zona), energía, bolsa con materiales, consumibles, 2 ranuras de reliquia y coleccionables.
- Bendiciones: la de la Tierra al vencer al jefe y una secundaria a elegir entre tres.
- Fabricación con el artesano, comercio y trueque con el comerciante (moneda ficticia: «ecos»).
- Museo de la Memoria desde el santuario de Memoria.

### Códice de la Memoria (Tunic)

Ocho páginas escondidas que se revelan al hacer cosas que ya existían (asomarse al río, entrar al piso 1, abrir ofrendas ocultas tras el basalto, examinar el altar-trono y el sello del Guardián). Cinco enseñan mecánicas y tres son culturales con su origen (documentado, interpretación o ficción). Se ven en la pestaña Códice del menú. El Códice es un objeto ficticio: los olmecas no dejaron códices. Detalle en `Docs/Sistemas/Codice.md`.

### Acciones de senda (Octopath) y cocina (Eastward)

- Cada Guardián tiene un verbo: **Indagar** (Itzel, Ocel), **Persuadir** (Balam, Naira) o **Escuchar** (Xali, Ameyali, Yaret).
- Junto a un habitante con algo para tu verbo, **mantener «Hablar» 0,5 s** (un anillo se llena) lanza la acción: líneas propias y, una sola vez, objetos, experiencia, recetas u ofertas. Hay 11 respuestas **borrador** de 8 habitantes.
- Cocina en el fogón de la curandera (pestaña Cocinar): bebida de maíz, pescado envuelto en hoja y bebida de cacao. Ingredientes: maíz, pescado y cacao. Detalle en `Docs/Sistemas/Acciones_Senda.md`.

### Interfaz y comodidad

- HUD: vida, energía, experiencia, objetivo de la misión, barra del jefe con estela, panel de ruptura, números de daño, avisos emergentes, FPS.
- Diálogo con placa del nombre, retrato del NPC y texto letra por letra; la primera pulsación completa el texto.
- Opciones: calidad gráfica (Bajo, Medio, Alto), sacudida de cámara, números de daño y velocidad del texto.
- Menú del juego con pestañas: Bolsa, Bendiciones, Guardián, Códice, y según dónde se abra Fabricar, Cocinar, Comerciar o Museo.

### Guardado

Partida en JSON con versión y copia de respaldo. Se guarda al descansar en un santuario de Memoria y se continúa desde ahí. Las opciones del dispositivo (calidad y comodidad) van aparte, en PlayerPrefs.

## Controles

Los mismos controles funcionan en PC, mando y celular: los botones táctiles actúan como un mando virtual. Las acciones están en `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`.

| Acción | Teclado y ratón | Mando | Botón táctil |
| --- | --- | --- | --- |
| Moverse | WASD o flechas | Stick izquierdo | Joystick (abajo a la izquierda) |
| Atacar | Clic izquierdo | Botón Oeste | Atacar (grande, abajo a la derecha) |
| Esquivar | Espacio | Botón Sur | Esquivar |
| Defender (mantener) | Shift izquierdo | LB | Defender |
| Habilidad especial | F | RB | Especial |
| Golpe de tierra | C | Botón Este | Tierra |
| Hablar o examinar (mantener = acción de senda) | E | Botón Norte | Hablar |
| Usar el consumible más útil | 1 | Cruceta izquierda | Usar |
| Fijar o soltar objetivo | Tab | Pulsar el stick derecho | Fijar |
| Menú (bolsa) | I | Select | Menú (arriba a la derecha) |
| Opciones | Esc | Start | (desde el menú) |

La posición exacta de cada botón táctil está en `Docs/Controles_Tactiles.md`. Para mover un botón se cambia `TouchControlsSetup` y se regenera con **Crónicas → Crear controles táctiles en Boot**.

## Mapa técnico

Todo el contenido propio vive en `Assets/_Project/`, repartido en seis ensamblados de C# que dependen en un solo sentido: `Core` no conoce a nadie y `Editor` conoce a todos.

![Todo depende de Core y Editor conoce a todos](Estado_General_Ensamblados.png)

World, Gameplay y UI no se conocen entre sí: lo que Gameplay le cuenta al HUD o a la cámara pasa por los avisos estáticos de `Core/Feedback`.

### Dónde vive cada cosa

| Qué | Dónde |
| --- | --- |
| Servicios, eventos, guardado, historia, progreso, avisos para el HUD | `Scripts/Core` (`Boot`, `Events`, `Narrative`, `Save`, `Progress`, `Feedback`, `Travel`) |
| Datos de diseño (ScriptableObjects) | `Scripts/Data` (`Characters`, `Enemies`, `Narrative`, `Rewards`) y sus assets en `Data/` y `Resources/` |
| Jugador, combate, enemigos, NPC, interacción, mazmorra | `Scripts/Gameplay` (`Player`, `Combat`, `Enemies`, `Npc`, `Interaction`, `Dungeon`, `Rewards`, `Travel`, `Narrative`) |
| Cámara, pixel art, rendimiento | `Scripts/World/Rendering` |
| HUD, menús, controles táctiles | `Scripts/UI` y `Art/UI/Hud` (`Hud.uxml`, `Hud.uss`) |
| Generadores de escenas y assets, builds | `Scripts/Editor` (`SceneGeneration`, `Build`) |
| Pruebas | `Tests/EditMode` y `Tests/PlayMode` |
| Documentación | `Docs/` (fuera de `Assets`) |
| Flujo de arte en Blender | `Tools/ArtPipeline` |

### Piezas clave por sistema

| Sistema | Clases principales | Datos editables |
| --- | --- | --- |
| Combate del jugador | `PlayerAttack`, `PlayerMovement`, `PlayerDefense`, `PlayerSpecial`, `PlayerAbility`, `Health`, `TimeDilation` | Inspector del prefab `Prefabs/Player/Jugador` |
| Ruptura | `BreakGauge`, `Core/Feedback/BreakReadout` | `Data/Enemies/ED_*.asset` (`shieldPoints`, `weaknesses`) |
| Códice | `Core/Narrative/CodexService`, `Data/Narrative/CodexCatalog` | `Resources/Narrativa/CatalogoCodice` |
| Acciones de senda | `DialogueInteractable`, `PlayerInteractor`, `PathActionCatalog` | `Resources/Narrativa/AccionesSenda`, `GUA_*.asset` (`pathAction`) |
| Cocina y fabricación | `RecipeData` (`Station`), `GameMenuController` | `Data/Rewards/Olmeca/RCT_*.asset` |
| HUD | `HudController`, `MessageBox`, `DamageNumberLayer`, `HoldRing` | `Art/UI/Hud` |
| Cámara | `IsometricCameraFollow`, `PixelArtScreen` | Inspector de la cámara de cada escena |

### Comandos (Unity no está en el PATH)

```
U="/c/Program Files/Unity/Hub/Editor/6000.0.85f1/Editor/Unity.exe"
# Pruebas (no combinar -runTests con -quit)
"$U" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults <ruta>/editmode.xml -logFile <ruta>/editmode.log
"$U" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults <ruta>/playmode.xml -logFile <ruta>/playmode.log
# Build de Android (deja el APK en Builds/Android/CronicasDeMexico-dev.apk)
"$U" -batchmode -quit -projectPath . -buildTarget Android -executeMethod CronicasDeMexico.Editor.Build.AndroidBuild.BuildDevelopment -logFile <ruta>/android.log
# Regenerar la ciudad
"$U" -batchmode -quit -projectPath . -executeMethod CronicasDeMexico.Editor.SceneGeneration.OlmecaCityGenerator.Generate -logFile <ruta>/ciudad.log
```

El modo batch falla si el proyecto está abierto en el Editor. El APK se instala con el `adb` que trae Unity (`.../PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe install -r ...`); el celular debe autorizar la depuración USB.

## Reglas de trabajo para la IA

Cualquier IA que continúe debe seguir estas reglas; vienen de `CLAUDE.md` y de decisiones del autor.

**Antes de empezar**

1. Leer `CLAUDE.md`, `Docs/Estado_y_Plan.md` y la entrada más reciente de `Docs/Bitacora.md` (va arriba).
2. `git pull` y comprobar que el último commit coincide con el de la bitácora.
3. Proponer primero (análisis, archivos y plan por etapas) y **esperar autorización** antes de modificar.

**Mientras trabaja**

- Cambios pequeños; conservar escenas, prefabs y ajustes que funcionan; no añadir paquetes sin necesidad.
- Las escenas `Olmeca_Ciudad`, `Olmeca_Selva` y `Olmeca_Mazmorra_P1` a `P3` **no se editan a mano: se regeneran** con su generador.
- Los valores de diseño van en ScriptableObjects o en campos del Inspector, no escritos fijos en el código.
- Lo que Gameplay le cuenta al HUD sin canal de eventos pasa por `Core/Feedback` (así no hay que regenerar escenas).
- Toda ralentización del tiempo pasa por `TimeDilation`; nunca `Time.timeScale = 0` fuera de los menús (0 significa pausa).
- Estado estático: limpiarlo con `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` (la recarga de dominio está apagada).
- En `Update`, sin asignaciones de memoria ni `GetComponent`/`Find`; búferes fijos y reutilización.
- Todo debe verse bien y rendir en calidad Baja, en el i5-6500 y en Android de gama media.
- Cada lógica nueva lleva su prueba EditMode o PlayMode.

**Contenido cultural**

- Separar lo documentado, lo interpretado y lo ficticio; no inventar dioses históricos.
- No mezclar lo olmeca con lo mexica o lo maya (por ejemplo, nada de nombres en náhuatl para cosas olmecas).
- La historia, los nombres y los personajes los manda `Docs/Historia_Mundo.md` (biblia del autor).

**Al terminar**

- Listar los archivos modificados, las pruebas realmente ejecutadas y lo que falta probar en el Editor o en el celular. No declarar éxito sin pruebas.
- Añadir una entrada arriba en `Docs/Bitacora.md`.
- **No hacer commit ni push sin aprobación del autor.** Antes de que el autor cambie de PC, recordarle el commit y el push.
- Responder y documentar en español de México.

## Pendientes y decisiones abiertas

Lo más urgente es jugar todo a mano en el celular: las pruebas automáticas pasan, pero nadie ha sentido el combate ni las ventanas de tiempo con el dedo.

**Probar y ajustar (autor)**

- [ ] Jugar en el S24+ el combo, las esquivas, el bloqueo y la esquiva perfectos (ventanas de 0,2 s), y decir si son demasiado estrechas.
- [ ] Probar el botón Fijar, la mira y el recorrido de la cámara (35 %, tope 2,5 m).
- [ ] Probar la pulsación larga de «Hablar» (0,5 s) y su anillo.
- [ ] Revisar que el retrato del diálogo se vea nítido y que los números de daño se lean.
- [ ] Medir los FPS en la mazmorra y en la arena del jefe en el celular.

**Textos escritos por Claude que el autor debe revisar**

- [ ] Tabla de debilidades y escudos de cada enemigo (`Docs/Sistemas/Ruptura.md`).
- [ ] Las 8 páginas del Códice y su contraste con fuentes (`Docs/Sistemas/Codice.md`).
- [ ] Las 11 respuestas de las acciones de senda (`Docs/Sistemas/Acciones_Senda.md`).
- [ ] Los ingredientes, platillos y notas culturales de la cocina (mismo documento).
- [ ] Si se queda el nombre «Códice» o se usa otro.

**Decisiones abiertas del diseño**

- [ ] Si el epiclásico y lo tolteca entran en el arco III, y qué afinidades tienen los arcos III a V.
- [ ] Si la figura espiritual desaparece después del juramento.
- [ ] Si la energía debe recuperarse un poco con los golpes (hoy no se regenera).
- [ ] Si los jefes también deben quedar aturdidos con el bloqueo perfecto y la ruptura (hoy no).
- [ ] Descubrir las debilidades jugando (ocultas hasta acertarlas, como en Octopath) en lugar de mostrarlas siempre.
- [ ] Conexión con el arco II.

**Arte pendiente (autor)**

- [ ] Fichas propias del artesano de jade, la anciana y la figura espiritual (hoy repiten arte de otros).
- [ ] Eco borrado definitivo, vistas de frente y espalda de los enemigos, poses de daño y caída de los jefes.
- [ ] Efecto visual propio para el bloqueo perfecto, la esquiva perfecta y la ruptura (hoy es un destello blanco).
- [ ] Iconos del Códice y objeto visible para el fogón de la curandera.

**Técnico**

- [ ] El proyecto quedó configurado para compilar en Android; para Windows hay que usar `-buildTarget Win64`.
- [ ] Benchmark en el i5-6500 (PC de referencia).
- [ ] Archivos sin versionar que no son de Claude: `Docs/Bus/`, `Docs/Guerreros/Enemigos.png`, PNG grandes en `Tools/ArtPipeline/Salida/`, `UpgradeLog.htm`.

## Lo que quiero

Esta sección es del autor: aquí se escribe qué mejorar, quitar o agregar, y la IA trabaja a partir de ella. Una fila por idea; la primera es un ejemplo que puedes borrar.

| # | Área | Qué quiero | Por qué | Cómo sé que quedó bien | Prioridad | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | Combate | (Ejemplo) Que la esquiva perfecta sea un poco más fácil en el celular | Con el dedo no me sale casi nunca | La logro 1 de cada 3 veces contra los murciélagos de El Umbral | Media | Por hacer |
| 1 |  |  |  |  |  | Por hacer |
| 2 |  |  |  |  |  | Por hacer |
| 3 |  |  |  |  |  | Por hacer |

**Prioridad:** Alta, Media o Baja. **Estado:** Por hacer, En curso, Hecho o Descartado.

**Áreas sugeridas:** Combate, Enemigos, Jefes, Mundo, Historia, Personajes, Interfaz, Controles, Arte, Sonido, Rendimiento, Otro.

### Plantilla para una idea grande

Copia esto debajo cuando una idea necesite más explicación que una fila:

```markdown
### Idea: <nombre corto>
- Qué quiero: <lo que el jugador ve o hace distinto>
- Dónde pasa: <escena, enemigo, menú, botón>
- Inspiración: <juego y qué parte, si aplica>
- Por qué: <qué problema resuelve o qué se siente mal hoy>
- Lo que NO quiero: <límites, cosas que deben seguir igual>
- Cómo sé que quedó bien: <prueba concreta, con números si se puede>
- Prioridad: Alta / Media / Baja
```

### Ideas sueltas

Escribe aquí lo que se te ocurra aunque no esté claro todavía; la IA te ayudará a convertirlo en filas de la tabla.

-

## Glosario

| Término | Qué es |
| --- | --- |
| Acción de senda | Lo que hace el Guardián al mantener «Hablar» con un habitante: Indagar, Persuadir o Escuchar |
| Afinidad | Elemento de un Guardián o de un golpe: Agua, Tierra, Fuego, Viento, Luz u Oscuridad (sistema de fantasía) |
| Bendición | Poder que da un jefe; la de la Tierra da el golpe de tierra |
| Biblia | `Docs/Historia_Mundo.md`, el documento del autor que manda en historia y nombres |
| Códice de la Memoria | Libro ficticio del Guardián con páginas escondidas |
| Ecos | Moneda ficticia del Mundo de las Memorias |
| Escudo (ruptura) | Rombos de un enemigo; sin ellos queda roto y recibe más daño |
| Fijar | Mantener un enemigo como objetivo de los golpes y la cámara |
| Generador | Script del Editor que crea una escena o assets desde datos; esas escenas se regeneran, no se editan a mano |
| Guardián | El protagonista que elige el jugador (siete guerreros) y también los jefes que custodian cada cultura |
| Hitstop | Pausa de una fracción de segundo al acertar un golpe |
| Incursión | Ataque de espíritus a la ciudad desde el sendero del oeste |
| Santuario de Memoria / de Vida | Punto de guardado y control / curación una vez por incursión a la mazmorra |
| Sombra del Olvido | La amenaza ficticia que borra la memoria de las culturas |
| Ventana perfecta | Los primeros 0,2 s de una defensa o una esquiva, cuando cuentan como perfectas |
