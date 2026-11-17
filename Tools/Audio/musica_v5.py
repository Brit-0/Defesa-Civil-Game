"""MÃºsica da v5 (#523): um loop de 16 compassos a 100 BPM em dÃ³ maior, em 4 camadas do mesmo tamanho que tocam juntas
(o AudioManager sobe cada uma pelo caos), mais as vinhetas de vitÃ³ria e derrota e o arpejo de conserto pronto.

Instrumentos: marimba por sÃ­ntese modal (com o toque gravado de um bloco de madeira), pad e metais aditivos, baixo,
corda dedilhada (Karplus-Strong); percussÃ£o gravada da Gamemaster (chocalho de sementes, bloco de madeira, relÃ³gio,
tambor de metal). Cada camada tem a cauda do reverb dobrada no comeÃ§o: o loop nÃ£o tem emenda.

  C:\\Users\\PIETRO\\analises-lilith\\.venv-video\\Scripts\\python.exe -X utf8 Tools\\Audio\\musica_v5.py
"""
import json

import numpy as np

from audio_kit import (ASSETS, RAIZ, SR, aparar_cauda, celular, credito, cortar_antes_do_pico, dobrar_cauda, estereo,
                       fade, fontes_usadas, ganho, gm, gravar, integrado, limitar, misturar, mono, nivelar, passa_alta,
                       passa_baixa, passa_banda, presenca, reverb, tom)

BPM = 100
BATIDA = 60 / BPM
COMPASSO = 4 * BATIDA
COMPASSOS = 16
N = int(round(COMPASSOS * COMPASSO * SR))
PASTA = ASSETS / 'Audio' / 'V5' / 'Musica'
RNG = np.random.default_rng(523)

# Um acorde por compasso: (baixo, notas do pad) em MIDI
ACORDES = [(36, [60, 64, 67, 71]), (33, [57, 60, 64, 67]), (41, [53, 57, 60, 64]), (43, [55, 59, 62, 67]),
           (36, [60, 64, 67, 71]), (40, [52, 55, 59, 62]), (41, [53, 57, 60, 64]), (43, [55, 59, 62, 67]),
           (33, [57, 60, 64, 67]), (40, [52, 55, 59, 62]), (41, [53, 57, 60, 64]), (36, [55, 60, 64, 67]),
           (38, [50, 53, 57, 60]), (33, [57, 60, 64, 67]), (41, [53, 57, 60, 65]), (43, [55, 59, 62, 65])]


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


def t(n):
    return np.arange(n) / SR


def G(rel, som):
    credito(som, 'Gamemaster Pro Sound Collection: ' + rel)
    return gm(rel)


# ---------------------------------------------------------------- INSTRUMENTOS

def marimba(midi, forca=1.0, dur=None):
    """Barra de marimba: parciais 1, 3,99 e 10,65 com quedas diferentes + o toque gravado da baqueta."""
    f = hz(midi)
    queda = 1.6 * (523.0 / f) ** 0.5
    dur = dur or min(2.5, queda * 1.2)
    tt = t(int(dur * SR))
    x = np.zeros_like(tt)
    for razao, amp, q in ((1.0, 1.0, queda), (3.99, 0.32 * forca, queda * 0.22), (10.65, 0.1 * forca, queda * 0.06)):
        if f * razao < SR / 2.2:
            x += amp * np.sin(2 * np.pi * f * razao * tt + RNG.uniform(0, 0.3)) * np.exp(-tt * 6.9 / q)
    ataque = np.minimum(1, tt / 0.0015)
    toque = tom(TOQUE, 12 * np.log2(f / 700)) if abs(12 * np.log2(f / 700)) < 30 else TOQUE
    x = x * ataque
    x[:len(toque)] += toque[:len(x)] * 0.05 * forca
    return x * forca


def aditivo(f, dur, harm=8, queda_harm=1.2, desafino=(-6, 6), ataque=0.5, solta=0.8, brilho=1.0):
    """Onda serrilhada de banda limitada (soma de harmÃ´nicos), vozes desafinadas, envelope lento."""
    n = int((dur + solta) * SR)
    tt = t(n)
    x = np.zeros(n)
    for cents in desafino:
        fv = f * 2 ** (cents / 1200)
        for k in range(1, harm + 1):
            if fv * k > SR / 2.5:
                break
            x += np.sin(2 * np.pi * fv * k * tt + RNG.uniform(0, 2 * np.pi)) * (brilho ** (k - 1)) / k ** queda_harm
    env = np.minimum(1, tt / max(ataque, 1e-3))
    fim = np.clip((tt - dur) / max(solta, 1e-3), 0, 1)
    return x * env * np.cos(fim * np.pi / 2) ** 2 / len(desafino)


