# Personajes animales del flujo Blender -> Pixel Art (Crónicas de México). Etapas A y B del plan de personajes:
# el jaguar jugable y el jaguar corrupto. Ficha de diseño: Docs/Personajes/Fichas/Jaguar.md y Jaguar_Corrupto.md.
#
# Sin esqueleto de Blender: cada cuadro se construye con mallas paramétricas (troncos y elipsoides) a partir de una
# pose. Las patas se resuelven con cinemática inversa de dos huesos en el plano sagital. El pelaje usa el atributo
# "Rest", la posición de cada vértice en la pose de reposo: así las rosetas no se deslizan entre cuadros. El mismo
# cuerpo sirve para otros felinos y cuadrúpedos de culturas futuras cambiando proporciones, pelaje y poses.
import math

import bpy
from mathutils import Matrix, Vector

import pixelart_comun as pa

# Direcciones de pantalla de DirectionalSpriteSet (N, NE, E, SE, S) = variantes 1–5; el modelo mira a +X (SE).
GIRO_DIRECCION = (90, 45, 0, -45, -90)
# Lienzo de cada cuadro (px), píxel del centro del cuerpo en el suelo y color del contorno
LIENZO = (128, 112)
ANCLA = (64, 44)
CONTORNO = "#140C08"

# Jaguar (D: Panthera onca): cabeza y cuerpo de 1,1 a 1,85 m, cola de 0,45 a 0,75 m, cruz a 0,63–0,76 m; el más
# robusto de los felinos americanos, de cabeza grande, patas cortas y fuertes y rosetas con manchas en el centro.
# Las medidas base son de un adulto mediano; ESCALA lo agranda un poco para que se lea en pantalla.
ESCALA = 1.1
CENTRO_Z = 0.48            # altura del centro del cuerpo de pie
LARGO_COLA = 0.62
CABEZA = 1.22             # cabeza grande y ancha, rasgo del jaguar frente a otros felinos

# Tronco: estaciones (x, z del eje, medio ancho, medio alto), de la grupa al pecho
TRONCO = [(-0.55, 0.51, 0.08, 0.08), (-0.49, 0.51, 0.15, 0.16), (-0.36, 0.51, 0.18, 0.2), (-0.16, 0.5, 0.165, 0.18),
          (0.06, 0.49, 0.185, 0.225), (0.25, 0.48, 0.2, 0.26), (0.40, 0.49, 0.18, 0.23), (0.49, 0.52, 0.12, 0.14)]

# Patas: articulación superior en el cuerpo (x, y, z), largos de los huesos y radios
PATAS = {
    # delantera izquierda/derecha (la derecha es -Y, la que ve la cámara en SE), trasera izquierda/derecha
    "DI": {"raiz": (0.31, 0.12, 0.42), "huesos": (0.23, 0.22), "radios": (0.092, 0.07, 0.064), "delantera": True},
    "DD": {"raiz": (0.31, -0.12, 0.42), "huesos": (0.23, 0.22), "radios": (0.092, 0.07, 0.064), "delantera": True},
    "TI": {"raiz": (-0.36, 0.11, 0.45), "huesos": (0.24, 0.23), "radios": (0.11, 0.066, 0.056), "delantera": False},
    "TD": {"raiz": (-0.36, -0.11, 0.45), "huesos": (0.24, 0.23), "radios": (0.11, 0.066, 0.056), "delantera": False},
}
PIE_X = {"DI": 0.35, "DD": 0.35, "TI": -0.38, "TD": -0.38}
METATARSO = 0.13           # trasera: del corvejón al pie
MUNECA = 0.07              # delantera: de la muñeca al suelo

PELAJE = {
    # oro, crema del vientre, centro de la roseta, anillo de la roseta, nariz, ojos, interior de la boca
    "jaguar": {"base": "#D39A3E", "base_oscura": "#B87A2C", "crema": "#F2DFB4", "centro": "#A8642A", "anillo": "#24160E",
               "nariz": "#6A3E36", "ojo": "#E4B436", "boca": "#6B2424", "rampas": ("jaguar", "crema_jaguar")},
    # el corrupto: pelaje violeta casi negro, rosetas doradas como cicatrices y grietas que brillan (rampa olvido)
    "corrupto": {"base": "#4B2E6B", "base_oscura": "#2E1C45", "crema": "#7C68A0", "centro": "#6E4AA0",
                 "anillo": "#D9A93A", "nariz": "#1E1230", "ojo": "#D9CCFF", "boca": "#A98BE8",
                 "rampas": ("corrupto", "oro_corrupto", "olvido")},
}

