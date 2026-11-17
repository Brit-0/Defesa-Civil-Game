"""Enchente e meteoro por quarteirão (docs/jam/ENCHENTE_METEORO.md), iguais ao BlockDisasterManager, ao BlockIncident e
às contas do BlockDisasterRules do C#. Os números vêm do BlockDisasterData.cs (padrões) e do .asset (o que vale no jogo).

- Enchente: o quarteirão inteiro alaga (casas intactas e em chamas sem equipe; o fogo apaga). Um chamado só, no bueiro
  (a casa livre mais perto do meio do quarteirão), com equipe fixa. Sem o jogador a água escoa sozinha em
  floodAutoDrainTime; girando a válvula (o robô fica preso nela) em poucos segundos.
- Meteoro: aviso mais longo, depois o quarteirão inteiro vira cratera (qualquer casa sem equipe). Um chamado de
  reconstrução com equipe e material altos e obra longa; o ponto certo em sequência acelera a obra.
- Para-raios não protegem de nenhum dos dois. O caos conta cada casa do quarteirão.

    python -X utf8 docs/jam/sim/quarteirao.py                  # válvula, obra e as partidas com e sem os dois
    python -X utf8 docs/jam/sim/quarteirao.py --sementes 30 --jogadores bom,médio
"""
import math
import re
from pathlib import Path

from dados import RAIZ, _ler_yaml_simples
from sim import FOGO, INTACTA

DATA_CS = RAIZ / "Assets/SO/BlockDisasterData.cs"
DATA_ASSET = RAIZ / "Assets/Systems/BlockDisasters/Resources/BlockDisasters/BlockDisasterData.asset"
ALAGADA, DESTRUIDA = "A", "X"  # State.Flooded e State.Destroyed


def ler_dados(cs=DATA_CS, asset=DATA_ASSET):
    """Padrões do BlockDisasterData.cs; o .asset, se existir, manda (é o que o jogo carrega)."""
    texto = Path(cs).read_text(encoding="utf-8")
    d = {}
    for tipo, nome, valor in re.findall(r"public\s+(int|float)\s+(\w+)\s*=\s*(-?[\d.]+)f?\s*;", texto):
        d[nome] = int(valor) if tipo == "int" else float(valor)
    if Path(asset).exists():
        for nome, valor in _ler_yaml_simples(asset).items():
            if nome in d and re.fullmatch(r"-?[\d.]+(e-?\d+)?", valor):
                d[nome] = int(float(valor)) if isinstance(d[nome], int) else float(valor)
    return d


# --- contas do BlockDisasterRules (as mesmas dos testes de editor)
def tempo_obra(base, minimo, equipe):
    """WorkTime: o tempo base vale para a equipe mínima; gente a mais divide."""
    need = max(1, minimo)
    return base * need / max(need, equipe)


def passo_valvula(v, d, segurando, giro, dt):
    """ValveStep: um quadro da válvula. Devolve (água escoada, falhou)."""
    falhou = False
    if v["segurando"] and not segurando and v["pressao"] >= d["valveSlipPressure"]:
        falhou, v["pressao"] = True, 0.0
    if not segurando:
        v["sentido"] = 0.0
    v["segurando"] = segurando
    if segurando and abs(giro) > 0:
        if v["sentido"] == 0 and abs(giro) >= 2:
            v["sentido"] = math.copysign(1.0, giro)
        frente = max(0.0, giro * v["sentido"]) if v["sentido"] else 0.0
        v["pressao"] += frente / 360 * d["valvePressurePerTurn"]
    v["pressao"] = min(1.0, max(0.0, v["pressao"] - d["valvePressureDecay"] * dt))
    return v["pressao"] * d["valveDrainPerSecond"] * dt, falhou


def tempo_valvula(d, voltas=1.25, falhas=0, dt=1 / 60, limite=120.0):
    """Segundos para desentupir girando 'voltas' por segundo; cada falha solta o botão um quadro no meio do caminho."""
    v = dict(pressao=0.0, sentido=0.0, segurando=False)
    auto = tempo_obra(d["floodAutoDrainTime"], d["floodWorkers"], d["floodWorkers"]) if d["floodAutoDrainTime"] > 0 else 0
    progresso, t, soltas = 0.0, 0.0, 0
    while progresso < 1 and t < limite:
        soltar = soltas < falhas and progresso >= (soltas + 1) / (falhas + 1)
        escoou, falhou = passo_valvula(v, d, not soltar, 0.0 if soltar else voltas * 360 * dt, dt)
        progresso += (dt / auto if auto > 0 else 0.0) + escoou - (d["valveSlipPenalty"] if falhou else 0.0)
        soltas += 1 if soltar else 0
        t += dt
    return t


