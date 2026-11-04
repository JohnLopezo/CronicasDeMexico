# Recorta los sprites de los NPC de las fichas pintadas (Docs/NPC) a la resolución del escenario (lienzo 48×64,
# Docs/Arte_Olmeca_Demo.md). Solo usa numpy; se ejecuta con el Python que trae Blender:
#   "<Blender>/5.2/python/bin/python.exe" -I Tools/ArtPipeline/recortar_npc.py <hoja_revision.png>
# Escribe en Sprites/Npc Npc_<Nombre>_<Reposo1|CaminarN>_<dir>.png (01 Norte, 03 Este, 05 Sur) y, en la ruta pedida, una
# hoja con los personajes en reposo ampliados ×3 sobre tierra, en el orden de NPC, y otra (_caminar) con su caminata
# lateral. Las diagonales del juego reutilizan la vista Este.
# El lienzo y los pies están repetidos en PixelArtSpriteImporter (NpcPivot): si cambian aquí, cambiarlos allí.
import os
import struct
import sys
import zlib
from collections import deque

import numpy as np

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
FICHAS = os.path.join(RAIZ, "Docs", "NPC")
SALIDA = os.path.join(RAIZ, "Assets", "_Project", "Art", "Environment", "Olmeca", "Sprites", "Npc")
LIENZO = (48, 64)  # ancho, alto
PIES = 2           # px libres bajo los pies: el pivote queda ahí

INFANTILES = "Hoja de sprites infantiles olmecas.png"


def _ninos(fila_y, columnas):
    """Cajas (Norte, Este, Sur) de un niño: en esa ficha las columnas son frente, derecha, espalda, izquierda."""
    y0, y1 = fila_y
    frente, derecha, espalda = columnas
    return ((espalda[0], y0, espalda[1], y1), (derecha[0], y0, derecha[1], y1), (frente[0], y0, frente[1], y1))


# Nombre: ficha, px de ficha por px de juego y cajas de las vistas Norte, Este y Sur (x0, y0, x1, y1). El factor es por
# personaje, no por vista, para que el cuerpo mida lo mismo en todas. La vista Este mira a la derecha; ojo: en muchas
# fichas "Izquierda" y "Derecha" miran las dos a la derecha.
NPC = {
    "Comerciante": ("Comerciantes1.png", 8.0, ((690, 76, 860, 498), (440, 76, 605, 498), (155, 60, 355, 498))),
    "ComercianteMujer": ("Comerciantes1.png", 8.3, ((695, 575, 860, 985), (435, 575, 605, 985), (155, 556, 345, 985))),
    "Guerrero": ("Guerrero.png", 6.0, ((795, 190, 975, 525), (595, 190, 785, 525), (395, 185, 590, 525))),
    "Guardia": ("Guerrero2.png", 3.0, ((1375, 430, 1485, 595), (1235, 430, 1320, 595), (1090, 430, 1195, 595))),
    "Curandera": ("Curandera2.png", 4.2, ((620, 555, 735, 765), (1090, 330, 1195, 538), (620, 100, 735, 318))),
    "Anciano": ("Ficha de Personaje_ Anciano Mesoamericano.png", 6.0, ((795, 155, 950, 472), (620, 155, 770, 472), (395, 150, 565, 472))),
    "Sacerdote": ("Hoja de personaje del Sacerdote Mesoamericano.png", 5.4, ((785, 150, 950, 490), (605, 155, 760, 490), (395, 150, 575, 490))),
    "Agricultor": ("Hoja de personaje_ Agricultor Olmeca.png", 5.6, ((810, 165, 950, 465), (630, 165, 770, 465), (440, 160, 590, 465))),
    "Pescador": ("Hoja de sprites del Pescador Mesoamericano.png", 5.7, ((795, 160, 950, 478), (615, 160, 760, 478), (415, 160, 585, 478))),
    "Mensajero": ("Hoja de personaje_ Mensajero mesoamericano.png", 5.5, ((800, 170, 930, 465), (610, 170, 750, 465), (420, 170, 575, 465))),
    "Nino1": (INFANTILES, 5.5, _ninos((152, 382), ((50, 155), (175, 275), (295, 395)))),
    "Nina1": (INFANTILES, 5.5, _ninos((152, 382), ((560, 665), (675, 775), (795, 885)))),
    "Nino2": (INFANTILES, 5.5, _ninos((152, 382), ((1060, 1160), (1175, 1275), (1300, 1395)))),
    "Nina2": (INFANTILES, 5.5, _ninos((436, 670), ((40, 155), (165, 270), (300, 395)))),
    "Nino3": (INFANTILES, 5.5, _ninos((436, 670), ((560, 665), (680, 775), (795, 885)))),
    "Nina3": (INFANTILES, 5.5, _ninos((436, 670), ((1050, 1155), (1175, 1265), (1300, 1395)))),
    "Nino4": (INFANTILES, 5.5, _ninos((730, 980), ((30, 190), (185, 275), (300, 395)))),
    "Nina4": (INFANTILES, 5.5, _ninos((730, 980), ((545, 670), (680, 775), (800, 885)))),
    "Nino5": (INFANTILES, 5.5, _ninos((730, 980), ((1055, 1165), (1180, 1270), (1305, 1395)))),
}
DIRECCIONES = ("01", "03", "05")  # Norte, Este, Sur

