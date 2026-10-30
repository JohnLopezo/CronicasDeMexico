# Recursos Pixel Art del entorno olmeca (flujo Blender -> Pixel Art) y composición isométrica de prueba.
# Uso (sin abrir Blender):
#   "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup ^
#       --python Tools\ArtPipeline\generar_olmeca.py -- Tools\ArtPipeline\Salida\Olmeca [--sin-unity]
# Además de las salidas de revisión, copia los sprites del juego y Ciudad.json a Assets (ver exportar_unity).
import json
import math
import os
import random
import shutil
import sys
import zlib

import bpy
from mathutils import Euler, Matrix, Vector, noise

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


def cerca_basalto_y(rng, variante):
    """La cerca de basalto girada 90°: corre a lo largo de Y (lados este y oeste de un recinto)."""
    cerca_basalto(rng, variante)
    bpy.context.view_layer.update()   # sin esto matrix_world no refleja la posición ni la escala recién asignadas
    giro = Matrix.Rotation(math.radians(90), 4, 'Z')
    for o in bpy.context.scene.objects:
        if o.type == 'MESH':
            o.matrix_world = giro @ o.matrix_world


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


# ---------------------------------------------------------------- plataforma de arcilla (módulos por máscara de vecinos)

# Bits 1, 2, 4, 8 (DIR4): ese lado da al exterior y lleva talud. Bits 16–128 (DIAG): esquina cóncava,
# los dos lados cardinales tienen vecino pero la diagonal no. Los módulos se apilan (nivel superior en Z = H).
H_PLATAFORMA = 1.0
TALUD = 0.4
# Rellenos de arcillas de colores de La Venta (§8), de abajo arriba: (grosor en m, color). Solo colores de la paleta.
CAPAS_ARCILLA = [(0.1, "#644B2A"), (0.09, "#B16143"), (0.13, "#C6925A"), (0.08, "#7D745B"), (0.12, "#9B713F"),
                 (0.07, "#B16143"), (0.13, "#E3B47C"), (0.09, "#9C8B75"), (0.19, "#7B5B33")]
CARCAVAS = {1: (0.13, -0.31), 2: (0.27,), 4: (-0.18, 0.36), 8: (0.05, -0.38)}   # posición t de las cárcavas por lado
BORDE_TRASERO = "#1D2B1A"
PASTO_LABIO = ["#23551C", "#387022", "#568B26"]


def ancho_talud(t, bit):
    # Periódica en t como ancho_borde: módulos contiguos del mismo lado empalman. Ondulación suave e irregular
    # más cárcavas estrechas donde la lluvia se come el talud
    fase = FASE[bit]
    w = 1 + 0.07 * math.sin(2 * math.pi * t + fase) + 0.05 * math.sin(6 * math.pi * t + 2 * fase)
    for c in CARCAVAS[bit]:
        dt = (t - c + 0.5) % 1.0 - 0.5
        w += 0.45 * math.exp(-(dt / 0.035) ** 2)
    return TALUD * w


def perfil_talud(d, w):
    s = max(0.0, min(1.0, d / w))
    return s * s * (3 - 2 * s)   # pie y labio redondeados por la erosión


def altura_plataforma(x, y, mascara):
    """Fracción de H en (x, y). Lados con talud: mínimo de sus perfiles. Esquina cóncava: máximo de los perfiles
    de los dos vecinos, que en el borde del tile coincide con el del vecino."""
    s = 1.0
    for bit, (dx, dy) in DIR4.items():
        if mascara & bit:
            s = min(s, perfil_talud(0.5 - (x * dx + y * dy), ancho_talud(y if dx else x, bit)))
    for bit, (sx, sy, bx, by) in DIAG.items():
        if mascara & bit:
            s = min(s, max(perfil_talud(0.5 - y * sy, ancho_talud(x, by)),
                           perfil_talud(0.5 - x * sx, ancho_talud(y, bx))))
    return s


def relieve_talud(x, y):
    # Ruido periódico de período 1 en x e y (semilla fija): continuo entre módulos
    ax, ay = 2 * math.pi * x, 2 * math.pi * y
    return (noise.noise(Vector((math.cos(ax), math.sin(ax), math.cos(ay) + 3.1)))
            + noise.noise(Vector((math.cos(ay) + 7.3, math.sin(ay), math.cos(ax)))))


def color_capa(x, y, z):
    # Capas casi horizontales con ondulación periódica (continua entre módulos)
    z += 0.014 * math.sin(4 * math.pi * (x + y)) + 0.009 * math.sin(6 * math.pi * (x - y))
    acum = 0.0
    for grosor, c in CAPAS_ARCILLA:
        acum += grosor
        if z < acum:
            return c
    return CAPAS_ARCILLA[-1][1]


def plataforma_arcilla(rng, mascara):
    n = 48
    paso = 1.0 / n
    verts, cols, fracs = [], [], []
    for i in range(n + 1):
        for j in range(n + 1):
            x, y = -0.5 + i * paso, -0.5 + j * paso
            s = altura_plataforma(x, y, mascara)
            z = H_PLATAFORMA * s + 0.03 * 4 * s * (1 - s) * relieve_talud(x, y)
            verts.append(Vector((x, y, z)))
            fracs.append(s)
            if s > 0.95:   # el pasto de la cima cae un poco sobre el labio
                cols.append(PASTO_LABIO[int((relieve_talud(x * 3, y * 3) + 1.2) * 1.25) % 3])
            else:
                cols.append(color_capa(x, y, z))
    idx = lambda i, j: i * (n + 1) + j
    caras, cima = [], []
    for i in range(n):
        for j in range(n):
            q = (idx(i, j), idx(i + 1, j), idx(i + 1, j + 1), idx(i, j + 1))
            caras.append(q)
            cima.append(all(fracs[k] > 0.995 for k in q))

    # Faldón hasta el suelo en el perímetro: en la composición lo tapa el módulo vecino; evita huecos al verlo suelto
    borde = ([idx(i, 0) for i in range(n)] + [idx(n, j) for j in range(n)]
             + [idx(i, n) for i in range(n, 0, -1)] + [idx(0, j) for j in range(n, 0, -1)])
    for a, b in zip(borde, borde[1:] + borde[:1]):
        if verts[a].z > 0.01 or verts[b].z > 0.01:
            k = len(verts)
            for q in (a, b):
                verts += [verts[q].copy(), Vector((verts[q].x, verts[q].y, 0))]
                cols += [color_capa(verts[q].x, verts[q].y, verts[q].z - 0.02), CAPAS_ARCILLA[0][1]]
            caras.append((k, k + 2, k + 3, k + 1))
            cima.append(False)

    sup = pa.malla("Plataforma", verts, caras, cols)
    sup.data.materials.append(pa.mat_color_vertice("Arcilla"))
    sup.data.materials.append(pa.mat_ruido("Pasto", PASTO_SUELO, escala=3, desde=0.36, hasta=0.64))
    for poly, es_cima in zip(sup.data.polygons, cima):
        poly.material_index = int(es_cima)

    def altura(x, y):
        return H_PLATAFORMA * altura_plataforma(x, y, mascara)

    v, f, c = [], [], []
    mat_terron = pa.mat_ruido("Terron", ["#644B2A", "#9B713F", "#B16143", "#C6925A"], escala=25)
    for bit, (dx, dy) in DIR4.items():
        if not mascara & bit:
            continue
        for _ in range(rng.randint(3, 4)):   # mechones en el labio del talud
            t = rng.uniform(-0.42, 0.42)
            d = ancho_talud(t, bit) * rng.uniform(0.85, 1.0)
            x, y = (dx * (0.5 - d), t) if dx else (t, dy * (0.5 - d))
            matas(v, f, c, (x, y, altura(x, y)), rng.randint(8, 12), 0.13, 0.05, rng, grosor=0.02)
        for _ in range(rng.randint(1, 2)):   # pasto al pie
            t = rng.uniform(-0.42, 0.42)
            x, y = (dx * 0.46, t) if dx else (t, dy * 0.46)
            matas(v, f, c, (x, y, 0), rng.randint(6, 9), 0.1, 0.04, rng, grosor=0.018)
        for _ in range(rng.randint(1, 3)):   # terrones desprendidos al pie
            t = rng.uniform(-0.4, 0.4)
            d = rng.uniform(0.02, 0.08)
            x, y = (dx * (0.5 - d), t) if dx else (t, dy * (0.5 - d))
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=rng.uniform(0.025, 0.04), location=(x, y, altura(x, y)))
            g = bpy.context.active_object
            g.scale = (1, rng.uniform(0.7, 1), rng.uniform(0.5, 0.7))
            pa.asignar(g, mat_terron)
    for _ in range(rng.randint(1, 3)):   # matas sueltas en la cima
        x, y = rng.uniform(-0.38, 0.38), rng.uniform(-0.38, 0.38)
        if altura_plataforma(x, y, mascara) > 0.99:
            matas(v, f, c, (x, y, H_PLATAFORMA), rng.randint(6, 9), 0.1, 0.05, rng, grosor=0.02)
    if v:
        pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


def mascara_plataforma(celdas, X, Y):
    m = 0
    for bit, (dx, dy) in DIR4.items():
        if (X + dx, Y + dy) not in celdas:
            m |= bit
    for bit, (sx, sy, a, b) in DIAG.items():
        if not m & (a | b) and (X + sx, Y + sy) not in celdas:
            m |= bit
    return m


# ---------------------------------------------------------------- altar-trono

def caja(nombre, centro, medidas, bisel):
    bpy.ops.mesh.primitive_cube_add(size=1, location=centro)
    o = bpy.context.active_object
    o.name = nombre
    o.scale = medidas
    bpy.ops.object.transform_apply(scale=True)
    if bisel:
        bev = o.modifiers.new("Bisel", 'BEVEL')
        bev.width = bisel
        bev.segments = 2
    return o


def restar(obj, cortador):
    cortador.hide_render = True
    mod = obj.modifiers.new("Nicho", 'BOOLEAN')
    mod.operation = 'DIFFERENCE'
    mod.object = cortador
    obj.modifiers.move(len(obj.modifiers) - 1, 0)   # antes del bisel


