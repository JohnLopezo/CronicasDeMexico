# Auditoría de animaciones de los protagonistas (piloto: Balam)

Fecha: 2026-11-06. Commit auditado: `f042af4`. **Aprobada por el autor con sus propuestas de §8**, salvo la hoja nueva de Balam, que falta. Hechos después, con un cuadro provisional: Defense, Victory y SpecialAttack de Balam (F; el Golpe de tierra se queda en C). Walk de frente y de espalda: 4 cuadros generados para los siete. Estado general y cómo regenerar: `Docs/Personajes/Guerreros.md`.

## 1. Conclusión

- **Seis de las nueve animaciones ya están conectadas al juego:** Idle, Walk, Run, Attack, Hurt y Death, algunas con pocos cuadros (§3).
- **Faltan tres, tanto de arte listo como de sistema:** SpecialAttack, Defense y Victory.
- **El sistema actual sirve y no hay que reemplazarlo.** Basta con ampliarlo.
- **La hoja nueva de Balam, con ataque y habilidad especial, no está en el proyecto.** Esta auditoría usa su ficha actual.
- **El arte es el principal límite.** Las fichas son referencias con un solo cuadro de cada acción, y Habilidad Especial no se puede separar de su efecto por recorte.

## 2. Sistema actual

| Pieza | Qué hace | Veredicto |
|---|---|---|
| `DirectionalSpriteAnimator` | Elige sprite por dirección y estado: acción > caminar > reposo. Reflejo por escala X | **Conservar.** No se usa Animator de Unity en el jugador, ni hace falta |
| `DirectionalSpriteSet` | Sprites de una animación en 5 direcciones (N, NE, E, SE, S) | **Conservar** |
| `CharacterAnimationDriver` | Traduce eventos a animaciones: `Struck`, esquiva, `Damaged`, `Died` | **Ampliar** con especial, defensa y victoria |
| `GuardianData` | Datos del guerrero y sets: walk, attack, run, hit, death | **Ampliar** con tres sets |
| `GuerreroCharacterAssets` | Crea sets y datos desde los PNG | **Ampliar.** Las animaciones son una lista corta |
| `recortar_guerreros.py` | Recorta fichas a sprites | **Conservar** para lo actual; arte nuevo con cuadrícula fija (§7) |
| `PlayerAttack` | Golpe delante, 0,8 m de alcance, 0,7 m de radio, 0,35 s de espera. Evento `Struck` | **Sin cambios** |
| `PlayerAbility` | Solo el Golpe de tierra; exige la Bendición `tierra`. **No tiene evento ni animación** | **Ampliar** (§6) |
| `Health` | Vida, defensa fija, invulnerabilidad, eventos | Sin cambios para Hurt y Death; Defense pide un cambio pequeño |
| `PlayerMovement` | Esquiva 0,18 s con 0,3 s de invulnerabilidad, 0,6 s de espera | **Sin cambios** |
| `DungeonCompletion.Finish` | Da la nota y abre la pantalla de resultados | Punto de entrada de Victory (§6) |

**Direcciones:** el juego calcula 8 y dibuja 5; las del oeste se reflejan. Las fichas traen 3 vistas útiles, así que NE y SE reutilizan el Este. Las columnas "Izquierda" y "Derecha" de la ficha de Balam miran las dos a la derecha: el juego no necesita "Izquierda".

**Entradas (`InputSystem_Actions`):** existen Attack, Interact, Ability, Dodge, UseItem, Menu y Options. **No existe ninguna de defensa.**

## 3. Animaciones de Balam

| Requisito | En el juego | Cuadros reales | Vistas | Estado |
|---|---|---|---|---|
| Idle | `Reposo` | 1 | N, E, S | Funciona, sin movimiento |
| Walk | `Caminar` | 4 de lado; 2 aproximados de frente y espalda | N, E, S | Funciona; frente y espalda solo alzan los pies |
| Run | `Correr` | 3 | Solo E | **Solo durante la esquiva**: no hay correr normal |
| Attack | `Ataque` | 2 | Solo E | Funciona, **sin la energía del Jaguar** (se quitó al recortar) |
| **SpecialAttack** | — | 0 | — | **Falta** arte y conexión |
| **Defense** | — | 0 | — | **Falta** arte, acción, botón y regla |
| Hurt | `Dano` | 1, durante 0,4 s (antes 0,2: el destello la tapaba) | Solo E | Funciona |
| Death | `Muerte` | 5: Daño, Caída, Desplome, Espíritu, Final (añadidos a petición del autor, después de esta auditoría) | Solo E | Funciona |
| **Victory** | — | 0 | — | **Falta** arte y conexión |

