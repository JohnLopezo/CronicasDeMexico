# Sistema de progresión (M5)

Nivel, experiencia, estadísticas del animal, energía y Bendiciones. Implementación: `Core/Progress/Progression.cs`,
configuración en `Resources/Recompensas/ConfigProgresion` (`Data/Rewards/ProgressionConfig`) y perfil de estadísticas
en el `GuardianData` del Guardián (`Data/Characters/GUA_jaguar`). `Gameplay/Player/PlayerStats` aplica las
estadísticas a la `Health` y al `PlayerAttack` del jugador.

## 1. Escala ×10 (decisión 1)

Para que los niveles, las reliquias y las Bendiciones puedan sumar poco a poco, todos los números se multiplicaron
por 10. La proporción entre golpes y vida es la misma: un espíritu sigue cayendo de 3 zarpazos y el jugador aguanta
los mismos golpes que antes.

| Valor | Antes (M4) | Ahora (M5) | Dónde |
|---|---|---|---|
| Vida del jugador (nivel 1) | 5 | 50 | `GuardianData.baseStats`, `Jugador.prefab` |
| Daño del jugador (nivel 1) | 1 | 10 | `GuardianData.baseStats`, `PlayerAttack` |
| Espíritu corrompido: vida / contacto | 3 / 1 | 30 / 10 | `OlmecaJungleGenerator`, `EnemyChaser` |
| Eco borrado: vida / fragmento | 2 / 1 | 20 / 10 | `OlmecaDungeonGenerator`, `ErasedEcho` |
| Jaguar de Piedra: vida | 16 | 160 | `OlmecaDungeonGenerator` |
| Jaguar de Piedra: zarpazo / salto / rugido | 2 / 2 / 1 | 20 / 20 / 10 | `StoneJaguar` |
| Guardián de la Piedra Viva: vida | 40 | 400 | `OlmecaDungeonGenerator` |
| Guardián: golpe fuerte / ligero | 2 / 1 | 20 / 10 | `StoneGuardian` |
| Espíritu jaguar del prototipo: vida / contacto | 3 / 1 | 30 / 10 | `EspirituJaguar.prefab` |
| Santuario de Vida | 50 % de la vida | 50 % de la vida | Sin cambio: es proporcional |
| Semilla medicinal preparada | — | +20 de vida (40 % a nivel 1) | Nuevo |
| Defensa | — | Resta a cada golpe, mínimo 1 de daño | `Health.Defense` |

- **Comprobación.** Las pruebas PlayMode de combate (`CombatTests`, `OlmecaMazmorraCombatTests`,
  `OlmecaGuardianTests`) pasan con los valores nuevos. Tres se ajustaron porque usaban los números absolutos de antes
  (daño 1, Jaguar de 16 de vida) o no contaban la defensa.
- **Escenas sin regenerar.** El prototipo (`Olmeca_Prototipo`) conserva los valores serializados de antes hasta que
  se regenere. Las escenas olmecas (ciudad, selva y pisos) ya están regeneradas.

## 2. Estadísticas

`CharacterStats { maxHealth, attack, defense, maxEnergy }`. Las del nivel salen del perfil del Guardián:

```
nivel N = base + crecimiento × (N − 1)      (con redondeo hacia abajo)
Jaguar:   base (50, 10, 0, 100)    crecimiento (6, 1,5, 0,5, 5) por nivel
```

| Nivel | Vida | Ataque | Defensa | Energía |
|---|---|---|---|---|
| 1 | 50 | 10 | 0 | 100 |
| 2 | 56 | 11 | 0 | 105 |
| 3 | 62 | 13 | 1 | 110 |
| 4 | 68 | 14 | 1 | 115 |
| 5 | 74 | 16 | 2 | 120 |

Sobre el perfil se suman:

- **Las reliquias equipadas** (2 ranuras). Las que tienen `guardianOnly` solo suman con su Guardián.
- **Las Bendiciones principal y secundaria.**

Al subir de nivel, la vida máxima sube y la actual sube lo mismo, así que no se cura de golpe. Si una reliquia
quitada baja el máximo, la vida y la energía se recortan al nuevo máximo.

## 3. Experiencia y niveles

- **Experiencia para subir:** `100 × nivel^1,5` (100, 283, 520, 800, 1118…). El nivel máximo es 20.
- **Sobre el nivel de la zona y contra el farmeo:** las reglas están en `Sistema_Recompensas.md` §5. Los valores se
  configuran en `ConfigProgresion`:

  | Campo | Valor |
  |---|---|
  | `overLevelPenalty` | 0,25 |
  | `overLevelFloor` | 0,10 |
  | `repeatFactor` | 0,5 |
  | `repeatCutoff` | 0,2 |

- **Ritmo previsto** en una primera partida que explora todo:
  - **Selva:** 3 espíritus (45), 3 fragmentos (60) y las misiones 02 y 03 (100). Se llega al nivel 2.
  - **Mazmorra:** pisos (90), enemigos (unos 300), Jaguar (150), ofrendas (unas 170), misión 04 (100) y jefe (400).
    Se termina hacia el nivel 4–5.
  - **Regreso:** la nota (0–100) y la misión 06 (120).

## 4. Energía (decisión 2)

- **Sin regeneración automática.** La energía máxima es 100 a nivel 1 y la gastan las habilidades: el golpe de
  tierra cuesta 30.
- **Se recupera:**
  - por completo, al descansar en un santuario de Memoria (`MemoryShrine` → `FillEnergy`);
  - +40 con la esencia de energía (consumible);
  - con recompensas que la incluyan.
- **Al cargar:** una partida antigua o nueva empieza con la energía llena (`energy = −1` en el guardado).

## 5. Bendiciones (decisión 3)

- **Dos ranuras.**
  - **Principal:** fija. La ocupa la Bendición del jefe de la cultura y ya no se pierde ni se cambia
    (`EquipSecondary` la rechaza).
  - **Secundaria:** se elige y se puede cambiar en el menú (pestaña Bendiciones) entre las desbloqueadas.
- **Mazmorra olmeca:**
  - El Guardián da siempre la **Bendición de la Tierra** (principal), que es el golpe de tierra.
  - Al abrir la ofrenda final se elige **una secundaria entre tres**: Fuerza de la Tierra, Instinto del Jaguar o
    Memoria del Agua. Los detalles están en `Docs/Contenido/Bendiciones.md`.
- **Golpe de tierra** (`PlayerAbility`, tecla **C** / botón Este):
  - daño en un área de 2,5 m igual a 2 × ataque;
  - rompe el basalto agrietado (`BreakableBasalt`);
  - cuesta 30 de energía y tiene 4 s de enfriamiento;
  - muestra un círculo ámbar en el suelo.
- **Culturas futuras:** cada una añade su principal y sus secundarias, y las combinaciones crecen.

## 6. Caer (decisión 7)

La penalización es 0: no se pierde nada. Caer solo marca `incursion.caido` (para la nota) y devuelve al último
santuario, como en M2–M4. Si algún día se quiere un castigo, se activa desde `DefeatReturn`, sin tocar el resto.
