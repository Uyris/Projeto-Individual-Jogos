"""
Gerador procedural dos assets do jogo "Coletor Estelar".

Todos os sprites (pixel art) e sons/músicas (síntese chiptune) do jogo são
gerados por este script, então não há dependência de assets de terceiros.

Uso:  python Tools/generate_assets.py
Requer: numpy, Pillow
"""
import math
import os
import wave

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPR = os.path.join(ROOT, "Assets", "Art", "Sprites")
BG = os.path.join(ROOT, "Assets", "Art", "Backgrounds")
MUS = os.path.join(ROOT, "Assets", "Audio", "Music")
SFX = os.path.join(ROOT, "Assets", "Audio", "SFX")
for d in (SPR, BG, MUS, SFX):
    os.makedirs(d, exist_ok=True)

rng = np.random.default_rng(7)


# ---------------------------------------------------------------- sprites
def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def canvas(w, h=None):
    return np.zeros((h or w, w, 4), np.uint8)


def save(img, folder, name):
    Image.fromarray(img, "RGBA").save(os.path.join(folder, name))


def shaded_ellipse(img, cx, cy, rx, ry, cols, light=(-0.55, -0.65, 0.55), cut=None):
    """Elipse com sombreamento em faixas (estilo pixel art). cols = [escuro, médio, claro, brilho]."""
    lx, ly, lz = light
    n = math.sqrt(lx * lx + ly * ly + lz * lz)
    lx, ly, lz = lx / n, ly / n, lz / n
    h, w = img.shape[:2]
    for y in range(h):
        for x in range(w):
            dx = (x + 0.5 - cx) / rx
            dy = (y + 0.5 - cy) / ry
            d = dx * dx + dy * dy
            if d > 1 or (cut and not cut(x, y)):
                continue
            nz = math.sqrt(max(0.0, 1 - d))
            i = dx * lx + dy * ly + nz * lz
            if i > 0.92:
                c = cols[3]
            elif i > 0.62:
                c = cols[2]
            elif i > 0.25:
                c = cols[1]
            else:
                c = cols[0]
            img[y, x] = hexc(c)


def disc(img, cx, cy, r, color, ry=None):
    ry = ry or r
    h, w = img.shape[:2]
    for y in range(h):
        for x in range(w):
            if ((x + 0.5 - cx) / r) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                img[y, x] = hexc(color) if isinstance(color, str) else color


def rect(img, x0, y0, x1, y1, color):
    img[y0:y1 + 1, x0:x1 + 1] = hexc(color) if isinstance(color, str) else color


def poly(img, pts, color):
    layer = Image.new("RGBA", (img.shape[1], img.shape[0]), (0, 0, 0, 0))
    ImageDraw.Draw(layer).polygon(pts, fill=hexc(color))
    a = np.array(layer)
    m = a[..., 3] > 0
    img[m] = a[m]


def line(img, p0, p1, color, width=1):
    layer = Image.new("RGBA", (img.shape[1], img.shape[0]), (0, 0, 0, 0))
    ImageDraw.Draw(layer).line([p0, p1], fill=hexc(color), width=width)
    a = np.array(layer)
    m = a[..., 3] > 0
    img[m] = a[m]


def outline(img, color="#0d0a14"):
    a = img[..., 3] > 0
    nb = np.zeros_like(a)
    nb[1:, :] |= a[:-1, :]
    nb[:-1, :] |= a[1:, :]
    nb[:, 1:] |= a[:, :-1]
    nb[:, :-1] |= a[:, 1:]
    img[nb & ~a] = hexc(color)
    return img


def px(img, x, y, color):
    img[y, x] = hexc(color) if isinstance(color, str) else color


