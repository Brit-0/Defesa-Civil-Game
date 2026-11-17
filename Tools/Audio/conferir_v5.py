"""Confere os sons da v5 sem escutar: pico (e pico entre amostras), amostras cortadas, DC, loudness, faixa do celular,
pré-ataque (som preso a evento), emenda dos loops e o tamanho igual das camadas da música. Grava a tabela em
docs/jam/SFX_V5_conferencia.txt e a folha de contato (forma de onda + espectro de cada som) em docs/jam/sfx_v5_folha.png.

  C:\\Users\\PIETRO\\analises-lilith\\.venv-video\\Scripts\\python.exe -X utf8 Tools\\Audio\\conferir_v5.py
Sai com código 1 se algum som reprovar.
"""
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.signal import resample_poly, spectrogram

from audio_kit import ASSETS, RAIZ, SR, integrado, ler, momentaneo_max

PASTAS = [ASSETS / 'Audio' / 'V5', ASSETS / 'Systems' / 'Energy' / 'Resources' / 'FireEnergy']
SAIDA_TXT = RAIZ / 'docs' / 'jam' / 'SFX_V5_conferencia.txt'
SAIDA_PNG = RAIZ / 'docs' / 'jam' / 'sfx_v5_folha.png'
LACOS = ('_loop', 'amb_day', 'amb_night', 'music_layer')
SINCRONIA = ('thunder_near', 'rod_absorb', 'quake_hit', 'ui_', 'repair_tool', 'call_', 'fire_ignite', 'team_')


def medir(arq):
    est = ler(arq, estereo=True)
    x = est.mean(axis=1)
    laco = any(k in arq.stem for k in LACOS)
    pico = np.abs(est).max()
    entre = np.abs(resample_poly(est, 4, 1, axis=0)).max()
    m = dict(arq=arq, dur=len(x) / SR, pico=20 * np.log10(pico + 1e-12), pico_real=20 * np.log10(entre + 1e-12),
             cortadas=int(np.sum(np.abs(est) >= 0.999)), dc=float(np.mean(x)), laco=laco,
             estereo=bool(np.abs(est[:, 0] - est[:, 1]).max() > 1e-4))
    m['lufs'] = integrado(est) if laco else momentaneo_max(est)
    esp = np.abs(np.fft.rfft(x[:SR * 20])) ** 2
    f = np.fft.rfftfreq(len(x[:SR * 20]), 1 / SR)
    m['celular'] = 100 * esp[(f > 300) & (f < 6000)].sum() / esp.sum()
    env = np.abs(x)
    m['pre_ataque'] = int(np.argmax(env > env.max() * 0.1)) / SR
    if laco:
        n = int(0.05 * SR)
        fim, ini = x[-n:], x[:n]
        m['emenda_db'] = abs(10 * np.log10((np.mean(fim ** 2) + 1e-12) / (np.mean(ini ** 2) + 1e-12)))
        m['salto'] = float(abs(x[-1] - x[0]) / (np.abs(x).max() + 1e-12))
    m['x'] = x
    return m


def veredito(m):
    erros = []
    if m['cortadas']:
        erros.append(f"{m['cortadas']} amostras cortadas")
    if m['pico_real'] > -0.5:
        erros.append(f"pico entre amostras {m['pico_real']:.1f} dB")
    if abs(m['dc']) > 0.01:
        erros.append(f"DC {m['dc']:.3f}")
    # música: o tempo forte do compasso 1 é mais alto que o fim do 16 por desenho; só o salto da amostra estala
    limite_emenda = 99 if m['arq'].stem.startswith('music_layer') else 6 if m['arq'].stem.startswith('amb_') else 3
    if m['laco'] and (m['emenda_db'] > limite_emenda or m['salto'] > 0.15):
        erros.append(f"emenda do loop {m['emenda_db']:.1f} dB / salto {m['salto']:.2f}")
    if any(m['arq'].stem.startswith(k) for k in SINCRONIA) and m['pre_ataque'] > 0.03:
        erros.append(f"pré-ataque {1000 * m['pre_ataque']:.0f} ms")
    return erros


