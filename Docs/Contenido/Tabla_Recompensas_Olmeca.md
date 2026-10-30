# Tabla de recompensas olmeca (M5)

Valores del arco olmeca. Assets en `Data/Rewards/Olmeca/`:

| Prefijo | Qué es |
|---|---|
| `BOT_*` | Tablas de botín |
| `REC_*` | Paquetes fijos |
| `RCT_*` | Recetas |
| `OFE_*` | Ofertas |
| `BEN_*` | Bendiciones |
| `CUL_olmeca` | Entrada del Museo |

Las reglas generales están en `Docs/Sistemas/Sistema_Recompensas.md`. Lo aleatorio nunca es necesario para avanzar.

## Niveles recomendados por zona

| Zona | Nivel |
|---|---|
| Selva | 1 |
| Piso 1 «El Umbral» | 2 |
| Piso 2 «El Pantano» | 3 |
| Piso 3 «La antesala» y arena | 4 |

## Botín de enemigos (`EnemyReward`)

| Enemigo | Tabla | Experiencia | Garantizado | Probabilidades |
|---|---|---|---|---|
| Espíritu corrompido (selva, pisos 1 y 2) | `BOT_espiritu_corrompido` | 15 | — | Basalto 50 % (1–2), fibras 35 % (1–2), semillas 25 %, esencia del olvido 12 %, ecos 60 % (2–5) |
| Eco borrado (piso 2) | `BOT_eco_borrado` | 35 | — | Esencia del olvido 60 % (1–2), obsidiana 35 %, jade 10 %, esencia de energía 15 %, ecos 80 % (4–8) |
| Jaguar de Piedra (piso 2) | `BOT_jaguar_piedra` | 150 | 3 basalto, 2 jade, 1 esencia de energía, 30 ecos | — |

- **Reducción de experiencia:** se reduce por nivel sobre la zona y al repetir el mismo enemigo (0,5 por vez;
  la cuarta vez da 0).
- **Materiales:** se siguen dando al repetir. Su valor es bajo y su tope limita acumularlos.

## Ofrendas (`RewardChest`)

| Piso | Tipo | Id | Lo fijo (`REC_ofrenda_*`) | Tabla extra | Condición |
|---|---|---|---|---|---|
| 1 | Común (vasija) | `p1_comun` | Semilla preparada | `BOT_ofrenda_comun`: 10 xp; semillas, fibras, arcilla, semilla preparada 30 %, 5–10 ecos | Sala de llegada |
| 1 | Especial (caja) | `p1_especial` | 20 xp, **Hacha votiva** | `BOT_ofrenda_especial`: 20 xp; 1 jade seguro; obsidiana, esencia, semilla al 50 %; 10–20 ecos | Sala del grupo, junto a la alcoba |
| 1 | Oculta | `p1_oculta` | 40 xp, **Figurilla de jade**, **Reliquia espiritual** | — | Tras el basalto agrietado: **necesita la Bendición de la Tierra** |
| 2 | Especial | `p2_especial` | 20 xp, **Fragmento de estela**, infusión de basalto | `BOT_ofrenda_especial` | Sala del santuario de Vida |
| 2 | Oculta | `p2_oculta` | 40 xp, **Marca del jaguar**, esencia de energía | — | Tras el basalto agrietado: **Bendición de la Tierra** |
| 2 | Del Guardián | `p2_guardian` | 30 xp, **Protección de basalto**, 2 jade | — | Aparece al vencer al Jaguar de Piedra |
| 3 | Oculta | `p3_oculta` | 40 xp, **Espejo de mineral de hierro**, 2 semillas preparadas | — | Tras el basalto agrietado, junto al santuario: se abre en cuanto se tiene la Bendición |
| 3 | Final | `p3_final` | **Corazón de la Piedra Viva**, basalto de la Piedra Viva, 3 jade, 50 ecos; desbloquea la mejora del Corazón | — | Aparece al liberar al Guardián; cierra la mazmorra |

## Descubrimientos (`RewardOnEnter`, una vez)

| Piso | Experiencia |
|---|---|
| Piso 1 | 30 |
| Piso 2 | 45 |
| Piso 3 | 60 |
| Fragmento de memoria (selva, cada uno) | 20 y 1 fragmento menor |

## Jefe y cierre

| Momento | Paquete | Contenido |
|---|---|---|
| Liberar al Guardián | `REC_jefe_guardian` | 400 xp, **Bendición de la Tierra** (principal, obligatoria), **Fragmento de la Primera Memoria**. Banderas: `registro.cultura.olmeca`, `olm.primera_memoria`, `receta.reliquia_espiritual`. Empieza MP_OLM_06 |
| Ofrenda final | `REC_ofrenda_final` | Lo de la tabla anterior, nota y **elección de una Bendición secundaria entre tres** |

**Nota** (secretos: las especiales de los pisos 1 y 2 y la oculta del 3, más no haber caído):

| Nota | Experiencia | Objetos | Extra |
|---|---|---|---|
| C | 0 | 20 ecos | — |
| B | 30 | 40 ecos, semilla preparada | — |
| A | 60 | 60 ecos, 2 esencias de energía, 1 jade | — |
| S | 100 | 100 ecos, infusión de basalto, 2 jade | Título «Memoria de piedra» (`titulo.memoria_de_piedra`) |

## Misiones (`QuestData.reward`)

| Misión | Experiencia | Objetos | Desbloquea |
|---|---|---|---|
| MP_OLM_02 El asentamiento inquieto | 40 | 3 semillas, 2 arcilla | — |
| MP_OLM_03 Rastros en la selva | 60 | 15 ecos, 2 fibras | Receta del amuleto de jade |
| MP_OLM_04 La ciudad bajo la selva | 100 | Esencia de energía | Receta de la esencia de energía |
| MP_OLM_06 La primera memoria | 120 | 40 ecos | Recetas del talismán y de la protección, oferta de la infusión, `registro.restauracion_altar` |

- **MP_OLM_01 y MP_OLM_05** no tienen paquete propio. La 01 es el despertar, y la recompensa de la 05 es la del jefe.
- **MP_OLM_06 «La primera memoria»** (nueva). Empieza al liberar al Guardián y tiene dos pasos:
  1. Hablar con la anciana, que recuerda el final de su relato (bandera `olm.anciana_recordo`).
  2. Examinar el altar-trono de la plaza, donde se deja el fragmento.

  Al terminar pone `olm.memoria_restaurada`. El altar cambia de texto desde el primer paso.