def make_player():
    img = canvas(32)
    # mochila de oxigênio (atrás, à esquerda)
    rect(img, 4, 10, 9, 21, "#6b7488")
    rect(img, 4, 10, 9, 11, "#9aa3b8")
    rect(img, 5, 14, 8, 17, "#3bd16f")
    # traje vermelho (círculo do tutorial)
    shaded_ellipse(img, 16.5, 16, 11, 11, ["#7a1b24", "#b8303a", "#e2524f", "#ff9a86"])
    # viseira (olhando para a direita = +X)
    shaded_ellipse(img, 21.5, 16, 5.2, 7.2, ["#1d5f8a", "#2f9fd0", "#6fd9ff", "#e8fbff"],
                   light=(-0.3, -0.8, 0.5))
    px(img, 21, 12, "#ffffff")
    px(img, 22, 12, "#ffffff")
    px(img, 21, 13, "#ffffff")
    return outline(img)


def make_crystal(size=32):
    img = canvas(size)
    s = size / 32
    top, bl, br = (16 * s, 3 * s), (4 * s, 28 * s), (28 * s, 28 * s)
    mid = (16 * s, 21 * s)
    poly(img, [top, bl, mid], "#8ff7ff")
    poly(img, [top, mid, br], "#2bb7e3")
    poly(img, [bl, br, mid], "#1677b0")
    line(img, top, mid, "#d9ffff")
    px(img, int(16 * s), int(5 * s), "#ffffff")
    if size >= 32:
        px(img, 15, 7, "#ffffff")
        px(img, 14, 10, "#c7fdff")
    return outline(img, "#06263d")


def make_o2():
    img = canvas(32)
    rect(img, 13, 3, 18, 6, "#5d6577")
    rect(img, 12, 6, 19, 7, "#8a93a8")
    shaded_ellipse(img, 16, 10, 7, 4, ["#9fb0c4", "#cfdbe8", "#eef5fb", "#ffffff"])
    for y in range(10, 27):
        for x in range(9, 23):
            t = (x - 9) / 13
            c = "#9fb0c4" if t > 0.78 else "#cfdbe8" if t > 0.45 else "#eef5fb" if t > 0.15 else "#cfdbe8"
            img[y, x] = hexc(c)
    shaded_ellipse(img, 16, 26, 7, 3, ["#8496ac", "#9fb0c4", "#cfdbe8", "#cfdbe8"])
    rect(img, 9, 15, 22, 21, "#2fbf5f")
    rect(img, 9, 15, 22, 15, "#5fe58a")
    # "O2" em pixels
    for (x, y) in [(11, 17), (11, 18), (11, 19), (12, 16), (13, 16), (14, 17), (14, 18), (14, 19), (12, 20), (13, 20)]:
        px(img, x, y, "#ffffff")
    for (x, y) in [(16, 17), (17, 16), (18, 16), (19, 17), (18, 18), (17, 19), (16, 20), (17, 20), (18, 20), (19, 20)]:
        px(img, x, y, "#ffffff")
    return outline(img)


def make_drone():
    img = canvas(32)
    for (cx, cy) in [(7, 7), (25, 7), (7, 25), (25, 25)]:
        disc(img, cx, cy, 5.5, (60, 70, 90, 170))
        disc(img, cx, cy, 2, "#9aa3b8")
    line(img, (7, 7), (25, 25), "#4a5263", 2)
    line(img, (25, 7), (7, 25), "#4a5263", 2)
    shaded_ellipse(img, 16, 16, 9, 9, ["#3b4252", "#6b7488", "#9aa3b8", "#d4dbe8"])
    shaded_ellipse(img, 16, 16, 4.2, 4.2, ["#8a1010", "#e02424", "#ff6b5b", "#ffd0c8"])
    return outline(img)


