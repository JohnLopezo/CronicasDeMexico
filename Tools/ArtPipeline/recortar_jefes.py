# Recorta los seis jefes de la lámina "Jefes de Mazmorra" (Docs/Enemigos y Gefes de mazmorra) a sprites de juego.
# Solo usa numpy; se ejecuta con el Python que trae Blender:
#   "<Blender>/5.2/python/bin/python.exe" -I Tools/ArtPipeline/recortar_jefes.py Assets/_Project/Art/Environment/Olmeca/Sprites/Jefes <hoja_revision.png>
# Escribe Jefe_<Clave>_<Pose>1_03.png con las cuatro poses pequeñas de cada panel (la ilustración grande lleva un fondo
# pintado que no se separa bien): Reposo, Avance, Ataque y Especial, más Caida y Desplome generados desde el reposo. Solo de lado; lienzo de 128 de alto con los pies a
# 2 px (el pivote de PixelArtSpriteImporter para Sprites/Jefes). Las cajas terminan encima del pedestal de piedra.
# Son ecos ficticios inspirados en figuras de otras culturas, para la arena de pruebas (Docs/Enemigos/Jefes_Prueba.md).
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # con -I, Python no añade la carpeta del script
import recortar_enemigos as ren  # noqa: E402
import recortar_guerreros as rg  # noqa: E402
import recortar_npc as npc  # noqa: E402

LAMINA = os.path.join(npc.RAIZ, "Docs", "Enemigos y Gefes de mazmorra", "Crónicas de México_ Jefes de Mazmorra.png")
ALTO_LIENZO = 128
POSES = ("Reposo", "Avance", "Ataque", "Especial")
FILA_ALTA, FILA_BAJA = (431, 497), (879, 963)  # debajo del marco del panel, encima del pedestal


def panel(fila, *tramos):
    return dict(zip(POSES, ren.fila(fila, *tramos)))


# alto: px de juego del reposo, cerca del de la lámina (agrandar el pixel art lo emborrona). tol: distancia de color al
# fondo; baja, porque el turquesa y el hueso se parecen al fondo del templo. brillo_pose: poses que son sobre todo un
# efecto luminoso (la columna de agua), separadas por su brillo con ese umbral.
JEFES = {
    "SerpienteEmplumada": {"alto": 66, "tol": 20, "poses": panel(FILA_ALTA, (25, 135), (150, 265), (272, 395), (400, 505))},
    "SenorLluvia": {"alto": 63, "tol": 16, "brillo_pose": {"Especial": 60}, "poses": panel(FILA_ALTA, (538, 642), (648, 762), (758, 892), (903, 1012))},
    "MuroCraneos": {"alto": 66, "tol": 20, "poses": panel(FILA_ALTA, (1028, 1152), (1158, 1277), (1278, 1402), (1403, 1517))},
    "PerroFuego": {"alto": 53, "tol": 20, "poses": panel(FILA_BAJA, (38, 142), (140, 262), (255, 372), (370, 507))},
    "SenorMuertos": {"alto": 84, "tol": 16, "poses": panel(FILA_BAJA, (533, 642), (640, 752), (758, 882), (893, 1012))},
    "GuardianOfrenda": {"alto": 78, "tol": 20, "poses": panel(FILA_BAJA, (1033, 1152), (1143, 1272), (1268, 1392), (1393, 1512))},
}

# Cajas ampliadas donde la general cortaba la figura (las avisa recortar_enemigos.cortes): efectos que suben hasta el
# marco o se salen hacia la pose vecina. La pose vecina no se cuela: la máscara se queda con la figura más grande.
AJUSTES = {
    ("SerpienteEmplumada", "Especial"): (400, 418, 507, 497),
    ("SenorLluvia", "Especial"): (903, 418, 1014, 497),
    ("MuroCraneos", "Ataque"): (1278, 431, 1410, 497),
    ("MuroCraneos", "Especial"): (1403, 418, 1519, 497),
    ("SenorMuertos", "Ataque"): (758, 879, 894, 963),
}
# Siguen tocando el borde, porque la figura llega hasta el marco del panel o la pose vecina, y ampliar mete el marco:
# la cola del perro en reposo, las llamas de su ataque y las llamas moradas del Señor de los Muertos. Es poco y de
# efecto; arreglarlo pide limpiar esas poses a mano en la lámina.
for (jefe, pose), caja in AJUSTES.items():
    JEFES[jefe]["poses"][pose] = caja


def desplome(lienzo, alto, oscurecer):
    """Caída generada desde el reposo (la lámina no trae muerte): el cuerpo se aplasta contra el suelo y se oscurece.
    Sirve igual para un cuerpo humano, la serpiente o el perro. shortcut: rígida; sustituir si llegan poses de caída."""
    ys, xs = np.nonzero(lienzo[..., 3])
    s = lienzo[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    filas = np.linspace(0, s.shape[0] - 1, max(4, round(s.shape[0] * alto))).astype(int)
    caido = s[filas].copy()
    caido[..., :3] = (caido[..., :3] * oscurecer).astype(np.uint8)
    return caido


def _comprobar():
    figura = np.zeros((128, 40, 4), np.uint8)
    figura[46:126, 10:30] = 255  # 80 de alto, pies a 2 px
    assert desplome(figura, 0.4, 0.7).shape[0] == 32, "aplastada al 40 %"
    assert desplome(figura, 0.4, 0.7)[..., 0].max() == 178, "y oscurecida"


def main():
    _comprobar()
    carpeta, revision = sys.argv[1], sys.argv[2]
    os.makedirs(carpeta, exist_ok=True)
    img = npc.leer_png(LAMINA)
    escritos, filas = set(), []
    for nombre, g in JEFES.items():
        sprites, factor = ren.recortar(img, nombre, g, ALTO_LIENZO)
        ancho = sprites["Reposo"].shape[1]
        for pose, alto, oscurecer in (("Caida", 0.7, 0.85), ("Desplome", 0.4, 0.7)):
            sprites[pose] = npc.colocar(desplome(sprites["Reposo"], alto, oscurecer), f"{nombre}_{pose}",
                                        por_cuerpo=True, lienzo_px=(ancho, ALTO_LIENZO))
        for pose, lienzo in sprites.items():
            archivo = f"Jefe_{nombre}_{pose}1_03.png"
            npc.escribir_png(os.path.join(carpeta, archivo), lienzo)
            escritos.add(archivo)
        filas.append(list(sprites.values()))
        print(nombre, "factor", round(float(factor), 2), "lienzo", sprites["Reposo"].shape[1])
    for sobrante in os.listdir(carpeta):
        base = sobrante[:-5] if sobrante.endswith(".meta") else sobrante
        if base.startswith("Jefe_") and base.endswith(".png") and base not in escritos:
            os.remove(os.path.join(carpeta, sobrante))
    npc.escribir_png(revision, rg.hoja(filas, zoom=3))
    print("Listo:", len(JEFES), "jefes,", len(escritos), "sprites en", carpeta)


if __name__ == "__main__":
    main()
