# Flujo común Blender -> Pixel Art isométrico de Crónicas de México.
# Valores definidos en Docs/Arte_Olmeca_Demo.md (§2 paleta, §3 escala, §4 cámara, §10 flujo).
import math
import os

import bpy
import numpy as np
from mathutils import Vector

PPU = 32 * math.sqrt(2)   # píxeles de pantalla por metro de mundo (rombo de 64x32 por tile de 1 m)
SS = 4                    # supersampling: se renderiza a 4x y se reduce

# Ejes de la cámara isométrica 2:1 (rotación X=60, Z=45 en Blender = X=30, Y=45 en Unity)
CAM_ROT = (math.radians(60), 0.0, math.radians(45))
DERECHA = Vector((0.70710678, 0.70710678, 0.0))
ARRIBA = Vector((-0.35355339, 0.35355339, 0.8660254))
VISTA = Vector((-0.61237244, 0.61237244, -0.5))

# Luz común: sol desde arriba-izquierda de la pantalla (cara superior clara, izquierda media, derecha oscura)
HACIA_SOL = Vector((-0.3, -0.5, 1.2)).normalized()

RAMPAS = {
    "tierra": ["#3A2A1A", "#644B2A", "#7B5B33", "#9B713F", "#B6814C", "#C6925A", "#E3B47C", "#F1C891"],
    "basalto": ["#1E2420", "#2C302A", "#36382F", "#4A473B", "#615C48", "#7D745B", "#9C8B75", "#C9C0AB"],
    "verde_calido": ["#1D2B1A", "#23551C", "#387022", "#568B26", "#73AD36", "#92B535", "#AAC652"],
    "verde_frio": ["#0F2A17", "#124524", "#136B35", "#217233", "#328F3E", "#4A993E"],
    "musgo": ["#3E5F27", "#5A8732", "#84B345", "#899736"],
    "madera": ["#48331F", "#6D502E", "#9C7842", "#A28A6D"],
    "agua": ["#0B4F4A", "#0C7265", "#168787", "#489187", "#7FC3B5"],
    "acentos": ["#B16143", "#B24232", "#994F66", "#F47588"],
    # Luz propia: la corrupción de la Sombra del Olvido usa un violeta ajeno a la selva y los fragmentos de memoria,
    # el verde de la piedra pulida (Docs/Arte_Olmeca_Demo.md §2)
    "olvido": ["#2A1838", "#4B2A6B", "#7B4FB0", "#A98BE8", "#D9CCFF"],
    "jade": ["#1F5148", "#2E7D6B", "#4FB39A", "#9BE3C8", "#E2FFF2"],
    # El fuego del santuario de Vida (mazmorra, M2)
    "fuego": ["#5A2412", "#9A3A1C", "#D8642A", "#F2A541", "#FFE08A"],
}


def hex_srgb(h):
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (1, 3, 5))


def hex_lineal(h, a=1.0):
    return tuple(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in hex_srgb(h)) + (a,)


def paleta(*rampas):
    return [c for r in rampas for c in RAMPAS[r]]


# ---------------------------------------------------------------- escena

def nueva_escena(muestras=48):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = muestras
    sc.cycles.use_denoising = False
    sc.cycles.filter_width = 1.0
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'

    mundo = bpy.data.worlds.new("Mundo")
    _usar_nodos(mundo)
    fondo = mundo.node_tree.nodes['Background']
    fondo.inputs['Color'].default_value = hex_lineal("#8FA89A")
    fondo.inputs['Strength'].default_value = 0.95
    sc.world = mundo

    sol = bpy.data.objects.new("Sol", bpy.data.lights.new("Sol", 'SUN'))
    sol.data.energy = 4.6
    sol.data.angle = math.radians(2)
    sol.data.color = hex_srgb("#FFF1D6")
    sol.rotation_euler = HACIA_SOL.to_track_quat('Z', 'Y').to_euler()
    sc.collection.objects.link(sol)

    cam = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
    cam.data.type = 'ORTHO'
    cam.rotation_euler = CAM_ROT
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc


