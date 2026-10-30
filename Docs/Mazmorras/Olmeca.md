# Mazmorra olmeca: «La ciudad bajo la selva» (nombre provisional)

Fuente: `Docs/Historia_Mundo.md` §6 (capítulo I), §7, §8; GDD `Docs/Diseno.md` §7, §7.1, §9, §11, §17. Enemigos en `Docs/Enemigos/Olmeca.md`.

## Alcance

- **Vertical slice: 3 pisos**, para validar la base (GDD §17).
- **Meta: 8 pisos**, dentro del rango de 5–8 de la biblia.
- Hecha a mano; la generación procedural solo se plantea cuando la base funcione (GDD §7).
- Cada piso tiene objetivo, mecánica, peligro, recompensa y conexión narrativa. No son salas idénticas con enemigos al azar (§7.1).
- La vida **no** se recupera al cambiar de piso ni al limpiar salas (§8; GDD §9).

## Vertical slice (3 pisos)

| Piso | Nombre | Contenido | Narrativa |
|---|---|---|---|
| 1 | El Umbral | Entrada selvática, espíritus jaguar y pecaríes, aprendizaje del combate, primer santuario de Memoria (checkpoint) | El joven guerrero llegó hasta aquí; rastros de su expedición |
| 2 | El Pantano | Terreno que ralentiza, eco borrado, santuario de Vida con curación limitada, **mini-jefe Jaguar de Piedra** | Primeras estelas a medio borrar dentro de la estructura |
| 3 | Santuario final | Antesala sin enemigos y arena del **Guardián de la Piedra Viva** | La Primera Memoria; el guardián se libera |

## Meta (8 pisos)

| Piso | Nombre (GDD §7.1) | Función |
|---|---|---|
| 1 | El Umbral | Tutorial de combate y exploración |
| 2 | El Pantano | Terreno húmedo y zonas que ralentizan |
| 3 | Las Piedras del Jaguar | Acertijos de activación e indicios narrativos |
| 4 | El Guardián | Mini-jefe **Jaguar de Piedra** |
| 5 | El Templo Hundido | Mecanismos que cambian el nivel del agua |
| 6 | La Cámara Ritual | Memoria y santuario de descanso |
| 7 | El Corazón de Piedra | Enemigos élite y mini-jefe **Centinela de basalto** |
| 8 | Santuario Final | Fragmento de la Primera Memoria y jefe final |

La biblia pide mini-jefes "cada dos o tres pisos" sin forzar el ritmo: 4 y 7 cumplen.

## Recompensas del jefe

1. **Fragmento de la Primera Memoria:** restaura inscripciones (misión `MP_OLM_06`) y abre la conexión al arco II.
2. **Habilidad de tierra** (fija), útil en la mazmorra siguiente. Candidatas de Historia_Mundo §5.2: embestida, barrera de piedra, golpe de área. **Propuesta:** un golpe de tierra que también rompa obstáculos de basalto agrietado o levante bloques, para que el arco II pueda exigirlo.
3. **Acceso narrativo:** camino a la siguiente memoria cultural.
4. **Elección de una Bendición entre tres** (GDD §11): *Fuerza de la Tierra* (defensa), *Instinto del Jaguar* (daño cercano y movilidad), *Memoria del Agua* (recuperación limitada). Números pendientes de balance.

La recompensa es consecuencia de entender qué protegía el guardián (§7.4): el combate termina con su liberación y un recuerdo, no solo con su derrota.

## Santuarios

Tipos del GDD §9: Vida (curación parcial, limitada por incursión), Memoria (checkpoint y guardado), Espiritual (Bendiciones), Sabiduría (Códice) y Artesano (mejoras). Identidad visual olmeca: por ejemplo, un fogón bajo una enramada o un nicho de basalto (I).

**Implementado en M2 (2026-10-30):**

- **Memoria** (`MemoryShrine`): un nicho de basalto con una hacha votiva de jade. Hay uno en el piso 1, junto a la escalera de llegada, y otro en la antesala del piso 3.
  - Al descansar fija el punto de control (`PlayerCheckpoint`: escena, punto de aparición y vida actual) y guarda la partida (`SaveService`).
  - No cura. La primera vez, el jade se enciende (bandera `santuario_memoria.<id>`).
