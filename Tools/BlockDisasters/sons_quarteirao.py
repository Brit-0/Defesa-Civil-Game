"""Sons da enchente e do meteoro (docs/jam/ENCHENTE_METEORO.md), sintetizados (sem amostra de fora).

Saída em Assets/Systems/BlockDisasters/Resources/BlockDisasters/, mono 44,1 kHz 16 bits, com o .meta do AudioImporter
igual ao do Rod_Zap.wav (guid fixo por nome: regerar não quebra referência).
- Flood_Rush.wav: a água chegando (2,4 s): rumor de água que cresce e borbulha.
- Valve_Creak.wav: rangido e estalo metálico da válvula (0,2 s), a cada quarto de volta.
- Valve_Drain.wav: gorgolejo em loop (2 s, emenda sem estalo); o volume segue a pressão.
- Valve_Slip.wav: a falha leve (0,45 s): tranco de metal e respingo.
- Flood_Drained.wav: o ralo engolindo (1,3 s), bolhas descendo e o glub final.
- Meteor_Rumble.wav: ronco grave de um tremor do aviso (0,9 s).
- Meteor_Whoosh.wav: o assobio da queda (1,2 s), subindo até o impacto.
- Meteor_Impact.wav: a explosão (2,4 s): estrondo que afunda, poeira e estalos de entulho.

Uso: python -X utf8 Tools/BlockDisasters/sons_quarteirao.py
"""
import hashlib
import os
import wave

import numpy as np

RATE = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'Assets', 'Systems', 'BlockDisasters', 'Resources', 'BlockDisasters')

META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid(name):
    return hashlib.md5(('DefesaCivil.BlockDisasters.' + name).encode('utf-8')).hexdigest()


