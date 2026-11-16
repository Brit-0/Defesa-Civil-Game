# -*- coding: utf-8 -*-
"""Props de rua em pixel art, desenhados por script (pedido do Pietro, 16/11).

Gera em Assets/Arts/Houses/Roads/<Nome>/ o <Nome>.png (quadro _S e os quadros de brilho) e o
<Nome>_n.png (normal map), na escala do mapa (32 px por unidade), com a paleta de metal dos props do pack.
Depois, no Unity: Tools/City/Importar Props Pixel (fatia os quadros, pivô na base, liga o normal map).

Uso: python Tools/PixelProps/props_kit.py [--previa caminho.png]
"""
import os
import sys
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Arts', 'Houses', 'Roads')
PAD = 4  # px vazios embaixo de cada quadro (o pivô fica na base visível)

C = {
    'K': (0, 0, 0), 'h': (0xa6, 0xac, 0xd0), 'l': (0x91, 0x95, 0xbb), 'm': (0x61, 0x65, 0x8b), 'd': (0x53, 0x57, 0x78),
    's': (0x45, 0x49, 0x65), 'k': (0x2c, 0x2e, 0x44), 'j': (0x1e, 0x20, 0x32),
    'Y': (0xff, 0xd8, 0x8c), 'W': (0xff, 0xf6, 0xd8), 'O': (0xe9, 0xa8, 0x4c),
    'r': (0x6e, 0x26, 0x2c), 'y': (0x70, 0x58, 0x22), 'g': (0x1f, 0x5a, 0x3c),
    'R': (0xff, 0x46, 0x3c), 'A': (0xff, 0xc8, 0x3a), 'G': (0x4c, 0xf0, 0x7a),
    'F': (0xf2, 0xb5, 0x3a), 'f': (0xb9, 0x80, 0x24),
    'v': (0xc8, 0x2e, 0x2e), 'V': (0x8e, 0x1e, 0x24), 'w': (0xf4, 0xf4, 0xf8), 'b': (0x2a, 0x6a, 0xc8), 'B': (0x1c, 0x48, 0x96),
    'e': (0x2f, 0x8f, 0x5a), 'E': (0x1f, 0x66, 0x44),
}
N_FRONT, N_TOP, N_LEFT, N_RIGHT = (127, 30, 209), (127, 209, 225), (40, 110, 185), (214, 110, 185)
NORMALS = {'T': N_TOP, 'L': N_LEFT, 'R': N_RIGHT, 'F': N_FRONT}


class Art:
    """Quadro com y contado da base para cima; guarda cor e normal por pixel."""

    def __init__(self, w, h):
        self.w, self.h, self.px, self.nm = w, h, {}, {}

    def dot(self, x, y, c, n=N_FRONT):
        if c in '. ':
            return
        self.px[(x, y)] = C[c] + (255,)
        self.nm[(x, y)] = n

    def rows(self, x, ytop, lines, normals=None):
        """Desenha linhas de texto (de cima para baixo) a partir de (x, ytop)."""
        for j, line in enumerate(lines):
            for i, ch in enumerate(line):
                n = NORMALS.get(normals[j][i], N_FRONT) if normals else N_FRONT
                self.dot(x + i, ytop - j, ch, n)

    def pole(self, cx, y0, y1):
        for y in range(y0, y1 + 1):
            self.dot(cx - 1, y, 'l', N_LEFT)
            self.dot(cx, y, 'm')
            self.dot(cx + 1, y, 's', N_RIGHT)

    def base(self, cx):
        """Sapata e pedestal (11 px de altura): o pé do poste, largo e com degrau."""
        self.rows(cx - 5, 1, ['hlllmmmdds', 'ssssssssss'], ['TTTTTTTTTT', 'FFFFFFFFFF'])
        self.rows(cx - 4, 2, ['hllmmmds'], ['TTTTTTTT'])
        for y in range(3, 9):
            self.rows(cx - 3, y, ['hlmmdsk'], ['LLFFFRR'])
        self.rows(cx - 4, 9, ['hllmmdss'], ['TTTTTTTT'])
        self.rows(cx - 2, 10, ['lmmds'], ['TTTTT'])

    def outline(self):
        out = set()
        for (x, y) in self.px:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p = (x + dx, y + dy)
                if p not in self.px:
                    out.add(p)
        for p in out:
            self.px[p] = C['K'] + (255,)
            self.nm[p] = N_FRONT

    def image(self, normal=False):
        im = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        src = self.nm if normal else self.px
        for (x, y), c in src.items():
            yy = self.h - 1 - PAD - y
            if 0 <= x < self.w and 0 <= yy < self.h:
                im.putpixel((x, yy), (c + (255,)) if normal else c)
        return im


