# -*- coding: utf-8 -*-
"""Gerador de pixel art para 'Carta Branca - Ultima Mao'.
Paleta noir fria + carmim. Tudo desenhado proceduralmente, sem assets externos."""
import math, os
from PIL import Image, ImageDraw

OUT = "/home/claude/CartaBranca/Assets/Art"
os.makedirs(OUT, exist_ok=True)

# ---------------------------------------------------------------- paleta (14 cores frias + carmim/ouro)
INK      = (11, 13, 20, 255)
NOITE    = (20, 25, 38, 255)
AZUL_ESC = (32, 40, 60, 255)
AZUL     = (48, 60, 88, 255)
AZUL_MED = (66, 82, 118, 255)
AZUL_CLA = (92, 112, 154, 255)
CINZA    = (124, 142, 178, 255)
CINZA_CL = (166, 182, 210, 255)
PAPEL    = (233, 231, 220, 255)
PAPEL_SM = (196, 196, 190, 255)
CARMIM   = (176, 30, 56, 255)
CARMIM_C = (222, 70, 92, 255)
OURO     = (198, 162, 88, 255)
MADEIRA  = (74, 56, 50, 255)
MADEIRA_C= (104, 80, 64, 255)
NADA     = (0, 0, 0, 0)

def novo(w, h):
    im = Image.new("RGBA", (w, h), NADA)
    return im, ImageDraw.Draw(im)

def contorno(im, cor=INK):
    """Adiciona 1px de contorno escuro em volta do que ja foi desenhado."""
    w, h = im.size
    px = im.load()
    alvo = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] != 0:
                continue
            for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                nx, ny = x+dx, y+dy
                if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 200:
                    alvo.append((x, y)); break
    for p in alvo:
        px[p] = cor
    return im

def salvar(im, nome):
    im.save(os.path.join(OUT, nome + ".png"))

def elipse(d, cx, cy, rx, ry, cor):
    d.ellipse([cx-rx, cy-ry, cx+rx, cy+ry], fill=cor)

# ---------------------------------------------------------------- CARTA BRANCA (32x32)
# Baseline dos pes = 30. Cabeca grande (rubber-hose anos 30), chapeu de aba larga.

def perna(d, quadril, pe, cor=AZUL_MED, bota=INK):
    """Perna 'mangueira': curva simples de 2px com bota."""
    qx, qy = quadril; px_, py = pe
    passos = 7
    for i in range(passos + 1):
        t = i / passos
        bx = math.sin(t * math.pi) * (px_ - qx) * 0.35
        x = qx + (px_ - qx) * t + bx * 0.4
        y = qy + (py - qy) * t
        d.ellipse([x-1, y-1, x+1, y+1], fill=cor)
    d.rectangle([px_-2, py-1, px_+2, py+1], fill=bota)

def braco(d, ombro, mao, cor=AZUL_MED):
    ox, oy = ombro; mx, my = mao
    passos = 5
    for i in range(passos + 1):
        t = i / passos
        cx = ox + (mx-ox)*t
        cy = oy + (my-oy)*t + math.sin(t*math.pi) * 1.6
        d.ellipse([cx-1, cy-1, cx+1, cy+1], fill=cor)
    d.ellipse([mx-2, my-2, mx+2, my+2], fill=PAPEL_SM)

def chapeu(d, cx, cy):
    d.ellipse([cx-11, cy-2, cx+11, cy+2], fill=INK)
    d.rectangle([cx-5, cy-8, cx+5, cy], fill=INK)
    d.ellipse([cx-5, cy-10, cx+5, cy-6], fill=INK)
    d.rectangle([cx-5, cy-3, cx+5, cy-1], fill=CARMIM)
    d.point((cx-4, cy-2), fill=CARMIM_C)

def rosto(d, cx, cy, olho_dx=0, piscando=False):
    elipse(d, cx, cy, 6, 6, AZUL_ESC)
    elipse(d, cx, cy, 5, 5, PAPEL)
    d.ellipse([cx+1, cy-3, cx+5, cy+4], fill=PAPEL_SM)
    if piscando:
        d.rectangle([cx-1+olho_dx, cy, cx+1+olho_dx, cy], fill=CARMIM)
    else:
        d.rectangle([cx-1+olho_dx, cy-1, cx+olho_dx, cy+1], fill=CARMIM)
        d.point((cx-1+olho_dx, cy-1), fill=CARMIM_C)

