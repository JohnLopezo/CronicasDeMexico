# Sistema de recompensas (M5)

Cómo recibe el jugador experiencia, objetos, Bendiciones y acceso a contenido nuevo. Los valores concretos de la
cultura olmeca están en `Docs/Contenido/Tabla_Recompensas_Olmeca.md` y `Docs/Contenido/Catalogo_Objetos_Olmeca.md`.
El inventario, la progresión y la fabricación tienen su propio documento en esta carpeta.

Etiquetas de origen, como en el resto de la documentación: **D** = documentado, **I** = interpretación,
**F** = ficción del juego.

## 1. Principios

- **Explorar rinde más que repetir.** Descubrir un piso, abrir ofrendas y completar misiones da más experiencia que
  vencer otra vez a los mismos enemigos. Los enemigos que reaparecen dan cada vez menos y al final nada
  (§5).
- **Nada aleatorio bloquea el avance.** Lo necesario para avanzar siempre está garantizado: la Bendición de la
  Tierra, el Fragmento de la Primera Memoria y los objetos de misión. El azar solo añade materiales y consumibles
  de más.
- **Cada recompensa única se entrega una sola vez en toda la partida,** aunque se recargue la escena o la partida.
  Para eso cada una lleva una clave única que se guarda (§4).
- **Sin castigo por caer (decisión 7).** Al caer no se pierde experiencia, materiales ni objetos. El castigo se puede
  activar en el futuro, pero ahora vale 0. Lo único que cuenta caer es la nota de la mazmorra (§6).
- **Datos y no código.** Todo el contenido se define en ScriptableObjects editables en el Inspector: objetos,
  tablas, paquetes, recetas, ofertas, Bendiciones y entradas del Museo. Una cultura nueva solo añade assets.

## 2. Arquitectura

```
Core (sin dependencias)                Data (ScriptableObjects)              Gameplay / UI
─────────────────────────              ────────────────────────              ─────────────
Inventory      ◄── IItemCatalog ─────── ItemCatalog / ItemData               EnemyReward, RewardChest,
Progression    ◄── IProgressionConfig ─ ProgressionConfig (+GuardianData)    RewardOnEnter, DungeonCompletion,
               ◄── IBlessingCatalog ─── BlessingCatalog / BlessingData       MemoryFragmentInteractable,
RewardService  ◄── RewardPackage ◄──── RewardBundle.ToPackage / LootTable    PlayerStats, PlayerAbility,
Economy        ◄── ICraftRecipe ─────── RecipeData       (EconomyCatalog)    BreakableBasalt, ServicePoint
               ◄── ITradeOffer ──────── TradeOfferData                       GameMenuController (HUD y menú)
QuestService.Completed ──► RewardService (recompensa de misión, IQuestDefinition.Reward)
```

- **Servicios.** `GameBootstrapper` crea `Inventory`, `Progression` y `RewardService`, los registra en
  `ServiceLocator` y en el guardado. Los tres son `ISaveParticipant` y usan `SaveData` versión 2. Las partidas de
  la versión 1 se migran: nivel 1, sin objetos y con la energía llena.
- **Catálogos** en `Resources/Recompensas/`:
  - `CatalogoObjetos`
  - `CatalogoBendiciones`
  - `CatalogoEconomia`
  - `CatalogoMuseo`
  - `ConfigProgresion`
- **Contenido olmeca** en `Data/Rewards/Olmeca/`. Lo crea `OlmecaRewardAssets` al generar las escenas, y cada asset
  solo si no existe. Después se edita en el Inspector y regenerar no lo pisa. Para volver a los valores del código,
  se borra el asset.
- **Entregas.** Todo pasa por `RewardService.Grant(paquete, claveÚnica, nivelDeZona, claveDeRepetición)`. Este método:
  - suma la experiencia (con las reducciones de §5);
  - mete los objetos en la bolsa, respetando el tope de cada uno;
  - pone las banderas de desbloqueo (recetas, ofertas, registros del Museo);
  - desbloquea la Bendición, si el paquete trae una.

  Cada entrega lanza un aviso (`Notified`) que el HUD muestra como un mensaje breve.