pa.RAMPAS.setdefault("jaguar", ["#1A100A", "#24160E", "#4A2C16", "#7A4A20", "#A8642A", "#C98A36", "#E0AE52", "#ECC77C"])
pa.RAMPAS.setdefault("crema_jaguar", ["#B8A07A", "#D9C49A", "#F2DFB4", "#FBF1DA", "#6A3E36", "#6B2424", "#E4B436"])
pa.RAMPAS.setdefault("corrupto", ["#140C1E", "#1E1230", "#2E1C45", "#4B2E6B", "#6E4AA0", "#7C68A0", "#A390C4"])
pa.RAMPAS.setdefault("oro_corrupto", ["#6E5218", "#A87E24", "#D9A93A", "#F2D36B"])


# ---------------------------------------------------------------- mallas con atributos

def _marco(t):
    """Ejes (lado, arriba) perpendiculares a la tangente t."""
    ref = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((0, 1, 0))
    lado = ref.cross(t).normalized()
    return lado, t.cross(lado).normalized()


def _anillos(puntos, radios, n):
    verts = []
    for i, p in enumerate(puntos):
        t = (puntos[min(i + 1, len(puntos) - 1)] - puntos[max(i - 1, 0)]).normalized()
        lado, arriba = _marco(t)
        ry, rz = radios[i]
        for j in range(n):
            a = 2 * math.pi * j / n
            verts.append(p + lado * (ry * math.cos(a)) + arriba * (rz * math.sin(a)))
    return verts


def _crear(nombre, verts, caras, rest, vientre, mat):
    me = bpy.data.meshes.new(nombre)
    me.from_pydata([tuple(v) for v in verts], [], caras)
    me.update()
    me.polygons.foreach_set("use_smooth", [True] * len(me.polygons))
    a = me.attributes.new("Rest", 'FLOAT_VECTOR', 'POINT')
    a.data.foreach_set("vector", [c for v in rest for c in v])
    b = me.attributes.new("Vientre", 'FLOAT', 'POINT')
    b.data.foreach_set("value", vientre)
    obj = pa.enlazar(bpy.data.objects.new(nombre, me))
    pa.asignar(obj, mat)
    return obj


def tubo(nombre, puntos, radios, mat, rest_puntos=None, vientre=None, n=12):
    """Tronco por estaciones (puntos y radios (medio ancho, medio alto)), con tapas. rest_puntos: las mismas
    estaciones en la pose de reposo (para el pelaje); vientre(i, ángulo) -> 0..1 (crema)."""
    radios = [(r, r) if isinstance(r, (int, float)) else r for r in radios]
    verts = _anillos(puntos, radios, n)
    rest = _anillos(rest_puntos or puntos, radios, n)
    m = len(puntos)
    caras = [(i * n + j, i * n + (j + 1) % n, (i + 1) * n + (j + 1) % n, (i + 1) * n + j)
             for i in range(m - 1) for j in range(n)]
    for extremo, signo in ((0, -1), (m - 1, 1)):
        t = (puntos[min(extremo + 1, m - 1)] - puntos[max(extremo - 1, 0)]).normalized()
        tr = ((rest_puntos or puntos)[min(extremo + 1, m - 1)] - (rest_puntos or puntos)[max(extremo - 1, 0)]).normalized()
        c = len(verts)
        verts.append(puntos[extremo] + t * signo * min(radios[extremo]) * 0.6)
        rest.append((rest_puntos or puntos)[extremo] + tr * signo * min(radios[extremo]) * 0.6)
        anillo = [extremo * n + j for j in range(n)]
        caras += [(anillo[j], anillo[(j + 1) % n], c) if signo > 0 else (anillo[(j + 1) % n], anillo[j], c)
                  for j in range(n)]
    v = [vientre(i, 2 * math.pi * j / n) if vientre else 0.0 for i in range(m) for j in range(n)]
    v += [v[0], v[-1]]
    return _crear(nombre, verts, caras, rest, v, mat)


