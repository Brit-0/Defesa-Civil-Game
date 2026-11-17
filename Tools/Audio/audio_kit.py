"""Ferramentas de áudio da v5 (sons e música do Defesa Civil): ler gravações, cortar, trocar o tom pela reamostragem,
filtrar, juntar camadas, nivelar pelo loudness e gravar wav/ogg leves para o WebGL.

Rodar com o venv de vídeo (numpy, scipy, soundfile, pyloudnorm, pedalboard):
  C:\\Users\\PIETRO\\analises-lilith\\.venv-video\\Scripts\\python.exe -X utf8 Tools\\Audio\\sons_v5.py
"""
import subprocess
from fractions import Fraction
from pathlib import Path

import numpy as np
import pyloudnorm
from scipy.signal import butter, resample_poly, sosfilt

SR = 44100
GM = Path(r'C:\Users\PIETRO\Projetos\sons-gamemaster\Gamemaster Audio - Pro Sound Collection v1.3 - 16bit 48k')
SON = Path(r'C:\Users\PIETRO\Downloads\_sonniss')
RAIZ = Path(__file__).resolve().parents[2]
ASSETS = RAIZ / 'Assets'

_cache = {}
fontes_usadas = {}          # id do som -> lista de fontes (créditos)


def ler(caminho, estereo=False):
    """Gravação inteira em float64 a 44,1 kHz (mono ou estéreo [n, 2]); fica em cache."""
    caminho = Path(caminho)
    chave = (str(caminho), estereo)
    if chave not in _cache:
        canais = 2 if estereo else 1
        r = subprocess.run(['ffmpeg', '-v', 'error', '-nostdin', '-i', str(caminho), '-map', '0:a:0', '-ac', str(canais),
                            '-ar', str(SR), '-f', 'f32le', '-'], capture_output=True, check=True)
        x = np.frombuffer(r.stdout, '<f4').astype(np.float64)
        _cache[chave] = x.reshape(-1, 2) if estereo else x
    return _cache[chave].copy()


def gm(rel, estereo=False):
    return ler(GM / rel, estereo)


def son(rel, estereo=False):
    return ler(SON / rel, estereo)


def seg(x, ini, fim=None):
    a = int(ini * SR)
    b = len(x) if fim is None else int(fim * SR)
    return x[a:b].copy()


def silencio(segundos, estereo=False):
    n = int(segundos * SR)
    return np.zeros((n, 2)) if estereo else np.zeros(n)


def ganho(x, db):
    return x * 10 ** (db / 20)


def fade(x, entrada=0.0, saida=0.0):
    x = x.copy()
    n = len(x)
    if entrada > 0:
        k = min(n, int(entrada * SR))
        curva = np.sin(np.linspace(0, np.pi / 2, k)) ** 2
        x[:k] = (x[:k].T * curva).T
    if saida > 0:
        k = min(n, int(saida * SR))
        curva = np.cos(np.linspace(0, np.pi / 2, k)) ** 2
        x[n - k:] = (x[n - k:].T * curva).T
    return x


def tom(x, semitons):
    """Troca o tom reamostrando (a duração muda junto, como numa fita)."""
    if abs(semitons) < 1e-6:
        return x.copy()
    razao = Fraction(2 ** (-semitons / 12)).limit_denominator(400)
    return resample_poly(x, razao.numerator, razao.denominator, axis=0)


def _filtro(x, tipo, freq, ordem=2):
    sos = butter(ordem, freq, btype=tipo, fs=SR, output='sos')
    return sosfilt(sos, x, axis=0)


def passa_alta(x, hz, ordem=2):
    return _filtro(x, 'highpass', hz, ordem)


def passa_baixa(x, hz, ordem=2):
    return _filtro(x, 'lowpass', hz, ordem)


def passa_banda(x, baixo, alto, ordem=2):
    return _filtro(x, 'bandpass', [baixo, alto], ordem)


def realce(x, hz, db, largura=1.0):
    """Sino simples: soma a banda em volta de `hz` com o ganho pedido (positivo realça, negativo tira)."""
    banda = passa_banda(x, hz / (1 + largura / 2), hz * (1 + largura / 2))
    return x + banda * (10 ** (db / 20) - 1)


def celular(x):
    """Parte da energia entre 300 e 6000 Hz (o que alto-falante de notebook e celular toca), em %."""
    y = mono(x)
    esp = np.abs(np.fft.rfft(y)) ** 2
    f = np.fft.rfftfreq(len(y), 1 / SR)
    return 100 * esp[(f > 300) & (f < 6000)].sum() / (esp.sum() + 1e-20)


def presenca(x, db=-3.0, corte=220.0):
    """Harmônicos do grave (saturação em paralelo) somados acima de `corte`: o tremor e o trovão aparecem no notebook."""
    grave = passa_baixa(x, corte)
    topo = np.abs(grave).max() + 1e-9
    harm = passa_alta(np.tanh(grave * 4 / topo) * topo, corte * 1.3, ordem=4)
    return x + ganho(harm, db)


def mono(x):
    return x.mean(axis=1) if x.ndim == 2 else x


def estereo(x, abertura=0.0):
    """Mono para estéreo; abertura > 0 atrasa um lado alguns ms (largura sem mudar o centro)."""
    if x.ndim == 2:
        return x
    d = int(abertura * SR)
    direita = np.concatenate([np.zeros(d), x[:len(x) - d]]) if d else x
    return np.stack([x, direita], axis=1)


def cortar_antes_do_pico(x, folga=0.010, limiar_db=-12.0):
    """Começa `folga` s antes do primeiro ataque forte (o golpe soa no quadro do evento, sem pré-ataque)."""
    env = np.abs(mono(x))
    lim = env.max() * 10 ** (limiar_db / 20)
    i = int(np.argmax(env > lim))
    return x[max(0, i - int(folga * SR)):]


