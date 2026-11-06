# Cómo continuar con los guerreros

Última actualización: 2026-11-06, en el i7-10700. Rama `feature/prototipo-olmeca`, último commit de código `8a838dc`. Pruebas al cerrar: EditMode 107/107 y PlayMode 80/80.

Documentos relacionados: `Guerreros.md` (cómo están hechos y cómo regenerarlos) y `Auditoria_Animaciones.md` (decisiones aprobadas y arte que falta).

## Dónde estamos

- Se eligen 7 guerreros humanos, ligados al espíritu de su animal, al empezar y en la pestaña Guardián del menú.
- **Balam es el piloto:** es el único con Defensa, Victoria y Habilidad especial con pose propia.
- **Muertes con espíritu:** Balam, Xali, Itzel y Yaret, en 7 cuadros. Comprobadas a color en el juego real, en la selva.
- **Muertes provisionales:** Naira, Ameyali y Ocel caen en 3 cuadros, sin espíritu.
- Se reaparece a los 3 s.

| Acción | Teclado | Mando |
|---|---|---|
| Atacar | Clic izquierdo o Enter | Oeste |
| Esquivar | Espacio | Sur |
| Defender (mantener) | Q o clic derecho | LB |
| Habilidad especial (solo Balam) | F | RB |
| Golpe de tierra (tras liberar al jefe) | C | Este |
| Menú | I | Select |

**Para llegar a la mazmorra hay que empezar en la escena `Boot`.** Si se pulsa Play en `Olmeca_Selva`, la misión `MP_OLM_03` no empieza y la entrada no se abre.

## Qué sigue, en orden

1. **Corregir el salto de tamaño al recibir el golpe final.** La pose de Daño se ve más grande que el reposo, sobre todo en Naira y Ameyali. Se ajusta con la escala `dano` de cada guerrero en `recortar_guerreros.py`. Propuesto al autor; falta su respuesta.
2. **Saber qué pasa con el enemigo que reportó el autor.** Sigue sin identificar: hace falta saber cuál es, en qué escena está y qué se ve.
3. **Recorrer a pie la historia desde `Boot` hasta entrar en la mazmorra.** El arreglo del bloqueo solo se ha probado con teletransporte.
4. **Jugar con Balam** para valorar la Defensa, la Habilidad especial y el equilibrio de atributos, que aún no se han probado jugando.
5. **Con arte nuevo,** ver la lista siguiente.
6. **Los otros seis guerreros:** Defensa, Victoria y Habilidad especial propias, uno a uno, una vez aprobado Balam.
7. **Controles táctiles:** no existen; decidido para después del piloto. Sin ellos no se puede jugar en Android.

## Arte que falta (lo aporta el autor)

Copiar en `Docs/Guerreros`:

- Hojas de muerte de **Naira, Ameyali y Ocel**.
- Jaguar dorado de la Habilidad especial de Balam, **aparte del cuerpo**, con 4 a 6 cuadros. La hoja nueva de Balam que menciona el documento de animaciones no ha llegado al proyecto.
- Caminar de frente y de espalda: 4 cuadros reales por guerrero. Hoy se generan desde el reposo.

Cómo deben venir: fondo liso o transparente, cuadrícula regular, sin rótulos ni marcos, pies en la misma línea, misma escala, y efectos en imagen aparte.

## Cómo comprobar que se ve bien

Las pruebas automáticas no ven los gráficos. Lo que funcionó en esta sesión:

- **Hojas de revisión:** `recortar_guerreros.py` las escribe (`_acciones`, `_muerte`). Poner los sprites sobre un fondo magenta revela si se ve el fondo a través del cuerpo.
- **Ver el juego real:** una prueba PlayMode temporal que carga `Olmeca_Selva`, llama a `SelectGuardian` y `TryDamage` para cada guerrero, y lee `Camera.main.targetTexture` en cada cuadro de la muerte. La textura es de 642×482. Se borra al terminar: no es una prueba del proyecto.
- **Los menús de captura no sirven para juzgar el color:** en lote, los personajes salen a veces negros o blancos, también los NPC, y cambia entre ejecuciones. Sirven para ver posición y tamaño.

## Trampas conocidas

- **Unity debe estar cerrado** para el modo batch y las pruebas. Con pruebas en marcha, no cambiar archivos de `Assets`.
- **Probar la suite completa antes de subir.** PlayMode tarda varios minutos; si deja de escribir en su log, puede estar colgada.
- **`WaitForSeconds` no termina con el menú abierto:** el menú pone `Time.timeScale` en 0. En las pruebas, esperar en tiempo real.
- **Los servicios de Boot se comparten entre pruebas:** el nivel puede estar subido, así que no hay que fijar valores de vida ni dejar otro guerrero elegido.
- **El recorte falla de formas sutiles.** Ha cortado figuras, borrado caras al tomar la piel vecina por fondo y arrastrado suelo. Tras tocar `recortar_guerreros.py`, revisar las hojas de los siete, no solo el caso que se corrige.
- **Regenerar cuando cambian textos en escenas:** el reflejo está en la ciudad y las pistas y `returnDelay` en los pisos.
- **Subir desde Claude Code:** `git -c credential.username=JohnLopezo push`.
- **No subir** `UpgradeLog.htm`. `Docs/Bus` son capturas de errores del autor, sin subir hasta que él lo decida.

## Decisiones ya tomadas

No volver a preguntarlas: el protagonista es una persona; mandan las afinidades de las fichas; cada guerrero reparte el mismo presupuesto de atributos; Defensa se mantiene, sin moverse y a mitad de daño; Victoria dura 1,5 s antes de los resultados; correr solo al esquivar durante el piloto; se puede cambiar de guerrero a mitad de partida, sin recuperar vida ni energía.

**Pendientes del autor:** si `Docs/Bus` se sube, y si el Golpe de tierra se queda en C. Se dejó ahí para no cambiar los textos, aunque lo aprobado era moverlo.
