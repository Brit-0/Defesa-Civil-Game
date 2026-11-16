"""Kit de UI em pixel art do Defesa Civil: cada sprite desenhado por código com uma paleta fixa de 21 cores.

Uso:  python Tools/PixelUI/pixel_kit.py            (grava em Assets/Arts/UI/Pixel, com .meta e o FeelUITheme.asset)
      python Tools/PixelUI/pixel_kit.py --sheet X  (também grava a folha de contato ampliada em X)

Os GUIDs saem do nome do arquivo (md5), então rodar de novo nunca troca a referência de nada.
"""
import hashlib
import math
import os
import sys

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Arts', 'UI', 'Pixel')
THEME_ASSET = os.path.join(ROOT, 'Assets', 'Systems', 'UI', 'Resources', 'FeelUITheme.asset')

# Paleta fixa (21 cores). Letras usadas nos desenhos abaixo.
PALETTE = {
    'K': '141218',  # contorno
    'd': '1F1C25',  # painel escuro
    'p': '29262F',  # painel (cor dos painéis da cena)
    'm': '3A3442',  # painel médio
    'b': '594C63',  # botão (cor do UpgradeButton)
    'l': '7A6A87',  # botão claro
    'h': 'A895B5',  # brilho lilás
    'W': 'F2EBDD',  # creme
    'Y': 'FFD14D',  # dourado (título)
    'y': 'C98A2B',  # dourado escuro
    'n': '7A4A22',  # marrom
    'O': 'F28C28',  # laranja Defesa Civil
    'R': 'D9412F',  # vermelho
    'r': '8A2424',  # vermelho escuro
    'G': '5CC45E',  # verde
    'g': '2E7A3C',  # verde escuro
    'B': '3F7FE0',  # azul
    'u': '24448A',  # azul escuro
    '1': 'FFFFFF',  # rampa de cinza para tingir no Unity (cor por Image.color)
    '2': 'D0D0D0',
    '3': '9A9A9A',
}
RGBA = {k: tuple(int(v[i:i + 2], 16) for i in (0, 2, 4)) + (255,) for k, v in PALETTE.items()}


# ---------- grade ----------

def grid(w, h):
    return [['.'] * w for _ in range(h)]


def from_ascii(text):
    rows = [r for r in text.strip('\n').split('\n')]
    w = max(len(r) for r in rows)
    return [list(r.ljust(w, '.')) for r in rows]


def size(g):
    return len(g[0]), len(g)


def inside(w, h, x, y, k, r):
    """Retângulo recuado k pixels, com o canto cortado em diagonal (r pixels)."""
    x0, y0, x1, y1 = k, k, w - 1 - k, h - 1 - k
    if not (x0 <= x <= x1 and y0 <= y <= y1):
        return False
    return min(x - x0, x1 - x) + min(y - y0, y1 - y) >= r


def rings(g, specs, top=0, bottom=0):
    """specs: lista de (r, luz, base, sombra) do anel de fora para dentro; o último preenche o miolo.
    top/bottom deslocam a forma inteira (botão afundado)."""
    w, h = size(g)
    for y in range(h):
        for x in range(w):
            yy = y - top
            hh = h - top - bottom
            if yy < 0 or yy >= hh:
                continue
            for k, (r, light, base, shadow) in enumerate(specs):
                last = k == len(specs) - 1
                if not inside(w, hh, x, yy, k, r):
                    break
                if last or not inside(w, hh, x, yy, k + 1, specs[k + 1][0]):
                    if x == k or yy == k:
                        c = light
                    elif x == w - 1 - k or yy == hh - 1 - k:
                        c = shadow
                    else:
                        c = base
                    g[y][x] = c
                    break


def outline(g, ch='K', diagonal=False):
    w, h = size(g)
    add = []
    for y in range(h):
        for x in range(w):
            if g[y][x] != '.':
                continue
            n = [(1, 0), (-1, 0), (0, 1), (0, -1)]
            if diagonal:
                n += [(1, 1), (-1, -1), (1, -1), (-1, 1)]
            if any(0 <= x + dx < w and 0 <= y + dy < h and g[y + dy][x + dx] not in '.K' for dx, dy in n):
                add.append((x, y))
    for x, y in add:
        g[y][x] = ch
    return g