def make_alien():
    img = canvas(32)
    for cx in (8, 13, 19, 24):
        disc(img, cx, 25, 3.2, "#2f9e3f")
    shaded_ellipse(img, 16, 16, 12, 10.5, ["#1f6e2c", "#36a845", "#62e06a", "#c4ffb8"])
    # antenas
    line(img, (10, 7), (7, 2), "#36a845", 2)
    line(img, (22, 7), (25, 2), "#36a845", 2)
    disc(img, 6.5, 2.5, 2, "#ff5bd1")
    disc(img, 25.5, 2.5, 2, "#ff5bd1")
    # olho
    disc(img, 16, 15, 6, "#f4f7ff")
    disc(img, 17, 16, 3, "#1b0f2a")
    px(img, 14, 12, "#ffffff")
    px(img, 15, 12, "#ffffff")
    # boca
    line(img, (12, 22), (20, 22), "#123d19", 1)
    return outline(img)


def make_alert():
    img = canvas(8, 16)
    rect(img, 2, 0, 5, 9, "#ffd23f")
    rect(img, 2, 12, 5, 15, "#ffd23f")
    rect(img, 2, 0, 3, 9, "#fff1a8")
    return img


def make_mine():
    img = canvas(32)
    for k in range(8):
        a = k * math.pi / 4
        p0 = (16 + math.cos(a) * 6, 16 + math.sin(a) * 6)
        p1 = (16 + math.cos(a) * 14, 16 + math.sin(a) * 14)
        line(img, p0, p1, "#aab2c4", 3)
        disc(img, p1[0], p1[1], 1.6, "#dfe5f0")
    shaded_ellipse(img, 16, 16, 9, 9, ["#2a1a2e", "#4d3456", "#76557f", "#b896c0"])
    disc(img, 16, 16, 3.2, "#ff3b3b")
    px(img, 15, 15, "#ffd0c8")
    return outline(img)


def make_pod(open_):
    img = canvas(48)
    shaded_ellipse(img, 24, 24, 22, 22, ["#2c3448", "#4c5872", "#7d8bab", "#b9c6e0"])
    ring = ["#1e7a3a", "#2fd36a", "#8dffb0", "#e8fff0"] if open_ else ["#6a1520", "#c22a3a", "#ff6070", "#ffd0d6"]
    shaded_ellipse(img, 24, 24, 17, 17, ring, light=(0, 0, 1))
    shaded_ellipse(img, 24, 24, 13, 13, ["#151b2a", "#222b40", "#2e3954", "#3c4a6b"])
    if open_:
        for k, y in enumerate((14, 21, 28)):
            col = ["#8dffb0", "#5be890", "#2fd36a"][k]
            poly(img, [(24, y), (32, y + 6), (29, y + 6), (24, y + 2), (19, y + 6), (16, y + 6)], col)
    else:
        rect(img, 19, 23, 29, 32, "#c22a3a")
        rect(img, 19, 23, 29, 24, "#ff6070")
        for y in range(16, 24):
            for x in range(20, 29):
                dx, dy = x + 0.5 - 24.5, y + 0.5 - 23
                if 3.2 <= math.hypot(dx, dy) <= 5.2 and dy <= 0.5:
                    px(img, x, y, "#ffb0b8")
        rect(img, 23, 26, 25, 29, "#3c0a12")
    # rebites
    for k in range(8):
        a = k * math.pi / 4 + math.pi / 8
        px(img, int(24 + math.cos(a) * 19.5), int(24 + math.sin(a) * 19.5), "#dfe5f0")
    return outline(img)


def make_emitter():
    img = canvas(16)
    rect(img, 2, 2, 13, 13, "#4c5872")
    rect(img, 2, 2, 13, 3, "#7d8bab")
    rect(img, 2, 12, 13, 13, "#2c3448")
    disc(img, 8, 8, 3.5, "#ff3b3b")
    px(img, 7, 6, "#ffd0c8")
    return outline(img)