- **Vida** (`LifeShrine`): un fogón de tres piedras en la sala lateral del piso 2.
  - Cura la mitad de la vida máxima, redondeada hacia arriba, **una vez por incursión**. Con la vida llena no se gasta.
  - El uso queda en la bandera `incursion.santuario_vida.<id>`, que apaga el fuego. Las banderas `incursion.*` se borran al volver a la selva (`IncursionReset`): salir de la mazmorra termina la incursión.

## Derrota

Regreso al último santuario de Memoria con una penalización moderada configurable, **sin perder el progreso principal** (§8.4; GDD §9).

**Decidido e implementado en M2 (2026-10-30), en `DefeatReturn`:**

- **Con punto de control**, al caer se vuelve al último santuario de Memoria, aunque esté en otro piso, con la vida completa.
  - La penalización es configurable (`returnHealthFraction`) y por ahora vale 0.
  - El viaje recarga el piso. Los enemigos normales vuelven, mientras que la historia, las misiones y las banderas (por ejemplo, los jefes vencidos) se conservan.
  - No se recarga la partida guardada.
- **Sin punto de control** se reaparece en el sitio, como fuera de la mazmorra.
- Los enemigos de la mazmorra no reaparecen mientras se está en el piso (`EnemyChaser.respawns = false`). Vuelven al cargarlo de nuevo.

## Base implementada (M2, 2026-10-30)

- **Escenas** `Olmeca_Mazmorra_P1` a `P3`, generadas por `OlmecaDungeonGenerator` desde `Mazmorra_P<n>.json` (`planificar_piso`): salas de losas sobre el vacío, unidas por pasillos.
  - Cada piso tiene su escalera de subida (en el piso 1, a la selva) y su escalera de bajada (salvo el 3).
  - La abertura de la entrada de la selva lleva al piso 1 solo cuando `MP_OLM_03` está completada.
- **El piso 3 (M4):**
  - La antesala sin enemigos, con el santuario de Memoria, es el punto de control antes del jefe.
  - La losa sellada (`SealedPassage`) se abre al examinarla (`olm.sello_abierto`).
  - Detrás está la arena circular del Guardián de la Piedra Viva (`Docs/Enemigos/Olmeca.md`), con cuatro columnas en pie contra las que choca su embestida, escombros, raíces y un borde que se inunda en la fase 2.
  - Al caer en el combate se vuelve a la antesala y el Guardián se reinicia completo.

## Pisos 1 y 2 (M3, 2026-10-30)

- **El Umbral (piso 1), tutorial en salas seguidas:**
  1. Llegada con el santuario de Memoria.
  2. Un espíritu solo.
  3. Dos espíritus juntos, junto a la estela con los rastros de la expedición del guerrero.
  4. La bajada.
  - Las pistas (`TutorialHint`) se muestran al entrar en cada zona, una sola vez en la partida (bandera `tutorial.<id>`), y se quitan al salir o con Interactuar. Explican el movimiento, el santuario, el ataque y la esquiva, cómo pelear contra varios y que la vida no se recupera.
- **El Pantano (piso 2):**
  - Una sala inundada con charcos de **agua baja** que frenan a la mitad a quien los pisa, salvo durante la esquiva (`SlowZone`). Tiene dos **ecos borrados**, un espíritu y dos estelas borradas.
  - Una sala lateral con el santuario de Vida.
  - La **arena del Jaguar de Piedra** (`Docs/Enemigos/Olmeca.md`). La bajada al piso 3 está en la arena, tapada por escombros hasta vencerlo.
- **Combate nuevo:**
  - **Esquiva** (Espacio o el botón Sur, la antigua acción Jump): 0,18 s a 10 m/s, 0,3 s invulnerable y 0,6 s de espera entre esquivas.
  - **Avisos en el suelo** (`TelegraphMarker`): parpadean cada vez más rápido.
  - **Proyectiles reutilizados** (`Projectile`).
  - **Barra del jefe** en el HUD.
- **Misión `MP_OLM_04`:** detalle en `Docs/Historia/Misiones.md`.
- **Continuar:** al arrancar desde Boot con una partida guardada, el juego aparece en su santuario de Memoria (`GameBootstrapper.ContinueSavedGame`). Hasta que exista el menú de inicio, la partida se borra con **Crónicas → Borrar partida guardada**.
