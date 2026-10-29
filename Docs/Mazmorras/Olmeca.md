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

## Derrota

Regreso al último santuario de Memoria con una penalización moderada configurable, **sin perder el progreso principal** (§8.4; GDD §9). Qué persiste dentro de una incursión se decide antes de programarlo.
