"""Sprites dos terrenos de expansão (Assets/Systems/Expansion/Resources/Expansion), em pixel art, por script.

Gera o chão do terreno (terra batida com estacas e corda), o ponto do anel de alcance, a seta da borda, a placa
(9-slice) e os ícones de 16 px (base, armazém, obra, moeda). Paleta do kit de UI (Tools/PixelUI/pixel_kit.py) mais
os tons de terra do chão assado da cidade. Escreve o .meta de cada PNG (Sprite, PPU 32, Point, sem compressão) com
GUID fixo: rodar de novo não quebra referência nenhuma.

Uso: python Tools/Expansion/lots_kit.py [--previa saida.png]
"""
import hashlib
import os
import random
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'Assets', 'Systems', 'Expansion', 'Resources', 'Expansion')
META_TPL = os.path.join(ROOT, 'Tools', 'PixelUI', 'sprite.meta.txt')

C = {
    'K': '141218', 'p': '29262F', 'm': '3A3442', 'b': '594C63', 'h': 'A895B5', 'W': 'F2EBDD',
    'Y': 'FFD14D', 'y': 'C98A2B', 'n': '7A4A22', 'O': 'F28C28', 'R': 'D9412F', 'B': '3F7FE0', 'u': '24448A',
    'G': '5CC45E', 'g': '2E7A3C', '1': 'FFFFFF', '2': 'D0D0D0', '3': '9A9A9A',
    # terra batida (tons do caminho e da terra do chão assado)
    'e': '8A6E4A', 'E': '9E8258', 'f': '6E5638', 'F': '5A4630', 's': 'B8A070',
    # madeira e corda
    'w': '5A3A1E', 'x': '8A5A2E', 'c': 'C9B07A',
}
RGBA = {k: tuple(int(v[i:i + 2], 16) for i in (0, 2, 4)) + (255,) for k, v in C.items()}


def guid(name):
    return hashlib.md5(('defesa-civil/expansion/' + name).encode()).hexdigest()


def canvas(w, h):
    return Image.new('RGBA', (w, h), (0, 0, 0, 0))


def put(im, x, y, k):
    if 0 <= x < im.width and 0 <= y < im.height:
        im.putpixel((x, y), RGBA[k])


def grid(rows):
    """Desenho em texto: cada caractere é uma cor da paleta, '.' é transparente."""
    im = canvas(len(rows[0]), len(rows))
    for y, row in enumerate(rows):
        for x, k in enumerate(row):
            if k != '.':
                put(im, x, y, k)
    return im


def patch():
    """Terreno vazio: terra batida com borda irregular, corda entre 4 estacas com fita laranja."""
    w, h = 120, 76
    im = canvas(w, h)
    rnd = random.Random(484)
    for y in range(h):
        for x in range(w):
            edge = min(x, y, w - 1 - x, h - 1 - y)
            if edge < 2 and rnd.random() < 0.55:
                continue
            r = rnd.random()
            k = 'e' if r < 0.62 else 'E' if r < 0.82 else 'f' if r < 0.95 else 'F'
            if edge < 4:
                k = 'f' if r < 0.5 else 'e'
            put(im, x, y, k)
    # pedrinhas e marcas de pá
    for _ in range(26):
        x, y = rnd.randrange(8, w - 8), rnd.randrange(8, h - 8)
        put(im, x, y, 's')
        put(im, x + 1, y, 'E')
        put(im, x, y + 1, 'F')
    for _ in range(10):
        x, y = rnd.randrange(10, w - 14), rnd.randrange(10, h - 10)
        for i in range(5):
            put(im, x + i, y, 'f')
    # corda entre as estacas, a 6 px da borda
    m = 6
    for x in range(m, w - m):
        put(im, x, m, 'c')
        put(im, x, h - 1 - m, 'c')
    for y in range(m, h - m):
        put(im, m, y, 'c')
        put(im, w - 1 - m, y, 'c')
    # estacas com fita laranja da Defesa Civil
    for sx, sy in ((m, m), (w - 1 - m, m), (m, h - 1 - m), (w - 1 - m, h - 1 - m)):
        for dy in range(-6, 2):
            put(im, sx - 1, sy + dy, 'w')
            put(im, sx, sy + dy, 'x')
            put(im, sx + 1, sy + dy, 'w')
        for dx in (-1, 0, 1):
            put(im, sx + dx, sy - 7, 'K')
        for dx in range(2, 5):
            put(im, sx + dx, sy - 5, 'O')
            if dx < 4:
                put(im, sx + dx, sy - 4, 'O')
    return im


def dot():
    im = canvas(4, 4)
    for y in range(4):
        for x in range(4):
            put(im, x, y, '1')
    return im


def arrow():
    return grid([
        '.....KK.....',
        '....K11K....',
        '...K1111K...',
        '..K111111K..',
        '.K11111111K.',
        'K1111111111K',
        'KKKK1111KKKK',
        '...K1111K...',
        '...K1111K...',
        '...K1111K...',
        '...K2222K...',
        '...KKKKKK...',
    ])


