"""
Confere o mapa da fase (definido em Assets/Editor/RatoGulosoSetup.cs, entre MAP_BEGIN e MAP_END):
  - todos os queijos/relógios são alcançáveis pelo rato (com e sem passar por ratoeiras)
  - itens não encostam nas paredes
  - patrulhas dos gatos laranja não atravessam paredes
  - a grade de navegação do gato preto é toda conectada
e gera uma imagem de prévia do mapa.

Uso:  python3 Tools/validar_mapa.py [Assets/Editor/RatoGulosoSetup.cs] [ItchIo/mapa_preview.png]
"""
import os, sys, math, re
from collections import deque
from PIL import Image, ImageDraw

def load_map(path):
    txt = open(path, encoding="utf-8").read()
    if path.endswith(".cs"):
        block = re.search(r"MAP_BEGIN(.*?)MAP_END", txt, re.S).group(1)
        rows = re.findall(r'"([^"]+)"', block)
    else:
        rows = [l for l in txt.splitlines() if l.strip()]
    return rows

CELL = 0.5
X0, Y1 = -9.5, 4.5           # canto superior esquerdo do interior
def cell_center(c, r): return (X0 + (c + 0.5) * CELL, Y1 - (r + 0.5) * CELL)

def merge_walls(rows):
    H, W = len(rows), len(rows[0])
    used = [[False]*W for _ in range(H)]
    rects = []
    for r in range(H):
        for c in range(W):
            if rows[r][c] != '#' or used[r][c]: continue
            w = 1
            while c+w < W and rows[r][c+w] == '#' and not used[r][c+w]: w += 1
            h = 1
            while r+h < H and all(rows[r+h][c+k] == '#' and not used[r+h][c+k] for k in range(w)): h += 1
            for rr in range(r, r+h):
                for cc in range(c, c+w): used[rr][cc] = True
            # rect em coordenadas do mundo: xmin, ymin, xmax, ymax
            xmin = X0 + c*CELL; xmax = xmin + w*CELL
            ymax = Y1 - r*CELL; ymin = ymax - h*CELL
            rects.append((xmin, ymin, xmax, ymax))
    return rects

def outer_walls():
    t = 0.5
    return [(-10.0, 4.5, 10.0, 5.0), (-10.0, -6.0, 10.0, -5.5), (-10.0, -5.5, -9.5, 4.5), (9.5, -5.5, 10.0, 4.5)]

def dist_point_rect(p, rc):
    x, y = p; xmin, ymin, xmax, ymax = rc
    dx = max(xmin - x, 0, x - xmax); dy = max(ymin - y, 0, y - ymax)
    return math.hypot(dx, dy)

def parse(rows):
    items = {k: [] for k in "PQKTXx"}
    patrol = {}
    for r, row in enumerate(rows):
        for c, ch in enumerate(row):
            p = cell_center(c, r)
            if ch in items: items[ch].append(p)
            elif ch.isalpha() and ch.lower() in "abcdef":
                patrol.setdefault(ch.lower(), [None, None])[0 if ch.islower() else 1] = p
            elif ch not in ".#":
                raise SystemExit(f"caractere desconhecido {ch!r} em ({c},{r})")
    return items, patrol