def elipsoide(nombre, centro, ejes, mat, giro=Matrix.Identity(3), rest_centro=None, vientre=None, n=12, anillos=8):
    """Elipsoide de semiejes ejes, girado por giro (3×3) y centrado en centro. vientre(z normalizada) -> 0..1."""
    verts, rest, caras, v = [], [], [], []
    rc = Vector(rest_centro) if rest_centro is not None else Vector(centro)
    for i in range(anillos + 1):
        th = math.pi * i / anillos
        for j in range(n):
            ph = 2 * math.pi * j / n
            local = Vector((ejes[0] * math.sin(th) * math.cos(ph), ejes[1] * math.sin(th) * math.sin(ph), ejes[2] * math.cos(th)))
            verts.append(Vector(centro) + giro @ local)
            rest.append(rc + local)
            v.append(vientre(-math.cos(th), math.sin(th) * math.cos(ph)) if vientre else 0.0)
    for i in range(anillos):
        for j in range(n):
            caras.append((i * n + j, (i + 1) * n + j, (i + 1) * n + (j + 1) % n, i * n + (j + 1) % n))
    return _crear(nombre, verts, caras, rest, v, mat)


# ---------------------------------------------------------------- materiales

def mat_pelaje(nombre, pelaje, corrupto):
    """Pelaje con rosetas: Voronoi sobre la posición de reposo. El anillo de cada celda (roto con ruido) es la
    roseta y el centro, una mancha más oscura. El corrupto añade franjas borradas que brillan (la Sombra del
    Olvido le ha quitado pedazos)."""
    m = bpy.data.materials.new(nombre)
    nt, bsdf = pa._bsdf(m)
    N, L = nt.nodes, nt.links

    rest = N.new('ShaderNodeAttribute'); rest.attribute_name = "Rest"
    vien = N.new('ShaderNodeAttribute'); vien.attribute_name = "Vientre"
    vor = N.new('ShaderNodeTexVoronoi')
    vor.inputs['Scale'].default_value = 6.5
    vor.inputs['Randomness'].default_value = 0.8
    L.new(rest.outputs['Vector'], vor.inputs['Vector'])
    ruido = N.new('ShaderNodeTexNoise')
    ruido.inputs['Scale'].default_value = 26.0
    L.new(rest.outputs['Vector'], ruido.inputs['Vector'])

    def comparar(op, a, b):
        n_ = N.new('ShaderNodeMath'); n_.operation = op
        L.new(a, n_.inputs[0])
        if isinstance(b, float):
            n_.inputs[1].default_value = b
        else:
            L.new(b, n_.inputs[1])
        return n_.outputs['Value']

    def mezcla(fac, a, b):
        mx = N.new('ShaderNodeMix'); mx.data_type = 'RGBA'
        L.new(fac, pa._entrada(mx.inputs, 'Factor', 'VALUE'))
        for entrada, valor in (('A', a), ('B', b)):
            sock = pa._entrada(mx.inputs, entrada, 'RGBA')
            if isinstance(valor, str):
                sock.default_value = pa.hex_lineal(valor)
            else:
                L.new(valor, sock)
        return pa._entrada(mx.outputs, 'Result', 'RGBA')

    # Variación suave del pelaje y crema en vientre, garganta y hocico
    var = N.new('ShaderNodeTexNoise'); var.inputs['Scale'].default_value = 5.0
    L.new(rest.outputs['Vector'], var.inputs['Vector'])
    base = mezcla(comparar('GREATER_THAN', var.outputs['Fac'], 0.55), pelaje["base"], pelaje["base_oscura"])
    base = mezcla(vien.outputs['Fac'], base, pelaje["crema"])

    d = vor.outputs['Distance']
    sin_vientre = comparar('SUBTRACT', comparar('MULTIPLY', vien.outputs['Fac'], 0.75), 1.0)   # vientre − 1 (≤ 0)
    centro = comparar('MULTIPLY', comparar('LESS_THAN', d, 0.17), comparar('MULTIPLY', sin_vientre, -1.0))
    color = mezcla(centro, base, pelaje["centro"])
    anillo = comparar('MULTIPLY', comparar('GREATER_THAN', d, 0.23), comparar('LESS_THAN', d, 0.45))
    anillo = comparar('MULTIPLY', anillo, comparar('GREATER_THAN', ruido.outputs['Fac'], 0.36))
    color = mezcla(anillo, color, pelaje["anillo"])
    L.new(color, bsdf.inputs['Base Color'])

    if corrupto:   # franjas borradas: celdas grandes al azar que brillan en violeta pálido
        borr = N.new('ShaderNodeTexVoronoi'); borr.inputs['Scale'].default_value = 4.0
        L.new(rest.outputs['Vector'], borr.inputs['Vector'])
        sep = N.new('ShaderNodeSeparateColor')
        L.new(borr.outputs['Color'], sep.inputs['Color'])
        borrado = comparar('LESS_THAN', sep.outputs['Red'], 0.1)
        bsdf.inputs['Emission Color'].default_value = pa.hex_lineal("#D9CCFF")
        L.new(comparar('MULTIPLY', borrado, 1.1), bsdf.inputs['Emission Strength'])
    return m