def folha(medidas):
    colunas, lw, lh = 4, 420, 150
    linhas = (len(medidas) + colunas - 1) // colunas
    img = Image.new('RGB', (colunas * lw, 60 + linhas * lh), (18, 20, 26))
    d = ImageDraw.Draw(img)
    try:
        fonte = ImageFont.truetype('C:/Windows/Fonts/consola.ttf', 13)
        grande = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 22)
    except OSError:
        fonte = grande = ImageFont.load_default()
    d.text((12, 16), f'Defesa Civil v5D: folha de contato dos sons ({len(medidas)} arquivos) - forma de onda, espectro e medidas',
           fill=(240, 240, 240), font=grande)
    for k, m in enumerate(medidas):
        x0, y0 = (k % colunas) * lw, 60 + (k // colunas) * lh
        ok = not veredito(m)
        d.rectangle([x0 + 4, y0 + 4, x0 + lw - 4, y0 + lh - 4], outline=(70, 170, 90) if ok else (220, 70, 60), width=2)
        nome = m['arq'].stem if m['arq'].parent.name != 'FireEnergy' else 'energia/' + m['arq'].stem
        d.text((x0 + 10, y0 + 8), nome[:44], fill=(255, 255, 255), font=fonte)
        x = m['x']
        largura, alto, base = lw - 24, 46, y0 + 52
        blocos = np.array_split(x, largura)
        for i, b in enumerate(blocos):
            if len(b):
                a = int(min(1, np.abs(b).max()) * alto / 2)
                d.line([x0 + 12 + i, base - a, x0 + 12 + i, base + a], fill=(110, 190, 255))
        f, tt, s = spectrogram(x[:SR * 12], SR, nperseg=512, noverlap=256)
        s = 10 * np.log10(s[f < 12000] + 1e-12)
        s = np.clip((s - s.max() + 70) / 70, 0, 1)
        espec = Image.fromarray((np.flipud(s) * 255).astype(np.uint8)).resize((largura, 38)).convert('RGB')
        img.paste(espec, (x0 + 12, y0 + 80))
        tipo = 'LUFS int' if m['laco'] else 'LUFS max'
        d.text((x0 + 10, y0 + 122), f"{m['dur']:.2f}s  pico {m['pico']:.1f}  {m['lufs']:.1f} {tipo}  cel {m['celular']:.0f}%",
               fill=(200, 200, 200) if ok else (255, 120, 110), font=fonte)
    img.save(SAIDA_PNG, optimize=True)


def main():
    arqs = sorted(a for p in PASTAS for a in p.rglob('*') if a.suffix in ('.wav', '.ogg'))
    medidas = [medir(a) for a in arqs]
    linhas, reprovados, tamanho = [], 0, 0
    for m in medidas:
        erros = veredito(m)
        reprovados += bool(erros)
        tamanho += m['arq'].stat().st_size
        rel = m['arq'].relative_to(ASSETS)
        tipo = 'int' if m['laco'] else 'max'
        linhas.append(f"{'OK ' if not erros else 'NAO'} {str(rel):<62} {m['dur']:6.2f}s {'est' if m['estereo'] else 'mon'} "
                      f"pico {m['pico']:5.1f} real {m['pico_real']:5.1f} {m['lufs']:6.1f} LUFS {tipo} cel {m['celular']:3.0f}% "
                      f"ataque {1000 * m['pre_ataque']:4.0f}ms" + (f" emenda {m['emenda_db']:.1f}dB" if m['laco'] else '')
                      + ('  <- ' + '; '.join(erros) if erros else ''))
    camadas = [m for m in medidas if m['arq'].stem.startswith('music_layer')]
    tamanhos = {len(m['x']) for m in camadas}
    if len(tamanhos) > 1:
        reprovados += 1
        linhas.append(f'NAO camadas da música com tamanhos diferentes: {sorted(tamanhos)}')
    elif camadas:
        linhas.append(f'OK  {len(camadas)} camadas da música com {tamanhos.pop()} amostras cada (tocam juntas sem escorregar)')
    grupos = {}
    for m in medidas:
        if not m['laco']:
            grupos.setdefault(m['arq'].parent.name, []).append(m['lufs'])
    resumo = [f"{g}: {len(v)} sons, loudness máx. de {min(v):.1f} a {max(v):.1f} LUFS (mediana {np.median(v):.1f})"
              for g, v in sorted(grupos.items())]
    cab = [f'Conferência dos sons v5 ({len(medidas)} arquivos, {tamanho / 1e6:.2f} MB nos arquivos fonte), '
           f'{len(medidas) - reprovados} aprovados, {reprovados} reprovados', *resumo, '']
    SAIDA_TXT.write_text('\n'.join(cab + linhas) + '\n', 'utf-8')
    folha(medidas)
    print('\n'.join(cab))
    for linha in linhas:
        if linha.startswith('NAO'):
            print(linha)
    return 1 if reprovados else 0


if __name__ == '__main__':
    sys.exit(main())