## 4. Recursos gráficos verificados

| Comprobación | Resultado |
|---|---|
| ¿Spritesheet listo para Unity? | **No.** Son fichas ilustradas de 1536×1024 con rótulos, marcos y fondo pintado |
| Separación de fotogramas | Sin cuadrícula; las cajas se midieron a mano para cada ficha |
| Transparencia | **Ninguna:** PNG RGB de 8 bits. Se quita por recorte |
| Pixel art real | **No:** ilustración con aspecto pixelado que se reduce unas 3,4 a 5,4 veces |
| Importación en Unity | Correcta: 48 PPU, Point, sin mipmaps ni compresión, pivote (0,5; 2/64), alfa 0 o 255 |
| Pies | **Consistentes:** los 20 sprites de Balam tienen los pies en la misma fila |
| Alto | **Inconsistente:** Reposo 54 px, Caminar 48 a 51, Correr 48 a 51, Ataque 39 y 49, Daño 47 |
| Paleta | **Sin paleta común:** de 760 a 1180 colores por sprite. Reposo y Daño salen de dibujos a otra escala, lo que produce un salto visible |
| Lienzos | Distintos pero compatibles: 48, 80 o 112 × 64, con el mismo pivote normalizado |

### Prueba con los paneles sin usar de Balam

Recorte de prueba, fuera del proyecto, a la misma escala:

| Panel | Resultado | Decisión propuesta |
|---|---|---|
| Defensa | Limpio, 44×48 px; sin el escudo de energía, que aquí no existe como dibujo aparte | **Aprovechable** como un cuadro provisional |
| Victoria | Limpio, 56×49 px | **Aprovechable** como un cuadro provisional |
| Habilidad Especial, solo cuerpo | Pierde piernas y parte de la ropa, porque el efecto se monta sobre él | **No sirve** |
| Habilidad Especial, con efecto | Figura y jaguar dorado fundidos, 91-100 px de ancho, con halo del fondo | **No sirve**: no permite animar ni reutilizar el efecto |

## 5. Estado de los siete

Todas las fichas traen Frente, Izquierda, Espalda, Derecha, Reposo, Caminar, Correr, Ataque, Defensa, Habilidad Especial, Daño y Victoria. **Los paneles de Defensa, Habilidad y Victoria de los otros seis no se han probado todavía.**

| Guerrero | Idle | Walk | Run | Attack | Hurt | Death | Special / Defense / Victory |
|---|---|---|---|---|---|---|---|
| Balam | 1 | 4 + 2 | 3 | 2 | 1 | Hoja propia | Faltan |
| Xali | 1 | 4 + 2 | 3 | 2 | 1 | Hoja propia | Faltan |
| Yaret | 1 | 4 + 2 | 3 | 2, con restos verdes | 1 | Hoja propia | Faltan |
| Itzel | 1 | 4 + 2 | 3 | 2, con chispa | 1 | Hoja propia | Faltan |
| Ocel | 1 | 4 + 2 | 3 | 2, con restos morados | 1 | **Provisional** | Faltan |
| Naira | 1 | 4 + 2 | 3 | **1** | 1 | **Provisional** | Faltan |
| Ameyali | 1 | 4 + 2 | 3 | **1** | 1 | **Provisional** | Faltan |

## 6. Estructura técnica propuesta

Sin controladores nuevos que dupliquen lo existente:

1. **`GuardianData`:** añadir `specialSet`, `defenseSet` y `victorySet`.
2. **`CharacterAnimationDriver`:** reproducirlos desde eventos. Sustituir `Configure`, ya con siete parámetros posicionales, por una asignación con nombre, para no equivocar el orden al ampliarlo.
3. **SpecialAttack:** `PlayerAbility` emite un evento `Used`, igual que `PlayerAttack.Struck`. El poder de cada guerrero va como dato, no como un componente por personaje. **Antes hay que resolver la decisión 1.**
4. **Efectos separados del cuerpo:** un hijo del prefab del jugador con sus propios sprites, como ya hace `GolpeTierra`. Así Attack y SpecialAttack recuperan la energía del Jaguar sin ensuciar los cuadros del cuerpo, y se reutiliza para los siete.
5. **Defense:** acción nueva en `InputSystem_Actions` y un componente pequeño, `PlayerDefense`. La reducción de daño se hace en `Health`.
6. **Victory:** se dispara en `DungeonCompletion.Finish`. **Debe mostrarse antes de abrir los resultados:** el menú pone `Time.timeScale` en 0 y `DirectionalSpriteAnimator` usa `Time.deltaTime`, así que una animación quedaría congelada.

