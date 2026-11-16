"""Prévias da UI em pixel art, montadas sem o Unity (Pillow), com os sprites do kit e a fonte ModernDOS.

Uso: python Tools/PixelUI/previa_ui_pixel.py [pasta de saída]
Cada tela sai como ui_pixel_<tela>.png em 1920x1080 (a resolução de referência do CanvasScaler), com a faixa
"PRÉVIA" no canto: é maquete do layout do código (tamanhos e posições padrão dos scripts), não captura do jogo.
O 9-slice segue o Image.Type.Sliced: cantos com o pixel do sprite valendo PIXEL unidades, miolo esticado.
"""
import os
import re
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PX = os.path.join(ROOT, 'Assets', 'Arts', 'UI', 'Pixel')
FONT = os.path.join(ROOT, 'Assets', 'Fonts', 'ModernDOS8x16.ttf')
OUT = sys.argv[1] if len(sys.argv) > 1 else r'C:\Users\PIETRO\analises-lilith\jogo-defesa-civil\resultados'
CITY = os.path.join(OUT, '11_mapa_novo_inteiro.png')
W, H = 1920, 1080
PIXEL = 3  # FeelUITheme.pixelScale

CREAM, GOLD, INK = (242, 235, 221), (255, 209, 77), (20, 18, 24)
PANEL, BUTTON = (41, 38, 47), (89, 76, 99)
_cache = {}


def c(r, g, b, a=1.0):
    return (round(r * 255), round(g * 255), round(b * 255), round(a * 255))


def sprite(name):
    """PNG do kit e a borda do .meta (x esquerda, y baixo, z direita, w cima)."""
    if name not in _cache:
        path = name if os.path.isabs(name) else os.path.join(PX, name + '.png')
        im = Image.open(path).convert('RGBA')
        border = (0, 0, 0, 0)
        if os.path.exists(path + '.meta'):
            meta = open(path + '.meta').read()
            m = re.search(r'spriteBorder: \{x: (\d+), y: (\d+), z: (\d+), w: (\d+)\}', meta)
            if m:
                border = tuple(int(v) for v in m.groups())
            r = re.search(r'rect:\s+serializedVersion: 2\s+x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)', meta)
            if 'spriteMode: 2' in meta and r:  # folha: o primeiro recorte (y do Unity conta de baixo)
                x, y, w, h = (int(v) for v in r.groups())
                im = im.crop((x, im.height - y - h, x + w, im.height - y))
        _cache[name] = (im, border)
    return _cache[name]


def tint(im, color):
    if color is None:
        return im
    color = tuple(color) + (255,) * (4 - len(color))
    return ImageChops.multiply(im, Image.new('RGBA', im.size, color))


def paste(base, im, x, y):
    base.alpha_composite(im, (int(round(x)), int(round(y))))


def sliced(base, name, x, y, w, h, color=None, scale=PIXEL):
    """Image.Type.Sliced: x, y = canto de cima à esquerda na tela."""
    im, (bl, bb, br, bt) = sprite(name)
    sw, sh = im.size
    w, h = int(round(w)), int(round(h))
    k = min(1.0, w / max(1, (bl + br) * scale), h / max(1, (bb + bt) * scale))  # borda maior que a peça encolhe
    L, R, T, B = [int(round(v * scale * k)) for v in (bl, br, bt, bb)]
    cols = [(0, bl, L), (bl, sw - br, w - L - R), (sw - br, sw, R)]
    rows = [(0, bt, T), (bt, sh - bb, h - T - B), (sh - bb, sh, B)]
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    oy = 0
    for y0, y1, th in rows:
        ox = 0
        for x0, x1, tw in cols:
            if x1 > x0 and y1 > y0 and tw > 0 and th > 0:
                out.paste(im.crop((x0, y0, x1, y1)).resize((tw, th), Image.NEAREST), (ox, oy))
            ox += tw
        oy += th
    paste(base, tint(out, color), x, y)


