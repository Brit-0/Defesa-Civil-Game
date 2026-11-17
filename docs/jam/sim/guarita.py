"""Guaritas por quarteirão (docs/jam/GUARITAS.md): quanto vale automatizar o despacho só de alguns quarteirões.

v5A: o nível 4 da Automação (despacho sozinho na cidade inteira) saiu do jogo; no lugar, cada compra do upgrade
"outpost" ergue, depois da obra, uma guarita no quarteirão livre com mais casas (o robô escolhe assim). A guarita olha
o próprio quarteirão a cada OutpostData.dispatchInterval e manda a equipe sugerida ao chamado mais urgente que já
esperou o reactionDelay. Os robôs compram as guaritas logo depois da última automação (perfis.com_guaritas).

    python -X utf8 docs/jam/sim/guarita.py                        # preço do Outpost.asset, 0/1/3/5 guaritas
    python -X utf8 docs/jam/sim/guarita.py --custos 100,150,200 --crescimentos 1.3 --quantas 1,3,5
"""
import argparse
import statistics

from dados import OUTPOST_ASSET, ler_guarita, ler_mapa, ler_upgrades
from perfis import com_guaritas, perfis
from sim import ler_balance
from tabela import faixa, novo_jogo


def rodar(balance, ups, mapa, jogador, sementes, limite):
    jogos = []
    for semente in range(sementes):
        j = jogador.copia() if jogador is not None else None
        jogos.append(novo_jogo(balance, ups, mapa, j, semente, limite).rodar())
    return jogos


def linha(nome, jogos):
    n = len(jogos)
    vit = [j.t for j in jogos if j.fim == "vitória"]
    obras = [j.estreias["guarita"] / 60 for j in jogos if "guarita" in j.estreias]
    erguidas = statistics.mean(len(j.guaritas) for j in jogos)
    auto = sum(j.auto_despachos for j in jogos) / n
    consertos = statistics.median(j.consertos for j in jogos)
    return (f"| {nome} | {100 * len(vit) // n}% | {faixa(vit)} | {statistics.median(j.pico_caos for j in jogos):.2f} | "
            f"{consertos:.0f} | {erguidas:.1f} | {auto:.1f} | "
            f"{statistics.median(obras):.1f} |" if obras else
            f"| {nome} | {100 * len(vit) // n}% | {faixa(vit)} | {statistics.median(j.pico_caos for j in jogos):.2f} | "
            f"{consertos:.0f} | 0 | 0 | - |")


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--sementes", type=int, default=30)
    p.add_argument("--limite", type=float, default=1800)
    p.add_argument("--custos", help="preços da 1ª guarita (padrão: o do Outpost.asset)")
    p.add_argument("--crescimentos", help="crescimento do preço a cada guarita (padrão: o do Outpost.asset)")
    p.add_argument("--quantas", default="1,3,5", help="quantas guaritas o robô compra")
    p.add_argument("--jogadores", default="bom,médio,ruim")
    a = p.parse_args()

    balance, ups, mapa = ler_balance(), ler_upgrades(), ler_mapa()
    if "outpost" not in ups:
        raise SystemExit(f"sem o upgrade outpost em {OUTPOST_ASSET}")
    custos = [int(x) for x in a.custos.split(",")] if a.custos else [ups["outpost"]["baseCost"]]
    cresc = [float(x) for x in a.crescimentos.split(",")] if a.crescimentos else [ups["outpost"]["costGrowth"]]
    quantas = [int(x) for x in a.quantas.split(",")]
    print(f"outpost: {ups['outpost']}; dados {ler_guarita()}; automação máx. {ups['automation']['maxLevel']}")

    print("\n| jogador e compra | vence | tempo da vitória | pico de caos | consertos | guaritas erguidas | "
          "despachos sozinhos | minuto da 1ª guarita |")
    print("|---|---|---|---|---|---|---|---|")
    todos = perfis()
    for nome in a.jogadores.split(","):
        base = todos[nome]
        print(linha(f"{nome}, sem guarita", rodar(balance, ups, mapa, base, a.sementes, a.limite)))
        for custo in custos:
            for g in cresc:
                ups2 = dict(ups)
                ups2["outpost"] = dict(ups["outpost"], baseCost=custo, costGrowth=g)
                for n in quantas:
                    j = base.copia()
                    j.plano = com_guaritas(j.plano, n)
                    jogos = rodar(balance, ups2, mapa, j, a.sementes, a.limite)
                    print(linha(f"{nome}, {n} guarita ({custo}, ×{g:g})", jogos))


if __name__ == "__main__":
    main()