def glow(w, h, lamps, halo, radius):
    """Quadro de brilho (somado por cima): o miolo aceso e um halo em 3 degraus."""
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for (cx, cy, pts) in lamps:
        for y in range(-radius, radius + 1):
            for x in range(-radius * 2, radius * 2 + 1):
                d = ((x / 1.25) ** 2 + y ** 2) ** 0.5
                if d > radius + 0.3:
                    continue
                alpha = 150 if d <= radius * 0.42 else (88 if d <= radius * 0.72 else 40)
                yy = h - 1 - PAD - (cy + y)
                xx = int(round(cx + x))
                if 0 <= xx < w and 0 <= yy < h and im.getpixel((xx, yy))[3] < alpha:
                    im.putpixel((xx, yy), C[halo] + (alpha,))
        for (x, y, ch) in pts:
            im.putpixel((x, h - 1 - PAD - y), C[ch] + (255,))
    return im


# luminária tipo "cobra", com o braço saindo para a direita
HEAD_L = ['..hllllll....',
          '.lmmmmmmmdd..',
          'ldmmmmmmmddss',
          '.OYWWWWWYO.ds',
          '..OYYYYYO....']
HEAD_L_N = ['..TTTTTTT....', '.TTTTTTTTTT..', 'LFFFFFFFFFFRR', '.FFFFFFFFF.FR', '..FFFFFFF....']


def mirror_n(rows):
    return [r[::-1].replace('L', 'x').replace('R', 'L').replace('x', 'R') for r in rows]


def poste():
    w, h = 40, 64
    a = Art(w, h)
    cx = 27
    a.base(cx)
    a.pole(cx, 11, 43)
    a.rows(cx - 2, 27, ['hlmds', 'lmdss'], ['TTTTT', 'LFFFR'])  # anel no meio do poste
    # braço em curva até a luminária
    a.rows(cx - 3, 48, ['.lmm.', '.lmds', '..lms', '..lms', '..lms'], ['.TTT.', '.LFFR', '..LFR', '..LFR', '..LFR'])
    a.rows(cx - 15, 50, HEAD_L, HEAD_L_N)
    a.outline()
    lens = [(cx - 15 + i, 47, 'W') for i in range(3, 8)] + [(cx - 15 + i, 46, 'Y') for i in range(3, 8)]
    lens += [(cx - 13, 47, 'Y'), (cx - 7, 47, 'Y')]
    return 'Poste_Novo', a, [('G', glow(w, h, [(cx - 10, 45, lens)], 'Y', 7))], w, h


def poste_duplo():
    w, h = 64, 64
    a = Art(w, h)
    cx = 32
    a.base(cx)
    a.pole(cx, 11, 45)
    a.rows(cx - 2, 27, ['hlmds', 'lmdss'], ['TTTTT', 'LFFFR'])
    a.rows(cx - 3, 49, ['.lmmmd.', 'lmmmmds', '..lms..', '..lms..'], ['.TTTTT.', 'LFFFFFR', '..LFR..', '..LFR..'])
    a.rows(cx - 15, 51, HEAD_L, HEAD_L_N)
    a.rows(cx + 3, 51, [r[::-1] for r in HEAD_L], mirror_n(HEAD_L_N))
    a.dot(cx, 50, 'h', N_TOP)
    a.dot(cx, 51, 'l', N_TOP)
    a.outline()
    lamps = []
    for x0, o, mid in ((cx - 15, 3, cx - 10), (cx + 3, 5, cx + 10)):
        lens = [(x0 + o + i, 48, 'W') for i in range(5)] + [(x0 + o + i, 47, 'Y') for i in range(5)]
        lamps.append((mid, 46, lens))
    return 'Poste_Novo_Duplo', a, [('G', glow(w, h, lamps, 'Y', 7))], w, h


