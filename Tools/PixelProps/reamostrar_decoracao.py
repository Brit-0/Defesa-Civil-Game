"""Reamostra a decoração encolhida para o tamanho em que ela aparece no mapa (#526, 17/11/2026).

A reforma do mapa (f803e45) pôs árvore a 0,62, arbusto a 0,45, flor a 0,38 etc. (SpriteCatalog.DecorationScales).
Com filtro Point, encolher na tela some com texels: o contorno de 1 px quebra e o desenho deforma, e cada texel
da decoração fica menor que o das casas. Aqui cada folha (8 direções) e o seu _n.png viram a resolução exibida,
com contorno preto de 1 px refeito, e o PPU da textura cai na mesma proporção (32 * nova/antiga): o tamanho no
mundo da Image e do espelho [N] não muda, sem mexer na Gameplay.unity. Roda uma vez; folha já feita é pulada.

    python -I Tools/PixelProps/reamostrar_decoracao.py [--so NOME] [--previa PASTA]
--previa grava antes/depois ampliados (x4) em PASTA sem tocar nos assets.
"""
import argparse
import re
from collections import defaultdict
from pathlib import Path

import numpy as np
from PIL import Image

PROJ = Path(__file__).resolve().parents[2]
ROOT = PROJ / "Assets" / "Arts" / "Houses"
CATALOG = PROJ / "Assets" / "_Project" / "Scripts" / "Editor" / "CityGen" / "SpriteCatalog.cs"
PPU = 32
FUNDO_N = (128, 128, 255, 0)
MARCA = "reamostrado"


def regras():
    """Escalas e exclusões lidas do SpriteCatalog.cs, a mesma fonte do Build."""
    cs = CATALOG.read_text(encoding="utf-8")
    escalas = [(re.compile(p), float(s)) for p, s in re.findall(r'\(new Regex\("([^"]+)"\), ([\d.]+)f\)', cs)]
    excl = re.compile(re.search(r'NatureExcluded = new Regex\(\s*"([^"]+)"', cs).group(1))
    return escalas, excl


def arred(v):
    return int(np.floor(v + 0.5))


def tamanho(cw, ch, s):
    """Célula nova: texel o mais perto do das casas, com a largura no mundo (PPU pela altura) a menos de 0,008 u da
    antiga, dentro da tolerância de tamanho do validador da cena (R18, 0,01 u)."""
    melhor = None
    for w2 in range(arred(cw * s) - 1, arred(cw * s) + 2):
        for h2 in range(arred(ch * s) - 1, arred(ch * s) + 2):
            if s / PPU * abs(w2 * ch / h2 - cw) > 0.008:
                continue
            erro = max(abs(s * cw / w2 - 1), abs(s * ch / h2 - 1))
            if melhor is None or erro < melhor[0]:
                melhor = (erro, w2, h2)
    return (melhor[1], melhor[2]) if melhor else (arred(cw * s), arred(ch * s))


def caixas(n, n2):
    """Para cada pixel de destino, os índices de origem cujo centro cai nele e o peso (centro vale mais)."""
    passo = n / n2
    out = []
    for t in range(n2):
        a, b = t * passo, (t + 1) * passo
        meio, raio = (a + b) / 2, passo / 2
        idx = [i for i in range(n) if a <= i + 0.5 < b] or [min(n - 1, int(meio))]
        out.append([(i, 2.0 - abs(i + 0.5 - meio) / raio) for i in idx])
    return out


