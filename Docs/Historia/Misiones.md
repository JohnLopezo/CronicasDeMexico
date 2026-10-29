# Misiones

Misiones principales y secundarias. Solo el **arco I (olmeca)** está desarrollado; los demás arcos se añadirán al investigar sus culturas. Fuente: `Docs/Historia_Mundo.md` §6 (capítulo I), §8, §15; GDD `Docs/Diseno.md` §12, §17.

**Criterios (Historia_Mundo §15):** cada misión tiene consecuencias reconocibles (diálogos nuevos, objetos, mejoras, caminos o cambios en el pueblo); no todas giran alrededor de la Sombra; ninguna se reduce a "entregar objetos". Lo histórico se marca **D** (documentado), **I** (interpretación) o **F** (ficción del juego).

## Arco I — Misiones principales

| Id | Nombre (provisional) | Objetivo | Consecuencia |
|---|---|---|---|
| `MP_OLM_01` | El despertar | Explorar la orilla, ver el reflejo, seguir a la figura espiritual, elegir el Guardián y aceptar el juramento | Se desbloquea el camino al asentamiento; tutorial de movimiento e interacción |
| `MP_OLM_02` | El asentamiento inquieto | Hablar con el artesano, la anciana y el joven guerrero herido | Se conoce el problema desde tres puntos de vista; se abren las secundarias del pueblo |
| `MP_OLM_03` | Rastros en la selva | Seguir los rastros que describe el guerrero, enfrentar criaturas corrompidas y recuperar los primeros fragmentos de memoria menores | Se descubre la entrada a la antigua estructura; el guerrero reconoce el lugar |
| `MP_OLM_04` | La ciudad bajo la selva | Atravesar la mazmorra (`Docs/Mazmorras/Olmeca.md`) | Santuarios de descanso desbloqueados; mini-jefes vencidos |
| `MP_OLM_05` | La Piedra Viva | Vencer y liberar al Guardián de la Piedra Viva | Fragmento de la Primera Memoria, habilidad de tierra, elección de Bendición |
| `MP_OLM_06` | Lo que la piedra recuerda | Volver al asentamiento y usar el fragmento para restaurar una inscripción | Cambian los diálogos del pueblo; se revela que el borrado es deliberado; se abre la conexión al arco II |

## Arco I — Misiones secundarias (propuestas)

| Id | Nombre (provisional) | Quién la da | Resumen | Consecuencia | Base histórica |
|---|---|---|---|---|---|
| `MS_OLM_01` | Piedra verde | Artesano de jade | Recuperar piedra verde (jade o serpentina) perdida por un comerciante en un camino ahora peligroso | El artesano ofrece una mejora o un objeto; diálogo sobre el comercio a larga distancia | D: en La Venta se trabajó jade y serpentina traídos de lejos |
| `MS_OLM_02` | El relato que nadie repite | Anciana | Encontrar en la selva y en las ruinas los fragmentos de un relato que ella ya no recuerda completo | La anciana vuelve a contarlo; entrada nueva en el Códice | F (relato del juego) |
| `MS_OLM_03` | El humedal en silencio | Pescador o canoero | Investigar por qué los animales abandonaron una zona del pantano | Vuelve la fauna; se abre un paso en canoa o un atajo | I/F; flora y fauna según `Docs/Culturas/Olmeca.md` |
| `MS_OLM_04` | Las marcas borradas | Inscripciones (narrativa ambiental) | Examinar las estelas y el altar-trono del centro ceremonial, cuyos relieves se han perdido | Tras el fragmento, alguna se restaura y cuenta algo nuevo | D: altares-trono y estelas de La Venta; I/F: su borrado |
| `MS_OLM_05` | La herida del guerrero | Joven guerrero | Conseguir plantas para la sanadora; él cuenta qué vio en la expedición | El guerrero acompaña o vigila un tramo; pista para `MP_OLM_03` | I: plantas medicinales por investigar antes de nombrarlas |

## Recursos que ya existen en el proyecto

- **Inscripciones examinables** en la ciudad (`OlmecaCityGenerator`): el altar-trono con el **nicho vacío** y las estelas lisas "borradas por la lluvia y el tiempo". Encajan con el tema del borrado: tras `MP_OLM_06` pueden cambiar su texto.
- **Artesano de jade** con diálogo y patrulla (prototipo `OlmecaPrototypeGenerator`) y **espíritus jaguar** perseguidores (`EnemyChaser`).
- La aldea con enramadas, vasijas, muelle y canoas sirve como escenario de `MS_OLM_03`.

## Implementación

**Sistemas listos (2026-10-29, etapas N1–N3):** `NpcData`, `DialogueData` (ramas por banderas, que al terminar activan banderas, empiezan una misión y avisan `hablar:id`), `QuestData` y `QuestService` (pasos por avisos `hablar:`, `examinar:`, `vencer:`, `llegar:`, con cuenta; banderas `mision.ID.activa` y `.completada`; recompensas una sola vez), objetivo en el HUD y `SaveService`. Resumen técnico en `CLAUDE.md` (Arquitectura).

**`MP_OLM_02` jugable (2026-10-29, N4)** en `Olmeca_Ciudad`:

1. El **artesano** la empieza (rama sin misión) y dice que las piedras pierden sus marcas.
2. Paso 1, `hablar:anciana`: su rama de misión activa `olm.anciana_conto`.
3. Paso 2, `hablar:guerrero_joven`: su rama de misión solo aparece con `olm.anciana_conto`; antes, habla de lo suyo y no cuenta.
4. Recompensa: `olm.entrada_descubierta`. Las estelas lisas cambian su texto al completarla (pista de que el borrado no es natural).
5. El comerciante tiene diálogo de ambiente; su tienda y `MS_OLM_01` quedan para después.

Assets en `Data/Narrative/Olmeca/` y catálogo en `Resources/Narrativa/CatalogoMisiones` (los crea `OlmecaNarrativeAssets` si faltan). Altares y estelas avisan `examinar:altar_trono` y `examinar:estela`.

**`MP_OLM_01` jugable (2026-10-29, N5)** en `Olmeca_Ciudad`:

1. Empieza sola al cargar la escena (`StartQuestOnLoad`). El jugador aparece en la orilla noreste del río, junto a la figura espiritual.
2. Paso 1, `examinar:reflejo`: al asomarse al agua llegan los recuerdos rotos (una voz, una luz, piedras que se derrumban, la caída) y el reflejo del jaguar.
3. Paso 2, `hablar:figura_espiritual`: explica el Mundo de las Memorias y pide el juramento; activa `olm.juramento`.
4. Paso 3, `hablar:artesano_jade`: con el juramento, el artesano empieza `MP_OLM_02`; sin él, solo se sorprende de ver un jaguar tranquilo. Recompensa: `olm.despertar_completo`.

**Falta:** `MP_OLM_03` en adelante necesitan la selva y la mazmorra.