def semaforo():
    w, h = 32, 64
    a = Art(w, h)
    cx = 16
    a.base(cx)
    a.pole(cx, 11, 22)
    box = ['.lllllll.',
           'FkkkkkkkF',
           'Fkkrrrkkf', 'Fkrrrrrkf', 'Fkrrrrrkf', 'Fkrrrrrkf', 'Fkkrrrkkf',
           'Fkkkkkkkf',
           'Fkkyyykkf', 'Fkyyyyykf', 'Fkyyyyykf', 'Fkyyyyykf', 'Fkkyyykkf',
           'Fkkkkkkkf',
           'Fkkgggkkf', 'Fkgggggkf', 'Fkgggggkf', 'Fkgggggkf', 'Fkkgggkkf',
           'fkkkkkkkf',
           '.sssssss.']
    top = 22 + len(box)
    a.rows(cx - 4, top, box, ['T' * 9] + ['LFFFFFFFR'] * (len(box) - 1))
    a.rows(cx - 2, 23, ['lmmds'], ['TTTTT'])
    lamp_tops = {'G2': top - 2, 'G1': top - 8, 'G0': top - 14}  # vermelho em cima, verde embaixo
    for ytop in lamp_tops.values():
        a.dot(cx - 1, ytop - 1, 'd')  # reflexo apagado em cada lente
    a.outline()
    frames = []
    for name, ch in (('G0', 'G'), ('G1', 'A'), ('G2', 'R')):
        ytop = lamp_tops[name]
        pts = []
        for j, line in enumerate(['.xxx.', 'xxxxx', 'xxxxx', 'xxxxx', '.xxx.']):
            for i, c in enumerate(line):
                if c == 'x':
                    pts.append((cx - 2 + i, ytop - j, ch))
        pts.append((cx - 1, ytop - 1, 'W'))
        frames.append((name, glow(w, h, [(cx, ytop - 2, pts)], ch, 6)))
    return 'Semaforo_Novo', a, frames, w, h


def placa_pare():
    w, h = 32, 48
    a = Art(w, h)
    cx = 16
    a.rows(cx - 2, 1, ['lmmds', 'sssss'], ['TTTTT', 'FFFFF'])
    a.pole(cx, 2, 17)
    sign = ['..vvvvv..', '.vvvvvvv.', 'vvvvvvvvv', 'vwwwwwwwv', 'vwwwwwwwv', 'vvvvvvvvv', 'VVVVVVVVV', '.VVVVVVV.', '..VVVVV..']
    a.rows(cx - 4, 17 + len(sign), sign)
    a.outline()
    return 'Placa_Pare_Nova', a, [], w, h


def placa_rua():
    w, h = 32, 48
    a = Art(w, h)
    cx = 13
    a.rows(cx - 2, 1, ['lmmds', 'sssss'], ['TTTTT', 'FFFFF'])
    a.pole(cx, 2, 30)
    a.rows(cx + 2, 29, ['bbbbbbbbbbbb', 'bwwbwwwbwwbb', 'BBBBBBBBBBBB'])
    a.rows(cx - 10, 24, ['eeeeeeeee', 'ewwewwwee', 'EEEEEEEEE'])
    a.dot(cx, 31, 'h', N_TOP)
    a.outline()
    return 'Placa_Rua_Nova', a, [], w, h


def build(preview=None):
    sheets = []
    for fn in (poste, poste_duplo, semaforo, placa_pare, placa_rua):
        name, art, glows, w, h = fn()
        n = 1 + len(glows)
        sheet = Image.new('RGBA', (w * n, h), (0, 0, 0, 0))
        normal = Image.new('RGBA', (w * n, h), (127, 127, 255, 0))
        sheet.paste(art.image(), (0, 0))
        normal.paste(art.image(True), (0, 0))
        for i, (_, g) in enumerate(glows):
            sheet.paste(g, (w * (i + 1), 0))
        d = os.path.join(ROOT, name)
        os.makedirs(d, exist_ok=True)
        sheet.save(os.path.join(d, name + '.png'))
        normal.save(os.path.join(d, name + '_n.png'))
        bb = art.image().getbbox()
        print('%s: quadro %dx%d, visivel %dx%d px (%.2f u de altura), quadros: %s' % (
            name, w, h, bb[2] - bb[0], bb[3] - bb[1], (bb[3] - bb[1]) / 32.0, ', '.join(['S'] + [g[0] for g in glows])))
        sheets.append((sheet, glows, w, h))
    if preview:
        scale = 5
        width = sum(s[0].width for s in sheets) * scale + 10 * len(sheets)
        height = 64 * scale + 10
        out = Image.new('RGBA', (width, height), (0xb9, 0xae, 0x92, 255))
        x = 5
        for sheet, glows, w, h in sheets:
            comp = Image.new('RGBA', sheet.size, (0, 0, 0, 0))
            comp.paste(sheet.crop((0, 0, w, h)), (0, 0))
            for i in range(len(glows)):  # prévia: base + brilho por cima
                base = sheet.crop((0, 0, w, h)).copy()
                base.alpha_composite(sheet.crop((w * (i + 1), 0, w * (i + 2), h)))
                comp.paste(base, (w * (i + 1), 0))
            big = comp.resize((comp.width * scale, comp.height * scale), Image.NEAREST)
            out.paste(big, (x, height - 5 - big.height), big)
            x += big.width + 10
        out.save(preview)


if __name__ == '__main__':
    build(sys.argv[2] if len(sys.argv) > 2 and sys.argv[1] == '--previa' else None)