def _usar_nodos(idblock):
    try:
        idblock.use_nodes = True
    except (AttributeError, TypeError):
        pass  # en Blender 5 los nodos están siempre activos


def enlazar(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def malla(nombre, verts, caras, colores=None):
    """Crea un objeto de malla; colores = lista hex por vértice (atributo 'Col')."""
    me = bpy.data.meshes.new(nombre)
    me.from_pydata([tuple(v) for v in verts], [], caras)
    me.update()
    if colores is not None:
        attr = me.color_attributes.new(name="Col", type='FLOAT_COLOR', domain='POINT')
        for i, h in enumerate(colores):
            attr.data[i].color = hex_lineal(h)
    return enlazar(bpy.data.objects.new(nombre, me))


# ---------------------------------------------------------------- materiales

def _entrada(sockets, nombre, tipo=None):
    return next(s for s in sockets if s.name == nombre and (tipo is None or s.type == tipo))


def _bsdf(m):
    _usar_nodos(m)
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Roughness'].default_value = 1.0
    try:
        bsdf.inputs['Specular IOR Level'].default_value = 0.0
    except KeyError:
        pass
    return nt, bsdf


def _rampa(nt, colores, desde=0.3, hasta=0.7):
    r = nt.nodes.new('ShaderNodeValToRGB')
    els = r.color_ramp.elements
    while len(els) < len(colores):
        els.new(0.5)
    for i, h in enumerate(colores):
        els[i].position = desde + (hasta - desde) * i / max(1, len(colores) - 1)
        els[i].color = hex_lineal(h)
    return r


def _ruido(nt, escala, estirar=(1, 1, 1), detalle=4.0):
    tc = nt.nodes.new('ShaderNodeTexCoord')
    mp = nt.nodes.new('ShaderNodeMapping')
    mp.inputs['Scale'].default_value = estirar
    nz = nt.nodes.new('ShaderNodeTexNoise')
    nz.inputs['Scale'].default_value = escala
    nz.inputs['Detail'].default_value = detalle
    nt.links.new(tc.outputs['Object'], mp.inputs['Vector'])
    nt.links.new(mp.outputs['Vector'], nz.inputs['Vector'])
    return nz, tc


def mat_ruido(nombre, colores, escala=8.0, estirar=(1, 1, 1), desde=0.3, hasta=0.7):
    m = bpy.data.materials.new(nombre)
    nt, bsdf = _bsdf(m)
    nz, _ = _ruido(nt, escala, estirar)
    r = _rampa(nt, colores, desde, hasta)
    nt.links.new(nz.outputs['Fac'], r.inputs['Fac'])
    nt.links.new(r.outputs['Color'], bsdf.inputs['Base Color'])
    return m


def mat_piedra_musgo(nombre, piedra, musgo, escala=9.0, estirar=(1, 1, 0.35), altura_musgo=0.45, umbral_musgo=0.33):
    """Piedra con vetas verticales y musgo en caras superiores y cerca del suelo."""
    m = bpy.data.materials.new(nombre)
    nt, bsdf = _bsdf(m)
    nz, tc = _ruido(nt, escala, estirar)
    rp = _rampa(nt, piedra, 0.32, 0.68)
    nt.links.new(nz.outputs['Fac'], rp.inputs['Fac'])

    nm, _ = _ruido(nt, 14.0, (1, 1, 1), 6.0)
    rm = _rampa(nt, musgo, 0.4, 0.65)
    nt.links.new(nm.outputs['Fac'], rm.inputs['Fac'])

    sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    nt.links.new(tc.outputs['Object'], sep.inputs['Vector'])
    bajo = nt.nodes.new('ShaderNodeMapRange')
    bajo.inputs['From Min'].default_value = 0.0
    bajo.inputs['From Max'].default_value = altura_musgo
    bajo.inputs['To Min'].default_value = 1.0
    bajo.inputs['To Max'].default_value = 0.0
    nt.links.new(sep.outputs['Z'], bajo.inputs['Value'])

    geo = nt.nodes.new('ShaderNodeNewGeometry')
    sepn = nt.nodes.new('ShaderNodeSeparateXYZ')
    nt.links.new(geo.outputs['Normal'], sepn.inputs['Vector'])
    cima = nt.nodes.new('ShaderNodeMapRange')
    cima.inputs['From Min'].default_value = 0.55
    cima.inputs['From Max'].default_value = 0.95
    nt.links.new(sepn.outputs['Z'], cima.inputs['Value'])

    mx = nt.nodes.new('ShaderNodeMath'); mx.operation = 'MAXIMUM'
    nt.links.new(bajo.outputs['Result'], mx.inputs[0])
    nt.links.new(cima.outputs['Result'], mx.inputs[1])
    mul = nt.nodes.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'
    nt.links.new(mx.outputs['Value'], mul.inputs[0])
    nt.links.new(nm.outputs['Fac'], mul.inputs[1])
    umbral = nt.nodes.new('ShaderNodeMath'); umbral.operation = 'GREATER_THAN'
    umbral.inputs[1].default_value = umbral_musgo
    nt.links.new(mul.outputs['Value'], umbral.inputs[0])

    mez = nt.nodes.new('ShaderNodeMix'); mez.data_type = 'RGBA'
    nt.links.new(umbral.outputs['Value'], _entrada(mez.inputs, 'Factor', 'VALUE'))
    nt.links.new(rp.outputs['Color'], _entrada(mez.inputs, 'A', 'RGBA'))
    nt.links.new(rm.outputs['Color'], _entrada(mez.inputs, 'B', 'RGBA'))
    nt.links.new(_entrada(mez.outputs, 'Result', 'RGBA'), bsdf.inputs['Base Color'])
    return m


def mat_color_vertice(nombre):
    m = bpy.data.materials.new(nombre)
    nt, bsdf = _bsdf(m)
    at = nt.nodes.new('ShaderNodeAttribute')
    at.attribute_name = "Col"
    nt.links.new(at.outputs['Color'], bsdf.inputs['Base Color'])
    return m


def asignar(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj


# ---------------------------------------------------------------- render

def encuadrar(ancho, alto, ancla_px, ancla_mundo=(0, 0, 0)):
    """Cámara de modo que ancla_mundo caiga en el píxel ancla_px (x, y desde abajo-izquierda) del lienzo final."""
    sc = bpy.context.scene
    cam = sc.camera
    cam.data.ortho_scale = max(ancho, alto) / PPU
    sc.render.resolution_x = ancho * SS
    sc.render.resolution_y = alto * SS
    sc.render.resolution_percentage = 100
    dx = (ancla_px[0] - ancho / 2) / PPU
    dy = (ancla_px[1] - alto / 2) / PPU
    cam.location = Vector(ancla_mundo) - DERECHA * dx - ARRIBA * dy - VISTA * 30.0
    cam.data.clip_end = 100.0


def renderizar(ruta):
    sc = bpy.context.scene
    sc.render.filepath = ruta
    bpy.ops.render.render(write_still=True)
    return cargar(ruta)


def cargar(ruta):
    img = bpy.data.images.load(ruta)
    w, h = img.size
    a = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    bpy.data.images.remove(img)
    return a.reshape(h, w, 4)[::-1].copy()   # fila 0 = arriba


def guardar(a, ruta):
    h, w, _ = a.shape
    img = bpy.data.images.new(os.path.basename(ruta), w, h, alpha=True)
    img.pixels.foreach_set(np.ascontiguousarray(a[::-1], dtype=np.float32).ravel())
    img.filepath_raw = ruta
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)


# ---------------------------------------------------------------- posprocesado

def reducir(a, s=SS, umbral=0.5):
    """Bloques de s x s -> 1 px. Cobertura >= umbral = opaco; sin semitransparencias."""
    h, w, _ = a.shape
    b = a.reshape(h // s, s, w // s, s, 4)
    peso = b[..., 3]
    suma = (b[..., :3] * peso[..., None]).sum((1, 3))
    tot = peso.sum((1, 3))
    out = np.zeros((h // s, w // s, 4), dtype=np.float32)
    out[..., :3] = suma / np.maximum(tot, 1e-6)[..., None]
    out[..., 3] = (peso.mean((1, 3)) >= umbral).astype(np.float32)
    return out


def _lab(c):
    lin = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    m = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = lin @ m.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def cuantizar(a, colores):
    pal = np.array([hex_srgb(h) for h in colores])
    d = ((_lab(a[..., :3])[..., None, :] - _lab(pal)) ** 2).sum(-1)
    out = a.copy()
    out[..., :3] = pal[d.argmin(-1)]
    return out


def contorno(a, color):
    """Contorno exterior de 1 px."""
    op = a[..., 3] > 0.5
    borde = _vecinos4(op) & ~op
    a[borde, :3] = hex_srgb(color)
    a[borde, 3] = 1.0
    return a


def contorno_mascara(a, mascara, color):
    """Contorno solo alrededor de mascara (p. ej. sin el muro de una cerca modular, para que los
    módulos empalmen sin costuras). Solo se pinta donde el sprite es transparente."""
    borde = _vecinos4(mascara[..., 3] > 0.5) & ~(a[..., 3] > 0.5)
    a[borde, :3] = hex_srgb(color)
    a[borde, 3] = 1.0
    return a


def _vecinos4(op):
    vec = np.zeros_like(op)
    vec[1:] |= op[:-1]
    vec[:-1] |= op[1:]
    vec[:, 1:] |= op[:, :-1]
    vec[:, :-1] |= op[:, 1:]
    return vec


def limpiar(a):
    """Quita píxeles opacos sin vecinos y sustituye píxeles de color aislado por el color vecino más común."""
    op = a[..., 3] > 0.5
    h, w = op.shape
    pad = np.pad(op, 1)
    vecinos = sum(pad[1 + dy:h + 1 + dy, 1 + dx:w + 1 + dx]
                  for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx)
    a[op & (vecinos == 0), 3] = 0.0

    out = a.copy()
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            if a[y, x, 3] < 0.5:
                continue
            vs = [tuple(a[y + dy, x + dx, :3]) for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1))
                  if a[y + dy, x + dx, 3] > 0.5]
            if len(vs) == 4 and tuple(a[y, x, :3]) not in vs:
                out[y, x, :3] = max(set(vs), key=vs.count)
    return out


def unificar_borde_trasero(a, fila_esquinas):
    """Tiles: la primera fila opaca de los bordes traseros del rombo (por encima de las esquinas laterales)
    sale de un tono distinto por la cobertura parcial y forma líneas al encajar tiles. Se copia el píxel interior."""
    op = a[..., 3] > 0.5
    for x in range(a.shape[1]):
        ys = np.flatnonzero(op[:, x])
        if len(ys) > 1 and ys[0] < fila_esquinas and op[ys[0] + 1, x]:
            a[ys[0], x, :3] = a[ys[0] + 1, x, :3]
    return a


def toca_borde(a):
    op = a[..., 3] > 0.5
    return bool(op[0].any() or op[-1].any() or op[:, 0].any() or op[:, -1].any())


def sombra(a, color="#0F2A17", opacidad=0.45, umbral=0.25):
    """Render de shadow catcher -> sprite de sombra plano y semitransparente."""
    r = reducir(a, umbral=umbral)
    out = np.zeros_like(r)
    out[..., :3] = hex_srgb(color)
    out[..., 3] = r[..., 3] * opacidad
    return out


def ampliar(a, k):
    return np.repeat(np.repeat(a, k, axis=0), k, axis=1)


def sobre_fondo(a, color):
    out = np.empty_like(a)
    out[..., :3] = hex_srgb(color)
    out[..., 3] = 1.0
    al = a[..., 3:4]
    out[..., :3] = a[..., :3] * al + out[..., :3] * (1 - al)
    return out
