# Recorta los sprites de los guerreros jugables de las fichas de Docs/Guerreros, a la resolución del escenario.
# Solo usa numpy; se ejecuta con el Python que trae Blender:
#   "<Blender>/5.2/python/bin/python.exe" -I Tools/ArtPipeline/recortar_guerreros.py Assets/_Project/Art/Environment/Olmeca/Sprites/Guerreros <hoja_revision.png>
# Escribe Guerrero_<Nombre>_<Animación><cuadro>_<dir>.png (01 Norte, 03 Este, 05 Sur). Las fichas solo animan la vista
# lateral: las vistas Norte y Sur tienen reposo y pasos_frente() (aproximación desde el reposo), y el ataque, la carrera y el daño
# existen solo en 03. El pivote normalizado (centro, pies a 2 px) es igual en todos los lienzos.
# Muerte: su Daño, y Caída y Desplome de la hoja de muerte (el espíritu sería un efecto aparte). Quien no tiene hoja cae con
# caido(). El suelo y los efectos finos se quitan con limpiar().
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # con -I, Python no añade la carpeta del script
import recortar_npc as npc  # noqa: E402

FICHAS = os.path.join(npc.RAIZ, "Docs", "Guerreros")
ALTO = 54                 # px de juego de la figura en reposo: como el Guerrero de los NPC
LIENZO_VISTA = (48, 64)
LIENZO_ACCION = (80, 64)  # la capa y las zancadas de lado no caben en 48
LIENZO_ATAQUE = (112, 64)
OSCURO = 70               # valor bajo el que un píxel cuenta como oscuro al reducir (contorno u ojo)
CONTORNO = 45             # valor bajo el que la inundación del fondo no pasa: es el contorno del personaje
DOMINANTES = 6            # colores del fondo que cuentan para los huecos encerrados
FINO = 3                  # px de ficha: lo más delgado que esto y alejado del cuerpo (línea de suelo, arco) se quita

