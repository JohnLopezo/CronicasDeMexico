# Bendiciones (M5)

Las Bendiciones son dones de los Guardianes que el jugador libera. Son ficción del juego (**F**) y no representan
ninguna deidad ni creencia histórica concreta (`Docs/Historia_Mundo.md`). Assets: `Data/Rewards/Olmeca/BEN_*.asset`,
catálogo `Resources/Recompensas/CatalogoBendiciones`.

## Ranuras

- **Principal (1).** La ocupa la Bendición obligatoria del jefe de cada cultura. No se pierde, no se sustituye y no se
  puede poner en la ranura secundaria.
- **Secundaria (1).** Se elige al completar una mazmorra, entre tres, y se puede cambiar en el menú (pestaña
  Bendiciones) entre las desbloqueadas.
- **Futuro:** cada cultura añade su principal y sus secundarias. Con el tiempo, las combinaciones de principal y
  secundaria definen el estilo de cada partida. Una sola ranura principal es la propuesta para el vertical slice;
  cuántas habrá cuando haya varias culturas queda abierto.

## Cultura olmeca

### Bendición de la Tierra — principal (obligatoria)

*«El Guardián de la Piedra Viva te presta su peso: la tierra responde cuando la golpeas.»*

| | |
|---|---|
| **Se obtiene** | Al liberar al Guardián de la Piedra Viva |
| **Combate** | Golpe de tierra (tecla **C**, botón Este): daño 2 × ataque en un círculo de 2,5 m |
| **Exploración** | Rompe el **basalto agrietado**, que tapa una alcoba con una ofrenda oculta en cada piso |
| **Coste** | 30 de energía, sin regeneración (decisión 2); 4 s de enfriamiento |
| **Bonificación pasiva** | Ninguna |

### Secundarias (se elige una al abrir la ofrenda final)

| Id | Nombre | Bonificación | Estilo |
|---|---|---|---|
| `fuerza_tierra` | Fuerza de la Tierra | Defensa +4, vida +10 | Aguantar: menos daño de cada golpe |
| `instinto_jaguar` | Instinto del Jaguar | Ataque +4 | Atacar: desde el nivel 2, los espíritus caen de 2 zarpazos en vez de 3 |
| `memoria_agua` | Memoria del Agua | Energía +30, vida +10 | Habilidades: un golpe de tierra más antes de descansar |

- **Equilibrio.** Ninguna es obligatoria para avanzar. Instinto acorta los combates, Fuerza los hace más seguros y
  Agua da más habilidades. La diferencia es de estilo, no de poder.
- **Inspiración (I).** La relación del jaguar con la tierra y el agua en la iconografía olmeca es un tema de debate
  académico. Aquí solo inspira nombres; no es una afirmación sobre creencias olmecas.

## Pantalla de elección

Se abre sola al abrir la ofrenda final si aún no hay secundaria. Muestra las tres opciones, cada una con su
descripción y su efecto. Al elegir, se desbloquea y se equipa, y el jugador pasa a la pantalla de resultados. Si ya
hay secundaria (por ejemplo, al repetir la mazmorra en el futuro), se pasa directo a resultados.
