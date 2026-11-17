"""Arte e som do para-raio (Assets/Systems/LightningRod/Resources/LightningRod), por script. PARARAIO.md.

- Rod_Pole.png: o mastro que vai no telhado (16x46, PPU 32 como as casas): ponta e bola de cobre, isolador azul de dois
  discos, mastro de aço de 3 px com duas braçadeiras e a base presa no telhado, contorno de 1 px #141218, paleta do kit
  (Tools/PixelUI/pixel_kit.py).
- Rod_Icon.png: o ícone 64x64 do Pixel Lab escolhido (candidato da #537), com alfa binário, recortado sem reescalar,
  centrado e com 2 px de contorno externo, como os ícones da #527.
- Rod_Zap.wav: o estalo do raio absorvido (0,6 s): estalos de faísca, zumbido elétrico que treme e chiado que some.
  Sintetizado aqui (sem licença de terceiros), semente fixa: rodar de novo dá o mesmo arquivo.
Escreve o .meta de cada arquivo com GUID fixo: rodar de novo não quebra referência nenhuma.

Uso: python Tools/LightningRod/rod_kit.py [--icone candidato.png] [--previa saida.png]
"""
import argparse
import hashlib
import math
import os
import random
import struct
import wave

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'Assets', 'Systems', 'LightningRod', 'Resources', 'LightningRod')
META_TPL = os.path.join(ROOT, 'Tools', 'PixelUI', 'sprite.meta.txt')

C = {
    'K': '141218', 'b': '594C63', 'm': '3A3442', 'h': 'A895B5', 'W': 'F2EBDD',
    'Y': 'FFD14D', 'y': 'C98A2B', 'n': '7A4A22', 'B': '3F7FE0', 'u': '24448A',
    '1': 'FFFFFF', '2': 'D0D0D0', '3': '9A9A9A',
}
RGBA = {k: tuple(int(v[i:i + 2], 16) for i in (0, 2, 4)) + (255,) for k, v in C.items()}
INK = RGBA['K']


def guid(name):
    return hashlib.md5(('defesa-civil/lightningrod/' + name).encode()).hexdigest()


def sprite_meta(name):
    tpl = open(META_TPL, encoding='utf-8').read()
    return (tpl.replace('{GUID}', guid(name))
               .replace('{BORDER}', '{x: 0, y: 0, z: 0, w: 0}')
               .replace('{TYPE}', '8')
               .replace('{READABLE}', '0')
               .replace('{SPRITEMODE}', '1'))


def write_meta(path, text):
    with open(path + '.meta', 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)


