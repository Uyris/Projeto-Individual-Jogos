"""
Gerador procedural dos assets do jogo "Coletor Estelar".

Por enquanto gera as formas básicas (quadrado, círculo e triângulo) usadas
como sprites e o som de coleta (síntese chiptune).

Uso:  python Tools/generate_assets.py
Requer: numpy, Pillow
"""
import os
import wave

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPR = os.path.join(ROOT, "Assets", "Art", "Sprites")
SFX = os.path.join(ROOT, "Assets", "Audio", "SFX")
for d in (SPR, SFX):
    os.makedirs(d, exist_ok=True)


# ---------------------------------------------------------------- sprites
def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def canvas(w, h=None):
    return np.zeros((h or w, w, 4), np.uint8)


def save(img, folder, name):
    Image.fromarray(img, "RGBA").save(os.path.join(folder, name))


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


def make_white(n=8):
    img = canvas(n)
    img[...] = (255, 255, 255, 255)
    return img


def make_circle(n=32):
    img = canvas(n)
    disc(img, n / 2, n / 2, n / 2 - 0.5, (255, 255, 255, 255))
    return img


def make_triangle(n=32):
    img = canvas(n)
    poly(img, [(16, 3), (4, 28), (28, 28)], "#ffffff")
    return img


def build_sprites():
    save(make_white(32), SPR, "square.png")
    save(make_circle(32), SPR, "circle.png")
    save(make_triangle(32), SPR, "triangle.png")


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


def sfx_collect():
    a = note("pulse", 88, 0.06, 0.5, duty=0.25, d=0.02, s=0.8, r=0.01)
    b = note("pulse", 93, 0.12, 0.5, duty=0.25, d=0.05, s=0.6, r=0.06)
    return np.concatenate([a, b])


def build_audio():
    write_wav(os.path.join(SFX, "collect.wav"), sfx_collect())


if __name__ == "__main__":
    build_sprites()
    build_audio()
    print("Assets gerados em", ROOT)
