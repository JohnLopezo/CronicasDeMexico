# Ficha: Jaguar (Guardián jugable)

## 1. Identidad

- **Papel:** el protagonista despierta en el Mundo de las Memorias con forma de jaguar (`Historia_Mundo.md` §2).
  Es el Guardián del vertical slice.
- **Afinidades:** Tierra / Oscuridad. La Bendición de la Tierra la concede el jefe olmeca.
- **Inspiración:**
  - **D:** el jaguar (*Panthera onca*) es nativo de América y vivió en la región olmeca. Es el felino más robusto
    del continente: cabeza grande, mandíbula fuerte, patas cortas y gruesas, y rosetas con manchas en el centro.
  - **F:** que sea un Guardián y su forma de comportarse en el juego.

## 2. Anatomía y proporciones

Cuadrúpedo con anatomía y movimiento naturales: cadera y hombros como articulaciones, patas de dos huesos resueltas
con cinemática inversa, corvejón en las traseras y muñeca en las delanteras.

| Medida | Referencia real (D) | En el juego |
|---|---|---|
| Cabeza y cuerpo | 1,1–1,85 m | 1,15 m (×1,1 de escala) |
| Cola | 0,45–0,75 m | 0,62 m |
| Altura a la cruz | 0,63–0,76 m | 0,73 m |
| Cabeza | Grande y ancha | ×1,22 sobre lo natural, para que se lea en pantalla |

Para que se lea en pantalla, el cuerpo es algo más robusto y corto que en la realidad, y la cabeza más grande.

## 3. Paleta

| Uso | Color |
|---|---|
| Pelaje base | `#D39A3E` (variación `#B87A2C`) |
| Vientre, garganta y hocico | `#F2DFB4` |
| Centro de la roseta | `#A8642A` |
| Anillo de la roseta | `#24160E` |
| Orejas por detrás | `#1A100A` |
| Nariz | `#6A3E36` |
| Ojos | `#E4B436` |
| Interior de la boca | `#6B2424` |
| Contorno | `#140C08` |

**Rampas de cuantización:** `jaguar` y `crema_jaguar` (en `Tools/ArtPipeline/personajes.py`). Las rosetas son un
patrón Voronoi sobre la posición de reposo de cada vértice: no se deslizan entre cuadros.

## 4. Dimensiones en pantalla

- **Lienzo:** 128 × 112 px.
- **Ancla:** (64, 44), el suelo bajo el centro del cuerpo.
- **Tamaño visible:** unos 100 px de largo de perfil (E) con la cola y unos 75 px sin ella; de 40 a 60 px de alto
  según la dirección.
- **Colisión:** cápsula de 0,9 m de alto, 0,24 m de radio y centro a 0,45 m.

## 5. Animaciones

| Animación | Cuadros | Cuadros/s | Uso |
|---|---|---|---|
| Reposo | 4 | 3 | Quieto: respira y mueve la cola |
| Caminar | 6 | 10 | Al moverse; secuencia lateral (trasera izquierda, delantera izquierda, trasera derecha, delantera derecha) |
| Correr | 6 | 16 | Durante la esquiva; galope rotatorio con el cuerpo que se recoge y se estira |
| Zarpazo | 4 | 14 | Al atacar: anticipación, zarpa en alto, golpe y recuperación |
| Golpeado | 2 | 10 | Al recibir daño: retrocede con las orejas atrás |
| Muerte | 4 | 7 | Se tambalea, se hunde y cae de costado; se queda en el último cuadro |

Las 5 direcciones dan 26 cuadros por dirección y 130 sprites en total.

## 6. Comportamiento

El jaguar usa el controlador actual sin cambios: `PlayerMovement`, `PlayerAttack`, la esquiva, `PlayerStats` y
`PlayerAbility`.

- **Animación:** `CharacterAnimationDriver` traduce los sucesos a animaciones:
  - atacar → Zarpazo;
  - esquivar → Correr;
  - recibir daño → Golpeado;
  - morir → Muerte.

  `DirectionalSpriteAnimator` elige caminar o reposar.
- **Golpe de tierra:** de momento no tiene animación propia; usa el círculo ámbar en el suelo.

## 7. Implementación

- **Modelo y poses:** `Tools/ArtPipeline/personajes.py` (`jaguar()`, `pose()`, `ANIMACIONES_JAGUAR`).
- **Flujo de arte:** lo registra `generar_olmeca.py`. Los sprites quedan en `Art/Environment/Olmeca/Sprites/Personajes`
  como `Jaguar_<Animación><cuadro>_<dirección>`.
- **Vista previa:** `vista_personajes.py` hace una hoja por animación sin rehacer todo el flujo.
- **Unity:**
  - `JaguarCharacterAssets` crea `Data/Characters/DSS_Jaguar_<Animación>`.
  - `PlayerPrefabUpgrade` aplica al prefab `Jugador` los sprites, la cápsula y el `CharacterAnimationDriver`.
  - `PlacePlayer` ya no escala la raíz, porque los sprites están a la resolución del escenario.

## 8. Adaptación a otras culturas

El cuerpo paramétrico sirve para otros cuadrúpedos, como el lobo mexicano, el xoloitzcuintle o el pecarí.

- **Se cambia:** las tablas `TRONCO`, `PATAS`, `CABEZA` y `PELAJE`, y la forma de la cabeza. Para un felino
  distinto basta con otro pelaje.
- **Se conservan:** el sistema de poses, los ciclos de marcha y la integración en Unity.