def baixo(midi, dur, forca=1.0, curto=False):
    f = hz(midi)
    tt = t(int((dur + 0.15) * SR))
    x = np.sin(2 * np.pi * f * tt) + 0.45 * np.sin(4 * np.pi * f * tt) + 0.2 * np.sin(6 * np.pi * f * tt) + \
        0.08 * np.sin(8 * np.pi * f * tt)
    if curto:
        env = np.minimum(1, tt / 0.004) * np.exp(-tt * 5.5)
    else:
        env = np.minimum(1, tt / 0.05) * (0.75 + 0.25 * np.exp(-tt * 3))
    env *= np.cos(np.clip((tt - dur) / 0.15, 0, 1) * np.pi / 2) ** 2
    return np.tanh(1.4 * x * env) * forca


def dedilhado(midi, dur=0.5, abafar=0.55, forca=1.0):
    """Karplus-Strong por blocos de um perÃ­odo (rÃ¡pido em numpy); abafar perto de 0,5 = corda abafada."""
    f = hz(midi)
    p = int(SR / f)
    n = int(dur * SR)
    y = np.zeros(n + p + 1)
    y[:p + 1] = RNG.uniform(-1, 1, p + 1)
    y[:p + 1] = np.convolve(y[:p + 1], np.ones(3) / 3, 'same')
    perda = 0.996 ** (220 / f)
    i = p + 1
    while i < len(y):
        j = min(len(y), i + p)
        y[i:j] = perda * (abafar * y[i - p:j - p] + (1 - abafar) * y[i - p - 1:j - p - 1])
        i = j
    x = y[p + 1:p + 1 + n]
    return fade(x * forca, 0.002, min(0.08, dur / 3))


def bumbo(forca=1.0):
    tt = t(int(0.4 * SR))
    freq = 45 + 75 * np.exp(-tt * 30)
    x = np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-tt * 9)
    clique = passa_alta(RNG.normal(0, 1, len(tt)), 2500) * np.exp(-tt * 400) * 0.15
    return (x + clique) * forca


def tambor_grave(forca=1.0):
    tt = t(int(0.9 * SR))
    freq = 62 + 40 * np.exp(-tt * 18)
    x = np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-tt * 4.5)
    pele = passa_baixa(TAMBOR, 900)[:len(tt)]
    x[:len(pele)] += pele * 0.6
    return np.tanh(1.5 * x) * forca


def colocar(saida, x, inicio, ganho_db=0.0, pan=0.0):
    """Soma `x` em `saida` a partir de `inicio` s (dando a volta no loop); pan -1 esquerda, 1 direita."""
    i = max(0, int(round(inicio * SR)))
    x = ganho(x, ganho_db)
    if saida.ndim == 2:
        if x.ndim == 1:
            esq, dir_ = np.cos((pan + 1) * np.pi / 4), np.sin((pan + 1) * np.pi / 4)
            x = np.stack([x * esq * 1.414, x * dir_ * 1.414], axis=1)
    elif x.ndim == 2:
        x = mono(x)
    fim = i + len(x)
    if fim > len(saida):
        x = x[:len(saida) - i]
    saida[i:i + len(x)] += x


def humano(s=0.008):
    return RNG.uniform(-s, s)


# ---------------------------------------------------------------- CAMADAS