def plate():
    rows = ['K' * 24, 'K' + 'h' * 22 + 'K', 'K' + 'b' + 'm' * 20 + 'b' + 'K']
    rows += ['K' + 'm' + 'p' * 20 + 'm' + 'K'] * 17
    rows += ['K' + 'm' * 22 + 'K', 'K' + 'p' * 22 + 'K', 'K' * 24, '.' + 'K' * 22 + '.']
    return grid(rows)


ICONS = {
    # Base da Guarda: escudo azul com o triângulo laranja da Defesa Civil
    'Lot_IconBase': [
        '................',
        '..KKKKKKKKKKKK..',
        '..KBBBBBBBBBBK..',
        '..KBBBBOOBBBBK..',
        '..KBBBOOOOBBBK..',
        '..KBBBOWWOBBBK..',
        '..KBBOOWWOOBBK..',
        '..KBBOOOOOOBBK..',
        '..KBOOOOOOOOBK..',
        '..KBBBBBBBBBBK..',
        '...KBBBBBBBBK...',
        '...KuBBBBBBuK...',
        '....KuBBBBuK....',
        '.....KuuuuK.....',
        '......KKKK......',
        '................',
    ],
    # Armazém: caixote de madeira
    'Lot_IconDepot': [
        '................',
        '.KKKKKKKKKKKKKK.',
        '.KYYYYYYYYYYYYK.',
        '.KyyyyyyyyyyyyK.',
        '.KKKKKKKKKKKKKK.',
        '.KyYyyyyyyyyYyK.',
        '.KyyYyyyyyyYyyK.',
        '.KyyyYyyyyYyyyK.',
        '.KyyyyYyyYyyyyK.',
        '.KyyyyyYYyyyyyK.',
        '.KyyyyYyyYyyyyK.',
        '.KyyyYyyyyYyyyK.',
        '.KyyYyyyyyyYyyK.',
        '.KnnnnnnnnnnnnK.',
        '.KKKKKKKKKKKKKK.',
        '................',
    ],
    # Obra: martelo
    'Lot_IconBuild': [
        '................',
        '...KKKKKKK......',
        '..K3332223K.....',
        '..K2221113K.....',
        '..KKKK2223KK....',
        '.....KK23KxK....',
        '.......KKxxK....',
        '.......KxxK.....',
        '......KxxK......',
        '.....KxxK.......',
        '....KxxK........',
        '...KxxK.........',
        '..KxxK..........',
        '..KwK...........',
        '..KK............',
        '................',
    ],
    'Lot_IconCoin': [
        '................',
        '.....KKKKKK.....',
        '....KYYYYYYK....',
        '...KYYWWYYYyK...',
        '..KYYWYYYYYYyK..',
        '..KYWYYyyYYYyK..',
        '..KYYYyYYYYYyK..',
        '..KYYYYyyYYYyK..',
        '..KYYYYYYyYYyK..',
        '..KYYYyyyYYYyK..',
        '..KYYYYYYYYyyK..',
        '...KyYYYYYyyK...',
        '....KyyyyyyK....',
        '.....KKKKKK.....',
        '................',
        '................',
    ],
}


def sprites():
    """nome -> (imagem, borda 9-slice esquerda, baixo, direita, cima)"""
    out = {
        'Lot_Patch': (patch(), (0, 0, 0, 0)),
        'Lot_Dot': (dot(), (0, 0, 0, 0)),
        'Lot_Arrow': (arrow(), (0, 0, 0, 0)),
        'Lot_Plate': (plate(), (6, 6, 6, 6)),
    }
    for name, rows in ICONS.items():
        out[name] = (grid(rows), (0, 0, 0, 0))
    return out


def meta(name, border):
    tpl = open(META_TPL, encoding='utf-8').read()
    l, b, r, t = border
    return (tpl.replace('{GUID}', guid(name))
               .replace('{BORDER}', '{x: %d, y: %d, z: %d, w: %d}' % (l, b, r, t))
               .replace('{TYPE}', '8')
               .replace('{READABLE}', '0')
               .replace('{SPRITEMODE}', '1'))


def main():
    os.makedirs(OUT, exist_ok=True)
    items = sprites()
    for name, (im, border) in items.items():
        im.save(os.path.join(OUT, name + '.png'))
        with open(os.path.join(OUT, name + '.png.meta'), 'w', encoding='utf-8', newline='\n') as f:
            f.write(meta(name, border))
    print('%d sprites em %s' % (len(items), os.path.relpath(OUT, ROOT)))
    if '--previa' in sys.argv:
        dest = sys.argv[sys.argv.index('--previa') + 1]
        scale = 4
        w = sum(im.width * scale + 16 for im, _ in items.values()) + 16
        h = max(im.height for im, _ in items.values()) * scale + 32
        sheet = Image.new('RGBA', (w, h), (60, 90, 55, 255))
        x = 16
        for im, _ in items.values():
            sheet.alpha_composite(im.resize((im.width * scale, im.height * scale), Image.NEAREST), (x, 16))
            x += im.width * scale + 16
        sheet.save(dest)
        print('prévia', dest, sheet.size)


if __name__ == '__main__':
    main()