def make_floor():
    img = canvas(64)
    base = np.array(hexc("#1b2232"), np.int16)
    for qy in range(2):
        for qx in range(2):
            x0, y0 = qx * 32, qy * 32
            var = rng.integers(-3, 4, size=(32, 32, 1))
            tile = np.clip(base + np.concatenate([var, var, var, np.zeros_like(var)], 2), 0, 255)
            tile[..., 3] = 255
            img[y0:y0 + 32, x0:x0 + 32] = tile
            rect(img, x0, y0, x0 + 31, y0, "#11161f")
            rect(img, x0, y0, x0, y0 + 31, "#11161f")
            rect(img, x0 + 1, y0 + 1, x0 + 30, y0 + 1, "#26304a")
            rect(img, x0 + 1, y0 + 1, x0 + 1, y0 + 30, "#26304a")
            rect(img, x0 + 1, y0 + 31, x0 + 31, y0 + 31, "#151b28")
            rect(img, x0 + 31, y0 + 1, x0 + 31, y0 + 31, "#151b28")
            for (rx, ry) in [(3, 3), (28, 3), (3, 28), (28, 28)]:
                px(img, x0 + rx, y0 + ry, "#3a4a66")
                px(img, x0 + rx + 1, y0 + ry + 1, "#0f141d")
            if (qx, qy) == (1, 0):  # grade de ventilação
                for k in range(6):
                    rect(img, x0 + 8, y0 + 8 + k * 3, x0 + 23, y0 + 8 + k * 3, "#0f141d")
                    rect(img, x0 + 8, y0 + 9 + k * 3, x0 + 23, y0 + 9 + k * 3, "#2a3550")
            if (qx, qy) == (0, 1):  # faixa de sinalização
                for k in range(0, 26, 2):
                    rect(img, x0 + 3 + k, y0 + 14, x0 + 3 + k, y0 + 17, "#3a3420" if (k // 2) % 2 else "#4a4026")
    return img


def make_wall():
    img = canvas(32)
    rect(img, 0, 0, 31, 31, "#46557a")
    rect(img, 0, 0, 31, 3, "#7184b0")
    rect(img, 0, 0, 2, 31, "#5d6f99")
    rect(img, 0, 28, 31, 31, "#2b3550")
    rect(img, 29, 0, 31, 31, "#323d5a")
    rect(img, 6, 9, 25, 22, "#3e4c6e")
    rect(img, 6, 9, 25, 9, "#2b3550")
    rect(img, 6, 22, 25, 22, "#5d6f99")
    for (x, y) in [(5, 5), (26, 5), (5, 26), (26, 26)]:
        px(img, x, y, "#a9b9dc")
        px(img, x + 1, y + 1, "#1c2335")
    return img


def make_white(n=8):
    img = canvas(n)
    img[...] = (255, 255, 255, 255)
    return img


def make_circle(n=32):
    img = canvas(n)
    disc(img, n / 2, n / 2, n / 2 - 0.5, (255, 255, 255, 255))
    return img


def make_spark():
    img = canvas(8)
    rect(img, 3, 0, 4, 7, (255, 255, 255, 255))
    rect(img, 0, 3, 7, 4, (255, 255, 255, 255))
    rect(img, 2, 2, 5, 5, (255, 255, 255, 255))
    return img


def make_space_bg(w=960, h=540, seed=3):
    r = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)

    def noise(scale, octaves=5):
        acc = np.zeros((h, w), np.float32)
        amp, tot = 1.0, 0.0
        for o in range(octaves):
            gs = int(scale * (2 ** o)) + 2
            grid = r.random((gs, gs)).astype(np.float32)
            im = Image.fromarray((grid * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
            acc += np.asarray(im, np.float32) / 255 * amp
            tot += amp
            amp *= 0.5
        return acc / tot

    n1, n2 = noise(3), noise(4)
    base = np.stack([np.full((h, w), 9), np.full((h, w), 10), np.full((h, w), 24)], -1).astype(np.float32)
    neb1 = np.clip((n1 - 0.45) * 2.6, 0, 1)[..., None] * np.array([95, 40, 140], np.float32)
    neb2 = np.clip((n2 - 0.5) * 2.6, 0, 1)[..., None] * np.array([20, 80, 140], np.float32)
    img = base + neb1 + neb2
    for _ in range(900):
        x, y = r.integers(0, w), r.integers(0, h)
        b = r.random() ** 3
        img[y, x] = np.clip(img[y, x] + 120 + 135 * b, 0, 255)
    for _ in range(40):
        x, y = r.integers(2, w - 2), r.integers(2, h - 2)
        c = np.array([255, 245, 220]) if r.random() < 0.5 else np.array([190, 230, 255])
        img[y, x] = c
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            img[y + dy, x + dx] = np.clip(img[y + dy, x + dx] * 0.4 + c * 0.6, 0, 255)
    out = np.concatenate([np.clip(img, 0, 255).astype(np.uint8), np.full((h, w, 1), 255, np.uint8)], -1)
    return out


def build_sprites():
    save(make_player(), SPR, "player.png")
    save(make_crystal(), SPR, "crystal.png")
    save(make_o2(), SPR, "oxygen_tank.png")
    save(make_drone(), SPR, "drone.png")
    save(make_alien(), SPR, "alien.png")
    save(make_alert(), SPR, "alert.png")
    save(make_mine(), SPR, "mine.png")
    save(make_pod(False), SPR, "pod_locked.png")
    save(make_pod(True), SPR, "pod_open.png")
    save(make_emitter(), SPR, "laser_emitter.png")
    save(make_floor(), SPR, "floor.png")
    save(make_wall(), SPR, "wall.png")
    save(make_white(32), SPR, "square.png")
    save(make_circle(32), SPR, "circle.png")
    save(make_spark(), SPR, "spark.png")
    save(make_space_bg(), BG, "space.png")


# ---------------------------------------------------------------- áudio
SR = 22050


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def env(n, a=0.005, d=0.08, s=0.6, r=0.05, length=None):
    length = length or n
    e = np.ones(n, np.float32) * s
    ai, di, ri = int(a * SR), int(d * SR), int(r * SR)
    ai = max(1, min(ai, n))
    e[:ai] = np.linspace(0, 1, ai)
    dd = min(di, n - ai)
    if dd > 0:
        e[ai:ai + dd] = np.linspace(1, s, dd)
    rel = min(ri, n)
    if rel > 0:
        e[n - rel:] *= np.linspace(1, 0, rel)
    return e


def osc(kind, freq, n, duty=0.5, vib=0.0, vib_rate=5.5, phase0=0.0):
    t = np.arange(n) / SR
    f = freq * (1 + vib * np.sin(2 * np.pi * vib_rate * t)) if vib else np.full(n, freq)
    ph = (phase0 + np.cumsum(f) / SR) % 1.0
    if kind == "pulse":
        return np.where(ph < duty, 1.0, -1.0).astype(np.float32)
    if kind == "tri":
        return (4 * np.abs(ph - 0.5) - 1).astype(np.float32)
    if kind == "saw":
        return (2 * ph - 1).astype(np.float32)
    return np.sin(2 * np.pi * ph).astype(np.float32)


def sweep(kind, f0, f1, n, duty=0.5, curve=1.0):
    k = np.linspace(0, 1, n) ** curve
    f = f0 + (f1 - f0) * k
    ph = np.cumsum(f) / SR % 1.0
    if kind == "pulse":
        return np.where(ph < duty, 1.0, -1.0).astype(np.float32)
    if kind == "tri":
        return (4 * np.abs(ph - 0.5) - 1).astype(np.float32)
    return np.sin(2 * np.pi * ph).astype(np.float32)


def noise(n, hp=False):
    x = rng.uniform(-1, 1, n).astype(np.float32)
    if hp:
        x = np.diff(np.concatenate([[0], x])).astype(np.float32) * 0.6
    return x


def lowpass(x, a=0.2):
    y = np.zeros_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += a * (x[i] - acc)
        y[i] = acc
    return y


def mix_into(buf, sig, start):
    end = start + len(sig)
    if end > len(buf):
        sig = sig[: len(buf) - start]
        end = len(buf)
    buf[start:end] += sig


def write_wav(path, sig, peak=0.89):
    m = np.max(np.abs(sig)) or 1
    s = (sig / m * peak * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(s.tobytes())


def note(kind, n_midi, dur, vol, duty=0.5, a=0.005, d=0.08, s=0.6, r=0.04, vib=0.0):
    n = int(dur * SR)
    return osc(kind, midi(n_midi), n, duty, vib) * env(n, a, d, s, r) * vol


def kick(vol=0.9):
    n = int(0.16 * SR)
    return sweep("sine", 150, 42, n, curve=0.4) * np.exp(-np.linspace(0, 7, n)) * vol


def snare(vol=0.5):
    n = int(0.14 * SR)
    return (noise(n) * 0.8 + sweep("tri", 220, 160, n) * 0.4) * np.exp(-np.linspace(0, 9, n)) * vol


def hat(vol=0.18, length=0.04):
    n = int(length * SR)
    return noise(n, hp=True) * np.exp(-np.linspace(0, 6, n)) * vol


CHORDS = {
    "Am": [57, 60, 64], "F": [53, 57, 60], "C": [48, 52, 55], "G": [55, 59, 62],
    "E": [52, 56, 59], "Dm": [50, 53, 57],
}


def game_music():
    bpm = 132
    six = 60 / bpm / 4
    prog = ["Am", "F", "C", "G", "Am", "F", "G", "E"] * 2
    bars = len(prog)
    total = int(bars * 16 * six * SR)
    buf = np.zeros(total + SR * 2, np.float32)
    melody = [
        [(69, 4), (72, 2), (76, 2), (74, 2), (72, 2), (69, 4)],
        [(72, 4), (69, 2), (65, 2), (69, 4), (72, 4)],
        [(76, 4), (79, 2), (76, 2), (74, 2), (72, 2), (67, 4)],
        [(74, 6), (71, 2), (67, 4), (74, 4)],
        [(76, 2), (76, 2), (81, 4), (79, 2), (76, 2), (74, 4)],
        [(72, 2), (74, 2), (76, 4), (77, 4), (76, 4)],
        [(74, 4), (71, 4), (67, 2), (69, 2), (71, 4)],
        [(68, 4), (71, 4), (76, 8)],
    ]
    for b, ch in enumerate(prog):
        bar0 = b * 16 * six
        root = CHORDS[ch][0] - 12
        tones = CHORDS[ch]
        # baixo
        pat = [0, 0, 12, 0, 7, 0, 12, 7]
        for k, off in enumerate(pat):
            st = int((bar0 + k * 2 * six) * SR)
            mix_into(buf, note("pulse", root + off - 12 if off == 0 and k == 0 else root + off, 2 * six * 0.9, 0.30,
                               duty=0.5, d=0.05, s=0.5, r=0.03), st)
        # arpejo
        arp = [tones[0], tones[1], tones[2], tones[0] + 12]
        for k in range(16):
            st = int((bar0 + k * six) * SR)
            mix_into(buf, note("pulse", arp[k % 4] + 12, six * 0.8, 0.075 if b < 8 else 0.095, duty=0.125,
                               d=0.04, s=0.3, r=0.02), st)
        # bateria
        for k in range(16):
            st = int((bar0 + k * six) * SR)
            if k in (0, 8) or (k == 10 and b % 2):
                mix_into(buf, kick(), st)
            if k in (4, 12):
                mix_into(buf, snare(), st)
            if k % 2 == 0:
                mix_into(buf, hat(0.13 if k % 4 else 0.18), st)
            if k == 14:
                mix_into(buf, hat(0.12, 0.12), st)
        if b % 8 == 7:  # virada
            for k in (13, 14, 15):
                mix_into(buf, snare(0.35), int((bar0 + k * six) * SR))
        # melodia
        t = bar0
        for (n_, l) in melody[b % 8]:
            st = int(t * SR)
            dur = l * six
            mix_into(buf, note("pulse", n_, dur * 0.95, 0.16, duty=0.25, a=0.01, d=0.1, s=0.7, r=0.05,
                               vib=0.004 if l >= 4 else 0), st)
            if b >= 8:
                mix_into(buf, note("pulse", n_ - 12, dur * 0.95, 0.07, duty=0.5, a=0.01, d=0.1, s=0.6, r=0.05), st)
            t += dur
    out = buf[:total].copy()
    out[: len(buf) - total] += buf[total:]
    return out


def menu_music():
    bpm = 84
    beat = 60 / bpm
    prog = ["Am", "F", "C", "G", "Am", "F", "Dm", "E"]
    total = int(len(prog) * 4 * beat * SR)
    buf = np.zeros(total + SR * 4, np.float32)
    for b, ch in enumerate(prog):
        bar0 = b * 4 * beat
        tones = CHORDS[ch]
        st = int(bar0 * SR)
        n = int(4 * beat * SR * 1.15)
        for tn in tones:
            sig = (osc("tri", midi(tn), n, vib=0.003, vib_rate=4) * 0.6 +
                   osc("sine", midi(tn + 12), n) * 0.25) * env(n, a=0.6, d=0.5, s=0.8, r=0.9) * 0.16
            mix_into(buf, sig, st)
        mix_into(buf, note("tri", tones[0] - 12, 4 * beat * 0.95, 0.30, a=0.05, d=0.4, s=0.7, r=0.3), st)
        arp = [tones[0] + 12, tones[1] + 12, tones[2] + 12, tones[0] + 24, tones[2] + 12, tones[1] + 12, tones[0] + 12, tones[2]]
        for k in range(8):
            s0 = int((bar0 + k * beat / 2) * SR)
            n2 = int(beat * 1.6 * SR)
            sig = osc("sine", midi(arp[k]), n2) * np.exp(-np.linspace(0, 5, n2)) * 0.11
            sig += osc("sine", midi(arp[k]) * 2.01, n2) * np.exp(-np.linspace(0, 9, n2)) * 0.03
            mix_into(buf, sig, s0)
    out = buf[:total].copy()
    out[: len(buf) - total] += buf[total:]
    return out


def sfx_collect():
    a = note("pulse", 88, 0.06, 0.5, duty=0.25, d=0.02, s=0.8, r=0.01)
    b = note("pulse", 93, 0.12, 0.5, duty=0.25, d=0.05, s=0.6, r=0.06)
    return np.concatenate([a, b])


def sfx_oxygen():
    n = int(0.35 * SR)
    s = sweep("sine", 400, 1100, n, curve=0.7) * (0.6 + 0.4 * np.sin(np.linspace(0, 40, n)))
    return s * env(n, 0.01, 0.1, 0.7, 0.12) * 0.6


def sfx_hurt():
    n = int(0.32 * SR)
    s = sweep("pulse", 420, 90, n, duty=0.5, curve=0.6) * 0.5 + noise(n) * 0.35 * np.exp(-np.linspace(0, 6, n))
    return s * env(n, 0.002, 0.1, 0.6, 0.12)


def sfx_dash():
    n = int(0.22 * SR)
    s = lowpass(noise(n), 0.35) * 1.6 + sweep("sine", 300, 900, n) * 0.15
    return s * np.sin(np.linspace(0, np.pi, n)) * 0.7


def sfx_unlock():
    out = np.zeros(int(0.9 * SR), np.float32)
    for k, n_ in enumerate([72, 76, 79, 84]):
        mix_into(out, note("pulse", n_, 0.35 if k == 3 else 0.12, 0.35, duty=0.25, d=0.05, s=0.6, r=0.1, vib=0.004 if k == 3 else 0), int(k * 0.09 * SR))
    mix_into(out, note("tri", 60, 0.6, 0.35, a=0.01, d=0.2, s=0.6, r=0.2), int(0.27 * SR))
    return out


def sfx_victory():
    out = np.zeros(int(2.2 * SR), np.float32)
    seq = [(72, 0.0, 0.12), (76, 0.12, 0.12), (79, 0.24, 0.12), (84, 0.36, 0.3), (79, 0.7, 0.12), (84, 0.84, 0.9)]
    for n_, t0, d in seq:
        mix_into(out, note("pulse", n_, d, 0.32, duty=0.25, d=0.05, s=0.7, r=0.08, vib=0.005 if d > 0.5 else 0), int(t0 * SR))
        mix_into(out, note("pulse", n_ - 12, d, 0.14, duty=0.5, d=0.05, s=0.6, r=0.08), int(t0 * SR))
    for tn in (48, 52, 55):
        mix_into(out, note("tri", tn, 1.2, 0.2, a=0.02, d=0.3, s=0.7, r=0.4), int(0.84 * SR))
    return out


def sfx_gameover():
    out = np.zeros(int(2.0 * SR), np.float32)
    for k, n_ in enumerate([67, 66, 65, 64]):
        d = 0.9 if k == 3 else 0.28
        mix_into(out, note("pulse", n_, d, 0.32, duty=0.5, d=0.1, s=0.7, r=0.15, vib=0.008 if k == 3 else 0), int(k * 0.3 * SR))
        mix_into(out, note("tri", n_ - 24, d, 0.3, d=0.1, s=0.7, r=0.15), int(k * 0.3 * SR))
    return out


def sfx_click():
    return note("pulse", 84, 0.05, 0.4, duty=0.5, d=0.02, s=0.4, r=0.02)


def sfx_select():
    return note("pulse", 79, 0.035, 0.25, duty=0.25, d=0.01, s=0.4, r=0.015)


def sfx_alarm():
    a = note("pulse", 81, 0.1, 0.35, duty=0.5, d=0.02, s=0.8, r=0.02)
    return np.concatenate([a, np.zeros(int(0.04 * SR), np.float32), note("pulse", 76, 0.1, 0.35, d=0.02, s=0.8, r=0.03)])


def sfx_alert():
    a = note("pulse", 86, 0.05, 0.35, duty=0.25, d=0.01, s=0.8, r=0.01)
    return np.concatenate([a, np.zeros(int(0.03 * SR), np.float32), a])


def build_audio():
    write_wav(os.path.join(MUS, "music_game.wav"), game_music(), 0.8)
    write_wav(os.path.join(MUS, "music_menu.wav"), menu_music(), 0.75)
    write_wav(os.path.join(SFX, "collect.wav"), sfx_collect())
    write_wav(os.path.join(SFX, "oxygen.wav"), sfx_oxygen())
    write_wav(os.path.join(SFX, "hurt.wav"), sfx_hurt())
    write_wav(os.path.join(SFX, "dash.wav"), sfx_dash(), 0.6)
    write_wav(os.path.join(SFX, "unlock.wav"), sfx_unlock())
    write_wav(os.path.join(SFX, "victory.wav"), sfx_victory())
    write_wav(os.path.join(SFX, "gameover.wav"), sfx_gameover())
    write_wav(os.path.join(SFX, "click.wav"), sfx_click(), 0.6)
    write_wav(os.path.join(SFX, "select.wav"), sfx_select(), 0.4)
    write_wav(os.path.join(SFX, "alarm.wav"), sfx_alarm(), 0.6)
    write_wav(os.path.join(SFX, "alert.wav"), sfx_alert(), 0.6)


if __name__ == "__main__":
    build_sprites()
    build_audio()
    print("Assets gerados em", ROOT)