def altar_trono(rng, variante):
    """Altar-trono de basalto (tipo Altar 4 de La Venta, a escala reducida: 2,0 × 1,3 × 1,1 m). Bloque con nicho
    frontal de arco rebajado en la cara -Y (la iluminada) y cornisa saliente. Sin relieves; el nicho queda vacío
    hasta decidir la figura (§8: la figura es D, su ausencia es I)."""
    cuerpo = caja("Cuerpo", (0, 0, 0.42), (2.0, 1.3, 0.84), 0.035)
    cornisa = caja("Cornisa", (0, -0.1, 0.97), (2.24, 1.62, 0.26), 0.035)   # sobresale más por el frente
    nicho = caja("NichoCaja", (0, -0.65, 0.3), (0.78, 0.9, 0.4), 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.39, depth=0.9, location=(0, -0.65, 0.42),
                                        rotation=(math.pi / 2, 0, 0))
    arco = bpy.context.active_object
    restar(cuerpo, nicho)
    restar(cuerpo, arco)
    # Basalto pulido: rampa corta y ruido grande (la piedra labrada no es tan moteada como la roca suelta);
    # el musgo queda en manchas sobre la cornisa y en la base
    mat = pa.mat_piedra_musgo("BasaltoAltar", ["#4A473B", "#615C48", "#7D745B", "#9C8B75"],
                              ["#3E5F27", "#5A8732", "#84B345"], escala=3.5, estirar=(1, 1, 1), altura_musgo=0.2,
                              umbral_musgo=0.66)
    for o in (cuerpo, cornisa):
        pa.asignar(o, mat)

    # Fragmentos desportillados de la cornisa al pie y lajas hundidas
    mat_frag = pa.mat_ruido("Fragmento", ["#36382F", "#4A473B", "#615C48", "#7D745B"], escala=9)
    for x, y in ((0.75, -0.84), (-0.7, -0.9), (1.16, 0.2)):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=rng.uniform(0.05, 0.08),
                                              location=(x + rng.uniform(-0.05, 0.05), y, 0.02))
        g = bpy.context.active_object
        g.scale = (1, rng.uniform(0.7, 1), rng.uniform(0.45, 0.65))
        g.rotation_euler.z = rng.uniform(0, math.pi)
        pa.asignar(g, mat_frag)

    v, f, c = [], [], []
    for _ in range(9):   # pasto contra la base, en los dos lados visibles
        if rng.random() < 0.55:
            centro = (rng.uniform(-0.95, 0.95), -0.68, 0)
        else:
            centro = (1.03, rng.uniform(-0.6, 0.6), 0)
        matas(v, f, c, centro, rng.randint(8, 12), 0.16, 0.06, rng, grosor=0.022)
    helecho(v, f, c, (-0.95, -0.78, 0), 0.9, rng)
    helecho(v, f, c, (1.08, 0.5, 0), 0.75, rng)
    matas(v, f, c, (0.0, -0.62, 0.12), 6, 0.08, 0.1, rng, grosor=0.02)   # pasto que entra en el nicho
    pa.asignar(pa.malla("Plantas", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- estelas lisas

def losa(nombre, ancho, grueso, alto, mat):
    """Losa de basalto con la cima algo más estrecha y aristas redondeadas, con el origen en el centro de la base."""
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.name = nombre
    for vt in o.data.vertices:
        vt.co.x *= ancho * (0.9 if vt.co.z > 0 else 1.0)
        vt.co.y *= grueso * (0.9 if vt.co.z > 0 else 1.0)
        vt.co.z = (vt.co.z + 0.5) * alto
    bev = o.modifiers.new("Bisel", 'BEVEL')
    bev.width = min(0.07, grueso * 0.3)
    bev.segments = 3
    return pa.asignar(o, mat)


def mat_estela():
    return pa.mat_piedra_musgo("BasaltoEstela", ["#36382F", "#4A473B", "#615C48", "#7D745B", "#9C8B75"],
                               ["#3E5F27", "#5A8732", "#84B345"], escala=4, estirar=(1, 1, 0.6), altura_musgo=0.3,
                               umbral_musgo=0.5)


def pie_estela(rng, radio, n=4):
    v, f, c = [], [], []
    for _ in range(n):
        a = rng.uniform(-math.pi, 0.5)   # sobre todo por delante (lados -Y y +X, los visibles)
        matas(v, f, c, (math.cos(a) * radio, math.sin(a) * radio, 0), rng.randint(7, 10), 0.14, 0.05, rng, grosor=0.02)
    return v, f, c


def estela(rng, variante):
    """Estela lisa pequeña (0,55 × 0,22 × 1,1 m), hundida 5 cm, con la cara ancha hacia -Y."""
    o = losa("Estela", 0.55, 0.22, 1.15, mat_estela())
    o.location.z = -0.05
    o.rotation_euler.z = math.radians(rng.uniform(-8, 8))
    v, f, c = pie_estela(rng, 0.2)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


def estela_grande(rng, variante):
    """Estela lisa grande (0,9 × 0,3 × 1,35 m). Variantes: 1 en pie, 2 inclinada y medio hundida, 3 rota con el
    trozo superior caído."""
    mat = mat_estela()
    v, f, c = [], [], []
    if variante == 1:
        o = losa("Estela", 0.9, 0.3, 1.4, mat)
        o.location.z = -0.05
        o.rotation_euler.z = math.radians(rng.uniform(-6, 6))
        v, f, c = pie_estela(rng, 0.3, 5)
    elif variante == 2:
        o = losa("Estela", 0.9, 0.3, 1.4, mat)
        o.location = (0, 0.05, -0.18)
        o.rotation_euler = Euler((math.radians(14), math.radians(-4), math.radians(4)))   # vencida hacia -Y (hacia la cámara)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.3, location=(0, -0.08, -0.16))
        monticulo = bpy.context.active_object   # tierra removida al pie, casi cubierta de pasto
        monticulo.scale = (1.2, 0.75, 0.45)
        pa.asignar(monticulo, pa.mat_ruido("TierraEstela", ["#3A2A1A", "#644B2A", "#7B5B33"], escala=9))
        v, f, c = pie_estela(rng, 0.3, 8)
    else:
        tocon = losa("Tocon", 0.9, 0.3, 0.75, mat)
        tocon.location.z = -0.05
        # Fractura irregular: dos cortes en ángulos opuestos dejan un pico y una muesca
        for x, z, ry in ((0.42, 0.72, -32), (-0.4, 0.86, 24)):
            bpy.ops.mesh.primitive_cube_add(size=1, location=(x, 0, z))
            corte = bpy.context.active_object
            corte.scale = (0.9, 0.8, 0.3)
            corte.rotation_euler = Euler((math.radians(rng.uniform(-8, 8)), math.radians(ry), 0))
            restar(tocon, corte)
        # Trozo superior caído de plano delante de la base, sin musgo en la cara que quedó arriba (era la de atrás)
        trozo = losa("Trozo", 0.8, 0.26, 0.58, pa.mat_ruido("BasaltoCaido2", ["#36382F", "#4A473B", "#615C48", "#7D745B"],
                                                           escala=5))
        trozo.rotation_euler = Euler((math.radians(90), 0, math.radians(14)))
        trozo.location = (0.08, -0.2, 0.13)
        for x, y in ((0.32, 0.12), (-0.2, -0.3)):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=rng.uniform(0.04, 0.06), location=(x, y, 0.02))
            g = bpy.context.active_object
            g.scale = (1, 0.8, 0.55)
            pa.asignar(g, pa.mat_ruido("Fragmento", ["#36382F", "#4A473B", "#615C48", "#7D745B"], escala=9))
        v, f, c = pie_estela(rng, 0.34, 5)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- rampa de arcilla

ANCHO_TALUD_RAMPA = 0.18


def rampa_arcilla(rng, variante):
    """Rampa de tierra de 2 m de largo por 1 m de ancho que sube H_PLATAFORMA (pendiente de ~27°). Se coloca pegada
    a un lado con talud de la plataforma; su extremo alto rellena ese talud (se dibuja después, por delante).
    Origen: centro de su huella. Variantes: 1 sube hacia +Y (va en el lado -Y de la plataforma),
    2 sube hacia -X (va en el lado +X)."""
    largo, relleno = 2.0, TALUD + 0.05
    n_u, n_v = 60, 24
    verts, cols = [], []
    for i in range(n_u + 1):
        u = -largo / 2 + (largo + relleno) * i / n_u   # a lo largo: -1 al pie, +1 en el borde de la plataforma
        for j in range(n_v + 1):
            w = -0.5 + j / n_v                           # a lo ancho
            subida = min(1.0, max(0.0, (u + largo / 2) / largo))
            lado = perfil_talud(0.5 - abs(w), ANCHO_TALUD_RAMPA)
            z = H_PLATAFORMA * min(subida, lado)
            x, y = (w, u) if variante == 1 else (-u, w)
            verts.append(Vector((x, y, z)))
            if lado > 0.97:   # piso apisonado, con pasto en las orillas
                cols.append(PASTO_LABIO[1] if lado < 0.995 and rng.random() < 0.5 else rng.choice(TIERRA_CAMINO[1:4]))
            else:
                cols.append(color_capa(x, y, z))
    idx = lambda i, j: i * (n_v + 1) + j
    caras = [(idx(i, j), idx(i + 1, j), idx(i + 1, j + 1), idx(i, j + 1)) for i in range(n_u) for j in range(n_v)]
    pa.asignar(pa.malla("Rampa", verts, caras, cols), pa.mat_color_vertice("Arcilla"))

    v, f, c = [], [], []
    for _ in range(6):   # mechones en los bordes
        u = rng.uniform(-0.9, 0.9)
        w = rng.choice((-1, 1)) * 0.42
        x, y = (w, u) if variante == 1 else (-u, w)
        matas(v, f, c, (x, y, H_PLATAFORMA * min(1.0, (u + 1) / 2) * 0.8), rng.randint(6, 9), 0.12, 0.04, rng, grosor=0.02)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- aldea: casas de bajareque y fogón

PAJA = ["#6D502E", "#9C7842", "#A28A6D", "#C6925A"]
BAJAREQUE = ["#B6814C", "#C6925A", "#E3B47C", "#C6925A"]
VARAS = ["#6D502E", "#9C7842"]


def perimetro(a, b, r, n):
    """n puntos equiespaciados sobre un rectángulo redondeado (semiejes a, b; radio de esquina r). Todos los
    anillos empiezan en el mismo sitio relativo, así que se pueden unir anillo con anillo. r = b da extremos
    semicirculares (casa absidal); a = b = r, un círculo."""
    r = max(0.0, min(r, a, b))
    denso = []
    for k, (cx, cy) in enumerate(((a - r, b - r), (-(a - r), b - r), (-(a - r), -(b - r)), (a - r, -(b - r)))):
        for m in range(17):
            ang = math.pi / 2 * (k + m / 16)
            denso.append(Vector((cx + r * math.cos(ang), cy + r * math.sin(ang), 0)))
    largo = [0.0]
    for p, q in zip(denso, denso[1:] + denso[:1]):
        largo.append(largo[-1] + (q - p).length)
    total = largo[-1]
    pts, k = [], 0
    for i in range(n):
        t = total * i / n
        while largo[k + 1] < t:
            k += 1
        p, q = denso[k], denso[(k + 1) % len(denso)]
        seg = largo[k + 1] - largo[k]
        pts.append(p.lerp(q, (t - largo[k]) / seg if seg > 1e-9 else 0.0))
    return pts


def basamento(rng, a, b, r, alto=0.2, margen=0.3):
    """Basamento bajo de tierra apisonada con taludes."""
    n = 96
    abajo = perimetro(a + margen + 0.15, b + margen + 0.15, r + margen + 0.15, n)
    arriba = perimetro(a + margen, b + margen, r + margen, n)
    verts = [p.copy() for p in abajo] + [p + Vector((0, 0, alto)) for p in arriba]
    caras = [(j, (j + 1) % n, n + (j + 1) % n, n + j) for j in range(n)] + [tuple(range(n, 2 * n))]
    cols = [rng.choice(["#644B2A", "#7B5B33"]) for _ in abajo] + [rng.choice(["#9B713F", "#B6814C"]) for _ in arriba]
    pa.asignar(pa.malla("Basamento", verts, caras, cols), pa.mat_color_vertice("Tierra"))


def muros_bajareque(rng, a, b, r, z0, alto, puerta=0.28, alto_puerta=0.82):
    """Muros de bajareque: enlucido de barro con desconchones donde asoma el entramado de varas.
    Puerta en el lado -Y (el iluminado)."""
    n = max(48, int(4 * (a + b) / 0.04))
    filas = 24
    anillo = perimetro(a, b, r, n)
    sem = rng.uniform(0, 50)
    verts, cols = [], []
    for fila in range(filas + 1):
        z = z0 + alto * fila / filas
        for j, p in enumerate(anillo):
            verts.append(p + Vector((0, 0, z)))
            if z - z0 < 0.08:
                cols.append("#7B5B33")   # salpicado de barro al pie
            elif noise.noise(Vector((p.x * 3 + sem, p.y * 3, z * 3))) > 0.28:
                cols.append(VARAS[j % 2])
            else:
                cols.append(BAJAREQUE[int((noise.noise(Vector((p.x * 9, p.y * 9 + sem, z * 9))) + 1) * 2) % 4])
    caras = []
    for fila in range(filas):
        zc = alto * (fila + 0.5) / filas
        for j in range(n):
            p = anillo[j].lerp(anillo[(j + 1) % n], 0.5)
            if p.y < -b + 0.02 and abs(p.x) < puerta and zc < alto_puerta:
                continue   # vano de la puerta: el interior queda en sombra
            caras.append((fila * n + j, fila * n + (j + 1) % n, (fila + 1) * n + (j + 1) % n, (fila + 1) * n + j))
    pa.asignar(pa.malla("Muros", verts, caras, cols), pa.mat_color_vertice("Bajareque"))
    mat = pa.mat_ruido("Jamba", ["#48331F", "#6D502E"], escala=20)   # jambas y dintel de madera
    for x in (-puerta - 0.02, puerta + 0.02):
        pa.asignar(caja("Jamba", (x, -b - 0.005, z0 + alto_puerta / 2), (0.05, 0.05, alto_puerta), 0), mat)
    pa.asignar(caja("Dintel", (0, -b - 0.005, z0 + alto_puerta + 0.025), (2 * puerta + 0.12, 0.05, 0.05), 0), mat)


def techo_palma(rng, a, b, r, z_alero, z_cumbrera, n=120, anillos=16):
    """Techo de palma a cuatro aguas sobre un rectángulo redondeado: los anillos se encogen hacia la cumbrera
    (b llega a 0; a se acorta lo mismo). Alero con flecos irregulares; la paja forma vetas en el sentido de la
    pendiente y solo hay una franja de amarre discreta a media altura."""
    verts, cols = [], []
    for k in range(anillos + 1):
        t = k / anillos
        bk, ak, rk = b * (1 - t), a - b * t, r * (1 - t)
        z = z_alero + (z_cumbrera - z_alero) * t
        for p in perimetro(ak, bk, rk, n):
            if k == 0:   # flecos: el borde cuelga de forma irregular
                p = p * (1 + rng.uniform(0, 0.03)) + Vector((0, 0, -rng.uniform(0.02, 0.1)))
            verts.append(p + Vector((0, 0, z)))
            # Ruido fino a lo largo del anillo y estirado entre anillos: vetas que bajan por el techo
            veta = noise.noise(Vector((p.x * 16, p.y * 16, k * 0.12)))
            if k == anillos // 2 and veta < 0.3:
                cols.append(PAJA[0])   # amarre
            elif k >= anillos - 1:
                cols.append(PAJA[0])   # cumbrera
            else:
                cols.append(PAJA[0] if veta < -0.35 else PAJA[1] if veta < 0.05 else PAJA[2] if veta < 0.4 else PAJA[3])
    caras = [(k * n + j, k * n + (j + 1) % n, (k + 1) * n + (j + 1) % n, (k + 1) * n + j)
             for k in range(anillos) for j in range(n)]
    pa.asignar(pa.malla("Techo", verts, caras, cols), pa.mat_color_vertice("Palma"))


def casa_bajareque(rng, variante):
    """Casa de bajareque con techo de palma sobre basamento bajo (D: casas perecederas en torno a La Venta; la forma
    concreta es I). Variantes: 1 rectangular, 2 absidal (extremos redondeados), 3 pequeña y redonda."""
    a, b, r, cumbrera = {1: (1.0, 0.7, 0.06, 2.45), 2: (1.05, 0.65, 0.65, 2.4), 3: (0.6, 0.6, 0.6, 2.2)}[variante]
    z0, alto, vuelo = 0.2, 1.0, 0.3
    basamento(rng, a, b, r)
    muros_bajareque(rng, a, b, r, z0, alto)
    techo_palma(rng, a + vuelo, b + vuelo, max(r + vuelo, 0.1) if r > 0.1 else 0.1, z0 + alto + 0.04, cumbrera)
    v, f, c = [], [], []
    for _ in range(7):
        ang = rng.uniform(-math.pi, 0.6)
        matas(v, f, c, Vector((math.cos(ang) * (a + 0.5), math.sin(ang) * (b + 0.5), 0)), rng.randint(7, 11), 0.15,
              0.06, rng, grosor=0.022)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


def vasija(nombre, centro, radio, alto, colores, cuello=0.0):
    """Tecomate (vasija esférica sin cuello, típica olmeca) u olla con cuello; boca oscura."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, radius=radio, location=centro + Vector((0, 0, alto * 0.5)))
    o = bpy.context.active_object
    o.name = nombre
    o.scale.z = alto / (2 * radio)
    pa.asignar(o, pa.mat_ruido(nombre, colores, escala=14))
    tope = centro.z + alto
    if cuello:
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radio * 0.5, depth=cuello,
                                            location=centro + Vector((0, 0, alto + cuello / 2 - 0.01)))
        pa.asignar(bpy.context.active_object, pa.mat_ruido(nombre + "Cuello", colores, escala=14))
        tope += cuello
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radio * (0.42 if cuello else 0.45), depth=0.01,
                                        location=(centro.x, centro.y, tope - 0.004))
    pa.asignar(bpy.context.active_object, pa.mat_ruido(nombre + "Boca", ["#1E2420", "#2C302A"], escala=10))


def fogon(rng, variante):
    """Fogón de tres piedras con un tecomate tiznado encima, ceniza, leña y dos vasijas al lado."""
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.24, depth=0.012, location=(0, 0, 0.006))
    pa.asignar(bpy.context.active_object, pa.mat_ruido("Ceniza", ["#7D745B", "#9C8B75", "#C9C0AB", "#4A473B"], escala=16))
    mat_piedra = pa.mat_ruido("PiedraFogon", ["#2C302A", "#36382F", "#4A473B"], escala=12)
    for k in range(3):   # las tres piedras quedan fuera del contorno del tecomate para que se lean
        ang = -math.pi / 2 + 2 * math.pi * k / 3
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.075,
                                              location=(math.cos(ang) * 0.15, math.sin(ang) * 0.15, 0.07))
        g = bpy.context.active_object
        g.scale = (1, 0.85, 1.2)
        pa.asignar(g, mat_piedra)
    vasija("Tecomate", Vector((0, 0, 0.12)), 0.095, 0.16, ["#644B2A", "#7B5B33", "#3A2A1A"])
    vasija("Olla", Vector((0.34, 0.12, 0)), 0.12, 0.24, ["#C6925A", "#E3B47C", "#B6814C"], cuello=0.06)
    vasija("TecomateRojo", Vector((0.25, -0.22, 0)), 0.085, 0.14, ["#B16143", "#9B713F", "#B16143"])
    mat_lena = pa.mat_ruido("Lena", ["#48331F", "#6D502E", "#9C7842"], escala=20)
    for x, y, giro in ((-0.28, -0.1, 20), (-0.26, -0.04, 35), (-0.2, 0.22, -40)):
        o = caja("Lena", (x, y, 0.025), (0.32, 0.04, 0.04), 0)
        o.rotation_euler.z = math.radians(giro)
        pa.asignar(o, mat_lena)


# ---------------------------------------------------------------- escombros de basalto

def escombros(rng, variante):
    """Escombros de basalto: 1 tramos de columna caídos, 2 montón de fragmentos, 3 tocón de columna con su tramo
    caído al lado."""
    mat = pa.mat_piedra_musgo("BasaltoEscombro", ["#2C302A", "#36382F", "#4A473B", "#615C48", "#7D745B", "#9C8B75"],
                              ["#3E5F27", "#5A8732", "#84B345"], escala=8, estirar=(1, 1, 0.5), altura_musgo=0.12,
                              umbral_musgo=0.45)
    mat_frag = pa.mat_ruido("FragmentoEscombro", ["#2C302A", "#36382F", "#4A473B", "#615C48", "#7D745B"], escala=10)

    def tramo(nombre, largo, radio, centro, giro):
        """Tramo de columna tumbado, centrado en centro (el prisma crece desde su base: se compensa)."""
        o = prisma_basalto(nombre, rng, largo, radio)
        o.rotation_euler = Euler((0, math.radians(90), math.radians(giro)))
        eje = Vector((math.cos(math.radians(giro)), math.sin(math.radians(giro)), 0))
        o.location = Vector(centro) - eje * (largo / 2)
        return pa.asignar(o, mat)

    def fragmentos(k, zona, r0, r1):
        for _ in range(k):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=rng.uniform(r0, r1),
                                                  location=(rng.uniform(-zona, zona), rng.uniform(-zona, zona), 0.01))
            g = bpy.context.active_object
            g.scale = (1, rng.uniform(0.6, 1), rng.uniform(0.4, 0.7))
            g.rotation_euler.z = rng.uniform(0, math.pi)
            pa.asignar(g, mat_frag)

    if variante == 1:
        tramo("Tramo1", 0.5, 0.09, (0.0, 0.05, 0.07), 30)
        tramo("Tramo2", 0.38, 0.085, (-0.06, -0.14, 0.065), -55)
        fragmentos(3, 0.25, 0.03, 0.05)
    elif variante == 2:
        fragmentos(7, 0.18, 0.05, 0.09)
        tramo("Tramo", 0.28, 0.08, (0.1, 0.12, 0.06), 70)
    else:
        col = prisma_basalto("Tocon", rng, 0.36, 0.095)
        col.location = (-0.08, 0.08, 0)
        pa.asignar(col, mat)
        tramo("Caido", 0.45, 0.09, (0.12, -0.12, 0.07), 15)
        fragmentos(3, 0.22, 0.03, 0.05)
    v, f, c = [], [], []
    matas(v, f, c, (rng.uniform(-0.15, 0.15), -0.2, 0), 8, 0.12, 0.06, rng, grosor=0.02)
    helecho(v, f, c, (0.06, 0.14, 0), 0.55, rng)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- etapa A2: ceiba, troncos, vasijas, enramada, muelle, canoa

CORTEZA_CEIBA = ["#615C48", "#7D745B", "#9C8B75", "#A28A6D"]
MADERA = ["#48331F", "#6D502E", "#9C7842", "#A28A6D"]


def rama(v, f, c, p0, p1, r0, r1, colores, lados=7, tramos=4, rng=None):
    """Cilindro irregular de p0 a p1 que se adelgaza de r0 a r1 (troncos y ramas)."""
    eje = (p1 - p0)
    largo = eje.length
    d = eje.normalized()
    a = d.cross(ARRIBA_Z if abs(d.z) < 0.95 else Vector((1, 0, 0))).normalized()
    b = d.cross(a)
    k0 = len(v)
    for i in range(tramos + 1):
        t = i / tramos
        centro = p0 + eje * t
        r = r0 + (r1 - r0) * t
        for j in range(lados):
            ang = 2 * math.pi * j / lados
            rr = r * (1 + (0.08 * math.sin(3 * ang + 7 * t) if rng else 0))
            v.append(centro + (a * math.cos(ang) + b * math.sin(ang)) * rr)
            c.append(colores[(i + j) % len(colores)] if rng is None else rng.choice(colores))
    for i in range(tramos):
        for j in range(lados):
            q = k0 + i * lados
            f.append((q + j, q + (j + 1) % lados, q + lados + (j + 1) % lados, q + lados + j))
    f.append(tuple(k0 + tramos * lados + j for j in range(lados)))


def contrafuerte(v, f, c, angulo, alcance, alto, grosor, colores):
    """Raíz tabular de la ceiba: aleta triangular que baja del tronco al suelo."""
    d = Vector((math.cos(angulo), math.sin(angulo), 0))
    p = Vector((-d.y, d.x, 0)) * (grosor / 2)
    base_in, base_out, arriba = d * 0.22, d * alcance, d * 0.24 + ARRIBA_Z * alto
    k = len(v)
    for s in (p, -p):
        v += [base_in + s, base_out + s * 0.4, arriba + s]
    c += [colores[1], colores[0], colores[2]] * 2
    f += [(k, k + 1, k + 2), (k + 3, k + 5, k + 4), (k, k + 3, k + 4, k + 1), (k + 1, k + 4, k + 5, k + 2)]


def ceiba(rng, variante):
    """Ceiba (Ceiba pentandra), el árbol sagrado mesoamericano, nativo de Tabasco (D). Tronco gris con raíces
    tabulares y copa ancha en pisos horizontales. Altura comprimida a la escala del juego (~3,4 m)."""
    v, f, c = [], [], []
    alto_tronco = 2.2
    rama(v, f, c, Vector((0, 0, 0)), Vector((0.05, -0.03, alto_tronco)), 0.27, 0.17, CORTEZA_CEIBA, 10, 8, rng)
    for i in range(6):
        contrafuerte(v, f, c, i * math.pi / 3 + rng.uniform(-0.2, 0.2), rng.uniform(0.6, 0.85), rng.uniform(0.6, 0.9),
                     0.07, CORTEZA_CEIBA)
    copa = Vector((0.05, -0.03, alto_tronco))
    puntas = []
    for i in range(5):   # ramas casi horizontales: la silueta en pisos de la ceiba
        ang = i * 2 * math.pi / 5 + rng.uniform(-0.3, 0.3)
        punta = copa + Vector((math.cos(ang), math.sin(ang), 0)) * rng.uniform(0.8, 1.05) + ARRIBA_Z * rng.uniform(0.35, 0.7)
        rama(v, f, c, copa - ARRIBA_Z * 0.2, punta, 0.1, 0.045, CORTEZA_CEIBA, 6, 3, rng)
        puntas.append(punta)
    pa.asignar(pa.malla("Tronco", v, f, c), pa.mat_color_vertice("Corteza"))

    v, f, c = [], [], []
    mat_nucleo = pa.mat_ruido("NucleoCopa", ["#124524", "#23551C", "#217233", "#387022"], escala=6)
    for centro in puntas + [copa + ARRIBA_Z * 0.9]:
        r = rng.uniform(0.55, 0.72)
        # Núcleo relleno: sin él la copa se ve como hojas sueltas con huecos (y contornos por dentro)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=r * 0.88, location=centro)
        nucleo = bpy.context.active_object
        nucleo.scale.z = 0.42
        pa.asignar(nucleo, mat_nucleo)
        for _ in range(150):
            th = rng.uniform(0, 2 * math.pi)
            ph = math.radians(rng.uniform(0, 100))
            pos = Vector(centro) + Vector((r * math.sin(ph) * math.cos(th), r * math.sin(ph) * math.sin(th), 0.38 * r * math.cos(ph)))
            if pos.z - centro.z > 0.12:
                cols = rng.choice([("#568B26", "#92B535"), ("#387022", "#73AD36"), ("#4A993E", "#73AD36")])
            elif pos.z - centro.z > -0.05:
                cols = rng.choice([("#387022", "#568B26"), ("#217233", "#4A993E")])
            else:
                cols = rng.choice([("#124524", "#217233"), ("#0F2A17", "#136B35")])
            hoja(v, f, c, pos, (pos - Vector(centro)) + ARRIBA_Z * 0.2, rng.uniform(0.2, 0.26), 0.09, *cols)
    pa.asignar(pa.malla("Copa", v, f, c), pa.mat_color_vertice("Follaje"))

    v, f, c = [], [], []
    for _ in range(5):
        ang = rng.uniform(-math.pi, 0.5)
        matas(v, f, c, (math.cos(ang) * 0.75, math.sin(ang) * 0.75, 0), rng.randint(7, 10), 0.14, 0.06, rng, grosor=0.02)
    helecho(v, f, c, (0.55, -0.55, 0), 0.8, rng)
    pa.asignar(pa.malla("Base", v, f, c), pa.mat_color_vertice("Vegetal"))


def hongos(v, f, c, centro, rng, n=3):
    for _ in range(n):
        p = Vector(centro) + Vector((rng.uniform(-0.06, 0.06), rng.uniform(-0.06, 0.06), 0))
        alto = rng.uniform(0.03, 0.05)
        prisma_color(v, f, c, p, 0.008, alto, "#E3B47C", 5)
        prisma_color(v, f, c, p + ARRIBA_Z * alto, rng.uniform(0.025, 0.035), 0.012, rng.choice(["#B16143", "#C6925A"]), 8)


def tronco_caido(rng, variante):
    """1 tronco caído con musgo y hongos; 2 tocón con raíces."""
    v, f, c = [], [], []
    musgosa = MADERA[:3] + ["#5A8732", "#3E5F27"]
    if variante == 1:
        giro = math.radians(25)
        d = Vector((math.cos(giro), math.sin(giro), 0))
        p0, p1 = -d * 0.55 + ARRIBA_Z * 0.12, d * 0.55 + ARRIBA_Z * 0.11
        rama(v, f, c, p0, p1, 0.13, 0.11, musgosa, 9, 6, rng)
        rama(v, f, c, d * 0.15 + ARRIBA_Z * 0.18, d * 0.25 + Vector((-d.y, d.x, 0)) * 0.22 + ARRIBA_Z * 0.3, 0.035, 0.02,
             MADERA, 5, 2, rng)   # muñón de rama
        hongos(v, f, c, d * 0.2 - Vector((-d.y, d.x, 0)) * 0.14, rng)
        hongos(v, f, c, -d * 0.35 - Vector((-d.y, d.x, 0)) * 0.13, rng, 2)
    else:
        rama(v, f, c, Vector((0, 0, 0)), Vector((0, 0, 0.32)), 0.19, 0.17, musgosa, 10, 3, rng)
        prisma_color(v, f, c, Vector((0, 0, 0.32)), 0.165, 0.005, "#C6925A", 10)   # corte
        prisma_color(v, f, c, Vector((0, 0, 0.326)), 0.09, 0.004, "#9C7842", 10)   # anillos
        for i in range(5):
            ang = i * 2 * math.pi / 5 + rng.uniform(-0.3, 0.3)
            dd = Vector((math.cos(ang), math.sin(ang), 0))
            rama(v, f, c, dd * 0.12 + ARRIBA_Z * 0.1, dd * 0.38, 0.06, 0.025, MADERA, 5, 2, rng)
        hongos(v, f, c, (0.12, -0.18, 0), rng)
    pa.asignar(pa.malla("Tronco", v, f, c), pa.mat_color_vertice("Madera"))
    v, f, c = [], [], []
    matas(v, f, c, (0.1, -0.22, 0), 8, 0.12, 0.06, rng, grosor=0.02)
    helecho(v, f, c, (-0.3, -0.1, 0), 0.5, rng)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


def vasijas(rng, variante):
    """1 grupo de olla, tecomate y cántaro; 2 tinaja grande oscura con un cajete (cuenco) al lado."""
    if variante == 1:
        vasija("OllaGrande", Vector((0, 0.06, 0)), 0.16, 0.32, ["#C6925A", "#B6814C", "#E3B47C"], cuello=0.07)
        vasija("Tecomate", Vector((0.22, -0.13, 0)), 0.12, 0.19, ["#B16143", "#9B713F", "#B16143"])
        vasija("Cantaro", Vector((-0.2, -0.12, 0)), 0.085, 0.19, ["#9B713F", "#7B5B33", "#B6814C"], cuello=0.08)
    else:
        vasija("Tinaja", Vector((0, 0.04, 0)), 0.19, 0.38, ["#36382F", "#4A473B", "#2C302A"], cuello=0.05)
        bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.11, depth=0.07, location=(0.24, -0.16, 0.035))
        cajete = bpy.context.active_object
        cajete.scale = (1, 1, 1)
        pa.asignar(cajete, pa.mat_ruido("Cajete", ["#E3B47C", "#C9C0AB", "#C6925A"], escala=12))   # blanco (D: loza blanca)
        bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.085, depth=0.01, location=(0.24, -0.16, 0.07))
        pa.asignar(bpy.context.active_object, pa.mat_ruido("CajeteDentro", ["#7B5B33", "#644B2A"], escala=10))
    v, f, c = [], [], []
    matas(v, f, c, (-0.05, -0.25, 0), 7, 0.1, 0.07, rng, grosor=0.02)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


CAPAS_ENRAMADA = ("Enramada_Poste_01", "Enramada_Techo_01")   # se colocan junto a cada Enramada_01
POSTE_FRENTE = (0.9, -0.6)   # x, y del poste delantero respecto a la enramada (OlmecaCityGenerator usa el mismo)


def enramada(rng, variante, capa="base"):
    """Enramada: techo de palma sobre cuatro postes, sin muros, con un petate, vasijas y un metate (I).
    Para poder pasar por debajo se exporta en tres capas con el mismo lienzo y pivote: la base (postes del fondo,
    enseres y pasto), el poste delantero y el techo. En Unity, el poste se ordena por su propia profundidad y el techo
    se adelanta cuando el jugador está debajo (RoofOverhead). Lo que no es de la capa solo se oculta a la cámara: sigue
    dando sombra."""
    rng = random.Random(zlib.crc32(b"Enramada_01"))   # las mismas pajas y matas en las tres capas
    mat_poste = pa.mat_ruido("Poste", ["#48331F", "#6D502E", "#9C7842"], escala=14, estirar=(1, 1, 0.2))
    for x in (-0.9, 0.9):
        for y in (-0.6, 0.6):
            nombre = "PosteFrente" if (x, y) == POSTE_FRENTE else "Poste"   # el más cercano a la cámara (+x, -y)
            pa.asignar(caja(nombre, (x, y, 0.62), (0.08, 0.08, 1.24), 0), mat_poste)
        pa.asignar(caja("Viga", (x, 0, 1.22), (0.07, 1.35, 0.07), 0), mat_poste)
    techo_palma(rng, 1.25, 0.95, 0.1, 1.24, 2.15)
    # Enseres hacia el lado +y: por el centro queda paso (OlmecaCityGenerator, CompoundObstacles)
    petate = caja("Petate", (0.1, -0.05, 0.006), (1.1, 0.75, 0.012), 0)
    pa.asignar(petate, pa.mat_ruido("Petate", ["#9C7842", "#C6925A", "#E3B47C", "#B6814C"], escala=6, estirar=(14, 1, 1)))
    vasija("OllaEnramada", Vector((-0.5, 0.33, 0.012)), 0.11, 0.22, ["#C6925A", "#B6814C", "#E3B47C"], cuello=0.05)
    vasija("TecomateEnramada", Vector((-0.25, 0.42, 0.012)), 0.08, 0.13, ["#B16143", "#9B713F"])
    metate = caja("Metate", (0.45, 0.38, 0.07), (0.38, 0.22, 0.07), 0.015)   # piedra de moler de basalto
    metate.rotation_euler = Euler((0, math.radians(-8), math.radians(15)))
    mat_basalto = pa.mat_ruido("Metate", ["#4A473B", "#615C48", "#7D745B"], escala=10)
    pa.asignar(metate, mat_basalto)
    mano = caja("Mano", (0.45, 0.38, 0.13), (0.06, 0.24, 0.05), 0.02)
    mano.rotation_euler.z = math.radians(15)
    pa.asignar(mano, mat_basalto)
    v, f, c = [], [], []
    for _ in range(6):
        ang = rng.uniform(-math.pi, 0.6)
        matas(v, f, c, (math.cos(ang) * 1.15, math.sin(ang) * 0.85, 0), rng.randint(7, 10), 0.15, 0.06, rng, grosor=0.022)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))
    propias = {"base": lambda n: n not in ("Techo", "PosteFrente"), "poste": lambda n: n == "PosteFrente",
               "techo": lambda n: n == "Techo"}[capa]
    for o in bpy.context.scene.objects:
        if o.type == 'MESH' and not propias(o.name.split(".")[0]):
            o.visible_camera = False


def con_capas(objetos):
    """Añade tras cada enramada su poste delantero y su techo (en la composición se dibujan en este orden)."""
    return [o2 for o in objetos for o2 in [o] + [(n, *o[1:]) for n in (CAPAS_ENRAMADA if o[0] == "Enramada_01" else ())]]


def muelle(rng, variante):
    """Módulo de 1 m de muelle de madera sobre el agua (I/F, §8): tablones transversales, largueros y pilotes que
    acaban en la superficie del agua (-0,06 m). Variante 1 a lo largo de X, 2 a lo largo de Y."""
    mats = [pa.mat_ruido(f"Tablon{i}", cols, escala=16, estirar=(1, 6, 1)) for i, cols in
            enumerate([["#6D502E", "#9C7842"], ["#9C7842", "#A28A6D"], ["#48331F", "#6D502E"]])]
    for i in range(8):
        x = -0.4375 + i * 0.125
        t = caja("Tablon", (x, rng.uniform(-0.02, 0.02), 0.12 + rng.uniform(-0.006, 0.006)), (0.115, 0.78, 0.025), 0)
        t.rotation_euler.z = math.radians(rng.uniform(-2, 2))
        pa.asignar(t, rng.choice(mats))
    mat_viga = pa.mat_ruido("Larguero", ["#48331F", "#6D502E"], escala=12)
    for y in (-0.3, 0.3):
        pa.asignar(caja("Larguero", (0, y, 0.085), (1.0, 0.07, 0.05), 0), mat_viga)
    for x in (-0.4, 0.4):
        for y in (-0.36, 0.36):
            alto = 0.32 if (x > 0 and y < 0 and variante == 1) or (x > 0 and y > 0 and variante == 2) else 0.17
            pa.asignar(caja("Pilote", (x, y, -0.06 + alto / 2), (0.07, 0.07, alto), 0), mat_viga)
    if variante == 2:
        bpy.context.view_layer.update()
        giro = Matrix.Rotation(math.radians(90), 4, 'Z')
        for o in bpy.context.scene.objects:
            if o.type == 'MESH':
                o.matrix_world = giro @ o.matrix_world


def canoa(rng, variante):
    """Canoa monóxila (de un solo tronco), documentada en Mesoamérica (D). Solo se modela lo que asoma del agua:
    el objeto se coloca a la altura de la superficie (z = -0,06). Para que no se lea como un tronco: proa y popa en
    punta y algo levantadas, borda clara, interior hueco y oscuro, un remo de pala y un tecomate dentro."""
    largo, ancho, borda, grueso = 0.72, 0.2, 0.1, 0.035   # semilargo, semiancho, altura de la borda, pared
    n = 32
    # Sección de babor a estribor: línea de flotación, costado, borda exterior e interior, fondo del hueco
    tonos = ["#48331F", "#6D502E", "#A28A6D", "#A28A6D", "#3A2A1A", "#3A2A1A", "#6D502E", "#A28A6D", "#9C7842", "#6D502E"]
    verts, caras, cols = [], [], []
    for i in range(n + 1):
        s = -1 + 2 * i / n
        x = s * largo
        w = ancho * max(0.0, 1 - abs(s) ** 2.5) ** 0.6
        wi = max(w - grueso, 0.0)
        zr = borda + 0.07 * abs(s) ** 3   # la borda sube hacia las puntas
        fondo = zr - 0.08
        for k, (y, z) in enumerate([(-w * 0.9, 0), (-w, zr * 0.6), (-w, zr), (-wi, zr), (-wi * 0.6, fondo),
                                    (wi * 0.6, fondo), (wi, zr), (w, zr), (w, zr * 0.6), (w * 0.9, 0)]):
            verts.append(Vector((x, y, z)))
            cols.append(tonos[k] if rng.random() > 0.15 else tonos[max(k - 1, 0)])   # algo de veta
    m = len(tonos)
    caras = [(i * m + k, i * m + k + 1, (i + 1) * m + k + 1, (i + 1) * m + k) for i in range(n) for k in range(m - 1)]
    casco = pa.malla("Casco", verts, caras, cols)
    pa.asignar(casco, pa.mat_color_vertice("Casco"))
    giro = math.radians(12)
    casco.rotation_euler.z = giro
    rot = lambda x, y, z: Vector((x * math.cos(giro) - y * math.sin(giro), x * math.sin(giro) + y * math.cos(giro), z))
    # Remo atravesado sobre la borda: la pala asoma por el lado de la cámara
    mat_remo = pa.mat_ruido("Remo", ["#9C7842", "#A28A6D"], escala=12)
    ang = giro - math.radians(25)
    centro = rot(0.12, 0.05, borda + 0.02)
    hacia_pala = Vector((math.sin(ang), -math.cos(ang), 0))   # mitad del remo con la pala, hacia -y
    for nombre, pos, medidas in (("Remo", centro, (0.025, 0.5, 0.02)),
                                 ("Pala", centro + hacia_pala * 0.31 + Vector((0, 0, -0.01)), (0.09, 0.17, 0.012))):
        p = caja(nombre, pos, medidas, 0.004)
        p.rotation_euler.z = ang
        pa.asignar(p, mat_remo)
    vasija("TecomateCanoa", rot(-0.32, 0.0, borda - 0.08), 0.06, 0.1, ["#B16143", "#9B713F"])


def planificar_aldea():
    """Escena de prueba de la etapa A2: estanque con muelle y canoa, ceiba, enramada, vasijas y troncos."""
    rng = random.Random(31)
    estanque = lambda X, Y: ((X - 3) ** 2) / 9 + ((Y + 3.5) ** 2) / 4.5 < 1
    celdas = sorted(((X, Y) for X in range(-14, 15) for Y in range(-14, 15) if visible(X, Y)), key=lambda p: p[0] - p[1])
    tipo = {c: "agua" if estanque(*c) else "pasto" for c in celdas}
    suelo, mascaras = [], set()
    for X, Y in celdas:
        if tipo[(X, Y)] == "agua":
            suelo.append(("AGUA", X, Y))
            m = 0
            for bit, (dx, dy) in DIR4.items():
                if tipo.get((X + dx, Y + dy)) == "pasto":
                    m |= bit
            for bit, (sx, sy, _, _) in DIAG.items():
                if tipo.get((X + sx, Y + sy)) == "pasto":
                    m |= bit
            m = normalizar_mascara(m)
            if m:
                suelo.append((f"Tile_Orilla_{m:02d}", X, Y))
                mascaras.add(m)
        else:
            suelo.append((f"Tile_Pasto_Selva_{rng.choice([1, 1, 2, 3]):02d}", X, Y))
    objetos = [("Muelle_Madera_02", 3, y, 0.0) for y in (-1.6, -2.6, -3.6)]
    objetos += [("Canoa_01", 4.4, -3.1, -0.06), ("Nenufar_01", 1.6, -4.0, -0.06), ("Nenufar_01", 4.6, -4.7, -0.06),
                ("Tule_01", 6.1, -3.0, 0.0), ("Tule_01", 5.6, -4.9, 0.0), ("Tule_01", 0.3, -2.4, 0.0), ("Tule_01", 0.5, -4.4, 0.0),
                ("Ceiba_01", 0.2, -7.0, 0.0), ("Enramada_01", -3.2, -1.2, 0.0), ("Vasijas_01", -1.4, -2.4, 0.0),
                ("Vasijas_02", -4.6, -3.0, 0.0), ("Tronco_Caido_01", -5.6, -1.0, 0.0), ("Tronco_Caido_02", 6.6, 1.2, 0.0),
                ("Casa_Bajareque_01", 1.8, 4.2, 0.0), ("Fogon_01", 2.6, 2.0, 0.0),
                ("Palma_Corozo_01", -7.4, 4.2, 0.0), ("Palma_Corozo_02", 7.6, -1.2, 0.0), ("Palma_Corozo_01", -1.5, 6.5, 0.0),
                ("Matorral_Selva_01", -5.6, -5.2, 0.0), ("Matorral_Selva_02", 5.8, 4.4, 0.0), ("Matorral_Selva_03", -2.8, 3.6, 0.0),
                ("Helecho_01", -1.8, -5.2, 0.0), ("Helecho_02", 2.4, -6.6, 0.0), ("Pasto_Alto_01", -5.4, 0.2, 0.0),
                ("Arbusto_Selva_02", 4.6, 2.8, 0.0)]
    return {"suelo": suelo, "objetos": con_capas(objetos), "dosel": []}, mascaras


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


# ---------------------------------------------------------------- selva (M1, MP_OLM_03)

def mat_emision(nombre, color, fuerza=1.0):
    """Material con luz propia (corrupción, fragmentos de memoria): base negra, así que con fuerza 1 sale exactamente
    del color dado, sin que el sol lo aclare hasta el blanco."""
    m = bpy.data.materials.new(nombre)
    _, bsdf = pa._bsdf(m)
    bsdf.inputs['Base Color'].default_value = (0, 0, 0, 1)
    bsdf.inputs['Emission Color'].default_value = pa.hex_lineal(color)
    bsdf.inputs['Emission Strength'].default_value = fuerza
    return m


def grieta_olvido(rng, variante):
    """Mancha de tierra apagada con grietas que brillan en un violeta ajeno a la selva: rastro de la Sombra del Olvido
    (Docs/Enemigos/Olmeca.md, señales de corrupción; F). Va horneada en el suelo, como las sombras del dosel."""
    radio = 0.5 if variante == 1 else 0.68
    n = 30
    v = [Vector((0, 0, 0.004))]
    for i in range(n):
        a = 2 * math.pi * i / n
        lobulos = noise.noise(Vector((math.cos(a) * 1.4, math.sin(a) * 1.4, variante * 3.1)))
        dientes = noise.noise(Vector((math.cos(a) * 4.0, math.sin(a) * 4.0, variante * 7.3)))
        r = radio * max(0.35, 0.75 + 0.4 * lobulos + 0.12 * dientes)
        v.append(Vector((math.cos(a) * r, math.sin(a) * r, 0.004)))
    f = [(0, 1 + i, 1 + (i + 1) % n) for i in range(n)]
    pa.asignar(pa.malla("Mancha", v, f), pa.mat_ruido("TierraApagada", ["#2A1838", "#3A2A1A", "#644B2A", "#3A2A1A"], escala=12))

    v, f = [], []
    for k in range(rng.randint(4, 6)):
        p = Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 0.009))
        ang = 2 * math.pi * k / 5 + rng.uniform(-0.4, 0.4)
        ancho = 0.05
        for _ in range(rng.randint(5, 8)):
            ang += rng.uniform(-0.6, 0.6)
            d = Vector((math.cos(ang), math.sin(ang), 0))
            q = p + d * rng.uniform(0.07, 0.11)
            if q.length > radio * 0.85:
                break
            lado = Vector((-d.y, d.x, 0)) * ancho / 2
            i0 = len(v)
            v += [p + lado, p - lado, q - lado * 0.8, q + lado * 0.8]
            f.append((i0, i0 + 1, i0 + 2, i0 + 3))
            p, ancho = q, ancho * 0.85
    pa.asignar(pa.malla("Grietas", v, f), mat_emision("Olvido", "#A98BE8"))


def fragmento_memoria(rng, variante):
    """Fragmento de memoria menor (MP_OLM_03): una lasca de piedra verde que brilla sobre una piedra, entre el pasto.
    Lo deja cada espíritu corrompido al ser vencido (F); la piedra verde pulida remite al jade y la serpentina que se
    trabajaron en La Venta (D)."""
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.14, location=(0.03, 0.03, 0.04))
    base = bpy.context.active_object
    base.scale = (1.2, 1.0, 0.5)
    pa.asignar(base, pa.mat_ruido("PiedraFragmento", ["#2C302A", "#36382F", "#4A473B", "#615C48"], escala=14))
    v, f, c = [], [], []
    prisma_color(v, f, c, Vector((0, 0, 0)), 0.06, 0.28, "#4FB39A", 5)
    lasca = pa.malla("Lasca", v, f, c)
    lasca.location = (0.02, 0.02, 0.02)
    lasca.rotation_euler = Euler((math.radians(10), math.radians(-8), math.radians(20)))
    pa.asignar(lasca, mat_emision("Jade", "#4FB39A"))
    v, f, c = [], [], []
    matas(v, f, c, (0.0, -0.1, 0), 7, 0.1, 0.1, rng, grosor=0.014)
    pa.asignar(pa.malla("Pasto", v, f, c), pa.mat_color_vertice("Vegetal"))


def entrada_estructura(rng, variante):
    """Entrada a la antigua estructura (MP_OLM_03; la mazmorra «La ciudad bajo la selva»): montículo de tierra cubierto
    por la selva con una abertura enmarcada por columnas de basalto. Se inspira en la Tumba A de La Venta, un recinto de
    columnas naturales de basalto dentro de un montículo (D); que dé paso a una mazmorra es ficción (F). La abertura
    mira a +X, hacia el sendero y la cámara."""
    ax, ay, alto = 1.4, 1.9, 1.45
    anillos, lados = 8, 32
    v = []
    for i in range(anillos + 1):
        t = i / anillos
        r, z = math.cos(t * math.pi / 2), math.sin(t * math.pi / 2) * alto
        for j in range(lados):
            a = 2 * math.pi * j / lados
            g = 1 + 0.07 * noise.noise(Vector((math.cos(a) * 1.6, math.sin(a) * 1.6, t * 2.5)))
            v.append(Vector((math.cos(a) * ax * r * g, math.sin(a) * ay * r * g, z)))
    f = [(i * lados + j, i * lados + (j + 1) % lados, (i + 1) * lados + (j + 1) % lados, (i + 1) * lados + j)
         for i in range(anillos) for j in range(lados)]
    pa.asignar(pa.malla("Monticulo", v, f),
               pa.mat_ruido("TierraMonticulo", ["#3E5F27", "#644B2A", "#5A8732", "#7B5B33", "#387022"], escala=5))

    # La abertura: un hueco oscuro que asoma del montículo, entre dos pares de columnas con dos dinteles
    bpy.ops.mesh.primitive_cube_add(size=1, location=(1.18, 0, 0.5))
    hueco = bpy.context.active_object
    hueco.scale = (0.5, 0.78, 1.0)
    negro = bpy.data.materials.new("Hueco")
    _, bsdf = pa._bsdf(negro)
    bsdf.inputs['Base Color'].default_value = (0, 0, 0, 1)
    pa.asignar(hueco, negro)

    mat = pa.mat_piedra_musgo("BasaltoColumna", ["#2C302A", "#36382F", "#4A473B", "#615C48", "#7D745B", "#9C8B75"],
                              ["#3E5F27", "#5A8732", "#84B345"], escala=8, estirar=(1, 1, 0.5), altura_musgo=0.2,
                              umbral_musgo=0.42)
    for x in (1.42, 1.62):
        for y in (-0.5, 0.5):
            col = prisma_basalto("Columna", rng, rng.uniform(1.05, 1.15), 0.1)
            col.location = (x, y + rng.uniform(-0.03, 0.03), 0)
            pa.asignar(col, mat)
        dintel = prisma_basalto("Dintel", rng, 1.3, 0.09)
        dintel.rotation_euler = Euler((math.radians(90), 0, 0))   # crece hacia -Y desde y = 0.65
        dintel.location = (x, 0.65, 1.17)
        pa.asignar(dintel, mat)
    giro = math.radians(-20)   # una columna caída delante, junto al sendero
    caida = prisma_basalto("Caida", rng, 0.9, 0.09)
    caida.rotation_euler = Euler((0, math.radians(90), giro))
    caida.location = Vector((2.0, -1.1, 0.08)) - Vector((math.cos(giro), math.sin(giro), 0)) * 0.45
    pa.asignar(caida, mat)

    # La selva cubre el montículo (salvo la abertura) y su base
    v, f, c = [], [], []
    for _ in range(80):
        a, t = rng.uniform(0, 2 * math.pi), math.sqrt(rng.uniform(0, 1)) * 0.97
        x, y = math.cos(a) * ax * t, math.sin(a) * ay * t
        if x > 0.55 and abs(y) < 0.75:
            continue
        matas(v, f, c, (x, y, alto * math.sqrt(max(0.0, 1 - t * t)) - 0.03), rng.randint(5, 9), 0.16, 0.1, rng, grosor=0.02)
    for _ in range(10):
        a = rng.uniform(0, 2 * math.pi)
        x, y = math.cos(a) * ax * 0.98, math.sin(a) * ay * 0.98
        if x > 0.5 and abs(y) < 1.0:
            continue
        helecho(v, f, c, (x, y, 0), rng.uniform(0.6, 0.85), rng)
    pa.asignar(pa.malla("Vegetacion", v, f, c), pa.mat_color_vertice("Vegetal"))


# ---------------------------------------------------------------- mazmorra (M2): «La ciudad bajo la selva»

BASALTO_LOSA = ["#2C302A", "#36382F", "#4A473B", "#615C48", "#7D745B"]
BASALTO_COLUMNA = ["#2C302A", "#36382F", "#4A473B", "#615C48", "#7D745B", "#9C8B75"]
ALTO_PISO = 0.4   # grueso del piso que asoma sobre el vacío en los bordes delanteros


def holdout(obj):
    """El objeto tapa lo que hay detrás pero sale transparente (p. ej. el piso alrededor de un hueco)."""
    obj.is_holdout = True
    return obj


def tile_losa(rng, variante):
    """Tile del piso de la antigua estructura: cuatro losas de basalto asentadas en arcilla oscura, con juntas. Que el
    interior sea un piso de losas es ficción (F); en La Venta los pisos eran de arcillas de colores y hay mosaicos de
    serpentina enterrados (D). Variantes: 1 y 2 losas (2 con una partida y cascajo) y 3 húmeda con musgo en las juntas
    (El Pantano)."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, -ALTO_PISO / 2))
    base = bpy.context.active_object
    base.scale = (0.999, 0.999, ALTO_PISO)
    pa.asignar(base, pa.mat_ruido("ArcillaPiso", ["#1E2420", "#2C302A", "#3A2A1A", "#644B2A"], escala=5, estirar=(1, 1, 7)))

    humeda = variante == 3
    colores = ["#2C302A", "#36382F", "#36382F", "#4A473B", "#615C48"] if humeda else BASALTO_LOSA   # más oscura
    mat = pa.mat_ruido("Losa", colores, escala=6)
    mat.node_tree.nodes['Mapping'].inputs['Location'].default_value = (rng.uniform(0, 100), 0, 0)
    junta = 0.04
    lado = 0.5 - junta
    partida = rng.randrange(4) if variante == 2 else -1
    for k, (i, j) in enumerate(((0, 0), (0, 1), (1, 0), (1, 1))):
        cx, cy = -0.25 + 0.5 * i, -0.25 + 0.5 * j
        piezas = [((cx, cy), (lado, lado))]
        if k == partida:   # losa partida en dos
            piezas = [((cx - 0.12, cy), (lado / 2 - 0.02, lado)), ((cx + 0.13, cy), (lado / 2 - 0.02, lado))]
        for (px, py), (sx, sy) in piezas:
            alto = rng.uniform(0.03, 0.045)
            losa = caja("Losa", (px, py, alto / 2 - 0.02), (sx, sy, alto), 0.012)
            losa.rotation_euler = Euler((rng.uniform(-0.02, 0.02), rng.uniform(-0.02, 0.02), rng.uniform(-0.03, 0.03)))
            pa.asignar(losa, mat)

    if variante == 2:
        guijarros(rng, 5, 0.02, 0.035, ["#36382F", "#4A473B", "#615C48"], 0.4)
    if humeda:
        v, f, c = [], [], []
        for _ in range(rng.randint(3, 5)):   # musgo en las juntas, no sobre las losas
            if rng.random() < 0.5:
                centro = (rng.choice((-0.5, 0.0, 0.5)) * 0.98, rng.uniform(-0.45, 0.45), 0)
            else:
                centro = (rng.uniform(-0.45, 0.45), rng.choice((-0.5, 0.0, 0.5)) * 0.98, 0)
            matas(v, f, c, centro, rng.randint(5, 8), 0.06, 0.05, rng, base="#1D2B1A",
                  puntas=("#3E5F27", "#5A8732", "#387022"), grosor=0.016)
        pa.asignar(pa.malla("Musgo", v, f, c), pa.mat_color_vertice("Vegetal"))


def muro_basalto(rng, variante):
    """Módulo de 1 m de muro interior: columnas de basalto apretadas sobre una banqueta de arcilla. Recuerda el
    recinto de columnas de La Venta (D), aquí cerrado y bajo tierra (F). Sin plantas: es el interior de la estructura.
    Variantes: 1 y 2 completas, 3 con luz del olvido que se filtra entre las columnas."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.06))
    muro = bpy.context.active_object
    muro.name = "Muro"
    muro.scale = (1.0, 0.3, 0.12)
    pa.asignar(muro, pa.mat_ruido("Banqueta", ["#2C302A", "#3A2A1A", "#644B2A", "#7B5B33"], escala=9))

    mat = pa.mat_piedra_musgo("BasaltoMuro", BASALTO_COLUMNA, ["#3E5F27", "#5A8732"], escala=8, estirar=(1, 1, 0.5),
                              altura_musgo=0.15, umbral_musgo=0.5)
    for i in range(5):
        col = prisma_basalto(f"Columna{i}", rng, rng.uniform(1.35, 1.6), rng.uniform(0.1, 0.115))
        col.location = (-0.4 + 0.2 * i + rng.uniform(-0.015, 0.015), rng.uniform(-0.03, 0.03), 0.1)
        col.rotation_euler = Euler((rng.uniform(-0.02, 0.02), rng.uniform(-0.02, 0.02), 0))
        pa.asignar(col, mat)
    if variante == 3:   # una rendija entre la 2.ª y la 3.ª columna, en la cara que ve la cámara (-Y)
        v, f = [], []
        x, z = -0.1, 0.2
        while z < 1.2:
            dz = rng.uniform(0.12, 0.2)
            dx = rng.uniform(-0.03, 0.03)
            i0 = len(v)
            v += [Vector((x - 0.02, -0.14, z)), Vector((x + 0.02, -0.14, z)),
                  Vector((x + dx + 0.015, -0.14, z + dz)), Vector((x + dx - 0.015, -0.14, z + dz))]
            f.append((i0, i0 + 1, i0 + 2, i0 + 3))
            x, z = x + dx, z + dz
        pa.asignar(pa.malla("Rendija", v, f), mat_emision("Olvido", "#A98BE8"))


def muro_basalto_y(rng, variante):
    """El muro girado 90°: corre a lo largo de Y (lado -X de una sala) y muestra su cara +X a la cámara."""
    muro_basalto(rng, variante)
    bpy.context.view_layer.update()
    giro = Matrix.Rotation(math.radians(90), 4, 'Z')
    for o in bpy.context.scene.objects:
        if o.type == 'MESH':
            o.matrix_world = giro @ o.matrix_world


def portico(rng, ancho, alto, mat, x=0.0):
    """Dos columnas de basalto y un dintel que cruza a lo largo de Y, con un hueco negro detrás. Mira a +X."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x - 0.2, 0, alto / 2))
    hueco = bpy.context.active_object
    hueco.scale = (0.3, ancho, alto)
    negro = bpy.data.materials.new("Hueco")
    _, bsdf = pa._bsdf(negro)
    bsdf.inputs['Base Color'].default_value = (0, 0, 0, 1)
    pa.asignar(hueco, negro)
    for y in (-ancho / 2 - 0.1, ancho / 2 + 0.1):
        col = prisma_basalto("Jamba", rng, alto + 0.05, 0.12)
        col.location = (x, y, 0)
        pa.asignar(col, mat)
    dintel = prisma_basalto("Dintel", rng, ancho + 0.55, 0.11)
    dintel.rotation_euler = Euler((math.radians(90), 0, 0))   # crece hacia -Y
    dintel.location = (x, ancho / 2 + 0.27, alto + 0.08)
    pa.asignar(dintel, mat)


def santuario_memoria(rng, variante):
    """Santuario de Memoria (punto de control): un nicho de basalto con una hacha votiva de jade en pie sobre una losa.
    Las hachas de jade y serpentina como ofrenda están documentadas en La Venta (D); que guarden la memoria del lugar
    es ficción (F). Variantes: 1 dormido (jade apagado) y 2 despierto (el jade brilla). Mira a +X."""
    mat = pa.mat_piedra_musgo("BasaltoNicho", BASALTO_COLUMNA, ["#3E5F27", "#5A8732"], escala=8, estirar=(1, 1, 0.5),
                              altura_musgo=0.12, umbral_musgo=0.5)
    base = caja("Banqueta", (0.05, 0, 0.06), (0.9, 1.3, 0.12), 0.02)
    pa.asignar(base, pa.mat_ruido("ArcillaNicho", ["#3A2A1A", "#644B2A", "#7B5B33", "#B16143"], escala=8))
    respaldo = caja("Respaldo", (-0.35, 0, 0.75), (0.2, 1.2, 1.4), 0.02)
    pa.asignar(respaldo, mat)
    portico(rng, 0.7, 1.15, mat, x=-0.12)

    losa = caja("Altar", (0.18, 0, 0.2), (0.34, 0.5, 0.16), 0.02)
    pa.asignar(losa, pa.mat_ruido("LosaAltar", BASALTO_LOSA, escala=10))
    v, f, c = [], [], []
    prisma_color(v, f, c, Vector((0, 0, 0)), 0.05, 0.3, "#4FB39A", 4)
    hacha = pa.malla("Hacha", v, f, c)
    hacha.scale = (0.6, 1.3, 1.0)   # plana y ancha, como una hacha votiva
    hacha.location = (0.18, 0, 0.28)
    if variante == 2:
        pa.asignar(hacha, mat_emision("Jade", "#4FB39A", 1.2))
        bpy.ops.mesh.primitive_circle_add(vertices=24, radius=0.24, fill_type='NGON', location=(0.18, 0, 0.285))
        pa.asignar(bpy.context.active_object, mat_emision("Halo", "#2E7D6B", 0.8))
    else:
        pa.asignar(hacha, pa.mat_ruido("JadeApagado", ["#1F5148", "#2E7D6B", "#36382F"], escala=14))


def santuario_vida(rng, variante):
    """Santuario de Vida (curación limitada): un fogón de tres piedras sobre una banqueta de arcilla, con una olla de
    agua y leña. El fogón de tres piedras es doméstico y documentado en el Golfo (D); que su fuego cure es ficción (F).
    Variantes: 1 encendido y 2 apagado (ya se usó en esta incursión)."""
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.42, depth=0.1, location=(0, 0, 0.05))
    pa.asignar(bpy.context.active_object, pa.mat_ruido("ArcillaFogon", ["#3A2A1A", "#644B2A", "#7B5B33", "#B16143"], escala=8))
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.2, depth=0.012, location=(0, 0, 0.106))
    pa.asignar(bpy.context.active_object, pa.mat_ruido("Ceniza", ["#4A473B", "#7D745B", "#9C8B75", "#C9C0AB"], escala=16))
    mat_piedra = pa.mat_ruido("PiedraFogon", ["#2C302A", "#36382F", "#4A473B"], escala=12)
    for k in range(3):
        ang = -math.pi / 2 + 2 * math.pi * k / 3
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.08,
                                              location=(math.cos(ang) * 0.17, math.sin(ang) * 0.17, 0.17))
        g = bpy.context.active_object
        g.scale = (1, 0.85, 1.2)
        pa.asignar(g, mat_piedra)
    mat_lena = pa.mat_ruido("Lena", ["#48331F", "#6D502E", "#9C7842"], escala=20)
    for giro in (20, 80, 140):
        o = caja("Lena", (0, 0, 0.13), (0.3, 0.04, 0.04), 0)
        o.rotation_euler.z = math.radians(giro)
        pa.asignar(o, mat_lena)
    vasija("Olla", Vector((0.3, 0.26, 0.1)), 0.11, 0.22, ["#C6925A", "#E3B47C", "#B6814C"], cuello=0.05)

    if variante == 1:
        for (dx, dy, alto, radio, color, fuerza) in ((0, 0, 0.36, 0.09, "#D8642A", 1.0),
                                                       (0.04, -0.03, 0.26, 0.06, "#F2A541", 1.1),
                                                       (-0.04, 0.03, 0.22, 0.05, "#F2A541", 1.1),
                                                       (0.0, 0.0, 0.16, 0.04, "#FFE08A", 1.2)):
            bpy.ops.mesh.primitive_cone_add(vertices=7, radius1=radio, radius2=0, depth=alto,
                                            location=(dx, dy, 0.13 + alto / 2))
            llama = bpy.context.active_object
            llama.rotation_euler = Euler((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), 0))
            pa.asignar(llama, mat_emision("Llama", color, fuerza))
    else:
        for _ in range(4):   # brasas casi apagadas
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.025,
                                                  location=(rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 0.13))
            pa.asignar(bpy.context.active_object, mat_emision("Brasa", "#9A3A1C", 0.7))