# Cuadros de caminata de las fichas: dirección -> ficha, factor, y0, y1 y tramos x de cada cuadro. Solo los cuadros
# coherentes entre sí: en las fichas, algunos pierden la lanza, el bastón o el cesto y parpadearían al animarse.
# Las direcciones que no están aquí se animan con pasos(), a partir de la pose de reposo.
CAMINATA = {
    "Comerciante": {"03": ("ComercianteHombre.png", 3.0, 620, 782, ((1105, 1195), (1245, 1330)))},
    "ComercianteMujer": {"03": ("ComercianteMujer.png", 3.0, 610, 775, ((1110, 1200), (1250, 1325)))},
    "Guerrero": {"03": ("Guerrero.png", 4.6, 690, 950, ((405, 520), (530, 645), (650, 770)))},
    "Curandera": {
        "01": ("Curandera2.png", 4.2, 555, 765, ((620, 735), (780, 885), (935, 1040), (1085, 1190), (1235, 1340), (1385, 1490))),
        "03": ("Curandera2.png", 4.2, 778, 985, ((625, 735), (780, 885), (935, 1040), (1085, 1190), (1235, 1340), (1385, 1490))),
        "05": ("Curandera2.png", 4.2, 100, 318, ((620, 735), (780, 885), (935, 1040), (1085, 1190), (1235, 1340), (1385, 1490))),
    },
    "Anciano": {"03": ("Ficha de Personaje_ Anciano Mesoamericano.png", 3.5, 565, 745, ((320, 425), (450, 545), (565, 655), (670, 760)))},
    "Sacerdote": {"03": ("Hoja de personaje del Sacerdote Mesoamericano.png", 3.2, 555, 745, ((285, 395), (415, 520)))},
    "Agricultor": {"03": ("Hoja de personaje_ Agricultor Olmeca.png", 3.65, 570, 760, ((315, 425), (435, 540), (555, 655), (665, 770)))},
    "Pescador": {"03": ("Hoja de sprites del Pescador Mesoamericano.png", 3.15, 555, 730, ((345, 435), (430, 515), (505, 600)))},
    "Mensajero": {"03": ("Hoja de personaje_ Mensajero mesoamericano.png", 3.46, 555, 740, ((305, 400), (435, 525), (560, 650), (685, 780)))},
}


def leer_png(ruta):
    """PNG de 8 bits RGB o RGBA sin entrelazar, el formato de las fichas."""
    with open(ruta, "rb") as fh:
        d = fh.read()
    pos, idat = 8, b""
    while pos < len(d):
        n, tipo = struct.unpack(">I4s", d[pos:pos + 8])
        c = d[pos + 8:pos + 8 + n]
        if tipo == b"IHDR":
            w, h, prof, color, _, _, inter = struct.unpack(">IIBBBBB", c)
            if prof != 8 or inter != 0 or color not in (2, 6):
                raise ValueError(f"{ruta}: formato PNG no admitido ({prof} bits, color {color}, entrelazado {inter})")
        elif tipo == b"IDAT":
            idat += c
        pos += n + 12
    bpp = 3 if color == 2 else 4
    crudo = np.frombuffer(zlib.decompress(idat), np.uint8)
    salto = w * bpp + 1
    img = np.zeros((h, w * bpp), np.int32)
    for y in range(h):
        f, fila = crudo[y * salto], crudo[y * salto + 1:(y + 1) * salto].astype(np.int32)
        prev = img[y - 1] if y else np.zeros(w * bpp, np.int32)
        if f == 0:
            img[y] = fila
        elif f == 1:
            for k in range(bpp):
                img[y, k::bpp] = np.cumsum(fila[k::bpp]) & 255
        elif f == 2:
            img[y] = (fila + prev) & 255
        else:
            act = np.zeros(w * bpp, np.int32)
            for x in range(w * bpp):
                a = act[x - bpp] if x >= bpp else 0
                b = prev[x]
                if f == 3:
                    p = (a + b) // 2
                else:
                    c0 = prev[x - bpp] if x >= bpp else 0
                    pa, pb, pc = abs(b - c0), abs(a - c0), abs(a + b - 2 * c0)
                    p = a if pa <= pb and pa <= pc else b if pb <= pc else c0
                act[x] = (fila[x] + p) & 255
            img[y] = act
    return img.reshape(h, w, bpp)[:, :, :3].astype(np.uint8)