def reamostra(cor, nrm, w2, h2):
    """Célula RGBA (linhas de cima para baixo) e o normal map dela, para w2 x h2."""
    h, w = cor.shape[:2]
    op = cor[..., 3] > 127
    pad = np.pad(op, 1)
    contorno = op & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
    cy, cx = caixas(h, h2), caixas(w, w2)
    novo = np.zeros((h2, w2, 4), np.uint8)
    nn = np.zeros((h2, w2, 4), np.uint8)
    nn[:] = FUNDO_N
    cor_viva = np.zeros((h2, w2), bool)
    vec =nrm[..., :3].astype(np.float32) / 127.5 - 1.0
    for ty in range(h2):
        for tx in range(w2):
            total = opaco = 0.0
            votos, votos_c = defaultdict(float), defaultdict(float)
            soma_n = np.zeros(3, np.float32)
            for y, wy in cy[ty]:
                for x, wx in cx[tx]:
                    p = wy * wx
                    total += p
                    if not op[y, x]:
                        continue
                    opaco += p
                    soma_n += vec[y, x] * p
                    chave = tuple(cor[y, x, :3])
                    (votos_c if contorno[y, x] else votos)[chave] += p
            if opaco * 2 < total:
                continue
            escolha = max(votos or votos_c, key=(votos or votos_c).get)
            novo[ty, tx] = (*escolha, 255)
            cor_viva[ty, tx] = max(votos.values(), default=0.0) >= sum(votos_c.values())
            n = soma_n / max(1e-6, float(np.linalg.norm(soma_n)))
            nn[ty, tx] = (*np.clip(np.rint((n + 1.0) * 127.5), 0, 255).astype(np.uint8), 255)
    # contorno preto de 1 px na borda da nova silhueta (como na arte original); folha ou haste fina demais
    # (nenhum vizinho de dentro) fica com a cor, senão vira só contorno e some
    op2 = novo[..., 3] > 0
    pad = np.pad(op2, 1)
    borda = op2 & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
    dentro = np.pad(op2 & ~borda, 1)
    fina = borda & ~(dentro[:-2, 1:-1] | dentro[2:, 1:-1] | dentro[1:-1, :-2] | dentro[1:-1, 2:])
    novo[borda & ~(fina & cor_viva)] = (0, 0, 0, 255)
    return novo, nn


RECT = re.compile(r"(rect:\s*\n\s*serializedVersion: 2\s*\n\s*x: )(\S+)(\s*\n\s*y: )(\S+)(\s*\n\s*width: )(\S+)(\s*\n\s*height: )(\S+)")
NOME_RECT = re.compile(r"name: (\S+)\s*\n\s*rect:\s*\n\s*serializedVersion: 2\s*\n\s*x: (\S+)\s*\n\s*y: (\S+)")


def visivel(celula):
    """Retângulo visível (alfa > 20) em pixels, com y de baixo para cima como no SpriteCatalog.Measure."""
    h = celula.shape[0]
    ys, xs = np.nonzero(celula[..., 3] > 20)
    if len(xs) == 0:
        return 0, 0, celula.shape[1] - 1, h - 1
    return int(xs.min()), int(h - 1 - ys.max()), int(xs.max()), int(h - 1 - ys.min())