def escalera_subida(rng, variante):
    """Escalera que sube hacia la salida (al piso de arriba o a la selva): tres peldaños hacia -X hasta un pórtico de
    basalto con el hueco negro. Va contra el muro del lado -X de una sala y mira a +X."""
    mat = pa.mat_piedra_musgo("BasaltoPortico", BASALTO_COLUMNA, ["#3E5F27", "#5A8732", "#84B345"], escala=8,
                              estirar=(1, 1, 0.5), altura_musgo=0.2, umbral_musgo=0.45)
    respaldo = caja("Respaldo", (-0.75, 0, 0.9), (0.3, 1.9, 1.8), 0.02)
    pa.asignar(respaldo, mat)
    mat_losa = pa.mat_ruido("Peldano", BASALTO_LOSA, escala=9)
    for k in range(3):
        peldano = caja("Peldano", (0.25 - 0.25 * k, 0, 0.075 * (k + 1) / 2), (0.25, 1.0, 0.075 * (k + 1)), 0.01)
        pa.asignar(peldano, mat_losa)
    portico(rng, 0.9, 1.35, mat, x=-0.45)


def escalera_bajada(rng, variante):
    """Escalera que baja al piso siguiente: un hueco de 1 × 1,4 m en el piso con un marco de losas y peldaños de basalto
    que bajan hacia la cámara (+X) y se pierden en la oscuridad bajo el borde cercano: con la cámara a 30° solo se ve
    el fondo de un pozo si baja hacia ella. Va horneada en el suelo. El piso que rodea el hueco solo tapa (sale
    transparente) para que no se vea el pozo por fuera."""
    sx, sy = 0.5, 0.7
    for (cx, cy, ax, ay) in ((0, sy + 1.5, 4, 3), (0, -sy - 1.5, 4, 3), (sx + 1.5, 0, 3, 2 * sy), (-sx - 1.5, 0, 3, 2 * sy)):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, cy, -0.01))
        tapa = bpy.context.active_object
        tapa.scale = (ax, ay, 0.02)
        holdout(tapa)

    mat_marco = pa.mat_ruido("Marco", BASALTO_LOSA, escala=9)
    for (cx, cy, ax, ay) in ((0, sy + 0.08, 2 * sx + 0.32, 0.16), (0, -sy - 0.08, 2 * sx + 0.32, 0.16),
                             (sx + 0.08, 0, 0.16, 2 * sy), (-sx - 0.08, 0, 0.16, 2 * sy)):
        pa.asignar(caja("Marco", (cx, cy, 0.015), (ax, ay, 0.05), 0.01), mat_marco)

    # Paredes del pozo y peldaños, más oscuros cuanto más hondo
    pared = pa.mat_ruido("ParedPozo", ["#1E2420", "#2C302A", "#36382F"], escala=8)
    for (cx, cy, ax, ay) in ((0, sy + 0.05, 2 * sx, 0.1), (0, -sy - 0.05, 2 * sx, 0.1), (-sx - 0.05, 0, 0.1, 2 * sy)):
        pa.asignar(caja("Pared", (cx, cy, -0.6), (ax, ay, 1.2), 0), pared)
    tonos = (["#615C48", "#7D745B", "#9C8B75"], ["#4A473B", "#615C48", "#7D745B"], ["#36382F", "#4A473B", "#615C48"],
             ["#2C302A", "#36382F", "#4A473B"])
    for k in range(4):
        z = -0.13 * (k + 1)
        x = -sx + 0.25 * k + 0.125
        pa.asignar(caja("Peldano", (x, 0, z - 0.6), (0.25, 2 * sy, 1.2), 0.008), pa.mat_ruido(f"Peldano{k}", tonos[k], escala=9))
    fondo = bpy.data.materials.new("Fondo")
    _, bsdf = pa._bsdf(fondo)
    bsdf.inputs['Base Color'].default_value = (0, 0, 0, 1)
    pa.asignar(caja("Fondo", (sx + 0.3, 0, -1.0), (0.6, 2 * sy, 1.0), 0), fondo)   # la escalera sigue bajo el borde


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
    "Altar_Trono": (altar_trono, (136, 120), (68, 36), ("basalto", "musgo", "verde_calido", "verde_frio"), "#1E2420",
                    ((160, 88), (70, 40)), 0.5),
    "Estela_Lisa": (estela, (32, 64), (16, 12), ("basalto", "musgo", "verde_calido", "verde_frio"), "#1E2420",
                    ((80, 56), (26, 24)), 0.5),
    "Estela_Grande": (estela_grande, (64, 88), (32, 22), ("basalto", "musgo", "tierra", "verde_calido", "verde_frio"),
                      "#1E2420", ((112, 72), (36, 30)), 0.5),
    "Rampa_Arcilla": (rampa_arcilla, (136, 112), (68, 30), ("tierra", "acentos", "basalto", "verde_calido", "verde_frio"),
                      None, None, 0.5),
    "Casa_Bajareque": (casa_bajareque, (160, 152), (80, 40), ("tierra", "madera", "verde_calido", "verde_frio", "basalto"),
                       "#3A2A1A", ((208, 112), (76, 52)), 0.5),
    "Fogon": (fogon, (48, 48), (22, 14), ("tierra", "basalto", "madera", "acentos"), "#1E2420", SOMBRA_CHICA, 0.5),
    "Escombros_Basalto": (escombros, (48, 48), (24, 14), ("basalto", "musgo", "verde_calido", "verde_frio"), "#1E2420",
                          SOMBRA_CHICA, 0.5),
    "Cerca_Basalto_Y": (cerca_basalto_y, (64, 96), (32, 14),
                        ("basalto", "musgo", "tierra", "verde_calido", "verde_frio"), "#1E2420", ((128, 64), (40, 32)), 0.5),
    "Ceiba": (ceiba, (192, 208), (96, 28), ("verde_calido", "verde_frio", "madera", "basalto", "tierra"), "#0F2A17",
              ((256, 128), (48, 56)), 0.45),
    "Tronco_Caido": (tronco_caido, (64, 48), (32, 16), ("madera", "musgo", "verde_calido", "verde_frio", "tierra", "acentos"),
                     "#1D2B1A", ((96, 48), (30, 24)), 0.5),
    "Vasijas": (vasijas, (48, 48), (24, 12), ("tierra", "basalto", "acentos", "verde_calido"), "#3A2A1A", SOMBRA_CHICA, 0.5),
    "Enramada": (enramada, (160, 152), (80, 40), ("tierra", "madera", "verde_calido", "verde_frio", "basalto", "acentos"),
                 "#3A2A1A", ((208, 112), (76, 52)), 0.5),
    "Enramada_Poste": (lambda rng, v: enramada(rng, v, "poste"), (160, 152), (80, 40), ("tierra", "madera", "verde_calido", "verde_frio", "basalto", "acentos"),
                       "#3A2A1A", None, 0.5),
    "Enramada_Techo": (lambda rng, v: enramada(rng, v, "techo"), (160, 152), (80, 40), ("tierra", "madera", "verde_calido", "verde_frio", "basalto", "acentos"),
                       "#3A2A1A", None, 0.5),
    "Muelle_Madera": (muelle, (64, 64), (32, 22), ("madera", "tierra"), "#3A2A1A", None, 0.5),
    "Canoa": (canoa, (96, 64), (48, 26), ("madera", "tierra", "acentos"), "#3A2A1A", None, 0.5),
    "Plataforma_Arcilla": (plataforma_arcilla, (64, 96), (32, 18), ("tierra", "acentos", "basalto", "verde_calido", "verde_frio"),
                           None, None, 0.5),
    "Grieta_Olvido": (grieta_olvido, (96, 64), (48, 32), ("olvido", "tierra"), None, None, 0.5),
    "Fragmento_Memoria": (fragmento_memoria, (32, 40), (16, 10), ("basalto", "jade", "verde_calido"), "#1E2420",
                          SOMBRA_CHICA, 0.5),
    "Entrada_Estructura": (entrada_estructura, (192, 176), (96, 64),
                           ("tierra", "basalto", "musgo", "verde_calido", "verde_frio"), "#1E2420", ((256, 160), (100, 56)), 0.5),
    # Mazmorra (M2)
    "Tile_Losa_Basalto": (tile_losa, (64, 64), (32, 40), ("basalto", "tierra", "musgo", "verde_frio", "olvido"), None, None, 0.5),
    "Muro_Basalto": (muro_basalto, (64, 112), (32, 18), ("basalto", "tierra", "musgo", "olvido"), "#1E2420",
                     ((192, 112), (64, 48)), 0.5),
    "Muro_Basalto_Y": (muro_basalto_y, (64, 112), (32, 18), ("basalto", "tierra", "musgo", "olvido"), "#1E2420",
                       ((192, 112), (64, 48)), 0.5),
    "Santuario_Memoria": (santuario_memoria, (96, 128), (48, 30), ("basalto", "tierra", "acentos", "jade", "musgo"),
                          "#1E2420", ((160, 96), (56, 44)), 0.5),
    "Santuario_Vida": (santuario_vida, (64, 72), (32, 20), ("tierra", "basalto", "fuego", "madera"), "#1E2420",
                       SOMBRA_CHICA, 0.5),
    "Escalera_Subida": (escalera_subida, (128, 160), (64, 40), ("basalto", "tierra", "musgo"), "#1E2420",
                        ((192, 112), (70, 48)), 0.5),
    "Escalera_Bajada": (escalera_bajada, (96, 80), (48, 40), ("basalto",), None, None, 0.5),
}
VARIANTES = {"Tile_Camino_Tierra": 4, "Tile_Pasto_Selva": 3, "Tile_Agua_Anim": 4, "Cerca_Basalto": 3,
             "Palma_Corozo": 2, "Arbusto_Selva": 2, "Matorral_Selva": 3, "Pasto_Alto": 2, "Helecho": 2,
             "Roca_Basalto": 3, "Nenufar": 1, "Tule": 1, "Sombra_Dosel": 2, "Altar_Trono": 1,
             "Estela_Lisa": 1, "Estela_Grande": 3, "Rampa_Arcilla": 2, "Casa_Bajareque": 3, "Fogon": 1,
             "Escombros_Basalto": 3, "Cerca_Basalto_Y": 3, "Ceiba": 1, "Tronco_Caido": 2, "Vasijas": 2, "Enramada": 1,
             "Enramada_Poste": 1, "Enramada_Techo": 1, "Muelle_Madera": 2, "Canoa": 1,
             "Grieta_Olvido": 2, "Fragmento_Memoria": 1, "Entrada_Estructura": 1,
             "Tile_Losa_Basalto": 3, "Muro_Basalto": 3, "Muro_Basalto_Y": 3, "Santuario_Memoria": 2, "Santuario_Vida": 2,
             "Escalera_Subida": 1, "Escalera_Bajada": 1}
