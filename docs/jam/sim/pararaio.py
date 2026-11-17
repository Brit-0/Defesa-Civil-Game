"""Para-raio por quarteirão (docs/jam/PARARAIO.md): quanto vale proteger quarteirões dos raios da tempestade.

Cada compra do upgrade "rod" ergue, depois da obra, um para-raio no quarteirão livre com mais casas (o robô segue a dica
do jogo). Raio que cai em casa de quarteirão protegido é absorvido: nenhum dano, nenhum fogo. A "atração" é o peso
dessas casas no sorteio do raio (LightningRodData.attraction; 1 = o raio cai onde cairia). Os robôs compram os
para-raios logo depois da base nova (perfis.com_pararaio) e a tabela compara com quem não compra.

    python -X utf8 docs/jam/sim/pararaio.py                       # preço e atração dos .asset
    python -X utf8 docs/jam/sim/pararaio.py --custos 60,90 --atracoes 1,3,5 --quantos 1,2
"""
import argparse
import statistics

from dados import ROD_ASSET, ler_mapa, ler_pararaio, ler_upgrades
from perfis import com_pararaio, perfis
from sim import ler_balance
from tabela import faixa, novo_jogo


def rodar(balance, ups, mapa, jogador, sementes, limite, atracao):
    jogos = []
    for semente in range(sementes):
        j = jogador.copia() if jogador is not None else None
        jogo = novo_jogo(balance, ups, mapa, j, semente, limite)
        jogo.pararaio["atracao"] = atracao
        jogos.append(jogo.rodar())
    return jogos


def linha(nome, jogos):
    n = len(jogos)
    vit = [j.t for j in jogos if j.fim == "vitória"]
    raios = sum(j.raios for j in jogos)
    absorvidos = sum(j.absorvidos for j in jogos)
    obras = [j.estreias["para-raio"] / 60 for j in jogos if "para-raio" in j.estreias]
    return (f"| {nome} | {100 * len(vit) // n}% | {faixa(vit)} | {statistics.median(j.pico_caos for j in jogos):.2f} | "
            f"{statistics.median(j.consertos for j in jogos):.0f} | {raios / n:.1f} | {absorvidos / n:.1f} "
            f"({100 * absorvidos / max(1, raios):.0f}%) | {statistics.median(obras):.1f} |" if obras else
            f"| {nome} | {100 * len(vit) // n}% | {faixa(vit)} | {statistics.median(j.pico_caos for j in jogos):.2f} | "
            f"{statistics.median(j.consertos for j in jogos):.0f} | {raios / n:.1f} | - | - |")


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--sementes", type=int, default=30)
    p.add_argument("--limite", type=float, default=1800)
    p.add_argument("--custos", help="preços do 1º para-raio (padrão: o do Rod.asset)")
    p.add_argument("--atracoes", help="pesos da atração (padrão: o do LightningRodData.asset)")
    p.add_argument("--quantos", default="1,2", help="quantos para-raios o robô compra")
    p.add_argument("--jogadores", default="bom,médio,ruim")
    a = p.parse_args()

    balance, ups, mapa = ler_balance(), ler_upgrades(), ler_mapa()
    if "rod" not in ups:
        raise SystemExit(f"sem o upgrade rod em {ROD_ASSET}")
    custos = [int(x) for x in a.custos.split(",")] if a.custos else [ups["rod"]["baseCost"]]
    atracoes = [float(x) for x in a.atracoes.split(",")] if a.atracoes else [ler_pararaio()["atracao"]]
    quantos = [int(x) for x in a.quantos.split(",")]
    print(f"rod: {ups['rod']}; atração do asset {ler_pararaio()['atracao']}")

    print("\n| jogador e compra | vence | tempo da vitória | pico de caos | consertos | raios por partida | "
          "absorvidos | minuto do 1º para-raio |")
    print("|---|---|---|---|---|---|---|---|")
    todos = perfis()
    for nome in a.jogadores.split(","):
        base = todos[nome]
        print(linha(f"{nome}, sem para-raio", rodar(balance, ups, mapa, base, a.sementes, a.limite, 1.0)))
        for custo in custos:
            ups2 = dict(ups)
            ups2["rod"] = dict(ups["rod"], baseCost=custo)
            for atracao in atracoes:
                for n in quantos:
                    j = base.copia()
                    j.plano = com_pararaio(j.plano, n)
                    jogos = rodar(balance, ups2, mapa, j, a.sementes, a.limite, atracao)
                    print(linha(f"{nome}, {n} para-raio ({custo}, atração {atracao:g})", jogos))


if __name__ == "__main__":
    main()
