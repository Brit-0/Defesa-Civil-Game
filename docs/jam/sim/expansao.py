"""Expansão (docs/jam/EXPANSAO.md): quanto valem a base e o armazém novos nos terrenos, para escolher preço e quantidade.

Usa o mesmo simulador (sim.py) e os mesmos robôs (perfis.py). Os terrenos são os de ExpansionData.DefaultLots: a entrada
de cada um entra na rua mais perto, como faz a ExpansionManager. Cada compra de base2 ou depot2 ergue o prédio no
terreno livre que mais encurta a caminhada (o jogador decide rápido, o robô escolhe o melhor). A casa usa a base mais
perto em linha reta (GuardManager.GetNearestGuard) e anda pela rua a partir dela.

    python expansao.py                      # distâncias e a tabela dos preços candidatos
    python expansao.py --sementes 10 --custos 300,450 --depositos 250
"""
import argparse
import heapq
import math
import statistics

import json
from pathlib import Path

from dados import MAPA_PADRAO, ler_balance, ler_mapa, ler_upgrades
from perfis import PLANOS, perfis
from sim import Jogo, Local
from tabela import faixa

# ExpansionData.DefaultLots (rótulo, tipo, entrada na linha central da rua)
LOTES = [("OESTE", "base", (-21.95, -22.2486)), ("OESTE", "depot", (-25.55, -22.2486)),
         ("LESTE", "base", (23.75, -22.2486)), ("LESTE", "depot", (27.35, -22.2486))]
CRESCIMENTO = 1.6  # a segunda obra do mesmo tipo custa 1,6 vez a primeira
TEMPO_OBRA = 3.0   # ExpansionData.buildTime


def grafo(caminho=MAPA_PADRAO):
    dados = json.loads(Path(caminho).read_text(encoding="utf-8"))
    nos = {f"{s['type']} {s['id']}": (s["entry"]["x"], s["entry"]["y"]) for s in dados["structures"]}
    nos.update({f"Junction {j['id']}": (j["position"]["x"], j["position"]["y"]) for j in dados["junctions"]})
    viz = {nome: set() for nome in nos}
    for link in dados["links"]:
        viz[link["a"]].add(link["b"])
        viz[link["b"]].add(link["a"])
    for rotulo, tipo, entrada in LOTES:  # ExpansionManager.MakeEntry: liga a entrada nas duas pontas da rua mais perto
        a, b = min(((a, b) for a in viz for b in viz[a] if a < b), key=lambda ab: seg(entrada, nos[ab[0]], nos[ab[1]]))
        nome = f"Lote {rotulo} {tipo}"
        nos[nome] = entrada
        viz[nome] = {a, b}
        viz[a].add(nome)
        viz[b].add(nome)
    guarda = next(s for s in dados["structures"] if s["type"] == "Guard")
    return nos, viz, f"Guard {guarda['id']}"


def seg(p, a, b):
    ab = (b[0] - a[0], b[1] - a[1])
    l2 = ab[0] ** 2 + ab[1] ** 2
    t = 0.0 if l2 == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * ab[0] + (p[1] - a[1]) * ab[1]) / l2))
    return math.dist(p, (a[0] + ab[0] * t, a[1] + ab[1] * t))


def dijkstra(nos, viz, origem):
    dist, fila = {origem: 0.0}, [(0.0, origem)]
    while fila:
        d, no = heapq.heappop(fila)
        if d > dist[no]:
            continue
        for v in viz[no]:
            nd = d + math.dist(nos[no], nos[v])
            if nd < dist.get(v, math.inf):
                dist[v] = nd
                heapq.heappush(fila, (nd, v))
    return dist


class Cidade:
    """Distância pela rua de cada casa e armazém até a base que a atende, com as bases que existirem."""

    def __init__(self, mapa):
        self.nos, self.viz, self.guarda = grafo()
        self.tabelas = {self.guarda: dijkstra(self.nos, self.viz, self.guarda)}
        for rotulo, tipo, _ in LOTES:
            if tipo == "base":
                nome = f"Lote {rotulo} {tipo}"
                self.tabelas[nome] = dijkstra(self.nos, self.viz, nome)
        self.mapa = mapa

    def dist(self, nome_no, pos, bases):
        base = min(bases, key=lambda b: math.dist(pos, self.nos[b]))
        return self.tabelas[base][nome_no]

    def media(self, bases):
        return statistics.mean(self.dist(c["nome"], c["pos"], bases) for c in self.mapa["casas"])

    def melhor_base(self, bases):
        livres = [f"Lote {r} base" for r, t, _ in LOTES if t == "base" and f"Lote {r} base" not in bases]
        return min(livres, key=lambda b: self.media(bases + [b]), default=None)