def capa(d, cx, topo, base, larg_topo, larg_base, aba=0):
    """Capa curta: para na cintura para as pernas aparecerem."""
    pts = [(cx-larg_topo, topo), (cx+larg_topo, topo),
           (cx+larg_base+aba, base), (cx+larg_base*0.35+aba, base+1),
           (cx-larg_base*0.35+aba*0.3, base+1), (cx-larg_base+aba*0.3, base)]
    d.polygon(pts, fill=AZUL_ESC)
    d.line([(cx-larg_topo+1, topo+1), (cx-larg_base*0.7+aba*0.3, base-1)], fill=AZUL, width=1)
    d.line([(cx+larg_topo-1, topo+2), (cx+larg_base*0.7+aba, base-1)], fill=NOITE, width=1)

def carta_branca(pose, i):
    im, d = novo(32, 32)
    cx = 16
    if pose == "parado":
        bob = [0, 0, 1, 0][i]
        aba = [0, 1, 1, 0][i]
        perna(d, (13, 21+bob), (12, 30))
        perna(d, (19, 21+bob), (20, 30))
        capa(d, cx, 15+bob, 23, 5, 8, aba)
        braco(d, (11, 17+bob), (8, 22+bob))
        braco(d, (21, 17+bob), (24, 22+bob))
        rosto(d, cx, 11+bob, 0, piscando=(i == 2))
        chapeu(d, cx, 6+bob)
    elif pose == "corre":
        fase = i / 6.0 * math.tau
        bob = 1 if i in (1, 4) else 0
        pe_a = (12 + int(5*math.cos(fase)), 30 - max(0, int(4*math.sin(fase))))
        pe_b = (19 + int(5*math.cos(fase+math.pi)), 30 - max(0, int(4*math.sin(fase+math.pi))))
        perna(d, (14, 21+bob), pe_a)
        perna(d, (18, 21+bob), pe_b)
        capa(d, cx-1, 15+bob, 23, 5, 8, -4)
        braco(d, (11, 17+bob), (8+int(4*math.cos(fase+math.pi)), 21+bob))
        braco(d, (22, 17+bob), (25+int(3*math.cos(fase)), 21+bob))
        rosto(d, cx+1, 11+bob, 1)
        chapeu(d, cx+1, 6+bob)
    elif pose == "pula":
        perna(d, (13, 20), (11, 26))
        perna(d, (19, 20), (21, 25))
        capa(d, cx, 14, 22, 5, 7, -5)
        braco(d, (11, 16), (7, 11))
        braco(d, (22, 16), (26, 12))
        rosto(d, cx, 10, 1)
        chapeu(d, cx, 5)
    elif pose == "cai":
        perna(d, (13, 21), (9, 29))
        perna(d, (19, 21), (23, 28))
        capa(d, cx, 15, 23, 5, 9, -7)
        braco(d, (11, 17), (6, 20))
        braco(d, (22, 17), (27, 19))
        rosto(d, cx, 11, 1)
        chapeu(d, cx, 6)
    elif pose == "saca":
        rec = [0, 2, 1][i]
        perna(d, (13, 21), (11, 30))
        perna(d, (19, 21), (21, 30))
        capa(d, cx-rec, 15, 23, 5, 8, -2)
        braco(d, (11-rec, 17), (8-rec, 21))
        braco(d, (21-rec, 17), (27-rec, 17))
        rosto(d, cx-rec, 11, 1)
        chapeu(d, cx-rec, 6)
        if i >= 1:
            d.ellipse([26-rec, 14, 31-rec, 20], fill=PAPEL)
            d.ellipse([27-rec, 15, 30-rec, 19], fill=CARMIM_C)
    return contorno(im)

for i in range(4):  salvar(carta_branca("parado", i), "cb_parado_%d" % i)
for i in range(6):  salvar(carta_branca("corre", i),  "cb_corre_%d" % i)
salvar(carta_branca("pula", 0), "cb_pula_0")
salvar(carta_branca("cai", 0),  "cb_cai_0")
for i in range(3):  salvar(carta_branca("saca", i),   "cb_saca_%d" % i)

