# Estado del proyecto y plan para continuar

*Recopilación al 2026-10-29, al cerrar la sesión en el i7-10700. Rama `feature/prototipo-olmeca`.* Léela al retomar, junto con la última entrada de `Docs/Bitacora.md`.

## 1. Cómo retomar (en cualquier PC)

1. Cerrar Unity. `git status` limpio. `git pull`.
2. Comprobar que `git log -1` coincide con el último commit anotado en `Docs/Bitacora.md`.
3. Abrir Unity 6000.0.85f1 y esperar la importación.
4. Para jugar lo nuevo: abrir **`Scenes/Regions/Olmeca/Olmeca_Ciudad.unity`** y pulsar Play. *Boot arranca todavía en el prototipo (`Olmeca_Prototipo`).*
5. En Claude Code, abrirlo desde la carpeta del proyecto (lee `CLAUDE.md`) y decirle: "continuamos; lee `Docs/Estado_y_Plan.md` y la bitácora".

Reglas de trabajo: Claude propone un plan por etapas y espera autorización; no hace commit ni push sin que se le pida (`CLAUDE.md`, `Docs/DesarrolloMultiPC.md`).

## 2. Documentos de referencia

| Documento | Para qué |
|---|---|
| `Docs/Historia_Mundo.md` | **Biblia narrativa** (manda en historia, nombres, personajes y capítulos) |
| `Docs/Diseno.md` | GDD: sistemas, combate, mazmorras, UI, rendimiento (alineado con la biblia) |
| `Docs/Historia/` | `Capitulos.md`, `Misiones.md`, `Glosario.md` |
| `Docs/Personajes/` | `Protagonista.md` (Guardianes), `NPC_Olmecas.md` |
| `Docs/Culturas/Olmeca.md`, `Docs/Enemigos/Olmeca.md`, `Docs/Mazmorras/Olmeca.md` | Arco I |
| `Docs/Arte_Olmeca_Demo.md` | Dirección de arte pixel art y flujo de Blender (§12: estado del arte) |
| `Docs/Rendimiento.md` | Mediciones de FPS y presupuestos |
| `Docs/DesarrolloMultiPC.md` | Trabajo entre el i5-6500 y el i7-10700 |
| `Docs/Bitacora.md` | Qué se hizo en cada sesión |

## 3. Lo que está hecho

### Arte y escena
- Dirección de arte en **pixel art isométrico** producido con scripts de Blender (`Tools/ArtPipeline/`).
- Escena jugable **`Olmeca_Ciudad`**, generada por `OlmecaCityGenerator` desde `Ciudad.json`: centro ceremonial, montículo, recinto de columnas, aldea, enramadas que se cruzan por debajo, muelle y canoas. **No se edita a mano: se regenera.**

### Rendimiento
- La cámara de fondo usa un renderer sin efectos. En el i7, **Alto pasó de unos 105 a unos 290 FPS**.
- La herramienta de medición ya no se pausa si la ventana pierde el foco.

### Narrativa: decisiones tomadas
- Se usan los nombres de la biblia (Mundo de las Memorias, Sombra del Olvido, Guardián de la Piedra Viva, Fragmento de la Primera Memoria).
- El protagonista no tiene género definido.
- **Guardián del vertical slice: el Jaguar**.
- No hay prólogo aparte: los primeros pisos de la mazmorra olmeca hacen de tutorial.
- Hay **cinco arcos**, con las culturas del GDD repartidas dentro.
- La mazmorra olmeca tiene **3 pisos** en el vertical slice y **8** como meta.
- Cada jefe da el fragmento, una habilidad fija, el acceso a la siguiente región y la elección de una entre tres Bendiciones.

### Sistemas narrativos (en `Core/Narrative`, `Core/Save`, `Data/Narrative`; resumen técnico en `CLAUDE.md`)
- **`StoryState`**: guarda banderas y recibe avisos (`hablar:`, `examinar:`, `vencer:`, `llegar:`).
- **Diálogos por ramas** (`DialogueData`) y **personajes** (`NpcData`).
- **Misiones** (`QuestData`, `QuestService`): avanzan por pasos con esos avisos y dan las recompensas una sola vez.
- **Catálogo de misiones**: `Resources/Narrativa/CatalogoMisiones`.
- **Objetivo de la misión activa en el HUD.**
- **`SaveService`**: guarda en JSON con versión y copia de respaldo. **Todavía no se llama en el juego.**
- **Guardián**: `GuardianData`, con el asset `GUA_jaguar`.

