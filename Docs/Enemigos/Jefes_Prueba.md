# Jefes de prueba (lámina del autor)

Fuente: `Docs/Enemigos y Gefes de mazmorra/Crónicas de México_ Jefes de Mazmorra.png`. Decisión del autor (2026-11-10): los seis jefes van a una **arena de pruebas aparte** y se asignan a su mazmorra cuando se diseñen los arcos II a V.

## Por qué no están en la mazmorra olmeca

Las figuras de la lámina son de culturas mexica, tolteca, teotihuacana y maya, no olmecas. El GDD §3 pide no mezclar culturas en un arco, no inventar dioses históricos y que los dioses no sean simples monstruos. Por eso aquí son **ecos ficticios inspirados** en cada figura, con nombres provisionales:

| Clave | Nombre provisional | Inspiración (tipo) | Nota |
|---|---|---|---|
| `SerpienteEmplumada` | Eco de la Serpiente Emplumada | Quetzalcóatl (D: deidad del Posclásico) | |
| `SenorLluvia` | Eco del Señor de la Lluvia | Tláloc (D: Teotihuacan y mexicas) | |
| `MuroCraneos` | Eco del Muro de Cráneos | Tzompantli (D: muro de cráneos mexica) | No es un ser: aquí, un eco (F) |
| `PerroFuego` | Eco del Perro de Fuego | Xólotl (D: mexica) | |
| `SenorMuertos` | Eco del Señor de los Muertos | Deidades mexicas de la muerte (I) | La lámina dice «Tlacatecuhtli», que no está documentado |
| `GuardianOfrenda` | Eco del Guardián de la Ofrenda | Chac mool (D: escultura tolteca y maya) | No es una deidad |

**Pendiente del autor:** nombres definitivos, a qué arco va cada uno, y si alguno debe ser un encuentro que no se resuelva con violencia (GDD §11).

## Cómo se juega

- Escena `Scenes/Regions/Pruebas/Arena_Pruebas_Jefes.unity`, generada por `TrialArenaGenerator` (menú **Crónicas → Generar arena de pruebas de jefes**). Usa el escenario del piso 3 sin su historia: la losa sellada no está.
- Se abre esa escena y se pulsa Play. En la antesala hay **seis placas**, cada una con una inscripción que presenta a su jefe. Al pisar una, ese jefe aparece en el centro de la arena; despierta al acercarse (7 m) y muestra su barra en el HUD. Solo hay uno a la vez.
- No está enlazada con el resto del juego ni da recompensas.

## Habilidades

`TrialBoss`, guiado por `BossData` (`Data/Enemies/BD_<Clave>.asset`; se editan en el Inspector). Fases por vida: 2 al 60 % y 3 al 30 %, con 1 s invulnerable al cambiar. Todo golpe se avisa en el suelo; los golpes del jugador no lo interrumpen.

| Jefe | Vida | Habilidades (fase desde la que aparece) |
|---|---|---|
| Serpiente Emplumada | 300 | Suspiro Divino (franja, 10) · Tormenta de Plumas (7 plumas en abanico, 6) · Luz Sagrada (círculo a su alrededor, 10) · Vuelo Majestuoso (2: vuela y cae sobre el jugador, 14) |
| Señor de la Lluvia | 320 | Lluvia Sagrada (4 círculos, 8) · Rayo Acuático (franja, 10) · Inundación (2: círculo de 4 m que frena al 50 % 3 s, 6) · Invocación de Nubes (3: 6 círculos, 8) |
| Muro de Cráneos | 280 | Golpe Espectral (círculo delante, 12) · Aura de Miedo (círculo que frena, 4) · Invocación de Calaveras (2: 2 murciélagos) · Resurrección (pasiva: vuelve una vez con el 40 %) |
| Perro de Fuego | 260 | Furia Canina (dos zarpazos, 9) · Salto Infernal (cae sobre el jugador, 12) · Llamas Ancestrales (5 círculos, 8) · Aullido Maldito (2: círculo que frena, 5) |
| Señor de los Muertos | 320 | Abrazo Mortal (círculo grande delante, 14) · Tormenta Espectral (10 proyectiles en todas direcciones, 5) · Ritual de Huesos (2: invulnerable 3 s) · Invocación Funeraria (2: 2 espíritus jaguar) |
| Guardián de la Ofrenda | 360 | Golpe Terrenal (círculo delante, 12) · Lanzamiento de Ofrendas (3 piedras, 8) · Terremoto (2: círculo de 4 m, 10) · Ritual de Protección (2: recibe el 30 % del daño 4 s) |

Números provisionales, a la escala ×10 del M5 (el Guardián de la Piedra Viva tiene 400).

## Arte

`Tools/ArtPipeline/recortar_jefes.py` → `Sprites/Jefes/Jefe_<Clave>_<Pose>1_03.png` (Reposo, Avance, Ataque y Especial), lienzos de 128 px de alto con los pies a 2 px. Usa las cuatro poses pequeñas de cada panel: la ilustración grande lleva un fondo pintado que no se separa bien.

- Solo de lado y una imagen por pose; los jefes miden de 53 a 84 px (el jugador, 54): la lámina los dibuja pequeños y agrandarlos emborronaría el pixel art.
- Varias poses especiales son sobre todo un efecto (la columna de agua, el aura morada, la luz) y el cuerpo apenas se ve.
- No hay pose de daño ni de caída: al caer se desvanecen. Los proyectiles usan el fragmento del eco borrado.
- Las invocaciones usan los enemigos de la lámina (murciélagos como calaveras, espíritus jaguar).
