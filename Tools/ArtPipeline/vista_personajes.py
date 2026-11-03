# Vista previa de los personajes animales (personajes.py) sin rehacer todo el flujo: renderiza las animaciones
# pedidas y una hoja por animación (filas = direcciones N, NE, E, SE, S; columnas = cuadros), ampliada ×3.
# Uso:
#   blender --background --factory-startup --python Tools/ArtPipeline/vista_personajes.py -- <salida>
#       [--corrupto] [--anim Caminar,Reposo] [--dir 3,4]
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixelart_comun as pa  # noqa: E402
import personajes as pj  # noqa: E402

LIENZO = pj.LIENZO
ANCLA = pj.ANCLA
CONTORNO = pj.CONTORNO


def cuadro(salida, tmp, nombre, anim, f, variante, corrupto):
    pa.nueva_escena()
    pj.jaguar(anim, f, variante, corrupto)
    pa.encuadrar(*LIENZO, ANCLA)
    px = pa.reducir(pa.renderizar(os.path.join(tmp, nombre + "_4x.png")))
    rampas = pj.PELAJE["corrupto" if corrupto else "jaguar"]["rampas"]
    px = pa.limpiar(pa.cuantizar(px, pa.paleta(*rampas)))
    pa.contorno(px, CONTORNO)
    if pa.toca_borde(px):
        print("AVISO: toca el borde:", nombre)
    pa.guardar(px, os.path.join(salida, nombre + ".png"))
    return px


def hoja(imgs, columnas, k=3, fondo="#4A473B"):
    filas = [imgs[i:i + columnas] for i in range(0, len(imgs), columnas)]
    h, w = imgs[0].shape[:2]
    sep = 4
    out = np.zeros(((h + sep) * len(filas) + sep, (w + sep) * columnas + sep, 4), dtype=np.float32)
    for r, fila in enumerate(filas):
        for c, img in enumerate(fila):
            y, x = sep + r * (h + sep), sep + c * (w + sep)
            out[y:y + h, x:x + w] = img
    return pa.ampliar(pa.sobre_fondo(out, fondo), k)


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    salida = os.path.abspath(args[0])
    corrupto = "--corrupto" in args
    tabla = pj.ANIMACIONES_CORRUPTO if corrupto else pj.ANIMACIONES_JAGUAR
    anims = args[args.index("--anim") + 1].split(",") if "--anim" in args else list(tabla)
    dirs = [int(d) for d in args[args.index("--dir") + 1].split(",")] if "--dir" in args else [1, 2, 3, 4, 5]
    tmp = os.path.join(salida, "_render")
    os.makedirs(tmp, exist_ok=True)
    prefijo = "JaguarCorrupto" if corrupto else "Jaguar"
    for anim in anims:
        imgs = []
        for v in dirs:
            for f in range(tabla[anim]):
                imgs.append(cuadro(salida, tmp, f"{prefijo}_{anim}{f + 1}_{v:02d}", anim, f, v, corrupto))
        pa.guardar(hoja(imgs, tabla[anim]), os.path.join(salida, f"Hoja_{prefijo}_{anim}.png"))
    print("LISTO", salida)


main()