### Cambios necesarios en Unity

| Tipo | Cambio |
|---|---|
| Sprites | `Guerrero_Balam_{Especial,Defensa,Victoria}N_03.png` en `Sprites/Guerreros`; el importador ya los trata bien. Efectos en una carpeta `Efectos` con su pivote |
| Assets | 3 `DSS_Guerrero_Balam_*` y `GUA_jaguar` actualizado, generados por `GuerreroCharacterAssets` |
| Prefab | `Jugador.prefab`: componente de defensa y efecto, aplicados por `PlayerPrefabUpgrade` |
| Entrada | Acción `Defense` en teclado y mando |
| Escenas | No hace falta regenerarlas: heredan del prefab |
| Paquetes y Unity | Sin cambios |

### Problema aparte: controles táctiles

Android es la plataforma objetivo, pero **no se encontró ningún control táctil en pantalla**. Las únicas entradas de toque son tocar para atacar y las de interfaz; moverse, esquivar y usar habilidades no tienen una. Añadir Defense y SpecialAttack aumenta esta falta. No se ha probado en un dispositivo. No es parte de esta tarea, pero hay que decidir cuándo se hace.

## 7. Arte que se necesita

Para Balam, en este orden. Los cuadros son una propuesta mínima:

| Animación | Cuadros | Vistas | Nota |
|---|---|---|---|
| SpecialAttack, cuerpo | 3-4 | Lado | Sin efecto |
| SpecialAttack, efecto | 4-6 | — | Jaguar dorado **en imagen aparte** |
| Attack, efecto | 2-3 | — | Energía en imagen aparte; el cuerpo ya existe |
| Defense | 2 | Lado | Entrar y sostener. El panel actual sirve de provisional |
| Victory | 2-4 | Frente | El panel actual sirve de provisional |
| Idle | 2-4 | Frente, lado, espalda | Mejora |
| Walk | 4 | Frente, espalda | Mejora: hoy son aproximados |
| Attack | 2-3 | Frente, espalda | Mejora: hoy ataca de lado aunque mire al norte |

**Para que el recorte sea fiable, cada hoja nueva debería tener:**
- Fondo liso de un solo color, que no aparezca en el personaje; o transparencia real.
- Cuadrícula regular, sin rótulos ni marcos, con todos los cuadros del mismo tamaño.
- Los pies en la misma línea y el personaje a la misma escala en todas las hojas.
- Efectos, suelo y sombras fuera del cuerpo.

## 8. Decisiones del autor

1. **¿Qué es SpecialAttack frente al Golpe de tierra?** Ambos usarían el mismo botón (**C**, botón Este). Propuesta: **SpecialAttack, disponible desde el inicio, propio de cada guerrero y con coste de energía; el Golpe de tierra pasa a una acción aparte** al desbloquearlo, porque abre pasos en la mazmorra.
2. **¿Cómo funciona Defense?** Propuesta: mantener pulsado, no moverse y reducir el daño a la mitad mientras dure.
3. **¿Cuándo se ve Victory?** Propuesta: al abrir la ofrenda final, 1,5 s antes de los resultados.
4. **¿Correr normal?** Hoy solo se corre al esquivar. Propuesta: dejarlo así en el piloto, porque añadirlo cambia el ritmo de las mazmorras.
5. **Hoja nueva de Balam:** falta pasarla a `Docs/Guerreros`.
6. **¿Provisionales o esperar arte?** Propuesta: conectar ya Defense y Victory con sus paneles actuales y esperar el arte solo para SpecialAttack.

## 9. Plan del piloto (requiere aprobación)

1. Conectar Defense y Victory de Balam con los paneles actuales, con pruebas.
2. Con la decisión 1 y el arte, SpecialAttack y su efecto.
3. Efecto de energía en Attack.
4. Revisar a Balam jugando y aprobarlo como referencia.
5. Solo entonces, los otros seis, uno a uno. Cada uno necesita su arte y su poder.

## 10. Sin comprobar

- Las animaciones no se han revisado jugando; las mediciones son de los archivos y del código.
- La búsqueda de la hoja nueva de Balam cubrió `Docs/Guerreros` y, por nombre, Descargas, Escritorio, Imágenes y Documentos.
- No se comparó la paleta de los guerreros con la de los NPC.
- Rendimiento en Android de los efectos: sin medir.