def janela(progresso, sorteio, d):
    """NextWindow: a próxima janela do ponto certo, sempre à frente. -1 se não cabe mais."""
    inicio = progresso + d["rebuildWindowGapMin"] + (d["rebuildWindowGapMax"] - d["rebuildWindowGapMin"]) * sorteio
    return inicio if inicio + d["rebuildWindowSize"] <= 1 else -1.0


def escolher_quarteirao(livres, bloqueado, minimo, sorteio):
    """PickBlock: entre os quarteirões com casas livres suficientes e sem a Base nem Armazém; senão o com mais livres."""
    ok = [i for i, n in enumerate(livres) if n >= max(1, minimo) and not bloqueado[i]]
    if not ok:
        melhor = max(range(len(livres)), key=lambda i: livres[i], default=-1)
        return melhor if melhor >= 0 and livres[melhor] > 0 else -1
    return ok[min(len(ok) - 1, int(sorteio * len(ok)))]


# --- no simulador: o Jogo chama estes métodos (jogo.blocos)
class Quarteiroes:
    def __init__(self, jogo, mapa, dados=None):
        self.jogo, self.d = jogo, dict(dados or DADOS)
        self.blocos = {}
        for c in jogo.casas:
            if c.quarteirao:
                self.blocos.setdefault(c.quarteirao, []).append(c)
        self.ids = sorted(self.blocos)
        fechados = {mapa.get("bloco_guarda", "")} | {a.quarteirao for a in jogo.armazens}
        self.bloqueado = [q in fechados for q in self.ids]
        self.centro = {q: (sum(c.pos[0] for c in cs) / len(cs), sum(c.pos[1] for c in cs) / len(cs))
                       for q, cs in self.blocos.items()}
        self.incidentes, self.ultimo_meteoro = [], -math.inf
        self.enchentes = self.meteoros = self.apagados = self.desentupidas = self.acertos = self.erros_obra = 0
        self.obras = []  # segundos de cada obra do meteoro (com o ponto certo)

    # sorteio (DisasterManager + BlockDisasterManager.Allows)
    def estreias(self):
        """TryGetDebut: só os tipos com peso (peso 0 desliga o tipo, estreia inclusive)."""
        return tuple((tipo, self.d[libera]) for tipo, libera, peso in (("enchente", "floodUnlockTime", "floodWeight"),
                     ("meteoro", "meteorUnlockTime", "meteorWeight")) if self.d[peso] > 0)

    def pesos(self):
        t = self.jogo.t
        return [("enchente", self.d["floodWeight"] if t >= self.d["floodUnlockTime"] and self.permite("enchente") else 0),
                ("meteoro", self.d["meteorWeight"] if t >= self.d["meteorUnlockTime"] and self.permite("meteoro") else 0)]

    def permite(self, tipo):
        if tipo not in ("enchente", "meteoro"):
            return True
        meteoro = tipo == "meteoro"
        if meteoro and self.jogo.t - self.ultimo_meteoro < self.d["meteorCooldown"]:
            return False
        if any(i["meteoro"] == meteoro for i in self.incidentes):
            return False
        return self._escolher(meteoro, 0.0) is not None

    def aviso(self, tipo, aviso):
        return self.d["meteorWarningTime"] if tipo == "meteoro" else aviso

    @staticmethod
    def pode_entrar(c, meteoro):
        if c.equipe or c.pronta or c.inc is not None:
            return False
        return meteoro or c.estado in (INTACTA, FOGO)

    def _escolher(self, meteoro, sorteio):
        livres = [sum(1 for c in self.blocos[q] if self.pode_entrar(c, meteoro)) for q in self.ids]
        minimo = self.d["meteorMinHouses"] if meteoro else self.d["floodMinHouses"]
        i = escolher_quarteirao(livres, self.bloqueado, minimo, sorteio)
        return self.ids[i] if i >= 0 else None

    # o desastre (BlockDisasterManager.Run)
    def executar(self, tipo, o):
        jogo, meteoro = self.jogo, tipo == "meteoro"
        q = self._escolher(meteoro, jogo.rng.random())
        if q is None:
            return
        centro = self.centro[q]
        casas = sorted((c for c in self.blocos[q] if self.pode_entrar(c, meteoro)), key=lambda c: math.dist(c.pos, centro))
        inc = dict(meteoro=meteoro, lider=casas[0], casas=casas, inicio=jogo.t)
        for c in casas:
            if c.estado == FOGO:
                self.apagados += 1  # a água apaga o fogo (o meteoro também leva a casa em chamas)
            c.estado = DESTRUIDA if meteoro else ALAGADA
            c.dano, c.fogo, c.nivel_fogo, c.partes, c.destelhada = 0, 0.0, 0, 0, False
            c.critico, c.bonus, c.desde, c.inc = False, 0.0, jogo.t, inc
            if c.ocorrencia is None:
                c.ocorrencia = o
                o["casas"].append(c)
        self.incidentes.append(inc)
        if meteoro:
            self.ultimo_meteoro = jogo.t
            self.meteoros += 1
        else:
            self.enchentes += 1
        minuto = int(jogo.t // 60)
        jogo.chamados_min[minuto] = jogo.chamados_min.get(minuto, 0) + 1

    # o chamado único no líder (IWorkRule)
    def chamado(self, c):
        """Só o líder abre chamado; as outras casas do quarteirão passam o clique a ele."""
        return c.inc is None or c.inc["lider"] is c

    def equipe(self, c):
        return self.d["meteorWorkers"] if c.inc["meteoro"] else self.d["floodWorkers"]

    def material(self, c):
        return self.d["meteorMaterial"] if c.inc["meteoro"] and c.inc["lider"] is c else 0

    def tempo(self, c, equipe):
        if c.inc["meteoro"]:
            return tempo_obra(self.d["meteorWorkTime"], self.d["meteorWorkers"], equipe)
        return tempo_obra(self.d["floodAutoDrainTime"], self.d["floodWorkers"], equipe)

    def recompensa(self, c):
        por_casa = self.d["meteorRewardPerHouse"] if c.inc["meteoro"] else self.d["floodRewardPerHouse"]
        return por_casa * len(c.inc["casas"])

    def caos(self, c):
        return self.d["meteorChaosPerHouse"] if c.estado == DESTRUIDA else self.d["floodChaosPerHouse"]

    def comecar(self, c):
        jogo = self.jogo
        duracao = self.tempo(c, c.enviados)
        if c.inc["meteoro"]:
            self._obra(c, c.trabalho, 0.0, 0.0, janela(0.0, jogo.rng.random(), self.d), duracao, jogo.t)
            return
        jogo.agendar(jogo.t + duracao, jogo.terminar, c, c.trabalho)
        if jogo.jogador is not None:  # o robô vê a válvula e vai girar
            jogo.agendar(jogo.t + jogo.jogador.reacao, self._valvula, c, c.trabalho, jogo.t, duracao)

    def _valvula(self, c, trabalho, inicio, duracao):
        jogo, jogador = self.jogo, self.jogo.jogador
        if trabalho != c.trabalho or not c.trabalhando or not jogador.tenta_mini(jogo, 1):
            return
        falhas = 0 if jogo.rng.random() < jogador.acerto else 1
        feito = (jogo.t - inicio) / duracao  # a água que já escoou sozinha
        girando = tempo_valvula(self.d, falhas=falhas) * (1 - feito)
        jogador.ocupado_ate = max(jogador.ocupado_ate, jogo.t + girando)  # preso na válvula enquanto gira
        c.trabalho += 1
        self.desentupidas += 1
        jogo.agendar(jogo.t + girando, jogo.terminar, c, c.trabalho)

    def _obra(self, c, trabalho, progresso, boost, proxima, duracao, desde):
        """House.TickWork com o IWorkRule do meteoro, de janela em janela: o robô clica no meio de cada janela que vê."""
        jogo, d, dt = self.jogo, self.d, self.jogo.passo
        if trabalho != c.trabalho or not c.trabalhando:
            return
        t = jogo.t
        alvo = proxima + d["rebuildWindowSize"] / 2 if proxima >= 0 else 2.0
        while progresso < 1 and progresso < alvo:
            boost = max(0.0, boost - d["rebuildBoostDecay"] * dt)
            progresso += dt / duracao * (1 + boost)
            t += dt
        if progresso >= 1:
            self.obras.append(t - desde)
            jogo.agendar(t, jogo.terminar, c, c.trabalho)
            return
        jogo.agendar(t, self._janela, c, trabalho, progresso, boost, proxima, duracao, desde)

    def _janela(self, c, trabalho, progresso, boost, proxima, duracao, desde):
        jogo, d = self.jogo, self.d
        if trabalho != c.trabalho or not c.trabalhando:
            return
        if jogo.jogador is not None and jogo.jogador.tenta_mini(jogo, 1):
            if jogo.rng.random() < jogo.jogador.acerto:
                self.acertos += 1
                boost = min(d["rebuildMaxBoost"], boost + d["rebuildHitBoost"])
                progresso += d["rebuildHitProgress"]
            else:
                self.erros_obra += 1
                boost *= 0.5
                progresso -= d["rebuildMissPenalty"]
        self._obra(c, trabalho, progresso, boost, janela(progresso, jogo.rng.random(), d), duracao, desde)

    def resolver(self, c):
        """Coleta no líder: o quarteirão inteiro volta (OnCallResolved -> LeaveIncident)."""
        inc = c.inc
        if inc in self.incidentes:
            self.incidentes.remove(inc)
        for x in inc["casas"]:
            x.inc = None
            x.equipe = x.trabalhando = x.pronta = False
        return inc["casas"]


DADOS = ler_dados()


def main():
    import argparse
    import statistics

    from perfis import perfis
    from sim import ler_balance, ler_mapa, ler_upgrades
    from tabela import faixa, novo_jogo

    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--sementes", type=int, default=10)
    p.add_argument("--limite", type=float, default=1800)
    p.add_argument("--jogadores", default="bom,médio,ruim")
    a = p.parse_args()
    d = DADOS

    print(f"válvula: {tempo_valvula(d):.1f} s girando sem soltar, {tempo_valvula(d, falhas=1):.1f} s com uma falha; "
          f"sozinha {d['floodAutoDrainTime']:.0f} s")
    print(f"obra do meteoro: base {d['meteorWorkTime']:.0f} s com {d['meteorWorkers']} (material {d['meteorMaterial']})")

    balance, ups, mapa = ler_balance(), ler_upgrades(), ler_mapa()
    print("\n| jogador | com enchente e meteoro | vence | tempo da vitória | pico de caos | enchentes | meteoros | "
          "estreia enchente | estreia meteoro | obra do meteoro |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    todos = perfis()
    for nome in a.jogadores.split(","):
        for com in (False, True):
            jogos = []
            for semente in range(a.sementes):
                jogo = novo_jogo(balance, ups, mapa, todos[nome].copia(), semente, a.limite)
                if not com:
                    jogo.blocos.d.update(floodWeight=0, meteorWeight=0)
                jogos.append(jogo.rodar())
            vit = [j.t for j in jogos if j.fim == "vitória"]
            est = {k: [j.estreias[k] / 60 for j in jogos if k in j.estreias] for k in ("enchente", "meteoro")}
            obras = [s for j in jogos for s in j.blocos.obras]
            fmt = lambda xs: f"{statistics.median(xs):.1f} min ({len(xs)}/{len(jogos)})" if xs else "-"  # noqa: E731
            print(f"| {nome} | {'sim' if com else 'não'} | {100 * len(vit) // len(jogos)}% | {faixa(vit)} | "
                  f"{statistics.median(j.pico_caos for j in jogos):.2f} | "
                  f"{sum(j.blocos.enchentes for j in jogos) / len(jogos):.1f} | "
                  f"{sum(j.blocos.meteoros for j in jogos) / len(jogos):.1f} | {fmt(est['enchente'])} | "
                  f"{fmt(est['meteoro'])} | {f'{statistics.median(obras):.0f} s' if obras else '-'} |")


if __name__ == "__main__":
    main()