class JogoExpansao(Jogo):
    def __init__(self, cidade, *args, **kw):
        super().__init__(*args, **kw)
        self.cidade, self.bases, self.depositos = cidade, [cidade.guarda], []

    def comprar(self, uid):
        if not super().comprar(uid):
            return False
        if uid == "base2":
            nova = self.cidade.melhor_base(self.bases)
            if nova:
                self.agendar(self.t + TEMPO_OBRA, self._erguer_base, nova)
        elif uid == "depot2":
            self.agendar(self.t + TEMPO_OBRA, self._erguer_deposito)
        return True

    def _erguer_base(self, nome):
        self.bases.append(nome)
        for c in self.casas + self.armazens:
            c.dist = self.cidade.dist(c.nome, c.pos, self.bases)

    def _erguer_deposito(self):
        # o terreno de armazém livre mais perto (pela rua) de alguma base
        livres = [f"Lote {r} depot" for r, t, _ in LOTES if t == "depot" and f"Lote {r} depot" not in self.depositos]
        if not livres:
            return
        nome = min(livres, key=lambda n: self.cidade.dist(n, self.cidade.nos[n], self.bases))
        self.depositos.append(nome)
        pos = self.cidade.nos[nome]
        self.armazens.append(Local(dict(nome=nome, pos=pos, dist=self.cidade.dist(nome, pos, self.bases)), armazem=True))


def rodar(cidade, balance, ups, mapa, jogador, sementes, limite):
    jogos = []
    for semente in range(sementes):
        j = jogador.copia() if jogador is not None else None
        jogos.append(JogoExpansao(cidade, balance, ups, mapa, j, semente, limite=limite).rodar())
    return jogos


def linha(nome, jogos):
    n = len(jogos)
    vit = [j.t for j in jogos if j.fim == "vitória"]
    der = [j.t for j in jogos if j.fim == "derrota"]
    compras = [m for j in jogos for m, uid, _ in j.compras if uid in ("base2", "depot2")]
    return (f"| {nome} | {100 * len(vit) // n}% | {100 * len(der) // n}% | {faixa(vit)} | "
            f"{statistics.median(j.pico_caos for j in jogos):.2f} | "
            f"{statistics.median(compras):.1f} |" if compras else
            f"| {nome} | {100 * len(vit) // n}% | {100 * len(der) // n}% | {faixa(vit)} | "
            f"{statistics.median(j.pico_caos for j in jogos):.2f} | - |")


def com_expansao(plano, base, depot, pos_base, pos_depot):
    plano = [p for p in plano if p != "goal"]
    for i in range(base):
        plano.insert(min(len(plano), pos_base + i * 3), "base2")
    for i in range(depot):
        plano.insert(min(len(plano), pos_depot + i * 3), "depot2")
    return plano + ["goal"]


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--sementes", type=int, default=20)
    p.add_argument("--limite", type=float, default=1800)
    p.add_argument("--custos", default="300,400,550", help="custos da primeira base nova")
    p.add_argument("--depositos", default="200", help="custos do primeiro armazém novo")
    p.add_argument("--plano", default="carrinho", choices=sorted(PLANOS))
    a = p.parse_args()

    balance, ups, mapa = ler_balance(), ler_upgrades(), ler_mapa()
    cidade = Cidade(mapa)
    bases = [cidade.guarda]
    print(f"Caminhada média de uma casa até a base: só a Guarda {cidade.media(bases):.1f} u")
    for n in range(2):
        nova = cidade.melhor_base(bases)
        bases.append(nova)
        print(f"  + {nova}: {cidade.media(bases):.1f} u")

    print("\n| jogador e compra | vitória | derrota | tempo da vitória | pico de caos | minuto da 1ª obra |")
    print("|---|---|---|---|---|---|")
    jogadores = perfis(a.plano)
    for nome in ("bom", "médio"):
        print(linha(f"{nome}, sem expansão", rodar(cidade, balance, ups, mapa, jogadores[nome], a.sementes, a.limite)))
        for custo in (int(x) for x in a.custos.split(",")):
            for custo_dep in (int(x) for x in a.depositos.split(",")):
                ups2 = dict(ups)
                ups2["base2"] = dict(baseCost=custo, costGrowth=CRESCIMENTO, maxLevel=2, effectPerLevel=1)
                ups2["depot2"] = dict(baseCost=custo_dep, costGrowth=CRESCIMENTO, maxLevel=2, effectPerLevel=1)
                for nb, nd in ((1, 0), (2, 0), (1, 1), (2, 2)):
                    j = jogadores[nome].copia()
                    j.plano = com_expansao(PLANOS[a.plano], nb, nd, 5, 7)
                    jogos = rodar(cidade, balance, ups2, mapa, j, a.sementes, a.limite)
                    print(linha(f"{nome}, {nb} base ({custo}) + {nd} arm. ({custo_dep})", jogos))


if __name__ == "__main__":
    main()
