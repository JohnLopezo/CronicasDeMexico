# Sistema de inventario (M5)

La **bolsa** del jugador: materiales, consumibles, reliquias, coleccionables, objetos de la historia y ecos.
Implementación: `Core/Progress/Inventory.cs` (sin dependencias de Unity más allá de `Mathf`) con catálogo
`IItemCatalog` → `Data/Rewards/ItemCatalog` en `Resources/Recompensas/CatalogoObjetos`.

## Categorías

| Categoría | Tope por defecto | Se vende | Uso |
|---|---|---|---|
| Material | 30–60 | Sí (si tiene valor) | Fabricar, trueque, mejorar |
| Consumible | 3–5 | Sí | Tecla 1 o botón «Usar» en la bolsa |
| Reliquia | 1 | Sí, salvo únicas y equipadas | Equipar en una de las **2 ranuras** |
| Coleccionable | 1 | No | Abre su entrada del Museo (`registro.<entrada>`) |
| Narrativo | 1–9 | **Nunca** | Historia y misiones; no se pierde |
| Moneda (ecos) | 9999 | — | Comprar al comerciante |

- **Topes.** El tope (`maxStack`) va en cada `ItemData`. Lo que no cabe se descarta y el aviso solo cuenta lo que
  entró. Los topes bajos de los consumibles evitan acumular curaciones: los combates siguen siendo exigentes.
- **Reliquias.** Son accesorios del animal: amuletos, collares, marcas o talismanes, nunca armaduras humanas. Hay dos
  ranuras. Las que tienen `guardianOnly` solo suman con ese Guardián; por ejemplo, la Marca del jaguar.
- **Objetos únicos y narrativos.** No se venden ni se dan en trueque, y tampoco se pierden al caer (la penalización
  es 0). Una reliquia equipada tampoco se puede vender.

## Consumibles

Los aplica `Gameplay/Player/PlayerStats` sobre la `Health` del jugador y la energía de `Progression`:

| Efecto (`ConsumableEffect`) | Comportamiento |
|---|---|
| `heal` | Cura esa vida (no se usa con la vida llena) |
| `energy` | Recupera esa energía (no se usa con la energía llena) |
| `defenseBonus` + `duration` | Defensa extra temporal; no se acumula con otra igual |

- **Tecla 1 (`UseItem`):** usa el consumible que más falte. Primero cura si la vida está por debajo del 60 %;
  después, la energía.
- **Desde el menú:** la bolsa pide el uso con `Inventory.RequestUse`, para que la UI no dependa de Gameplay.

## Guardado

`SaveData.items` guarda la lista de `{id, count}` en el orden en que se obtuvieron, y `SaveData.equippedRelics` las
reliquias equipadas. Al cargar se ignoran los ids vacíos, las cantidades no positivas y las reliquias que ya no
se tienen.

## Interfaz

La pestaña **Bolsa** del menú (`GameMenuController`) tiene dos partes:

- **Resumen:** ecos, nivel y estadísticas actuales (vida, ataque, defensa y energía).
- **Filas por objeto:** icono de 16 × 16, nombre coloreado por rareza, cantidad, descripción y uso, más los botones
  **Usar**, **Equipar** o **Quitar**, según la categoría.

Los iconos los dibuja `Editor/SceneGeneration/ItemIconFactory` (provisionales, como los sprites de personajes) en
`Art/UI/Iconos`.