def camada_base():
    """Calma: pad em maj7, marimba esparsa e baixo longo."""
    som = 'music_layer_0'
    pad = np.zeros((N + 4 * SR, 2))
    mar = np.zeros((N + 4 * SR, 2))
    bx = np.zeros(N + 4 * SR)
    padroes = [[0, 3, 5, 6], [0, 2, 3, 5, 6, 7]]
    for c, (raiz, notas) in enumerate(ACORDES):
        t0 = c * COMPASSO
        for nota in notas:
            for lado, des in ((0, (-7, 3)), (1, (-3, 8))):
                v = aditivo(hz(nota), COMPASSO, harm=7, queda_harm=1.5, desafino=des, ataque=0.7, solta=1.0, brilho=0.85)
                pad[int(t0 * SR):int(t0 * SR) + len(v), lado] += v * 0.16
        colocar(bx, baixo(raiz, COMPASSO * 0.95, 0.5), t0)
        tons = sorted(notas)
        for k, oitavo in enumerate(padroes[1 if c >= 8 else 0]):
            nota = tons[(k + c) % len(tons)] + 12 + (12 if oitavo == 7 else 0)
            forca = 0.55 + 0.35 * (oitavo in (0, 3)) + RNG.uniform(-0.08, 0.08)
            colocar(mar, marimba(nota, forca), t0 + oitavo * BATIDA / 2 + humano(), -2, pan=0.35 * np.sin(k * 1.7))
    pad = passa_baixa(pad, 2400)
    mar = reverb(mar, sala=0.55, umido=0.28, seco=0.85)
    mistura = pad + mar[:len(pad)] + estereo(passa_baixa(bx, 1400)) * 0.9
    credito(som, 'Gamemaster Pro Sound Collection: Miscellaneous/Wood/wood_block_sticks_hit_clap_03.wav (toque da marimba)')
    return mistura


def camada_ritmo():
    """Pulso: chocalho de sementes em colcheias, bloco de madeira no 2 e no 4, bumbo macio e baixo dedilhado."""
    som = 'music_layer_1'
    x = np.zeros((N + 3 * SR, 2))
    chocalhos = [passa_alta(G(f'Miscellaneous/Sprinkle Seeds Salt Pepper/shaker_sprinkle_seeds_cook_garden_0{i}.wav', som), 3000)
                 for i in (1, 2, 3)]
    chocalhos = [aparar_cauda(cortar_antes_do_pico(c, 0.03)) for c in chocalhos]
    blocos = [aparar_cauda(G(f'Miscellaneous/Wood/wood_block_sticks_hit_clap_0{i}.wav', som)) for i in (1, 4, 6)]
    for c, (raiz, _) in enumerate(ACORDES):
        t0 = c * COMPASSO
        for k in range(8):
            forte = k % 2 == 0
            colocar(x, chocalhos[RNG.integers(3)], t0 + k * BATIDA / 2 + humano(0.006), -7 if forte else -12, pan=0.4)
        for b in (1, 3):
            colocar(x, tom(blocos[RNG.integers(3)], -5), t0 + b * BATIDA + humano(0.004), -7, pan=-0.3)
        for b in (0, 2.5):
            colocar(x, bumbo(0.9 if b == 0 else 0.6), t0 + b * BATIDA, -9)
        for k, (passo, salto) in enumerate(((0, 0), (1.5, 7), (2, 12), (3, 7), (3.5, 0))):
            colocar(x, baixo(raiz + salto, BATIDA * 0.45, 0.55, curto=True), t0 + passo * BATIDA + humano(0.004), -3)
    # harmÃ´nicos do bumbo e do baixo: o pulso continua no alto-falante do notebook
    return presenca(passa_alta(x, 30), -6)


def camada_tensao():
    """TensÃ£o: corda abafada em semicolcheias, relÃ³gio tique-taque e tambor grave no fim de cada 2 compassos."""
    som = 'music_layer_2'
    x = np.zeros((N + 3 * SR, 2))
    tique = [aparar_cauda(G(f'Miscellaneous/Clock/clock_tick_0{i}.wav', som)) for i in (1, 2)]
    taque = [aparar_cauda(G(f'Miscellaneous/Clock/clock_tock_0{i}.wav', som)) for i in (1, 3)]
    for c, (raiz, notas) in enumerate(ACORDES):
        t0 = c * COMPASSO
        figura = [raiz + 24, raiz + 31, raiz + 36, raiz + 31]
        for k in range(16):
            nota = figura[k % 4] + (12 if k in (6, 14) else 0)
            forca = 0.9 if k % 4 == 0 else 0.6
            colocar(x, dedilhado(nota, 0.32, 0.52, forca), t0 + k * BATIDA / 4 + humano(0.004), -6, pan=-0.25)
        for k in range(8):
            clip = (tique if k % 2 == 0 else taque)[RNG.integers(2)]
            colocar(x, clip, t0 + k * BATIDA / 2, -15, pan=0.5)
        if c % 2 == 1:
            for b in (3.0, 3.5):
                colocar(x, tambor_grave(0.8), t0 + b * BATIDA, -5)
    return reverb(x, sala=0.4, umido=0.16, seco=0.92)[:len(x)]


