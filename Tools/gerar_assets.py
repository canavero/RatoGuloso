#!/usr/bin/env python3
"""
Gera TODOS os assets de arte e áudio do jogo "Rato Guloso" de forma procedural
(nenhum asset de terceiros é usado, então não há problema de direitos autorais).

Uso (a partir da raiz do projeto):
    python3 Tools/gerar_assets.py

Requer: Python 3, Pillow e NumPy  (pip install pillow numpy)

Saídas:
    Assets/Art/*.png              sprites do jogo
    Assets/Resources/Audio/*.wav  músicas e efeitos sonoros
    ItchIo/capa.png               imagem de capa para a página do itch.io (630x500)
"""
import math
import os
import random
import sys
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), ".."))
ART = os.path.join(ROOT, "Assets", "Art")
AUDIO = os.path.join(ROOT, "Assets", "Resources", "Audio")
ITCH = os.path.join(ROOT, "ItchIo")
FONTS = os.path.join(ROOT, "Assets", "Fonts")
for d in (ART, AUDIO, ITCH):
    os.makedirs(d, exist_ok=True)

SS = 4  # supersampling para bordas suaves


# =============================================================================
# ARTE
# =============================================================================
class Canvas:
    """Desenha em resolução 4x e reduz no final (antialiasing)."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    def s(self, v):
        return int(round(v * SS))

    def box(self, x0, y0, x1, y1):
        return [self.s(x0), self.s(y0), self.s(x1), self.s(y1)]

    def ellipse(self, cx, cy, rx, ry, fill, outline=None, width=0):
        self.d.ellipse(self.box(cx - rx, cy - ry, cx + rx, cy + ry), fill=fill, outline=outline, width=self.s(width))

    def rect(self, x0, y0, x1, y1, fill, outline=None, width=0, radius=0):
        self.d.rounded_rectangle(self.box(x0, y0, x1, y1), radius=self.s(radius), fill=fill, outline=outline, width=self.s(width))

    def poly(self, pts, fill, outline=None, width=0):
        self.d.polygon([(self.s(x), self.s(y)) for x, y in pts], fill=fill, outline=outline, width=self.s(width) if width else 1)

    def line(self, pts, fill, width):
        self.d.line([(self.s(x), self.s(y)) for x, y in pts], fill=fill, width=self.s(width), joint="curve")

    def arc(self, cx, cy, r, start, end, fill, width):
        self.d.arc(self.box(cx - r, cy - r, cx + r, cy + r), start, end, fill=fill, width=self.s(width))

    def save(self, name):
        out = self.img.resize((self.w, self.h), Image.LANCZOS)
        out.save(os.path.join(ART, name))
        return out


def outline_shadow(img, radius=2, color=(40, 25, 15, 110)):
    """Sombra suave embaixo do sprite (dá profundidade na visão de cima)."""
    a = img.split()[3].filter(ImageFilter.GaussianBlur(radius))
    shadow = Image.new("RGBA", img.size, color[:3] + (0,))
    shadow.putalpha(a.point(lambda v: int(v * color[3] / 255)))
    base = Image.new("RGBA", img.size, (0, 0, 0, 0))
    base.alpha_composite(shadow, (2, 3))
    base.alpha_composite(img)
    return base


def draw_mouse():
    c = Canvas(128, 128)
    body, belly, dark = (168, 168, 182, 255), (205, 205, 215, 255), (70, 70, 85, 255)
    pink = (245, 150, 170, 255)
    # rabo (atrás, à esquerda)
    c.line([(36, 64), (22, 58), (12, 66), (6, 80), (12, 92)], fill=pink, width=4)
    # corpo
    c.ellipse(56, 64, 30, 24, fill=body, outline=dark, width=2)
    c.ellipse(58, 64, 18, 13, fill=belly)
    # orelhas
    for oy in (40, 88):
        c.ellipse(80, oy, 13, 13, fill=body, outline=dark, width=2)
        c.ellipse(80, oy, 8, 8, fill=pink)
    # cabeça
    c.ellipse(92, 64, 20, 17, fill=body, outline=dark, width=2)
    c.poly([(100, 52), (122, 64), (100, 76)], fill=body)
    # bigodes
    for dy in (-6, 0, 6):
        c.line([(112, 64 + dy * 0.4), (126, 64 + dy * 1.6)], fill=dark, width=1)
    # nariz e olhos
    c.ellipse(121, 64, 4, 4, fill=pink, outline=dark, width=1)
    for ey in (56, 72):
        c.ellipse(102, ey, 4, 4, fill=(20, 20, 25, 255))
        c.ellipse(103, ey - 1.5, 1.4, 1.4, fill=(255, 255, 255, 255))
    out = c.img.resize((128, 128), Image.LANCZOS)
    outline_shadow(out).save(os.path.join(ART, "rato.png"))


def draw_cheese(size=128, name="queijo.png"):
    c = Canvas(size, size)
    k = size / 128
    tri = [(14 * k, 24 * k), (118 * k, 64 * k), (14 * k, 104 * k)]
    c.poly(tri, fill=(255, 205, 50, 255), outline=(200, 130, 20, 255), width=3 * k)
    # casca (borda mais escura do lado largo)
    c.poly([(14 * k, 24 * k), (24 * k, 28 * k), (24 * k, 100 * k), (14 * k, 104 * k)], fill=(240, 160, 30, 255))
    for (hx, hy, hr) in ((44, 52, 8), (62, 74, 6), (38, 82, 5), (78, 60, 4), (50, 40, 3.5)):
        c.ellipse(hx * k, hy * k, hr * k, hr * k, fill=(225, 160, 25, 255))
    out = c.img.resize((size, size), Image.LANCZOS)
    outline_shadow(out).save(os.path.join(ART, name))


def draw_clock():
    c = Canvas(128, 128)
    blue, dark = (70, 150, 240, 255), (25, 50, 100, 255)
    # sinos
    for bx in (34, 94):
        c.ellipse(bx, 26, 16, 14, fill=(250, 200, 60, 255), outline=dark, width=2)
    c.line([(40, 108), (30, 120)], fill=dark, width=5)
    c.line([(88, 108), (98, 120)], fill=dark, width=5)
    c.ellipse(64, 68, 46, 46, fill=blue, outline=dark, width=3)
    c.ellipse(64, 68, 36, 36, fill=(250, 250, 245, 255), outline=dark, width=2)
    for i in range(12):
        a = i * math.pi / 6
        r0, r1 = (28, 33) if i % 3 else (25, 33)
        c.line([(64 + r0 * math.cos(a), 68 + r0 * math.sin(a)), (64 + r1 * math.cos(a), 68 + r1 * math.sin(a))], fill=dark, width=2)
    c.line([(64, 68), (64, 44)], fill=dark, width=4)
    c.line([(64, 68), (82, 76)], fill=(220, 50, 50, 255), width=3)
    c.ellipse(64, 68, 4, 4, fill=dark)
    out = c.img.resize((128, 128), Image.LANCZOS)
    outline_shadow(out).save(os.path.join(ART, "relogio.png"))


def draw_cat(name, fur, stripes, eye, outline, inner_ear):
    """Gato visto de cima, olhando para a direita."""
    c = Canvas(128, 128)
    # rabo
    c.line([(30, 64), (14, 54), (8, 38), (16, 26)], fill=fur, width=9)
    c.line([(30, 64), (14, 54), (8, 38), (16, 26)], fill=outline, width=1)
    # corpo
    c.ellipse(52, 64, 32, 26, fill=fur, outline=outline, width=2)
    for sx in (40, 52, 64):
        c.line([(sx, 42), (sx + 4, 52)], fill=stripes, width=4)
        c.line([(sx, 86), (sx + 4, 76)], fill=stripes, width=4)
    # patas
    for px, py in ((74, 40), (74, 88), (34, 42), (34, 86)):
        c.ellipse(px, py, 8, 6, fill=fur, outline=outline, width=2)
    # cabeça e orelhas
    c.poly([(84, 40), (104, 30), (100, 54)], fill=fur, outline=outline, width=2)
    c.poly([(84, 88), (104, 98), (100, 74)], fill=fur, outline=outline, width=2)
    c.poly([(89, 42), (101, 36), (98, 51)], fill=inner_ear)
    c.poly([(89, 86), (101, 92), (98, 77)], fill=inner_ear)
    c.ellipse(96, 64, 24, 24, fill=fur, outline=outline, width=2)
    # olhos
    for ey in (54, 74):
        c.ellipse(106, ey, 7, 6, fill=eye, outline=outline, width=1)
        c.ellipse(108, ey, 2, 5, fill=(15, 15, 15, 255))
    c.poly([(117, 60), (122, 64), (117, 68)], fill=(240, 130, 150, 255))
    for dy in (-5, 5):
        c.line([(114, 64 + dy), (127, 64 + dy * 2.2)], fill=(250, 250, 250, 230), width=1)
    out = c.img.resize((128, 128), Image.LANCZOS)
    outline_shadow(out).save(os.path.join(ART, name))


def draw_trap(closed):
    c = Canvas(128, 128)
    wood, dark, metal = (200, 150, 95, 255), (110, 70, 40, 255), (175, 180, 190, 255)
    c.rect(12, 34, 116, 94, fill=wood, outline=dark, width=3, radius=6)
    for y in (48, 62, 78):
        c.line([(20, y), (108, y + 2)], fill=(180, 130, 80, 255), width=1)
    c.rect(56, 40, 62, 88, fill=metal, outline=(90, 90, 100, 255), width=1)  # mola
    if closed:
        c.line([(60, 38), (102, 38), (102, 90), (60, 90)], fill=metal, width=4)
        c.line([(60, 38), (102, 38), (102, 90), (60, 90)], fill=(90, 90, 100, 255), width=1)
    else:
        c.line([(60, 38), (18, 38), (18, 90), (60, 90)], fill=metal, width=4)
        c.line([(60, 38), (18, 38), (18, 90), (60, 90)], fill=(90, 90, 100, 255), width=1)
        draw_small_cheese(c, 86, 64)
    out = c.img.resize((128, 128), Image.LANCZOS)
    outline_shadow(out).save(os.path.join(ART, "ratoeira_fechada.png" if closed else "ratoeira.png"))


def draw_small_cheese(c, cx, cy):
    c.poly([(cx - 10, cy - 9), (cx + 12, cy), (cx - 10, cy + 9)], fill=(255, 205, 50, 255), outline=(200, 130, 20, 255), width=1)


def draw_floor():
    # 128 px = 1 unidade -> xadrez de 0,5 unidade (alinha com a grade do mapa)
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 255))
    d = ImageDraw.Draw(img)
    light, dark = (246, 238, 222, 255), (226, 212, 188, 255)
    for i in range(2):
        for j in range(2):
            col = light if (i + j) % 2 == 0 else dark
            d.rectangle([i * 64, j * 64, i * 64 + 63, j * 64 + 63], fill=col)
    rnd = random.Random(7)
    px = img.load()
    for _ in range(900):  # textura leve
        x, y = rnd.randrange(128), rnd.randrange(128)
        r, g, b, a = px[x, y]
        k = rnd.randint(-6, 6)
        px[x, y] = (max(0, min(255, r + k)), max(0, min(255, g + k)), max(0, min(255, b + k)), 255)
    for i in range(0, 128, 64):  # rejunte
        d.line([(i, 0), (i, 127)], fill=(205, 190, 165, 255), width=1)
        d.line([(0, i), (127, i)], fill=(205, 190, 165, 255), width=1)
    img.save(os.path.join(ART, "piso.png"))


def draw_counter():
    # 64 px = 0,5 unidade: tampo de madeira do balcão
    img = Image.new("RGBA", (64, 64), (150, 98, 58, 255))
    d = ImageDraw.Draw(img)
    rnd = random.Random(3)
    for y in range(0, 64, 4):
        shade = rnd.randint(-14, 10)
        d.line([(0, y), (63, y + rnd.randint(-1, 1))], fill=(150 + shade, 98 + shade, 58 + shade // 2, 255), width=2)
    for _ in range(3):
        x, y = rnd.randrange(8, 56), rnd.randrange(8, 56)
        d.ellipse([x - 3, y - 2, x + 3, y + 2], outline=(110, 70, 40, 255))
    d.rectangle([0, 0, 63, 63], outline=(115, 72, 40, 255), width=2)
    img.save(os.path.join(ART, "balcao.png"))


def draw_bricks():
    img = Image.new("RGBA", (64, 64), (95, 60, 50, 255))
    d = ImageDraw.Draw(img)
    rnd = random.Random(5)
    for row in range(4):
        y = row * 16
        off = 0 if row % 2 == 0 else 16
        for x in range(-32 + off, 64, 32):
            shade = rnd.randint(-12, 12)
            d.rectangle([x + 1, y + 1, x + 30, y + 14], fill=(150 + shade, 75 + shade // 2, 55 + shade // 2, 255))
    img.save(os.path.join(ART, "parede.png"))


def draw_heart():
    c = Canvas(64, 64)
    red, dark = (235, 60, 70, 255), (120, 20, 30, 255)
    c.ellipse(22, 24, 14, 14, fill=red, outline=dark, width=2)
    c.ellipse(42, 24, 14, 14, fill=red, outline=dark, width=2)
    c.poly([(9, 30), (55, 30), (32, 56)], fill=red, outline=dark, width=2)
    c.rect(12, 20, 52, 34, fill=red)
    c.ellipse(20, 20, 4, 4, fill=(255, 190, 190, 255))
    c.save("coracao.png")


def draw_bolt():
    c = Canvas(64, 64)
    c.poly([(36, 4), (12, 36), (30, 36), (24, 60), (52, 24), (34, 24), (42, 4)], fill=(255, 215, 60, 255), outline=(150, 100, 10, 255), width=2)
    c.save("raio.png")


def draw_alert():
    c = Canvas(64, 64)
    c.ellipse(32, 28, 24, 22, fill=(255, 255, 255, 255), outline=(40, 40, 40, 255), width=2)
    c.poly([(26, 46), (32, 60), (38, 46)], fill=(255, 255, 255, 255))
    c.rect(28, 12, 36, 34, fill=(220, 40, 40, 255), radius=3)
    c.ellipse(32, 41, 4, 4, fill=(220, 40, 40, 255))
    c.save("alerta.png")


def draw_ui_panel():
    # retângulo branco com cantos arredondados: usado como 9-slice (borda 20px) nos botões/painéis
    c = Canvas(64, 64)
    c.rect(0, 0, 63, 63, fill=(255, 255, 255, 255), radius=16)
    c.save("ui_painel.png")


# =============================================================================
# ÁUDIO (síntese simples estilo chiptune)
# =============================================================================
SR = 22050


def note_freq(n):
    """'C4', 'A#3', 'Eb5' -> Hz"""
    names = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
    semi = names[n[0]]
    i = 1
    if n[i] == "#":
        semi += 1; i += 1
    elif n[i] == "b":
        semi -= 1; i += 1
    octave = int(n[i:])
    midi = 12 * (octave + 1) + semi
    return 440.0 * 2 ** ((midi - 69) / 12)


def osc(freq, dur, kind="square", duty=0.5, phase0=0.0):
    n = max(1, int(SR * dur))
    if callable(freq):
        f = freq(np.arange(n) / SR)
        ph = phase0 + np.cumsum(f) / SR
    else:
        ph = phase0 + freq * np.arange(n) / SR
    ph = ph % 1.0
    if kind == "square":
        return np.where(ph < duty, 1.0, -1.0)
    if kind == "triangle":
        return 4 * np.abs(ph - 0.5) - 1
    if kind == "saw":
        return 2 * ph - 1
    return np.sin(2 * np.pi * ph)


def env(n, attack=0.005, decay=0.05, sustain=0.6, release=0.05):
    t = np.arange(n) / SR
    dur = n / SR
    e = np.ones(n) * sustain
    a = t < attack
    e[a] = t[a] / max(attack, 1e-6)
    d = (t >= attack) & (t < attack + decay)
    e[d] = 1 - (1 - sustain) * (t[d] - attack) / max(decay, 1e-6)
    r = t > dur - release
    e[r] *= np.clip((dur - t[r]) / max(release, 1e-6), 0, 1)
    return e


def tone(freq, dur, kind="square", vol=0.5, duty=0.5, **kw):
    s = osc(freq, dur, kind, duty)
    return s * env(len(s), **kw) * vol


def noise(dur, vol=0.5, seed=1):
    rng = np.random.default_rng(seed)
    return rng.uniform(-1, 1, int(SR * dur)) * vol


def lowpass(x, cutoff):
    """filtro passa-baixa de 1 polo (cutoff pode ser um array)."""
    y = np.zeros_like(x)
    cutoff = np.broadcast_to(cutoff, x.shape)
    acc = 0.0
    for i in range(len(x)):
        a = 1 - math.exp(-2 * math.pi * cutoff[i] / SR)
        acc += a * (x[i] - acc)
        y[i] = acc
    return y


def concat(*parts):
    return np.concatenate(parts)


def mix_at(buf, sig, start):
    i = int(start * SR)
    end = min(len(buf), i + len(sig))
    if end > i:
        buf[i:end] += sig[: end - i]


def write_wav(name, x, peak=0.9):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x)) or 1.0
    x = x / m * peak
    data = (x * 32767).astype("<i2").tobytes()
    with wave.open(os.path.join(AUDIO, name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data)


def sfx():
    # coletar queijo: "plim" subindo
    write_wav("coletar.wav", concat(
        tone(note_freq("B5"), 0.06, "square", 0.5, duty=0.25, decay=0.04, sustain=0.5, release=0.01),
        tone(note_freq("E6"), 0.14, "square", 0.5, duty=0.25, decay=0.1, sustain=0.2, release=0.04)), 0.7)

    # relógio: arpejo brilhante
    parts = [tone(note_freq(n), 0.06, "triangle", 0.6, decay=0.03, sustain=0.6, release=0.01) for n in ("C6", "E6", "G6")]
    parts.append(tone(note_freq("C7"), 0.35, "sine", 0.6, decay=0.3, sustain=0.0, release=0.05))
    write_wav("relogio.wav", concat(*parts), 0.7)

    # dano: ruído + queda de tom
    n = noise(0.12, 0.6, 2) * env(int(SR * 0.12), decay=0.1, sustain=0.0)
    sweep = osc(lambda t: 420 * np.exp(-t * 5), 0.32, "square", 0.5) * env(int(SR * 0.32), decay=0.3, sustain=0.0) * 0.6
    buf = np.zeros(int(SR * 0.34)); mix_at(buf, n, 0); mix_at(buf, sweep, 0.0)
    write_wav("dano.wav", buf, 0.85)

    # ratoeira: estalo seco
    click = noise(0.025, 1.0, 3) * env(int(SR * 0.025), attack=0.0005, decay=0.02, sustain=0.0)
    thud = tone(110, 0.12, "sine", 0.9, decay=0.1, sustain=0.0)
    buf = np.zeros(int(SR * 0.14)); mix_at(buf, click, 0); mix_at(buf, thud, 0.005)
    write_wav("ratoeira.wav", buf, 0.85)

    # dash: "fuuu" (ruído filtrado com corte subindo e descendo)
    d = 0.22
    nn = noise(d, 1.0, 4)
    t = np.arange(len(nn)) / SR
    cut = 600 + 3500 * np.sin(np.pi * t / d)
    write_wav("dash.wav", lowpass(nn, cut) * env(len(nn), attack=0.03, decay=0.1, sustain=0.5, release=0.08), 0.6)

    # tick dos últimos segundos
    write_wav("tick.wav", tone(1760, 0.05, "sine", 0.8, attack=0.001, decay=0.04, sustain=0.0), 0.6)

    # clique de UI
    write_wav("clique.wav", tone(880, 0.04, "square", 0.5, duty=0.25, attack=0.001, decay=0.03, sustain=0.0), 0.5)

    # vitória
    seq = [("C5", 0.1), ("E5", 0.1), ("G5", 0.1), ("C6", 0.1), ("G5", 0.1), ("C6", 0.5)]
    parts = [tone(note_freq(nm), du, "square", 0.45, duty=0.25, decay=0.05, sustain=0.6, release=0.03) for nm, du in seq]
    lead = concat(*parts)
    bass = concat(tone(note_freq("C3"), 0.5, "triangle", 0.7, sustain=0.8), tone(note_freq("C4"), 0.5, "triangle", 0.7, sustain=0.8, release=0.2))
    buf = np.zeros(max(len(lead), len(bass))); mix_at(buf, lead, 0); mix_at(buf, bass, 0)
    write_wav("vitoria.wav", buf, 0.8)

    # derrota: "uá-uá-uá-uááá"
    seq = [("G4", 0.22), ("F#4", 0.22), ("F4", 0.22)]
    parts = [tone(note_freq(nm), du, "square", 0.45, duty=0.5, decay=0.05, sustain=0.7, release=0.04) for nm, du in seq]
    last = osc(lambda t: note_freq("E4") * (1 + 0.02 * np.sin(2 * np.pi * 6 * t)), 0.8, "square", 0.5)
    parts.append(last * env(len(last), decay=0.1, sustain=0.7, release=0.3) * 0.45)
    write_wav("derrota.wav", lowpass(concat(*parts), 2500), 0.8)

    # miau do gato preto quando te vê
    d = 0.55
    f = lambda t: 520 + 260 * np.sin(np.pi * np.clip(t / d, 0, 1)) - 140 * (t / d)
    s = osc(f, d, "saw")
    t = np.arange(len(s)) / SR
    cut = 900 + 2600 * np.sin(np.pi * np.clip(t / d * 1.2, 0, 1))
    write_wav("miau.wav", lowpass(s, cut) * env(len(s), attack=0.04, decay=0.1, sustain=0.8, release=0.2), 0.7)


def drum_kick():
    s = osc(lambda t: 150 * np.exp(-t * 25) + 45, 0.16, "sine")
    return s * env(len(s), attack=0.001, decay=0.15, sustain=0.0) * 0.9


def drum_snare():
    n = noise(0.12, 0.7, 9)
    return lowpass(n, 5000) * env(len(n), attack=0.001, decay=0.11, sustain=0.0)


def drum_hat():
    n = noise(0.035, 0.35, 11)
    return (n - lowpass(n, 4000)) * env(len(n), attack=0.001, decay=0.03, sustain=0.0)


def render_song(bpm, bars, chords, melody, lead_kind, bass_pattern, drums, lead_vol=0.32, pad=True):
    beat = 60.0 / bpm
    total = bars * 4 * beat
    buf = np.zeros(int(SR * total) + 1)
    # acordes / baixo (um acorde por compasso)
    for bar in range(bars):
        root, third, fifth = chords[bar % len(chords)]
        t0 = bar * 4 * beat
        for k, step in enumerate(bass_pattern):
            if step is None:
                continue
            nm = {"r": root, "R": root, "f": fifth}[step]
            f = note_freq(nm) / (1 if step != "R" else 0.5)
            mix_at(buf, tone(f, beat * 0.45, "triangle", 0.5, decay=0.05, sustain=0.7, release=0.03), t0 + k * beat / 2)
        if pad:
            for nm in (root, third, fifth):
                f = note_freq(nm) * 2
                mix_at(buf, tone(f, 4 * beat * 0.98, "square", 0.045, duty=0.125, attack=0.05, decay=0.3, sustain=0.6, release=0.2), t0)
    # melodia: lista de (nota ou None, duração em batidas)
    t = 0.0
    for nm, dur in melody:
        if nm is not None:
            mix_at(buf, tone(note_freq(nm), dur * beat * 0.9, lead_kind, lead_vol, duty=0.25, decay=0.08, sustain=0.55, release=0.04), t)
        t += dur * beat
    # bateria
    if drums:
        k, s, h = drum_kick(), drum_snare(), drum_hat()
        for bar in range(bars):
            t0 = bar * 4 * beat
            for i in range(8):
                tt = t0 + i * beat / 2
                if i in (0, 3, 4):
                    mix_at(buf, k, tt)
                if i in (2, 6):
                    mix_at(buf, s, tt)
                mix_at(buf, h, tt)
    return buf[: int(SR * total)]


def music():
    # Menu: tranquila, 96 BPM, C - Am - F - G
    chords = [("C3", "E3", "G3"), ("A2", "C3", "E3"), ("F2", "A2", "C3"), ("G2", "B2", "D3")]
    mel = [
        ("E5", 1), ("G5", 1), ("A5", 1), ("G5", 1),
        ("E5", 1), ("C5", 1), ("D5", 2),
        ("C5", 1), ("A4", 1), ("C5", 1), ("F5", 1),
        ("E5", 2), ("D5", 2),
        ("E5", 1), ("G5", 1), ("C6", 1), ("B5", 1),
        ("A5", 1), ("G5", 1), ("E5", 2),
        ("F5", 1), ("E5", 1), ("D5", 1), ("C5", 1),
        ("D5", 2), ("G4", 2),
    ]
    song = render_song(96, 8, chords, mel, "triangle", ["r", None, "f", None, "R", None, "f", None], drums=False, lead_vol=0.4)
    write_wav("musica_menu.wav", song, 0.6)

    # Jogo: agitada, 148 BPM, Am - F - C - G, 16 compassos
    chords = [("A2", "C3", "E3"), ("F2", "A2", "C3"), ("C3", "E3", "G3"), ("G2", "B2", "D3")]
    a = [("A4", .5), ("C5", .5), ("E5", .5), ("A5", .5), ("G5", .5), ("E5", .5), ("C5", .5), ("E5", .5)]
    b = [("F5", .5), ("E5", .5), ("C5", .5), ("A4", .5), ("C5", 1), (None, .5), ("A4", .5)]
    c_ = [("G5", .5), ("E5", .5), ("G5", .5), ("C6", .5), ("B5", .5), ("G5", .5), ("E5", .5), ("G5", .5)]
    d_ = [("D5", .5), ("B4", .5), ("D5", .5), ("G5", .5), ("F#5", 1), ("D5", 1)]
    e = [("A5", 1), ("G5", .5), ("E5", .5), ("D5", .5), ("E5", .5), ("C5", 1)]
    f_ = [("A4", .5), ("A4", .5), ("C5", .5), ("D5", .5), ("E5", 1), ("C5", 1)]
    g_ = [("E5", .5), ("G5", .5), ("C6", 1), ("B5", .5), ("A5", .5), ("G5", 1)]
    h = [("D5", .5), ("E5", .5), ("D5", .5), ("B4", .5), ("G4", 1), (None, 1)]
    mel = a + b + c_ + d_ + e + f_ + g_ + h
    mel = mel + mel
    song = render_song(148, 16, chords, mel, "square", ["R", "r", "R", "r", "R", "r", "f", "r"], drums=True, lead_vol=0.3)
    write_wav("musica_jogo.wav", song, 0.6)


# =============================================================================
# CAPA DO ITCH.IO
# =============================================================================
def font(size, black=True):
    path = os.path.join(FONTS, "Inter-Black.otf" if black else "Inter-Bold.otf")
    try:
        return ImageFont.truetype(path, size)
    except OSError:
        return ImageFont.load_default()


def cover():
    W, H = 630, 500
    floor = Image.open(os.path.join(ART, "piso.png")).convert("RGBA").resize((96, 96), Image.NEAREST)
    img = Image.new("RGBA", (W, H))
    for x in range(0, W, 96):
        for y in range(0, H, 96):
            img.alpha_composite(floor, (x, y))
    shade = Image.new("RGBA", (W, H), (60, 30, 10, 70))
    img.alpha_composite(shade)
    counter = Image.open(os.path.join(ART, "balcao.png")).convert("RGBA").resize((48, 48))
    for x in range(0, W, 48):
        img.alpha_composite(counter, (x, H - 48))

    def paste(name, size, pos, angle=0, tint=None):
        s = Image.open(os.path.join(ART, name)).convert("RGBA").resize((size, size), Image.LANCZOS)
        if tint:  # mesmo efeito do SpriteRenderer.color da Unity (multiplica a cor)
            r, g, b, a = s.split()
            r = r.point(lambda v: v * tint[0] // 255); g = g.point(lambda v: v * tint[1] // 255); b = b.point(lambda v: v * tint[2] // 255)
            s = Image.merge("RGBA", (r, g, b, a))
        if angle:
            s = s.rotate(angle, resample=Image.BICUBIC, expand=True)
        img.alpha_composite(s, (int(pos[0] - s.width / 2), int(pos[1] - s.height / 2)))

    for i, (x, y, a) in enumerate(((90, 300, 15), (540, 330, -20), (200, 400, 40), (440, 410, -10), (320, 230, 0))):
        paste("queijo.png", 90 if i < 4 else 120, (x, y), a)
    paste("gato.png", 200, (520, 190), 200, tint=(255, 150, 60))
    paste("gato_preto.png", 160, (95, 170), -20)
    paste("rato.png", 210, (320, 330), 20)

    d = ImageDraw.Draw(img)
    title = "RATO GULOSO"
    f = font(78)
    tw = d.textlength(title, font=f)
    for dx, dy in ((4, 6), (0, 0)):
        d.text(((W - tw) / 2 + dx, 28 + dy), title, font=f, fill=(90, 40, 10) if dx else (255, 205, 50),
               stroke_width=6, stroke_fill=(90, 40, 10))
    sub = "A Corrida do Queijo"
    f2 = font(32, black=False)
    sw = d.textlength(sub, font=f2)
    d.text(((W - sw) / 2, 120), sub, font=f2, fill=(255, 255, 255), stroke_width=4, stroke_fill=(90, 40, 10))
    img.convert("RGB").save(os.path.join(ITCH, "capa.png"))


if __name__ == "__main__":
    draw_mouse()
    draw_cheese()
    draw_clock()
    # gato.png é desenhado em tons claros para ser TINGIDO de laranja na Unity (SpriteRenderer.color)
    draw_cat("gato.png", fur=(245, 245, 245, 255), stripes=(200, 200, 200, 255), eye=(255, 255, 120, 255),
             outline=(90, 80, 70, 255), inner_ear=(250, 200, 200, 255))
    draw_cat("gato_preto.png", fur=(45, 45, 55, 255), stripes=(30, 30, 38, 255), eye=(140, 255, 90, 255),
             outline=(15, 15, 20, 255), inner_ear=(120, 70, 80, 255))
    draw_trap(False)
    draw_trap(True)
    draw_floor()
    draw_counter()
    draw_bricks()
    draw_heart()
    draw_bolt()
    draw_alert()
    draw_ui_panel()
    print("sprites ok")
    sfx()
    music()
    print("audio ok")
    cover()
    print("capa ok")