def shade(mask, base, light, dark):
    """Pinta uma máscara (True/False) com luz em cima e à esquerda e sombra embaixo e à direita."""
    h, w = len(mask), len(mask[0])
    g = grid(w, h)
    for y in range(h):
        for x in range(w):
            if not mask[y][x]:
                continue
            up = y > 0 and mask[y - 1][x]
            left = x > 0 and mask[y][x - 1]
            down = y < h - 1 and mask[y + 1][x]
            right = x < w - 1 and mask[y][x + 1]
            if not down or not right:
                g[y][x] = dark
            elif not up or not left:
                g[y][x] = light
            else:
                g[y][x] = base
    return g


def mask_from(fn, w, h):
    return [[fn(x + .5, y + .5) for x in range(w)] for y in range(h)]


def poly(points):
    def test(px, py):
        hit = False
        j = len(points) - 1
        for i in range(len(points)):
            xi, yi = points[i]
            xj, yj = points[j]
            if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
                hit = not hit
            j = i
        return hit
    return test


def disk(cx, cy, r):
    return lambda x, y: (x - cx) ** 2 + (y - cy) ** 2 <= r * r


def put(g, x, y, c):
    w, h = size(g)
    if 0 <= x < w and 0 <= y < h:
        g[y][x] = c


def flip_v(mask):
    return [row[:] for row in reversed(mask)]


def scale(g, s):
    return [[c for c in row for _ in range(s)] for row in g for _ in range(s)]


# ---------- painéis (9-slice) ----------

def window():
    g = grid(16, 16)
    rings(g, [(2, 'K', 'K', 'K'), (1, 'l', 'b', 'm'), (0, 'b', 'b', 'b'), (0, 'b', 'b', 'b'),
              (1, 'K', 'K', 'K'), (0, 'd', 'p', 'p'), (0, 'p', 'p', 'p')])
    for x, y in ((2, 2), (13, 2), (2, 13), (13, 13)):  # rebites
        put(g, x, y, 'h')
        put(g, x + 1, y + 1, 'd')
    return g, (6, 6, 6, 6)


def inset():
    g = grid(6, 6)
    rings(g, [(1, 'K', 'd', 'm'), (0, 'd', 'd', 'd')])
    return g, (2, 2, 2, 2)


def gold_frame():
    g = grid(14, 14)
    rings(g, [(2, 'K', 'K', 'K'), (1, 'W', 'Y', 'y'), (0, 'Y', 'Y', 'y'), (1, 'K', 'K', 'K'), (0, 'd', 'p', 'p'),
              (0, 'p', 'p', 'p')])
    return g, (5, 5, 5, 5)


def tint_frame():
    g = grid(12, 12)
    rings(g, [(2, 'K', 'K', 'K'), (1, '1', '2', '3'), (0, '2', '2', '3'), (1, 'K', 'K', 'K'), (0, '.', '.', '.')])
    for y in range(12):
        for x in range(12):
            if g[y][x] == '.' and inside(12, 12, x, y, 4, 0):
                g[y][x] = '.'
    return g, (4, 4, 4, 4)


def ribbon():
    """Faixa do título da tela de fim: rabo de andorinha nas pontas. Altura exata de 40 px (120 unidades em 1080p),
    para o V nunca esticar na vertical; só o miolo estica na horizontal."""
    w, h = 48, 40
    g = grid(w, h)
    c = (h - 1) / 2
    for y in range(h):
        notch = max(0, int(10 - abs(y - c)))
        for x in range(w):
            if notch <= x <= w - 1 - notch:
                g[y][x] = '2'
    for x in range(w):
        for y in range(h):
            if g[y][x] == '.':
                continue
            if y <= 2 or g[y - 1][x] == '.':
                g[y][x] = '1'
            elif y >= h - 4:
                g[y][x] = '3'
    for y in range(3, h - 4):  # dobras do pano
        for x in (12, 13, w - 14, w - 13):
            if g[y][x] != '.':
                g[y][x] = '3' if x in (12, w - 13) else '1'
    out = [row[:] for row in g]
    for y in range(h):
        for x in range(w):
            if g[y][x] == '.':
                continue
            n = [(1, 0), (-1, 0), (0, 1), (0, -1)]
            if any(not (0 <= x + dx < w and 0 <= y + dy < h) or g[y + dy][x + dx] == '.' for dx, dy in n):
                out[y][x] = 'K'
    return out, (16, 0, 16, 0)


def bar_frame():
    g = grid(10, 10)
    rings(g, [(1, 'K', 'K', 'K'), (0, 'l', 'b', 'm'), (0, 'K', 'K', 'K'), (0, 'd', 'd', 'd')])
    return g, (3, 3, 3, 3)


