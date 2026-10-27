# CRÓNICAS DE MÉXICO — DOCUMENTO MAESTRO DE DISEÑO (GDD)

**Versión:** 1.0 — consolidación de diseño, sujeta a revisión.  
**Estado:** Especificación de referencia; no implica implementación.  
**Motor:** Unity 6 / C# · **Plataforma:** Android, arquitectura preparada para iOS · **Estilo:** 2.5D Pixel Art híbrido con geometría 3D estilizada · **Género:** Action RPG, exploración y dungeon crawler · **Modo:** un jugador.

> **Instrucción para Claude Code:** lee este documento completo antes de proponer cambios. Contrasta cada requisito con el repositorio existente; no presupongas que está implementado. En la auditoría actual NO modifiques archivos. Separa requisitos confirmados, propuestas de diseño y pendientes. Conserva escenas, scripts, prefabs y ajustes funcionales. No realices commits sin autorización.

## 1. Visión y pilares

Crónicas de México es una aventura fantástica sobre la memoria de las culturas que habitaron el actual territorio mexicano. Un protagonista renacido como un Guardián animal viaja entre memorias de distintas épocas, explora asentamientos y regiones, conoce personajes, supera mazmorras y recibe poderes que transforman su estilo de combate. Pilares: (1) identidad mexicana investigada y respetuosa; (2) exploración y narrativa ambiental; (3) combate accesible, preciso y variado; (4) progresión por culturas, recuerdos y bendiciones; (5) rendimiento y legibilidad en móviles.

La inspiración funcional combina viaje entre asentamientos y rutas regionales con mazmorras de acción por salas. No copiar material protegido de Pokémon, Soul Knight ni otras obras.

## 2. Historia, mundo y tono

Un joven muere tras una expedición vinculada a un objeto antiguo. Su conciencia despierta en el **Mundo de la Memoria**, una dimensión fantástica que conserva ecos de comunidades y civilizaciones de México. No vuelve como humano: su alma se vincula al animal Guardián que el jugador elige. Una fuerza denominada provisionalmente **El Olvido** altera recuerdos, lugares y guardianes espirituales. El protagonista recupera Fragmentos de Memoria y aprende que las culturas no son piezas congeladas: nacen, se transforman, conviven e influyen unas en otras. Su misión es impedir la destrucción de esas memorias, no borrar el cambio histórico.

Tono: aventura colorida, misteriosa, emotiva y ocasionalmente solemne; los dioses o entidades importantes deben tener presencia y motivaciones, no ser simples monstruos. La revelación final enfrenta al protagonista con El Olvido y una decisión sobre la preservación de las memorias. Los finales y diálogos definitivos están pendientes de guion.

## 3. Estructura histórica y capítulos

Ruta jugable **provisional**, NO cronología lineal de reemplazo entre culturas:

1. Primeras comunidades agrícolas — prólogo/tutorial (contexto por investigar).
2. Olmeca — primer capítulo y primera mazmorra principal.
3. Zapoteca.
4. Teotihuacana.
5. Maya (seleccionar ciudades y periodo concretos).
6. Epiclásico — capítulo de transición, no una sola cultura.
7. Tolteca.
8. Mixteca.
9. Purépecha.
10. Mexica — tramo final antes del contacto español.

Las culturas coexistieron durante periodos extensos. La transición entre capítulos ocurre mediante **memorias/eras**, no mediante la afirmación de que una sociedad desapareció cuando surgió la siguiente. Investigar fechas, regiones, arquitectura, iconografía, lenguas, organización social y fuentes por capítulo. Incluir otras culturas cuando corresponda. Separar explícitamente arqueología documentada, interpretaciones y ficción. No inventar dioses históricos: cuando la atribución sea incierta, usar una entidad ficticia claramente identificada.

## 4. Personaje y Guardianes

El jugador define un nombre y elige **un Guardián inicial**, que puede mantener durante la aventura. Cada Guardián tiene silueta, personalidad, movimiento, estadísticas, ataque básico, ataque pesado, tres habilidades activas, definitiva y pasiva; evolución espiritual cosmética y funcional en etapas posteriores. Los elementos base son **Agua, Tierra, Fuego, Viento, Luz y Oscuridad**. Las afinidades no determinan la personalidad.

