# Calcula dónde van las piezas de la lámina «Mazmorras ancestrales» en cada piso (D4) y dibuja el mapa para revisarlo.
# Reglas: en suelo, lejos del vacío y del agua; a 1,8 m de portales, santuarios, ofrendas y enemigos; pinchos en los pasos
# entre salas (con la placa, dos pinchos a sus lados); braseros, esteras, cabezas y cercas junto a los muros; cercas,
# cabezas y vasijas fuera del pasillo central; en el piso 3, solo la antesala (la arena es del Guardián).
# Uso, desde la raíz del proyecto:  python Tools/ArtPipeline/colocar_piezas.py [salida.json]
# El resultado se copia a mano en OlmecaDungeonGenerator.Props; si cambia el trazado de un piso, se recalcula.
import sys
import json, math
PLAN = {  # cuántas de cada pieza por piso (el 1 es el tutorial: menos trampas)
    1: dict(brasero=4, cristal=2, vasija=4, cabeza=1, estera=2, pinchos=2, placa=1, cerca=1),
    2: dict(brasero=4, cristal=3, vasija=5, cabeza=1, estera=2, pinchos=4, placa=1, cerca=2),
    3: dict(brasero=2, cristal=2, vasija=3, cabeza=1, estera=1, pinchos=2, placa=0, cerca=0),
}
salida = {}
for n, cuenta in PLAN.items():
    d = json.load(open(f"Assets/_Project/Data/World/Olmeca/Mazmorra_P{n}.json", encoding="utf-8"))
    vac = {(c["x"], c["y"]) for c in d["vacio"]}
    agua = {(c["x"], c["y"]) for c in d.get("agua", [])} | {(c["x"], c["y"]) for c in d.get("inundacion", [])}
    x0, x1, y0, y1 = d["limites"]
    puntos = []
    for v in d["puntos"].values():
        if isinstance(v, dict) and (v["x"] or v["y"]): puntos.append((v["x"], v["y"]))
        elif isinstance(v, list): puntos += [(p["x"], p["y"]) for p in v]
    muros = [(o["x"], o["y"]) for o in d["objetos"] if o["sprite"].startswith("Muro")]
    otros = [(o["x"], o["y"]) for o in d["objetos"] if not o["sprite"].startswith("Muro")]
    tope_y = 4.4 if n == 3 else 99   # piso 3: solo la antesala; la arena es del Guardián
    def libre(x, y, borde=1):
        if y > tope_y: return False
        for dx in range(-borde, borde + 1):
            for dy in range(-borde, borde + 1):
                c = (x + dx, y + dy)
                if c in vac or c in agua or not (x0 <= c[0] <= x1 and y0 <= c[1] <= y1): return False
        return all(math.dist((x, y), p) >= 1.8 for p in puntos) and all(math.dist((x, y), o) >= 1.0 for o in muros + otros)
    celdas = [(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1) if libre(x, y)]
    junto_muro = [c for c in celdas if min((math.dist(c, m) for m in muros), default=9) <= 1.6]
    # paso entre salas: en las filas del pasillo, donde arriba o abajo hay vacío cerca (estrechamiento)
    pasos = [c for c in celdas if ((c[0], c[1] + 2) in vac or (c[0], c[1] + 3) in vac) and ((c[0], c[1] - 2) in vac or (c[0], c[1] - 3) in vac)]
    puestos = []
    def elegir(lista, k, separa=2.6):
        hechos = []
        for c in sorted(lista, key=lambda c: (c[0] * 7 + c[1] * 3) % 11 + c[0] * 0.01):
            if len(hechos) >= k: break
            if all(math.dist(c, p) >= separa for p in puestos):
                hechos.append(c); puestos.append(c)
        return hechos
    sel = {}
    sel["pinchos"] = elegir(pasos, cuenta["pinchos"], 3.0)
    sel["placa"] = elegir(pasos, cuenta["placa"], 2.2)
    # Cada placa activa dos pinchos a sus lados (arriba y abajo en el pasillo): avisan y dejan pasar.
    sel["pinchos_placa"] = []
    for (px, py) in sel["placa"]:
        for c in ((px, py + 1), (px, py - 1)):
            if c not in vac and c not in agua:
                sel["pinchos_placa"].append(c); puestos.append(c)
    sel["brasero"] = elegir(junto_muro, cuenta["brasero"])
    # Las esteras cuelgan del muro del fondo (más y que la celda); la cabeza y la cerca, junto a cualquier muro.
    fondo = [c for c in junto_muro if any(m[1] > c[1] + 0.5 and abs(m[0] - c[0]) <= 1.2 for m in muros)]
    sel["estera"] = elegir(fondo, cuenta["estera"], 2.0)
    # Fuera del pasillo que une las salas (de la llegada a la bajada): la cerca lo partiría y la cabeza estorbaría.
    llegada = d["puntos"]["llegada_arriba"]["y"]
    fuera = [c for c in junto_muro if abs(c[1] - llegada) > 1.6 and abs(c[1]) > 1.6]
    sel["cabeza"] = elegir(fuera, cuenta["cabeza"], 1.8)
    sel["cerca"] = elegir(fuera, cuenta["cerca"], 1.8)
    sel["cristal"] = elegir(celdas, cuenta["cristal"])
    sel["vasija"] = elegir([c for c in celdas if c[1] != 0], cuenta["vasija"], 2.0)  # no en la línea central del pasillo
    salida[n] = sel
    # mapa
    marca = {"pinchos": "^", "pinchos_placa": "^", "placa": "P", "brasero": "B", "estera": "E", "cabeza": "H", "cerca": "F", "cristal": "*", "vasija": "v"}
    m = {c: marca[k] for k, cs in sel.items() for c in cs}
    print(f"== P{n}: " + ", ".join(f"{k} {len(v)}/{cuenta.get(k, len(v))}" for k, v in sel.items()))
    ys = [y for x, y in vac]
    for y in range(min(max(ys), int(tope_y) + 2 if n == 3 else max(ys)), min(ys) - 1, -1):
        fila = ""
        for x in range(x0, x1 + 1):
            c = (x, y)
            fila += m.get(c) or ("x" if any(math.dist(c, p) < 0.8 for p in puntos) else " " if c in vac else "~" if c in agua else ".")
        if fila.strip(): print(f"{y:4d} {fila}")
if len(sys.argv) > 1:
    json.dump({str(k): {kk: [list(c) for c in vv] for kk, vv in v.items()} for k, v in salida.items()}, open(sys.argv[1], "w"), indent=1)