def bar_fill():
    g = grid(6, 6)
    for y in range(6):
        for x in range(6):
            g[y][x] = '1' if y == 0 else ('3' if y == 5 else '2')
        g[y][5] = '1' if y < 5 else '2'  # brilho na ponta que avança
    return g, (0, 1, 1, 1)


# ---------- botões (9-slice, quatro estados) ----------

def button(face, light, lip, sparkle, pressed=False, inner=None):
    g = grid(16, 16)
    if pressed:
        rings(g, [(2, 'K', 'K', 'K'), (1, 'd', face, face), (0, face, face, face)], top=2)
        return g
    rings(g, [(2, 'K', 'K', 'K'), (1, light, face, face), (0, face, face, face)])
    for y in (13, 14):  # lábio de baixo: o botão tem altura
        for x in range(16):
            if g[y][x] not in '.K':
                g[y][x] = lip
    if inner:
        for y in range(2, 13):
            for x in range(16):
                if g[y][x] not in '.K' and (x in (1, 14) or y == 12):
                    g[y][x] = inner
    put(g, 2, 2, sparkle)
    return g


def button_mask():
    g = button('1', '1', '1', '1')
    return [[c if c == '.' else '1' for c in row] for row in g]


BUTTON_BORDER = (5, 6, 5, 5)  # esquerda, baixo, direita, cima (ordem do .meta: x, y, z, w)


# ---------- botões redondos (inteiros) ----------

def round_button(face, light, lip, pressed=False):
    n = 24
    g = grid(n, n)
    c = (n - 1) / 2
    for y in range(n):
        for x in range(n):
            d = math.hypot(x - c, y - c)
            if d > c + .3:
                continue
            if d > c - .7:
                g[y][x] = 'K'
            elif pressed:
                g[y][x] = 'd' if math.hypot(x - c, y - c + 2) > c - .7 else face
            else:
                top = math.hypot(x - c, y - c + 1.5) <= c - 1.7
                if not top:
                    g[y][x] = lip
                elif math.hypot(x - c + 1, y - c + 2.5) > c - 1.7:
                    g[y][x] = light
                else:
                    g[y][x] = face
    return g


def round_mask():
    g = round_button('1', '1', '1')
    return [[c if c == '.' else '1' for c in row] for row in g]


# ---------- discos ----------

def disc(n=16):
    c = (n - 1) / 2
    return [['1' if math.hypot(x - c, y - c) <= c + .3 else '.' for x in range(n)] for y in range(n)]


def ring():
    n = 24
    c = (n - 1) / 2
    g = grid(n, n)
    for y in range(n):
        for x in range(n):
            d = math.hypot(x - c, y - c)
            if c - 4.2 < d <= c - .7:
                g[y][x] = 'W' if d < c - 3.2 and x + y < n - 1 else ('y' if x + y > n else 'Y')
            elif c - 5.2 < d <= c - 4.2 or c - .7 < d <= c + .3:
                g[y][x] = 'K'
    return g


def dot(on):
    m = mask_from(disk(4, 4, 3.2), 8, 8)
    g = shade(m, 'Y', 'W', 'y') if on else shade(m, 'm', 'l', 'd')
    return outline(g)


# ---------- ícones 16x16 ----------

def icon_coin():
    m = mask_from(disk(8, 8, 6.4), 16, 16)
    g = shade(m, 'Y', 'W', 'y')
    for y in range(16):
        for x in range(16):
            d = math.hypot(x + .5 - 8, y + .5 - 8)
            if 3.4 <= d < 4.4:
                g[y][x] = 'y' if (x + y) < 15 else 'W'
    put(g, 7, 6, 'y'); put(g, 7, 7, 'y'); put(g, 7, 8, 'y'); put(g, 7, 9, 'y')  # o traço do meio
    put(g, 8, 6, 'W'); put(g, 8, 7, 'W'); put(g, 8, 8, 'W'); put(g, 8, 9, 'W')
    return outline(g)


def icon_chaos():
    m = mask_from(poly([(8, 1.2), (15, 14.5), (1, 14.5)]), 16, 16)
    g = shade(m, 'Y', 'W', 'y')
    for y in range(5, 11):
        put(g, 7, y, 'K'); put(g, 8, y, 'K')
    put(g, 7, 12, 'K'); put(g, 8, 12, 'K')
    return outline(g)


