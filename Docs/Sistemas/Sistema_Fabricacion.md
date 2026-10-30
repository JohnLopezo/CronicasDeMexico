# Fabricación, mejora y comercio (M5)

Economía simple a propósito (decisión 4): se fabrica con el artesano, se compra, se vende y se cambia con el
comerciante, y no hay más. Implementación: `Core/Progress/Economy.cs` (reglas sin estado), `Data/Rewards/RecipeData`
y `TradeOfferData`, `Resources/Recompensas/CatalogoEconomia`. En la ciudad, `ServicePoint` abre las pestañas
**Fabricar** (taller del artesano) y **Comerciar** (puesto del comerciante).

## Ecos: la moneda del juego

> **Los «ecos» son una unidad ficticia (F) del Mundo de las Memorias: destellos de memoria que el comerciante acepta.
> NO representan ninguna moneda histórica.** Ninguna cultura de Mesoamérica usó «ecos». Para el comercio real se
> documentan bienes de intercambio como el cacao, las mantas de algodón y las cuentas de piedra verde, pero el
> juego no los presenta como moneda. Por eso también se permite el **trueque directo** de materiales con los NPC.

- Los ecos se obtienen de enemigos, ofrendas, misiones, la nota de la mazmorra y la venta de materiales.
- Ocupan un espacio en la bolsa (tope 9999) y nunca se pierden.

## Fabricación (artesano de jade)

Para fabricar, la receta tiene que estar **desbloqueada** (su bandera `receta.*`) y hay que tener **todos** los
materiales. Al fabricar se consumen y se recibe el resultado. La pantalla muestra lo que falta de cada material.

| Receta | Materiales | Resultado | Se desbloquea |
|---|---|---|---|
| Semilla medicinal preparada | 2 semillas + 1 arcilla | 1 semilla preparada | Desde el principio |
| Amuleto de jade | 2 jade + 2 fibras | Amuleto de jade | Misión MP_OLM_03 |
| Esencia de energía | 2 esencias del olvido + 1 semilla | 1 esencia de energía | Misión MP_OLM_04 |
| Talismán de obsidiana | 2 obsidiana + 1 esencia + 1 fibra | Talismán | Misión MP_OLM_06 |
| Protección de basalto | 4 basalto + 2 fibras | Protección de basalto | Misión MP_OLM_06 |
| Reliquia espiritual | 1 jade + 1 fragmento de memoria menor | Reliquia espiritual | Liberar al Guardián |
| **Mejora:** Corazón despierto | Corazón de la Piedra Viva + basalto de la Piedra Viva + 3 jade | Corazón de la Piedra Viva, despierto | Ofrenda final |

- **Mejora.** Se hace con una receta que consume la reliquia de base y da la versión mejorada. Así no hace falta un
  sistema de niveles de objeto.
- **Fragmentos menores.** Los fragmentos de la selva son objetos narrativos que no se venden. La única forma de
  gastarlos es la reliquia espiritual.

## Comercio (comerciante)

| Oferta | Se recibe | Se paga | Se desbloquea |
|---|---|---|---|
| Semilla preparada | 1 | 12 ecos | Desde el principio |
| Esencia de energía | 1 | 15 ecos | Desde el principio |
| Fibras | 3 | 6 ecos | Desde el principio |
| Arcilla | 2 | 4 ecos | Desde el principio |
| **Trueque:** obsidiana | 1 | 3 basalto | Desde el principio |
| **Trueque:** jade | 1 | 2 obsidiana | Desde el principio |
| Infusión de basalto | 1 | 30 ecos | Misión MP_OLM_06 |

- **Venta.** Cualquier objeto vendible se vende por su valor en ecos (columna «Valor» del catálogo).
- **No se venden ni se dan en trueque:**
  - los objetos narrativos;
  - los únicos (por ejemplo, el Corazón o la Marca del jaguar);
  - los coleccionables;
  - las reliquias equipadas.
- **Contexto (I/D).** El comerciante trae obsidiana y piedra verde de lejos: el intercambio a larga distancia de
  obsidiana y jade está documentado para el Golfo olmeca (**D**). Sus precios y el trueque concreto son ficción
  del juego.

## Para ajustar

- **Precios y recetas** se cambian en sus assets (`Data/Rewards/Olmeca/RCT_*`, `OFE_*`) desde el Inspector.
- **Una oferta o receta nueva** se crea como asset y se añade a `CatalogoEconomia`. `OlmecaRewardAssets` rehace la
  lista del catálogo al regenerar: las que se creen a mano deben añadirse también en el código, o se perderán de la
  lista.