### Arco olmeca jugable en `Olmeca_Ciudad`
1. **`MP_OLM_01`, "El despertar"**: el jugador aparece en la orilla noreste del río, se asoma al agua (recuerdos y reflejo del jaguar), escucha a la figura espiritual (juramento) y busca al artesano.
2. **`MP_OLM_02`, "El asentamiento inquieto"**: artesano de jade → anciana → joven guerrero. Al terminar, las estelas lisas cambian su texto, como pista del borrado.
3. **Habitantes**: figura espiritual, artesano, anciana, guerrero y comerciante, con sprites provisionales (misma silueta, colores distintos).
4. **Datos editables** en `Data/Narrative/Olmeca/`. El generador los crea **solo si faltan**, así que se editan en el Inspector. Para volver al texto del código, se borra el asset.

### Pruebas
EditMode 55/55 y PlayMode 32/32 (al 2026-10-29).

## 4. Pendiente de decidir (por el autor)

1. **¿Boot arranca en `Olmeca_Ciudad`** en lugar del prototipo? (Recomendado: sí. Es un cambio pequeño en `Boot.unity`.)
2. **Afinidad inicial del Jaguar.** Propuesta: oscuridad; el GDD sugería oscuridad o tierra.
3. **Si el epiclásico y lo tolteca entran en el arco III**, y qué afinidades corresponden a los arcos III a V.
4. **Si la figura espiritual desaparece** después del juramento.

## 5. Pendiente del autor (arte)

- Sprites del **Jaguar** jugable. Hoy el jugador es un humano provisional, aunque el reflejo diga "jaguar".
- Sprites de los **NPC**, con siluetas propias, y de la **figura espiritual**.
- Cabeza colosal, enemigos animados y **Guardián de la Piedra Viva**.

## 6. Plan propuesto (cada etapa se presenta y espera autorización)

### Inmediato
- **Probar el arco en el Editor** (abrir `Olmeca_Ciudad`) y anotar lo que se sienta mal: textos, posiciones, ritmo.
- Según la decisión 1, hacer que Boot arranque en la ciudad.

### Fase narrativa 2, continuación: selva y mazmorra (corresponde a las fases 5 y 6 del GDD)

| Etapa | Contenido |
|---|---|
| **M1. Rastros en la selva** (`MP_OLM_03`) | Zona de selva al oeste de la ciudad, con espíritus jaguar corrompidos (`ReportOnDefeat`), fragmentos de memoria menores y la entrada a la estructura (`ReportOnEnter`). Señales visuales de corrupción. |
| **M2. Base de mazmorra** | Escena por piso; transición entre pisos; santuario de **Memoria** (punto de control y **primer uso real de `SaveService`**) y de **Vida** (curación limitada); derrota → volver al último santuario sin perder el progreso principal. |
| **M3. Pisos 1 y 2** | El Umbral (tutorial de combate) y El Pantano (terreno que ralentiza, eco borrado). Mini-jefe **Jaguar de Piedra**. |
| **M4. Piso 3 y jefe** | Arena del **Guardián de la Piedra Viva** en tres fases (`Docs/Enemigos/Olmeca.md`). |
| **M5. Recompensas y cierre** | Fragmento de la Primera Memoria, habilidad de tierra (propuesta: golpe que rompe basalto agrietado), elección de Bendición, `MP_OLM_06` (restaurar una inscripción) y conexión al arco II. |

### Otros pendientes técnicos
- Menú de inicio con **Continuar o Nueva partida**, para usar `SaveService`.
- Pantalla de elección de Guardián, cuando haya más de uno.
- Misiones secundarias del arco I (`MS_OLM_01` a `MS_OLM_05`, en `Docs/Historia/Misiones.md`) y la tienda del comerciante.
- Arte de las ruinas que falta: escombros de basalto y mosaico de serpentina (`Arte_Olmeca_Demo.md` §12).
- **Medir en Android**, en el i7, y hacer el **benchmark del i5** (`Docs/Rendimiento.md`).