def icon_quake():
    g = grid(16, 16)
    pts = [(1, 9), (4, 9), (5, 5), (7, 13), (9, 3), (11, 12), (12, 9), (14, 9)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 3) + 1
        for i in range(steps + 1):
            t = i / steps
            x, y = round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t)
            put(g, x, y, 'O'); put(g, x, y - 1, 'Y')
    return outline(g)


def icon_storm():
    g = from_ascii('''
................
......WWW.......
....WWWWWWW.....
...WWWWWWWWW....
..WWWWWWWWWWWW..
.WWWWWWWWWWWWWW.
.hhhhhhhhhhhhhh.
..hhhhhYYhhhhh..
.......YY.......
......YY........
.....YYYYY......
.......YY.......
......YY........
......Y.........
................
................''')
    return outline(g)


def icon_fire():
    g = from_ascii('''
................
.......R........
......RR........
......RRR....R..
.....RROR...RR..
....RROOR..RRR..
....ROOORRRROR..
...RROOYORROOR..
...ROOYYOOOOOR..
...ROYYYYOOYOR..
...ROYWWYYYYOR..
...ROYWWWYYYOR..
....ROYWWYYOR...
.....RROOOORR...
.......RRRR.....
................''')
    return outline(g)


def icon_house():
    g = from_ascii('''
................
.......RR...nn..
......RRRR..nn..
.....RRRRRR.nn..
....RRRRRRRRnn..
...RRRRRRRRRRR..
..rrrrrrrrrrrrr.
...WWWWWWWWWWW..
...WBBWWWWnnWW..
...WBBWWWWnnWW..
...WWWWWWWnnWW..
...hhhhhhhnnhh..
................
................
................
................''')
    return outline(g)


def icon_clock():
    m = mask_from(disk(8, 8, 6.4), 16, 16)
    g = shade(m, 'W', 'W', 'h')
    for y in range(16):
        for x in range(16):
            d = math.hypot(x + .5 - 8, y + .5 - 8)
            if 5.0 <= d <= 6.4 and m[y][x]:
                g[y][x] = 'l' if x + y < 15 else 'b'
    for y in range(4, 9):
        put(g, 7, y, 'K')
    for x in range(8, 11):
        put(g, x, 8, 'K')
    put(g, 7, 8, 'R')
    return outline(g)


def icon_star():
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        r = 7.2 if i % 2 == 0 else 3.1
        pts.append((8 + r * math.cos(a), 8.6 + r * math.sin(a)))
    g = shade(mask_from(poly(pts), 16, 16), 'Y', 'W', 'y')
    return outline(g)


def icon_pause():
    m = [[(3 <= x <= 6 or 9 <= x <= 12) and 3 <= y <= 12 for x in range(16)] for y in range(16)]
    return outline(shade(m, 'W', 'W', 'h'))


def icon_close():
    def hit(x, y):
        return 1.5 < x < 14.5 and 1.5 < y < 14.5 and (abs(x - y) < 2.2 or abs(x + y - 16) < 2.2)
    return outline(shade(mask_from(hit, 16, 16), 'W', 'W', 'h'))


def arrow_mask():
    return mask_from(poly([(8, 1), (15, 8), (11, 8), (11, 15), (5, 15), (5, 8), (1, 8)]), 16, 16)


def icon_arrow_up():
    return outline(shade(arrow_mask(), 'Y', 'W', 'y'))


def icon_arrow_down():
    return outline(shade(flip_v(arrow_mask()), 'Y', 'W', 'y'))


def emblem():
    n = 32
    g = shade(mask_from(disk(16, 16, 14.6), n, n), 'B', 'B', 'u')
    for y in range(n):
        for x in range(n):
            d = math.hypot(x + .5 - 16, y + .5 - 16)
            if 12.4 <= d <= 13.6:
                g[y][x] = 'W'
            elif 13.6 < d <= 14.6 and g[y][x] != '.':
                g[y][x] = 'u'
    tri = mask_from(poly([(16, 4.5), (26.5, 23.5), (5.5, 23.5)]), n, n)
    t = shade(tri, 'O', 'Y', 'n')
    for y in range(n):
        for x in range(n):
            if t[y][x] != '.':
                g[y][x] = t[y][x]
    inner = mask_from(poly([(16, 13), (20.5, 21), (11.5, 21)]), n, n)
    for y in range(n):
        for x in range(n):
            if inner[y][x]:
                g[y][x] = 'W'
    return outline(g)


# ---------- cursor (16x16 desenhado, gravado em 32x32) ----------