| Guardián | Rol propuesto | Afinidad propuesta | Identidad jugable |
|---|---|---|---|
| Jaguar | Daño cercano | Oscuridad/Tierra | Garras, salto, agresividad y remates |
| Águila real | Movilidad/distancia | Viento | Ataques aéreos, detección y reposicionamiento |
| Ajolote | Control/soporte | Agua | Proyectiles y curación limitada |
| Lobo mexicano | Equilibrado | Viento/Tierra | Embestidas, aullido, supervivencia |
| Quetzal | Distancia/apoyo | Viento/Luz | Energía espiritual y exploración |
| Xoloitzcuintle | Protección/espiritual | Por definir | Vínculo con memorias y defensa |
| Vaquita marina | Control acuático | Agua | Olas y empuje; viabilidad de movilidad terrestre por definir |
| Mariposa monarca | Por definir | Por definir | Personaje futuro o fauna/NPC |

**Alcance inicial:** implementar completamente **un** Guardián y usar los demás como diseño futuro; Jaguar es candidato recomendado, sujeto a lo existente en el proyecto. No todos los animales mencionados son endémicos exclusivos; verificar distribución y contexto cultural.

## 5. Dirección artística y cámara

Presentación objetivo: **Pixel Art moderno en 2.5D**, sprites/animaciones legibles y escenarios con geometría 3D estilizada, materiales coherentes y profundidad. El prototipo actual contiene formas y colores simples; evolucionarlo incrementalmente, sin descartar mecánicas funcionales. El resultado final debe incluir vegetación por capas, suelos con variación, detalles arquitectónicos investigados, sombras optimizadas, agua estilizada, efectos ambientales y señales visuales de interacción.

Definir cámara semisométrica fija o suavemente seguida, ángulo y escala coherentes; movimiento en ocho direcciones; sombras bajo personajes; orden de oclusión y transparencia cuando árboles/columnas tapen al jugador. Usar sprites con filtros y escalado adecuados, atlas, paletas consistentes y efectos que no oculten el combate. Resolución artística y tamaño exacto de sprites se decidirán mediante pruebas; punto de partida conceptual: personajes de ~32–48 píxeles de altura en su arte base, no regla cerrada.

Animaciones por Guardián: reposo, caminar/correr, ataques, pesado, habilidad, esquive, recibir daño, derrota, interacción, victoria; direcciones necesarias según cámara. Mantener diferenciación por silueta y color.

## 6. Estructura del mundo y exploración

**Mundo de la Memoria → capítulo cultural/era → región → asentamiento/hub → caminos/biomas → ruinas → mazmorra.** Las regiones se desbloquean progresivamente; no crear de inicio un mapa abierto continuo de los 32 estados. Cada capítulo puede incluir ciudad/pueblo, mercado, artesanos, NPC, misiones, fauna ambiental, rutas con encuentros, secretos y entrada ceremonial de mazmorra. La arquitectura, el clima y la vegetación varían por lugar y época.

Los asentamientos son zonas principalmente seguras para diálogo, comercio, preparación y narrativa. Los caminos tienen exploración, eventos y encuentros. Las transiciones entre eras se expresan con cambios de música, iluminación, vegetación, arquitectura y una secuencia narrativa, no solo una pantalla de carga.

## 7. Mazmorras y pisos

Cada cultura principal tiene una mazmorra de varios pisos, con progresión de dificultad y mini-jefes aproximadamente cada dos o tres pisos cuando el ritmo lo justifique. Cantidades propuestas, ajustables:

| Capítulo | Pisos objetivo |
|---|---:|
| Prólogo | 5 |
| Olmeca | 8 |
| Zapoteca | 9 |
| Teotihuacana | 10 |
| Maya | 12 |
| Epiclásico | 10 |
| Tolteca | 12 |
| Mixteca | 13 |
| Purépecha | 14 |
| Mexica | 15 |

Tipos de sala: combate, tesoro, acertijo, historia, descanso, santuario, secreto, mini-jefe y jefe. Cada piso debe tener objetivo, mecánica, peligro, recompensa y conexión narrativa. Primeras mazmorras hechas a mano; generación procedural solo tras demostrar que la base funciona. Reutilizar prefabs y mecánicas, pero dar identidad visual y cultural propia a cada capítulo.