# vistas: y0, y1 y tramos x de Norte, Este y Sur. fila: y0, y1 de reposo, caminata, carrera y ataque.
# dano: caja y su escala frente a la fila (ese panel está dibujado más grande). tol: distancia de color al fondo de la
# ficha. huecos: quitar también el fondo encerrado (entre piernas); se apaga si borra plumas del color del fondo.
# tol_cuadro: tolerancia propia de una animación, donde la general borra ropa o deja efectos. muerte: hoja, px de hoja por
# px de juego, cajas y su tolerancia; contorno, si el fondo es tan oscuro como el contorno normal. defensa y victoria: caja y
# escala de ese panel, como dano (provisional, una pose; hoy solo el piloto, Balam). especial: igual.
GUERREROS = {
    "Balam": {
        "ficha": "Hoja de sprites de Balam, Guardián Jaguar.png",
        "vistas": (185, 480, (792, 955), (982, 1125), (412, 590)),
        "fila": (550, 740), "reposo": (70, 195),
        "caminar": ((262, 358), (360, 455), (458, 568), (572, 685)),
        "correr": ((740, 865), (868, 990), (1005, 1145)),
        "ataque": ((1185, 1340), (1340, 1475)),
        "dano": ((870, 800, 1060, 1005), 1.2), "tol": 70, "huecos": True,
        "defensa": ((90, 800, 280, 1005), 1.2), "victoria": ((1215, 800, 1450, 1005), 1.2),
        "especial": ((375, 793, 790, 1010), 1.2),  # con el jaguar dorado: en la ficha está fundido con el cuerpo
        "muerte": {"ficha": "Hoja_Muerte_ Balam, Guardián Jaguar.png", "factor": 3.4, "tol": 26, "contorno": 20,
                   "cajas": ((350, 470, 608, 628), (636, 520, 886, 628))},
    },
    "Xali": {
        "ficha": "Ficha de Personaje Xali, Guardiana Ajolote.png",
        "vistas": (180, 488, (798, 978), (1002, 1168), (422, 612)),
        "fila": (555, 740), "reposo": (75, 200),
        "caminar": ((240, 360), (372, 478), (490, 595), (600, 705)),
        "correr": ((735, 875), (880, 1020), (1030, 1170)),
        "ataque": ((1175, 1310), (1310, 1465)), "tol_cuadro": {"Correr": 34, "Ataque": 34},
        "dano": ((880, 795, 1120, 1000), 1.2), "tol": 70, "huecos": True,
        "muerte": {"ficha": "Hoja_muerte_Xali_ Guardiana del Agua.png", "factor": 4.4, "tol": 60, "contorno": 30,
                   "cajas": ((440, 415, 745, 615), (790, 500, 1135, 610))},
    },
    "Naira": {
        "ficha": "Hoja de personaje pixel art de Naira, guardiana lobo.png",
        "vistas": (168, 488, (786, 978), (992, 1165), (425, 628)),
        "fila": (540, 720), "reposo": (70, 200),
        "caminar": ((240, 368), (378, 490), (495, 605), (605, 735)),
        "correr": ((730, 905), (895, 1035), (1040, 1180)),
        "ataque": ((1170, 1310),), "tol_cuadro": {"Ataque": 25}, "suelo": True,
        "dano": ((895, 780, 1115, 1000), 1.45), "tol": 34, "huecos": True,
    },
    "Ameyali": {
        "ficha": "Hoja de personaje_ Ameyali, Guardiana Mariposa.png",
        "vistas": (172, 490, (778, 978), (993, 1138), (435, 620)),
        "fila": (540, 725), "reposo": (60, 215),
        "caminar": ((240, 360), (375, 485), (490, 595), (600, 720)),
        "correr": ((740, 900), (910, 1070), (1100, 1265)),
        "ataque": ((1100, 1265),),  # la ficha solo trae cuatro cuadros, y el golpe va envuelto en llamas del color de sus alas
        "dano": ((918, 775, 1112, 1005), 1.3), "tol": 70, "huecos": True,
    },
    "Itzel": {
        "ficha": "Hoja de personaje_ Itzel, Guardiana Águila Real.png",
        "vistas": (168, 488, (790, 970), (982, 1132), (410, 592)),
        "fila": (550, 730), "reposo": (58, 190),
        "caminar": ((235, 350), (358, 470), (470, 580), (585, 695)),
        "correr": ((725, 860), (865, 995), (1005, 1150)),
        "ataque": ((1170, 1310), (1310, 1445)),
        "dano": ((905, 785, 1075, 1005), 1.35), "tol": 70, "huecos": True,
        "muerte": {"ficha": "Itzel_ muerte.png", "factor": 6.2, "tol": 70,
                   "cajas": ((420, 235, 700, 490), (790, 345, 1085, 490))},
    },
    "Yaret": {
        "ficha": "Hoja de personaje_ Yaret, Guardián Quetzal.png",
        "vistas": (168, 488, (790, 955), (976, 1120), (412, 600)),
        "fila": (550, 720), "reposo": (48, 175),
        "caminar": ((215, 325), (335, 435), (435, 545), (545, 655)),
        "correr": ((680, 850), (850, 985), (995, 1145)), "suelo": True,
        "ataque": ((1145, 1290), (1320, 1460)),
        "dano": ((915, 775, 1170, 1000), 1.4), "tol": 70, "huecos": False,
        "muerte": {"ficha": "muerte de Yaret, Guardián Quetzal.png", "factor": 4.6, "tol": 70,
                   "cajas": ((428, 240, 690, 510), (785, 390, 1095, 510))},
    },
    "Ocel": {
        "ficha": "Ocel, Guardián del Xoloitzcuintle.png",
        "vistas": (172, 488, (798, 990), (982, 1125), (412, 625)),
        "fila": (545, 715), "reposo": (65, 195),
        "caminar": ((220, 345), (355, 465), (470, 580), (580, 700)),
        "correr": ((695, 855), (865, 995), (1005, 1150)),
        "ataque": ((1160, 1300), (1300, 1460)),
        "dano": ((895, 780, 1072, 1000), 1.4), "tol": 70, "huecos": True,
    },
}