def t_axis(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def lowpass(x, cutoff):
    """Passa-baixa de um polo; cutoff pode ser número ou um vetor do tamanho do sinal."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    a = 1.0 - np.exp(-2.0 * np.pi * cutoff / RATE)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += a[i] * (x[i] - acc)
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def bandpass(x, low, high):
    return lowpass(highpass(x, low), high)


def env(t, attack, decay):
    """Envelope que sobe em 'attack' segundos e cai exponencialmente com constante 'decay'."""
    rise = np.clip(t / max(attack, 1e-4), 0.0, 1.0)
    fall = np.exp(-np.maximum(t - attack, 0.0) / decay)
    return rise * fall


def bubbles(rng, seconds, count, low, high, length=(0.02, 0.06), sweep=1.0, start=0.0, end=None):
    """Bolhas: senos curtos com o tom subindo (sweep > 1) ou descendo (< 1)."""
    out = np.zeros(int(seconds * RATE))
    end = seconds if end is None else end
    for _ in range(count):
        at = rng.uniform(start, max(start, end - 0.07))
        dur = rng.uniform(*length)
        n = int(dur * RATE)
        i = int(at * RATE)
        if i + n >= len(out):
            continue
        tt = np.arange(n) / RATE
        f0 = rng.uniform(low, high)
        freq = f0 * sweep ** (tt / dur)
        phase = 2 * np.pi * np.cumsum(freq) / RATE
        out[i:i + n] += np.sin(phase) * np.sin(np.pi * tt / dur) ** 2 * rng.uniform(0.4, 1.0)
    return out


def resonances(t, freqs, decays, amps):
    return sum(a * np.sin(2 * np.pi * f * t) * np.exp(-t / d) for f, d, a in zip(freqs, decays, amps))


def normalize(x, peak=0.9):
    x = x - np.mean(x)
    m = np.max(np.abs(x))
    return x * (peak / m) if m > 0 else x


def fade(x, fade_in=0.005, fade_out=0.02):
    n_in, n_out = int(fade_in * RATE), int(fade_out * RATE)
    if n_in:
        x[:n_in] *= np.linspace(0, 1, n_in)
    if n_out:
        x[-n_out:] *= np.linspace(1, 0, n_out)
    return x


def flood_rush(rng):
    t = t_axis(2.4)
    noise = rng.standard_normal(len(t))
    body = lowpass(noise, 700 + 500 * np.sin(2 * np.pi * 0.7 * t) ** 2)
    hiss = bandpass(noise, 1500, 4500) * 0.25
    swell = env(t, 0.7, 0.9)
    gurgle = bubbles(rng, 2.4, 45, 250, 700, sweep=1.6, start=0.3, end=2.1) * 0.35
    return fade((body + hiss) * swell + gurgle * swell)


def valve_creak(rng):
    t = t_axis(0.2)
    click = resonances(t, [920, 1530, 2310], [0.025, 0.018, 0.012], [1.0, 0.6, 0.4])
    tick = highpass(rng.standard_normal(len(t)), 2000) * np.exp(-t / 0.004) * 0.8
    # rangido: dente de serra grave que oscila
    f = 150 + 40 * np.sin(2 * np.pi * 30 * t)
    saw = ((np.cumsum(f) / RATE) % 1.0) * 2 - 1
    creak = lowpass(saw, 1800) * env(t, 0.03, 0.05) * 0.5
    return fade(click + tick + creak, 0.001, 0.02)


def valve_drain(rng):
    seconds, cross = 2.0, 0.3
    t = t_axis(seconds + cross)
    noise = rng.standard_normal(len(t))
    wobble = 0.6 + 0.4 * np.sin(2 * np.pi * 3.1 * t) * np.sin(2 * np.pi * 0.9 * t)
    body = lowpass(noise, 450) * wobble
    gurgle = bubbles(rng, seconds + cross, 70, 180, 520, sweep=1.8) * 0.5
    x = body + gurgle
    # emenda do loop: o rabo entra por cima do começo
    n = int(seconds * RATE)
    c = min(int(cross * RATE), len(x) - n)
    head = x[:c] * np.linspace(0, 1, c) + x[n:n + c] * np.linspace(1, 0, c)
    return np.concatenate([head, x[c:n]])


def valve_slip(rng):
    t = t_axis(0.45)
    clank = resonances(t, [610, 1170, 1890, 2750], [0.09, 0.06, 0.04, 0.03], [1.0, 0.7, 0.5, 0.3])
    hit = highpass(rng.standard_normal(len(t)), 1200) * np.exp(-t / 0.01)
    splash = bandpass(rng.standard_normal(len(t)), 900, 6000) * env(np.maximum(t - 0.05, 0), 0.02, 0.12)
    splash[t < 0.05] = 0
    return fade(clank * 0.8 + hit * 0.6 + splash * 0.7, 0.001, 0.03)


def flood_drained(rng):
    t = t_axis(1.3)
    noise = rng.standard_normal(len(t))
    swirl = lowpass(noise, 900 * np.exp(-t / 0.8) + 150) * env(t, 0.05, 0.6)
    down = bubbles(rng, 1.3, 40, 300, 800, sweep=0.5, end=1.0) * 0.6
    glub_t = np.maximum(t - 1.05, 0)
    glub = np.sin(2 * np.pi * (180 - 60 * glub_t / 0.2) * glub_t) * env(glub_t, 0.01, 0.06) * (t >= 1.05)
    return fade(swirl + down + glub * 0.9)


def meteor_rumble(rng):
    t = t_axis(0.9)
    noise = rng.standard_normal(len(t))
    low = lowpass(lowpass(noise, 140), 140)
    sub = np.sin(2 * np.pi * 42 * t + 2 * np.sin(2 * np.pi * 6 * t))
    return fade((normalize(low, 1.0) + sub * 0.7) * env(t, 0.12, 0.35))


def meteor_whoosh(rng):
    t = t_axis(1.2)
    noise = rng.standard_normal(len(t))
    k = t / t[-1]
    center = 250 * (12 ** k)
    band = lowpass(highpass(noise, center * 0.5), center * 1.6)
    whistle = np.sin(2 * np.pi * np.cumsum(500 + 1700 * k ** 2) / RATE) * 0.15
    amp = k ** 1.6
    return fade((band + whistle) * amp, 0.05, 0.01)


def meteor_impact(rng):
    t = t_axis(2.4)
    noise = rng.standard_normal(len(t))
    freq = 35 + 60 * np.exp(-t / 0.18)
    boom = np.sin(2 * np.pi * np.cumsum(freq) / RATE) * env(t, 0.004, 0.55)
    blast = lowpass(noise, 2500 * np.exp(-t / 0.2) + 200) * env(t, 0.002, 0.35)
    dust = lowpass(noise, 600) * env(t, 0.15, 0.8) * 0.4
    crackle = np.zeros(len(t))
    for _ in range(70):
        at = rng.uniform(0.15, 1.9)
        i = int(at * RATE)
        n = int(0.012 * RATE)
        if i + n < len(t):
            crackle[i:i + n] += highpass(rng.standard_normal(n), 1500) * np.exp(-np.arange(n) / (0.003 * RATE)) * rng.uniform(0.2, 0.8) * np.exp(-at / 0.9)
    return fade(boom * 1.2 + blast * 0.9 + dust + crackle * 0.6, 0.001, 0.1)


def write(name, samples):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.wav')
    data = (np.clip(normalize(samples), -1, 1) * 32767).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    meta = path + '.meta'
    if not os.path.exists(meta):
        with open(meta, 'w', encoding='utf-8', newline='\n') as f:
            f.write(META.format(guid=guid(name)))
    print(f'{name}.wav {len(data) / RATE:.2f} s {os.path.getsize(path)} bytes')


def main():
    sounds = [
        ('Flood_Rush', flood_rush), ('Valve_Creak', valve_creak), ('Valve_Drain', valve_drain),
        ('Valve_Slip', valve_slip), ('Flood_Drained', flood_drained), ('Meteor_Rumble', meteor_rumble),
        ('Meteor_Whoosh', meteor_whoosh), ('Meteor_Impact', meteor_impact),
    ]
    for i, (name, make) in enumerate(sounds):
        write(name, make(np.random.default_rng(521 + i)))


if __name__ == '__main__':
    main()