# ---------------------------------------------------------------- INIMIGO ESPADAS (24x24) - anda no chao
def naipe_espadas(d, cx, cy, cor):
    d.polygon([(cx, cy-5), (cx-4, cy+1), (cx+4, cy+1)], fill=cor)
    d.ellipse([cx-4, cy-2, cx, cy+3], fill=cor)
    d.ellipse([cx, cy-2, cx+4, cy+3], fill=cor)
    d.rectangle([cx-1, cy+2, cx+1, cy+5], fill=cor)

def naipe_copas(d, cx, cy, cor):
    d.ellipse([cx-5, cy-4, cx-0, cy+2], fill=cor)
    d.ellipse([cx+0, cy-4, cx+5, cy+2], fill=cor)
    d.polygon([(cx-5, cy), (cx+5, cy), (cx, cy+6)], fill=cor)

def inimigo_espadas(i):
    im, d = novo(24, 24)
    fase = i / 4.0 * math.tau
    bob = 1 if i in (1, 3) else 0
    cx = 12
    # capa esfarrapada
    d.polygon([(cx-6, 8+bob), (cx+6, 8+bob), (cx+8, 19), (cx+4, 18), (cx+1, 20),
               (cx-3, 18), (cx-8, 19)], fill=AZUL)
    d.line([(cx-5, 10+bob), (cx-6, 18)], fill=AZUL_MED)
    # pernas
    for lado, off in ((-1, 0.0), (1, math.pi)):
        px_ = cx + lado*3 + int(3*math.cos(fase+off))
        py = 22 - max(0, int(2*math.sin(fase+off)))
        d.line([(cx+lado*2, 17), (px_, py)], fill=CINZA, width=2)
        d.rectangle([px_-2, py-1, px_+2, py+1], fill=INK)
    # cabeca / mascara de espadas
    elipse(d, cx, 7+bob, 6, 6, AZUL_ESC)
    naipe_espadas(d, cx, 7+bob, PAPEL)
    d.rectangle([cx-1, 6+bob, cx, 7+bob], fill=CARMIM)
    # lamina
    d.line([(cx+6, 12+bob), (cx+10, 8+bob)], fill=CINZA_CL, width=1)
    d.point((cx+10, 8+bob), fill=PAPEL)
    return contorno(im)

for i in range(4): salvar(inimigo_espadas(i), "esp_anda_%d" % i)