# Las transiciones y los módulos de plataforma se generan solo para las máscaras que usan las composiciones
SIN_AVISO_BORDE = {"Tile_Camino_Tierra", "Tile_Pasto_Selva", "Tile_Agua_Anim", "Tile_Camino_Trans", "Tile_Orilla",
                   "Plataforma_Arcilla", "Tile_Losa_Basalto"}
ELEVACION = {"Plataforma_Arcilla": H_PLATAFORMA}   # altura de la cara superior de los módulos que se empalman


def es_modular(tipo):
    return tipo.startswith("Tile_") or tipo in ELEVACION


def borde_trasero(a, silueta, mascara):
    """Línea oscura de 1 px sobre los bordes traseros con talud (lados -X a la izquierda, +Y a la derecha): esos taludes
    no se ven desde la cámara y sin la línea la cima se confunde con el pasto de detrás. Solo en lados sin vecino,
    para que no aparezcan costuras entre módulos. silueta = render sin los mechones, para que la línea siga el labio;
    los mechones quedan por delante de ella."""
    op = silueta[..., 3] > 0.5
    mitad = a.shape[1] // 2
    for x in range(a.shape[1]):
        if not mascara & (2 if x < mitad else 4):
            continue
        ys = pa.np.flatnonzero(op[:, x])
        if len(ys) and ys[0] > 0 and a[ys[0] - 1, x, 3] < 0.5:
            a[ys[0] - 1, x, :3] = pa.hex_srgb(BORDE_TRASERO)
            a[ys[0] - 1, x, 3] = 1.0
    return a


