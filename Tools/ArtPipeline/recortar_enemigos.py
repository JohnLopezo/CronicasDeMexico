# Recorta los enemigos comunes de la lámina "Enemigos y Mazmorra" (Docs/Enemigos y Gefes de mazmorra) a sprites de juego.
# Solo usa numpy; se ejecuta con el Python que trae Blender:
#   "<Blender>/5.2/python/bin/python.exe" -I Tools/ArtPipeline/recortar_enemigos.py Assets/_Project/Art/Environment/Olmeca/Sprites/Enemigos <hoja_revision.png>
# Escribe Enemigo_<Nombre>_<Pose>1_03.png: la lámina trae una sola imagen por pose y solo de lado, mirando al este (los que
# miran al oeste se voltean con "espejo"). Lienzo de 64 de alto con los pies a 2 px (el pivote de PixelArtSpriteImporter).
# Reutiliza la máscara y la reducción de recortar_guerreros.py: la figura es lo que queda al inundar el fondo desde el borde.
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # con -I, Python no añade la carpeta del script
import recortar_guerreros as rg  # noqa: E402
import recortar_npc as npc  # noqa: E402

LAMINA = os.path.join(npc.RAIZ, "Docs", "Enemigos y Gefes de mazmorra", "Crónicas de México_ Enemigos y Mazmorra.png")
ALTO_LIENZO = 64
MARGEN = (6, 4)  # px de lámina alrededor de cada caja: las poses están juntas y uno mayor mete la vecina

# alto: px de juego del reposo (el resto de poses usa su misma escala). poses: nombre -> caja (x0, y0, x1, y1) en la lámina.
# tol: distancia de color al fondo (tol_pose: la de una pose, donde la general borra plumas del color del fondo). espejo: poses que miran al oeste. brillo: figura sin contorno oscuro (espectral); se
# separa por lo que destaca del fondo (brillo_sobre_fondo), con ese umbral. excluir: rectángulos de la lámina (x0, y0, x1,
# y1) que se quitan de la figura en una pose, para lo que la toca sin ser suyo.
FILA_ALTA, FILA_BAJA = (296, 350), (378, 440)
FILA_ALTA_V, FILA_BAJA_V = (632, 690), (705, 758)


def fila(y, *tramos):
    return [(x0, y[0], x1, y[1]) for x0, x1 in tramos]


def poses(nombres, cajas):
    return dict(zip(nombres, cajas))


TERRESTRE = ("Reposo", "Caminar", "Correr", "Ataque", "Defensa", "Dano", "Muerte")
VOLADOR = ("Reposo", "Volar", "Ataque", "Defensa", "Dano", "Muerte")

ENEMIGOS = {
    "Serpiente": {"alto": 30, "tol": 30, "excluir": {"Ataque": ((0, 421, 34, 450),)}, "poses": poses(TERRESTRE, fila(FILA_ALTA, (35, 122), (133, 217), (235, 322))
                                                       + fila(FILA_BAJA, (28, 102), (108, 194), (190, 267), (275, 342)))},
    "Cocodrilo": {"alto": 30, "tol": 60, "poses": poses(TERRESTRE, fila(FILA_ALTA, (357, 452), (455, 547), (550, 642))
                                                       + fila(FILA_BAJA, (355, 452), (450, 514), (513, 577), (578, 647)))},
    "EspirituJaguar": {"alto": 34, "tol": 60, "brillo": 40, "espejo": ("Reposo", "Ataque"),
                       "poses": poses(TERRESTRE, fila(FILA_ALTA, (665, 739), (745, 824), (835, 904))
                                      + fila(FILA_BAJA, (665, 739), (738, 792), (798, 849), (853, 907)))},
    "Mono": {"alto": 32, "tol": 60, "poses": poses(TERRESTRE, fila(FILA_ALTA, (923, 997), (1005, 1084), (1093, 1157))
                                                  + fila(FILA_BAJA, (925, 997), (995, 1067), (1063, 1117), (1113, 1172)))},
    "Avispa": {"alto": 26, "tol": 60, "poses": poses(VOLADOR, fila(FILA_ALTA_V, (41, 119), (147, 233), (266, 350))
                                                    + fila(FILA_BAJA_V, (46, 116), (155, 224), (243, 335)))},
    "Guacamaya": {"alto": 26, "tol": 20, "tol_pose": {"Ataque": 10}, "poses": poses(VOLADOR, fila(FILA_ALTA_V, (397, 484), (502, 606), (622, 732))
                                                       + fila(FILA_BAJA_V, (405, 469), (514, 589), (631, 712)))},
    "Murcielago": {"alto": 22, "tol": 60, "poses": poses(VOLADOR, fila(FILA_ALTA_V, (776, 874), (887, 997), (1013, 1111))
                                                        + fila(FILA_BAJA_V, (796, 871), (896, 988), (1015, 1094)))},
}