# ---------------------------------------------------------------- INIMIGO COPAS (24x24) - voa
def inimigo_copas(i):
    im, d = novo(24, 24)
    cx, cy = 12, 12
    asa = [0, 3, 0, -3][i]
    bob = [0, -1, 0, 1][i]
    cy += bob
    # asas
    for lado in (-1, 1):
        d.polygon([(cx+lado*4, cy-1), (cx+lado*11, cy-4-asa), (cx+lado*10, cy+2-asa//2),
                   (cx+lado*5, cy+3)], fill=AZUL_MED)
        d.line([(cx+lado*5, cy), (cx+lado*10, cy-3-asa)], fill=CINZA, width=1)
    # corpo em forma de copas
    naipe_copas(d, cx, cy, AZUL_ESC)
    naipe_copas(d, cx, cy+1, CARMIM)
    d.ellipse([cx-4, cy-4, cx-1, cy-1], fill=CARMIM_C)
    # olho
    d.rectangle([cx-1, cy-1, cx+1, cy], fill=PAPEL)
    d.point((cx, cy-1), fill=INK)
    # rastro esfarrapado
    d.line([(cx, cy+6), (cx-1+asa//2, cy+9)], fill=AZUL, width=2)
    return contorno(im)

for i in range(4): salvar(inimigo_copas(i), "cop_voa_%d" % i)

# ---------------------------------------------------------------- PROJETIL: carta girando (16x16)
def carta_proj(i):
    im, d = novo(16, 16)
    larg = [6, 4, 2, 4][i]
    d.rectangle([8-larg, 3, 8+larg, 12], fill=PAPEL)
    d.rectangle([8-larg, 3, 8-larg+1, 12], fill=PAPEL_SM)
    if larg >= 4:
        naipe_espadas(d, 8, 8, CARMIM)
    else:
        d.rectangle([8-1, 6, 8+1, 9], fill=CARMIM)
    return contorno(im)

for i in range(4): salvar(carta_proj(i), "carta_%d" % i)

# ---------------------------------------------------------------- FX
im, d = novo(16, 16)
for r, c in ((7, CARMIM), (5, CARMIM_C), (3, PAPEL)):
    elipse(d, 8, 8, r, r, c)
salvar(im, "fx_estouro")

im, d = novo(16, 16)
for r, c in ((7, AZUL), (5, AZUL_MED), (3, CINZA_CL)):
    elipse(d, 8, 8, r, r, c)
salvar(im, "fx_poeira")

im, d = novo(32, 32)
for r in range(15, 0, -1):
    a = int(150 * (1 - r/15.0) ** 2)
    d.ellipse([16-r, 16-r, 16+r, 16+r], fill=(226, 74, 92, a))
salvar(im, "brilho")

# ---------------------------------------------------------------- CENARIO
# chao (16x16, tileavel)
im, d = novo(16, 16)
d.rectangle([0, 0, 15, 15], fill=NOITE)
d.rectangle([0, 0, 15, 2], fill=AZUL)
d.rectangle([0, 0, 15, 0], fill=AZUL_MED)
for x, y in ((2, 5), (9, 7), (5, 11), (13, 9), (7, 14)):
    d.point((x, y), fill=AZUL_ESC)
salvar(im, "tile_chao")

# plataforma de madeira (16x16, tileavel na horizontal)
im, d = novo(16, 8)
d.rectangle([0, 0, 15, 7], fill=MADEIRA)
d.rectangle([0, 0, 15, 1], fill=MADEIRA_C)
d.rectangle([0, 6, 15, 7], fill=INK)
d.line([(3, 3), (3, 5)], fill=MADEIRA_C)
d.line([(11, 3), (11, 5)], fill=MADEIRA_C)
d.line([(7, 2), (7, 5)], fill=(60, 44, 40, 255))
salvar(im, "tile_plataforma")

# ceu (gradiente vertical, esticado)
im, d = novo(8, 64)
for y in range(64):
    t = y / 63.0
    c = tuple(int(a + (b-a)*t) for a, b in zip((16, 20, 34), (58, 50, 74))) + (255,)
    d.line([(0, y), (7, y)], fill=c)
salvar(im, "ceu")

# lua
im, d = novo(32, 32)
elipse(d, 16, 16, 13, 13, (232, 228, 206, 60))
elipse(d, 16, 16, 10, 10, PAPEL)
for cxx, cyy, rr in ((12, 13, 2), (20, 18, 3), (15, 21, 1)):
    elipse(d, cxx, cyy, rr, rr, PAPEL_SM)
salvar(im, "lua")

# silhuetas de mesas/cactos (tileavel horizontal)
def horizonte(nome, cor, alt, semente):
    w, h = 128, 48
    im, d = novo(w, h)
    rnd = semente
    x = 0
    while x < w:
        rnd = (rnd * 1103515245 + 12345) % 2147483648
        larg = 12 + rnd % 26
        rnd = (rnd * 1103515245 + 12345) % 2147483648
        a = alt - 6 + rnd % 14
        d.rectangle([x, h-a, min(w-1, x+larg), h-1], fill=cor)
        d.rectangle([x, h-a, min(w-1, x+larg), h-a], fill=AZUL_ESC)
        x += larg + 3 + rnd % 7
    return im

salvar(horizonte("longe", NOITE, 22, 7), "horizonte_longe")
salvar(horizonte("perto", (14, 18, 28, 255), 30, 91), "horizonte_perto")

# poste / totem de respawn
im, d = novo(16, 32)
d.rectangle([7, 6, 9, 31], fill=MADEIRA)
d.rectangle([3, 4, 13, 12], fill=AZUL_ESC)
naipe_espadas(d, 8, 8, OURO)
salvar(contorno(im), "totem")

# pixel branco 1x1 (utilitario)
im, d = novo(4, 4)
d.rectangle([0, 0, 3, 3], fill=(255, 255, 255, 255))
salvar(im, "pixel")

print("sprites gerados:", len(os.listdir(OUT)))