### 7.1 Mazmorra olmeca — «La ciudad bajo la selva» (provisional)

1. **El Umbral:** entrada selvática, combate y exploración tutorial.
2. **El Pantano:** terreno húmedo y zonas que ralentizan.
3. **Las Piedras del Jaguar:** acertijos de activación e indicios narrativos.
4. **El Guardián:** mini-jefe «Jaguar de Piedra» (criatura ficticia).
5. **El Templo Hundido:** mecanismos que alteran el nivel del agua.
6. **La Cámara Ritual:** memoria y santuario de descanso.
7. **El Corazón de Piedra:** enemigos élite y segundo mini-jefe «Guardián de la Piedra»; reajustar distribución si el ritmo requiere más espacio.
8. **Santuario Final:** «Piedra del Primer Recuerdo» y combate con el jefe final.

**Jefe final:** «El Guardián de la Primera Piedra», entidad ficticia inspirada en motivos monumentales y felinos olmecas; **no** describirla como dios olmeca documentado. Tras el combate se libera de la corrupción, revela un recuerdo y concede una Bendición.

## 8. Combate y controles

Action RPG rápido, justo y legible en pantallas táctiles. En Android horizontal: joystick izquierdo; a la derecha ataque, esquive, habilidad contextual y definitiva; diseñar cuidadosamente la accesibilidad de ataque pesado y otras habilidades para evitar saturación. Teclado/ratón para pruebas de Editor. Implementar Input System y acciones desacopladas de dispositivos.

Mecánicas: movimiento en ocho direcciones, ataque básico y combo, pesado, tres habilidades por Guardián, definitiva con carga, esquive con breve invulnerabilidad, esquive perfecto con recompensa, interrupción limitada, hitboxes/hurtboxes, retroalimentación de impacto y ventanas claras de preparación/ejecución/recuperación. Bloqueo o absorción pueden ser habilidades específicas, no obligatorias para todos.

Estadísticas: HP, daño, defensa, velocidad, energía; ataque físico/elemental, resistencias y críticos con límites. Estados: quemadura, ralentización/congelación, enraizamiento, aturdimiento, corrupción y otros según balance. Sistema elemental configurable; empezar con pocos elementos y ampliar. No diseñar dificultad mediante inflar HP indiscriminadamente.

## 9. Vida, curación, descanso, muerte y guardado

**Regla central confirmada: la vida NO se regenera automáticamente tras cada combate ni al cambiar de piso.** Solo se recupera mediante habilidades, consumibles, Bendiciones, NPC/eventos específicos y zonas de descanso. La curación debe ser escasa y estratégica; el Ajolote puede curarse de forma limitada, nunca sin restricciones. La disponibilidad de curación en santuarios puede estar limitada por incursión.

Santuarios:
- **Vida:** curación parcial o completa, según balance.
- **Memoria:** checkpoint y guardado, independientes de la curación.
- **Espiritual:** gestión de Bendiciones.
- **Sabiduría:** Códice y recuerdos.
- **Artesano:** mejoras.

Al llegar a 0 HP: animación de derrota, regreso al último checkpoint y penalización moderada configurable (por ejemplo, parte de recursos no esenciales), **sin perder permanentemente el progreso principal**. Definir qué persiste en una incursión antes de programarlo.

Guardar: identificador y nombre del Guardián, nivel/XP, HP, inventario, bendiciones, misiones, capítulo, piso, checkpoint, fragmentos, diálogos/eventos importantes. Usar datos versionados y mecanismo de respaldo/recuperación. No depender de PlayerPrefs como sistema principal de partidas.

## 10. Enemigos, IA y jefes

Arquetipos: melee, rápido, tanque, distancia, mágico/apoyo, volador, control, élite, mini-jefe y jefe. IA modular con Idle → Patrol/Detect → Chase/Reposition → Telegraph → Attack → Recover → Defeated. Grupos con roles complementarios, evitando comportamientos injustos. Los guerreros históricos no son «malos» por pertenecer a una cultura: los enemigos pueden ser espíritus corrompidos, constructos y guardianes ficticios. Cualquier personaje histórico debe investigarse antes de representarlo.