def camada_perigo():
    """Perigo: metais graves nos tempos fortes, tambores, cordas em trÃªmulo e uma subida de ruÃ­do a cada 4 compassos."""
    x = np.zeros((N + 4 * SR, 2))
    for c, (raiz, notas) in enumerate(ACORDES):
        t0 = c * COMPASSO
        for b, forca in ((0, 1.0), (2.5, 0.7)):
            for nota in (raiz + 12, raiz + 19, raiz + 24):
                metal = aditivo(hz(nota), BATIDA * 0.7, harm=14, queda_harm=0.9, desafino=(-8, 0, 7), ataque=0.03,
                                solta=0.35, brilho=0.92)
                colocar(x, metal, t0 + b * BATIDA, -9 + 20 * np.log10(forca), pan=0.0)
        for b in (0, 1.5, 3):
            colocar(x, tambor_grave(1.0), t0 + b * BATIDA + humano(0.003), -4)
        for nota in sorted(notas)[-2:]:
            corda = aditivo(hz(nota + 12), COMPASSO, harm=9, queda_harm=1.1, desafino=(-5, 5), ataque=0.05, solta=0.3)
            tremolo = 0.6 + 0.4 * np.abs(np.sin(np.pi * np.arange(len(corda)) / SR / (BATIDA / 4)))
            colocar(x, corda * tremolo, t0, -16, pan=0.6 if nota % 2 else -0.6)
        if c % 4 == 2:
            dur = 2 * COMPASSO
            ruido = RNG.normal(0, 1, int(dur * SR))
            subida = np.zeros_like(ruido)
            for parte in range(8):
                a, b = parte * len(ruido) // 8, (parte + 1) * len(ruido) // 8
                subida[a:b] = passa_banda(ruido, 300 * 1.45 ** parte, 600 * 1.45 ** parte)[a:b]
            subida *= np.linspace(0, 1, len(subida)) ** 2
            colocar(x, subida, t0, -20)
    return reverb(passa_alta(x, 35), sala=0.5, umido=0.2, seco=0.9)[:len(x)]


# ---------------------------------------------------------------- VINHETAS E ARPEJO

def brilho(som):
    return passa_alta(G('Miscellaneous/Bell/chime_bell_05.wav', som), 2500)


def arpejo_conserto():
    saida = []
    for notas in ((72, 76, 79, 84), (76, 79, 84, 88), (79, 84, 88, 91)):
        x = np.zeros((int(1.6 * SR), 2))
        for k, nota in enumerate(notas):
            colocar(x, marimba(nota, 0.75 + 0.1 * k), 0.065 * k, 0, pan=-0.3 + 0.2 * k)
        colocar(x, marimba(notas[-1] + 12, 0.6), 0.26, -6, pan=0.3)
        colocar(x, brilho('repair_done'), 0.2, -15, pan=0.2)
        saida.append(aparar_cauda(mono(reverb(x, sala=0.35, umido=0.22))))
    return saida


def vinheta_vitoria():
    som = 'jingle_win'
    x = np.zeros((int(4.5 * SR), 2))
    for k, nota in enumerate((60, 64, 67, 72, 76)):
        colocar(x, marimba(nota + 12, 0.8), 0.07 * k, -1, pan=-0.4 + 0.2 * k)
    for nota in (48, 55, 60, 64, 67):
        colocar(x, aditivo(hz(nota), 1.4, harm=12, queda_harm=1.0, desafino=(-6, 0, 6), ataque=0.03, solta=1.2,
                           brilho=0.9), 0.42, -6)
        colocar(x, aditivo(hz(nota + 12), 2.2, harm=7, queda_harm=1.5, desafino=(-7, 7), ataque=0.3, solta=1.4), 0.42, -8)
    colocar(x, marimba(84, 1.0), 0.42, -2)
    colocar(x, bumbo(1.0), 0.42, -2)
    colocar(x, brilho(som), 0.45, -10, pan=0.3)
    chocalho = aparar_cauda(G('Miscellaneous/Sprinkle Seeds Salt Pepper/shaker_sprinkle_seeds_cook_garden_02.wav', som))
    for k in range(6):
        colocar(x, passa_alta(chocalho, 3000), 0.06 * k, -18 + 2 * k, pan=0.5)
    return [fade(reverb(x, sala=0.6, umido=0.25)[:int(3.6 * SR)], saida=0.8)]