def mat_plano(nombre, color, emision=0.0):
    m = bpy.data.materials.new(nombre)
    _, bsdf = pa._bsdf(m)
    bsdf.inputs['Base Color'].default_value = pa.hex_lineal(color)
    if emision:
        bsdf.inputs['Base Color'].default_value = (0, 0, 0, 1)
        bsdf.inputs['Emission Color'].default_value = pa.hex_lineal(color)
        bsdf.inputs['Emission Strength'].default_value = emision
    return m


# ---------------------------------------------------------------- poses

def _pata(fase, zancada, alza, apoyo=0.6):
    """Ciclo de una pata: en apoyo va de delante atrás por el suelo; en el aire vuelve levantándose."""
    if fase < apoyo:
        return zancada * (0.5 - fase / apoyo), 0.0, 0.0
    s = (fase - apoyo) / (1 - apoyo)
    return zancada * (-0.5 + s), alza * math.sin(math.pi * s), math.sin(math.pi * s)


def pose_base():
    return {"avance": 0.0, "alto": 0.0, "cabeceo": 0.0, "arco": 0.0, "rodar": 0.0, "respiro": 1.0,
            "cabeza": (0.0, 0.0), "boca": 0.0, "orejas": 0.0, "ojos": 1.0, "garras": None,
            "patas": {k: (0.0, 0.0, 0.0) for k in PATAS}, "libres": False,
            "cola": (0.0, 0.25, 0.0)}   # alzado, curva, vaivén


ANIMACIONES_JAGUAR = {"Reposo": 4, "Caminar": 6, "Correr": 6, "Zarpazo": 4, "Golpeado": 2, "Muerte": 4}
ANIMACIONES_CORRUPTO = {"Reposo": 4, "Caminar": 6, "Acecho": 2, "Salto": 2, "Golpeado": 2, "Muerte": 3}