def main(path, out_png):
    rows = load_map(path)
    H, W = len(rows), len(rows[0])
    assert H == 20 and all(len(r) == W == 38 for r in rows), (H, [len(r) for r in rows])
    walls = merge_walls(rows) + outer_walls()
    items, patrol = parse(rows)
    ok = True
    def fail(msg):
        nonlocal ok; ok = False; print("ERRO:", msg)
    assert len(items['P']) == 1 and len(items['X']) <= 1
    print(f"paredes internas: {len(merge_walls(rows))} retângulos | queijos {len(items['Q'])} | relógios {len(items['K'])} | ratoeiras {len(items['T'])} | patrulhas {len(patrol)}")

    PLAYER_R, TRAP_HALF = 0.30, (0.31, 0.18)
    # grade fina para alcance do jogador
    step = 0.05
    nx, ny = int(19/step), int(10/step)
    def free(x, y, avoid_traps):
        if any(dist_point_rect((x, y), w) < PLAYER_R + 0.01 for w in walls): return False
        if avoid_traps:
            for tx, ty in items['T']:
                rc = (tx-TRAP_HALF[0], ty-TRAP_HALF[1], tx+TRAP_HALF[0], ty+TRAP_HALF[1])
                if dist_point_rect((x, y), rc) < PLAYER_R + 0.1: return False
        return True
    for avoid in (False, True):
        grid = [[free(X0 + (i+0.5)*step, -5.5 + (j+0.5)*step, avoid) for j in range(ny)] for i in range(nx)]
        def idx(p): return (int((p[0]-X0)/step), int((p[1]+5.5)/step))
        s = idx(items['P'][0]); seen = {s}; dq = deque([s])
        while dq:
            i, j = dq.popleft()
            for di, dj in ((1,0),(-1,0),(0,1),(0,-1)):
                n = (i+di, j+dj)
                if 0 <= n[0] < nx and 0 <= n[1] < ny and n not in seen and grid[n[0]][n[1]]:
                    seen.add(n); dq.append(n)
        for k in "QK":
            for p in items[k]:
                if idx(p) not in seen:
                    fail(f"{k} em {p} inalcançável (evitando ratoeiras={avoid})")
    # itens não podem encostar em paredes
    for k in "QKTPXx":
        for p in items[k]:
            d = min(dist_point_rect(p, w) for w in walls)
            if d < 0.5: fail(f"{k} em {p} muito perto da parede ({d:.2f})")
    # patrulhas: retas, sem paredes no caminho (raio do gato 0.36)
    for key, (a, b) in sorted(patrol.items()):
        if a is None or b is None: fail(f"patrulha {key} incompleta"); continue
        n = 200
        for t in range(n+1):
            p = (a[0] + (b[0]-a[0])*t/n, a[1] + (b[1]-a[1])*t/n)
            if min(dist_point_rect(p, w) for w in walls) < 0.42:
                fail(f"patrulha {key} passa perto de parede em {p}"); break
        for tp in items['T']:
            pass
    # grade de navegação do gato preto (igual ao NavGrid.cs): célula 0.5, folga 0.38
    CLEAR = 0.38
    walk = [[min(dist_point_rect(cell_center(c, r), w) for w in walls) >= CLEAR for c in range(W)] for r in range(H)]
    def cell_of(p): return (int((p[0]-X0)/CELL), int((Y1-p[1])/CELL))
    if items['X']:
        sc = cell_of(items['X'][0])
        if not walk[sc[1]][sc[0]]: fail("gato preto nasce em célula bloqueada")
        seen = {sc}; dq = deque([sc])
        while dq:
            c, r = dq.popleft()
            for dc, dr in ((1,0),(-1,0),(0,1),(0,-1)):
                n = (c+dc, r+dr)
                if 0 <= n[0] < W and 0 <= n[1] < H and n not in seen and walk[n[1]][n[0]]:
                    seen.add(n); dq.append(n)
        for p in items['x']:
            if cell_of(p) not in seen: fail(f"ponto de patrulha do gato preto {p} inalcançável")
        total = sum(sum(r) for r in walk)
        print(f"navegação do gato preto: {len(seen)}/{total} células caminháveis conectadas")
        if len(seen) != total: fail("grade de navegação tem ilhas desconectadas")

    # prévia
    S = 40
    img = Image.new("RGB", (int(20*S), int(11*S)), (40, 30, 25))
    d = ImageDraw.Draw(img)
    def px(x, y): return ((x+10)*S, (5-y)*S)
    d.rectangle([px(-9.5, 4.5), px(9.5, -5.5)], fill=(235, 225, 205))
    for r in range(H):
        for c in range(W):
            if not walk[r][c]:
                x, y = cell_center(c, r)
                d.rectangle([px(x-0.25, y+0.25), px(x+0.25, y-0.25)], fill=(215, 200, 180))
    for xmin, ymin, xmax, ymax in walls:
        d.rectangle([px(xmin, ymax), px(xmax, ymin)], fill=(120, 80, 50), outline=(60, 40, 25))
    for key, (a, b) in patrol.items():
        if a and b:
            d.line([px(*a), px(*b)], fill=(240, 140, 30), width=3)
            d.ellipse([px(a[0]-0.38, a[1]+0.38), px(a[0]+0.38, a[1]-0.38)], fill=(240, 140, 30))
    def dot(p, col, rad):
        d.ellipse([px(p[0]-rad, p[1]+rad), px(p[0]+rad, p[1]-rad)], fill=col)
    for p in items['Q']: dot(p, (250, 210, 40), 0.3)
    for p in items['K']: dot(p, (60, 160, 255), 0.3)
    for p in items['T']: d.rectangle([px(p[0]-0.3, p[1]+0.22), px(p[0]+0.3, p[1]-0.22)], fill=(150, 150, 150))
    for p in items['x']: dot(p, (90, 90, 90), 0.12)
    for p in items['X']: dot(p, (20, 20, 20), 0.4)
    dot(items['P'][0], (200, 40, 40), 0.3)
    img.save(out_png)
    print("OK" if ok else "FALHOU")
    return ok

if __name__ == "__main__":
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
    src = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, "Assets", "Editor", "RatoGulosoSetup.cs")
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(root, "Tools", "mapa_preview.png")
    sys.exit(0 if main(src, out) else 1)
