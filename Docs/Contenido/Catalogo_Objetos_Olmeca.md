# Catálogo de objetos olmecas (M5)

Los objetos del arco olmeca. Los crea `OlmecaRewardAssets` en `Data/Rewards/Olmeca/OBJ_<id>.asset`, con su icono en
`Art/UI/Iconos/ICO_<id>.png`; después se editan en el Inspector. Cada objeto lleva una **nota de origen** visible en el
asset:

- **D:** documentado (`Docs/Culturas/Olmeca.md`);
- **I:** interpretación;
- **F:** ficción del juego.

**Nada ficticio se presenta como hecho histórico.**

## Materiales

| Id | Nombre | Rareza | Origen | Tope | Valor | Uso |
|---|---|---|---|---|---|---|
| `jade` | Jade | Poco común | **D:** jade y serpentina traídos de lejos a La Venta | 30 | 12 | Reliquias, trueque |
| `obsidiana` | Obsidiana | Poco común | **D:** llegaba por intercambio. **I:** su uso como material de mejora | 30 | 10 | Talismán, trueque |
| `basalto` | Basalto | Común | **D:** basalto de los Tuxtlas para monumentos y columnas | 60 | 3 | Protección, trueque |
| `fibras` | Fibras vegetales | Común | **I:** cordelería; el detalle no está documentado aquí | 60 | 2 | Cordeles de amuletos |
| `semillas` | Semillas medicinales | Común | **I:** plantas medicinales sin identificar; no se nombran especies hasta investigarlas | 60 | 3 | Semilla preparada |
| `arcilla` | Arcilla | Común | **D:** cerámica olmeca | 60 | 2 | Semilla preparada |
| `esencia_olvido` | Esencia del olvido | Poco común | **F:** la Sombra del Olvido | 30 | 8 | Talismán, esencia de energía |
| `basalto_guardian` | Basalto de la Piedra Viva | Épico | **F:** material único del jefe | 3 | — | Despertar el Corazón (no se vende) |
| `ecos` | Ecos | Común | **F: moneda inventada; NO es una moneda histórica** | 9999 | — | Comprar |

**Plumas:** se omiten en el arco olmeca. Las plumas preciosas se asocian sobre todo a culturas y periodos
posteriores (tolteca, mexica). Se dejan para esos arcos, en lugar de forzarlas aquí.

## Consumibles

| Id | Nombre | Rareza | Efecto | Tope | Origen |
|---|---|---|---|---|---|
| `semilla_preparada` | Semilla medicinal preparada | Poco común | +20 de vida | 5 | **I:** preparación medicinal. **F:** su efecto |
| `esencia_energia` | Esencia de energía | Poco común | +40 de energía | 5 | **F** |
| `infusion_basalto` | Infusión de basalto | Raro | +5 de defensa durante 30 s | 3 | **F** |

## Reliquias (accesorios del animal; 2 ranuras)

| Id | Nombre | Rareza | Bonificación | Cómo se obtiene | Notas |
|---|---|---|---|---|---|
| `amuleto_jade` | Amuleto de jade | Poco común | Vida +10, defensa +2 | Artesano | **I:** cuentas de jade como adorno |
| `talisman_obsidiana` | Talismán de obsidiana | Poco común | Ataque +3 | Artesano | **F** |
| `proteccion_basalto` | Protección de basalto | Raro | Defensa +4 | Ofrenda del Guardián (piso 2), artesano | **F** |
| `reliquia_espiritual` | Reliquia espiritual | Raro | Energía +20 | Ofrenda oculta del piso 1, artesano | **F** |
| `marca_jaguar` | Marca del jaguar | Raro | **Solo Jaguar:** ataque +2, vida +10 | Ofrenda oculta del piso 2 | **F**; no se vende |
| `corazon_piedra_viva` | Corazón de la Piedra Viva | **Legendario** | Vida +20, defensa +3, energía +20 | Ofrenda final | **F**; única, no se vende |
| `corazon_despierto` | Corazón de la Piedra Viva, despierto | **Legendario** | Vida +30, defensa +4, energía +30 | Mejora con el artesano | **F**; única |

## Coleccionables culturales (Museo de la Memoria)

| Id | Nombre | Rareza | Dónde | Origen |
|---|---|---|---|---|
| `hacha_votiva` | Hacha votiva | Poco común | Ofrenda especial del piso 1 | **D:** hachas pulidas de jade y serpentina ofrendadas en La Venta |
| `figurilla_jade` | Figurilla de jade | Raro | Ofrenda oculta del piso 1 | **D:** figurillas de jade y serpentina en ofrendas (Ofrenda 4 de La Venta). **F:** esta pieza concreta |
| `fragmento_estela` | Fragmento de estela borrada | Común | Ofrenda especial del piso 2 | **F:** prueba del borrado |
| `espejo_mineral` | Espejo de mineral de hierro | Raro | Ofrenda oculta del piso 3 | **D:** espejos cóncavos de ilmenita y magnetita pulidos en La Venta |

Al obtenerlos se pone la bandera `registro.<id>`, que muestra su entrada en el Museo.

## Objetos de la historia

| Id | Nombre | Rareza | Dónde | Notas |
|---|---|---|---|---|
| `fragmento_menor` | Fragmento de memoria menor | Poco común | Los 3 fragmentos de la selva | **F**; con jade se hace la reliquia espiritual |
| `fragmento_primera_memoria` | Fragmento de la Primera Memoria | Legendario | Al liberar al Guardián | **F**; objeto central del arco (`Historia_Mundo.md`). Se lleva al altar-trono (MP_OLM_06) |
