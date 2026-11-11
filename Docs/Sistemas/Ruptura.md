# Sistema de Ruptura (E4)

Inspirado en el escudo y la ruptura de Octopath Traveler II, adaptado a combate de acción. Es un sistema de juego
ficticio: las afinidades son las de la biblia (`Docs/Historia_Mundo.md` §5), no una clasificación histórica.

## Cómo funciona

- Cada enemigo tiene un **escudo** de puntos (rombos en el HUD) y una o más **debilidades** de afinidad.
- **Quitan un punto:**
  - cada golpe del jugador con una afinidad débil;
  - un **bloqueo perfecto** (E3), con cualquier Guardián y también contra los jefes.
- Los golpes de otra afinidad hacen su daño normal pero no tocan el escudo.
- **Afinidad del golpe:**
  - el golpe normal, el combo y la habilidad especial llevan la del Guardián elegido (`GuardianData.InitialAffinity`);
  - el golpe de tierra de la Bendición es siempre **Tierra**.
- **Sin escudo, el enemigo queda roto** 2,5 s (3 s los jefes):
  - recibe **×1,5 de daño** (`Health.Vulnerability`);
  - los enemigos comunes quedan además **aturdidos** (`IStaggerable`). Los jefes no, porque sus ataques son
    secuencias por fases.
- Al terminar, el escudo vuelve entero.
- **HUD:** panel arriba al centro, bajo la barra del jefe. Muestra el nombre del último enemigo golpeado, sus rombos,
  sus debilidades en color y «¡RUPTURA!» mientras está roto. Se oculta a los 4 s sin golpearlo o al caer.

## Afinidades de los Guardianes

| Guardián | Afinidad |
|---|---|
| Balam (jaguar) | Tierra |
| Xali (ajolote) | Agua |
| Naira (lobo), Yaret (quetzal) | Viento |
| Ameyali (mariposa), Itzel (águila) | Luz |
| Ocel (xoloitzcuintle) | Oscuridad |

Ningún Guardián es de Fuego: por eso ningún enemigo olmeca es débil al fuego por ahora.

## Debilidades (propuesta de Claude, pendiente del autor)

| Enemigo | Escudo | Débil a | Razón de juego |
|---|---|---|---|
| Serpiente | 2 | Viento, Luz | Rápida y en zigzag; la luz la deslumbra. |
| Cocodrilo | 4 | Tierra, Viento | Acorazado; los golpes de tierra lo voltean. |
| Espíritu jaguar | 3 | Luz, Agua | Espíritu del olvido: la luz y el reflejo del agua (memoria) lo deshacen. |
| Mono agresivo | 2 | Tierra, Oscuridad | Ataca de lejos; Balam lo puede castigar. |
| Avispa gigante | 1 | Viento, Agua | Débil y en enjambre: se rompen en cadena. |
| Guacamaya hostil | 2 | Agua, Oscuridad | Ave de viento: el viento no la daña más. |
| Murciélago vampiro | 1 | Luz, Viento | Criatura de cueva. |
| Espíritu corrompido (selva, incursiones) | 3 | Luz, Agua | Igual que el espíritu jaguar. |
| Eco borrado | 3 | Luz, Agua | Olvido. |
| Espíritu del Jaguar (mini-jefe) | 5 | Luz, Tierra | Balam lo puede romper con su afinidad. |
| Guardián de la Piedra Viva (jefe) | 6 | Agua, Viento | Jefe de tierra. Con Balam se rompe con bloqueos perfectos, que es la habilidad que enseña el arco. |

Con Balam, el Guardián del vertical slice, rompen por afinidad el cocodrilo, el mono y el mini-jefe. Los demás se
rompen con bloqueos perfectos o eligiendo otro Guardián.

## Dónde se cambia

- **Enemigos comunes:** `shieldPoints`, `weaknesses` y `breakSeconds` en sus `ED_*.asset` (`Data/Enemies`), desde el
  Inspector. Los valores por defecto para un asset nuevo están en `EnemyCharacterAssets` (`.Breaks(...)`).
- **Espíritu corrompido, eco, mini-jefe y jefe:** en código, en el `Awake` de `EnemyChaser`, `ErasedEcho`,
  `StoneJaguar` y `StoneGuardian` (`BreakGauge.Ensure`). Están marcados como `shortcut:` hasta que tengan datos
  propios.
- **Jefes de la arena de pruebas** (`TrialBoss`): sin ruptura, porque no están enlazados con el juego.

## Pendiente

- Que el autor apruebe o corrija la tabla.
- Descubrir debilidades jugando (Octopath las oculta hasta acertarlas). Necesitaría guardarlas en la partida.
- Efecto visual de ruptura sobre el enemigo; hoy son la pausa, la sacudida y el HUD.