def outline(im, steps=1):
    """Contorno de 1 px (4 vizinhos) em volta do que é opaco, 'steps' vezes; o que encosta na borda vira contorno."""
    for _ in range(steps):
        w, h = im.size
        src = im.load()
        out = im.copy()
        dst = out.load()
        for y in range(h):
            for x in range(w):
                if src[x, y][3]:
                    if x in (0, w - 1) or y in (0, h - 1):
                        dst[x, y] = INK
                    continue
                if any(0 <= x + dx < w and 0 <= y + dy < h and src[x + dx, y + dy][3]
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    dst[x, y] = INK
        im = out
    return im


def pole():
    """Mastro 16x46 (y = 0 no topo; o pé fica na última linha): lido de longe no telhado, com 3 px de mastro."""
    im = Image.new('RGBA', (16, 46), (0, 0, 0, 0))
    px = im.load()

    def put(x, y, k):
        px[x, y] = RGBA[k]

    def row(y, x0, cores):
        for i, k in enumerate(cores):
            put(x0 + i, y, k)

    # ponta de cobre afinando para cima, luz à esquerda
    for y in range(1, 3):
        put(7, y, 'Y')
    for y in range(3, 9):
        row(y, 7, 'Yy')
    # bola e colar de cobre
    row(9, 6, 'YYyn')
    row(10, 6, 'Yyyn')
    row(11, 6, 'yynn')
    row(12, 6, 'nnnn')
    # isolador azul de dois discos
    for y, x0, cores in ((13, 5, 'BBBBuu'), (14, 4, '1BBBBBuu'), (15, 4, 'BBBBBuuu'), (16, 5, 'uuuuuu'),
                         (17, 4, '1BBBBBuu'), (18, 5, 'uuuuuu')):
        row(y, x0, cores)
    # mastro de aço de 3 px com duas braçadeiras
    for y in range(19, 40):
        row(y, 6, '123')
    for y in (25, 33):
        row(y, 5, 'hhhbb')
    # base presa no telhado
    row(40, 5, 'hhbbb')
    row(41, 4, 'hhhbbbb')
    row(42, 3, 'mmmmmmmmm')
    row(43, 2, 'mmmmmmmmmmm')
    row(44, 2, 'mmmmmmmmmmm')
    return outline(im)


def icon(path):
    """Candidato do Pixel Lab: alfa binário, recorte justo centrado no 64x64 sem reescalar, 2 px de contorno."""
    im = Image.open(path).convert('RGBA')
    im.putdata([p[:3] + (255,) if p[3] >= 128 else (0, 0, 0, 0) for p in im.getdata()])
    obj = im.crop(im.getbbox())
    w, h = obj.size
    base = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    cut = obj.crop((max(0, (w - 64) // 2), max(0, (h - 64) // 2),
                    max(0, (w - 64) // 2) + min(w, 64), max(0, (h - 64) // 2) + min(h, 64)))
    base.alpha_composite(cut, (max(0, (64 - w) // 2), max(0, (64 - h) // 2)))
    return outline(base, 2)


def zap(path, rate=44100, seconds=0.6, seed=537):
    """Estalo elétrico: faíscas (rajadas curtas de ruído agudo), zumbido de 110 Hz que treme e um chiado que some."""
    rng = random.Random(seed)
    n = int(rate * seconds)
    out = [0.0] * n

    # faíscas: muitas no começo, rareando
    t = 0.0
    while t < seconds * 0.85:
        start = int(t * rate)
        length = int(rate * rng.uniform(0.0015, 0.006))
        amp = rng.uniform(0.5, 1.0) * (1.0 - t / seconds) ** 1.5
        last = 0.0
        for i in range(length):
            if start + i >= n:
                break
            noise = rng.uniform(-1.0, 1.0)
            out[start + i] += (noise - last) * amp * (1.0 - i / length)  # diferença = passa-alta
            last = noise
        t += rng.expovariate(70.0 if t < 0.15 else 25.0)

    # zumbido: dente de serra 110 Hz com tremor de 37 Hz, ataque curto e queda
    for i in range(n):
        s = i / rate
        env = min(1.0, s / 0.01) * math.exp(-s / 0.18)
        tremor = 0.6 + 0.4 * math.sin(2 * math.pi * 37 * s + 3 * math.sin(2 * math.pi * 7 * s))
        saw = 2.0 * ((s * 110.0) % 1.0) - 1.0
        out[i] += 0.32 * env * tremor * saw

    # chiado que some
    last = 0.0
    for i in range(n):
        s = i / rate
        noise = rng.uniform(-1.0, 1.0)
        out[i] += 0.18 * (noise - last) * math.exp(-s / 0.25)
        last = noise

    peak = max(abs(v) for v in out) or 1.0
    gain = 0.7 / peak  # -3 dB
    fade = int(rate * 0.02)
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        frames = bytearray()
        for i, v in enumerate(out):
            if i >= n - fade:
                v *= (n - i) / fade
            frames += struct.pack('<h', int(max(-1.0, min(1.0, v * gain)) * 32767))
        w.writeframes(bytes(frames))


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('--icone', help='candidato do Pixel Lab para o Rod_Icon (sem ele, o Rod_Icon fica como está)')
    p.add_argument('--previa', help='folha com o mastro e o ícone ampliados')
    a = p.parse_args()

    os.makedirs(OUT, exist_ok=True)
    folder_meta = OUT + '.meta'
    if not os.path.exists(folder_meta):
        with open(folder_meta, 'w', encoding='utf-8', newline='\n') as f:
            f.write('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                    '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % guid('folder'))

    made = {'Rod_Pole': pole()}
    if a.icone:
        made['Rod_Icon'] = icon(a.icone)
    for name, im in made.items():
        dest = os.path.join(OUT, name + '.png')
        im.save(dest)
        write_meta(dest, sprite_meta(name))
        print(name, im.size, guid(name))

    sound = os.path.join(OUT, 'Rod_Zap.wav')
    zap(sound)
    write_meta(sound, 'fileFormatVersion: 2\nguid: %s\n' % guid('Rod_Zap'))
    print('Rod_Zap.wav', os.path.getsize(sound), guid('Rod_Zap'))

    if a.previa:
        pieces = [Image.open(os.path.join(OUT, n + '.png')).convert('RGBA') for n in ('Rod_Pole', 'Rod_Icon')
                  if os.path.exists(os.path.join(OUT, n + '.png'))]
        scale = 6
        sheet = Image.new('RGBA', (sum(im.width * scale + 20 for im in pieces) + 20, 64 * scale + 40), (58, 52, 66, 255))
        x = 20
        for im in pieces:
            big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
            sheet.alpha_composite(big, (x, 20))
            x += big.width + 20
        sheet.save(a.previa)
        print('prévia', a.previa, sheet.size)


if __name__ == '__main__':
    main()
