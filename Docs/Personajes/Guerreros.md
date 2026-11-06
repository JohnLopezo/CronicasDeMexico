# Guerreros jugables

**Decisión del autor (2026-11-06):** el protagonista es una persona, un guerrero o guerrera con nombre propio, ligado al espíritu de su animal. Sustituye a la forma animal original porque funciona mejor en el juego. El animal se expresa en el atuendo, los poderes y el espíritu que aparece en las hojas de muerte. Esta dirección seguirá mejorándose: las mejoras nuevas parten de aquí, no del jaguar cuadrúpedo.

Auditoría de animaciones y propuesta técnica: `Docs/Personajes/Auditoria_Animaciones.md`. Historia y reglas: `Docs/Historia_Mundo.md` §2 y §16, y `Docs/Personajes/Protagonista.md`. Atributos: `Docs/Sistemas/Sistema_Progresion.md` §2.

## Estado de cada guerrero

| Guerrero | Id | Animal | Afinidad inicial | Ataque | Muerte |
|---|---|---|---|---|---|
| Balam | `jaguar` | Jaguar | Tierra | 2 cuadros | Hoja propia |
| Xali | `ajolote` | Ajolote | Agua | 2 cuadros | Hoja propia |
| Naira | `lobo` | Lobo mexicano | Viento | **1 cuadro** | **Provisional** |
| Ameyali | `mariposa` | Mariposa monarca | Luz | **1 cuadro** | **Provisional** |
| Itzel | `aguila` | Águila real | Luz | 2 cuadros, con chispa | Hoja propia |
| Yaret | `quetzal` | Quetzal | Viento | 2 cuadros, con restos verdes | Hoja propia |
| Ocel | `xolo` | Xoloitzcuintle | Oscuridad | 2 cuadros, con restos morados | **Provisional** |

Balam conserva el id `jaguar`: es el de las partidas guardadas y el de `OBJ_marca_jaguar`. No cambiarlo.

**Provisional** significa la pose de Daño tumbada (`caido()`), que se ve rígida.

## Cómo está hecho

- **Arte:** `Tools/ArtPipeline/recortar_guerreros.py` recorta `Docs/Guerreros` a `Sprites/Guerreros`, a 48 PPU y con 54 px de alto en reposo. Se escriben Norte, Este y Sur; las diagonales reutilizan el Este. Correr, atacar, recibir daño y morir solo existen de lado.
- **Datos:** `GuerreroCharacterAssets` crea un `GUA_<id>` y sus `DSS_Guerrero_*`, y el catálogo `Resources/Personajes/CatalogoGuardianes`. Los textos, afinidades y atributos se editan en su tabla: el asset se sobrescribe.
- **Elección:** al principio: sin partida guardada, es lo primero que se ve; con ella, tras Nueva partida. Al pulsar Play desde una escena de juego también se pide, con el juego en pausa (`StartMenuController`). Después, en la pestaña Guardián del menú de juego (`GameMenuController`).
- **Juego:** `Progression.Profile` es el elegido y se guarda en `guardianId`. `PlayerStats` aplica sus sprites al aparecer el jugador en cada escena. Cambiar a mitad de partida no cura ni recupera energía.
- **Defensa:** Q, clic derecho o LB, mantenidos: quieto, sin atacar y a mitad de daño (`PlayerDefense`). Funciona con los siete; **solo Balam muestra su pose**.
- **Victoria:** 1,5 s al abrir la ofrenda final, antes de los resultados. **Solo Balam**; los demás pasan directo. Ambas poses son provisionales y están en la tabla `GUERREROS` del script.
- **Marcas:** hay una por guerrero; `RewardService` retira del paquete las de otro Guardián.

## Cómo mejorarlos

### Regenerar después de cambiar arte o datos

```bash
# 1. Recortar (también escribe las hojas de revisión)
"<Blender>/5.2/python/bin/python.exe" -I Tools/ArtPipeline/recortar_guerreros.py Assets/_Project/Art/Environment/Olmeca/Sprites/Guerreros <hoja_revision.png>
# 2. Rehacer sets, GUA_, catálogo y prefab (sin regenerar escenas; con Unity cerrado)
"$U" -batchmode -quit -projectPath . -executeMethod CronicasDeMexico.Editor.SceneGeneration.PlayerPrefabUpgrade.Ensure -logFile <ruta>/guerreros.log
# 3. Ver a los siete junto a los NPC: menú Crónicas → Capturar guerreros (pasarela)
```

### Añadir una hoja de muerte

Pendientes: **Naira, Ameyali y Ocel**. En su entrada de `GUERREROS`, añadir `"muerte"` como en Balam: hoja, `factor` (px de hoja por px de juego), `tol` y `cajas` de Caída y Desplome. El cuadro de Daño va primero automáticamente.

### Rehacer una ficha

Si la nueva ficha cambia de distribución, hay que medir de nuevo sus cajas: le pasó a Ameyali, cuyas coordenadas dejaron de valer. Los nombres de los PNG de `Docs/Guerreros` están en la tabla del script.

**Las fichas se recortan mejor sobre fondo liso y claro, sin efectos ni suelo.** Casi todos los defectos actuales vienen de separar la figura del fondo y de sus poderes.

### Añadir un guerrero

1. Entrada en `GUERREROS` (`recortar_guerreros.py`).
2. Fila en `Guerreros` (`GuerreroCharacterAssets`), con presupuesto de atributos igual al de los demás; `GuerreroAssetsTests` lo vigila.
3. Marca en `OlmecaRewardAssets`: objeto `marca_<id>` y línea en la ofrenda `p2_oculta`.
4. Caso en `GuerreroAssetsTests.Guardian_HasEveryAnimation`.

## Mejoras previstas

- **Hojas de muerte** de Naira, Ameyali y Ocel.
- **Más cuadros:** reposo animado (hoy 1), caminar de frente y de espalda (hoy 2 aproximados), ataque de Naira y Ameyali, y ataque hacia el norte y el sur (hoy usan la pose de lado).
- **Atributos:** ajustar jugando. A 8 de ataque, un espíritu de 30 cae en 4 golpes y no en 3.
- **Estilo:** los guerreros se ven más oscuros y recargados que los NPC.
- **Estilos de combate propios** (`Docs/Diseno.md` §4): hoy todos pelean igual y solo cambian los números. Xali, Itzel y Yaret deberían atacar o apoyar a distancia. Necesita arte de efectos; es una etapa aparte.
- **Efectos ya dibujados en las fichas** y sin usar: Defensa, Habilidad Especial, Victoria y el espíritu animal de las hojas de muerte.
- **Marca al cambiar de guerrero:** si se cambia tras abrir la ofrenda del piso 2, el nuevo guerrero no recibe la suya.
- **Limpieza:** quitar el arte del jaguar jugable (`Jaguar_*`, `DSS_Jaguar_*`, `Fichas/Jaguar.md`) cuando la dirección esté asentada. Hoy lo usa `Capturar jaguares (pasarela)`, y el flujo de Blender lo sigue generando. El jaguar corrupto y el Jaguar de Piedra son enemigos y se quedan.
