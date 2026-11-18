"""Roda o simulador com vários jogadores e sementes e imprime a tabela (Markdown) para o STATUS_B.md.

  python docs/jam/sim/tabela.py                       # padrões do C# (BalanceData.cs e JamDataMenu.cs)
  python docs/jam/sim/tabela.py --planos              # o jogador "bom" com cada plano de compra
  python docs/jam/sim/tabela.py --set workerSpeed=1.5 --set income.baseCost=300
  python docs/jam/sim/tabela.py --assets Assets/Resources   # valores dos .asset (o que o jogo carrega)
  python docs/jam/sim/tabela.py --mapa <outro city_layout.json>
  python docs/jam/sim/tabela.py --linha bom           # linha do tempo de uma partida (compras e desastres)
  python docs/jam/sim/tabela.py --curva               # a curva dos dados minuto a minuto (RITMO_V3.md)
  python docs/jam/sim/tabela.py --sem-expansao        # os robôs não compram a base nova (por padrão compram uma)
  python docs/jam/sim/tabela.py --sem-estreia         # sem a estreia garantida do RITMO_V3 (o C# a faz desde a #534)
  python docs/jam/sim/tabela.py --curto               # planos curtos do RITMO_V3 (sem continuação, para-raio e guaritas)
Perfis principais: bom, médio e ruim (RITMO_V3, com os planos longos do BALANCEAMENTO_V5) e o muito eficiente
(ADAPTATIVA_V6B); a tabela também mostra a estreia de cada sistema, os chamados por minuto, o pico do caos e o ritmo
adaptativo em cada fase de 4 minutos. Sem a dificuldade adaptativa: --set paceIntervalCut=0 --set paceHouseBoost=0.
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


_CIDADE = {}


def novo_jogo(balance, ups, mapa, jogador, semente, limite):
    """Com base2/depot2 nos upgrades, a partida tem os terrenos da expansão (expansao.py); sem eles, o Jogo puro."""
    if "base2" not in ups and "depot2" not in ups:
        return Jogo(balance, ups, mapa, jogador, semente, limite=limite)
    from expansao import Cidade, JogoExpansao  # aqui dentro: expansao.py importa faixa daqui
    cidade = _CIDADE.setdefault(id(mapa), Cidade(mapa))
    return JogoExpansao(cidade, balance, ups, mapa, jogador, semente, limite=limite)


def rodar_perfil(balance, ups, mapa, jogador, sementes, limite):
    jogos = []
    for semente in range(sementes):
        j = jogador.copia() if jogador is not None else None
        jogos.append(novo_jogo(balance, ups, mapa, j, semente, limite).rodar())
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


ESTREIAS = ("fogo", "tempestade", "expansão", "para-raio", "enchente", "guarita", "energia", "terremoto forte",
            "meteoro")

CABECALHO_ESTREIA = ("| Jogador | " + " | ".join(ESTREIAS) + " |\n|---|" + "---|" * len(ESTREIAS))


def linha_estreia(nome, jogos):
    """RITMO_V3: quando cada sistema aparece pela primeira vez (mediana e p10–p90; % das partidas em que apareceu)."""
    celulas = []
    for chave in ESTREIAS:
        tempos = [j.estreias[chave] for j in jogos if chave in j.estreias]
        if not tempos:
            celulas.append("—")
            continue
        texto = faixa(tempos)
        if len(tempos) < len(jogos):
            texto += f" em {100 * len(tempos) // len(jogos)}%"
        celulas.append(texto)
    return f"| {nome} | " + " | ".join(celulas) + " |"


FASES = ((0, 4), (4, 8), (8, 12), (12, 16), (16, 20), (20, 24))  # BALANCEAMENTO_V5: partidas de 10 a 20 min

CABECALHO_FASES = ("| Jogador | " + " | ".join(f"min {a}–{b}" for a, b in FASES) + " |\n|---|" + "---|" * len(FASES))


def linha_fases(nome, jogos, medida):
    """RITMO_V3: chamados por minuto (casas atingidas) ou pico do caos em cada fase, mediana das partidas ainda vivas."""
    celulas = []
    for a, b in FASES:
        vivas = [j for j in jogos if j.t >= b * 60]
        if not vivas:
            celulas.append("—")
            continue
        if medida == "chamados":
            valor = statistics.median(sum(j.chamados_min.get(m, 0) for m in range(a, b)) / (b - a) for j in vivas)
            celulas.append(f"{valor:.1f}")
        elif medida == "ritmo":  # ADAPTATIVA_V6B: ritmo adaptativo médio da fase (0 = a curva de sempre, 1 = o teto)
            valor = statistics.median(sum(j.ritmo_min.get(m, 0.0) for m in range(a, b)) / (b - a) for j in vivas)
            celulas.append(f"{valor:.2f}")
        else:
            valor = statistics.median(max(j.caos_min.get(m, 0.0) for m in range(a, b)) for j in vivas)
            celulas.append(f"{valor:.2f}")
    return f"| {nome} | " + " | ".join(celulas) + " |"


def curva(balance, limite_min=24):
    """RITMO_V3: a curva que os dados descrevem, minuto a minuto (sem sorteio); BALANCEAMENTO_V5: a rampa até o teto
    e a subida lenta depois dele (as contas do Jogo, iguais às do DisasterManager.GetInterval/GetHouseCount)."""
    from quarteirao import DADOS
    b = balance
    jogo = Jogo.__new__(Jogo)  # só as contas da rampa, que leem b e t
    jogo.b = b
    print("| Minuto | Intervalo (s) | Casas por desastre | Terremoto forte | Chamados/min (nominal) | Tipo novo | Energia |")
    print("|---|---|---|---|---|---|---|")
    for m in range(limite_min + 1):
        jogo.t = t = m * 60
        intervalo, casas = jogo.intervalo_desastre(), jogo.casas_por_desastre()
        novos = [nome for nome, libera in (("incêndio", b["fireUnlockTime"]), ("tempestade", b["stormUnlockTime"]),
                                           ("enchente", DADOS["floodUnlockTime"]), ("meteoro", DADOS["meteorUnlockTime"]))
                 if t - 60 < libera <= t]
        energia = "sim" if 0 <= b.get("unlockTime", -1) <= t else "não"
        forte = round(casas * b["strongDisasterHouseMultiplier"])
        print(f"| {m} | {intervalo:.1f} | {casas} | {forte} | {60 * casas / intervalo:.1f} | {', '.join(novos) or '—'} "
              f"| {energia} |")


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--sementes", type=int, default=30)
    p.add_argument("--limite", type=float, default=1800, help="segundos de partida até desistir")
    p.add_argument("--set", action="append", default=[], help="campo=valor ou upgrade.campo=valor")
    p.add_argument("--assets", help="pasta Assets/Resources com os .asset do jogo")
    p.add_argument("--mapa", help="outro city_layout.json")
    p.add_argument("--plano", default="renda", choices=sorted(PLANOS), help="plano de compra do bom")
    p.add_argument("--plano-medio", default="carrinho", choices=sorted(PLANOS), help="plano de compra do médio")
    p.add_argument("--curto", action="store_true", help="planos curtos do RITMO_V3 (sem continuação, para-raio e guaritas)")
    p.add_argument("--planos", action="store_true", help="compara os planos de compra do jogador bom")
    p.add_argument("--linha", help="imprime a linha do tempo da semente 0 deste jogador")
    p.add_argument("--busca", type=int, default=0, help="sorteia N planos de compra e mostra os 5 melhores (o teto)")
    p.add_argument("--curva", action="store_true", help="imprime a curva dos dados minuto a minuto (RITMO_V3)")
    p.add_argument("--sem-expansao", action="store_true", help="os robôs não compram a base nova")
    p.add_argument("--sem-estreia", action="store_true",
                   help="tira a estreia garantida (o 1º sorteio depois de liberar um tipo é esse tipo, como o C# faz desde a #534)")
    a = p.parse_args()
    Jogo.estreia_garantida = not a.sem_estreia

    balance, ups = ler_balance(), ler_upgrades()
    if a.assets:
        balance, ups = ler_assets(a.assets, balance, ups)
    balance, ups = aplicar_ajustes(balance, ups, a.set)
    if a.curva:
        curva(balance)
        return
    mapa = ler_mapa(a.mapa) if a.mapa else ler_mapa()

    armazens = ", ".join(f"{x['dist']:.1f}" for x in mapa["armazens"])
    print(f"{len(mapa['casas'])} casas; Guarda → casa pela rua: mediana "
          f"{statistics.median(c['dist'] for c in mapa['casas']):.1f}; Guarda → Armazém: "
          f"{armazens}; {a.sementes} partidas por linha\n")

    if a.linha:
        jogador = perfis(a.plano, not a.sem_expansao, a.plano_medio, a.curto)[a.linha]
        jogador = jogador.copia() if jogador else None
        jogo = novo_jogo(balance, ups, mapa, jogador, 0, a.limite).rodar()
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
            jogador = perfis(nome, curto=a.curto)["bom"]
            print(linha_tabela(f"bom, plano {nome}", rodar_perfil(balance, ups, mapa, jogador, a.sementes, a.limite)))
        return

    resultados = {nome: rodar_perfil(balance, ups, mapa, jogador, a.sementes, a.limite)
                  for nome, jogador in perfis(a.plano, not a.sem_expansao, a.plano_medio, a.curto).items()}
    for nome, jogos in resultados.items():
        print(linha_tabela(nome, jogos))
    principais = [n for n in ("bom", "médio", "ruim", "muito eficiente") if n in resultados]
    print("\nEstreia de cada sistema, em minutos de partida (mediana e p10–p90)\n\n" + CABECALHO_ESTREIA)
    for nome in principais:
        print(linha_estreia(nome, resultados[nome]))
    print("\nChamados por minuto (casas atingidas) em cada fase\n\n" + CABECALHO_FASES)
    for nome in principais:
        print(linha_fases(nome, resultados[nome], "chamados"))
    print("\nPico do caos em cada fase\n\n" + CABECALHO_FASES)
    for nome in principais:
        print(linha_fases(nome, resultados[nome], "caos"))
    print("\nRitmo adaptativo médio em cada fase (ADAPTATIVA_V6B; 0 = a curva de sempre)\n\n" + CABECALHO_FASES)
    for nome in principais:
        print(linha_fases(nome, resultados[nome], "ritmo"))
    print("\n| Jogador | Desastres na partida | Casas atingidas por minuto |\n|---|---|---|")
    for nome in principais:
        js = resultados[nome]
        print(f"| {nome} | {statistics.median(len(j.desastres) for j in js):.0f} | "
              f"{statistics.median(sum(j.chamados_min.values()) / (j.t / 60) for j in js):.1f} |")
    print("\n" + CABECALHO_MINI)
    for nome, jogos in resultados.items():
        if jogos[0].jogador is not None:
            print(linha_mini(nome, jogos))
    print("\n" + CABECALHO_FOGO)
    for nome, jogos in resultados.items():
        print(linha_fogo(nome, jogos))


if __name__ == "__main__":
    main()
