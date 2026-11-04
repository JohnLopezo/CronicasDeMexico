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
| **El joven guerrero** | Dador de misiones | Volvió herido de una expedición; vio la entrada a la estructura y las criaturas corrompidas. Vigila esa entrada y pelea con lo que se acerque (decisión del autor, 2026-11-04) | Ficha `Docs/NPC/Guerrero.png`: joven con lanza, escudo y cinta turquesa, sin vendaje | `MP_OLM_02`, `MP_OLM_03`, `MS_OLM_05` |
| **El comerciante** | Comerciante | Trae materiales de rutas que se han vuelto peligrosas; habla del intercambio a larga distancia (D: obsidiana, piedra verde, conchas) | Bulto o mecapal a la espalda, sandalias, viajero | Tienda; `MS_OLM_01` |

## El guardia de la aldea (cambio de historia, 2026-11-04)

**Decisión del autor:** quien protege la aldea es **el guardia**, un habitante común del que puede haber uno o muchos. **No confundir con los Guardianes** (el protagonista Jaguar, el Guardián de la Piedra Viva). En datos y código se llama `guardia` / `NpcDefender`, nunca "Guardian".

- **Por qué:** el joven guerrero deja el pueblo al dar `MP_OLM_03`, así que él no puede defenderlo después.
- **Hoy hay 3:** dos en el sendero del oeste y uno cerca de la plaza. Comparten `NPC_guardia` y diálogo. Para añadir más, basta otra línea en `OlmecaCityGenerator.Settlers`.
- **Incursiones:** ver `Docs/Historia/Misiones.md`.

## Habitantes de ambiente

Todos patrullan una ruta corta y dicen frases propias (F); aún no dan misiones. Las rutas las vigila `Settlers_WalkTheirRoutes_WithWalkingAnimation`.

| Habitante | Rol | Notas |
|---|---|---|
| Pescador | Vida del río; dará `MS_OLM_03` | Junto al muelle (I). Sombrero de ala ancha de la ficha: interpretación sin contrastar |
| Agricultor | Milpa cerca de la aldea | Maíz, frijol, calabaza (D para la región; por confirmar el detalle). Mismo sombrero (I) |
| Curandera | Atiende al guerrero; plantas | Plantas por investigar antes de nombrarlas (I). Aún no cura: afectaría al equilibrio |
| Anciano | Guarda la tradición, en la plaza | Personaje aparte de la anciana de la historia (F) |
| Sacerdote | Guía espiritual, junto al altar-trono | Ofrendas enterradas (D en La Venta). El penacho de plumas de la ficha es **interpretación por contrastar**: puede leerse como de otra cultura |
| Mensajero | Corre entre la plaza y la aldea | Sus pergaminos son **interpretación por contrastar**; los diálogos hablan de "avisos", nunca de cartas |
| Comerciante mujer | Vendedora de ambiente, sin tienda | La tienda sigue siendo del comerciante |
| Niñas y niños (9) | Juego y curiosidad | Reaccionan al Guardián animal |
| Alfarera | Pendiente, sin ficha | Junto a las vasijas de la aldea. Cerámica (D: los olmecas fabricaban cerámica) |

## Lo que ya existe en el proyecto

- **En la ciudad (`Olmeca_Ciudad`, 2026-10-29):** el artesano de jade (patrulla corta junto a su casa), la anciana (junto a las enramadas), el joven guerrero (junto al fogón, cerca de la plaza) y el comerciante (junto al muelle). Los coloca `OlmecaCityGenerator` (`Settlers`, posiciones en x, y del flujo).
- **Datos:** `Data/Narrative/Olmeca/NPC_*.asset` y `DLG_*.asset`, creados por `OlmecaNarrativeAssets` **solo si no existen**: se editan en el Inspector y regenerar la ciudad no los pisa. Para volver al texto del código, borrar el asset.
- **Arte final (2026-11-04):** las fichas del autor están en `Docs/NPC/`. `Tools/ArtPipeline/recortar_npc.py` las recorta a `Sprites/Npc` (lienzo 48×64, pies a 2 px; 32 PPU, que con la escala de la raíz da un píxel de pantalla por píxel de sprite) y `NpcCharacterAssets` arma sus juegos de sprites.
  - **Caminata:** de lado, con los cuadros de la ficha cuando son coherentes; de frente, de espalda y en Guardia y niños, pasos generados desde el reposo (`pasos()`, sin balanceo de brazos). Solo la Curandera tiene cuadros reales en todas las vistas.
  - **Faltan fichas de:** el artesano de jade, la anciana y la figura espiritual. Siguen con arte provisional (`PlaceholderSpriteFactory`). Mejorarían el resultado: caminata de frente y de espalda de todos, de lado para niños y guardia, y una pose de ataque real del guardia (hoy señala con la mano).
- `Artesano` (prefab) sigue en el prototipo (`Olmeca_Prototipo`) con sus líneas fijas.
- **Figura espiritual (N5):** en la orilla del despertar, con sprite provisional claro (`PlaceholderSpriteFactory.Spirit`), sin rasgos de deidad. Por ahora no desaparece tras el juramento.
- **Joven guerrero en la selva (M1):** al dar `MP_OLM_03` se adelanta a la entrada de la estructura (`Olmeca_Selva`, con `DLG_guerrero_joven_entrada`). En la ciudad deja de estar al volver a cargarla (`ActiveWhenFlags` con `olm.guerrero_en_selva`).
- **Quietos a propósito:** la figura espiritual y el joven guerrero de la selva (vuelve a su sitio si pelea).