def producir(salida, tmp, tipo, variante):
    constructor, lienzo, ancla, rampas, color_contorno, cfg_sombra, umbral = TIPOS[tipo]
    nombre = f"{tipo}_{variante:02d}"
    pa.nueva_escena()
    constructor(random.Random(zlib.crc32(nombre.encode())), variante)
    if es_modular(tipo):   # solapamiento de ~1 px: sin él quedan huecos en escalera entre tiles vecinos
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
    if es_modular(tipo):
        pa.unificar_borde_trasero(px, lienzo[1] - ancla[1] - round(pa.PPU * pa.ARRIBA.z * ELEVACION.get(tipo, 0.0)))
    if tipo == "Plataforma_Arcilla":
        pasto = bpy.context.scene.objects.get("Pasto")
        if pasto:
            pasto.visible_camera = False
        silueta = pa.reducir(pa.renderizar(os.path.join(tmp, nombre + "_silueta_4x.png")), umbral=umbral)
        if pasto:
            pasto.visible_camera = True
        borde_trasero(px, silueta, variante)
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


def pantalla(X, Y, Z=0.0, centro=(W_COMP / 2, H_COMP / 2)):
    """Píxel del lienzo de composición donde cae el punto del mundo; centro = píxel del origen."""
    p = Vector((X, Y, Z))
    return round(centro[0] + pa.PPU * p.dot(pa.DERECHA)), round(centro[1] - pa.PPU * p.dot(pa.ARRIBA))


