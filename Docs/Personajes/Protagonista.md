# Protagonista: el Guardián

Fuente: `Docs/Historia_Mundo.md` §2; GDD `Docs/Diseno.md` §4. "El protagonista" y "el Guardián" nombran el papel; **cada Guardián tiene nombre y género propios** (decisión del autor, 2026-11-06).

## Vida anterior (§2.1)

Una persona del mundo moderno, con curiosidad por la naturaleza, las culturas antiguas y las historias ocultas en los paisajes. Muere en un accidente durante una expedición relacionada con una zona arqueológica mexicana.

- **Lo que sabe el jugador al principio:** solo recuerdos fragmentados — una voz, una luz, el sonido de piedras que se derrumban, la sensación de caer.
- **Cómo se usan:** aparecen en momentos importantes (por ejemplo, al recibir un fragmento). Los detalles de la expedición se revelan poco a poco.
- **Tono:** la muerte no se muestra como espectáculo; explica el comienzo de la segunda existencia.
- **Diferencia con el GDD:** el GDD hablaba de "un joven" y de "un objeto antiguo". Se adopta la versión de la biblia. El objeto antiguo puede seguir siendo un detalle de la expedición si más adelante sirve a la historia (pendiente).
- **Nombre:** el GDD §4 permite que el jugador defina un nombre. Se mantiene como opción; la historia no depende de él.

## Despertar y juramento (§2.2–2.4)

Despierta en la selva sin voz, descubre en el reflejo un rostro que no es el suyo y conoce a la figura espiritual. Ha sido elegido no para gobernar, sino porque puede ver el mundo desde fuera de sus épocas. Acepta ayudar porque los seres que encuentra lo necesitan, no porque lo entienda todo.

**Arco personal:** de los recuerdos rotos a aceptar que no necesita resolver su propia muerte para saber por qué sigue el viaje (epílogo, §14).

## Guardianes elegibles

La elección define apariencia, atributos, animaciones y estilo de combate; **no bloquea la historia** (§2.3, regla 13). Cada Guardián tiene una afinidad inicial y puede desarrollar las seis.

| Guardián | Estilo (biblia) | Rol y afinidad propuestos (GDD) | Contexto biológico (verificar antes de afirmarlo en el juego) |
|---|---|---|---|
| Jaguar | Fuerza, movilidad, combate cercano | Daño cercano; oscuridad/tierra | *Panthera onca*, nativo de América; presente en la región olmeca |
| Ajolote | Recuperación, resistencia, afinidad acuática | Control/soporte; agua; curación limitada | *Ambystoma mexicanum*, endémico de los lagos del Valle de México |
| Quetzal | Agilidad, distancia, movilidad aérea mediante habilidades | Distancia/apoyo; viento/luz | Quetzal resplandeciente, nativo del sureste de México y Centroamérica |
| Mariposa monarca | Velocidad, evasión, poderes de luz | **Nuevo en la biblia:** luz | Especie migratoria; inverna en bosques de México. No es endémica |
| Águila real | Percepción, precisión, ataques a distancia | Movilidad/distancia; viento | Nativa de gran parte del hemisferio norte; emblema nacional |
| Xoloitzcuintle | Resistencia espiritual, rastreo, protección | Protección/espiritual; por definir | Perro doméstico de linaje antiguo en México |
| Lobo mexicano | Persecución, velocidad, combate sostenido | Equilibrado; viento/tierra | Subespecie *Canis lupus baileyi*; nativo del norte de México y suroeste de EE. UU. |
| Vaquita marina | — (no está en la biblia) | Control acuático; agua | Endémica del Alto Golfo de California. **Se conserva como Guardián posible a futuro**; su movilidad en tierra está por resolver |

El juego distinguirá **endémico**, **nativo** y **emblemático** (§2.3). La tabla anterior es orientativa: comprobar distribución y estado de conservación con fuentes antes de usarlo en textos del juego.

**Afinidades adoptadas (decisión del autor, 2026-10-30, a partir de `Docs/Personages.png`).** Cada Guardián tiene dos
afinidades; la primera es la inicial.

| Guardián | Afinidades |
|---|---|
| Jaguar | Tierra / Oscuridad |
| Ajolote | Agua / Luz |
| Quetzal | Viento / Luz |
| Águila real | Luz / Viento |
| Xoloitzcuintle | Oscuridad / Luz |
| Lobo mexicano | Viento / Tierra |

La Mariposa monarca (luz, en la biblia) y la Vaquita marina (agua, a futuro) no aparecen en la imagen; sus afinidades
siguen abiertas. Estas afinidades sustituyen las propuestas del GDD de la tabla anterior. **2026-11-06:** mandan las fichas, así que Naira empieza en viento e Itzel en luz; el águila deja el fuego.

**Diseño visual del Jaguar:** ficha en `Docs/Personajes/Fichas/Jaguar.md`.

## Alcance inicial

Primero se implementó **un** Guardián, el Jaguar (2026-10-29). Desde el 2026-11-06 se eligen siete guerreros en Nueva partida, con las fichas de `Docs/Guerreros` y el mismo juego para todos: atributos propios, pero sin estilos de combate distintos todavía. El espíritu jaguar enemigo no cambia.

**Datos:** siete `GUA_` (`GuardianData`) en `Resources/Personajes/CatalogoGuardianes`; Balam conserva el id `jaguar`. `SaveData.guardianId` guarda el elegido. Tablas de afinidad arriba y de atributos en `Docs/Sistemas/Sistema_Progresion.md`.

**Decisión del autor (2026-11-06):** son guerreros humanos, con nombre y género, ligados al espíritu de su animal; sustituye a la forma animal original de la biblia.

## Reglas (Historia_Mundo §16)

- El protagonista es siempre el Guardián elegido, ligado al espíritu de su animal (regla 1). El rostro de su vida anterior solo asoma en recuerdos y en el epílogo.
- Ninguna especie es indispensable para completar la historia (regla 13).
- La historia debe poder completarse con todas las especies jugables (validación, §17).