def processa(png, s, previa=None):
    meta_p = png.with_name(png.name + ".meta")
    bruto = meta_p.read_bytes()
    fim = "\r\n" if b"\r\n" in bruto else "\n"
    meta = bruto.decode("utf-8").replace("\r\n", "\n")
    if MARCA in meta or not re.search(r"spritePixelsToUnits: 32\s*\n", meta):
        return f"pulada (já feita) {png.stem}"
    rects = [tuple(float(m.group(k)) for k in (2, 4, 6, 8)) for m in RECT.finditer(meta)]
    cw, ch = rects[0][2], rects[0][3]
    assert all(r[2] == cw and r[3] == ch and r[0] % cw == 0 and r[1] % ch == 0 for r in rects), f"células irregulares em {png}"
    cw, ch = int(cw), int(ch)
    cw2, ch2 = tamanho(cw, ch, s)
    ppu2 = PPU * ch2 / ch
    cor = np.array(Image.open(png).convert("RGBA"))
    nrm = np.array(Image.open(png.with_name(png.stem + "_n.png")).convert("RGBA"))
    H, W = cor.shape[:2]
    # medidas da arte original do _S: o catálogo segue com elas e a cidade sai igual (posição, ordem por Y, R18, R30)
    sx, sy = next((int(float(x)), int(float(y))) for n, x, y in NOME_RECT.findall(meta) if n == f"{png.stem}_S")
    vis = visivel(cor[H - sy - ch:H - sy, sx:sx + cw])
    H2, W2 = H // ch * ch2, W // cw * cw2
    cor2 = np.zeros((H2, W2, 4), np.uint8)
    nrm2 = np.zeros((H2, W2, 4), np.uint8)
    nrm2[:] = FUNDO_N
    for x, y, _, _ in rects:
        x, y = int(x), int(y)
        x2, y2 = x // cw * cw2, y // ch * ch2
        a = cor[H - y - ch:H - y, x:x + cw]
        b = nrm[H - y - ch:H - y, x:x + cw]
        c2, n2 = reamostra(a, b, cw2, ch2)
        cor2[H2 - y2 - ch2:H2 - y2, x2:x2 + cw2] = c2
        nrm2[H2 - y2 - ch2:H2 - y2, x2:x2 + cw2] = n2
    linha = f"{png.stem} s={s} {cw}x{ch} -> {cw2}x{ch2} ppu={ppu2:g} texel x={s * cw / cw2:.3f} y={s * ch / ch2:.3f}"
    if previa:
        _previa(previa, png.stem, cor, cor2, cw, ch, cw2, ch2, s)
        return linha
    Image.fromarray(cor2).save(png)
    Image.fromarray(nrm2).save(png.with_name(png.stem + "_n.png"))

    def troca(m):
        x, y, w, h = (float(m.group(k)) for k in (2, 4, 6, 8))
        return f"{m.group(1)}{int(x) // cw * cw2}{m.group(3)}{int(y) // ch * ch2}{m.group(5)}{cw2}{m.group(7)}{ch2}"

    meta = RECT.sub(troca, meta)
    meta = re.sub(r"spritePixelsToUnits: 32(\s*\n)", lambda m: f"spritePixelsToUnits: {ppu2:g}{m.group(1)}", meta, count=1)
    meta = re.sub(r"(\n  userData:)[^\n]*", lambda m: f"{m.group(1)} {MARCA} {cw}x{ch} por {s:g} vis={','.join(map(str, vis))} (#526)", meta, count=1)
    meta_p.write_bytes(meta.replace("\n", fim).encode("utf-8"))
    return linha


def _previa(pasta, nome, cor, cor2, cw, ch, cw2, ch2, s):
    """Célula _S: original, o que a tela fazia (vizinho mais próximo a s) e a reamostrada, lado a lado x4."""
    a = Image.fromarray(cor[:ch, :cw])  # a célula de cima é a _S (y maior no .meta)
    b = Image.fromarray(cor2[:ch2, :cw2])
    perto = a.resize((cw2, ch2), Image.NEAREST)
    k = 4
    tela = Image.new("RGBA", (k * (cw + 2 * cw2 + 8), k * ch), (90, 140, 80, 255))
    tela.paste(a.resize((cw * k, ch * k), Image.NEAREST), (0, 0), a.resize((cw * k, ch * k), Image.NEAREST))
    for i, im in enumerate((perto, b)):
        big = im.resize((cw2 * k, ch2 * k), Image.NEAREST)
        tela.paste(big, (k * (cw + 4 + i * (cw2 + 4)), k * (ch - ch2)), big)
    Path(pasta).mkdir(parents=True, exist_ok=True)
    tela.save(Path(pasta) / f"{nome}.png")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--so")
    ap.add_argument("--previa")
    a = ap.parse_args()
    escalas, excl = regras()
    feitas = 0
    for png in sorted(ROOT.glob("*/*/*.png")):
        cat, tex = png.parent.parent.name, png.parent.name
        if png.stem != tex or cat == "Carros" or (cat == "Nature" and excl.match(tex)) or (a.so and tex != a.so):
            continue
        s = next((s for r, s in escalas if r.search(tex)), 1.0)
        if s == 1.0:
            continue
        print(processa(png, s, a.previa))
        feitas += 1
    print(f"folhas: {feitas}")


if __name__ == "__main__":
    main()
