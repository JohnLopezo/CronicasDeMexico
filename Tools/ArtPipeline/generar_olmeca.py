# Recursos Pixel Art del entorno olmeca (flujo Blender -> Pixel Art) y composición isométrica de prueba.
# Uso (sin abrir Blender):
#   "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup ^
#       --python Tools\ArtPipeline\generar_pruebas.py -- Tools\ArtPipeline\Salida\Pruebas
import json
import math
import os
import random
import shutil
import sys
import zlib

import bpy
from mathutils import Euler, Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixelart_comun as pa  # noqa: E402

ARRIBA_Z = Vector((0, 0, 1))


# ---------------------------------------------------------------- piezas reutilizables

def matas(v, f, c, centro, n, alto, radio, rng, base="#23551C", puntas=("#568B26", "#73AD36", "#92B535"), grosor=0.012):
    """Hojas de pasto: un triángulo por hoja, base oscura y punta clara."""
    for _ in range(n):
        ang = rng.uniform(0, 2 * math.pi)
        b = Vector(centro) + Vector((math.cos(ang), math.sin(ang), 0)) * rng.uniform(0, radio)
        d = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0))
        d = d.normalized() if d.length > 1e-3 else Vector((1, 0, 0))
        perp = Vector((-d.y, d.x, 0))
        h = alto * rng.uniform(0.6, 1.0)
        k = len(v)
        v += [b + perp * grosor, b - perp * grosor, b + d * h * 0.35 + ARRIBA_Z * h]
        f.append((k, k + 1, k + 2))
        c += [base, base, rng.choice(puntas)]


def fronda(v, f, c, base, azim, elev, largo, caida, n_foliolos, largo_foliolo, colores, rng):
    """Hoja pinnada: raquis curvo con folíolos a ambos lados (palma, helecho)."""
    h = Vector((math.cos(azim), math.sin(azim), 0))
    lado = Vector((-h.y, h.x, 0))

    def punto(s):
        return base + h * (largo * s * math.cos(elev)) + ARRIBA_Z * (largo * s * math.sin(elev) - caida * s * s)

    col_base, col_punta = colores
    for i in range(n_foliolos):
        s = 0.1 + 0.9 * i / (n_foliolos - 1)
        p = punto(s)
        tang = (punto(min(1.0, s + 0.02)) - punto(max(0.0, s - 0.02))).normalized()
        lf = largo_foliolo * math.sin(math.pi * min(1.0, s * 1.05)) ** 0.6 * rng.uniform(0.85, 1.1) + 0.03
        for signo in (-1, 1):
            d = (lado * signo + tang * 0.4 - ARRIBA_Z * (0.12 + 0.2 * s)).normalized()
            w = 0.018 + 0.008 * rng.random()
            k = len(v)
            v += [p, p + d * lf * 0.4 + tang * w, p + d * lf, p + d * lf * 0.4 - tang * w]
            f.append((k, k + 1, k + 2, k + 3))
            punta = col_punta if rng.random() > 0.2 else col_base
            c += [col_base, col_base, punta, col_base]


def helecho(v, f, c, centro, escala, rng):
    n = rng.randint(6, 8)
    for i in range(n):
        az = 2 * math.pi * i / n + rng.uniform(-0.3, 0.3)
        colores = rng.choice([("#124524", "#328F3E"), ("#136B35", "#4A993E"), ("#23551C", "#568B26")])
        fronda(v, f, c, Vector(centro) + ARRIBA_Z * 0.02, az, math.radians(rng.uniform(35, 65)),
               0.32 * escala, 0.22 * escala, 9, 0.09 * escala, colores, rng)


def prisma_basalto(nombre, rng, alto, radio):
    """Columna natural de basalto: prisma irregular de 5–7 caras, sin labrar, partida en tramos por
    juntas transversales (como el basalto columnar real) y con la cima inclinada."""
    lados = rng.choice([5, 6, 6, 7])
    giro = rng.uniform(0, 2 * math.pi)
    perfil = []
    for j in range(lados):
        a = giro + 2 * math.pi * j / lados + rng.uniform(-0.15, 0.15)
        r = radio * rng.uniform(0.88, 1.12)
        perfil.append(Vector((math.cos(a) * r, math.sin(a) * r, 0)))
    inclinacion = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)) * 0.08

    cortes = sorted(rng.uniform(0.25, 0.85) * alto for _ in range(rng.randint(1, 2)))
    tramos = list(zip([0.0] + cortes, cortes + [alto]))
    v, f = [], []
    for t, (z0, z1) in enumerate(tramos):
        ultimo = t == len(tramos) - 1
        k = len(v)
        enc = 0.94 if t else 1.0   # tramos superiores un poco más delgados y desalineados
        des = Vector((rng.uniform(-0.01, 0.01), rng.uniform(-0.01, 0.01), 0)) if t else Vector()
        v += [p * enc + des + Vector((0, 0, z0 + (0.012 if t else 0))) for p in perfil]
        v += [p * enc + des + Vector((0, 0, z1 + (p.x * inclinacion.x + p.y * inclinacion.y if ultimo else 0)))
              for p in perfil]
        f += [(k + j, k + (j + 1) % lados, k + lados + (j + 1) % lados, k + lados + j) for j in range(lados)]
        f += [tuple(k + lados + j for j in range(lados)), tuple(k + j for j in reversed(range(lados)))]
    o = pa.malla(nombre, v, f)
    bev = o.modifiers.new("Bisel", 'BEVEL')
    bev.width = 0.014
    bev.segments = 1
    return o


# ---------------------------------------------------------------- recursos

TIERRA_CAMINO = ["#7B5B33", "#9B713F", "#B6814C", "#C6925A", "#E3B47C"]
TIERRA_HUMEDA = ["#644B2A", "#7B5B33", "#9B713F", "#B6814C", "#C6925A"]
PASTO_SUELO = ["#23551C", "#387022", "#568B26", "#73AD36"]
ALTURA_PASTO = 0.012


def atenuar_borde(x, y, ancho=0.1):
    """1 en el interior del tile y 0 en su borde: el relieve no rompe la continuidad entre tiles."""
    return min(1.0, (0.5 - max(abs(x), abs(y))) / ancho)


def guijarros(rng, n, r0, r1, colores, zona=0.4):
    mat = pa.mat_ruido("Guijarro", colores, escala=30)
    for _ in range(n):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=rng.uniform(r0, r1),
                                              location=(rng.uniform(-zona, zona), rng.uniform(-zona, zona), 0.0))
        g = bpy.context.active_object
        g.scale = (1, rng.uniform(0.7, 1), rng.uniform(0.45, 0.7))
        g.rotation_euler.z = rng.uniform(0, 2 * math.pi)
        pa.asignar(g, mat)