**Jefe olmeca:** arena circular con piedra, vegetación y agua. Fase 1: zarpazo, embestida y golpe de tierra. Fase 2 (~60% HP): sube el agua y aparecen ondas, rugido y fragmentos. Fase 3 (~25% HP): secuencias combinadas y «Furia de la Primera Piedra», con zonas de impacto anunciadas. Todos los ataques importantes tienen telegráficos y oportunidades de respuesta. La victoria purifica al Guardián, otorga el Fragmento de Memoria y presenta tres Bendiciones posibles.

## 11. Bendiciones, habilidades y progresión

Cada jefe de capítulo concede una **elección entre tres Bendiciones**, de la cual el jugador toma una; la Bendición acompaña al personaje a capítulos posteriores. Ejemplos olmecas: **Fuerza de la Tierra** (defensa), **Instinto del Jaguar** (daño cercano/movilidad) y **Memoria del Agua** (mejora limitada de recuperación). Números y efectos definitivos pendientes de balance.

Evolución espiritual por etapas (Guardián → Despierto → Ancestral), sin obligar a que el animal evolucione como una criatura de colección. Builds defensivas, ofensivas, movilidad y soporte; limitar ranuras/capacidad para impedir acumulación desequilibrada. XP, materiales, equipo ligero, cosméticos y Códice complementan la progresión. Las habilidades obtenidas de entidades divinas deben modificar significativamente opciones de la siguiente mazmorra.

## 12. NPC, misiones y narrativa interactiva

Sistema de NPC con ID persistente, rol, horarios/estados si aportan valor, árbol de diálogo, condiciones, misiones, recompensas y consecuencias. Primer hub con al menos un guía narrativo, comerciante, artesano y dador de misiones. Las conversaciones deben reflejar diferencias culturales y de Guardián cuando tenga sentido, sin estereotipos. Misiones principales para memoria/capítulos y secundarias para exploración, comunidad, secretos y Códice. Narrativa ambiental mediante objetos, arquitectura, escenas y recuerdos; evitar largos bloques obligatorios de texto.

Las apariciones de dioses o entidades: cambios en ambiente, sonido, iluminación y cámara; diálogo o prueba; combate cuando corresponda; resolución y Bendición. No todos los encuentros divinos deben resolverse mediante violencia.

## 13. UI/UX y accesibilidad

Android en horizontal; joystick táctil y botones grandes, zonas seguras, UI adaptable a resoluciones/aspectos, HUD con vida/energía/habilidades/estados, interacción contextual, inventario, mapa, misiones, Códice, pausa y opciones. Señales de daño distinguibles sin depender exclusivamente del color; tamaño de texto legible; opciones de sonido, vibración y calidad gráfica. Menús utilizables con ratón y controles táctiles. La pantalla de selección debe mostrar diferencias de Guardianes sin abrumar al usuario.

## 14. Sonido y música

Ambientes diferenciados por bioma, pueblo, templo, combate, jefe y transición; sonidos para pasos, golpes, esquives, agua, viento, interfaz, NPC y poderes. Música original o recursos con licencia compatible. No presentar música inventada como reconstrucción histórica auténtica. Diseñar AudioManager y mezcladores para controlar volúmenes y transiciones.

## 15. Arquitectura Unity y escalabilidad

Mantener Unity 6 y C#. Propuesta de carpetas dentro de `Assets/_Project`: `Art`, `Audio`, `Materials`, `Prefabs`, `Scenes`, `Scripts`, `ScriptableObjects`, `UI`, `Animations`, `Settings`, `Tests`, `Documentation`. Organizar scripts por funciones: `Core`, `Player`, `Combat`, `Abilities`, `Elements`, `Enemies`, `Bosses`, `Dungeon`, `World`, `NPC`, `Dialogue`, `Quests`, `Inventory`, `Blessings`, `SaveSystem`, `UI`, `Audio`.

Datos de Guardianes, enemigos, habilidades, elementos, bendiciones y salas mediante ScriptableObjects cuando convenga. Usar componentes desacoplados, prefabs reutilizables, escenas aditivas solo si son necesarias, eventos cuidadosamente gestionados, identificadores persistentes y pruebas. Evitar sobreingeniería y refactorizaciones masivas sin justificación. Para iOS, separar entrada, almacenamiento, interfaces y ajustes dependientes de plataforma; la compilación y publicación final de iOS requerirá entorno compatible de Apple.