def cursor_arrow():
    return outline(from_ascii('''
................
.W..............
.WW.............
.WWW............
.WWWW...........
.WWWWW..........
.WWWWWW.........
.WWWWWWW........
.WWWWWWWW.......
.WWWWWhhhh......
.WWhWWh.........
.Wh..hWW........
......hWW.......
.......hW.......
................
................'''), diagonal=True)


HAND = '''
................
......WW........
.....WWWh.......
.....WWWh.......
.....WWWh.......
.....WWWhWhWh...
.....WWWWhWhWh..
..WW.WWWWWWWWWh.
..WWWWWWWWWWWWh.
...WWWWWWWWWWWh.
...WWWWWWWWWWWh.
....WWWWWWWWWh..
....WWWWWWWWWh..
.....WWWWWWWh...
................
................'''


def cursor_hand():
    return outline(from_ascii(HAND), diagonal=True)


def cursor_press():
    rows = from_ascii(HAND)
    # dedo dobrado: a ponta some e a mão desce um pixel
    rows = [list('.' * 16)] + [list('.' * 16) if y < 3 else rows[y] for y in range(15)]
    for x in range(16):
        if rows[4][x] != '.':
            rows[4][x] = 'h'
    return outline(rows, diagonal=True)


# ---------- lista do kit ----------
# nome do arquivo -> (desenho, borda do 9-slice ou None, campo do FeelUITheme)

def kit():
    w, wb = window()
    i, ib = inset()
    gf, gfb = gold_frame()
    tf, tfb = tint_frame()
    rb, rbb = ribbon()
    bf, bfb = bar_frame()
    fl, flb = bar_fill()
    items = {
        'Px_Window': (w, wb, 'window'),
        'Px_Inset': (i, ib, 'inset'),
        'Px_GoldFrame': (gf, gfb, 'goldFrame'),
        'Px_TintFrame': (tf, tfb, 'tintFrame'),
        'Px_Ribbon': (rb, rbb, 'ribbon'),
        'Px_BarFrame': (bf, bfb, 'barFrame'),
        'Px_BarFill': (fl, flb, 'barFill'),
        'Px_Button': (button('b', 'l', 'm', 'h'), BUTTON_BORDER, 'buttonNormal'),
        'Px_ButtonHover': (button('l', 'h', 'b', 'W'), BUTTON_BORDER, 'buttonHover'),
        'Px_ButtonPressed': (button('m', 'd', 'm', 'm', pressed=True), BUTTON_BORDER, 'buttonPressed'),
        'Px_ButtonDisabled': (button('p', 'm', 'd', 'm'), BUTTON_BORDER, 'buttonDisabled'),
        'Px_ButtonMask': (button_mask(), BUTTON_BORDER, 'buttonMask'),
        'Px_Primary': (button('Y', 'W', 'y', 'W'), BUTTON_BORDER, 'primaryNormal'),
        'Px_PrimaryHover': (button('Y', 'W', 'O', 'W', inner='W'), BUTTON_BORDER, 'primaryHover'),
        'Px_PrimaryPressed': (button('y', 'n', 'y', 'y', pressed=True), BUTTON_BORDER, 'primaryPressed'),
        'Px_Round': (round_button('b', 'l', 'm'), None, 'roundNormal'),
        'Px_RoundHover': (round_button('l', 'h', 'b'), None, 'roundHover'),
        'Px_RoundPressed': (round_button('m', 'm', 'm', pressed=True), None, 'roundPressed'),
        'Px_RoundMask': (round_mask(), None, 'roundMask'),
        'Px_Disc': (disc(), None, 'disc'),
        'Px_DotOn': (dot(True), None, 'dotOn'),
        'Px_DotOff': (dot(False), None, 'dotOff'),
        'Px_Ring': (ring(), (11, 11, 11, 11), 'ring'),  # 9-slice: cresce com a casa sem mudar o pixel
        'Px_IconCoin': (icon_coin(), None, 'iconCoin'),
        'Px_IconChaos': (icon_chaos(), None, 'iconChaos'),
        'Px_IconQuake': (icon_quake(), None, 'iconEarthquake'),
        'Px_IconStorm': (icon_storm(), None, 'iconStorm'),
        'Px_IconFire': (icon_fire(), None, 'iconFire'),
        'Px_IconHouse': (icon_house(), None, 'iconHouse'),
        'Px_IconClock': (icon_clock(), None, 'iconClock'),
        'Px_IconStar': (icon_star(), None, 'iconStar'),
        'Px_IconPause': (icon_pause(), None, 'iconPause'),
        'Px_IconClose': (icon_close(), None, 'iconClose'),
        'Px_IconArrowUp': (icon_arrow_up(), None, 'iconArrowUp'),
        'Px_IconArrowDown': (icon_arrow_down(), None, 'iconArrowDown'),
        'Px_Emblem': (emblem(), None, 'emblem'),
    }
    cursors = {
        'Px_CursorArrow': (scale(cursor_arrow(), 2), 'cursorNormal'),
        'Px_CursorHand': (scale(cursor_hand(), 2), 'cursorHover'),
        'Px_CursorPress': (scale(cursor_press(), 2), 'cursorPressed'),
    }
    return items, cursors


