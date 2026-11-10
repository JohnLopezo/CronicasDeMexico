# Enemigos del arco olmeca

Fuente: `Docs/Historia_Mundo.md` §4.3, §7, §10; GDD `Docs/Diseno.md` §10. **F** = ficción del juego; **D** = documentado; **I** = interpretación.

## Criterios

- **Dos clases de hostilidad** (§4.3): criaturas que atacan por razones naturales (territorio, hambre, miedo) y criaturas **corrompidas** por la Sombra. Se distinguen a simple vista.
- **Señales de corrupción:** grietas luminosas, fragmentos de símbolos, colores ajenos al entorno y animaciones irregulares. Al vencer a un corrompido se puede recuperar un fragmento de memoria menor.
- **No todo se resuelve matando:** algunas criaturas naturales se apaciguan o se evitan, y una misión (`MS_OLM_03`) puede devolver la calma a una zona.
- **Sin "olmecas enemigos" genéricos** (regla 14): el guerrero del pueblo es aliado; si aparecen guerreros corrompidos, son ecos de la memoria y deben justificarse en la historia.
- **IA modular** (GDD §10): Idle → Patrol/Detect → Chase/Reposition → Telegraph → Attack → Recover → Defeated. Ataques importantes con aviso.

## Catálogo (propuesta)

| Enemigo | Clase | Arquetipo (GDD §10) | Dónde | Comportamiento | Tipo |
|---|---|---|---|---|---|
| **Espíritu jaguar corrompido** | Corrompido | Melee rápido | Selva, pisos 1–3 | Acecha y embiste. **Ya existe** como `EspirituJaguar` (`EnemyChaser`). **Desde el 2026-11-10 lleva el arte del Espíritu Jaguar de la lámina del autor** (el jaguar corrupto de Blender se retiró por decisión del autor): agazapado con la pose de defensa, salta con la de correr. En la selva (`Olmeca_Selva`, M1) hay tres con arte corrompido provisional (`PlaceholderSpriteFactory.CorruptedJaguarSpirit`: pelaje apagado y grietas violetas) que laten a ritmo irregular (`CorruptionPulse`), no reaparecen y dejan un fragmento de memoria | F (motivo felino: I) |
| Pecarí | Natural | Tanque ligero | Selva | Carga en línea recta si se le molesta; se puede evitar | D (fauna nativa) |
| Cocodrilo de pantano | Natural | Emboscada | Orillas y pantano | Espera en el agua; ataque rápido con aviso; no persigue lejos | D (*Crocodylus moreletii*, nativo; verificar distribución) |
| Murciélagos | Natural | Volador, enjambre | Cuevas y cámaras | Grupos pequeños; molestan más que dañan | D |
| **Eco borrado** | Corrompido | Mágico / distancia | Ruinas y mazmorra | Silueta incompleta que dispara fragmentos de símbolos; se desvanece y reaparece. **Implementado en M3** (`ErasedEcho`, El Pantano): brilla 0,6 s antes de cada disparo, salta entre tres puntos tras dos disparos o al recibir un golpe, y desvanecido no recibe daño. Arte provisional: la silueta humana violeta con franjas borradas (`PlaceholderSpriteFactory.ErasedEcho`) | F |
| **Basalto despierto** | Corrompido | Tanque | Mazmorra, pisos intermedios | Bloque de basalto columnar animado; lento, golpe de área anunciado | F (material: D) |
| **Enredadera de olvido** | Corrompido | Control | Pantano y templo hundido | Raíces que ralentizan o enraízan | F |

## Enemigos de la lámina del autor (2026-11-10)

Fuente: `Docs/Enemigos y Gefes de mazmorra/Crónicas de México_ Enemigos y Mazmorra.png`. Siete enemigos comunes, todos fauna de la selva salvo el espíritu jaguar (F). **Etapas 1 a 4 hechas** (arte, sistema común, habilidades y colocación).

- **Arte:** `Tools/ArtPipeline/recortar_enemigos.py` → `Sprites/Enemigos/Enemigo_<Nombre>_<Pose>1_03.png`. La lámina trae una imagen por pose y solo de lado; las cinco direcciones usan esa vista. Lienzos de 64 px de alto, pies a 2 px, 48 PPU.
- **Datos:** `Data/Enemies/ED_<Nombre>.asset` (`EnemyData`). Los crea `EnemyCharacterAssets` solo si faltan: se editan en el Inspector. `-executeMethod CronicasDeMexico.Editor.SceneGeneration.EnemyCharacterAssets.BuildAll` rehace sprites y animaciones sin tocar los valores.
- **Comportamiento:** `WildEnemy` (IA modular del GDD §10: reposo → persecución → aviso → golpe → recuperación). Todo ataque se avisa: círculo o franja en el suelo, o la pose de ataque antes del proyectil. Un golpe durante el aviso lo interrumpe.

