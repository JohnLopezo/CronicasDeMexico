# Habitantes del asentamiento olmeca

Fuente: `Docs/Historia_Mundo.md` §6 (capítulo I), §9; GDD `Docs/Diseno.md` §12, §17 (el hub del vertical slice necesita al menos un guía narrativo, un comerciante, un artesano y un dador de misiones). Contexto histórico en `Docs/Culturas/Olmeca.md`.

## Criterios

- **Sin nombres en "lengua olmeca":** esa lengua no está documentada. Se usan papeles (la anciana, el artesano) o nombres ficticios señalados como tales.
- **Diferencias visuales reales** (§9.2): silueta, edad, complexión, postura, peinado, accesorios, herramientas y animaciones; nada de un mismo modelo recoloreado. Sin caricaturizar.
- **Vestimenta y adornos:** basarse en figurillas y esculturas olmecas (taparrabos, faldellines, capas, tocados, orejeras, collares de piedra verde); los detalles de tela y color son **interpretación** y se marcan así.
- **Vidas propias** (§15): no todos hablan de la Sombra; tienen trabajo, familia y preocupaciones cotidianas.
- **Rutinas sencillas** para Android (§9.3): patrulla corta, trabajo en sitio, conversación; el proyecto ya tiene `NpcPatrol`.

## Personajes principales

| Personaje | Papel en el hub | Qué aporta a la historia | Silueta y rasgos (propuesta) | Misiones |
|---|---|---|---|---|
| **La figura espiritual** | Guía narrativo del despertar | Explica el Mundo de las Memorias y el juramento. No es un dios ni un enemigo; aparece y desaparece | Forma luminosa, sin rasgos culturales fijos que la identifiquen con una deidad (F) | `MP_OLM_01` |
| **El artesano de jade** | Artesano y mejoras | Nota que algunas piedras perdieron sus marcas; trabaja piedra verde traída de lejos (D: jade y serpentina en La Venta). **Ya existe** en el prototipo con tres líneas de diálogo | Adulto de complexión media, sentado o encorvado sobre su trabajo, herramientas de pulido, collar de cuentas verdes | `MP_OLM_02`, `MS_OLM_01` |
| **La anciana** | Guía de relatos y Códice | Recuerda relatos que ya nadie puede repetir; su memoria empieza a fallar por la Sombra | Encorvada, bastón, manto, cabello recogido; se mueve poco | `MP_OLM_02`, `MS_OLM_02` |
| **El joven guerrero** | Dador de misiones | Volvió herido de una expedición; vio la entrada a la estructura y las criaturas corrompidas. Protege, no ataca | Joven, delgado, vendaje en el brazo, lanza o macana apoyada, postura cansada | `MP_OLM_02`, `MP_OLM_03`, `MS_OLM_05` |
| **El comerciante** | Comerciante | Trae materiales de rutas que se han vuelto peligrosas; habla del intercambio a larga distancia (D: obsidiana, piedra verde, conchas) | Bulto o mecapal a la espalda, sandalias, viajero | Tienda; `MS_OLM_01` |

## Habitantes de ambiente (propuesta)

| Habitante | Rol | Notas |
|---|---|---|
| Pescador o canoero | Vida del río; da `MS_OLM_03` | Usa el muelle y las canoas de la aldea (I) |
| Agricultora o agricultor | Milpa cerca de la aldea | Maíz, frijol, calabaza (D para la región; por confirmar el detalle) |
| Sanadora | Atiende al guerrero; plantas | Plantas por investigar antes de nombrarlas (I) |
| Niñas y niños | Juego y curiosidad | Reaccionan al Guardián animal |
| Alfarera | Junto a las vasijas de la aldea | Cerámica (D: los olmecas fabricaban cerámica) |

## Lo que ya existe en el proyecto

- **En la ciudad (`Olmeca_Ciudad`, 2026-10-29):** el artesano de jade (patrulla corta junto a su casa), la anciana (junto a las enramadas), el joven guerrero (junto al fogón, cerca de la plaza) y el comerciante (junto al muelle). Los coloca `OlmecaCityGenerator` (`Settlers`, posiciones en x, y del flujo).
- **Datos:** `Data/Narrative/Olmeca/NPC_*.asset` y `DLG_*.asset`, creados por `OlmecaNarrativeAssets` **solo si no existen**: se editan en el Inspector y regenerar la ciudad no los pisa. Para volver al texto del código, borrar el asset.
- **Arte provisional:** la misma silueta de 32×48 con paletas distintas (`PlaceholderSpriteFactory`: Artisan, Elder, Warrior, Merchant). Incumple a propósito la regla de siluetas propias hasta que llegue el arte final, que **hace el autor** (Arte_Olmeca_Demo §12).
- `Artesano` (prefab) sigue en el prototipo (`Olmeca_Prototipo`) con sus líneas fijas.
- **Figura espiritual (N5):** en la orilla del despertar, con sprite provisional claro (`PlaceholderSpriteFactory.Spirit`), sin rasgos de deidad. Por ahora no desaparece tras el juramento.
- Faltan los habitantes de ambiente.