def simple(base, name, cx, cy, scale=PIXEL, color=None):
    """Ícone com o pixel inteiro (FeelUI.CreateIcon), centrado em cx, cy."""
    im, _ = sprite(name)
    im = im.resize((round(im.width * scale), round(im.height * scale)), Image.NEAREST)
    paste(base, tint(im, color), cx - im.width / 2, cy - im.height / 2)


def fit(base, path, cx, cy, size):
    """Sprite de fora do kit com preserveAspect numa caixa quadrada."""
    im, _ = sprite(path)
    k = size / max(im.size)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.NEAREST)
    paste(base, im, cx - im.width / 2, cy - im.height / 2)


def font(size):
    return ImageFont.truetype(FONT, int(size))


def text(base, s, cx, cy, size, color=CREAM, anchor='mm', spacing=0, shadow=False):
    s = re.sub(r'<[^>]+>', '', s)
    d = ImageDraw.Draw(base)
    if shadow:  # sombra de 1 pixel do kit (ChaosBar)
        d.multiline_text((cx + PIXEL, cy + PIXEL), s, font=font(size), fill=INK + (255,), anchor=anchor,
                         align='center' if anchor[0] == 'm' else 'left', spacing=spacing)
    d.multiline_text((cx, cy), s, font=font(size), fill=tuple(color)[:3] + (255,), anchor=anchor,
                     align='center' if anchor[0] == 'm' else 'left', spacing=spacing)


def button(base, x, y, w, h, label, size=40, state='', primary=False, rect=True):
    fam = 'Px_Primary' if primary else 'Px_Button'
    name = fam + {'': '', 'hover': 'Hover', 'pressed': 'Pressed', 'disabled': ''}[state]
    if state == 'disabled':
        name = 'Px_ButtonDisabled'
    sliced(base, name, x, y, w, h)
    lift = 0 if state == 'pressed' else 2 * PIXEL  # FeelUI.LiftLabel
    color = PANEL if primary else (CREAM if state != 'disabled' else (122, 106, 135))
    if label:
        text(base, label, x + w / 2, y + h / 2 - lift / 2 + (PIXEL if state == 'pressed' else 0), size, color)


def round_button(base, cx, cy, size, icon=None, state=''):
    name = 'Px_Round' + {'': '', 'hover': 'Hover', 'pressed': 'Pressed'}[state]
    im, _ = sprite(name)
    k = size / im.width
    paste(base, im.resize((round(im.width * k), round(im.height * k)), Image.NEAREST), cx - size / 2, cy - size / 2)
    if icon:
        im, _ = sprite(icon)
        s = size * 2 / 3 / im.width
        im = im.resize((round(im.width * s), round(im.height * s)), Image.NEAREST)
        paste(base, im, cx - im.width / 2, cy - im.height / 2 - (0 if state == 'pressed' else size / 24))


def plate(base, x0, y0, x1, y1):
    """HudPlate: inset atrás de ícone + número, 3 px de folga."""
    pad = 3 * PIXEL
    sliced(base, 'Px_Inset', x0 - pad, y0 - pad, x1 - x0 + 2 * pad, y1 - y0 + 2 * pad)


def city(dim=0):
    """Fundo: recorte da cidade (sem UI) ampliado até o zoom do jogo."""
    if os.path.exists(CITY):
        im = Image.open(CITY).convert('RGBA')
        cw, ch = 800, 450
        x0, y0 = (im.width - cw) // 2 - 40, (im.height - ch) // 2 + 30
        im = im.crop((x0, y0, x0 + cw, y0 + ch)).resize((W, H), Image.NEAREST)
    else:
        im = Image.new('RGBA', (W, H), (70, 80, 70, 255))
    if dim:
        im.alpha_composite(Image.new('RGBA', (W, H), (0, 0, 0, int(dim * 255))))
    return im


def stamp(base, name, note):
    """Faixa PRÉVIA no canto de baixo à direita: a imagem não é captura do jogo."""
    label = 'PRÉVIA 1920x1080 - %s - montada por script, não é captura do jogo' % note
    f = font(24)
    d = ImageDraw.Draw(base)
    w = d.textlength(label, font=f)
    d.rectangle((W - w - 36, H - 46, W, H), fill=(20, 18, 24, 230))
    d.text((W - w - 18, H - 23), label, font=f, fill=GOLD + (255,), anchor='lm')
    path = os.path.join(OUT, 'ui_pixel_%s.png' % name)
    base.convert('RGB').save(path)
    print(path)