def visible(X, Y, margen=80):
    sx, sy = pantalla(X, Y)
    return -margen < sx < W_COMP + margen and -margen < sy < H_COMP + 2 * margen


def planificar():
    """Diseño de la escena de prueba: tipos de celda, transiciones necesarias y objetos.
    Devuelve el plan y las máscaras de transición que hay que generar."""
    rng = random.Random(5)
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


def planificar_ruinas():
    """Escena de prueba de las ruinas: plataforma de dos niveles sobre pasto (con una esquina cóncava en el nivel
    inferior) y altar-trono al pie."""
    rng = random.Random(11)
    inferior = {(X, Y) for X in range(-3, 3) for Y in range(-2, 3)} - {(2, -2), (2, -1)}
    superior = {(X, Y) for X in range(-2, 1) for Y in range(-1, 2)}
    celdas = sorted(((X, Y) for X in range(-14, 15) for Y in range(-14, 15) if visible(X, Y)), key=lambda p: p[0] - p[1])
    suelo = [(f"Tile_Pasto_Selva_{rng.choice([1, 1, 2, 3]):02d}", X, Y) for X, Y in celdas]
    objetos, mascaras = [], set()
    for nivel, z in ((inferior, 0.0), (superior, H_PLATAFORMA)):
        for X, Y in nivel:
            m = mascara_plataforma(nivel, X, Y)
            mascaras.add(m)
            objetos.append((f"Plataforma_Arcilla_{m:02d}", X, Y, z))
    objetos += [("Palma_Corozo_01", -4.6, 1.2, 0.0), ("Palma_Corozo_02", -1.5, 4.3, 0.0), ("Palma_Corozo_01", 4.4, 3.6, 0.0),
                ("Matorral_Selva_02", 3.4, 0.6, 0.0), ("Matorral_Selva_01", -2.6, -3.4, 0.0), ("Matorral_Selva_03", 3.0, -3.0, 0.0),
                ("Pasto_Alto_01", 3.1, -1.4, 0.0), ("Pasto_Alto_02", -1.4, -2.8, 0.0), ("Helecho_01", 2.3, -2.3, 0.0),
                ("Roca_Basalto_03", 2.7, -5.2, 0.0), ("Roca_Basalto_01", 4.6, -1.2, 0.0),
                ("Altar_Trono_01", 0.4, -4.6, 0.0),
                ("Estela_Grande_01", -1.0, -3.6, 0.0), ("Rampa_Arcilla_01", -2.0, -3.5, 0.0), ("Estela_Lisa_01", -3.3, -4.0, 0.0), ("Estela_Grande_02", 2.9, -4.0, 0.0),
                ("Estela_Grande_03", 4.1, -3.2, 0.0), ("Estela_Lisa_01", -4.3, -3.3, 0.0),
                ("Casa_Bajareque_01", 6.4, 0.6, 0.0), ("Casa_Bajareque_02", 2.5, 5.0, 0.0), ("Fogon_01", 5.0, -0.6, 0.0),
                ("Escombros_Basalto_01", -1.6, -6.0, 0.0), ("Escombros_Basalto_02", 3.8, -5.8, 0.0)]
    return {"suelo": suelo, "objetos": objetos, "dosel": []}, mascaras


def planificar_ciudad():
    """Ciudad olmeca de unos 50×46 m inspirada en La Venta (§8). El norte apunta hacia la cámara (-Y), así que el Gran
    Montículo (Complejo C, al sur) queda al fondo y el recinto de columnas (Complejo A, al norte) en primer plano.
    Eje de la ciudad: X = 0. Aldea de casas de bajareque al este, junto al río; selva en el oeste y en los bordes.
    Devuelve el plan de la composición, las máscaras que hay que generar y los datos de colisión para Unity."""
    rng = random.Random(23)
    X0, X1, Y0, Y1 = -24, 26, -20, 26

    # ---- relieve: Gran Montículo de tres niveles con rampas (cada rampa necesita 3 celdas planas delante)
    niveles = [
        {(X, Y) for X in range(-6, 7) for Y in range(10, 23)},
        {(X, Y) for X in range(-4, 5) for Y in range(13, 21)},
        {(X, Y) for X in range(-2, 2) for Y in range(15, 19)},
    ]
    rampas = [("Rampa_Arcilla_01", 0, 8.5, 0.0), ("Rampa_Arcilla_01", 0, 11.5, 1.0), ("Rampa_Arcilla_02", 2.5, 16.5, 2.0)]
    # Plataforma baja dentro del recinto (Complejo A), sin rampa: se rodea
    niveles_a = [{(X, Y) for X in range(-2, 3) for Y in range(-12, -9)}]

    # ---- suelo
    rio = lambda X, Y: X > 21.5 + 1.8 * math.sin(Y * 0.23)
    pantano = lambda X, Y: ((X - 15.5) ** 2) / 10 + ((Y + 15) ** 2) / 5 < 1
    plaza = lambda X, Y: -6 <= X <= 6 and 0 <= Y <= 7
    calzada = lambda X, Y: X in (-1, 0) and -17 <= Y <= 7
    camino_aldea = lambda X, Y: (Y in (1, 2) and 7 <= X <= 20) or (X in (14, 15) and -12 <= Y <= 13)
    bajo_plataforma = niveles[0] | niveles_a[0]
    tipo_celda = {}
    for X in range(X0, X1 + 1):
        for Y in range(Y0, Y1 + 1):
            if rio(X, Y) or pantano(X, Y):
                t = "agua"
            elif plaza(X, Y) or calzada(X, Y) or camino_aldea(X, Y):
                t = "camino"
            else:
                t = "pasto"
            tipo_celda[(X, Y)] = t

    def mascara(X, Y, es_otro):
        m = 0
        for bit, (dx, dy) in DIR4.items():
            if es_otro(tipo_celda.get((X + dx, Y + dy))):
                m |= bit
        for bit, (sx, sy, _, _) in DIAG.items():
            if es_otro(tipo_celda.get((X + sx, Y + sy))):
                m |= bit
        return normalizar_mascara(m)

    mascaras = {"Tile_Camino_Trans": set(), "Tile_Orilla": set(), "Plataforma_Arcilla": set()}
    suelo = []
    for X, Y in sorted(tipo_celda, key=lambda p: p[0] - p[1]):
        if (X, Y) in bajo_plataforma:
            continue   # los módulos de plataforma cubren la celda entera
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

    objetos = []
    for grupo in (niveles, niveles_a):
        for z, nivel in enumerate(grupo):
            for X, Y in nivel:
                m = mascara_plataforma(nivel, X, Y)
                mascaras["Plataforma_Arcilla"].add(m)
                objetos.append((f"Plataforma_Arcilla_{m:02d}", X, Y, float(z)))
    objetos += rampas

    # ---- ocupación: celdas que no admiten objetos sueltos
    ocupadas = set(bajo_plataforma)
    for _, X, Y, _ in rampas:
        for dx in (-1.5, -0.5, 0.5, 1.5):
            for dy in (-1.5, -0.5, 0.5, 1.5):
                ocupadas.add((round(X + dx * 0.7), round(Y + dy * 0.7)))
    puntos = []   # (X, Y, radio libre) de objetos ya colocados

    def libre(X, Y, r):
        celda = (round(X), round(Y))
        if celda not in tipo_celda or celda in ocupadas:
            return False
        return all((X - a) ** 2 + (Y - b) ** 2 >= (r + rb) ** 2 for a, b, rb in puntos)

    def poner(nombre, X, Y, r, z=0.0):
        objetos.append((nombre, X, Y, z))
        puntos.append((X, Y, r))

    # ---- Complejo A: recinto de columnas de basalto con entrada hacia el montículo
    cercas = []
    for i in range(10):
        x = -4.95 + 1.1 * i
        cercas.append((f"Cerca_Basalto_{[1, 2, 1, 3, 2][i % 5]:02d}", x, -15.0))
        if abs(x) > 1.3:
            cercas.append((f"Cerca_Basalto_{[2, 1, 2, 1, 3][i % 5]:02d}", x, -3.6))
    for i in range(10):
        y = -14.45 + 1.1 * i
        for x in (-5.6, 5.6):
            cercas.append((f"Cerca_Basalto_Y_{[1, 2, 1, 2, 3][(i + int(x > 0)) % 5]:02d}", x, y))
    for n, X, Y in cercas:
        if abs(X) < 1.3 and Y < -14:   # paso de la calzada por el lado norte
            continue
        poner(n, X, Y, 0.55)

    # ---- monumentos (centro ceremonial)
    for nombre, X, Y in [
        ("Altar_Trono_01", -5.2, 7.8), ("Altar_Trono_01", 5.2, 7.8),                       # al pie del montículo
        ("Estela_Grande_01", -2.4, 8.9), ("Estela_Grande_01", 2.4, 8.9),
        ("Estela_Lisa_01", -1.6, 3.0), ("Estela_Lisa_01", 1.6, 3.0), ("Estela_Lisa_01", -1.6, -1.0), ("Estela_Lisa_01", 1.6, -1.0),
        ("Estela_Grande_02", -3.6, -6.2), ("Estela_Grande_03", 3.4, -6.6), ("Estela_Grande_01", -3.2, -13.2),
        ("Estela_Lisa_01", 3.6, -13.0),
        ("Escombros_Basalto_01", 4.2, -8.6), ("Escombros_Basalto_02", -4.3, -9.4), ("Escombros_Basalto_03", 2.9, -4.8),
        ("Escombros_Basalto_02", -6.6, 0.8), ("Escombros_Basalto_01", 7.8, 5.6), ("Escombros_Basalto_03", -7.9, -5.5),
    ]:
        poner(nombre, X, Y, 1.2 if nombre.startswith("Altar") else 0.5)

    # ---- aldea: casas de bajareque, cada una con su fogón delante
    junto_camino = lambda X, Y, d: any(tipo_celda.get((round(X + dx), round(Y + dy))) == "camino"
                                      for dx in (-d, 0, d) for dy in (-d, 0, d))
    casas = 0
    for _ in range(900):
        if casas == 14:
            break
        X, Y = rng.uniform(8.5, 20.5), rng.uniform(-17, 14)
        if (tipo_celda.get((round(X), round(Y))) != "pasto" or junto_camino(X, Y, 1.6) or not libre(X, Y, 2.0)
                or any(tipo_celda.get((round(X + dx), round(Y + dy))) == "agua" for dx in (-2, 0, 2) for dy in (-2, 0, 2))):
            continue
        poner(f"Casa_Bajareque_{rng.choice([1, 1, 2, 2, 3]):02d}", X, Y, 2.0)
        casas += 1
        if rng.random() < 0.7 and libre(X + 0.9, Y - 2.5, 0.3):   # delante de la puerta, fuera del radio de la casa
            poner("Fogon_01", X + 0.9, Y - 2.5, 0.4)

    # ---- vegetación
    def zona(X, Y):
        borde = min(X - X0, X1 - X, Y - Y0, Y1 - Y)
        if borde < 4 or (X < -9 and Y < 9) or (Y > 23):
            return "selva"
        if X > 8:
            return "aldea"
        return "abierto"

    def sembrar(nombres, cantidad, permitido, r, z=0.0, intentos=None, azar=None):
        azar = azar or rng
        for _ in range(intentos or cantidad * 30):
            if cantidad == 0:
                return
            X, Y = azar.uniform(X0, X1), azar.uniform(Y0, Y1)
            celda = (round(X), round(Y))
            if celda in tipo_celda and permitido(celda, X, Y) and libre(X, Y, r):
                poner(azar.choice(nombres), X, Y, r, z)
                cantidad -= 1

    es = lambda tipo, *zonas: (lambda celda, X, Y: tipo_celda[celda] == tipo and (not zonas or zona(X, Y) in zonas))
    junto_a = lambda tipo: (lambda celda, X, Y: tipo_celda[celda] == "pasto" and any(
        tipo_celda.get((celda[0] + dx, celda[1] + dy)) == tipo for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))))
    matorrales = ["Matorral_Selva_01", "Matorral_Selva_02", "Matorral_Selva_03"]
    palmas = ["Palma_Corozo_01", "Palma_Corozo_02"]
    sembrar(palmas, 100, es("pasto", "selva"), 1.0)
    sembrar(palmas, 14, es("pasto", "aldea"), 1.3)
    sembrar(palmas, 6, es("pasto", "abierto"), 1.6)
    sembrar(matorrales, 420, es("pasto", "selva"), 0.5)
    sembrar(["Tule_01"], 26, junto_a("agua"), 0.3, intentos=6000)
    sembrar(["Nenufar_01"], 14, lambda c_, X, Y: tipo_celda[c_] == "agua" and not junto_a("pasto")(c_, X, Y), 0.5, z=-0.06)
    sembrar(matorrales, 24, junto_a("agua"), 0.6)
    sembrar(["Arbusto_Selva_01", "Arbusto_Selva_01", "Arbusto_Selva_02"], 130, es("pasto", "selva", "aldea"), 0.55)
    sembrar(["Helecho_01", "Helecho_02"], 260, es("pasto", "selva"), 0.4)
    sembrar(["Helecho_01", "Helecho_02"], 25, es("pasto", "abierto", "aldea"), 0.5)
    sembrar(["Pasto_Alto_01", "Pasto_Alto_02"], 60, junto_a("camino"), 0.4)
    sembrar(["Pasto_Alto_01", "Pasto_Alto_02"], 160, es("pasto"), 0.35)
    sembrar(["Roca_Basalto_01", "Roca_Basalto_02", "Roca_Basalto_03"], 26, es("pasto"), 0.7)
    # Plantas sobre los niveles del montículo (la selva lo va cubriendo)
    for z, nivel in enumerate(niveles):
        arriba = [c for c in nivel if mascara_plataforma(nivel, *c) == 0 and
                  (z + 1 >= len(niveles) or c not in niveles[z + 1])]
        for X, Y in rng.sample(arriba, min(len(arriba), 6 - 2 * z)):
            objetos.append((rng.choice(["Pasto_Alto_01", "Helecho_01", "Arbusto_Selva_01"]), X + rng.uniform(-0.3, 0.3),
                            Y + rng.uniform(-0.3, 0.3), z + H_PLATAFORMA))

    dosel_ = [(f"Sombra_Dosel_{1 + i % 2:02d}", rng.uniform(X0, X1), rng.uniform(Y0, Y1)) for i in range(160)]
    dosel_ = [d for d in dosel_ if zona(d[1], d[2]) == "selva" and tipo_celda.get((round(d[1]), round(d[2]))) == "pasto"][:80]

    # ---- recursos de la etapa A2, con su propio generador: lo anterior no cambia de sitio, salvo la vegetación baja
    # que despejan los objetos grandes (la selva ya ocupa todo el espacio libre)
    azar = random.Random(41)
    VEGETACION = ("Matorral_Selva", "Helecho", "Pasto_Alto", "Arbusto_Selva", "Tule", "Roca_Basalto", "Palma_Corozo")

    def despejar_y_poner(nombre, X, Y, r, z=0.0, forzar=False):
        """Pone un objeto grande si no choca con nada que no sea vegetación; quita la vegetación a menos de r.
        forzar: no comprueba los objetos fijos (piezas de un mismo conjunto, como los módulos del muelle)."""
        celda = (round(X), round(Y))
        if celda not in tipo_celda or (celda in ocupadas and not forzar):
            return False
        fijos = [(a, b, rb) for a, b, rb in puntos
                 if not any(o[0].startswith(VEGETACION) and o[1] == a and o[2] == b for o in objetos)]
        if not forzar and any((X - a) ** 2 + (Y - b) ** 2 < (r + rb) ** 2 for a, b, rb in fijos):
            return False
        quitar = [o for o in objetos if o[0].startswith(VEGETACION) and (o[1] - X) ** 2 + (o[2] - Y) ** 2 < r * r]
        for o in quitar:
            objetos.remove(o)
            puntos[:] = [q for q in puntos if not (q[0] == o[1] and q[1] == o[2])]
        poner(nombre, X, Y, r, z)
        return True

    def sembrar_grandes(nombre, cantidad, permitido, r, intentos=4000):
        for _ in range(intentos):
            if cantidad == 0:
                return
            X, Y = azar.uniform(X0, X1), azar.uniform(Y0, Y1)
            celda = (round(X), round(Y))
            if celda in tipo_celda and permitido(celda, X, Y) and despejar_y_poner(nombre, X, Y, r):
                cantidad -= 1
    # Muelle de 4 módulos desde la orilla de la aldea hacia el río, con dos canoas amarradas. Se busca la fila libre
    # más cercana a y = -6; bajo el muelle el agua no bloquea (se camina por su tablero).
    muelles, celdas_muelle = [], set()
    for Y in sorted(range(-14, 12), key=lambda y: abs(y + 6)):
        orilla = min((X for X in range(8, X1 + 1) if tipo_celda.get((X, Y)) == "agua"), default=None)
        if orilla is None or not all(tipo_celda.get((orilla + i, Y)) == "agua" for i in range(4)):
            continue
        if not despejar_y_poner("Muelle_Madera_01", orilla - 0.4, Y, 1.0):
            continue
        for i in range(1, 4):
            despejar_y_poner("Muelle_Madera_01", orilla - 0.4 + i, Y, 0.6, forzar=True)
        for dx, dy in ((1.3, 0.95), (2.5, -0.95)):
            if tipo_celda.get((round(orilla + dx), round(Y + dy))) == "agua":
                despejar_y_poner("Canoa_01", orilla + dx, Y + dy, 0.6, -0.06, forzar=True)
        muelles.append({"x0": orilla - 0.9, "x1": orilla + 3.1, "y": Y, "ancho": 0.8, "z": 0.14})
        celdas_muelle = {(orilla + i, Y) for i in range(3)}
        break

    despejar_y_poner("Ceiba_01", -8.6, 5.0, 1.6)   # ceiba junto a la plaza
    sembrar_grandes("Ceiba_01", 5, es("pasto", "selva"), 1.6)
    sembrar_grandes("Enramada_01", 3, lambda c_, X, Y: es("pasto", "aldea")(c_, X, Y) and not junto_camino(X, Y, 1.2), 1.5)
    sembrar(["Vasijas_01", "Vasijas_02"], 7, es("pasto", "aldea"), 0.45, azar=azar, intentos=3000)
    sembrar_grandes("Tronco_Caido_01", 10, es("pasto", "selva"), 0.7)
    sembrar_grandes("Tronco_Caido_02", 6, es("pasto", "selva", "abierto"), 0.5)

    # ---- sendero hacia la selva (MP_OLM_03): sale de la plaza por el oeste hasta el borde del mapa. Va al final y con
    # su propio azar para que nada de lo anterior cambie de sitio: solo se rehacen los tiles de camino que toca y se
    # quitan los objetos que caen encima (salvo las ceibas, que lo flanquean).
    sendero_y = lambda X: 3 + round(1.2 * math.sin((X + 7) * 0.3))
    sendero = {(X, Y) for X in range(X0, -6) for Y in (sendero_y(X), sendero_y(X) + 1)}
    for celda in sendero:
        tipo_celda[celda] = "camino"
    azar_sendero = random.Random(97)
    nuevas = {}
    for celda in sorted({(X + dx, Y + dy) for X, Y in sendero for dx in (-1, 0, 1) for dy in (-1, 0, 1)}):
        if tipo_celda.get(celda) != "camino" or celda in bajo_plataforma:
            continue
        m = mascara(*celda, lambda o: o == "pasto")
        if m:
            nuevas[celda] = f"Tile_Camino_Trans_{m:02d}"
            mascaras["Tile_Camino_Trans"].add(m)
        else:
            nuevas[celda] = f"Tile_Camino_Tierra_{azar_sendero.choice([1, 1, 2, 4]):02d}"
    suelo[:] = [(nuevas.get((X, Y), n), X, Y) for n, X, Y in suelo]
    en_sendero = lambda X, Y: any(abs(X - a) < 0.85 and abs(Y - b) < 0.85 for a, b in sendero)
    objetos[:] = [o for o in objetos if o[3] > 0.01 or o[0].startswith("Ceiba") or not en_sendero(o[1], o[2])]
    punto = lambda X, Y: {"x": round(X, 2), "y": round(Y, 2)}

    colision = {
        "puntos": {   # para Unity: el portal a la selva y dónde aparece el jugador al volver de ella
            "salida_selva": punto(X0 + 0.3, sendero_y(X0) + 0.5),
            "llegada_selva": punto(X0 + 3.0, sendero_y(X0 + 3) + 0.5),
        },
        "niveles": [[list(c) for c in sorted(n)] for n in niveles],
        "niveles_recinto": [[list(c) for c in sorted(n)] for n in niveles_a],
        "rampas": [{"sprite": n, "x": X, "y": Y, "z": z} for n, X, Y, z in rampas],
        "agua": [list(c) for c, t in sorted(tipo_celda.items()) if t == "agua"],
        "agua_bloqueo": [list(c) for c, t in sorted(tipo_celda.items()) if t == "agua" and c not in celdas_muelle],
        "muelles": muelles,
        "limites": [X0, X1, Y0, Y1],
    }
    return {"suelo": suelo, "objetos": con_capas(objetos), "dosel": dosel_}, mascaras, colision