- **Desbloqueos** como banderas de `StoryState`, que ya se guardan:

  | Prefijo | Desbloquea |
  |---|---|
  | `receta.*` | recetas |
  | `oferta.*` | ofertas del comerciante |
  | `registro.*` | entradas del Museo |
  | `ofrenda.*` | ofrendas abiertas |
  | `roto.*` | basalto agrietado roto |
  | `descubrir.*` | zonas descubiertas |

## 3. Fuentes de recompensa

| Fuente | Componente | Qué da | Cuántas veces |
|---|---|---|---|
| Enemigo | `EnemyReward` (`LootTable` + nivel de zona) | Experiencia y materiales al azar | Cada vez, con menos experiencia al repetir (§5) |
| Ofrenda común / especial / oculta / del Guardián / final | `RewardChest` (`RewardBundle` fijo + `LootTable` opcional) | Lo fijo siempre; lo de la tabla, al azar | Una vez (`ofrenda.<id>`) |
| Descubrir un piso o una zona | `RewardOnEnter` | Experiencia | Una vez (`descubrir.<clave>`) |
| Fragmento de memoria | `MemoryFragmentInteractable` | Fragmento menor + 20 de experiencia | Una vez por fragmento |
| Jefe final (al liberarlo) | `DungeonCompletion` | Bendición de la Tierra, Fragmento de la Primera Memoria, experiencia y desbloqueos | Una vez (`jefe.<cultura>`) |
| Nota de la mazmorra | `DungeonCompletion.Finish` (lo llama la ofrenda final) | Paquete según C/B/A/S | Una vez (`nota.<cultura>`) |
| Misión | `QuestData.reward` (`RewardBundle`) | Lo que diga su paquete | Una vez (`mision.<id>`) |
| Fabricación y comercio | `Economy` + menú | Objeto a cambio de materiales o ecos | Sin límite |

**Ofrendas.** Son el «cofre» del juego. En La Venta se enterraban ofrendas de jade, figurillas y espejos (**D**). La
caja de lajas y la vasija tapada son interpretación (**I**). Hay cinco tipos:

- **Común:** vasija tapada, con consumibles y materiales.
- **Especial:** caja de lajas, fuera del camino principal; da un coleccionable y jade.
- **Oculta:** tras el basalto agrietado; da una reliquia o un coleccionable raro.
- **Del Guardián:** aparece al vencer a un jefe intermedio, el Jaguar de Piedra.
- **Final:** aparece al liberar al jefe de la mazmorra y la cierra.

Las ofrendas ocultas de los pisos 1 y 2 piden la Bendición de la Tierra, que se gana al final: son contenido para
volver. La del piso 3 se puede abrir antes que la final.

## 4. Unicidad y guardado

`RewardService` guarda las claves ya entregadas en `grantedRewards`. Hay dos garantías:

- **Lo único no se duplica.** Una ofrenda entrega al abrirse y su bandera se pone al cerrar el mensaje. Si se recarga
  con el mensaje abierto, la clave ya estaba guardada y no se entrega dos veces.
- **Lo único no se pierde.** Si el jefe se liberó pero la partida se cerró antes de recibir la recompensa,
  `DungeonCompletion` la entrega al volver a cargar.

## 5. Experiencia: nivel de zona y protección contra el farmeo (decisión 5)

`Progression.AddExperience(cantidad, nivelZona, claveRepetición)` aplica dos multiplicadores. Todos sus valores se
configuran en `ConfigProgresion`.

- **Por nivel.** Si el nivel del jugador supera el de la zona, el multiplicador es
  `max(mínimo, 1 − reducción × (nivel − nivelZona))`, con reducción 0,25 y mínimo 0,10.