# ---------------------------------------------------------------- telas

def chaos_bar(base, value=.62):
    w, h = 560, 44
    x, y = (W - w) / 2, 26
    sliced(base, 'Px_BarFrame', x, y, w, h)
    inset = 3 * PIXEL
    iw = w - 2 * inset
    fill = c(.95, .8, .25) if value < .75 else c(.9, .22, .2)
    sliced(base, 'Px_BarFill', x + inset, y + inset, iw * value, h - 2 * inset, fill)
    d = ImageDraw.Draw(base)
    for t in (.25, .5, .75):
        tx = x + inset + iw * t
        d.rectangle((tx, y + inset, tx + PIXEL - 1, y + h - inset - 1), fill=(0, 0, 0, 90))
    tx = x + inset + iw * .8
    d.rectangle((tx, y + inset, tx + PIXEL - 1, y + h - inset - 1), fill=c(.9, .22, .2))
    text(base, 'CAOS', W / 2, y + h / 2, 30, shadow=True)
    simple(base, 'Px_IconChaos', x + PIXEL, y + h / 2 - PIXEL / 2)


def banner(base, kind='quake'):
    w, h = 860, 92
    x, y = (W - w) / 2, 88
    color = {'quake': c(.9, .58, .2), 'storm': c(.3, .58, .98), 'fire': c(.95, .3, .2)}[kind]
    edge = 4 * PIXEL
    sliced(base, 'Px_TintFrame', x, y, w, h, color)
    ImageDraw.Draw(base).rectangle((x + edge, y + edge, x + w - edge - 1, y + h - edge - 1), fill=PANEL)
    slot = 16 * PIXEL
    simple(base, {'quake': 'Px_IconQuake', 'storm': 'Px_IconStorm', 'fire': 'Px_IconFire'}[kind],
           x + edge + 16 + slot / 2, y + h / 2)
    box = h - edge * 2 - 2 * PIXEL
    bx = x + w - (edge + 16 + h / 2)
    sliced(base, 'Px_Inset', bx - box * 1.25 / 2, y + (h - box) / 2, box * 1.25, box)
    text(base, '3', bx, y + h / 2, 60)
    name = {'quake': 'TERREMOTO', 'storm': 'TEMPESTADE', 'fire': 'INCÊNDIO'}[kind]
    text(base, name + ' CHEGANDO!', W / 2, y + h / 2, 46)


def counters(base):
    icons = os.path.join(ROOT, 'Assets', 'Arts', 'Icons')
    for i, (icon, value) in enumerate((('Worker.png', '3'), ('BuildMaterial.png', '3'))):
        cy = 76 + i * 96
        plate(base, 30, cy - 40, 160, cy + 40)
        fit(base, os.path.join(icons, icon), 70, cy, 80)
        text(base, value, 140, cy, 40)
    plate(base, 1740, 40, 1884, 88)
    text(base, '20', 1786, 64, 40)
    simple(base, 'Px_IconCoin', 1856, 64)
    round_button(base, 1856, 150, 72, 'Px_IconPause')


def tip(base):
    w, h = 1240, 84
    x, y = (W - w) / 2, H - 110 - h
    sliced(base, 'Px_GoldFrame', x, y, w, h)
    edge = 5 * PIXEL
    dot = 8 * PIXEL
    left = edge * 2 + dot / 2
    for i in range(3):
        simple(base, 'Px_DotOn' if i == 0 else 'Px_DotOff', x + left + i * (dot + 10), y + h / 2)
    text(base, 'Uma casa precisa de ajuda! Clique nela para abrir um chamado.',
         x + left + 3 * (dot + 10) + (w - left - 3 * (dot + 10) - edge * 2) / 2, y + h / 2 - edge * .25, 34)


