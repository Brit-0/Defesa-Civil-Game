# -*- coding: utf-8 -*-
"""Marcadores do mapa em pixel art (pino de chamado, seta de borda, placa de nome e ícones de 12 px).

Uso: python Tools/PixelProps/markers_kit.py [--previa arquivo.png]
Gera os PNGs em Assets/Arts/UI/Markers/; no Unity, Tools/City/Ligar Marcadores importa e liga na cena.
O disco e o recheio são brancos para o MapMarkers tingir pela cor do tipo; contorno preto grosso já vem no desenho.
"""
import math
import os
import sys

from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Arts", "UI", "Markers")
INK = (20, 18, 24, 255)
WHITE = (255, 255, 255, 255)
COLORS = {
    "#": WHITE, "+": (255, 209, 77, 255), "o": (242, 140, 40, 255), "-": INK,
    "b": (122, 74, 34, 255), "g": (208, 208, 208, 255),
}


def disc(size, bands):
    """Disco de bandas concêntricas: bands = [(raio, cor)] de fora para dentro."""
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    c = (size - 1) / 2.0
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - c, y - c)
            for r, col in bands:
                if d <= r:
                    im.putpixel((x, y), col(x, y) if callable(col) else col)
    return im


def shade(x, y):
    """Recheio com luz de cima à esquerda: o tom multiplica a cor do tipo."""
    v = 255 - int(max(0, (x + y) - 22) * 5)
    return (v, v, v, 255)


def outline(im, color=INK):
    w, h = im.size
    src = im.copy()
    for y in range(h):
        for x in range(w):
            if src.getpixel((x, y))[3]:
                continue
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and src.getpixel((nx, ny))[3]:
                        im.putpixel((x, y), color)
    return im


def grid(rows, pad=1):
    h, w = len(rows), max(len(r) for r in rows)
    im = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in COLORS:
                im.putpixel((x + pad, y + pad), COLORS[ch])
    return outline(im)


ICONS = {
    "Mk_Icon_Fire": [
        ".....#......",
        "....##......",
        "....###.....",
        "...####..#..",
        "..#####.##..",
        "..#########.",
        ".####++####.",
        ".###++++###.",
        ".##++oo++##.",
        ".##+oooo+##.",
        "..#+oooo+#..",
        "...######...",
    ],
    "Mk_Icon_Damage": [
        ".....##.....",
        "....####....",
        "...######...",
        "..########..",
        ".####-#####.",
        "####-#######",
        ".###--#####.",
        ".####--####.",
        ".#####-####.",
        ".####-#####.",
        ".###-######.",
        ".##########.",
    ],
    "Mk_Icon_Storm": [
        "......####..",
        ".....####...",
        "....####....",
        "...####.....",
        "..########..",
        "..+######...",
        "....+###....",
        "...+###.....",
        "..+###......",
        "..+##.......",
        "..+#........",
        "..+.........",
    ],
    "Mk_Icon_Done": [
        "............",
        "..........##",
        ".........###",
        "........###.",
        ".......###..",
        "##....###...",
        "###..###....",
        ".######.....",
        "..####......",
        "...##.......",
        "............",
        "............",
    ],
    "Mk_Icon_Guard": [
        "############",
        "#####oo#####",
        "####oooo####",
        "###oooooo###",
        "##oooooooo##",
        "############",
        ".##########.",
        ".##########.",
        "..########..",
        "...######...",
        "....####....",
        ".....##.....",
    ],
    "Mk_Icon_Depot": [
        "############",
        "#b########b#",
        "##b######b##",
        "###b####b###",
        "####b##b####",
        "#####bb#####",
        "#####bb#####",
        "####b##b####",
        "###b####b###",
        "##b######b##",
        "#b########b#",
        "############",
    ],
}


def arrow():
    """Seta apontando para cima, branca com contorno preto de 2 px."""
    rows = [
        "......##......",
        ".....####.....",
        "....######....",
        "...########...",
        "..##########..",
        ".############.",
        "##############",
    ]
    im = Image.new("RGBA", (18, 11), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == "#":
                im.putpixel((x + 2, y + 2), WHITE)
    return outline(outline(im))


def plate():
    """Placa 9-slice (borda de 4 px): fundo escuro, friso claro e contorno preto."""
    im = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    for y in range(12):
        for x in range(12):
            corner = (x in (0, 11) and y in (0, 11))
            if corner:
                continue
            edge = min(x, y, 11 - x, 11 - y)
            near = (x in (1, 10) and y in (1, 10))
            if edge == 0 or near:
                col = INK
            elif edge == 1 or (x in (2, 9) and y in (2, 9)):
                col = WHITE
            else:
                col = (255, 255, 255, 0)
            im.putpixel((x, y), col)
    fill = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    for y in range(12):
        for x in range(12):
            if im.getpixel((x, y))[3] == 0 and 1 < x < 10 and 1 < y < 10:
                fill.putpixel((x, y), (36, 33, 44, 240))
    fill.alpha_composite(im)
    return fill


def build():
    sheets = {
        # contorno preto de 2 px, aro branco de 2 px e miolo escuro (aparece quando o recheio está em progresso)
        "Mk_Disc": disc(26, [(13.0, INK), (11.0, WHITE), (9.0, (36, 33, 44, 255))]),
        "Mk_Fill": disc(26, [(9.0, shade)]),
        "Mk_Ring": disc(32, [(15.6, WHITE), (12.6, (0, 0, 0, 0))]),
        "Mk_Badge": disc(14, [(7.0, INK), (5.6, WHITE), (4.4, (36, 33, 44, 255))]),
        "Mk_Arrow": arrow(),
        "Mk_Plate": plate(),
    }
    for name, rows in ICONS.items():
        sheets[name] = grid(rows)
    os.makedirs(OUT, exist_ok=True)
    for name, im in sheets.items():
        im.save(os.path.join(OUT, name + ".png"))
    return sheets


def preview(sheets, path, scale=6):
    names = list(sheets)
    w = sum(sheets[n].width for n in names) + 4 * (len(names) + 1)
    h = max(sheets[n].height for n in names) + 8
    im = Image.new("RGBA", (w, h), (110, 150, 110, 255))
    x = 4
    for n in names:
        im.alpha_composite(sheets[n], (x, 4))
        x += sheets[n].width + 4
    im.resize((w * scale, h * scale), Image.NEAREST).save(path)


if __name__ == "__main__":
    made = build()
    if "--previa" in sys.argv:
        preview(made, sys.argv[sys.argv.index("--previa") + 1])
    print("marcadores:", ", ".join("%s %dx%d" % (n, i.width, i.height) for n, i in made.items()))
