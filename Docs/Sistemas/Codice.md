# Códice de la Memoria (E6)

Inspirado en las páginas del manual de Tunic. Son páginas escondidas que enseñan mecánicas sin interrumpir y cuentan
lo que se sabe de cada cultura. El GDD lo pone en el menú (§156) y en la progresión de «Sabiduría» (§127).

## Aviso cultural

- El **Códice de la Memoria es un objeto ficticio** del Mundo de las Memorias: el libro del Guardián.
- **No es un códice olmeca.** No se conservan códices olmecas, y los códices conocidos de Mesoamérica son de culturas
  y épocas posteriores. Para no mezclar culturas (`CLAUDE.md`), las páginas olmecas no dicen que los olmecas
  escribieran libros.
- Cada página dice su origen (documentado, interpretación o ficción del juego), como el Museo.

## Cómo funciona

- **Cómo se encuentra una página:** con su disparador, que es uno de estos dos:
  - un **aviso** de la historia: `examinar:id`, `llegar:id`, `vencer:id` o `hablar:id`;
  - una **bandera**, por ejemplo `ofrenda.p1_oculta`, que pone la ofrenda oculta al abrirse.
- **Sin escenas nuevas:** las páginas se esconden en lugares que ya existen, sin regenerar ninguna escena.
- **Guardado:** lo encontrado es la bandera `codice.<id>` de `StoryState`, que ya se guarda con la partida. Nueva
  partida empieza sin páginas.
- **Partidas antiguas:** al cargar una partida de antes del Códice, las páginas cuyo disparador es una bandera que ya
  se cumplía aparecen sin aviso.
- **Al encontrarla:** sale un aviso emergente («Nueva página del Códice: …»).
- **Pestaña Códice del menú:** muestra cuántas hay. Las encontradas se ven con su texto y su origen; las que faltan
  muestran «??? — pista».
- **Código:**
  - `Core/Narrative/CodexService`: interfaces `ICodexPage` y `ICodexCatalog`.
  - `Data/Narrative/CodexCatalog`: asset `Resources/Narrativa/CatalogoCodice`, que se edita en el Inspector.
  - `Editor/SceneGeneration/CodexAssets`: crea el asset solo si falta. Menú **Crónicas → Crear catálogo del Códice**.

## Páginas del arco olmeca

| Id | Título | Dónde (disparador) | Origen |
|---|---|---|---|
| ruptura | Escudos y afinidades | Asomarse al río en el despertar (`examinar:reflejo`) | Ficción |
| combo | El golpe que se encadena | Entrar al piso 1 (`llegar:mazmorra_piso1`) | Ficción |
| esquiva | Esquivar en el último instante | Ofrenda especial de El Umbral (`ofrenda.p1_especial`) | Ficción |
| bloqueo | Defender en el instante | Ofrenda oculta del piso 1, tras el basalto (`ofrenda.p1_oculta`) | Ficción |
| basalto | La piedra que escucha | Ofrenda oculta del Pantano (`ofrenda.p2_oculta`) | Ficción |
| altares | Altares que eran tronos | Examinar el altar-trono de la ciudad (`examinar:altar_trono`) | Interpretación |
| cabezas | Las cabezas colosales | Examinar el sello del Guardián (`examinar:sello_guardian`) | Documentado |
| jade | El jade azul-verde | Ofrenda oculta del piso 3 (`ofrenda.p3_oculta`) | Documentado |

### Fuentes de las páginas culturales (para contrastar)

- **Cabezas colosales:**
  - Se conocen 17, de San Lorenzo, La Venta, Tres Zapotes y Rancho La Cobata.
  - Son de basalto del cerro Cintepec, en la sierra de los Tuxtlas.
  - Que retraten a gobernantes es la interpretación más extendida.
- **Altares-trono:** David Grove propuso que los «altares» de La Venta eran tronos; hoy es la lectura más aceptada.
  El Altar 4 muestra una figura que sale de un nicho.
- **Jade:** la jadeíta azul-verde olmeca se ha relacionado con fuentes del valle del Motagua, en Guatemala.

**El autor debe contrastar los textos con fuentes antes de publicarlos** (`Docs/Culturas/Olmeca.md`).

## Pendiente

- Que el autor apruebe los textos, los lugares y si prefiere otro nombre en lugar de «Códice».
- Páginas de la selva. La selva no tiene un aviso propio fuera de las estelas, que comparten el id `estela`. Haría
  falta un id por estela o un rincón nuevo con `ReportOnEnter`, y eso obliga a regenerar la escena.
- Arte de página: icono en el aviso y en el menú.