def pointer(base, cx, cy, height=190):
    """Aro dourado (9-slice, altura da casa x 1.25) e a seta pixel acima (TutorialTips)."""
    size = height * 1.25
    sliced(base, 'Px_Ring', cx - size / 2, cy - size / 2, size, size)
    simple(base, 'Px_IconArrowDown', cx, cy - height / 2 - 26 - 23, PIXEL)


def fire_timer(base, cx, cy, w=110, h=10, left=.55):
    d = ImageDraw.Draw(base)
    x, y = cx - w / 2, cy - h / 2
    d.rectangle((x, y, x + w - 1, y + h - 1), fill=INK)
    p = h / 5
    d.rectangle((x + p, y + p, x + p + (w - 2 * p) * left - 1, y + h - p - 1), fill=c(.95, .55, .2))


def hud():
    base = city()
    counters(base)
    chaos_bar(base)
    banner(base)
    pointer(base, 352, 625, 170)
    fire_timer(base, 1810, 420)
    tip(base)
    sliced(base, 'Px_Button', 24, 950, 92, 92)
    simple(base, 'Px_IconArrowUp', 70, 990)
    stamp(base, 'hud', 'HUD: contadores, caos, aviso, dica')


def upgrades():
    base = city()
    counters(base)
    chaos_bar(base, .4)
    x, y, w, h = 6, 230, 430, 650
    sliced(base, 'Px_Window', x, y, w, h)
    text(base, 'Melhorias', x + 28, y + 34, 36, anchor='lm')
    round_button(base, x + w - 36, y + 34, 48, 'Px_IconClose')
    arts = os.path.join(ROOT, 'Assets', 'Arts')
    rows = [('Contratar trabalhador', 30, '0/12', 'Icons/Worker.png'),
            ('Botas de corrida', 25, '0/8', 'UI/Arrow.png'),
            ('Ferramentas melhores', 40, '0/8', 'Icons/Kit.png'),
            ('Carrinho de mão', 60, '0/4', 'Icons/BuildMaterial.png'),
            ('Seguro municipal', 50, '0/10', 'Houses/Suburban/Casa_A/Casa_A.png'),
            ('Automação', 150, '0/4', 'Houses/Carros/Ambulancia/Ambulancia.png'),
            ('Repasse da Prefeitura', 200, '0/5', 'Houses/Commercial/Predio_A/Predio_A.png'),
            ('Centro de Operações', 5000, '0/1', 'Houses/Commercial/Arranha_Ceu_A/Arranha_Ceu_A.png')]
    for i, (name, price, count, art) in enumerate(rows):
        ry = y + 66 + i * 72
        state = 'hover' if i == 1 else 'disabled' if price > 100 else ''
        sliced(base, 'Px_ButtonHover' if state == 'hover' else 'Px_ButtonDisabled' if state else 'Px_Button',
               x + 14, ry, w - 28, 64)
        lift = 0 if state == 'disabled' else 2 * PIXEL
        cy = ry + 32 - lift / 2
        fit(base, os.path.join(arts, art), x + 52, cy, 44)
        dim = (150, 138, 160) if state == 'disabled' else CREAM
        text(base, name, x + 86, cy - 12, 22, dim, anchor='lm')
        simple(base, 'Px_IconCoin', x + 96, cy + 14, 1.25)
        text(base, str(price), x + 112, cy + 14, 20, GOLD if not state == 'disabled' else (201, 138, 43), anchor='lm')
        text(base, count, x + w - 34, cy, 22, dim, anchor='rm')
    stamp(base, 'melhorias', 'aba de melhorias (2a linha em hover, sem dinheiro apagadas)')


