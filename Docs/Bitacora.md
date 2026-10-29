# Bitácora de sesiones

Registro corto de cada sesión de trabajo, para retomar en cualquiera de las dos PC (`Docs/DesarrolloMultiPC.md`). La memoria de Claude Code es local de cada PC; lo que deba saberse en la otra va aquí, en `CLAUDE.md` o en el resto de `Docs/`.

**Cómo se usa:**

- **Al empezar:** leer la última entrada, `git pull` y comprobar que el último commit coincide con el anotado.
- **Al terminar:** añadir una entrada **arriba** (la más reciente primero) y subirla en el mismo push que el trabajo. Así la otra PC ve el estado real al hacer `git pull`.
- Cada entrada es breve; el detalle técnico vive en los documentos de cada área (por ejemplo, `Docs/Arte_Olmeca_Demo.md` §12) y en los commits.

Plantilla:

```markdown
## AAAA-MM-DD — PC (i5-6500 / i7-10700)

- **Rama / último commit:** `rama` @ `abc1234`
- **Hecho:** …
- **Estado al cerrar:** pruebas (EditMode x/x, PlayMode x/x), push hecho o no, cambios locales sin subir.
- **Siguiente:** …
```

---

## 2026-10-29 — i7-10700

- **Rama / último commit al empezar:** `feature/prototipo-olmeca` @ `1d3eb5c` (al día con origin).
- **Hecho:** se crea esta bitácora, enlazada desde `CLAUDE.md` y como paso de la rutina diaria (`Docs/DesarrolloMultiPC.md` §5). Se corrigen avisos desactualizados en `Docs/DesarrolloMultiPC.md` §10 y `Docs/Arte_Olmeca_Demo.md` §12 (trabajo "sin commit en el i7" y la decisión sobre versionar `Salida/`, que ya va por LFS).
  Commit `88c918c` (bitácora y avisos); se descartó una regeneración sin cambios de `Olmeca_Ciudad.unity`.
- **Hecho (cámara de fondo):** usa el renderer sin efectos (índice 1 añadido a los tres URP Assets) y no pide HDR, MSAA, sombras ni texturas. Escena regenerada. En el i7, Alto pasa de 99–114 a 286–300 FPS y Medio de 202–222 a 271–349; Bajo igual (`Docs/Rendimiento.md`). La medición ahora activa `runInBackground` y ya no se pausa al perder el foco.
  Commit `6144f3f`.
- **Hecho (narrativa, fase 1):** la biblia del autor queda en `Docs/Historia_Mundo.md` (texto íntegro y decisiones de integración al inicio). Derivados del arco olmeca: `Docs/Historia/` (Capitulos, Misiones, Glosario), `Docs/Personajes/` (Protagonista, NPC_Olmecas), `Docs/Culturas/Olmeca.md`, `Docs/Enemigos/Olmeca.md`, `Docs/Mazmorras/Olmeca.md`. `Diseno.md` §2, §3, §4, §7.1 y §11 llevan notas de alineación (lo anterior se conserva como antecedente) y `CLAUDE.md` apunta a la biblia.
- **Decisiones:** nombres de la biblia; protagonista sin género; Mariposa monarca como Guardián y Vaquita a futuro; cinco arcos con las culturas del GDD dentro; vertical slice de 3 pisos y meta de 8; jefe = fragmento + habilidad fija + acceso + Bendición a elegir.
  Commit `6f74768`. Después el usuario decidió: **Guardián del vertical slice = Jaguar**; el prólogo del GDD se sustituye por **tutorial en los primeros pisos olmecas**.
- **Abierto:** epiclásico y tolteca dentro del arco III; afinidades de los arcos III–V.
- **Hecho (fase narrativa 2, N1–N3):** `StoryState` (banderas y avisos), `NpcData` y `DialogueData` con ramas; `DialogueInteractable` las usa (conserva las líneas fijas); `QuestData`, `QuestCatalog` y `QuestService`; `InscriptionInteractable` con id, `ReportOnDefeat` y `ReportOnEnter`; objetivo de misión en el HUD; `SaveService` (JSON con versión y respaldo). Boot registra los tres servicios; nada guarda ni carga aún en juego.
- **Estado:** EditMode 55/55 (nuevas: `NarrativeTests`, `SaveServiceTests`), PlayMode 29/29 (nueva: `HudObjectiveTests`; `BootTests` comprueba los servicios nuevos).
- **Siguiente:** N4 (NPC del asentamiento en la ciudad, diálogos y `MP_OLM_02`) y N5 (despertar), con plan y capturas; escombros de basalto; medir en Android (corresponde a esta PC). Benchmark del i5 cuando se trabaje allí.

## 2026-10-28 — i7-10700

- **Rama / último commit:** `feature/prototipo-olmeca` @ `1d3eb5c` (push hecho).
- **Hecho:** ruinas olmecas (plataforma, altar, estelas) y recursos de la aldea en el flujo de pixel art de Blender; trazado `Ciudad.json` y escena jugable `Olmeca_Ciudad` (etapas A–E); `PixelArtScreen` a la resolución del arte y medición de rendimiento; ceibas, enramadas, vasijas, troncos, muelle y canoas en la ciudad; canoa rehecha y enramada que se cruza por debajo (`DepthAnchors`, `RoofOverhead`).
- **Estado al cerrar:** EditMode 42/42, PlayMode 27/27. FPS del i7 anotados en `Docs/Rendimiento.md`.
- **Siguiente:** benchmark en el i5-6500 (referencia), Android, cámara de fondo en Alto, escombros y mosaico de las ruinas.

## 2026-10-27 — i7-10700

- **Rama / último commit:** `feature/prototipo-olmeca` @ `8401228` (push hecho).
- **Hecho:** animación direccional e interacción; NPC con diálogo, combate básico y `Docs/Diseno.md`; estabilización del proyecto (Fase 1), documentación de dos PC (`Docs/DesarrolloMultiPC.md`, `Docs/Rendimiento.md`) y cambio de dirección de arte a pixel art isométrico con el flujo de Blender (`Tools/ArtPipeline/`).
- **Siguiente:** ruinas olmecas y ciudad jugable.

## 2026-10-26 y anteriores

- Sin registro de PC. Commits: base técnica (Hito 1.0, `setup-git.ps1`) y prototipo jugable olmeca HD-2D (`7d0471b`), fusionado a `main` por PR #1 y #3.
