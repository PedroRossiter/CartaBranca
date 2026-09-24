# -*- coding: utf-8 -*-
"""Efeitos sonoros e trilha de 'Carta Branca - Ultima Mao', sintetizados do zero.
Mesma filosofia da arte: nada de asset externo, tudo e uma funcao do tempo.
So usa a biblioteca padrao (wave, struct, math, random).
Uso: python Ferramentas/gerar_som.py  (escreve em Assets/Audio)

Os nomes dos arquivos batem com o enum Som do jogo (Som.Tiro -> tiro.wav)."""
import math, os, random, struct, wave

SR = 22050
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Audio")
os.makedirs(OUT, exist_ok=True)
random.seed(7)
TAU = math.tau

def salvar(nome, amostras, vol=0.8):
    pico = max(1e-6, max(abs(a) for a in amostras))
    quadros = bytearray()
    for a in amostras:
        v = max(-1.0, min(1.0, a / pico * vol))
        quadros += struct.pack("<h", int(v * 32767))
    with wave.open(os.path.join(OUT, nome + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(bytes(quadros))

def gerar(dur, fn):
    return [fn(i / SR) for i in range(int(SR * dur))]

def env(t, dur, ataque=0.004, curva=1.6):
    if t < ataque:
        return t / ataque
    return max(0.0, 1.0 - (t - ataque) / max(1e-6, dur - ataque)) ** curva

def quadrada(fase):
    return 1.0 if fase % 1.0 < 0.5 else -1.0

def tri(fase):
    x = fase % 1.0
    return 4.0 * abs(x - 0.5) - 1.0

def varre(f0, f1, t, dur):
    """fase (em ciclos) de uma varredura linear de frequencia f0 -> f1."""
    return f0 * t + (f1 - f0) * t * t / (2.0 * dur)

def ruido():
    return random.uniform(-1.0, 1.0)

def passa_baixa(amostras, a):
    y, out = 0.0, []
    for x in amostras:
        y += a * (x - y)
        out.append(y)
    return out

def sino(f, t, dur):
    return (math.sin(TAU*f*t) + 0.45*math.sin(TAU*f*2.01*t) + 0.2*math.sin(TAU*f*3.02*t)) * math.exp(-t * 6.0 / dur)

def nota(n):  # n = semitons a partir do La 440
    return 440.0 * 2 ** (n / 12.0)

# ------------------------------------------------------------------ efeitos
D = 0.09
salvar("tiro", gerar(D, lambda t: (0.55*quadrada(varre(1700, 500, t, D)) + 0.45*ruido()) * env(t, D)), 0.5)

D = 0.06
salvar("acerto", passa_baixa(gerar(D, lambda t: (ruido()*0.8 + 0.6*quadrada(900*t)) * env(t, D, 0.001, 2.5)), 0.5), 0.55)

D = 0.22
salvar("abate", gerar(D, lambda t: (0.7*quadrada(varre(560, 120, t, D)) + 0.3*ruido()*math.exp(-t*20)) * env(t, D)), 0.6)

def ficha(t):
    D = 0.2
    f = 988.0 if t < 0.06 else 1319.0
    tl = t if t < 0.06 else t - 0.06
    return sino(f, tl, 0.25) * (1.0 if t < D - 0.01 else 0.0)
salvar("ficha", gerar(0.2, ficha), 0.55)

def especial(t):
    D = 0.95
    whoosh = ruido() * math.sin(math.pi * min(1.0, t / 0.45)) * 0.35
    arpejo = 0.0
    for k, n in enumerate((0, 3, 7, 12, 15)):   # La menor subindo: a mao inteira na mesa
        t0 = 0.06 + k * 0.08
        if t >= t0:
            arpejo += tri(nota(n) * (t - t0)) * math.exp(-(t - t0) * 5.0) * 0.4
    return (whoosh + arpejo) * env(t, D, 0.01, 1.0)
salvar("especial", passa_baixa(gerar(0.95, especial), 0.35), 0.75)

def morte(t):
    D = 0.9
    trem = 0.6 + 0.4 * math.sin(TAU * 11 * t)
    return (0.7*quadrada(varre(440, 55, t, D)) * trem + 0.3*ruido()*math.exp(-t*4)) * env(t, D, 0.005, 1.2)
salvar("morte", passa_baixa(gerar(0.9, morte), 0.25), 0.7)

def onda(t):
    return sino(nota(-2 + 12), t, 0.6) + (sino(nota(5 + 12), t - 0.14, 0.6) if t > 0.14 else 0.0)
salvar("onda", gerar(0.7, onda), 0.55)

D = 0.14
salvar("esquiva", passa_baixa(gerar(D, lambda t: ruido() * math.sin(math.pi * t / D)), 0.18), 0.5)

D = 0.09
salvar("pulo", gerar(D, lambda t: math.sin(TAU * varre(300, 720, t, D)) * env(t, D)), 0.35)

D = 0.04
salvar("clique", gerar(D, lambda t: quadrada(1400*t) * env(t, D, 0.001, 3.0)), 0.35)

def cobra(t):
    # "wah-wah" de trombone triste: a casa levou o pote
    notas = ((0.0, 0.28, -5), (0.3, 0.58, -6), (0.6, 1.1, -7))
    s = 0.0
    for t0, t1, n in notas:
        if t0 <= t < t1:
            tl = t - t0
            vib = 1.0 + 0.012 * math.sin(TAU * 6 * tl) * (1.0 if n == -7 else 0.0)
            s = quadrada(nota(n - 12) * vib * tl) * env(tl, t1 - t0, 0.02, 0.8)
    return s
salvar("cobra", passa_baixa(gerar(1.1, cobra), 0.12), 0.6)

def ouros(t):
    s = 0.0
    for k, n in enumerate((7, 11, 14, 19, 14, 19)):
        t0 = k * 0.055
        if t >= t0:
            s += tri(nota(n + 12) * (t - t0)) * math.exp(-(t - t0) * 12.0) * 0.5
    return s
salvar("ouros", gerar(0.5, ouros), 0.5)

D = 0.16
salvar("negado", gerar(D, lambda t: quadrada(110*t) * (1.0 if (t % 0.08) < 0.05 else 0.0) * env(t, D, 0.002, 0.5)), 0.45)

# ------------------------------------------------------------------ trilha: walking bass noir em Re menor
BPM = 100.0
BATIDA = 60.0 / BPM
# 4 compassos de 4 tempos: Dm | Bb | Gm | A7  (graus em semitons a partir do La 440)
BAIXO = [-31, -28, -24, -21,   -35, -31, -28, -24,   -38, -35, -31, -28,   -36, -32, -29, -26]
DUR = BATIDA * len(BAIXO)
N = int(SR * DUR)
musica = [0.0] * N

def somar(t0, amostras, ganho):
    i0 = int(t0 * SR)
    for i, a in enumerate(amostras):
        j = (i0 + i) % N          # da a volta: o loop fecha sem emenda
        musica[j] += a * ganho

for k, n in enumerate(BAIXO):
    f = nota(n)
    dur = BATIDA * 0.95
    somar(k * BATIDA, gerar(dur, lambda t, f=f, dur=dur:
          (math.sin(TAU*f*t) + 0.35*math.sin(TAU*2*f*t) + 0.1*math.sin(TAU*3*f*t)) * env(t, dur, 0.01, 0.9)), 0.55)

# ride com swing (colcheia longa + curta) e escovinha nos tempos 2 e 4
for k in range(len(BAIXO)):
    for sub, ganho in ((0.0, 0.10), (0.66, 0.06)):
        t0 = (k + sub) * BATIDA
        somar(t0, passa_baixa(gerar(0.12, lambda t: ruido() * math.exp(-t * 35)), 0.9), ganho)
    if k % 2 == 1:
        somar(k * BATIDA, passa_baixa(gerar(0.22, lambda t: ruido() * math.sin(math.pi * t / 0.22)), 0.2), 0.12)

# acorde de piano abafado no primeiro tempo de cada compasso
ACORDES = [(-7, -4, 0), (-11, -7, -4), (-14, -11, -7), (-12, -8, -5)]
for c, acorde in enumerate(ACORDES):
    for n in acorde:
        somar(c * 4 * BATIDA + 0.02, gerar(1.4, lambda t, f=nota(n): sino(f, t, 1.6) * 0.5), 0.10)

salvar("musica_mesa", musica, 0.7)

print("sons gerados em", os.path.abspath(OUT), "-", len(os.listdir(OUT)), "arquivos")
