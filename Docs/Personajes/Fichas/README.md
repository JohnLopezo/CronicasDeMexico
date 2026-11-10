# Catálogo de personajes: fichas de diseño

Una ficha por personaje, con la misma estructura para que el catálogo se pueda reutilizar en las demás culturas de
México. Para cada cultura se adaptan vestimenta, arquitectura, fauna y rasgos históricos sin perder la identidad
visual del juego.

## Estructura de cada ficha

1. **Identidad:** papel en la historia y en el juego, y de qué se inspira (**D** documentado, **I** interpretación,
   **F** ficción).
2. **Anatomía y proporciones:** medidas reales de referencia y medidas en el juego.
3. **Paleta:** colores en hexadecimal y rampas de cuantización del flujo.
4. **Dimensiones en pantalla:** lienzo, ancla y tamaño aproximado en píxeles; píxeles por metro (45,25).
5. **Animaciones:** cuadros, cuadros por segundo y cuándo se usan.
6. **Comportamiento:** lo que hace en el juego (jugador, NPC o enemigo) y sus patrones reconocibles.
7. **Implementación:** archivos del flujo de arte, assets de Unity y componentes.
8. **Adaptación a otras culturas:** qué se cambia y qué se conserva.

## Reglas comunes de estilo

- **Pixel art HD-2D isométrico.** Se modela en Blender y se renderiza en 5 direcciones (N, NE, E, SE, S); el oeste
  es el este volteado. Supermuestreo ×4, cuantización a rampas fijas y contorno de 1 px.
- **Escala.** Tiles de 64 × 32 px por metro de suelo y 45,25 px por metro. Los personajes miden de 64 a 96 px de
  largo o alto en pantalla.
- **Coherencia.** Sol desde arriba a la izquierda de la pantalla, como en los escenarios. Sin semitransparencias.
- **Rendimiento.** Un sprite por cuadro, sin esqueletos en tiempo de ejecución. Coste en el HD 530 igual que
  cualquier sprite.
- **Plataformas.** Texturas PNG sin compresión con pivote propio: valen para Unity 6 en Windows, Android e iOS.

## Fichas

| Ficha | Etapa | Estado |
|---|---|---|
| [Jaguar (jugable)](Jaguar.md) | A | Hecho, pendiente de aprobación |
| [Jaguar corrupto](Jaguar_Corrupto.md) | B | Hecho, pendiente de aprobación |
| NPC olmecas | C | Por hacer |
| Guardián de la Piedra Viva (coloso de basalto) | D | Por hacer |
| Objetos | E | Por hacer |
