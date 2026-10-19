"""
Gerador procedural dos assets do jogo "Coletor Estelar".

Por enquanto gera só as formas básicas (quadrado, círculo e triângulo) usadas
como sprites; as cores são trocadas no Unity.

Uso:  python Tools/generate_assets.py
Requer: numpy, Pillow
"""
import os

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPR = os.path.join(ROOT, "Assets", "Art", "Sprites")
os.makedirs(SPR, exist_ok=True)


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


if __name__ == "__main__":
    build_sprites()
    print("Assets gerados em", ROOT)