def pose(anim, cuadro, corrupto=False):
    """Pose del cuadro (0..n-1) de una animación."""
    p = pose_base()
    n = (ANIMACIONES_CORRUPTO if corrupto else ANIMACIONES_JAGUAR)[anim]
    f = cuadro / n
    if corrupto:   # siempre algo encorvado, con el lomo erizado y la boca entreabierta
        p["arco"] = 0.035
        p["boca"] = 0.25
        p["orejas"] = 0.6
    if anim == "Reposo":
        p["respiro"] = 1.0 + 0.025 * math.sin(2 * math.pi * f)
        p["cabeza"] = (2.0 * math.sin(2 * math.pi * f), 0.0)
        p["cola"] = (0.0, 0.3, math.sin(2 * math.pi * f))
    elif anim == "Caminar":
        desfases = {"TI": 0.0, "DI": 0.25, "TD": 0.5, "DD": 0.75}
        p["patas"] = {k: _pata((f + d) % 1.0, 0.42, 0.11) for k, d in desfases.items()}
        p["alto"] = 0.012 * math.cos(4 * math.pi * f)
        p["cabeza"] = (4.0 + 3.0 * math.cos(4 * math.pi * f), 0.0)
        p["cola"] = (0.05, 0.3, math.sin(2 * math.pi * f))
    elif anim == "Correr":   # galope rotatorio: traseras juntas, luego delanteras; el cuerpo se recoge y se estira
        desfases = {"TI": 0.0, "TD": 0.08, "DI": 0.45, "DD": 0.55}
        p["patas"] = {k: _pata((f + d) % 1.0, 0.85, 0.17, apoyo=0.38) for k, d in desfases.items()}
        p["arco"] += 0.05 * math.sin(2 * math.pi * f)
        p["cabeceo"] = 6.0 * math.sin(2 * math.pi * f + 1.0)
        p["alto"] = 0.03 + 0.03 * math.sin(2 * math.pi * f + 2.0)
        p["cabeza"] = (10.0, 0.0)
        p["orejas"] = 1.0
        p["cola"] = (0.35, 0.05, 0.3 * math.sin(2 * math.pi * f))
    elif anim == "Zarpazo":
        claves = [  # (avance, alto, cabeceo, pata DD (dx, dz, flexión), boca, cabeza)
            (-0.05, -0.04, -4, (-0.06, 0.16, 1.0), 0.5, -4),
            (-0.02, 0.02, -14, (0.18, 0.44, 0.3), 1.0, -12),
            (0.08, -0.02, 6, (0.44, 0.12, 0.0), 0.8, 6),
            (0.02, 0.0, 0, (0.18, 0.04, 0.4), 0.3, 0),
        ][cuadro]
        p["avance"], p["alto"], p["cabeceo"] = claves[:3]
        p["patas"]["DD"] = claves[3]
        p["boca"], p["cabeza"] = claves[4], (claves[5], 0.0)
        p["orejas"] = 1.0
        p["garras"] = "DD"
        p["patas"]["TI"] = (0.04, 0.0, 0.0)
        p["patas"]["TD"] = (-0.06, 0.0, 0.0)
        p["cola"] = (0.25, 0.15, 0.6)
    elif anim == "Golpeado":
        k = 1.0 if cuadro == 0 else 0.45
        p["avance"] = -0.07 * k
        p["cabeceo"] = -8 * k
        p["cabeza"] = (-16 * k, 8 * k)
        p["boca"] = 0.8 * k
        p["orejas"] = 1.0
        p["ojos"] = 0.3
        p["patas"]["DI"] = (0.07 * k, 0.0, 0.0)
        p["patas"]["DD"] = (0.05 * k, 0.03 * k, 0.4 * k)
        p["cola"] = (0.4 * k, 0.1, 0.8)
    elif anim == "Muerte":
        claves = [  # (alto, rodar, cabeceo, cabeza, libres, ojos)
            (-0.06, 0, 6, 18, False, 0.6),
            (-0.18, 28, 10, 22, True, 0.3),
            (-0.26, 66, 4, 12, True, 0.0),
            (-0.29, 88, 0, 6, True, 0.0),
        ][min(cuadro, 3) if not corrupto else [0, 2, 3][cuadro]]
        p["alto"], p["rodar"], p["cabeceo"] = claves[:3]
        p["cabeza"] = (claves[3], 0.0)
        p["libres"], p["ojos"] = claves[4], claves[5]
        p["boca"] = 0.35
        if claves[4]:   # patas flojas, algo dobladas
            p["patas"] = {k: (0.06 if PATAS[k]["delantera"] else -0.04, 0.06, 0.6) for k in PATAS}
        else:
            p["patas"]["DI"] = (0.06, 0.0, 0.0)
            p["patas"]["DD"] = (0.08, 0.0, 0.0)
        p["cola"] = (-0.2, 0.05, 0.0)
    elif anim == "Acecho":   # corrupto: se agazapa antes del salto (el aviso)
        k = 1.0 if cuadro == 0 else 1.15
        p["alto"] = -0.12 * k
        p["cabeceo"] = 5
        p["cabeza"] = (-4, 0.0)
        p["boca"] = 0.6
        p["orejas"] = 1.0
        p["patas"]["DI"] = (0.08, 0.0, 0.0)
        p["patas"]["DD"] = (0.06, 0.0, 0.0)
        p["patas"]["TI"] = (0.1 * k, 0.0, 0.0)
        p["patas"]["TD"] = (0.12 * k, 0.0, 0.0)
        p["cola"] = (-0.15, 0.2, 0.9 if cuadro else -0.9)
    elif anim == "Salto":   # corrupto: estirado en el aire y la caída
        if cuadro == 0:
            p["alto"], p["cabeceo"], p["libres"] = 0.2, -8, True
            p["patas"] = {"DI": (0.3, 0.1, 0.0), "DD": (0.36, 0.14, 0.0), "TI": (-0.3, 0.12, 0.3), "TD": (-0.26, 0.1, 0.3)}
            p["boca"] = 1.0
        else:
            p["alto"], p["cabeceo"] = -0.05, 10
            p["patas"] = {"DI": (0.22, 0.0, 0.0), "DD": (0.26, 0.0, 0.0), "TI": (-0.1, 0.08, 0.5), "TD": (-0.14, 0.06, 0.5)}
            p["boca"] = 0.7
        p["orejas"] = 1.0
        p["cola"] = (0.3, 0.0, 0.0)
    return p