def misturar(*camadas, estereo_saida=False):
    """camadas = (sinal, atraso_s, ganho_db); devolve a soma do tamanho da mais longa."""
    fim = max(int(a * SR) + len(s) for s, a, _ in camadas)
    saida = np.zeros((fim, 2)) if estereo_saida else np.zeros(fim)
    for s, a, g in camadas:
        if estereo_saida and s.ndim == 1:
            s = estereo(s)
        if not estereo_saida and s.ndim == 2:
            s = mono(s)
        i = int(a * SR)
        saida[i:i + len(s)] += ganho(s, g)
    return saida


def aparar_cauda(x, limiar_db=-60.0, folga=0.03):
    env = np.abs(mono(x))
    lim = env.max() * 10 ** (limiar_db / 20)
    acima = np.flatnonzero(env > lim)
    fim = (acima[-1] if len(acima) else len(x)) + int(folga * SR)
    return fade(x[:min(len(x), fim)], saida=min(folga, 0.02))


def reverb(x, sala=0.35, umido=0.18, seco=0.9, amortecer=0.5, largura=1.0):
    import pedalboard
    entrada = x if x.ndim == 2 else estereo(x)
    cauda = np.zeros((int(sala * 3 * SR), 2))
    entrada = np.concatenate([entrada, cauda]).astype(np.float32)
    pb = pedalboard.Pedalboard([pedalboard.Reverb(room_size=sala, wet_level=umido, dry_level=seco, damping=amortecer,
                                                  width=largura)])
    y = pb(entrada.T, SR).T.astype(np.float64)
    return y if x.ndim == 2 else mono(y)


def laco(x, cruzar=0.8):
    """Loop sem emenda: o fim cruza com o começo (potência constante) e o arquivo fica `cruzar` s mais curto."""
    k = int(cruzar * SR)
    corpo = x[:len(x) - k].copy()
    t = np.linspace(0, np.pi / 2, k)
    sai = np.cos(t) if x.ndim == 1 else np.cos(t)[:, None]
    entra = np.sin(t) if x.ndim == 1 else np.sin(t)[:, None]
    corpo[:k] = corpo[:k] * entra + x[len(x) - k:] * sai
    return corpo


def dobrar_cauda(x, comprimento):
    """Para música em loop: o que passa de `comprimento` amostras volta somado no começo."""
    corpo = x[:comprimento].copy()
    resto = x[comprimento:]
    while len(resto):
        corpo[:len(resto[:comprimento])] += resto[:comprimento]
        resto = resto[comprimento:]
    return corpo


def momentaneo_max(x):
    """Maior loudness momentâneo (janela 400 ms, passo 100 ms), em LUFS; som curto é completado com silêncio."""
    medidor = pyloudnorm.Meter(SR, block_size=0.4)
    y = x if x.ndim == 2 else x[:, None]
    n = int(0.4 * SR)
    if len(y) < n:
        y = np.concatenate([y, np.zeros((n - len(y), y.shape[1]))])
    maior = -120.0
    for i in range(0, len(y) - n + 1, int(0.1 * SR)):
        janela = y[i:i + n]
        if not np.any(janela):
            continue
        v = medidor.integrated_loudness(janela if janela.shape[1] > 1 else janela[:, 0])
        maior = max(maior, v)
    return maior


def integrado(x):
    medidor = pyloudnorm.Meter(SR)
    return medidor.integrated_loudness(x if x.ndim == 2 else x)


def nivelar(x, alvo_lufs=None, teto_db=-1.0, integrado_alvo=None):
    """Nivela pelo loudness (momentâneo máximo para one-shot, integrado para loop) e segura o pico no teto."""
    if integrado_alvo is not None:
        x = ganho(x, integrado_alvo - integrado(x))
    elif alvo_lufs is not None:
        x = ganho(x, alvo_lufs - momentaneo_max(x))
    pico = np.abs(x).max()
    teto = 10 ** (teto_db / 20)
    if pico > teto:
        x = limitar(x, teto_db)
    return x


def limitar(x, teto_db=-1.0):
    """Limitador macio: abaixo de -6 dB do teto passa igual; acima dobra suave (tanh) até o teto."""
    teto = 10 ** (teto_db / 20)
    joelho = teto * 0.5
    y = x.copy()
    a = np.abs(y)
    m = a > joelho
    y[m] = np.sign(y[m]) * (joelho + (teto - joelho) * np.tanh((a[m] - joelho) / (teto - joelho)))
    return y


def gravar(x, destino, ogg=False, qualidade=4):
    """wav 16 bit (efeitos) ou ogg vorbis (loops); cria a pasta. Devolve o caminho."""
    destino = Path(destino)
    destino.parent.mkdir(parents=True, exist_ok=True)
    y = np.clip(x, -1, 1).astype('<f4')
    canais = 2 if y.ndim == 2 else 1
    cmd = ['ffmpeg', '-v', 'error', '-nostdin', '-y', '-f', 'f32le', '-ar', str(SR), '-ac', str(canais), '-i', '-']
    cmd += ['-c:a', 'libvorbis', '-q:a', str(qualidade)] if ogg else ['-c:a', 'pcm_s16le']
    subprocess.run(cmd + ['-map_metadata', '-1', str(destino)], input=y.tobytes(), check=True)
    return destino


def credito(som, *fontes):
    fontes_usadas.setdefault(som, [])
    for f in fontes:
        if f not in fontes_usadas[som]:
            fontes_usadas[som].append(f)