def menu():
    base = city(.4)
    bw = W - 180
    sliced(base, 'Px_Window', 90, H / 2 - 200 - 150, bw, 300)
    text(base, 'DEFESA CIVIL', W / 2, H / 2 - 230, 150, (255, 209, 77))
    for side in (-1, 1):
        simple(base, 'Px_Emblem', W / 2 + side * 600, H / 2 - 230, PIXEL * 1.25)
    text(base, 'Mantenha a cidade de pé.', W / 2, H / 2 - 200 + 150 - 40, 40, CREAM)
    button(base, W / 2 - 190, H / 2 + 150 - 52, 380, 104, 'JOGAR', 56, primary=True)
    hh = 30 * 5.6
    sliced(base, 'Px_Window', 90, H / 2 + 340 - hh / 2, bw, hh)
    how = ('1. Clique numa casa com problema para abrir um chamado\n2. Escolha a base, monte a equipe e envie\n'
           '3. Receba o pagamento e compre melhorias antes que o caos tome a cidade')
    text(base, how, W / 2, H / 2 + 340, 30, (219, 224, 237), spacing=14)
    stamp(base, 'menu', 'menu inicial')


def pause():
    base = city()
    counters(base)
    chaos_bar(base, .5)
    base.alpha_composite(Image.new('RGBA', (W, H), (0, 0, 0, 153)))
    pw, ph = 620, 460
    x, y = (W - pw) / 2, (H - ph) / 2
    sliced(base, 'Px_Window', x, y, pw, ph)
    text(base, 'PAUSADO', W / 2, y + 90, 84)
    button(base, W / 2 - 180, H / 2 - 10 - 42, 360, 84, 'CONTINUAR', 40, primary=True)
    button(base, W / 2 - 180, H / 2 + 10 + 84 + 14 - 42, 360, 84, 'REINICIAR', 40, state='hover')
    text(base, 'Tecla P pausa e continua', W / 2, y + ph - 34, 26, (179, 186, 204))
    stamp(base, 'pausa', 'pausa (REINICIAR em hover)')


def end(won):
    base = city()
    dim = c(0, .02, .04, .72) if won else c(.12, 0, 0, .78)
    base.alpha_composite(Image.new('RGBA', (W, H), dim))
    rays = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(rays)
    for i in range(12):
        import math
        a = math.radians(i * 30 + 8)
        b = math.atan2(55, 1100)
        pts = [(W / 2, H / 2)] + [(W / 2 + 1100 * math.cos(a + s * b), H / 2 + 1100 * math.sin(a + s * b)) for s in (-1, 1)]
        d.polygon(pts, fill=c(1, .85, .4, .1))
    base.alpha_composite(rays)
    pw, ph = 860, 620
    x, y = (W - pw) / 2, (H - ph) / 2
    sliced(base, 'Px_Window', x, y, pw, ph)
    rim, _ = sprite('Px_Ribbon')
    rw, rh = 980, rim.height * PIXEL
    rx, ry = (W - rw) / 2, y - rh / 2
    sliced(base, 'Px_Ribbon', rx, ry, rw, rh, c(.25, .62, .33) if won else c(.72, .2, .17))
    for s in (-1, 1):
        simple(base, 'Px_IconStar' if won else 'Px_IconFire', W / 2 + s * (rw / 2 - 16 * PIXEL - 30), ry + rh / 2 - PIXEL)
    title = 'CIDADE SALVA!' if won else 'O CAOS VENCEU'
    text(base, title, W / 2, ry + rh / 2 - PIXEL, 76)
    sub = 'A Defesa Civil deu conta do recado.' if won else 'A cidade ficou tempo demais sem socorro.'
    text(base, sub, W / 2, y + 116, 30, (191, 199, 217))
    stats = [('Tempo', '08:42', 'Px_IconClock'), ('Casas consertadas', '37', 'Px_IconHouse'),
             ('Dinheiro arrecadado', '1250', 'Px_IconCoin')]
    for i, (name, value, icon) in enumerate(stats):
        cy = y + 196 + 80 * i
        sx = (W - 720) / 2
        sliced(base, 'Px_Inset', sx, cy - 33, 720, 66)
        simple(base, icon, sx + 20 + 24, cy)
        text(base, name, sx + 20 + 48 + 18, cy, 38, anchor='lm')
        text(base, value, sx + 720 - 28, cy, 38, (255, 209, 77), anchor='rm')
    button(base, W / 2 - 170, y + ph - 40 - 88, 340, 88, 'REINICIAR', 40, primary=True)
    stamp(base, 'vitoria' if won else 'derrota', 'tela de fim, ' + ('vitória' if won else 'derrota'))