# ---------- gravação ----------

def to_image(g):
    w, h = size(g)
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    px = im.load()
    for y in range(h):
        for x in range(w):
            c = g[y][x]
            if c == '.':
                continue
            if c not in RGBA:
                raise ValueError('cor fora da paleta: %r' % c)
            px[x, y] = RGBA[c]
    return im


def guid(name):
    return hashlib.md5(('defesa-civil/ui-pixel/' + name).encode()).hexdigest()


def meta_text(name, border, cursor=False):
    l, b, r, t = border or (0, 0, 0, 0)
    tpl = open(os.path.join(os.path.dirname(__file__), 'sprite.meta.txt'), encoding='utf-8').read()
    return (tpl.replace('{GUID}', guid(name))
               .replace('{BORDER}', '{x: %d, y: %d, z: %d, w: %d}' % (l, b, r, t))
               .replace('{TYPE}', '7' if cursor else '8')
               .replace('{READABLE}', '1' if cursor else '0')
               .replace('{SPRITEMODE}', '0' if cursor else '1'))


def write(name, g, border, cursor=False):
    to_image(g).save(os.path.join(OUT, name + '.png'))
    with open(os.path.join(OUT, name + '.png.meta'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(meta_text(name, border, cursor))


def write_theme(items, cursors):
    text = open(THEME_ASSET, encoding='utf-8').read()
    head = text.split('  usePixelArt:')[0].rstrip('\n') + '\n'
    lines = ['  usePixelArt: 1', '  pixelScale: 3', '  skinSceneHud: 1']
    for name, (_, _, field) in items.items():
        lines.append('  %s: {fileID: 21300000, guid: %s, type: 3}' % (field, guid(name)))
    for name, (_, field) in cursors.items():
        lines.append('  %s: {fileID: 2800000, guid: %s, type: 3}' % (field, guid(name)))
    lines.append('  cursorHotspot: {x: 2, y: 2}')
    lines.append('  cursorHandHotspot: {x: 13, y: 2}')
    with open(THEME_ASSET, 'w', encoding='utf-8', newline='\n') as f:
        f.write(head + '\n'.join(lines) + '\n')


def contact_sheet(items, cursors, path, zoom=6):
    tiles = [(n, to_image(g)) for n, (g, _, _) in items.items()] + [(n, to_image(g)) for n, (g, _) in cursors.items()]
    cols, cell = 7, 34 * zoom
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cell, rows * cell), (60, 58, 66, 255))
    for i, (n, im) in enumerate(tiles):
        big = im.resize((im.width * zoom, im.height * zoom), Image.NEAREST)
        if big.width > cell - 8:
            big = im.resize((im.width * zoom // 2, im.height * zoom // 2), Image.NEAREST)
        x, y = (i % cols) * cell + (cell - big.width) // 2, (i // cols) * cell + (cell - big.height) // 2
        sheet.alpha_composite(big, (x, y))
    sheet.save(path)


def main():
    os.makedirs(OUT, exist_ok=True)
    items, cursors = kit()
    colors = set()
    for name, (g, border, _) in items.items():
        write(name, g, border)
        colors |= {c for row in g for c in row if c != '.'}
    for name, (g, _) in cursors.items():
        write(name, g, None, cursor=True)
        colors |= {c for row in g for c in row if c != '.'}
    write_theme(items, cursors)
    print('%d sprites, %d cursores, %d cores usadas de %d' % (len(items), len(cursors), len(colors), len(PALETTE)))
    if '--sheet' in sys.argv:
        contact_sheet(items, cursors, sys.argv[sys.argv.index('--sheet') + 1])


if __name__ == '__main__':
    main()
