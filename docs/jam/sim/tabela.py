"""Roda o simulador com vários jogadores e sementes e imprime a tabela (Markdown) para o STATUS_B.md.

  python docs/jam/sim/tabela.py                       # padrões do C# (BalanceData.cs e JamDataMenu.cs)
  python docs/jam/sim/tabela.py --planos              # o jogador "bom" com cada plano de compra
  python docs/jam/sim/tabela.py --set workerSpeed=1.5 --set income.baseCost=300
  python docs/jam/sim/tabela.py --assets Assets/Resources   # valores dos .asset (o que o jogo carrega)
  python docs/jam/sim/tabela.py --mapa <outro city_layout.json>
  python docs/jam/sim/tabela.py --linha bom           # linha do tempo de uma partida (compras e desastres)
"""
import argparse
import random
import statistics
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from perfis import PLANOS, perfis  # noqa: E402
from sim import Jogo, aplicar_ajustes, ler_assets, ler_balance, ler_mapa, ler_upgrades  # noqa: E402


def mmss(segundos):
    if segundos is None:
        return "—"
    return f"{int(segundos // 60)}:{int(segundos % 60):02d}"


def faixa(valores):
    if not valores:
        return "—"
    valores = sorted(valores)
    baixo = valores[int(0.1 * (len(valores) - 1))]
    alto = valores[int(0.9 * (len(valores) - 1))]
    return f"{mmss(statistics.median(valores))} ({mmss(baixo)}–{mmss(alto)})"


def rodar_perfil(balance, ups, mapa, jogador, sementes, limite):
    jogos = []
    for semente in range(sementes):
        j = jogador.copia() if jogador is not None else None
        jogos.append(Jogo(balance, ups, mapa, j, semente, limite=limite).rodar())
    return jogos


def linha_tabela(nome, jogos):
    n = len(jogos)
    vit = [j.t for j in jogos if j.fim == "vitória"]
    der = [j.t for j in jogos if j.fim == "derrota"]
    return (f"| {nome} | {100 * len(vit) // n}% | {100 * len(der) // n}% | {faixa(vit)} | {faixa(der)} | "
            f"{statistics.median(j.consertos for j in jogos):.0f} | {statistics.median(j.ganho_total for j in jogos):.0f} | "
            f"{statistics.median(j.renda_total for j in jogos):.0f} | {statistics.median(j.pico_caos for j in jogos):.2f} |")


def buscar_planos(balance, ups, mapa, quantos, sementes, limite):
    """Teto do jogo: o jogador bom com N planos de compra sorteados; os 5 que vencem mais cedo."""
    sorteio = random.Random(1)
    ids = [uid for uid in ups if uid != "goal"]
    base = perfis()["bom"]
    resultados = []
    for _ in range(quantos):
        plano = sorteio.choices(ids, k=sorteio.randint(6, 24)) + ["goal"]
        jogador = base.copia()
        jogador.plano = plano
        jogos = rodar_perfil(balance, ups, mapa, jogador, sementes, limite)
        tempos = sorted(j.t if j.fim == "vitória" else limite * 2 for j in jogos)
        resultados.append((statistics.median(tempos), sum(j.fim == "vitória" for j in jogos), plano))
    resultados.sort(key=lambda r: r[0])
    for mediana, vitorias, plano in resultados[:5]:
        print(f"{mmss(mediana) if mediana < limite else 'não vence'} | vence {vitorias}/{sementes} | {' '.join(plano)}")


CABECALHO = ("| Jogador | Vence | Perde | Tempo até vencer, mediana (p10–p90) | Tempo até perder | Consertos | "
             "Dinheiro ganho | Do repasse | Pico do caos |\n|---|---|---|---|---|---|---|---|---|")

CABECALHO_MINI = ("| Jogador | Cliques/min | Pontos certos (erros) | Jatos apagados | Escoradas (de pé) | "
                  "Bônus de ocorrência | Marcos | Terremotos fortes |\n|---|---|---|---|---|---|---|---|")


def linha_mini(nome, jogos):
    """Revisão do gameloop: quanto o jogador joga (cliques por minuto) e o que os mini games e marcos rendem."""
    def med(f):
        return statistics.median(f(j) for j in jogos)
    cpm = med(lambda j: j.jogador.cliques / max(1.0, j.t / 60) if j.jogador else 0)
    fortes = med(lambda j: sum(1 for d in j.desastres if d[1].endswith("forte")))
    return (f"| {nome} | {cpm:.0f} | {med(lambda j: j.criticos):.0f} ({med(lambda j: j.erros):.0f}) | "
            f"{med(lambda j: j.herois):.0f} | {med(lambda j: j.escoradas):.0f} ({med(lambda j: j.de_pe):.0f}) | "
            f"{med(lambda j: j.bonus_ocorrencia):.0f} | {med(lambda j: j.marcos):.0f} | {fortes:.0f} |")