def paleta_fondo(c):
    """Colores frecuentes del borde del recorte: estas fichas pintan el fondo con selva y piedra, no con un solo verde."""
    borde = np.concatenate((c[0], c[-1], c[:, 0], c[:, -1])).astype(np.int32)
    clave = (borde[:, 0] >> 3) << 10 | (borde[:, 1] >> 3) << 5 | (borde[:, 2] >> 3)
    valores, cuentas = np.unique(clave, return_counts=True)
    orden = np.argsort(-cuentas)  # de más a menos frecuente: los primeros son los dominantes (los usa la regla de huecos)
    frecuentes = valores[orden][cuentas[orden] >= max(3, len(borde) // 100)]  # lo raro es personaje que toca el borde
    return np.array([borde[clave == v].mean(axis=0) for v in frecuentes], np.float32)


def _crecer(m, n):
    for _ in range(n):
        g = m.copy()
        g[1:] |= m[:-1]
        g[:-1] |= m[1:]
        g[:, 1:] |= m[:, :-1]
        g[:, :-1] |= m[:, 1:]
        m = g
    return m


def limpiar(m):
    """Apertura con reconstrucción corta: lo fino y lejano al cuerpo se va; las puntas de pelo y plumas, pegadas a él, no."""
    grueso = ~_crecer(~m, FINO)
    return _crecer(_crecer(grueso, FINO) & m, FINO) & m  # el segundo paso devuelve las esquinas que el primero no alcanza


def sin_suelo(c, m):
    """El suelo pintado bajo los pies es verde y pasa la inundación por pegarse a ellos; en esa franja no hay ropa verde."""
    ys = np.nonzero(m.any(axis=1))[0]
    franja = ys.max() - (ys.max() - ys.min()) // 8
    matiz, sat, _ = npc._matiz_y_sat(c)
    verde = (matiz > 75) & (matiz < 195) & (sat > 0.15) & (c.max(axis=-1) >= CONTORNO)
    verde[:franja] = False
    return m & ~verde


def mascara(c, tol, huecos, umbral_contorno=CONTORNO):
    """Personaje = lo que queda al inundar el fondo desde el borde; el contorno oscuro lo protege."""
    pal = paleta_fondo(c)
    todas = np.sqrt(((c[:, :, None, :].astype(np.float32) - pal[None, None]) ** 2).sum(-1))
    dist = todas.min(-1)
    contorno = c.max(axis=-1) < umbral_contorno

    # La sombra pintada bajo los pies es el mismo matiz del fondo, más oscuro (igual que recortar_npc.separar)
    borde = np.concatenate((c[0], c[-1], c[:, 0], c[:, -1]))
    matiz_b, _, _ = npc._matiz_y_sat(np.median(borde, axis=0, keepdims=True).astype(np.uint8))
    matiz, sat, _ = npc._matiz_y_sat(c)
    sombra = (np.abs((matiz - matiz_b[0] + 180) % 360 - 180) < 22) & (sat > 0.18)

    fondo = np.zeros(dist.shape, bool)
    alto, ancho = dist.shape
    for comp in npc._componentes(((dist < tol) | sombra) & ~contorno):
        ys, xs = np.array(comp).T
        if ys.min() == 0 or xs.min() == 0 or ys.max() == alto - 1 or xs.max() == ancho - 1:
            fondo[ys, xs] = True
    if huecos:
        for comp in npc._componentes((todas[..., :DOMINANTES].min(-1) < tol * 0.5) & ~fondo & ~contorno):
            if len(comp) >= 30:
                ys, xs = np.array(comp).T
                fondo[ys, xs] = True

    componentes = npc._componentes(limpiar(~fondo))
    if not componentes:
        raise ValueError("el recorte no contiene personaje")
    ys, xs = np.array(max(componentes, key=len)).T  # fuera chispas, mariposas y figuras vecinas
    salida = np.zeros(dist.shape, bool)
    salida[ys, xs] = True
    return salida


def reducir(c, m, factor):
    """Como recortar_npc.reducir, pero un bloque con bastante oscuro se queda oscuro: la mediana borraba ojos y contorno."""
    ys, xs = np.nonzero(m)
    c, m = c[ys.min():ys.max() + 1, xs.min():xs.max() + 1], m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    alto, ancho = max(1, round(m.shape[0] / factor)), max(1, round(m.shape[1] / factor))
    sal = np.zeros((alto, ancho, 4), np.uint8)
    for y in range(alto):
        ya, yb = int(y * factor), max(int(y * factor) + 1, int((y + 1) * factor))
        for x in range(ancho):
            xa, xb = int(x * factor), max(int(x * factor) + 1, int((x + 1) * factor))
            mb = m[ya:yb, xa:xb]
            if mb.mean() < 0.5:
                continue
            colores = c[ya:yb, xa:xb][mb]
            oscuros = colores[colores.max(axis=1) < OSCURO]
            sal[y, x, :3] = np.median(oscuros if len(oscuros) >= 0.3 * len(colores) else colores, axis=0)
            sal[y, x, 3] = 255
    return sal


def altura(m):
    ys = np.nonzero(m.any(axis=1))[0]
    return ys.max() - ys.min() + 1


def validar(nombre, lienzo, alto_min=30):
    """Un recorte fallido suele quedar en un resto diminuto o en un bloque lleno de fondo; mejor parar que subirlo."""
    alfa = lienzo[..., 3] > 0
    h = lienzo.shape[0]
    if not alto_min <= altura(alfa) <= h - npc.PIES:
        raise ValueError(f"{nombre}: altura {altura(alfa)} fuera de lo esperado")
    if not alfa[h - npc.PIES - 1].any():
        raise ValueError(f"{nombre}: los pies no llegan al pivote")
    relleno = alfa.sum() / max(1, alfa.any(axis=0).sum() * altura(alfa))
    if relleno > 0.9:
        raise ValueError(f"{nombre}: {relleno:.0%} de su caja es opaca; parece fondo sin quitar")
    return lienzo


def pasos_frente(lienzo, levantar=2):
    """Caminata de cuatro cuadros desde la pose de reposo, para las vistas que las fichas no animan: un pie arriba, cuerpo
    abajo, el otro pie arriba, cuerpo abajo; el cuerpo se balancea hacia el pie que apoya. shortcut: es una aproximación, sin
    brazos ni capa que se muevan; sustituir por cuadros reales cuando haya hojas de frente y de espalda."""
    alfa = lienzo[..., 3] > 0
    ys = np.nonzero(alfa.any(axis=1))[0]
    y1 = ys.max() + 1
    corte = y1 - max(6, (y1 - ys.min()) // 4)
    medio = lienzo.shape[1] // 2
    margen = alfa[:, 0].any() or alfa[:, -1].any()  # sin sitio para balancearse: el rodar lo partiría por el borde

    def pegar(destino, trozo):
        destino[trozo[..., 3] > 0] = trozo[trozo[..., 3] > 0]

    cuadros = []
    for lado, balanceo, baja in ((0, 1, 0), (None, 0, 1), (1, -1, 0), (None, 0, 1)):
        c = np.zeros_like(lienzo)
        for mitad in (0, 1):
            xs = slice(0, medio) if mitad == 0 else slice(medio, None)
            sube = levantar if mitad == lado else 0
            pegar(c[corte - sube:y1 - sube, xs], lienzo[corte:y1, xs])
        cuerpo = np.roll(lienzo[:corte], 0 if margen else balanceo, axis=1)
        pegar(c[baja:corte + baja], cuerpo)
        cuadros.append(c)
    return cuadros


def caido(dano):
    """Quien no tiene hoja de muerte cae con su pose de daño tumbada. shortcut: es rígida; sustituir al llegar su hoja."""
    ys, xs = np.nonzero(dano[..., 3])
    tumbado = np.rot90(dano[ys.min():ys.max() + 1, xs.min():xs.max() + 1], k=-1)
    return npc.colocar(tumbado, "caido", por_cuerpo=True, lienzo_px=LIENZO_ATAQUE)


def hoja(filas, zoom=3, sep=6):
    """Una fila por guerrero, con cada lienzo a su ancho."""
    ancho = max(sum(s.shape[1] * zoom + sep for s in fila) for fila in filas) + sep
    alto_celda = LIENZO_VISTA[1] * zoom + sep
    salida = np.full((alto_celda * len(filas) + sep, ancho, 4), (120, 112, 92, 255), np.uint8)
    for i, fila in enumerate(filas):
        x = sep
        for spr in fila:
            z = np.kron(spr, np.ones((zoom, zoom, 1), np.uint8))
            region = salida[sep + i * alto_celda:sep + i * alto_celda + z.shape[0], x:x + z.shape[1]]
            region[z[..., 3] > 0] = z[z[..., 3] > 0]
            x += z.shape[1] + sep
    return salida


def _comprobar():
    """Lo que un cambio en limpiar() o caido() rompería sin dar error: se vería solo en la hoja de revisión."""
    m = np.zeros((40, 60), bool)
    m[5:35, 5:25] = True  # cuerpo
    m[20, 25:60] = True   # línea de suelo de 1 px pegada al cuerpo
    limpio = limpiar(m)
    assert limpio[5:35, 5:25].all() and not limpio[20, 40:].any()
    dano = np.zeros((64, 80, 4), np.uint8)
    dano[10:62, 30:50, 3] = 255  # 20 × 52 de pie
    tumbado = caido(dano)
    assert altura(tumbado[..., 3] > 0) == 20 and tumbado[64 - npc.PIES - 1, ..., 3].any()
    reposo = np.zeros((64, 48, 4), np.uint8)
    reposo[10:62, 14:34, 3] = 255
    pasos = pasos_frente(reposo)
    assert len(pasos) == 4 and all(p[61, ..., 3].any() for p in pasos), "un pie siempre apoya"
    assert not pasos[0][61, 14:24, 3].any() and pasos[0][61, 24:34, 3].all(), "el primer cuadro levanta un pie"
    assert not pasos[2][61, 24:34, 3].any() and pasos[2][61, 14:24, 3].all(), "el tercero, el otro"


def main():
    _comprobar()
    carpeta, revision = sys.argv[1], sys.argv[2]
    os.makedirs(carpeta, exist_ok=True)
    escritos, filas_a, filas_b, filas_c = set(), [], [], []

    def guardar(nombre_png, rgba):
        escritos.add(nombre_png)
        npc.escribir_png(os.path.join(carpeta, nombre_png), rgba)

    for nombre, g in GUERREROS.items():
        img = npc.leer_png(os.path.join(FICHAS, g["ficha"]))
        tol, huecos = g["tol"], g["huecos"]

        def recorte(caja, factor, etiqueta, lienzo, por_cuerpo=True):
            x0, y0, x1, y1 = caja
            c = img[y0:y1, x0:x1]
            m = mascara(c, tol, huecos)
            spr = reducir(c, sin_suelo(c, m) if g.get("suelo") else m, factor)
            return validar(etiqueta, npc.colocar(spr, etiqueta, por_cuerpo=por_cuerpo, lienzo_px=lienzo))

        # El factor es por personaje, no por vista, para que el cuerpo mida lo mismo en todas: manda la vista de frente.
        y0, y1, norte, este, sur = g["vistas"]
        c_sur = img[y0:y1, sur[0]:sur[1]]
        factor = altura(mascara(c_sur, tol, huecos)) / ALTO
        vistas = []
        for tramo, direccion in zip((norte, este, sur), npc.DIRECCIONES):
            lienzo = recorte((tramo[0], y0, tramo[1], y1), factor, f"{nombre}_{direccion}", LIENZO_VISTA, por_cuerpo=False)
            guardar(f"Guerrero_{nombre}_Reposo1_{direccion}.png", lienzo)
            vistas.append(lienzo)
            if direccion != "03":
                for n, cuadro in enumerate(pasos_frente(lienzo), 1):
                    guardar(f"Guerrero_{nombre}_Caminar{n}_{direccion}.png", cuadro)

        fy0, fy1 = g["fila"]
        c_rep = img[fy0:fy1, g["reposo"][0]:g["reposo"][1]]
        factor_fila = altura(mascara(c_rep, tol, huecos)) / ALTO

        def fila(animacion, tramos, lienzo):
            nonlocal tol
            cuadros = []
            general, tol = tol, g.get("tol_cuadro", {}).get(animacion, tol)
            for n, (xa, xb) in enumerate(tramos, 1):
                cuadro = recorte((xa, fy0, xb, fy1), factor_fila, f"{nombre}_{animacion}{n}", lienzo)
                guardar(f"Guerrero_{nombre}_{animacion}{n}_03.png", cuadro)
                cuadros.append(cuadro)
            tol = general
            return cuadros

        caminar = fila("Caminar", g["caminar"], LIENZO_ACCION)
        correr = fila("Correr", g["correr"], LIENZO_ACCION)
        ataque = fila("Ataque", g["ataque"], LIENZO_ATAQUE)
        caja_dano, escala = g["dano"]
        dano = recorte(caja_dano, factor_fila * escala, f"{nombre}_Dano", LIENZO_ACCION)
        guardar(f"Guerrero_{nombre}_Dano1_03.png", dano)

        datos = g.get("muerte")
        if datos:
            hoja_muerte = npc.leer_png(os.path.join(FICHAS, datos["ficha"]))
            muerte = [dano]
            for n, (x0, y0, x1, y1) in enumerate(datos["cajas"], 2):
                c = hoja_muerte[y0:y1, x0:x1]
                m = mascara(c, datos["tol"], huecos, datos.get("contorno", CONTORNO))
                spr = npc.colocar(reducir(c, m, datos["factor"]), f"{nombre}_Muerte{n}", por_cuerpo=True, lienzo_px=LIENZO_ATAQUE)
                muerte.append(validar(f"{nombre}_Muerte{n}", spr, alto_min=12))
        else:
            muerte = [dano, validar(f"{nombre}_Muerte2", caido(dano), alto_min=12)]
        for n, cuadro in enumerate(muerte, 1):
            guardar(f"Guerrero_{nombre}_Muerte{n}_03.png", cuadro)

        poses = []
        for clave, animacion, lienzo_pose in (("defensa", "Defensa", LIENZO_ACCION), ("victoria", "Victoria", LIENZO_ACCION), ("especial", "Especial", LIENZO_ATAQUE)):
            if clave in g:
                caja, escala = g[clave]
                pose = recorte(caja, factor_fila * escala, f"{nombre}_{animacion}", lienzo_pose)
                guardar(f"Guerrero_{nombre}_{animacion}1_03.png", pose)
                poses.append(pose)

        filas_a.append(vistas + caminar)
        filas_b.append(correr + ataque + [dano] + poses)
        filas_c.append(muerte)
        print(nombre, "factor", round(float(factor), 2), "fila", round(float(factor_fila), 2))

    # Una versión anterior con más cuadros dejaría sobrantes que Unity cargaría como parte de la animación
    for sobrante in os.listdir(carpeta):
        base = sobrante[:-5] if sobrante.endswith(".meta") else sobrante
        if base.startswith("Guerrero_") and base.endswith(".png") and base not in escritos:
            os.remove(os.path.join(carpeta, sobrante))

    raiz, ext = os.path.splitext(revision)
    npc.escribir_png(revision, hoja(filas_a))
    npc.escribir_png(f"{raiz}_acciones{ext}", hoja(filas_b))
    npc.escribir_png(f"{raiz}_muerte{ext}", hoja(filas_c))
    print("Listo:", len(GUERREROS), "guerreros,", len(escritos), "sprites en", carpeta)


if __name__ == "__main__":
    main()