def kit():
    base = Image.new('RGBA', (W, H), PANEL + (255,))
    text(base, 'Kit pixel da UI (1 pixel do sprite = 3 unidades em 1080p)', 40, 40, 32, GOLD, anchor='lm')
    rows = [
        ('Painéis', [('Px_Window', 180, 120), ('Px_Inset', 160, 80), ('Px_GoldFrame', 200, 84),
                     ('Px_TintFrame', 200, 84), ('Px_BarFrame', 260, 44)]),
    ]
    x = 40
    for name, w, h in rows[0][1]:
        color = c(.9, .58, .2) if name == 'Px_TintFrame' else None
        sliced(base, name, x, 90, w, h, color)
        text(base, name[3:], x + w / 2, 90 + h + 22, 20, (168, 149, 181))
        x += w + 36
    x = 40
    for label, fam in (('normal', ''), ('hover', 'Hover'), ('apertado', 'Pressed')):
        button(base, x, 270, 230, 80, label, 32, state={'': '', 'Hover': 'hover', 'Pressed': 'pressed'}[fam])
        button(base, x, 370, 230, 80, label, 32, state={'': '', 'Hover': 'hover', 'Pressed': 'pressed'}[fam], primary=True)
        round_button(base, x + 115, 520, 72, 'Px_IconPause', {'': '', 'Hover': 'hover', 'Pressed': 'pressed'}[fam])
        x += 260
    button(base, x, 270, 230, 80, 'desativado', 32, state='disabled')
    sliced(base, 'Px_Ribbon', x + 270, 270, 560, 120, c(.25, .62, .33))
    sliced(base, 'Px_Ribbon', x + 270, 410, 560, 120, c(.72, .2, .17))
    icons = ['Px_IconCoin', 'Px_IconChaos', 'Px_IconQuake', 'Px_IconStorm', 'Px_IconFire', 'Px_IconHouse',
             'Px_IconClock', 'Px_IconStar', 'Px_IconPause', 'Px_IconClose', 'Px_IconArrowUp', 'Px_IconArrowDown',
             'Px_DotOn', 'Px_DotOff', 'Px_Disc', 'Px_Ring', 'Px_Emblem']
    for i, n in enumerate(icons):
        cx, cy = 80 + (i % 9) * 110, 640 + (i // 9) * 130
        simple(base, n, cx, cy, PIXEL if sprite(n)[0].width <= 24 else 2)
        text(base, n[3:].replace('Icon', ''), cx, cy + 50, 18, (168, 149, 181))
    for i, n in enumerate(('Px_CursorArrow', 'Px_CursorHand', 'Px_CursorPress')):
        simple(base, n, 1100 + i * 120, 680, 2)
        text(base, n[9:], 1100 + i * 120, 740, 18, (168, 149, 181))
    text(base, 'cursores (32x32, tamanho real x2)', 1220, 780, 18, (168, 149, 181))
    pal = ['141218', '1F1C25', '29262F', '3A3442', '594C63', '7A6A87', 'A895B5', 'F2EBDD', 'FFD14D', 'C98A2B',
           '7A4A22', 'F28C28', 'D9412F', '8A2424', '5CC45E', '2E7A3C', '3F7FE0', '24448A', 'FFFFFF', 'D0D0D0', '9A9A9A']
    d = ImageDraw.Draw(base)
    for i, hx in enumerate(pal):
        px0 = 1100 + (i % 7) * 100
        py0 = 840 + (i // 7) * 60
        d.rectangle((px0, py0, px0 + 90, py0 + 50), fill=tuple(int(hx[k:k + 2], 16) for k in (0, 2, 4)), outline=INK)
    text(base, 'paleta (21 cores)', 1100, 820, 18, (168, 149, 181), anchor='lm')
    stamp(base, 'kit', 'kit e cursores')


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for f in (hud, upgrades, menu, pause, lambda: end(True), lambda: end(False), kit):
        f()