| Enemigo | Zona prevista | Vida | Movimiento | Ataque | Defensa |
|---|---|---|---|---|---|
| Serpiente | Selva | 25 | Zigzag | Mordida en círculo, 8 | Se enrosca tras un golpe: mitad de daño 1,2 s |
| Cocodrilo | Pantano | 60 | Emboscada (detecta a la mitad, no persigue lejos) | Embestida en franja, 14 | Escamas: mitad de daño de frente |
| Espíritu jaguar | Piso 3 | 35 | Persecución | Embestida en franja, 12 | Se desvanece 1 s tras un golpe y reaparece junto al jugador |
| Mono | Selva | 22 | Guarda 4 m de distancia | Piedra (proyectil), 6 | Salta atrás si el jugador se le pega |
| Avispa | Selva | 12 | Vuela, rumbo errático | Picada en franja, 6 | Esquiva de lado |
| Guacamaya | Selva | 20 | Vuela | Picado en franja, 9 | Sube fuera de alcance 1,5 s |
| Murciélago | El Umbral | 10 | Vuela, rumbo errático | Mordida en círculo, 5 | Esquiva de lado |

**Habilidades especiales** (etapa 3, `EnemySpecial`; cada una con su aviso y su propia espera, y un golpe durante el aviso la interrumpe):

| Enemigo | Habilidad | Detalle |
|---|---|---|
| Serpiente | Escupitajo | Desde 1 a 4 m: proyectil de 4 que frena al 50 % durante 2 s |
| Cocodrilo | Coletazo | Si lo tienen a 1,6 m: círculo de 1,8 m a su alrededor, 10 |
| Espíritu jaguar | Zarpazo doble | A 1,3 m: dos círculos seguidos delante, 7 cada uno; el segundo apunta a donde esté el jugador |
| Mono | Abanico | Tres frutos a −20°, 0° y 20°, 5 cada uno |
| Avispa | Enjambre | Cuando una ataca, las de su clase a 4 m atacan a la vez |
| Guacamaya | Graznido | Anillo de 3 m: 3 de daño y frena al 50 % durante 2 s |
| Murciélago | Mordida que cura y chillido | La mordida le devuelve la vida que quita; el chillido, anillo de 2,2 m: 2 y frena al 60 % durante 1,5 s |

**Colocación** (etapa 4; `BuildFauna` en `OlmecaJungleGenerator` y `OlmecaDungeonGenerator`, con `EnemyCharacterAssets.Place`): se colocan en los tramos entre puntos del flujo, sin tocar los JSON de Blender, y solo donde un rayo encuentra suelo (si no, en un punto que se sabe transitable).

| Escena | Enemigos | Reaparecen | Botín |
|---|---|---|---|
| `Olmeca_Selva` | Serpiente y guacamaya entre el primer y el segundo claro; mono entre el segundo y el tercero; 3 avispas antes de la entrada | Sí, a los 20 s | `BOT_fauna` (8 de experiencia, fibras, semillas, ecos), nivel 1 |
| El Umbral (P1) | 2 murciélagos cerca de los espíritus de las salas del fondo | No | `BOT_fauna`, nivel 2 |
| El Pantano (P2) | 2 cocodrilos: uno camino del santuario de Vida y otro junto al espíritu | No | `BOT_fauna`, nivel 3 |
| Piso 3 | 2 espíritus jaguar en la antesala, camino del santuario de Memoria | No | `BOT_espiritu_corrompido`, nivel 4 |

Números provisionales, a la escala ×10 del M5. La piedra y el escupitajo usan el fragmento del eco como sprite provisional.

## Mini-jefes

| Mini-jefe | Piso (meta de 8) | Idea |
|---|---|---|
| **Jaguar de Piedra** | 4 (en el vertical slice, piso 2) | Criatura ficticia: escultura felina animada por la corrupción. Saltos, zarpazos y un rugido que levanta polvo (GDD §7.1). **Implementado en M3** (`StoneJaguar`, arena del Pantano; ver abajo) |
| **Centinela de basalto** (antes "Guardián de la Piedra" en el GDD) | 7 | Se renombra para no confundirse con el jefe final. Guardián menor que custodia el corazón de la mazmorra |