def tile_camino(rng, variante):
    """Tile de camino de tierra. Variantes: 1 guijarros y pasto en esquinas, 2 grupo de piedras,
    3 borde con pasto, 4 tierra húmeda y compacta."""
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=40, y_subdivisions=40, size=1.0)
    sup = bpy.context.active_object
    semilla = rng.uniform(0, 100)
    for vt in sup.data.vertices:
        x, y = vt.co.x, vt.co.y
        if max(abs(x), abs(y)) < 0.499:
            vt.co.z = (0.012 * noise.noise(Vector((x * 5 + semilla, y * 5, 0.3)))
                       + 0.005 * noise.noise(Vector((x * 17, y * 17 + semilla, 1.7))))
    tierra = TIERRA_HUMEDA if variante == 4 else TIERRA_CAMINO
    mat = pa.mat_ruido("Camino", tierra, escala=3.5, desde=0.36, hasta=0.64)
    mat.node_tree.nodes['Mapping'].inputs['Location'].default_value = (semilla, 0, 0)
    pa.asignar(sup, mat)

    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, -0.11))
    costado = bpy.context.active_object
    costado.scale = (0.999, 0.999, 0.22)
    pa.asignar(costado, pa.mat_ruido("Costado", ["#3A2A1A", "#644B2A", "#7B5B33", "#9B713F"], escala=5, estirar=(1, 1, 7)))

    claros = ["#615C48", "#7D745B", "#9C8B75", "#C9C0AB"]
    if variante == 2:
        guijarros(rng, 4, 0.03, 0.05, claros)
        for _ in range(3):   # grupo de piedras medianas en una esquina
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=rng.uniform(0.06, 0.09),
                                                  location=(0.22 + rng.uniform(-0.08, 0.08), -0.2 + rng.uniform(-0.08, 0.08), 0))
            g = bpy.context.active_object
            g.scale = (1, rng.uniform(0.75, 1), rng.uniform(0.5, 0.7))
            pa.asignar(g, pa.mat_ruido("Piedra", ["#4A473B", "#615C48", "#7D745B", "#9C8B75"], escala=12))
    else:
        guijarros(rng, 9 if variante == 1 else 5, 0.03, 0.06, claros)
    guijarros(rng, 10, 0.012, 0.02, ["#3A2A1A", "#644B2A"], 0.45)   # terrones oscuros

    v, f, c = [], [], []
    if variante == 1:
        for centro in [(-0.38, 0.36, 0), (0.36, -0.38, 0)]:
            matas(v, f, c, centro, rng.randint(14, 18), 0.17, 0.08, rng, grosor=0.022)
    elif variante == 3:
        for i in range(5):
            matas(v, f, c, (-0.4 + 0.2 * i, 0.42, 0), rng.randint(12, 16), 0.2, 0.08, rng, grosor=0.024)
    elif variante == 4:
        matas(v, f, c, (-0.36, -0.36, 0), 12, 0.15, 0.06, rng, grosor=0.02)
    if v:
        pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