def mascara_figura(img, caja, g, pose=None):
    brillo = g.get("brillo_pose", {}).get(pose, g.get("brillo"))
    if brillo is not None:  # sin contorno: la inundación del fondo lo atraviesa
        x0, y0, x1, y1 = caja
        c = img[max(0, y0 - MARGEN[1]):y1 + MARGEN[1], max(0, x0 - MARGEN[0]):x1 + MARGEN[0]]
        m = rg.brillo_sobre_fondo(c, brillo)
        m = ~rg._crecer(~rg._crecer(m, 2), 2)  # cierre: une las líneas de luz sueltas en un solo cuerpo
    else:
        c, m = rg.figura(img, caja, g.get("tol_pose", {}).get(pose, g["tol"]), True, MARGEN)
    # Zonas de la lámina que no son de la figura aunque la toquen (el adorno del marco bajo la cola de la serpiente).
    x0, y0 = max(0, caja[0] - MARGEN[0]), max(0, caja[1] - MARGEN[1])
    for ex0, ey0, ex1, ey1 in g.get("excluir", {}).get(pose, ()):
        m[max(0, ey0 - y0):max(0, ey1 - y0), max(0, ex0 - x0):max(0, ex1 - x0)] = False
    comps = npc._componentes(rg.limpiar(m))
    if not comps:
        raise ValueError(f"{caja}: sin figura")
    ys, xs = np.array(max(comps, key=len)).T  # fuera la pose vecina y los restos del fondo
    m = np.zeros(m.shape, bool)
    m[ys, xs] = True
    CORTES.extend(f"{pose}: {lado}" for lado in cortes(m))
    return c, m


CORTES = []   # lo que cada recorte deja tocando el borde del recorte: la figura sigue fuera de la caja


def cortes(m, filas=6):
    """Lados del recorte que la figura toca en más de <filas> filas o columnas: ahí la caja la corta y en el juego se ve
    «mordida» (reportado por el autor el 2026-11-10). Abajo no cuenta: los pies se apoyan en el borde."""
    return [lado for lado, borde in (("izquierda", m[:, 0]), ("derecha", m[:, -1]), ("arriba", m[0])) if borde.sum() > filas]


def recortar(img, nombre, g, alto_lienzo=ALTO_LIENZO):
    cajas = g["poses"]
    _, m_reposo = mascara_figura(img, cajas["Reposo"], g)
    factor = rg.altura(m_reposo) / g["alto"]
    sprites = {}
    for pose, caja in cajas.items():
        CORTES.clear()
        c, m = mascara_figura(img, caja, g, pose)
        for aviso in CORTES:
            print(f"AVISO {nombre} {aviso}: la caja corta la figura")
        spr = rg.reducir(c, m, factor)
        if pose in g.get("espejo", ()):
            spr = spr[:, ::-1]
        sprites[pose] = spr
    # colocar() centra por la mediana del cuerpo: en una figura asimétrica, el lado largo manda.
    def medio_ancho(s):
        centro = int(np.median(np.nonzero(s[..., 3] > 0)[1]))
        return max(centro, s.shape[1] - centro)
    ancho = 2 * max(medio_ancho(s) for s in sprites.values()) + 4
    return {pose: npc.colocar(s, f"{nombre}_{pose}", por_cuerpo=True, lienzo_px=(ancho, alto_lienzo))
            for pose, s in sprites.items()}, factor


def _comprobar():
    """Las cajas de un enemigo no deben pisarse: la máscara se quedaría con la pose vecina sin dar error."""
    for nombre, g in ENEMIGOS.items():
        cajas = sorted(g["poses"].values(), key=lambda c: (c[1], c[0]))
        for a, b in zip(cajas, cajas[1:]):
            assert a[1] != b[1] or a[2] <= b[0] + 6, f"{nombre}: cajas solapadas {a} {b}"


def main():
    _comprobar()
    carpeta, revision = sys.argv[1], sys.argv[2]
    os.makedirs(carpeta, exist_ok=True)
    img = npc.leer_png(LAMINA)
    escritos, filas = set(), []
    for nombre, g in ENEMIGOS.items():
        sprites, factor = recortar(img, nombre, g)
        for pose, lienzo in sprites.items():
            archivo = f"Enemigo_{nombre}_{pose}1_03.png"
            npc.escribir_png(os.path.join(carpeta, archivo), lienzo)
            escritos.add(archivo)
        filas.append(list(sprites.values()))
        print(nombre, "factor", round(float(factor), 2), "lienzo", sprites["Reposo"].shape[1])
    for sobrante in os.listdir(carpeta):
        base = sobrante[:-5] if sobrante.endswith(".meta") else sobrante
        if base.startswith("Enemigo_") and base.endswith(".png") and base not in escritos:
            os.remove(os.path.join(carpeta, sobrante))
    npc.escribir_png(revision, rg.hoja(filas, zoom=4))
    print("Listo:", len(ENEMIGOS), "enemigos,", len(escritos), "sprites en", carpeta)


if __name__ == "__main__":
    main()