### Jaguar de Piedra en el juego (M3, 2026-10-30)

- **Arte:** escultura rígida modelada en Blender. Tiene cinco poses (acecho, zancada, agazapado, salto y zarpazo) en las cinco direcciones de pantalla, más la pose liberada (sentado y sin grietas). Al ser de piedra, cada pose solo gira las piezas y no hace falta esqueleto.
- **Despertar:** duerme hasta que el jugador entra en la arena. Entonces muestra su barra en el HUD y acecha.
- **Ataques:** todos se avisan con una marca en el suelo (`TelegraphMarker`) y una pose. No hace daño por contacto.
  - **Zarpazo** (cerca): círculo corto delante. Avisa 0,55 s y hace 2 de daño.
  - **Salto** (lejos): la marca aparece donde estaba el jugador. Avisa 0,8 s, cae ahí y hace 2 de daño. Después queda 0,9 s expuesto.
  - **Rugido:** anillo de 3,2 m que empuja y hace 1 de daño. Es uno de cada tres ataques, o uno de cada dos por debajo de la mitad de la vida.
- **Al vencerlo** (16 de vida, números provisionales):
  - Se libera: queda sentado, sin grietas, y deja un recuerdo breve.
  - Se activa `olm.jaguar_piedra_vencido`: no vuelve y se abre la bajada al piso 3.
  - No da el fragmento grande, que es del Guardián.

## Jefe final: el Guardián de la Piedra Viva

Figura **original** inspirada en la monumentalidad escultórica olmeca y en las fuerzas de la tierra, la lluvia y la memoria del territorio; **no** es una deidad olmeca documentada (Historia_Mundo §6; GDD §7.1, §10). Protege la Primera Memoria y se enfrenta al protagonista porque la corrupción le hace ver en él una amenaza.

- **Arena:** circular, con piedra, vegetación y agua.
- **Fase 1:** zarpazo, embestida y golpe de tierra.
- **Fase 2 (~60 % de vida):** sube el agua; ondas, rugido y fragmentos.
- **Fase 3 (~25 %):** secuencias combinadas y «Furia de la Primera Piedra», con zonas de impacto anunciadas.
- **Resolución:** se libera de la corrupción, revela un recuerdo y concede el fragmento, la habilidad de tierra y la elección de Bendición (`Docs/Mazmorras/Olmeca.md`).

Las fases vienen del GDD §10; los números son provisionales hasta el balance.

### El Guardián en el juego (M4, 2026-10-30)

- **Arte:** figura felina maciza y geométrica modelada en Blender, rígida como el Jaguar, de unos 3 m. Lleva un tocado plano y un disco en el pecho, la Primera Memoria que protege: violeta mientras está corrupto, jade al liberarse.
  - Es una figura original (F) inspirada en la monumentalidad olmeca (I), no una deidad ni una copia de una pieza concreta.
  - Poses: sentado, erguido, paso, embestida, golpe (alzado), rugido y liberado.
- **Despertar:** espera sentado en el centro de la arena circular, detrás de la losa de la antesala, y despierta al acercarse el jugador. Tiene 40 de vida, barra de jefe y no hace daño por contacto salvo en la embestida.
- **Fase 1:**
  - Zarpazo: círculo delante, 2 de daño.
  - Embestida: franja de 8 m hacia el jugador, 2 de daño. Si choca con una columna o el borde, queda aturdido 1,6 s.
  - Golpe de tierra: círculo de 3,5 m a su alrededor, 2 de daño.
- **Fase 2** (≤ 60 %):
  - Ruge y queda invulnerable 1,2 s.
  - Sube el agua del borde de la arena, que frena.
  - Ondas: dos anillos seguidos de 3,5 y 7 m, con el centro a salvo. Cada uno hace 1 de daño.
  - Fragmentos de piedra: cinco en abanico de 60°.
  - Rugido que empuja.
- **Fase 3** (≤ 25 %): avisos un 25 % más cortos, menos espera entre ataques y la **Furia de la Primera Piedra**: dos tandas de tres zonas, una sobre el jugador, 2 de daño.
- **Al vencerlo:**
  - Queda sentado con el disco de jade encendido y cuenta su recuerdo («guardaba la primera memoria…»).
  - Se activa `olm.guardian_liberado`, completa `MP_OLM_05` y no vuelve.
  - El fragmento, la habilidad de tierra y la Bendición llegan en M5.
