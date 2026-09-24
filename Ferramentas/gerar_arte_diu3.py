# -*- coding: utf-8 -*-
"""Sprites novos do DIU3 para 'Carta Branca - Ultima Mao'.
Mesma paleta noir fria + carmim/ouro e o mesmo passe de contorno do gerar_arte.py.
Gera: fichas (prata e ouro), Naipe de Paus, Naipe de Ouros, efeitos da Ultima Mao e molduras de UI.
Uso: python Ferramentas/gerar_arte_diu3.py  (escreve direto em Assets/Art)"""
import math, os
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Art")
os.makedirs(OUT, exist_ok=True)

# ---------------------------------------------------------------- paleta (identica ao DIU2)
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
OURO_C   = (236, 204, 128, 255)
MADEIRA  = (74, 56, 50, 255)
MADEIRA_C= (104, 80, 64, 255)
NADA     = (0, 0, 0, 0)

def novo(w, h):
    im = Image.new("RGBA", (w, h), NADA)
    return im, ImageDraw.Draw(im)

def contorno(im, cor=INK):
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

def naipe_paus(d, cx, cy, cor):
    # tres lobos separados: o trevo precisa ser lido mesmo em 24 px
    elipse(d, cx, cy-3, 2, 2, cor)
    elipse(d, cx-3, cy+1, 2, 2, cor)
    elipse(d, cx+3, cy+1, 2, 2, cor)
    d.rectangle([cx-1, cy-1, cx+1, cy+5], fill=cor)
    d.polygon([(cx-2, cy+5), (cx+2, cy+5), (cx, cy+3)], fill=cor)

def naipe_ouros(d, cx, cy, cor, h=6, w=5):
    d.polygon([(cx, cy-h), (cx+w, cy), (cx, cy+h), (cx-w, cy)], fill=cor)

# ---------------------------------------------------------------- FICHAS (12x12) - girando em 4 quadros
def ficha(i, cor, cor_c):
    im, d = novo(12, 12)
    cx = cy = 6
    rx = [5, 4, 1, 4][i]
    if rx <= 1:
        d.rectangle([cx-1, 1, cx+1, 10], fill=cor)
        d.line([(cx, 2), (cx, 9)], fill=PAPEL_SM)
    else:
        elipse(d, cx, cy, rx, 5, cor)
        elipse(d, cx, cy, rx-2, 3, PAPEL)
        elipse(d, cx, cy, max(1, rx-3), 2, cor_c)
        d.point((cx, 1), fill=PAPEL)
        d.point((cx, 10), fill=PAPEL)
        if rx >= 4:
            d.point((cx-rx+1, cy), fill=PAPEL)
            d.point((cx+rx-1, cy), fill=PAPEL)
    return contorno(im)

for i in range(4): salvar(ficha(i, CARMIM, CARMIM_C), "ficha_%d" % i)
for i in range(4): salvar(ficha(i, OURO, OURO_C), "fichaouro_%d" % i)

# ---------------------------------------------------------------- NAIPE DE PAUS (24x24) - fumaca que contorna
def inimigo_paus(i):
    im, d = novo(24, 24)
    cx = 12
    bob = [0, -1, 0, 1][i]
    ond = [0, 1, 0, -1][i]
    # rastro de fumaca
    for k, (ox, oy, r) in enumerate(((0, 15, 5), (ond, 18, 4), (-ond, 20, 3), (ond*2, 22, 1))):
        elipse(d, cx+ox, oy+bob, r, max(1, r-1), AZUL_ESC if k % 2 == 0 else NOITE)
    # corpo
    elipse(d, cx, 13+bob, 6, 4, AZUL)
    d.line([(cx-4, 11+bob), (cx-5, 15+bob)], fill=AZUL_MED)
    # porrete
    ang = [-0.6, -1.0, -0.6, -0.2][i]
    ox, oy = cx+5, 12+bob
    ex, ey = ox+int(6*math.cos(ang)), oy+int(6*math.sin(ang))
    d.line([(ox, oy), (ex, ey)], fill=MADEIRA_C, width=2)
    elipse(d, ex, ey, 2, 2, MADEIRA)
    # cabeca de paus com olhos carmim
    naipe_paus(d, cx, 7+bob, PAPEL_SM)
    d.point((cx-3, 8+bob), fill=CARMIM_C)
    d.point((cx+3, 8+bob), fill=CARMIM_C)
    return contorno(im)

for i in range(4): salvar(inimigo_paus(i), "paus_voa_%d" % i)

# ---------------------------------------------------------------- NAIPE DE OUROS (24x24) - o pote que foge
def inimigo_ouros(i):
    im, d = novo(24, 24)
    cx, cy = 12, 11
    asa = [0, 3, 0, -3][i]
    bob = [0, -1, 0, 1][i]
    cy += bob
    for lado in (-1, 1):
        d.polygon([(cx+lado*4, cy-1), (cx+lado*10, cy-5-asa), (cx+lado*9, cy+1-asa//2),
                   (cx+lado*5, cy+2)], fill=AZUL_MED)
        d.line([(cx+lado*5, cy-1), (cx+lado*9, cy-4-asa)], fill=CINZA, width=1)
    naipe_ouros(d, cx, cy, OURO, 7, 5)
    naipe_ouros(d, cx-1, cy-1, OURO_C, 4, 2)
    # olho e sorriso de quem leva o pote
    d.rectangle([cx, cy-1, cx+2, cy], fill=PAPEL)
    d.point((cx+2, cy-1), fill=INK)
    d.line([(cx-1, cy+3), (cx+2, cy+2)], fill=CARMIM)
    # moedas caindo
    d.point((cx-2+asa//3, cy+9), fill=OURO_C)
    d.point((cx+3, cy+11-bob), fill=OURO)
    return contorno(im)

for i in range(4): salvar(inimigo_ouros(i), "ouros_voa_%d" % i)

# ---------------------------------------------------------------- FX da Ultima Mao e da ficha
im, d = novo(32, 32)
d.ellipse([2, 2, 29, 29], outline=PAPEL, width=2)
d.ellipse([5, 5, 26, 26], outline=CARMIM_C, width=1)
salvar(im, "fx_anel")

im, d = novo(8, 8)
d.line([(4, 0), (4, 7)], fill=OURO_C)
d.line([(0, 4), (7, 4)], fill=OURO_C)
d.rectangle([3, 3, 5, 5], fill=PAPEL)
salvar(im, "fx_brilho")

# ---------------------------------------------------------------- UI (fatiada em 9: borda de 4 px)
im, d = novo(16, 16)
d.rectangle([0, 0, 15, 15], fill=INK)
d.rectangle([1, 1, 14, 14], fill=AZUL_ESC)
d.rectangle([2, 2, 13, 13], fill=CARMIM)
d.rectangle([3, 3, 12, 12], fill=(20, 25, 38, 240))
salvar(im, "ui_painel")

# botao em tons neutros: a cor final vem do tint do Button (normal / selecionado / pressionado)
im, d = novo(16, 16)
d.rectangle([0, 0, 15, 15], fill=INK)
d.rectangle([1, 1, 14, 14], fill=(200, 200, 200, 255))
d.rectangle([1, 1, 14, 2], fill=(255, 255, 255, 255))
d.rectangle([1, 13, 14, 14], fill=(140, 140, 140, 255))
salvar(im, "ui_botao")

print("sprites DIU3 gerados em", os.path.abspath(OUT))