CABECALHO_FOGO = ("| Jogador | Jatos apagados | Fogos que cresceram | Gerador (nível) | Apagões (religou) | "
                  "Segundos sem energia | Segundos em sobrecarga | Negados |\n|---|---|---|---|---|---|---|---|")


def linha_fogo(nome, jogos):
    """Fogo e energia (ENERGIA_FOGO.md): o jato, o fogo que cresce, o gerador e os apagões."""
    def med(f):
        return statistics.median(f(j) for j in jogos)
    return (f"| {nome} | {med(lambda j: j.herois):.0f} | {med(lambda j: j.fogos_cresceram):.0f} | "
            f"{med(lambda j: j.niveis.get('generator', 0)):.0f} | {med(lambda j: j.rede.apagoes):.0f} "
            f"({med(lambda j: j.rede.religados):.0f}) | {med(lambda j: j.rede.segundos_sem_energia):.0f} | "
            f"{med(lambda j: j.rede.sobrecarga_s):.0f} | {med(lambda j: j.negados):.0f} |")


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--sementes", type=int, default=30)
    p.add_argument("--limite", type=float, default=1800, help="segundos de partida até desistir")
    p.add_argument("--set", action="append", default=[], help="campo=valor ou upgrade.campo=valor")
    p.add_argument("--assets", help="pasta Assets/Resources com os .asset do jogo")
    p.add_argument("--mapa", help="outro city_layout.json")
    p.add_argument("--plano", default="carrinho", choices=sorted(PLANOS), help="plano de compra do bom e do médio")
    p.add_argument("--planos", action="store_true", help="compara os planos de compra do jogador bom")
    p.add_argument("--linha", help="imprime a linha do tempo da semente 0 deste jogador")
    p.add_argument("--busca", type=int, default=0, help="sorteia N planos de compra e mostra os 5 melhores (o teto)")
    a = p.parse_args()

    balance, ups = ler_balance(), ler_upgrades()
    if a.assets:
        balance, ups = ler_assets(a.assets, balance, ups)
    balance, ups = aplicar_ajustes(balance, ups, a.set)
    mapa = ler_mapa(a.mapa) if a.mapa else ler_mapa()

    armazens = ", ".join(f"{x['dist']:.1f}" for x in mapa["armazens"])
    print(f"{len(mapa['casas'])} casas; Guarda → casa pela rua: mediana "
          f"{statistics.median(c['dist'] for c in mapa['casas']):.1f}; Guarda → Armazém: "
          f"{armazens}; {a.sementes} partidas por linha\n")

    if a.linha:
        jogador = perfis(a.plano)[a.linha]
        jogador = jogador.copia() if jogador else None
        jogo = Jogo(balance, ups, mapa, jogador, 0, limite=a.limite).rodar()
        for minuto, uid, nivel in jogo.compras:
            print(f"{mmss(minuto * 60)} compra {uid} {nivel}")
        for minuto, tipo, n in jogo.desastres:
            print(f"{mmss(minuto * 60)} {tipo} ({n})")
        print(f"fim: {jogo.fim} aos {mmss(jogo.t)}; consertos {jogo.consertos}; ganho {jogo.ganho_total}; "
              f"negados {jogo.negados}; cliques {jogador.cliques if jogador else 0}; pontos certos {jogo.criticos} "
              f"(erros {jogo.erros}); heróis {jogo.herois}; escoradas {jogo.escoradas} (de pé {jogo.de_pe}); "
              f"bônus de ocorrência {jogo.bonus_ocorrencia}; marcos {jogo.marcos}; fogos que cresceram "
              f"{jogo.fogos_cresceram}; apagões {jogo.rede.apagoes} (religou {jogo.rede.religados})")
        return

    if a.busca:
        buscar_planos(balance, ups, mapa, a.busca, a.sementes, a.limite)
        return

    print(CABECALHO)
    if a.planos:
        for nome in sorted(PLANOS):
            jogador = perfis(nome)["bom"]
            print(linha_tabela(f"bom, plano {nome}", rodar_perfil(balance, ups, mapa, jogador, a.sementes, a.limite)))
        return

    resultados = {nome: rodar_perfil(balance, ups, mapa, jogador, a.sementes, a.limite)
                  for nome, jogador in perfis(a.plano).items()}
    for nome, jogos in resultados.items():
        print(linha_tabela(nome, jogos))
    print("\n" + CABECALHO_MINI)
    for nome, jogos in resultados.items():
        if jogos[0].jogador is not None:
            print(linha_mini(nome, jogos))
    print("\n" + CABECALHO_FOGO)
    for nome, jogos in resultados.items():
        print(linha_fogo(nome, jogos))


if __name__ == "__main__":
    main()