# ---------------------------------------------------------------- construcción

def _ik(raiz, objetivo, l1, l2, atras):
    """Articulación intermedia de una cadena de dos huesos en el plano sagital (x, z). atras: la articulación
    queda hacia -X (codo de las delanteras); si no, hacia +X (rodilla de las traseras)."""
    d = objetivo - raiz
    dist = max(1e-4, min(d.length, l1 + l2 - 1e-4))
    d_dir = d.normalized()
    a = (l1 * l1 - l2 * l2 + dist * dist) / (2 * dist)
    h = math.sqrt(max(0.0, l1 * l1 - a * a))
    perp = Vector((d_dir.z, 0, -d_dir.x))   # para una pata hacia abajo apunta a -X
    medio = raiz + d_dir * a + (perp if atras else -perp) * h
    medio.y = raiz.y + (objetivo.y - raiz.y) * 0.5
    return medio


def jaguar(anim, cuadro, variante, corrupto=False):
    """Un cuadro del jaguar (o del jaguar corrupto) en la dirección variante (1–5)."""
    p = pose(anim, cuadro, corrupto)
    pel = PELAJE["corrupto" if corrupto else "jaguar"]
    piel = mat_pelaje("Pelaje", pel, corrupto)
    oscuro = mat_plano("Oscuro", "#1A100A")
    nariz = mat_plano("Nariz", pel["nariz"])
    ojo = mat_plano("Ojo", pel["ojo"], 1.2 if corrupto else 0.0)
    boca = mat_plano("Boca", pel["boca"], 0.9 if corrupto else 0.0)
    blanco = mat_plano("Diente", "#FBF1DA")
    olvido = mat_plano("Grieta", "#A98BE8", 1.3)

    flaco = 0.9 if corrupto else 1.0           # el corrupto es más enjuto y de patas más largas
    largo_pata = 1.07 if corrupto else 1.0
    giro = Matrix.Rotation(math.radians(GIRO_DIRECCION[variante - 1]), 4, 'Z') @ Matrix.Scale(ESCALA, 4)

    # Transformación del cuerpo: avance, altura, cabeceo (+ = nariz abajo) y rodar sobre el costado
    centro = Vector((0.0, 0.0, CENTRO_Z))
    cuerpo = (Matrix.Translation(Vector((p["avance"], 0, p["alto"])) + centro)
              @ Matrix.Rotation(math.radians(p["rodar"]), 4, 'X')
              @ Matrix.Rotation(math.radians(p["cabeceo"]), 4, 'Y')
              @ Matrix.Translation(-centro))

    def en_cuerpo(v):
        return cuerpo @ Vector(v)

    def arco(x):
        return p["arco"] * math.sin(math.pi * (x + 0.55) / 1.04)

    partes = []

    # Tronco
    puntos = [en_cuerpo((x, 0, z + arco(x))) for x, z, _, _ in TRONCO]
    rest = [Vector((x, 0, z)) for x, z, _, _ in TRONCO]
    radios = [(ry * flaco * (p["respiro"] if 0 < x < 0.5 else 1.0), rz * (p["respiro"] if 0 < x < 0.5 else 1.0))
              for x, _, ry, rz in TRONCO]

    def vientre_tronco(i, a):
        abajo = -math.sin(a)
        pecho = 0.25 if TRONCO[i][0] > 0.25 else 0.0
        return max(0.0, min(1.0, (abajo - 0.45 + pecho) / 0.35))

    partes.append(tubo("Tronco", puntos, radios, piel, rest, vientre_tronco, n=14))

    # Cuello y cabeza
    base_cuello = Vector((0.44, 0, 0.57 + arco(0.44)))
    cabeceo_cabeza, guinada = p["cabeza"]
    rot_cab = (Matrix.Rotation(math.radians(guinada), 3, 'Z') @ Matrix.Rotation(math.radians(cabeceo_cabeza), 3, 'Y'))
    centro_cab = base_cuello + rot_cab @ Vector((0.26, 0, 0.09))
    cuello = [en_cuerpo(base_cuello), en_cuerpo(base_cuello.lerp(centro_cab, 0.5) + Vector((0, 0, 0.015))),
              en_cuerpo(centro_cab - rot_cab @ Vector((0.06, 0, 0)))]
    partes.append(tubo("Cuello", cuello, [(0.14 * flaco, 0.15), (0.12 * flaco, 0.13), (0.1, 0.11)], piel,
                       [Vector((0.5, 0, 0.56)), Vector((0.64, 0, 0.62)), Vector((0.71, 0, 0.64))],
                       lambda i, a: max(0.0, -math.sin(a) - 0.2)))
    rot4 = cuerpo.to_3x3() @ rot_cab
    c_cab = en_cuerpo(centro_cab)
    rest_cab = Vector((0.78, 0, 0.64))

    def pieza(nombre, local, ejes, mat, vientre=None, giro_local=Matrix.Identity(3)):
        local = Vector(local) * CABEZA
        ejes = tuple(e * CABEZA for e in ejes)
        partes.append(elipsoide(nombre, c_cab + rot4 @ local, ejes, mat, rot4 @ giro_local,
                                rest_cab + Vector(local), vientre))

    pieza("Craneo", (0, 0, 0), (0.125, 0.105 * flaco, 0.098), piel, lambda z, x: max(0.0, -z - 0.35))
    pieza("Mejillas", (0.035, 0, -0.03), (0.09, 0.118 * flaco, 0.07), piel, lambda z, x: max(0.0, -z * 0.9))
    pieza("Hocico", (0.115, 0, -0.035), (0.075, 0.07, 0.06), piel, lambda z, x: 0.6 + max(0.0, -z) * 0.4)
    apertura = p["boca"]
    giro_mandibula = Matrix.Rotation(math.radians(22 * apertura), 3, 'Y')
    partes.append(elipsoide("Mandibula", c_cab + rot4 @ ((Vector((0.06, 0, -0.07)) + giro_mandibula @ Vector((0.05, 0, -0.01))) * CABEZA),
                            (0.07 * CABEZA, 0.055 * CABEZA, 0.03 * CABEZA), piel, rot4 @ giro_mandibula, rest_cab + Vector((0.1, 0, -0.08)),
                            lambda z, x: 1.0))
    if apertura > 0.2:
        pieza("Boca", (0.12, 0, -0.07 - 0.02 * apertura), (0.045, 0.045, 0.012 + 0.02 * apertura), boca)
        for y in (-0.03, 0.03):
            pieza("Colmillo", (0.165, y, -0.065), (0.008, 0.008, 0.02), blanco)
    pieza("Nariz", (0.185, 0, -0.012), (0.018, 0.026, 0.015), nariz)
    for y in (-1, 1):
        ojos = p["ojos"]
        pieza("Ojo", (0.095, y * 0.055, 0.025), (0.016, 0.02, 0.014 * max(0.25, ojos)), ojo if ojos > 0.2 else oscuro)
        aplastar = p["orejas"]
        oreja_loc = (-0.04 - 0.03 * aplastar, y * 0.075, 0.085 - 0.03 * aplastar)
        oreja_giro = Matrix.Rotation(math.radians(-50 * aplastar), 3, 'Y') @ Matrix.Rotation(math.radians(y * 15), 3, 'X')
        pieza("Oreja", oreja_loc, (0.026, 0.038, 0.045), oscuro if not corrupto else piel, giro_local=oreja_giro)

    # Patas por cinemática inversa
    for clave, datos in PATAS.items():
        dx, dz, flex = p["patas"][clave]
        raiz = en_cuerpo(Vector(datos["raiz"]) + Vector((0, 0, arco(datos["raiz"][0]))))
        l1, l2 = (h * largo_pata for h in datos["huesos"])
        y = datos["raiz"][1]
        suelo = Vector((PIE_X[clave] + dx, y * 1.05, dz))
        # Apoyadas, las patas pisan el suelo; en el aire o al caer siguen al cuerpo
        pie = en_cuerpo(suelo + Vector((0, 0, 0.02))) if p["libres"] else suelo
        if datos["delantera"]:
            muneca = pie + Vector((-0.02 + 0.06 * flex, 0, MUNECA * largo_pata))
            codo = _ik(raiz, muneca, l1, l2, atras=True)
            cadena = [raiz, codo, muneca, pie + Vector((0.035 - 0.05 * flex, 0, 0.025))]
        else:
            corvejon = pie + Vector((-0.05 - 0.06 * flex, 0, METATARSO * largo_pata * (1 - 0.2 * flex)))
            rodilla = _ik(raiz, corvejon, l1, l2, atras=False)
            cadena = [raiz, rodilla, corvejon, pie + Vector((0.03, 0, 0.025))]
        r0, r1, r2 = datos["radios"]
        largos = [0.0]
        for a, b in zip(cadena, cadena[1:]):
            largos.append(largos[-1] + (b - a).length)
        base_rest = Vector((datos["raiz"][0] + (3.0 if datos["delantera"] else -3.0), y * 4, 0))
        rest = [base_rest + Vector((0, 0, -l)) for l in largos]
        partes.append(tubo(f"Pata{clave}", cadena, [r0 * flaco, r1 * flaco, r2 * flaco, r2 * 0.95], piel, rest,
                           lambda i, a: 0.0 if i == 0 else 0.25, n=10))
        zarpa = cadena[-1]
        partes.append(elipsoide(f"Zarpa{clave}", zarpa + Vector((0.025, 0, -0.005)), (0.06, 0.045, 0.028), piel,
                                rest_centro=base_rest + Vector((0, 0, -largos[-1] - 0.05)), vientre=lambda z, x: 0.3))
        if p["garras"] == clave:
            for k in (-1, 0, 1):
                partes.append(elipsoide("Garra", zarpa + Vector((0.075, k * 0.022, -0.01)), (0.022, 0.006, 0.006), blanco))

    # Cola: se curva atrás y abajo y sube en la punta; los últimos anillos oscuros
    alzado, curva, vaiven = p["cola"]
    puntos, rest, radios = [], [], []
    punto = Vector((-0.53, 0, 0.53 + arco(-0.53)))
    angulo = math.radians(-155 + 40 * alzado)
    segs = 10
    for i in range(segs + 1):
        t = i / segs
        puntos.append(en_cuerpo(punto + Vector((0, 0.07 * vaiven * t * t, 0))))
        rest.append(Vector((-0.6 - LARGO_COLA * t, 0, 0.52)))
        radios.append(0.055 - 0.028 * t)
        angulo += math.radians(curva * 60 * t)
        punto = punto + Vector((math.cos(angulo), 0, math.sin(angulo))) * (LARGO_COLA / segs)
    partes.append(tubo("Cola", puntos, radios, piel, rest, lambda i, a: 0.15 if -math.sin(a) > 0.5 else 0.0, n=8))

    # Corrupción: grietas que brillan en los flancos y el lomo
    if corrupto:
        for lado in (-1, 1):
            for x, z, g in ((-0.3, 0.55, 30), (-0.05, 0.47, -20), (0.22, 0.52, 40), (0.36, 0.42, -35)):
                ancho = next(r[2] for r in TRONCO if r[0] >= x) * flaco
                c = en_cuerpo(Vector((x, lado * (ancho + 0.004), z + arco(x))))
                partes.append(elipsoide("Grieta", c, (0.055, 0.006, 0.01), olvido,
                                        cuerpo.to_3x3() @ Matrix.Rotation(math.radians(g), 3, 'X')))
        for x in (-0.25, 0.0, 0.25):
            c = en_cuerpo(Vector((x, 0, 0.5 + next(r[3] for r in TRONCO if r[0] >= x) + arco(x))))
            partes.append(elipsoide("GrietaLomo", c, (0.05, 0.012, 0.006), olvido, cuerpo.to_3x3()))

    for o in partes:
        o.matrix_world = giro @ o.matrix_world