- **Por repetición.** Cada enemigo concreto (escena/nombre) lleva la cuenta de las veces que se ha vencido. El
  multiplicador es `factor^veces`, con factor 0,5. Por debajo del corte (0,2) da 0: la cuarta vez ya no da nada. La
  cuenta se guarda en la partida.
- **Niveles recomendados:**

  | Zona | Nivel |
  |---|---|
  | Selva | 1 |
  | Piso 1 | 2 |
  | Piso 2 | 3 |
  | Piso 3 | 4 |

- **Sin reducciones.** Las ofrendas, los descubrimientos y las misiones no llevan clave de repetición ni nivel de
  zona, así que siempre dan su experiencia completa.

## 6. Nota de la mazmorra (C/B/A/S)

La calcula `DungeonCompletion.Grade(secretos, total, cayó)` cuando se abre la ofrenda final. Se cuentan los
secretos encontrados más un punto si no se cayó en toda la incursión:

| Nota | Condición |
|---|---|
| S | Todos los secretos y sin caer |
| A | Falta un punto |
| B | Al menos un punto |
| C | Ninguno |

- **Secretos olmecas** (3): las ofrendas especiales de los pisos 1 y 2 y la oculta del piso 3. Todos se pueden
  alcanzar antes de abrir la ofrenda final.
- **Caer** activa la bandera `incursion.caido`, que `IncursionReset` borra al salir a la selva.
- **Recompensa por nota:** cada nota tiene su paquete (`REC_nota_*`). La nota S también da el título «Memoria de
  piedra», una bandera reservada para el Museo.

## 7. Cierre de la mazmorra (decisión 3)

1. **El Guardián queda liberado.** Se pone `olm.guardian_liberado`, y `DungeonCompletion` entrega `REC_jefe_guardian`:
   - la Bendición de la Tierra (principal);
   - el Fragmento de la Primera Memoria;
   - 400 de experiencia;
   - los desbloqueos.

   Después empieza `MP_OLM_06`.
2. **Aparece la ofrenda final.** Al abrirla da el Corazón de la Piedra Viva y otros materiales, y calcula la nota.
3. **Se abre la pantalla de elección** de Bendición secundaria, entre tres. Después viene la de resultados. Si el
   jugador ya tenía una secundaria, pasa directo a resultados.

## 8. Rareza

Cinco niveles: **Común, Poco común, Raro, Épico, Legendario**. Se ven en el color del nombre del objeto (clases
`rareza-*` de `Hud.uss`). La rareza no cambia ninguna regla; solo orienta al jugador. Lo legendario es único: se da
una sola vez, no se vende y no se apila.

## 9. Interfaz

- **HUD:** barras de energía y experiencia, nivel y avisos breves (como máximo 3 a la vez, 2,6 s cada uno).
- **Menú (I / Select):** pestañas Bolsa y Bendiciones, más las de contexto:
  - Fabricar, en el taller del artesano;
  - Comerciar, en el puesto del comerciante;
  - Museo, en el santuario de Memoria.

  Además hay dos pantallas modales: la elección de Bendición y los resultados. El menú pausa el juego (`timeScale` 0).
- **Uso rápido:** la tecla **1** (cruceta izquierda) usa el mejor consumible que haga falta.
- **Mundo → menú:** el mundo abre el menú por el canal `EVT_Menu_Abrir` (`MenuTabs`).

## 10. Para una cultura nueva

1. Crear sus `ItemData`, `LootTable`, `RewardBundle`, `RecipeData`, `TradeOfferData`, `BlessingData` (con su
   `culture`) y su `CultureRecord`.
2. Añadirlos a los catálogos de `Resources/Recompensas`.
3. En sus escenas, poner `EnemyReward` con el nivel de la zona, `RewardChest`, `RewardOnEnter` y un
   `DungeonCompletion` con su `cultureId`.
4. La elección de Bendición muestra las secundarias de esa cultura (`BlessingCatalog.SecondaryChoices`). Las
   combinaciones crecen con cada cultura.