def vinheta_derrota():
    som = 'jingle_lose'
    x = np.zeros((int(4.5 * SR), 2))
    for k, nota in enumerate((67, 63, 60, 55)):
        colocar(x, marimba(nota, 0.8 - 0.08 * k), 0.26 * k, -1, pan=0.3 - 0.2 * k)
    for nota in (36, 43, 48, 51):
        colocar(x, aditivo(hz(nota), 1.8, harm=10, queda_harm=1.1, desafino=(-9, 0, 8), ataque=0.15, solta=1.4,
                           brilho=0.85), 1.05, -5)
    colocar(x, tambor_grave(1.0), 1.05, -1)
    credito(som, 'Gamemaster Pro Sound Collection: Metal/metal_drum_impact_thud_03.wav (pele do tambor)')
    return [fade(reverb(passa_baixa(x, 5000), sala=0.7, umido=0.3)[:int(3.8 * SR)], saida=1.0)]


# ----------------------------------------------------------------

def gravar_camadas():
    camadas = [camada_base(), camada_ritmo(), camada_tensao(), camada_perigo()]
    alvos = (-22.0, -22.5, -22.0, -21.0)
    prontas = []
    for x, alvo in zip(camadas, alvos):
        x = dobrar_cauda(x, N)
        prontas.append(ganho(x, alvo - integrado(x)))
    soma = sum(prontas)
    pico = np.abs(soma).max()
    corte = min(0.0, -1.5 - 20 * np.log10(pico))
    for i, x in enumerate(prontas):
        x = limitar(ganho(x, corte), -1.0)
        gravar(x, PASTA / f'music_layer_{i}_01.ogg', ogg=True, qualidade=4)
        print(f'music_layer_{i}: {len(x) / SR:.3f}s {integrado(x):6.1f} LUFS pico {20 * np.log10(np.abs(x).max()):5.1f}')
    soma = sum(limitar(ganho(x, corte), -1.0) for x in prontas)
    print(f'todas juntas: {integrado(soma):6.1f} LUFS pico {20 * np.log10(np.abs(soma).max()):5.1f} (corte {corte:.1f} dB)')
    for k in range(1, 4):
        print(f'  camadas 0..{k - 1}: {integrado(sum(prontas[:k]) * 10 ** (corte / 20)):6.1f} LUFS')


def gravar_vinhetas():
    for nome, var, pasta, alvo in (('repair_done', arpejo_conserto(), 'Chamados', -16), ('jingle_win', vinheta_vitoria(), 'Musica', -14),
                                   ('jingle_lose', vinheta_derrota(), 'Musica', -15)):
        for i, x in enumerate(var, 1):
            x = passa_alta(x, 25)
            if celular(x) < 25:
                x = presenca(x, -6)
            x = nivelar(x, alvo, -1.0)
            destino = gravar(x, ASSETS / 'Audio' / 'V5' / pasta / f'{nome}_{i:02d}.wav')
            print(f'{destino.relative_to(RAIZ)} {len(x) / SR:.2f}s pico {20 * np.log10(np.abs(x).max()):5.1f}')


TOQUE = cortar_antes_do_pico(passa_alta(gm('Miscellaneous/Wood/wood_block_sticks_hit_clap_03.wav'), 1500), 0.0005)[:int(0.03 * SR)]
TAMBOR = cortar_antes_do_pico(gm('Metal/metal_drum_impact_thud_03.wav'), 0.002)

if __name__ == '__main__':
    gravar_vinhetas()
    gravar_camadas()
    for som in ('music_layer_0', 'repair_done', 'jingle_win'):
        credito(som, 'SÃ­ntese prÃ³pria (marimba modal, pad e metais aditivos, Karplus-Strong) em Tools/Audio/musica_v5.py')
    creditos = RAIZ / 'Tools' / 'Audio' / 'creditos_sons.json'
    antigos = json.loads(creditos.read_text('utf-8')) if creditos.exists() else {}
    antigos.update(fontes_usadas)
    creditos.write_text(json.dumps(antigos, ensure_ascii=False, indent=1), 'utf-8')