def planificar_selva():
    """Selva al oeste de la ciudad (MP_OLM_03, «Rastros en la selva»), de 36×20 m. Se entra por el este (+X) desde el
    sendero de la ciudad; el sendero cruza tres claros, donde acechan los espíritus corrompidos, y acaba al oeste en la
    entrada a la antigua estructura. Las grietas del olvido se espesan hacia la entrada. Mismo formato que
    planificar_ciudad; colision["puntos"] lleva los puntos de juego (x, y del flujo) para el generador de Unity."""
    rng = random.Random(61)
    X0, X1, Y0, Y1 = -18, 18, -10, 10
    centro = lambda X: 2.6 * math.sin(X * 0.2) - 0.5           # eje del sendero
    entrada = (-14.5, -2.3)
    explanada = (-10.6, -2.3)                                     # tierra pisada delante de la entrada
    claros = [(11.0, centro(11.0)), (2.0, centro(2.0)), (-6.5, centro(-6.5))]
    dist2 = lambda X, Y, p: (X - p[0]) ** 2 + (Y - p[1]) ** 2

    tipo_celda = {}
    for X in range(X0, X1 + 1):
        for Y in range(Y0, Y1 + 1):
            camino = (X >= -12 and abs(Y - centro(X)) < 1.05) or dist2(X, Y, explanada) < 2.4 ** 2
            tipo_celda[(X, Y)] = "camino" if camino else "pasto"

    def zona(X, Y):
        if (any(dist2(X, Y, c) < 3.2 ** 2 for c in claros) or dist2(X, Y, explanada) < 3.0 ** 2
                or dist2(X, Y, entrada) < 3.0 ** 2):
            return "claro"
        if X > -13 and abs(Y - centro(X)) < 2.2:
            return "orilla"   # borde del sendero: solo vegetación baja
        return "selva"

    def mascara(X, Y, es_otro):
        m = 0
        for bit, (dx, dy) in DIR4.items():
            if es_otro(tipo_celda.get((X + dx, Y + dy))):
                m |= bit
        for bit, (sx, sy, _, _) in DIAG.items():
            if es_otro(tipo_celda.get((X + sx, Y + sy))):
                m |= bit
        return normalizar_mascara(m)

    mascaras = {"Tile_Camino_Trans": set(), "Tile_Orilla": set(), "Plataforma_Arcilla": set()}
    suelo = []
    for X, Y in sorted(tipo_celda, key=lambda p: p[0] - p[1]):
        if tipo_celda[(X, Y)] == "camino":
            m = mascara(X, Y, lambda o: o == "pasto")
            if m:
                suelo.append((f"Tile_Camino_Trans_{m:02d}", X, Y))
                mascaras["Tile_Camino_Trans"].add(m)
            else:
                suelo.append((f"Tile_Camino_Tierra_{rng.choice([1, 1, 2, 4]):02d}", X, Y))
        else:
            suelo.append((f"Tile_Pasto_Selva_{rng.choice([1, 1, 2, 3]):02d}", X, Y))

    objetos, puntos = [], []

    def libre(X, Y, r):
        return (round(X), round(Y)) in tipo_celda and all((X - a) ** 2 + (Y - b) ** 2 >= (r + rb) ** 2 for a, b, rb in puntos)

    def poner(nombre, X, Y, r, z=0.0):
        objetos.append((nombre, X, Y, z))
        puntos.append((X, Y, r))

    def sembrar(nombres, cantidad, permitido, r):
        for _ in range(cantidad * 30):
            if cantidad == 0:
                return
            X, Y = rng.uniform(X0, X1), rng.uniform(Y0, Y1)
            celda = (round(X), round(Y))
            if celda in tipo_celda and permitido(celda, X, Y) and libre(X, Y, r):
                poner(rng.choice(nombres), X, Y, r)
                cantidad -= 1

    # ---- la entrada, ruinas alrededor y dos ceibas que flanquean el sendero ("más allá de las ceibas", MP_OLM_02)
    poner("Entrada_Estructura_01", *entrada, 2.4)
    for nombre, X, Y in [("Escombros_Basalto_01", -12.2, -4.6), ("Escombros_Basalto_03", -11.4, 1.0),
                         ("Escombros_Basalto_02", -8.4, -5.2), ("Estela_Grande_03", -9.0, 0.2),
                         ("Estela_Lisa_01", -12.6, -5.6), ("Ceiba_01", -2.5, 1.6), ("Ceiba_01", -2.9, -5.2)]:
        poner(nombre, X, Y, 1.6 if nombre.startswith("Ceiba") else 0.6)

    en = lambda *zonas: (lambda celda, X, Y: tipo_celda[celda] == "pasto" and zona(X, Y) in zonas)
    sembrar(["Ceiba_01"], 3, en("selva"), 1.6)
    sembrar(["Tronco_Caido_01"], 6, en("selva"), 0.7)
    sembrar(["Tronco_Caido_02"], 4, en("selva"), 0.5)
    sembrar(["Palma_Corozo_01", "Palma_Corozo_02"], 50, en("selva"), 1.0)
    sembrar(["Roca_Basalto_01", "Roca_Basalto_02", "Roca_Basalto_03"], 18, en("selva"), 0.7)
    # Sotobosque: lo variado primero, porque el matorral llena todo el hueco que quede
    sembrar(["Arbusto_Selva_01", "Arbusto_Selva_01", "Arbusto_Selva_02"], 60, en("selva", "orilla"), 0.55)
    sembrar(["Helecho_01", "Helecho_02"], 80, en("selva"), 0.4)
    matorrales = ["Matorral_Selva_01", "Matorral_Selva_02", "Matorral_Selva_03"]
    sembrar(matorrales, 380, en("selva"), 0.5)
    sembrar(matorrales, 30, en("orilla"), 0.6)
    sembrar(["Helecho_01", "Helecho_02"], 40, en("orilla", "claro"), 0.5)
    sembrar(["Pasto_Alto_01", "Pasto_Alto_02"], 90, en("orilla", "claro"), 0.38)

    dosel_ = [(f"Sombra_Dosel_{1 + i % 2:02d}", rng.uniform(X0, X1), rng.uniform(Y0, Y1)) for i in range(240)]
    dosel_ = [d for d in dosel_ if zona(d[1], d[2]) == "selva"][:110]

    # ---- grietas del olvido (horneadas en el suelo): más cuanto más cerca de la entrada
    marcas = []
    for (cx, cy), n in zip(claros + [explanada], (2, 3, 3, 4)):
        for _ in range(300):
            if n == 0:
                break
            X, Y = cx + rng.uniform(-2.6, 2.6), cy + rng.uniform(-2.6, 2.6)
            if ((round(X), round(Y)) in tipo_celda and dist2(X, Y, entrada) > 2.6 ** 2
                    and all(dist2(X, Y, m[1:]) > 1.6 ** 2 for m in marcas)):
                marcas.append((f"Grieta_Olvido_{rng.choice([1, 2]):02d}", X, Y))
                n -= 1

    punto = lambda X, Y: {"x": round(X, 2), "y": round(Y, 2)}
    colision = {
        "niveles": [], "niveles_recinto": [], "rampas": [], "agua": [], "agua_bloqueo": [], "muelles": [],
        "limites": [X0, X1, Y0, Y1],
        "puntos": {
            "llegada": punto(X1 - 2.2, centro(X1 - 2.2)),        # al venir de la ciudad
            "salida": punto(X1 - 0.2, centro(X1)),                # portal de vuelta a la ciudad
            "zona_selva": punto(X1 - 5.0, centro(X1 - 5.0)),      # aviso llegar:selva
            "claros": [punto(*c) for c in claros],               # espíritus corrompidos
            "fragmentos": [punto(c[0] + 0.9, c[1] + 0.7) for c in claros],
            "entrada": punto(*explanada),                        # aviso llegar:entrada_estructura
            "guerrero": punto(-11.6, -0.4),
            "mazmorra": punto(entrada[0] + 1.65, entrada[1]),     # portal en la abertura, al piso 1 (M2)
            "desde_mazmorra": punto(-11.4, -2.9),                # al subir del piso 1
        },
    }
    return {"suelo": suelo, "objetos": con_capas(objetos), "dosel": dosel_, "marcas": marcas}, mascaras, colision


# ---------------------------------------------------------------- pisos de la mazmorra (M2)

def rect(x0, x1, y0, y1):
    return {(X, Y) for X in range(x0, x1 + 1) for Y in range(y0, y1 + 1)}


# Por piso: límites, celdas con piso, escalera de subida (centro y celdas de Y donde reemplaza al muro), punto de
# llegada al bajar, escalera de bajada y su llegada al subir, santuarios, enemigos, adornos, variantes de losa y del
# muro (pesos) y la losa sellada del piso 3. Coordenadas del flujo: la cámara mira desde +X, -Y.
PISOS = {
    1: {"limites": (-16, 16, -10, 10),
        "celdas": rect(-14, -9, -3, 3) | rect(-8, -5, -1, 1) | rect(-4, 4, -5, 5) | rect(5, 8, -2, 0) | rect(9, 14, -4, 2),
        "subida": (-13.6, 0.0), "llegada_arriba": (-12.2, 0.0),
        "bajada": (12.0, -1.0), "llegada_abajo": (10.4, -1.0),
        "memoria": (-13.95, -2.3), "memoria_llegada": (-12.7, -2.3),
        "enemigos": [(-1.0, 2.0), (2.0, -2.5)],
        "adornos": [("Escombros_Basalto_01", -3.0, 4.0), ("Escombros_Basalto_03", 3.2, 4.2),
                    ("Escombros_Basalto_02", 13.5, 1.5), ("Estela_Lisa_01", -3.2, -4.3)],
        "losas": [1, 1, 1, 2, 2], "muros": [1, 2, 1, 2, 1, 2, 3]},
    2: {"limites": (-16, 16, -10, 10),
        "celdas": rect(-14, -9, -2, 4) | rect(-8, -5, 0, 2) | rect(-4, 5, -4, 5) | rect(-1, 0, -6, -5)
                  | rect(-3, 2, -9, -7) | rect(6, 9, 0, 2) | rect(10, 14, -3, 3),
        "subida": (-13.6, 1.0), "llegada_arriba": (-12.2, 1.0),
        "bajada": (12.5, 0.0), "llegada_abajo": (10.9, 0.0),
        "vida": (1.2, -8.2),
        "enemigos": [(-1.5, 3.0), (3.0, 1.0), (0.5, -2.5)],
        "adornos": [("Escombros_Basalto_02", 4.5, 4.5), ("Escombros_Basalto_01", -3.5, -3.5),
                    ("Estela_Lisa_01", 13.8, 2.6), ("Vasijas_01", -2.6, -7.4)],
        "losas": [3, 3, 3, 1, 2], "muros": [1, 2, 1, 2, 3]},
    3: {"limites": (-12, 12, -9, 9),
        "celdas": rect(-10, -6, -2, 2) | rect(-5, -2, -1, 1) | rect(-1, 8, -4, 4),
        "subida": (-9.6, 0.0), "llegada_arriba": (-8.2, 0.0),
        "memoria": (-0.95, 2.6), "memoria_llegada": (0.3, 2.6),
        "sello": (5.0, 3.6),
        "enemigos": [],
        "adornos": [("Vasijas_01", -0.6, -3.4), ("Vasijas_02", 6.5, -3.2), ("Escombros_Basalto_03", 7.2, 2.0)],
        "losas": [1, 1, 2], "muros": [1, 2, 3]},
}