def escribir_png(ruta, rgba):
    h = rgba.shape[0]
    crudo = b"".join(b"\0" + rgba[y].tobytes() for y in range(h))

    def trozo(t, c):
        return struct.pack(">I", len(c)) + t + c + struct.pack(">I", zlib.crc32(t + c))

    with open(ruta, "wb") as fh:
        fh.write(b"\x89PNG\r\n\x1a\n" + trozo(b"IHDR", struct.pack(">IIBBBBB", rgba.shape[1], h, 8, 6, 0, 0, 0))
                 + trozo(b"IDAT", zlib.compress(crudo)) + trozo(b"IEND", b""))


# Pose de ataque de quienes pelean: ficha, factor y caja. La lanza extendida necesita un lienzo más ancho; el pivote
# normalizado (centro, pies a 2 px) es el mismo.
ATAQUE = {
    "Guerrero": ("Guerrero.png", 5.0, (1150, 680, 1515, 980)),
    "Guardia": ("Guerrero2.png", 3.0, (1080, 795, 1228, 985)),
}
LIENZO_ATAQUE = (112, 64)


def _matiz_y_sat(c):
    """Matiz (grados) y saturación de una imagen RGB."""
    c = c.astype(np.float32)
    v = c.max(axis=-1)
    delta = v - c.min(axis=-1)
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    d = np.maximum(delta, 1e-3)
    h = np.where(v == r, ((g - b) / d) % 6, np.where(v == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    return h, delta / np.maximum(v, 1e-3), v


def _componentes(mascara):
    """Lista de componentes de 4 vecinos, como listas de (y, x)."""
    h, w = mascara.shape
    visto = np.zeros((h, w), bool)
    salida = []
    for sy, sx in zip(*np.nonzero(mascara)):
        if visto[sy, sx]:
            continue
        comp, q = [], deque([(sy, sx)])
        visto[sy, sx] = True
        while q:
            y, x = q.popleft()
            comp.append((y, x))
            for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= ny < h and 0 <= nx < w and mascara[ny, nx] and not visto[ny, nx]:
                    visto[ny, nx] = True
                    q.append((ny, nx))
        salida.append(comp)
    return salida


def separar(c):
    """Máscara del personaje: el fondo es el verde del borde del recorte (y la sombra pintada, su mismo matiz más
    oscuro). Entra solo desde el borde, así que el contorno oscuro protege los verdes del personaje."""
    borde = np.concatenate((c[0], c[-1], c[:, 0], c[:, -1]))
    matiz_b, _, v_b = _matiz_y_sat(np.median(borde, axis=0, keepdims=True).astype(np.uint8))
    matiz, sat, v = _matiz_y_sat(c)
    dif = np.abs((matiz - matiz_b[0] + 180) % 360 - 180)
    verde = (dif < 22) & (sat > 0.18) & (v < v_b[0] * 1.45)

    fondo = np.zeros(verde.shape, bool)
    for comp in _componentes(verde):
        ys, xs = np.array(comp).T
        if ys.min() == 0 or xs.min() == 0 or ys.max() == verde.shape[0] - 1 or xs.max() == verde.shape[1] - 1:
            fondo[ys, xs] = True

    # Huecos cerrados (entre piernas, bajo el brazo): solo si el verde es casi idéntico al del fondo y no es una mota
    estricto = (dif < 12) & (sat > 0.25) & (v < v_b[0] * 1.2) & ~fondo
    for comp in _componentes(estricto):
        if len(comp) >= 30:
            ys, xs = np.array(comp).T
            fondo[ys, xs] = True

    componentes = _componentes(~fondo)
    if not componentes:
        raise ValueError("el recorte no contiene personaje")
    ys, xs = np.array(max(componentes, key=len)).T  # fuera rótulos y figuras vecinas
    mascara = np.zeros(verde.shape, bool)
    mascara[ys, xs] = True
    return mascara


def reducir(c, m, factor):
    """Reduce por bloques: un px de juego es opaco si lo es la mitad de su bloque, con la mediana de sus colores."""
    ys, xs = np.nonzero(m)
    c, m = c[ys.min():ys.max() + 1, xs.min():xs.max() + 1], m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    alto, ancho = round(m.shape[0] / factor), round(m.shape[1] / factor)
    sal = np.zeros((alto, ancho, 4), np.uint8)
    for y in range(alto):
        ya, yb = int(y * factor), max(int(y * factor) + 1, int((y + 1) * factor))
        for x in range(ancho):
            xa, xb = int(x * factor), max(int(x * factor) + 1, int((x + 1) * factor))
            mb = m[ya:yb, xa:xb]
            if mb.mean() >= 0.5:
                sal[y, x, :3] = np.median(c[ya:yb, xa:xb][mb], axis=0)
                sal[y, x, 3] = 255
    return sal


def colocar(spr, nombre, por_cuerpo=False, lienzo_px=LIENZO):
    """Pone el sprite en el lienzo con los pies en la línea del pivote y centrado por los pies (no por la caja: la
    lanza o el bastón la desplazan). Al caminar, los pies se abren y cierran: se centra por el cuerpo para que no
    tiemble de lado."""
    ys = np.nonzero(spr[..., 3].any(axis=1))[0]
    spr = spr[ys.min():ys.max() + 1]
    w, h = lienzo_px
    zona =spr[..., 3] > 0 if por_cuerpo else spr[-max(4, spr.shape[0] // 8):, :, 3] > 0
    centro = int(np.median(np.nonzero(zona)[1]))
    x0, y0 = w // 2 - centro, h - PIES - spr.shape[0]
    # Comprobación: un sprite que no cabe se recortaría sin avisar y el personaje saldría mutilado en el juego.
    if x0 < 0 or y0 < 0 or x0 + spr.shape[1] > w:
        raise ValueError(f"{nombre}: {spr.shape[1]}×{spr.shape[0]} px con centro {centro} no cabe en {w}×{h}")
    lienzo = np.zeros((h, w, 4), np.uint8)
    lienzo[y0:y0 + spr.shape[0], x0:x0 + spr.shape[1]] = spr
    return lienzo


def con_lanza(cuadro, reposo_sur):
    """La ficha del guerrero solo trae la lanza en un cuadro de caminata: se copia la de su vista frontal, donde está
    entera (desde la columna 13 del lienzo), delante de él y detrás del cuerpo y del escudo. Bajo la punta, ancha, solo
    el asta: a su lado está la mano."""
    lanza = reposo_sur[:, 13:20].copy()
    punta_fin = np.nonzero(lanza[..., 3].any(axis=1))[0].min() + 12
    lanza[punta_fin:, 4:] = 0
    ys, xs = np.array(max(_componentes(lanza[..., 3] > 0), key=len)).T  # fuera las plumas del tocado, sueltas al lado
    solo_lanza = np.zeros_like(lanza)
    solo_lanza[ys, xs] = lanza[ys, xs]
    lanza = solo_lanza
    x = int(np.median(np.nonzero(cuadro[..., 3])[1])) + 9
    destino = cuadro[:, x:x + lanza.shape[1]]
    pegar = (lanza[..., 3] > 0) & (destino[..., 3] == 0)
    destino[pegar] = lanza[pegar]
    return cuadro


def pasos(lienzo, levantar=2):
    """Dos cuadros de caminata a partir del reposo, para las vistas que las fichas no animan: levanta un pie y luego
    el otro. shortcut: es una aproximación, sin balanceo de brazos; sustituir por cuadros reales si llegan fichas."""
    ys = np.nonzero(lienzo[..., 3].any(axis=1))[0]
    y0, y1 = ys.min(), ys.max() + 1
    pie = y1 - max(3, (y1 - y0) // 8)
    medio = LIENZO[0] // 2
    cuadros = []
    for columnas in (slice(0, medio), slice(medio, None)):
        c = lienzo.copy()
        trozo = lienzo[pie:y1, columnas].copy()
        c[pie:y1, columnas] = 0
        destino = c[pie - levantar:y1 - levantar, columnas]
        destino[trozo[..., 3] > 0] = trozo[trozo[..., 3] > 0]
        cuadros.append(c)
    return cuadros


def hoja_revision(todos, zoom=3, columnas=4, por_fila=3):
    w, h = LIENZO
    celda = (w * zoom * por_fila + 24, h * zoom + 12)
    filas = (len(todos) + columnas - 1) // columnas
    hoja = np.full((celda[1] * filas, celda[0] * columnas, 4), (120, 112, 92, 255), np.uint8)
    for i, vistas in enumerate(todos):
        for j, spr in enumerate(vistas):
            x, y = (i % columnas) * celda[0] + 6 + j * w * zoom, (i // columnas) * celda[1] + 6
            z = np.kron(spr, np.ones((zoom, zoom, 1), np.uint8))
            region = hoja[y:y + z.shape[0], x:x + z.shape[1]]
            region[z[..., 3] > 0] = z[z[..., 3] > 0]
    return hoja


def main():
    revision = sys.argv[1]
    os.makedirs(SALIDA, exist_ok=True)
    fichas, todos, caminatas, escritos = {}, [], [], set()
    def ficha(archivo):
        if archivo not in fichas:
            fichas[archivo] = leer_png(os.path.join(FICHAS, archivo))
        return fichas[archivo]

    def guardar(nombre_png, rgba):
        escritos.add(nombre_png)
        escribir_png(os.path.join(SALIDA, nombre_png), rgba)

    for nombre, (archivo, factor, cajas) in NPC.items():
        vistas = []
        for caja, direccion in zip(cajas, DIRECCIONES):
            x0, y0, x1, y1 = caja
            c = ficha(archivo)[y0:y1, x0:x1]
            lienzo = colocar(reducir(c, separar(c), factor), f"{nombre}_{direccion}")
            guardar(f"Npc_{nombre}_Reposo1_{direccion}.png", lienzo)
            vistas.append(lienzo)

            real = CAMINATA.get(nombre, {}).get(direccion)
            if real:
                a, k, ya, yb, tramos = real
                cuadros = []
                for xa, xb in tramos:
                    c = ficha(a)[ya:yb, xa:xb]
                    cuadros.append(colocar(reducir(c, separar(c), k), f"{nombre}_Caminar_{direccion}", por_cuerpo=True))
                if nombre == "Guerrero":
                    x0, y0, x1, y1 = cajas[2]
                    c = ficha(archivo)[y0:y1, x0:x1]
                    sur = colocar(reducir(c, separar(c), factor), nombre)
                    cuadros = [con_lanza(cuadro, sur) for cuadro in cuadros]
            else:
                cuadros = pasos(lienzo)
            for n, cuadro in enumerate(cuadros, 1):
                guardar(f"Npc_{nombre}_Caminar{n}_{direccion}.png", cuadro)
            if direccion == "03":
                caminatas.append(cuadros)
        todos.append(vistas)
        if nombre in ATAQUE:
            a, k, (xa, ya, xb, yb) = ATAQUE[nombre]
            c = ficha(a)[ya:yb, xa:xb]
            guardar(f"Npc_{nombre}_Ataque1_03.png",
                    colocar(reducir(c, separar(c), k), f"{nombre}_Ataque", por_cuerpo=True, lienzo_px=LIENZO_ATAQUE))
        print(nombre, "alto", [int(v[..., 3].any(axis=1).sum()) for v in vistas],
              "ancho", [int(v[..., 3].any(axis=0).sum()) for v in vistas])
    # Sprites de una versión anterior: Unity cargaría un Caminar sobrante como otro cuadro de la animación.
    for sobrante in os.listdir(SALIDA):
        base = sobrante[:-5] if sobrante.endswith(".meta") else sobrante
        if base.endswith(".png") and base not in escritos:
            os.remove(os.path.join(SALIDA, sobrante))

    escribir_png(revision, hoja_revision(todos))
    # Al lado, los cuadros laterales de caminata: una fila por personaje
    escribir_png(os.path.splitext(revision)[0] + "_caminar.png", hoja_revision(caminatas, columnas=1, por_fila=6))
    print("Listo:", len(NPC), "personajes en", SALIDA)


if __name__ == "__main__":
    main()