## 16. Rendimiento y hardware

Equipo de desarrollo de referencia: HP ProDesk 600 G2, Intel Core i5-6500, 12 GB RAM, SSD 1 TB, probablemente Intel HD 530 salvo confirmación de otra GPU. Priorizar un flujo liviano. Android de gama media como objetivo inicial: **30 FPS estables tras pruebas en dispositivo real**; 60 FPS opcionales. Preferir pipeline gráfico existente si es viable; evaluar URP, atlas, pooling, batching, materiales compartidos, iluminación horneada, sombras limitadas, texturas comprimidas, LOD/culling según necesidad y reducción de GC.

Perfiles **Bajo, Medio y Alto**. Según el estado reportado por Claude, actualmente los builds Windows inician en **Bajo**; en Bajo se desactivan HDR y Depth Texture y la cámara apaga postprocesado. **`Docs/Rendimiento.md` está desactualizado:** corregir estas descripciones en Fase 1, después de autorizar cambios. La tabla de FPS permanece vacía hasta mediciones reales; no inventar resultados.

## 17. Primer vertical slice y entregables

Antes de producir todos los capítulos, crear un **vertical slice olmeca** jugable: un Guardián con controles PC/Android, cámara estable, escenario visualmente coherente, pequeño hub con cuatro NPC, dos enemigos comunes, un mini-jefe, mazmorra de **tres pisos para validación**, zona de descanso, jefe, una Bendición, inventario básico, guardado, UI y transición narrativa. Una vez probado, ampliar la mazmorra olmeca a ocho pisos y luego construir el pipeline de otras culturas.

Criterios de aceptación: escena carga sin errores críticos; movimiento de ocho direcciones; controles táctiles probados; hitboxes fiables; daño/curación cumplen reglas; checkpoint funciona; jefe telegráfico y derrotable; Bendición persiste; guardado/carga reproducibles; interfaz legible; rendimiento medido. No declarar éxito sin pruebas.

## 18. Plan de trabajo y reglas para Claude Code

**Fase 0 — Auditoría sin cambios:** inventariar escenas, scripts, prefabs, paquetes, Input System, cámara, iluminación/pipeline, NPC, combate, mazmorras, guardado, UI, calidad, Android y estado de Git. Identificar qué existe, qué falta y qué está roto. Registrar rutas exactas y prioridades. **No editar archivos durante esta fase.**

**Fase 1 — Estabilización:** corregir errores, documentar ajustes de rendimiento y proteger escenas funcionales. Solicitar aprobación antes de modificar.

**Fase 2 — Arte y cámara:** guía visual, materiales, ambientación, escalas, cámara y sprites.

**Fase 3 — Guardián:** movimiento, animaciones, combate y controles.

**Fase 4 — Mundo/NPC:** hub, interacción, diálogos y misiones.

**Fase 5 — Mazmorra:** salas, pisos, enemigos, descanso y recompensas.

**Fase 6 — Jefe:** fases, IA, animación, recompensa y Bendición.

**Fase 7 — Persistencia/UI:** guardado, inventario, Códice, misiones.

**Fase 8 — Optimización:** Android real, perfiles y validación.

Antes de cambios: inspeccionar el repositorio, comprobar `git status`, entender dependencias y explicar alcance. Durante: cambios pequeños, preservar trabajo existente, no borrar recursos ni añadir paquetes sin necesidad. Después: enumerar archivos modificados, pruebas efectivamente realizadas, errores y pendientes de Editor/dispositivo. No hacer commits sin aprobación.

## 19. Decisiones abiertas

- Guardián definitivo del vertical slice y arte de producción.
- Ángulo exacto de cámara, resolución de sprites, pipeline de animación y estrategia 2D/3D.
- Épocas y asentamientos históricos concretos por cultura.
- Lista final de dioses, entidades y asociaciones verificadas.
- Matriz elemental, estadísticas, números de balance y economía.
- Estructura definitiva de muerte, penalizaciones y guardado en mazmorra.
- Finales narrativos y diálogos completos.
- Alcance final de iOS, controles y dispositivos mínimos.

**Prioridad inmediata:** que Claude lea este documento y complete su auditoría actual **sin editar** el proyecto; después presentar un plan de mejora del prototipo que aparece en Unity.
