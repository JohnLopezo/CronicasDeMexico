# Acciones de senda y cocina (E7)

Inspirado en las acciones de senda de Octopath Traveler II y en la cocina de Eastward.

## Acciones de senda

- **Cómo se usa:** cerca de un habitante con algo para tu Guardián, el aviso dice «Hablar · mantén: Indagar».
  - Tocar Hablar conversa como siempre.
  - **Mantener pulsado 0,5 s** lanza la acción de senda.
  - Es el mismo botón en PC y en el teléfono, así que no cambia la distribución de los controles táctiles.
- **Cuándo se espera a soltar:** solo con habitantes que tienen una acción disponible. Con el resto de cosas,
  Interactuar responde al pulsar, como antes.
- **El verbo depende del Guardián** (`GuardianData.PathAction`, según el estilo de su ficha):

| Verbo | Guardianes | Idea |
|---|---|---|
| Indagar | Itzel (águila), Ocel (xoloitzcuintle) | Percepción y rastreo: notar lo que el habitante calla. |
| Persuadir | Balam (jaguar), Naira (lobo) | Fuerza y presencia: que confíe en ti y te dé o enseñe algo. |
| Escuchar | Xali (ajolote), Ameyali (mariposa), Yaret (quetzal) | Cuidado y calma: dejar que hable. |

- **Qué da:** unas líneas propias y, a veces, objetos, experiencia o banderas (recetas, ofertas).
  - Se da **una sola vez por partida** (`senda.<npc>.<verbo>`), al terminar las líneas. Alejarse a medias no cuenta.
  - Cambiar de Guardián en el menú permite probar otro verbo con el mismo habitante.
- **Datos:** `Resources/Narrativa/AccionesSenda` (`PathActionCatalog`), que se edita en el Inspector. Lo crea
  `PathActionAssets` solo si falta.
- **Código:** `DialogueInteractable` implementa `IPathActionTarget`; `PlayerInteractor` detecta la pulsación larga.

### Respuestas: borradores de Claude, pendientes del autor

No añaden historia a la biblia: solo repiten lo que ya se ve en el juego (las estelas que callan, el olvido y las
incursiones del oeste).

| Habitante | Verbo | Resumen | Da |
|---|---|---|---|
| Artesano de jade | Indagar | Ya no termina sus piezas: olvida qué figura buscaba. | Jade ×1, 10 XP |
| Artesano de jade | Persuadir | Te enseña a atar un amuleto. | Receta del amuleto de jade |
| Anciana | Escuchar | «El río se acuerda de todo; le tengo miedo al silencio». | 20 XP |
| Curandera | Escuchar | El cacao «devuelve las ganas». | **Receta de la bebida de cacao** |
| Curandera | Indagar | Guarda hojas: llega gente con heridas que no recuerda. | Semillas medicinales ×3 |
| Pescador | Persuadir | Sacas tú la red. | Pescado ×3 |
| Pescador | Escuchar | El río «olvidó su camino». | Pescado ×1, 10 XP |
| Comerciante | Persuadir | No le vende cacao a cualquiera. | **Empieza a vender cacao** |
| Comerciante | Indagar | Rumor de ofrendas tras la piedra agrietada. | 15 XP |
| Guardia | Indagar | Las criaturas llegan por el oeste «como si quisieran borrar a la gente». | 10 XP |
| Joven guerrero | Persuadir | «No golpees tres veces si no sabes cómo salir». | Semilla preparada ×1, 10 XP |

Cada verbo tiene al menos 3 respuestas, así que todos los Guardianes tienen algo que hacer en la ciudad.

## Cocina de la curandera

- **Dónde:** el **fogón** junto a la curandera (`Fogon_Curandera`, «Cocinar») abre la pestaña **Cocinar** del menú.
- **Recetas:** son `RecipeData` con `station = Cocina`; las del artesano siguen en Fabricar.
- **Qué hace:** los platillos combinan curación, energía y defensa temporal. Las semillas solo curan, por eso no
  repiten lo que hace el artesano.

| Platillo | Ingredientes | Efecto | Se desbloquea |
|---|---|---|---|
| Bebida de maíz | Maíz ×2 | +10 vida, +25 energía | Desde el principio |
| Pescado envuelto en hoja | Pescado ×1, fibras ×1 | +25 vida, defensa +3 durante 30 s | Desde el principio |
| Bebida de cacao | Cacao ×1, maíz ×1 | +40 energía, defensa +5 durante 45 s | Escuchando a la curandera |

**Ingredientes:**
- El comerciante vende maíz (3 por 4 ecos) y pescado (5 ecos).
- Vende cacao (12 ecos) cuando lo persuades.
- El pescador da pescado con su acción de senda.

**Aviso cultural (separar D, I y F):**
- D: el maíz, la pesca y la tortuga en la dieta de la costa del Golfo, y los restos de cacao en vasijas de época
  olmeca.
- I: las recetas concretas.
- F: los efectos en el juego.
- No se usan nombres en náhuatl, como «atole», porque son de otra cultura y época.
- El autor debe contrastarlo con fuentes (`Docs/Culturas/Olmeca.md`).

## Pendiente

- Que el autor revise y reescriba las líneas.
- Una señal de carga mientras se mantiene pulsado (hoy no se ve nada hasta los 0,5 s).
- Probar la pulsación larga en el S24+.
- Icono propio del fogón en la escena (hoy es solo el punto de interacción, como el taller del artesano).