def planificar_piso(piso):
    """Un piso de la mazmorra olmeca, «La ciudad bajo la selva» (M2: la base; el contenido de cada piso llega en M3 y
    M4). Salas de losas sobre el vacío unidas por pasillos, con muros de columnas en los bordes traseros (lados -X y +Y,
    lejos de la cámara); en los delanteros asoma el grueso del piso. Se sube por la escalera del muro -X de la primera
    sala y se baja por la horneada en el suelo. colision["vacio"] son las celdas sin piso (en Unity, cajas que no se
    atraviesan) y colision["puntos"], los puntos de juego. Mismo formato que planificar_selva."""
    datos = PISOS[piso]
    rng = random.Random(70 + piso)
    X0, X1, Y0, Y1 = datos["limites"]
    celdas = datos["celdas"]
    sx, sy = datos["subida"]
    bx, by = datos.get("sello", (99, 99))

    suelo = []
    for X, Y in sorted(celdas, key=lambda p: p[0] - p[1]):
        suelo.append((f"Tile_Losa_Basalto_{rng.choice(datos['losas']):02d}", X, Y))

    # Muros en los bordes traseros; la escalera de subida ocupa tres módulos del muro -X
    objetos = []
    muro = lambda: rng.choice(datos["muros"])
    for X, Y in sorted(celdas):
        if (X - 1, Y) not in celdas and not (abs(X - 0.5 - (sx - 0.9)) < 0.6 and abs(Y - sy) < 1.5):
            objetos.append((f"Muro_Basalto_Y_{muro():02d}", X - 0.5, Y, 0.0))
        if (X, Y + 1) not in celdas:
            objetos.append((f"Muro_Basalto_{muro():02d}", X, Y + 0.5, 0.0))

    objetos.append(("Escalera_Subida_01", sx, sy, 0.0))
    if "memoria" in datos:
        objetos.append(("Santuario_Memoria_01", *datos["memoria"], 0.0))
    if "vida" in datos:
        objetos.append(("Santuario_Vida_01", *datos["vida"], 0.0))
    if "sello" in datos:
        objetos.append(("Estela_Grande_01", bx, by, 0.0))
    for nombre, X, Y in datos["adornos"]:
        objetos.append((nombre, X, Y, 0.0))
    marcas = [("Escalera_Bajada_01", *datos["bajada"])] if "bajada" in datos else []
    if "sello" in datos:   # la corrupción sale de debajo de la losa sellada
        marcas += [("Grieta_Olvido_02", bx - 1.0, by - 1.3), ("Grieta_Olvido_01", bx + 1.2, by - 1.0)]

    punto = lambda p: {"x": round(p[0], 2), "y": round(p[1], 2)}
    puntos = {clave: punto(datos[clave]) for clave in
              ("subida", "llegada_arriba", "bajada", "llegada_abajo", "memoria", "memoria_llegada", "vida", "sello")
              if clave in datos}
    puntos["enemigos"] = [punto(p) for p in datos["enemigos"]]
    colision = {
        "niveles": [], "niveles_recinto": [], "rampas": [], "agua": [], "agua_bloqueo": [], "muelles": [],
        "limites": [X0, X1, Y0, Y1],
        "vacio": sorted((X, Y) for X in range(X0, X1 + 1) for Y in range(Y0, Y1 + 1) if (X, Y) not in celdas),
        "puntos": puntos,
    }
    return {"suelo": suelo, "objetos": objetos, "dosel": [], "marcas": marcas}, colision


def exportar_ciudad(salida, plan, colision):
    """ciudad.json para el generador de la escena de Unity (etapa D): sprites con posición en metros
    (x, y del mundo del flujo; z hacia arriba) y los datos de colisión."""
    datos = {
        "nota": "Coordenadas del flujo Blender: +X derecha-abajo en pantalla, +Y derecha-arriba, Z arriba; 1 unidad = 1 m.",
        "suelo": [{"sprite": n, "x": X, "y": Y} for n, X, Y in plan["suelo"]],
        "dosel": [{"sprite": n, "x": round(X, 3), "y": round(Y, 3)} for n, X, Y in plan["dosel"]],
        "objetos": [{"sprite": n, "x": round(X, 3), "y": round(Y, 3), "z": z} for n, X, Y, z in plan["objetos"]],
        "colision": colision,
    }
    with open(os.path.join(salida, "ciudad.json"), "w", encoding="utf-8") as fh:
        json.dump(datos, fh, ensure_ascii=False, indent=1)


# Vista previa de la ciudad completa (sin ampliar) y recortes de 640×360 ampliados ×3
TAM_CIUDAD = (3264, 1800)
CENTRO_CIUDAD = (1508, 1000)
RECORTES_CIUDAD = {"Plaza": (0, 5), "Montículo": (0, 16), "Recinto": (0, -9), "Aldea": (15, -2)}


# La selva: 36×20 m
TAM_SELVA = (2048, 1216)
CENTRO_SELVA = (1024, 712)
RECORTES_SELVA = {"Llegada": (14, 0), "Claro": (2, 0.5), "Entrada": (-11, -2)}

# Los pisos de la mazmorra caben en el lienzo de la selva; fondo casi negro (el vacío bajo el piso)
FONDO_MAZMORRA = "#0B0D0B"
RECORTES_PISO = {1: {"Llegada": (-11.5, -1), "Sala": (0, 0), "Bajada": (11, -1)},
                 2: {"Llegada": (-11.5, 0), "Sala": (0, 0), "Vida": (0, -7)},
                 3: {"Llegada": (-7, 0), "Antesala": (3.5, 0.5)}}


def componer_region(salida, metas, plan, archivo, tam, centro, recortes, fondo="#13241A"):
    """Vista previa de una región completa (sin ampliar) y recortes de 640×360 ampliados ×3."""
    lienzo = componer(salida, metas, plan, archivo=archivo, tam=tam, centro=centro, ampliar=False, fondo=fondo)
    for nombre, (X, Y) in recortes.items():
        sx, sy = pantalla(X, Y, centro=centro)
        x0, y0 = sx - W_COMP // 2, sy - H_COMP // 2
        recorte = lienzo[y0:y0 + H_COMP, x0:x0 + W_COMP]
        pa.guardar(pa.ampliar(recorte, 3), os.path.join(salida, f"{archivo}_{nombre}_1920x1080_x3.png"))


def componer_ciudad(salida, metas, plan):
    componer_region(salida, metas, plan, "Ciudad", TAM_CIUDAD, CENTRO_CIUDAD, RECORTES_CIUDAD)


def componer(salida, metas, plan, cuadro_agua=1, archivo="Composicion", tam=(W_COMP, H_COMP), centro=None, ampliar=True,
             fondo="#13241A"):
    """Escena de prueba con la proyección de la cámara del juego (640x360 por defecto). No es una captura de Unity."""
    centro = centro or (tam[0] / 2, tam[1] / 2)
    lienzo = pa.sobre_fondo(pa.np.zeros((tam[1], tam[0], 4), dtype=pa.np.float32), fondo)
    cache = {}

    def img(n):
        if n not in cache:
            cache[n] = pa.cargar(os.path.join(salida, n + ".png"))
        return cache[n]

    def poner(n, X, Y, Z=0.0, sombra=False):
        meta = metas[n]["sombra"] if sombra else metas[n]
        i = img(n + "_Sombra") if sombra else img(n)
        sx, sy = pantalla(X, Y, Z, centro)
        pegar(lienzo, i, sx - meta["pivote_px"][0], sy - (i.shape[0] - meta["pivote_px"][1]))

    for n, X, Y in plan["suelo"]:   # ya ordenado por profundidad; la orilla va justo después de su agua
        poner(f"Tile_Agua_Anim_{cuadro_agua:02d}" if n == "AGUA" else n, X, Y)
    for n, X, Y in plan["dosel"] + plan.get("marcas", []):
        poner(n, X, Y)
    for n, X, Y, Z in plan["objetos"]:
        if "sombra" in metas[n]:
            poner(n, X, Y, Z, sombra=True)
    for n, X, Y, Z in sorted(plan["objetos"], key=lambda o: (o[1] - o[2], o[3])):   # a igual fondo, lo de abajo primero
        poner(n, X, Y, Z)

    pa.guardar(lienzo, os.path.join(salida, f"{archivo}_{tam[0]}x{tam[1]}.png"))
    if ampliar:
        pa.guardar(pa.ampliar(lienzo, 3), os.path.join(salida, f"{archivo}_{tam[0] * 3}x{tam[1] * 3}_x3.png"))
    return lienzo


# ---------------------------------------------------------------- exportación a Unity (etapa C)

RAIZ_PROYECTO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SPRITES_UNITY = os.path.join(RAIZ_PROYECTO, "Assets", "_Project", "Art", "Environment", "Olmeca", "Sprites")
DATOS_UNITY = os.path.join(RAIZ_PROYECTO, "Assets", "_Project", "Data", "World", "Olmeca")
TROZO_SUELO = 1024
# Lo que se hornea en el suelo (no se exporta como sprite suelto)
NO_EXPORTAR = ("Tile_Camino", "Tile_Pasto", "Tile_Orilla", "Sombra_Dosel", "Grieta_Olvido", "Tile_Losa", "Escalera_Bajada")


def capa_suelo(salida, metas, plan, tam, centro):
    """Suelo de una región horneado en una sola imagen, en el mismo orden que la composición: tiles (sin el agua, que
    en Unity va animada por debajo), sombras del dosel, grietas y sombras de los objetos. Lo transparente deja ver el
    agua."""
    lienzo = pa.np.zeros((tam[1], tam[0], 4), dtype=pa.np.float32)
    cache = {}

    def img(n):
        if n not in cache:
            cache[n] = pa.cargar(os.path.join(salida, n + ".png"))
        return cache[n]

    def poner(n, X, Y, Z=0.0, sombra=False):
        meta = metas[n]["sombra"] if sombra else metas[n]
        i = img(n + "_Sombra") if sombra else img(n)
        sx, sy = pantalla(X, Y, Z, centro)
        pegar(lienzo, i, sx - meta["pivote_px"][0], sy - (i.shape[0] - meta["pivote_px"][1]))

    for n, X, Y in plan["suelo"]:
        if n != "AGUA":
            poner(n, X, Y)
    for n, X, Y in plan["dosel"] + plan.get("marcas", []):
        poner(n, X, Y)
    for n, X, Y, Z in plan["objetos"]:
        if "sombra" in metas[n]:
            poner(n, X, Y, Z, sombra=True)
    # La sombra semitransparente sobre un píxel vacío (agua) quedaría con alfa parcial: el material recorta a 0.5,
    # así que se fuerza el alfa a 0 o 1 (sobre el agua no se ven sombras; en tierra ya son opacas tras mezclar)
    lienzo[..., 3] = (lienzo[..., 3] >= 0.5).astype(pa.np.float32)
    return lienzo


def trozos_suelo(salida, metas, plan, region, tam, centro, tmp, destinos, pivotes):
    """Hornea el suelo de una región y lo parte en trozos de hasta TROZO_SUELO px (Suelo_<Región>_NN)."""
    suelo = capa_suelo(salida, metas, plan, tam, centro)
    op = pa.np.argwhere(suelo[..., 3] > 0.5)
    (y0, x0), (y1, x1) = op.min(0), op.max(0) + 1
    trozos = []
    for ty in range(y0, y1, TROZO_SUELO):
        for tx in range(x0, x1, TROZO_SUELO):
            t = suelo[ty:min(ty + TROZO_SUELO, y1), tx:min(tx + TROZO_SUELO, x1)]
            if not (t[..., 3] > 0.5).any():
                continue
            nombre = f"Suelo_{region}_{len(trozos):02d}"
            ruta = os.path.join(tmp, nombre + ".png")
            pa.guardar(t, ruta)
            h, w = t.shape[:2]
            # Centro del trozo respecto al píxel del origen del mundo, en metros sobre el plano de la cámara
            derecha = (tx + w / 2 - centro[0]) / pa.PPU
            arriba = (centro[1] - (ty + h / 2)) / pa.PPU
            trozos.append({"sprite": nombre, "derecha": round(derecha, 5), "arriba": round(arriba, 5)})
            destinos["Suelo"].append((nombre, ruta))
            pivotes.append({"nombre": nombre, "lienzo": [w, h], "pivote": [w / 2, h / 2]})
    return trozos


def exportar_unity(salida, metas, regiones):
    """Copia a Assets los sprites del juego y los datos de cada región (<Región>.json). regiones = [(nombre, plan,
    colision, tam, centro)]: todas a la vez, porque cada carpeta se limpia de lo que ya no se genera. Conserva los
    .meta de los archivos que siguen existiendo (GUID estables)."""
    destinos = {"Objetos": [], "Agua": [], "Suelo": []}
    pivotes = []
    for nombre, meta in sorted(metas.items()):
        if nombre.startswith(NO_EXPORTAR):
            continue
        carpeta = "Agua" if nombre.startswith("Tile_Agua") else "Objetos"
        destinos[carpeta].append((nombre, os.path.join(salida, nombre + ".png")))
        pivotes.append({"nombre": nombre, "lienzo": list(meta["lienzo"]), "pivote": list(meta["pivote_px"])})

    tmp = os.path.join(salida, "_suelo")
    os.makedirs(tmp, exist_ok=True)
    datos = {}
    for region, plan, colision, tam, centro in regiones:
        trozos = trozos_suelo(salida, metas, plan, region, tam, centro, tmp, destinos, pivotes)
        datos[region] = {
            "nota": "Coordenadas del flujo Blender (x, y en el suelo, z arriba; 1 unidad = 1 m). En Unity: (y, z, -x).",
            "agua": [{"x": X, "y": Y} for n, X, Y in plan["suelo"] if n == "AGUA"],
            "suelo": trozos,
            "objetos": [{"sprite": n, "x": round(X, 3), "y": round(Y, 3), "z": z} for n, X, Y, z in plan["objetos"]],
            "niveles": [{"celdas": [{"x": c[0], "y": c[1]} for c in nivel], "z": k}
                        for k, nivel in enumerate(colision["niveles"])]
                       + [{"celdas": [{"x": c[0], "y": c[1]} for c in nivel], "z": k}
                          for k, nivel in enumerate(colision["niveles_recinto"])],
            "rampas": colision["rampas"],
            "agua_bloqueo": [{"x": c[0], "y": c[1]} for c in colision["agua_bloqueo"]],
            "muelles": colision["muelles"],
            "limites": colision["limites"],
            "puntos": colision["puntos"],
        }
        if "vacio" in colision:   # pisos de la mazmorra
            datos[region]["vacio"] = [{"x": c[0], "y": c[1]} for c in colision["vacio"]]

    for carpeta, archivos in destinos.items():
        dest = os.path.join(SPRITES_UNITY, carpeta)
        os.makedirs(dest, exist_ok=True)
        nuevos = {n + ".png" for n, _ in archivos}
        for viejo in os.listdir(dest):
            base = viejo[:-5] if viejo.endswith(".meta") else viejo
            if base.endswith(".png") and base not in nuevos:
                os.remove(os.path.join(dest, viejo))
        for n, origen in archivos:
            shutil.copyfile(origen, os.path.join(dest, n + ".png"))
    shutil.rmtree(tmp)

    with open(os.path.join(SPRITES_UNITY, "pivotes_unity.json"), "w", encoding="utf-8") as fh:
        json.dump({"ppu": pa.PPU, "sprites": pivotes}, fh, ensure_ascii=False, indent=1)
    os.makedirs(DATOS_UNITY, exist_ok=True)
    for region, contenido in datos.items():
        with open(os.path.join(DATOS_UNITY, region + ".json"), "w", encoding="utf-8") as fh:
            json.dump(contenido, fh, ensure_ascii=False, indent=1)
    print("Unity:", sum(len(a) for a in destinos.values()), "sprites en", SPRITES_UNITY)


def main():
    salida = os.path.abspath(sys.argv[sys.argv.index("--") + 1])
    if os.path.isdir(salida):
        shutil.rmtree(salida)   # la carpeta solo contiene salidas generadas por este script
    tmp = os.path.join(salida, "_render")
    os.makedirs(tmp)
    plan, mascaras = planificar()
    plan_ruinas, mascaras["Plataforma_Arcilla"] = planificar_ruinas()
    plan_ciudad, mascaras_ciudad, colision = planificar_ciudad()
    plan_selva, mascaras_selva, colision_selva = planificar_selva()
    plan_aldea, mascaras_aldea = planificar_aldea()
    mascaras["Tile_Orilla"] |= mascaras_aldea
    for tipo, ms in list(mascaras_ciudad.items()) + list(mascaras_selva.items()):
        mascaras[tipo] |= ms
    pisos = [(f"Mazmorra_P{n}", *planificar_piso(n)) for n in sorted(PISOS)]
    lotes = [(tipo, list(range(1, n + 1))) for tipo, n in VARIANTES.items()]
    lotes += [(tipo, sorted(ms)) for tipo, ms in mascaras.items()]
    solo_mazmorra = "--solo-mazmorra" in sys.argv   # para revisar el arte de los pisos sin rehacer todo (no exporta)
    if solo_mazmorra:
        usados = {n.rsplit("_", 1)[0] for _, plan_, _ in pisos for n, *_ in plan_["suelo"] + plan_["objetos"] + plan_["marcas"]}
        lotes = [(tipo, vs) for tipo, vs in lotes if tipo in usados]
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
    for region, plan_piso, _ in pisos:
        n = int(region[-1])
        componer_region(salida, metas, plan_piso, region, TAM_SELVA, CENTRO_SELVA, RECORTES_PISO[n], fondo=FONDO_MAZMORRA)
    if solo_mazmorra:
        shutil.rmtree(tmp)
        print("LISTO (solo mazmorra)", salida, len(metas), "sprites")
        return
    componer(salida, metas, plan)
    componer(salida, metas, plan_ruinas, archivo="Prueba_Ruinas")
    componer(salida, metas, plan_aldea, archivo="Prueba_Aldea")
    exportar_ciudad(salida, plan_ciudad, colision)
    componer_ciudad(salida, metas, plan_ciudad)
    componer_region(salida, metas, plan_selva, "Selva", TAM_SELVA, CENTRO_SELVA, RECORTES_SELVA)
    if "--sin-unity" not in sys.argv:
        exportar_unity(salida, metas, [("Ciudad", plan_ciudad, colision, TAM_CIUDAD, CENTRO_CIUDAD),
                                       ("Selva", plan_selva, colision_selva, TAM_SELVA, CENTRO_SELVA)]
                       + [(region, plan_piso, colision_piso, TAM_SELVA, CENTRO_SELVA)
                          for region, plan_piso, colision_piso in pisos])
    shutil.rmtree(tmp)
    print("LISTO", salida, len(metas), "sprites")


main()