def cerca_basalto(rng, variante):
    """Módulo de cerca de 1,1 m. El muro termina en caras planas (sin bisel) para empalmar con el
    siguiente módulo. Variantes: 1 y 2 completas, 3 rota con una columna caída."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.09))
    muro = bpy.context.active_object
    muro.name = "Muro"
    muro.scale = (1.1, 0.34, 0.18)
    pa.asignar(muro, pa.mat_ruido("Adobe", ["#644B2A", "#7B5B33", "#9B713F", "#B6814C"], escala=9))

    mat = pa.mat_piedra_musgo("Basalto", ["#2C302A", "#36382F", "#4A473B", "#615C48", "#7D745B", "#9C8B75"],
                              ["#3E5F27", "#5A8732", "#84B345"])
    rango = (0.3, 0.85) if variante == 3 else (0.85, 1.45)
    for i in range(5):
        if variante == 3 and i == 2:
            continue   # hueco de la columna caída
        col = prisma_basalto(f"Columna{i}", rng, rng.uniform(*rango), rng.uniform(0.085, 0.105))
        col.location = (-0.44 + 0.22 * i + rng.uniform(-0.015, 0.015), rng.uniform(-0.03, 0.03), 0.17)
        col.rotation_euler = Euler((rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), 0))
        pa.asignar(col, mat)
    if variante == 3:
        caida = prisma_basalto("Caida", rng, 0.6, 0.085)
        caida.rotation_euler = Euler((0, math.radians(90), math.radians(8)))
        caida.location = (-0.3, -0.24, 0.085)
        pa.asignar(caida, pa.mat_ruido("BasaltoCaido", ["#2C302A", "#36382F", "#4A473B", "#615C48", "#7D745B"], escala=9, estirar=(1, 1, 0.35)))

    v, f, c = [], [], []
    xs = [rng.uniform(-0.5, 0.45) for _ in range(rng.randint(2, 4))]
    for x in xs:
        matas(v, f, c, (x, -0.22, 0), rng.randint(7, 11), 0.13, 0.06, rng)
    if variante != 1:
        helecho(v, f, c, (rng.uniform(-0.3, 0.45), -0.25, 0), 0.85, rng)
    pa.asignar(pa.malla("Plantas", v, f, c), pa.mat_color_vertice("Vegetal"))


def fronda_cinta(v, f, c, base, azim, elev, largo, caida, ancho, colores, rng, muestras=13, dientes=True):
    """Fronda de palma como cinta dentada con quilla central: se lee como forma clara a 64 px,
    a diferencia de cientos de folíolos sueltos que se deshacen en ruido."""
    h = Vector((math.cos(azim), math.sin(azim), 0))
    lado = Vector((-h.y, h.x, 0))
    claro, medio = colores

    def punto(s):
        return base + h * (largo * s * math.cos(elev)) + ARRIBA_Z * (largo * s * math.sin(elev) - caida * s * s)

    for i in range(muestras):
        s = i / (muestras - 1)
        p = punto(s)
        mitad = ancho * math.sin(math.pi * (0.12 + 0.88 * s)) ** 0.7 * rng.uniform(0.9, 1.1)
        if dientes and i % 2:
            mitad *= 0.55   # dientes de los folíolos
        quilla = ARRIBA_Z * (-0.45 * mitad)
        k = len(v)
        v += [p, p + lado * mitad + quilla, p - lado * mitad + quilla]
        c += [claro, medio, medio]
        if i:
            f.append((k - 3, k - 2, k + 1, k))
            f.append((k - 3, k, k + 2, k - 1))


def palma(rng, variante):
    # Tronco anillado, levemente curvo (palma nativa tipo corozo: sin cocos)
    alto = 1.75 if variante == 1 else 1.45
    anillos, lados = 22, 10
    curva = Vector((0.04, -0.16, 0)) if variante == 1 else Vector((-0.1, 0.05, 0))
    v, f, c = [], [], []
    for i in range(anillos + 1):
        t = i / anillos
        centro = curva * (t * t) + ARRIBA_Z * (alto * t)
        bulto = abs(math.sin(t * math.pi * anillos / 2))
        r = (0.095 - 0.03 * t) * (0.9 + 0.14 * bulto)
        color = ["#48331F", "#6D502E", "#9C7842"][min(2, int(bulto * 3))]
        for j in range(lados):
            a = 2 * math.pi * j / lados
            v.append(centro + Vector((math.cos(a) * r, math.sin(a) * r, 0)))
            c.append(color)
    for i in range(anillos):
        for j in range(lados):
            a, b = i * lados + j, i * lados + (j + 1) % lados
            f.append((a, b, b + lados, a + lados))
    pa.asignar(pa.malla("Tronco", v, f, c), pa.mat_color_vertice("Madera"))

    cima = curva + ARRIBA_Z * alto
    v, f, c = [], [], []
    niveles = [  # (cantidad, elevación en grados, caída, colores quilla/borde)
        (4, (38, 55), 0.35, [("#AAC652", "#73AD36"), ("#92B535", "#568B26")]),
        (5, (12, 25), 0.45, [("#73AD36", "#387022"), ("#4A993E", "#217233")]),
        (5, (-8, 5), 0.4, [("#568B26", "#23551C"), ("#328F3E", "#124524")]),
    ]
    desfase = rng.uniform(0, 1)
    for n, (e0, e1), caida, colores in niveles:
        for i in range(n):
            az = desfase + 2 * math.pi * i / n + rng.uniform(-0.2, 0.2)
            fronda_cinta(v, f, c, cima, az, math.radians(rng.uniform(e0, e1)), rng.uniform(0.6, 0.67),
                         caida, 0.115, rng.choice(colores), rng)
        desfase += 0.55
    for _ in range(2):   # frondas secas colgando bajo la copa
        fronda_cinta(v, f, c, cima - ARRIBA_Z * 0.05, rng.uniform(0, 2 * math.pi), math.radians(-35),
                     0.42, 0.25, 0.07, ("#9C7842", "#6D502E"), rng, 9)
    pa.asignar(pa.malla("Copa", v, f, c), pa.mat_color_vertice("Follaje"))

    v, f, c = [], [], []
    matas(v, f, c, (0.12, -0.1, 0), 9, 0.12, 0.08, rng)
    helecho(v, f, c, (-0.16, -0.12, 0), 0.8, rng)
    pa.asignar(pa.malla("Base", v, f, c), pa.mat_color_vertice("Vegetal"))


def costado_tierra():
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, -0.11))
    costado = bpy.context.active_object
    costado.scale = (0.999, 0.999, 0.22)
    pa.asignar(costado, pa.mat_ruido("Costado", ["#3A2A1A", "#644B2A", "#7B5B33", "#9B713F"], escala=5, estirar=(1, 1, 7)))


def esferita(nombre, loc, radio, colores):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=radio, location=loc)
    o = bpy.context.active_object
    o.name = nombre
    pa.asignar(o, pa.mat_ruido(nombre, colores, escala=20))
    return o


def hoja(v, f, c, base, direccion, largo, ancho, col_base, col_punta):
    """Hoja ancha en rombo (arbustos, heliconias, nenúfares no)."""
    d = direccion.normalized()
    perp = d.cross(ARRIBA_Z)
    perp = perp.normalized() if perp.length > 1e-3 else Vector((1, 0, 0))
    k = len(v)
    v += [base, base + d * largo * 0.45 + perp * ancho, base + d * largo, base + d * largo * 0.45 - perp * ancho]
    f.append((k, k + 1, k + 2, k + 3))
    c += [col_base, col_base, col_punta, col_base]


def prisma_color(v, f, c, base, radio, alto, color, lados=6):
    k = len(v)
    for z in (0, alto):
        for j in range(lados):
            a = 2 * math.pi * j / lados
            v.append(base + Vector((math.cos(a) * radio, math.sin(a) * radio, z)))
            c.append(color)
    f += [(k + j, k + (j + 1) % lados, k + lados + (j + 1) % lados, k + lados + j) for j in range(lados)]
    f.append(tuple(k + lados + j for j in range(lados)))


# ---------------------------------------------------------------- suelo y agua

def tile_pasto(rng, variante):
    """Suelo de selva con pasto. Variantes: 1 pasto denso, 2 con hojarasca, 3 con florecillas."""
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=24, y_subdivisions=24, size=1.0)
    sup = bpy.context.active_object
    semilla = rng.uniform(0, 100)
    for vt in sup.data.vertices:
        x, y = vt.co.x, vt.co.y
        # Relieve siempre sobre z=0 (si baja, asoman las tapas del costado y del labio) y que se desvanece
        # hacia los bordes, que quedan todos a 0.012: así los tiles vecinos empalman sin surco
        vt.co.z = ALTURA_PASTO + 0.008 * noise.noise(Vector((x * 4 + semilla, y * 4, 0.5))) * atenuar_borde(x, y)
    mat = pa.mat_ruido("Pasto", PASTO_SUELO, escala=3, desde=0.36, hasta=0.64)
    mat.node_tree.nodes['Mapping'].inputs['Location'].default_value = (semilla, 0, 0)
    pa.asignar(sup, mat)
    costado_tierra()
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, -0.034))   # tapa bajo el relieve del terreno
    labio = bpy.context.active_object   # el pasto asoma por el borde superior del costado
    labio.scale = (1.0, 1.0, 0.04)
    pa.asignar(labio, pa.mat_ruido("Labio", ["#23551C", "#387022", "#568B26"], escala=12))

    v, f, c = [], [], []
    # Pocas matas bajas y de tono cercano al suelo: muchas generan un patrón de escamas al repetir el tile
    for _ in range(12):
        matas(v, f, c, (rng.uniform(-0.45, 0.45), rng.uniform(-0.45, 0.45), 0), rng.randint(4, 6), 0.06, 0.04, rng,
              base="#568B26", puntas=("#73AD36", "#92B535"), grosor=0.018)
    if variante == 2:   # hojarasca
        for _ in range(14):
            p = Vector((rng.uniform(-0.42, 0.42), rng.uniform(-0.42, 0.42), 0.012))
            hoja(v, f, c, p, Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0.05)), 0.06, 0.02,
                 rng.choice(["#6D502E", "#9C7842"]), rng.choice(["#9C7842", "#B6814C"]))
    pa.asignar(pa.malla("Hierba", v, f, c), pa.mat_color_vertice("Vegetal"))
    if variante == 3:   # florecillas
        for _ in range(7):
            col = rng.choice(["#F47588", "#F1C891"])
            esferita("Flor", (rng.uniform(-0.4, 0.4), rng.uniform(-0.4, 0.4), 0.05), 0.03, [col, col])


def tile_agua(rng, cuadro):
    """Agua de pantano: patrón periódico de 1 m (encaja entre tiles) animado en 4 cuadros en bucle.
    La superficie queda 6 cm bajo el nivel del suelo para que se vea la orilla de los tiles vecinos."""
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=48, y_subdivisions=48, size=1.0, location=(0, 0, -0.06))
    o = bpy.context.active_object
    attr = o.data.color_attributes.new(name="Col", type='FLOAT_COLOR', domain='POINT')
    t = 2 * math.pi * (cuadro - 1) / 4
    ondas = [(1, 2, 0.0, 1.0, 1), (3, -2, 1.3, 0.7, -1), (4, 1, 2.1, 0.5, 2), (-2, 5, 0.4, 0.45, 1), (5, 3, 2.9, 0.35, -2)]
    total = sum(o_[3] for o_ in ondas)
    for i, vt in enumerate(o.data.vertices):
        x, y = vt.co.x, vt.co.y
        s = sum(a * math.sin(2 * math.pi * (kx * x + ky * y) + fase + t * m) for kx, ky, fase, a, m in ondas) / total
        color = ("#7FC3B5" if s > 0.86 else "#489187" if s > 0.62 else "#168787" if s > 0.2
                 else "#0C7265" if s > -0.45 else "#0B4F4A")
        attr.data[i].color = pa.hex_lineal(color)
    pa.asignar(o, pa.mat_color_vertice("Agua"))


def nenufar(rng, variante):
    """Hojas flotantes con muesca y una flor (Nymphaea, nativa de los humedales de Tabasco)."""
    v, f, c = [], [], []
    for p in range(3):
        centro = Vector((rng.uniform(-0.13, 0.13), rng.uniform(-0.13, 0.13), 0.005 * p))
        r = rng.uniform(0.07, 0.1)
        a0 = rng.uniform(0, 2 * math.pi)
        k = len(v)
        v.append(centro)
        c.append("#387022")
        segs = 14
        for j in range(segs + 1):
            a = a0 + 0.35 + (2 * math.pi - 0.7) * j / segs
            v.append(centro + Vector((math.cos(a) * r, math.sin(a) * r, 0)))
            c.append(rng.choice(["#568B26", "#73AD36"]))
        f += [(k, k + 1 + j, k + 2 + j) for j in range(segs)]
    flor = Vector((rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05), 0.02))
    for j in range(6):
        a = 2 * math.pi * j / 6
        d = Vector((math.cos(a), math.sin(a), 0.9))
        hoja(v, f, c, flor, d, 0.06, 0.018, "#F47588", "#F47588")
    pa.asignar(pa.malla("Nenufar", v, f, c), pa.mat_color_vertice("Vegetal"))
    esferita("Centro", tuple(flor + Vector((0, 0, 0.02))), 0.014, ["#F1C891", "#F1C891"])


def tule(rng, variante):
    """Tule (Typha) de orilla: hojas altas y espigas pardas."""
    v, f, c = [], [], []
    matas(v, f, c, (0, 0, 0), 26, 0.7, 0.1, rng, base="#23551C", puntas=("#568B26", "#73AD36", "#387022"), grosor=0.032)
    for _ in range(3):
        b = Vector((rng.uniform(-0.06, 0.06), rng.uniform(-0.06, 0.06), 0))
        alto = rng.uniform(0.55, 0.72)
        prisma_color(v, f, c, b, 0.006, alto, "#387022", 4)
        prisma_color(v, f, c, b + ARRIBA_Z * (alto - 0.15), 0.032, 0.14, "#6D502E")
    pa.asignar(pa.malla("Tule", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- vegetación y rocas

def arbusto(rng, variante):
    """1: arbusto de hoja ancha de sotobosque; 2: heliconia (hojas en remo y brácteas rojas)."""
    v, f, c = [], [], []
    if variante == 1:
        centro = Vector((0, 0, 0.12))
        for _ in range(60):
            th = rng.uniform(0, 2 * math.pi)
            ph = math.radians(rng.uniform(0, 85))
            pos = centro + Vector((0.26 * math.sin(ph) * math.cos(th), 0.26 * math.sin(ph) * math.sin(th), 0.24 * math.cos(ph)))
            altura = pos.z / 0.36
            if altura > 0.75:
                cols = ("#568B26", rng.choice(["#92B535", "#73AD36"]))
            elif altura > 0.45:
                cols = rng.choice([("#387022", "#568B26"), ("#217233", "#4A993E")])
            else:
                cols = rng.choice([("#23551C", "#387022"), ("#124524", "#217233")])
            hoja(v, f, c, pos, (pos - centro) + ARRIBA_Z * 0.35, rng.uniform(0.15, 0.2), 0.07, *cols)
        pa.asignar(pa.malla("Hojas", v, f, c), pa.mat_color_vertice("Vegetal"))
        esferita("Nucleo", (0, 0, 0.14), 0.2, ["#1D2B1A", "#23551C"])
    else:
        for i in range(6):
            az = 2 * math.pi * i / 6 + rng.uniform(-0.3, 0.3)
            base = Vector((math.cos(az) * 0.03, math.sin(az) * 0.03, 0.12))
            prisma_color(v, f, c, Vector((base.x, base.y, 0)), 0.012, 0.12, "#387022", 4)
            fronda_cinta(v, f, c, base, az, math.radians(rng.uniform(50, 68)), rng.uniform(0.5, 0.6), 0.2, 0.085,
                         rng.choice([("#73AD36", "#387022"), ("#568B26", "#23551C")]), rng, 9, dientes=False)
        for b in range(2):
            tallo = Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 0))
            alto = rng.uniform(0.42, 0.55)
            prisma_color(v, f, c, tallo, 0.01, alto + 0.2, "#387022", 4)
            for j in range(5):
                lado = Vector((1, -1, 0)).normalized() * (1 if j % 2 else -1)
                p = tallo + ARRIBA_Z * (alto + 0.04 * j)
                hoja(v, f, c, p, lado + ARRIBA_Z * 0.6, 0.1 - 0.008 * j, 0.03, "#B24232", "#F47588")
        pa.asignar(pa.malla("Heliconia", v, f, c), pa.mat_color_vertice("Vegetal"))


def helecho_suelto(rng, variante):
    """1: helecho erguido; 2: helecho abierto y bajo."""
    v, f, c = [], [], []
    n = 9 if variante == 1 else 7
    e0, e1 = (38, 55) if variante == 1 else (15, 30)
    l0, l1, caida, ancho = (0.4, 0.46, 0.28, 0.075) if variante == 1 else (0.33, 0.38, 0.22, 0.07)
    for i in range(n):
        az = 2 * math.pi * i / n + rng.uniform(-0.25, 0.25)
        cols = rng.choice([("#4A993E", "#136B35"), ("#328F3E", "#124524"), ("#73AD36", "#387022")])
        fronda_cinta(v, f, c, Vector((0, 0, 0.02)), az, math.radians(rng.uniform(e0, e1)), rng.uniform(l0, l1),
                     caida, ancho, cols, rng, 11)
    pa.asignar(pa.malla("Helecho", v, f, c), pa.mat_color_vertice("Vegetal"))


def roca(rng, variante):
    """Rocas de basalto: 1 bloque grande, 2 par de bloques, 3 lajas."""
    mat = pa.mat_piedra_musgo("Roca", ["#36382F", "#4A473B", "#615C48", "#7D745B", "#9C8B75"],
                              ["#3E5F27", "#5A8732", "#84B345"], escala=6, estirar=(1, 1, 1), altura_musgo=0.08,
                              umbral_musgo=0.5)
    if variante in (1, 2):
        piezas = [(Vector((0, 0, 0)), 0.22)] if variante == 1 else [(Vector((-0.08, 0.06, 0)), 0.15), (Vector((0.12, -0.08, 0)), 0.1)]
        for centro, r in piezas:
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=r)
            o = bpy.context.active_object
            sem = rng.uniform(0, 50)
            for vt in o.data.vertices:
                vt.co += vt.co.normalized() * (r * 0.22 * noise.noise(vt.co * (4 / r) * 0.25 + Vector((sem, 0, 0))))
                vt.co.z *= 0.68
            o.location = centro + ARRIBA_Z * (r * 0.45)
            o.rotation_euler.z = rng.uniform(0, 2 * math.pi)
            pa.asignar(o, mat)
    else:   # lajas: caras superiores amplias, el musgo las cubriría por completo
        mat = pa.mat_ruido("Laja", ["#36382F", "#4A473B", "#615C48", "#7D745B", "#9C8B75"], escala=7)
        for j in range(3):
            bpy.ops.mesh.primitive_cube_add(size=1, location=(rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), 0.03 + 0.05 * j))
            o = bpy.context.active_object
            o.scale = (rng.uniform(0.22, 0.32), rng.uniform(0.14, 0.2), 0.06)
            o.rotation_euler = Euler((rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), rng.uniform(0, math.pi)))
            bev = o.modifiers.new("Bisel", 'BEVEL')
            bev.width = 0.015
            pa.asignar(o, mat)
    v, f, c = [], [], []
    matas(v, f, c, (0.18, -0.12, 0), 8, 0.1, 0.05, rng, grosor=0.018)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- transiciones (autotiles por máscara de vecinos)

# Bits de la máscara: vecinos cardinales y diagonales que son "pasto" (otro terreno)
DIR4 = {1: (1, 0), 2: (-1, 0), 4: (0, 1), 8: (0, -1)}
DIAG = {16: (1, 1, 1, 4), 32: (1, -1, 1, 8), 64: (-1, 1, 2, 4), 128: (-1, -1, 2, 8)}   # sx, sy, bits adyacentes
FASE = {1: 0.3, 2: 1.7, 4: 2.9, 8: 4.1}


def normalizar_mascara(m):
    """Una diagonal solo cuenta si ninguno de sus dos lados cardinales es pasto (si no, ya la cubre ese lado)."""
    for bit, (_, _, a, b) in DIAG.items():
        if m & bit and (m & a or m & b):
            m &= ~bit
    return m


def ancho_borde(t, fase):
    # Periódica en t (período 1): dos tiles contiguos con pasto en el mismo lado empalman sin escalón
    return 0.2 + 0.06 * math.sin(2 * math.pi * t + fase) + 0.035 * math.sin(4 * math.pi * t + 2 * fase)


def es_pasto(x, y, mascara):
    for bit, (dx, dy) in DIR4.items():
        if mascara & bit and x * dx + y * dy > 0.5 - ancho_borde(y if dx else x, FASE[bit]):
            return True
    for bit, (sx, sy, _, _) in DIAG.items():
        if mascara & bit and (0.5 - x * sx) + (0.5 - y * sy) < 0.26 + 0.04 * math.sin(7 * x + 3 * y):
            return True
    return False


def superficie_mixta(rng, mascara, modo):
    """modo 'camino': tierra con pasto entrando desde los lados de la máscara.
    modo 'orilla': solo el pasto y un talud de tierra; el resto queda transparente para ver el agua animada debajo."""
    n = 40
    paso = 1.0 / n
    pasto = [[es_pasto(-0.5 + (i + 0.5) * paso, -0.5 + (j + 0.5) * paso, mascara) for j in range(n)] for i in range(n)]

    def cerca_de_pasto(i, j):
        return any(pasto[a][b] for a in range(max(0, i - 1), min(n, i + 2)) for b in range(max(0, j - 1), min(n, j + 2)))

    caras = []
    for i in range(n):
        for j in range(n):
            if pasto[i][j]:
                caras.append((i, j, 1))
            elif modo == "camino" or cerca_de_pasto(i, j):
                caras.append((i, j, 0))

    en_pasto = set()
    for i, j, t in caras:
        if t:
            en_pasto.update({(i, j), (i + 1, j), (i, j + 1), (i + 1, j + 1)})
    semilla = rng.uniform(0, 100)
    verts = []
    for i in range(n + 1):
        for j in range(n + 1):
            x, y = -0.5 + i * paso, -0.5 + j * paso
            if (i, j) in en_pasto:
                z = ALTURA_PASTO + 0.006 * noise.noise(Vector((x * 4 + semilla, y * 4, 0.5))) * atenuar_borde(x, y)
            elif modo == "camino":
                z = 0.008 * noise.noise(Vector((x * 5 + semilla, y * 5, 0.3))) * atenuar_borde(x, y)
            else:
                z = -0.06
            verts.append(Vector((x, y, z)))
    idx = lambda i, j: i * (n + 1) + j
    sup = pa.malla("Superficie", verts, [(idx(i, j), idx(i + 1, j), idx(i + 1, j + 1), idx(i, j + 1)) for i, j, _ in caras])
    tierra = TIERRA_CAMINO if modo == "camino" else ["#3A2A1A", "#644B2A", "#7B5B33", "#9B713F"]
    sup.data.materials.append(pa.mat_ruido("Tierra", tierra, escala=3.5, desde=0.36, hasta=0.64))
    sup.data.materials.append(pa.mat_ruido("Pasto", PASTO_SUELO, escala=3, desde=0.36, hasta=0.64))
    for poly, (_, _, t) in zip(sup.data.polygons, caras):
        poly.material_index = t
    if modo == "camino":
        costado_tierra()

    v, f, c = [], [], []   # matas en el borde del pasto para que el contorno no sea liso
    for i, j, t in caras:
        if t and not all(pasto[a][b] for a in range(max(0, i - 1), min(n, i + 2)) for b in range(max(0, j - 1), min(n, j + 2))):
            if rng.random() < 0.12:
                matas(v, f, c, (-0.5 + (i + 0.5) * paso, -0.5 + (j + 0.5) * paso, 0.01), rng.randint(4, 6), 0.07, 0.03,
                      rng, base="#387022", puntas=("#568B26", "#73AD36", "#92B535"), grosor=0.018)
    if v:
        pa.asignar(pa.malla("Matas", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- sotobosque y dosel

def matorral(rng, variante):
    """Matorral denso de sotobosque: varios arbustos de hoja ancha, frondas y pasto, con base oscura."""
    v, f, c = [], [], []
    for _ in range(2 + variante):
        centro = Vector((rng.uniform(-0.18, 0.18), rng.uniform(-0.18, 0.18), rng.uniform(0.1, 0.2)))
        r = rng.uniform(0.13, 0.19)
        for _ in range(26):
            th = rng.uniform(0, 2 * math.pi)
            ph = math.radians(rng.uniform(0, 85))
            pos = centro + Vector((r * math.sin(ph) * math.cos(th), r * math.sin(ph) * math.sin(th), 0.9 * r * math.cos(ph)))
            if pos.z > 0.32:
                cols = ("#568B26", rng.choice(["#92B535", "#73AD36"]))
            elif pos.z > 0.18:
                cols = rng.choice([("#387022", "#568B26"), ("#217233", "#4A993E")])
            else:
                cols = rng.choice([("#124524", "#217233"), ("#0F2A17", "#136B35")])
            hoja(v, f, c, pos, (pos - centro) + ARRIBA_Z * 0.3, rng.uniform(0.12, 0.16), 0.06, *cols)
    for _ in range(2):
        base = Vector((rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), 0.02))
        for k in range(5):
            fronda_cinta(v, f, c, base, rng.uniform(0, 2 * math.pi), math.radians(rng.uniform(30, 50)), 0.32, 0.22, 0.06,
                         rng.choice([("#4A993E", "#136B35"), ("#328F3E", "#124524")]), rng, 11)
    matas(v, f, c, (rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), 0), 14, 0.3, 0.12, rng,
          base="#124524", puntas=("#387022", "#568B26", "#73AD36"), grosor=0.022)
    pa.asignar(pa.malla("Matorral", v, f, c), pa.mat_color_vertice("Vegetal"))
    esferita("Nucleo", (0, 0, 0.1), 0.2, ["#0F2A17", "#124524"])


def pasto_alto(rng, variante):
    v, f, c = [], [], []
    alto, n = (0.36, 34) if variante == 1 else (0.26, 26)
    matas(v, f, c, (0, 0, 0), n, alto, 0.12, rng, base="#23551C", puntas=("#568B26", "#73AD36", "#92B535"), grosor=0.026)
    pa.asignar(pa.malla("PastoAlto", v, f, c), pa.mat_color_vertice("Vegetal"))


def dosel(rng, variante):
    """Copa de árbol fuera de cuadro: solo se usa su sombra moteada sobre el suelo."""
    v, f, c = [], [], []
    alto = 2.6
    # Desplazada contra el sol para que la sombra caiga centrada en el origen
    centro = Vector((-0.25 * alto, -0.4167 * alto, alto))
    for _ in range(3 if variante == 1 else 2):
        sub = centro + Vector((rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), 0))
        for _ in range(55):
            p = sub + Vector((rng.gauss(0, 0.32), rng.gauss(0, 0.32), rng.uniform(-0.15, 0.15)))
            hoja(v, f, c, p, Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0.15)), 0.22, 0.08, "#23551C", "#23551C")
    pa.asignar(pa.malla("Dosel", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- producción

SOMBRA_CHICA = ((80, 40), (26, 20))
SOLAPE_TILE = 1.03
TIERRA_VERDES = ("tierra", "verde_calido", "verde_frio")
TIPOS = {
    # constructor, lienzo, ancla (x, y desde abajo), rampas, contorno, lienzo y ancla de la sombra, umbral
    "Tile_Camino_Tierra": (tile_camino, (64, 64), (32, 40), ("tierra", "basalto", "verde_calido"), None, None, 0.5),
    "Tile_Pasto_Selva": (tile_pasto, (64, 64), (32, 40), ("verde_calido", "verde_frio", "tierra", "madera", "acentos"),
                         None, None, 0.5),
    "Tile_Agua_Anim": (tile_agua, (64, 64), (32, 40), ("agua",), None, None, 0.5),
    "Tile_Camino_Trans": (lambda rng, m: superficie_mixta(rng, m, "camino"), (64, 64), (32, 40), TIERRA_VERDES, None, None, 0.5),
    "Tile_Orilla": (lambda rng, m: superficie_mixta(rng, m, "orilla"), (64, 64), (32, 40), TIERRA_VERDES, None, None, 0.5),
    "Cerca_Basalto": (cerca_basalto, (64, 96), (32, 14),
                      ("basalto", "musgo", "tierra", "verde_calido", "verde_frio"), "#1E2420", ((128, 64), (40, 32)), 0.5),
    "Palma_Corozo": (palma, (64, 128), (36, 12), ("verde_calido", "verde_frio", "madera"), "#0F2A17",
                     ((160, 64), (50, 32)), 0.4),
    "Arbusto_Selva": (arbusto, (48, 48), (24, 10), ("verde_calido", "verde_frio", "acentos"), "#0F2A17",
                      ((96, 48), (30, 24)), 0.45),
    "Matorral_Selva": (matorral, (64, 64), (32, 16), ("verde_calido", "verde_frio"), "#0F2A17", ((128, 64), (40, 32)), 0.45),
    "Pasto_Alto": (pasto_alto, (32, 48), (16, 8), ("verde_calido", "verde_frio"), "#0F2A17", ((64, 40), (20, 20)), 0.4),
    "Helecho": (helecho_suelto, (48, 48), (24, 12), ("verde_calido", "verde_frio"), "#0F2A17", SOMBRA_CHICA, 0.4),
    "Roca_Basalto": (roca, (48, 48), (24, 14), ("basalto", "musgo", "verde_calido"), "#1E2420", SOMBRA_CHICA, 0.5),
    "Nenufar": (nenufar, (32, 32), (16, 14), ("verde_calido", "acentos", "tierra"), "#0B4F4A", None, 0.45),
    "Tule": (tule, (32, 64), (16, 8), ("verde_calido", "madera"), "#0F2A17", ((64, 40), (20, 20)), 0.4),
    "Sombra_Dosel": (dosel, (160, 96), (80, 48), None, None, None, 0.3),
}
VARIANTES = {"Tile_Camino_Tierra": 4, "Tile_Pasto_Selva": 3, "Tile_Agua_Anim": 4, "Cerca_Basalto": 3,
             "Palma_Corozo": 2, "Arbusto_Selva": 2, "Matorral_Selva": 3, "Pasto_Alto": 2, "Helecho": 2,
             "Roca_Basalto": 3, "Nenufar": 1, "Tule": 1, "Sombra_Dosel": 2}
# Las transiciones se generan solo para las máscaras que usa la composición (ver planificar)
SIN_AVISO_BORDE = {"Tile_Camino_Tierra", "Tile_Pasto_Selva", "Tile_Agua_Anim", "Tile_Camino_Trans", "Tile_Orilla"}


def producir(salida, tmp, tipo, variante):
    constructor, lienzo, ancla, rampas, color_contorno, cfg_sombra, umbral = TIPOS[tipo]
    nombre = f"{tipo}_{variante:02d}"
    pa.nueva_escena()
    constructor(random.Random(zlib.crc32(nombre.encode())), variante)
    if tipo.startswith("Tile_"):   # solapamiento de ~1 px: sin él quedan huecos en escalera entre tiles vecinos
        for o in bpy.context.scene.objects:
            if o.type == 'MESH':
                o.scale.x *= SOLAPE_TILE
                o.scale.y *= SOLAPE_TILE
                o.location.x *= SOLAPE_TILE
                o.location.y *= SOLAPE_TILE
    meta = {"lienzo": lienzo, "pivote_px": ancla, "ppu": pa.PPU}

    if rampas is None:   # solo sombra (dosel fuera de cuadro)
        for o in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
            o.visible_camera = False
        bpy.ops.mesh.primitive_plane_add(size=12)
        bpy.context.active_object.is_shadow_catcher = True
        pa.encuadrar(*lienzo, ancla)
        s = pa.sombra(pa.renderizar(os.path.join(tmp, nombre + "_4x.png")), opacidad=0.32, umbral=umbral)
        if pa.toca_borde(s):
            print("AVISO: la sombra toca el borde del lienzo:", nombre)
        pa.guardar(s, os.path.join(salida, nombre + ".png"))
        return nombre, meta

    pa.encuadrar(*lienzo, ancla)
    px = pa.reducir(pa.renderizar(os.path.join(tmp, nombre + "_4x.png")), umbral=umbral)
    px = pa.limpiar(pa.cuantizar(px, pa.paleta(*rampas)))
    if tipo.startswith("Tile_"):
        pa.unificar_borde_trasero(px, lienzo[1] - ancla[1])
    if color_contorno:
        muro = bpy.context.scene.objects.get("Muro")
        if muro:   # contorno sin el muro: los módulos de cerca empalman sin muescas
            muro.visible_camera = False
            mascara = pa.reducir(pa.renderizar(os.path.join(tmp, nombre + "_mascara_4x.png")), umbral=umbral)
            muro.visible_camera = True
            pa.contorno_mascara(px, mascara, color_contorno)
        else:
            pa.contorno(px, color_contorno)
    if tipo not in SIN_AVISO_BORDE and pa.toca_borde(px):
        print("AVISO: el sprite toca el borde del lienzo:", nombre)
    pa.guardar(px, os.path.join(salida, nombre + ".png"))

    if cfg_sombra:
        objetos = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        bpy.ops.mesh.primitive_plane_add(size=12)
        bpy.context.active_object.is_shadow_catcher = True
        for o in objetos:
            o.visible_camera = False
        pa.encuadrar(*cfg_sombra[0], cfg_sombra[1])
        s = pa.sombra(pa.renderizar(os.path.join(tmp, nombre + "_sombra_4x.png")))
        if pa.toca_borde(s):
            print("AVISO: la sombra toca el borde del lienzo:", nombre)
        pa.guardar(s, os.path.join(salida, nombre + "_Sombra.png"))
        meta["sombra"] = {"lienzo": cfg_sombra[0], "pivote_px": cfg_sombra[1]}
    return nombre, meta


def pegar(lienzo, spr, x, y):
    """Mezcla alfa de spr (fila 0 = arriba) con su esquina superior izquierda en (x, y)."""
    H, W, _ = lienzo.shape
    h, w, _ = spr.shape
    x0, y0, x1, y1 = max(0, x), max(0, y), min(W, x + w), min(H, y + h)
    if x0 >= x1 or y0 >= y1:
        return
    s = spr[y0 - y:y1 - y, x0 - x:x1 - x]
    a = s[..., 3:4]
    d = lienzo[y0:y1, x0:x1]
    d[..., :3] = s[..., :3] * a + d[..., :3] * (1 - a)
    d[..., 3:4] = a + d[..., 3:4] * (1 - a)


def hoja_variantes(salida, nombres, archivo, k=4, por_fila=8):
    """Variantes de un tipo en filas, ampliadas."""
    imgs = [pa.cargar(os.path.join(salida, n + ".png")) for n in nombres]
    filas = [imgs[i:i + por_fila] for i in range(0, len(imgs), por_fila)]
    sep = 4
    alto_fila = max(i.shape[0] for i in imgs) + sep
    ancho = max(sum(i.shape[1] + sep for i in fila) for fila in filas) + sep
    hoja_ = pa.sobre_fondo(pa.np.zeros((alto_fila * len(filas) + sep, ancho, 4), dtype=pa.np.float32), "#13241A")
    for r, fila in enumerate(filas):
        x = sep
        for i in fila:
            pegar(hoja_, i, x, sep + r * alto_fila + alto_fila - sep - i.shape[0])
            x += i.shape[1] + sep
    pa.guardar(pa.ampliar(hoja_, k), os.path.join(salida, archivo))


# ---------------------------------------------------------------- composición

W_COMP, H_COMP = 640, 360


def pantalla(X, Y, Z=0.0):
    p = Vector((X, Y, Z))
    return round(W_COMP / 2 + pa.PPU * p.dot(pa.DERECHA)), round(H_COMP / 2 - pa.PPU * p.dot(pa.ARRIBA))


def planificar():
    """Diseño de la escena de prueba: tipos de celda, transiciones necesarias y objetos.
    Devuelve el plan y las máscaras de transición que hay que generar."""
    rng = random.Random(5)

    def visible(X, Y, margen=80):
        sx, sy = pantalla(X, Y)
        return -margen < sx < W_COMP + margen and -margen < sy < H_COMP + 2 * margen

    centro_camino = lambda X: round(1.0 * math.sin(X * 0.35))
    estanque = lambda X, Y: ((X + 1.0) ** 2) / 5.0 + ((Y + 6.6) ** 2) / 3.2 < 1.0

    celdas = sorted(((X, Y) for X in range(-14, 15) for Y in range(-14, 15) if visible(X, Y)), key=lambda p: p[0] - p[1])
    tipo_celda = {}
    for X, Y in celdas:
        yc = centro_camino(X)
        tipo_celda[(X, Y)] = "agua" if estanque(X, Y) else "camino" if Y in (yc - 1, yc) else "pasto"

    def mascara(X, Y, es_otro):
        m = 0
        for bit, (dx, dy) in DIR4.items():
            if es_otro(tipo_celda.get((X + dx, Y + dy))):
                m |= bit
        for bit, (sx, sy, _, _) in DIAG.items():
            if es_otro(tipo_celda.get((X + sx, Y + sy))):
                m |= bit
        return normalizar_mascara(m)

    suelo, mascaras = [], {"Tile_Camino_Trans": set(), "Tile_Orilla": set()}
    for X, Y in celdas:
        t = tipo_celda[(X, Y)]
        if t == "agua":
            suelo.append(("AGUA", X, Y))
            m = mascara(X, Y, lambda o: o in ("pasto", "camino"))
            if m:
                suelo.append((f"Tile_Orilla_{m:02d}", X, Y))
                mascaras["Tile_Orilla"].add(m)
        elif t == "camino":
            m = mascara(X, Y, lambda o: o == "pasto")
            if m:
                suelo.append((f"Tile_Camino_Trans_{m:02d}", X, Y))
                mascaras["Tile_Camino_Trans"].add(m)
            else:
                suelo.append((f"Tile_Camino_Tierra_{rng.choice([1, 1, 2, 4]):02d}", X, Y))
        else:
            suelo.append((f"Tile_Pasto_Selva_{rng.choice([1, 1, 2, 3]):02d}", X, Y))

    objetos = [(f"Cerca_Basalto_{[1, 2, 1, 3, 2][(i + 8) % 5]:02d}", -7 + i * 1.1, 3.0, 0.0) for i in range(12)]
    ocupados = [(o[1], o[2]) for o in objetos]

    def libre(X, Y, dist):
        return all((X - a) ** 2 + (Y - b) ** 2 >= dist * dist for a, b in ocupados)

    def sembrar(nombres, cantidad, permitido, dist, z=0.0, intentos=600):
        for _ in range(intentos):
            if cantidad == 0:
                return
            X, Y = rng.uniform(-12, 12), rng.uniform(-12, 12)
            celda = (round(X), round(Y))
            if celda in tipo_celda and permitido(celda, X, Y) and visible(X, Y, 20) and libre(X, Y, dist):
                objetos.append((rng.choice(nombres), X, Y, z))
                ocupados.append((X, Y))
                cantidad -= 1

    en_pasto = lambda celda, X, Y: tipo_celda[celda] == "pasto" and abs(Y - 3.0) > 0.6
    junto_a = lambda tipo: (lambda celda, X, Y: tipo_celda[celda] == "pasto" and any(
        tipo_celda.get((celda[0] + dx, celda[1] + dy)) == tipo for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))))
    pie_cerca = lambda celda, X, Y: tipo_celda[celda] == "pasto" and 0.5 < abs(Y - 3.0) < 1.1 and -7.5 < X < 5.5
    en_agua = lambda celda, X, Y: tipo_celda[celda] == "agua" and estanque(X, Y)
    matorrales = ["Matorral_Selva_01", "Matorral_Selva_02", "Matorral_Selva_03"]

    sembrar(["Palma_Corozo_01", "Palma_Corozo_02"], 13, en_pasto, 1.4)
    sembrar(matorrales, 9, pie_cerca, 0.7)
    sembrar(matorrales, 8, junto_a("agua"), 0.8)
    sembrar(["Tule_01"], 7, junto_a("agua"), 0.35)
    sembrar(["Nenufar_01"], 4, en_agua, 0.6, z=-0.06)
    sembrar(matorrales, 10, en_pasto, 0.9)
    sembrar(["Pasto_Alto_01", "Pasto_Alto_02"], 10, junto_a("camino"), 0.5)
    sembrar(["Arbusto_Selva_01", "Arbusto_Selva_01", "Arbusto_Selva_02"], 10, en_pasto, 0.8)
    sembrar(["Pasto_Alto_01", "Pasto_Alto_02"], 18, en_pasto, 0.5)
    sembrar(["Helecho_01", "Helecho_02"], 18, en_pasto, 0.6)
    sembrar(["Roca_Basalto_01", "Roca_Basalto_02", "Roca_Basalto_03"], 7, lambda c_, X, Y: tipo_celda[c_] != "agua", 0.9)

    dosel_ = [(f"Sombra_Dosel_{1 + i % 2:02d}", rng.uniform(-9, 9), rng.uniform(-9, 9)) for i in range(16)]
    dosel_ = [d for d in dosel_ if visible(d[1], d[2])][:9]
    return {"suelo": suelo, "objetos": objetos, "dosel": dosel_}, mascaras


def componer(salida, metas, plan, cuadro_agua=1, archivo="Composicion"):
    """Escena de prueba 640x360 con la proyección de la cámara del juego. No es una captura de Unity."""
    lienzo = pa.sobre_fondo(pa.np.zeros((H_COMP, W_COMP, 4), dtype=pa.np.float32), "#13241A")
    cache = {}

    def img(n):
        if n not in cache:
            cache[n] = pa.cargar(os.path.join(salida, n + ".png"))
        return cache[n]

    def poner(n, X, Y, Z=0.0, sombra=False):
        meta = metas[n]["sombra"] if sombra else metas[n]
        i = img(n + "_Sombra") if sombra else img(n)
        sx, sy = pantalla(X, Y, Z)
        pegar(lienzo, i, sx - meta["pivote_px"][0], sy - (i.shape[0] - meta["pivote_px"][1]))

    for n, X, Y in plan["suelo"]:   # ya ordenado por profundidad; la orilla va justo después de su agua
        poner(f"Tile_Agua_Anim_{cuadro_agua:02d}" if n == "AGUA" else n, X, Y)
    for n, X, Y in plan["dosel"]:
        poner(n, X, Y)
    for n, X, Y, Z in plan["objetos"]:
        if "sombra" in metas[n]:
            poner(n, X, Y, Z, sombra=True)
    for n, X, Y, Z in sorted(plan["objetos"], key=lambda o: o[1] - o[2]):
        poner(n, X, Y, Z)

    pa.guardar(lienzo, os.path.join(salida, f"{archivo}_640x360.png"))
    pa.guardar(pa.ampliar(lienzo, 3), os.path.join(salida, f"{archivo}_1920x1080_x3.png"))


def main():
    salida = os.path.abspath(sys.argv[sys.argv.index("--") + 1])
    if os.path.isdir(salida):
        shutil.rmtree(salida)   # la carpeta solo contiene salidas generadas por este script
    tmp = os.path.join(salida, "_render")
    os.makedirs(tmp)
    plan, mascaras = planificar()
    lotes = [(tipo, list(range(1, n + 1))) for tipo, n in VARIANTES.items()]
    lotes += [(tipo, sorted(ms)) for tipo, ms in mascaras.items()]
    metas = {}
    for tipo, variantes in lotes:
        nombres = []
        for variante in variantes:
            nombre, meta = producir(salida, tmp, tipo, variante)
            metas[nombre] = meta
            nombres.append(nombre)
        hoja_variantes(salida, nombres, f"{tipo}_variantes_x4.png")
    with open(os.path.join(salida, "pivotes.json"), "w", encoding="utf-8") as fh:
        json.dump(metas, fh, indent=2)
    componer(salida, metas, plan)
    shutil.rmtree(tmp)
    print("LISTO", salida, len(metas), "sprites")


main()
