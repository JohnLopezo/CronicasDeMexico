# Ficha: Jaguar corrupto

## 1. Identidad

- **Papel:** es el «espíritu corrompido» de la selva y de los pisos 1 y 2 de la mazmorra, y sustituye a la silueta
  humana provisional.
- **Historia:** es un jaguar de la selva al que la Sombra del Olvido le ha borrado pedazos (**F**). El guerrero lo
  cuenta así: «grietas que brillan en la piel, como si les faltaran pedazos» (`OlmecaNarrativeAssets`).
- **Regla 14:** no es un olmeca enemigo ni un animal malvado; es una criatura que ya no reconoce a nadie.
- **Relación con el protagonista:** es una variante visual y jugable del Guardián Jaguar, con rasgos propios. El
  jugador ve en él lo que él mismo podría ser.

## 2. Anatomía y proporciones

Comparte el cuerpo del jaguar jugable (`personajes.py`), con estas diferencias:

- **Más enjuto:** 0,9 del ancho.
- **Patas más largas:** un 7 % más.
- **Postura:** lomo arqueado y erizado, orejas medio aplastadas y boca entreabierta en todas las poses.

## 3. Paleta

| Uso | Color |
|---|---|
| Pelaje | `#4B2E6B` (variación `#2E1C45`) |
| Vientre | `#7C68A0` |
| Anillo de las rosetas | `#D9A93A` (oro: el jaguar que fue) |
| Centro de las rosetas | `#6E4AA0` |
| Zonas borradas | Celdas grandes al azar que brillan en `#D9CCFF` |
| Grietas en flancos y lomo | Brillan en `#A98BE8` |
| Ojos | `#D9CCFF`, luz propia |
| Boca | `#A98BE8`, luz propia |

**Rampas de cuantización:** `corrupto`, `oro_corrupto` y `olvido`. El morado y el dorado vienen de
`Docs/Personages.png`; el violeta es el de la Sombra del Olvido en todo el juego.

## 4. Dimensiones en pantalla

Las mismas que el jaguar jugable:

- **Lienzo:** 128 × 112 px.
- **Ancla:** (64, 44).
- **Colisión:** cápsula de 0,9 m de alto y 0,24 m de radio.

## 5. Animaciones

| Animación | Cuadros | Cuadros/s | Uso |
|---|---|---|---|
| Reposo | 4 | 3 | Quieto |
| Caminar | 6 | 11 | Persigue o vuelve a su sitio |
| Acecho | 2 | 6 | Se agazapa durante el aviso del salto |
| Salto | 2 | 6 | En el aire y al caer |
| Golpeado | 2 | 10 | Al recibir daño |
| Muerte | 3 | 6 | Cae de costado; luego desaparece |

Las 5 direcciones dan 19 cuadros por dirección y 95 sprites en total.

## 6. Comportamiento: patrón reconocible

Usa `EnemyChaser` con el salto avisado activado:

1. **Persigue:** a 4,5 m empieza a perseguir y a 7 m deja de hacerlo. El contacto hace 10 de daño.
2. **Avisa:** si el jugador está entre 1,4 y 3,2 m, se **agazapa 0,6 s**. En el suelo aparece una **franja
   violeta** de la trayectoria.
3. **Salta:** recorre 3,4 m en línea recta a 9 m/s y hace 10 de daño si alcanza al jugador.
4. **Queda expuesto 0,7 s** al caer: es el momento de contraatacar.
5. **Espera 2,5 s** antes de poder saltar otra vez.

Además:

- **Interrumpir:** un golpe durante el aviso cancela el salto.
- **Esquivar:** basta con salir de la franja o usar la esquiva.
- **Al morir:** muestra la caída y desaparece; no reaparece en esa visita. En la selva tampoco vuelve al recargar,
  porque su bandera de vencido se guarda.
- **Botín:** el de `BOT_espiritu_corrompido`.

## 7. Implementación

- **Modelo y poses:** `personajes.py` (`jaguar(..., corrupto=True)`, `ANIMACIONES_CORRUPTO`).
- **Sprites:** `JaguarCorrupto_<Animación><cuadro>_<dirección>`.
- **Unity:**
  - `JaguarCharacterAssets.Corrupted()` crea los juegos de sprites.
  - `OlmecaJungleGenerator.CreateCorruptedSpirit` monta el enemigo en la selva y en la mazmorra: sprites,
    `EnemyChaser` con el salto y la caída visible, `TelegraphMarker` en franja y `CharacterAnimationDriver`.

## 8. Adaptación a otras culturas

La corrupción es un pelaje y un juego de grietas sobre el cuerpo de cualquier animal. Cada arco puede tener su
fauna corrupta (por ejemplo, un lobo o un coyote corruptos en el norte) con la misma paleta del olvido. Así se
reconoce al enemigo en todas las culturas.
