"""Gera docs/jam/CREDITOS_AUDIO.md a partir de Tools/Audio/creditos_sons.json (escrito por sons_v5.py e musica_v5.py).

  python -X utf8 Tools\\Audio\\creditos_md.py
"""
import json
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
ENERGIA = {'power_down': 'sfx_blackout', 'power_restore': 'sfx_restore', 'energy_unlock': 'sfx_energy_on',
           'power_overload': 'sfx_overload', 'power_denied': 'sfx_no_energy', 'fuse_open': 'sfx_fuse_open',
           'fuse_switch': 'sfx_fuse_switch', 'fuse_wrong': 'sfx_fuse_wrong', 'fuse_done': 'sfx_fuse_done'}

CABECALHO = """# Créditos do áudio (v5D)

Gerado por `Tools/Audio/creditos_md.py` a partir de `Tools/Audio/creditos_sons.json`. Rodar de novo depois de mudar uma
receita em `sons_v5.py` ou `musica_v5.py`.

## Fontes e licenças

- **Gamemaster Audio - Pro Sound Collection v1.3** (pacote comprado pelo Pietro; decisão de 16/11 registrada em
  `docs/jam/STATUS_C.md`): uso livre de royalties em jogos; não se redistribui o pacote nem os arquivos soltos.
  Os sons do jogo são mixagens editadas (cortes, camadas, tom, filtros), não os arquivos originais.
- **Sonniss GDC 2026 Game Audio Bundle**: uso comercial sem obrigação de crédito; proibido revender os arquivos soltos
  e usar para treinar IA. Creditados aqui por cortesia, com o estúdio de cada coleção no caminho.
- **Síntese própria** (`Tools/Audio/musica_v5.py`): marimba modal, pad, metais e cordas aditivos, cordas dedilhadas
  (Karplus-Strong), bumbo e tambor sintéticos. Sem licença de terceiros.
- **Sons antigos que continuam no jogo** (`Assets/Audio/Sfx_Hover`, `Sfx_Whoosh`, `Sfx_Confetti`, `Sfx_Siren`,
  `Amb_Fire`): do pacote Gamemaster (tabela do T30 em `docs/jam/STATUS_C.md`).

Nota: o pedido citava CC0 ou síntese. Os efeitos ficaram nas gravações licenciadas acima porque o Pietro prefere som
gravado a som sintético; a música é síntese própria com percussão gravada do Gamemaster.

## Som por som
"""


def main():
    dados = json.loads((RAIZ / 'Tools' / 'Audio' / 'creditos_sons.json').read_text('utf-8'))
    linhas = [CABECALHO]
    for som in sorted(dados):
        nome = f'{som} (energia: `{ENERGIA[som]}.wav`)' if som in ENERGIA else f'`{som}`'
        linhas.append(f'- {nome}')
        for fonte in dados[som]:
            linhas.append(f'  - {fonte}')
    saida = RAIZ / 'docs' / 'jam' / 'CREDITOS_AUDIO.md'
    saida.write_text('\n'.join(linhas) + '\n', 'utf-8')
    print(f'{saida.relative_to(RAIZ)}: {len(dados)} sons')


if __name__ == '__main__':
    main()
