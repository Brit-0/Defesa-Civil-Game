"""Simulador de balanceamento da jam Defesa Civil (Agente B, T25), sem o Unity.

Reproduz as regras do código C# (GuardManager, House, Warehouse, DisasterManager, ChaosManager,
ResourcesManager, UpgradesManager, Stats). Os números vêm dos próprios arquivos C#: os padrões de
Assets/SO/BalanceData.cs e a tabela de upgrades de Assets/Systems/Masters/Editor/JamDataMenu.cs
(ou, com --assets, dos .asset que o menu de dados criou). O mapa vem do city_layout.json: posição de
cada casa e o grafo de ruas, o mesmo dos waypoints da cena; a caminhada é a distância pela rua.

O jogador é um robô com tempo de reação e tempo por clique (perfis em perfis.py). Passo de 0,1 s.
"""
import heapq
import json
import math
import random
import re
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[3]
BALANCE_CS = RAIZ / "Assets/SO/BalanceData.cs"
MENU_CS = RAIZ / "Assets/Systems/Masters/Editor/JamDataMenu.cs"
MAPA_PADRAO = RAIZ / "Assets/_Project/Data/Cities/city_layout.json"

INTACTA, DANIFICADA, FOGO = "I", "D", "F"


# ---------------------------------------------------------------- dados

def ler_balance(caminho=BALANCE_CS):
    """Padrões dos campos do BalanceData.cs (int ou float)."""
    texto = Path(caminho).read_text(encoding="utf-8")
    valores = {}
    for tipo, nome, valor in re.findall(r"public\s+(int|float)\s+(\w+)\s*=\s*(-?[\d.]+)f?\s*;", texto):
        valores[nome] = int(valor) if tipo == "int" else float(valor)
    return valores


def ler_upgrades(caminho=MENU_CS):
    """Tabela de upgrades do JamDataMenu.cs: id -> maxLevel, baseCost, costGrowth, effectPerLevel."""
    texto = Path(caminho).read_text(encoding="utf-8")
    padrao = (r'CreateUpgrade\("\w+",\s*UpgradeIds\.(\w+),\s*"[^"]*",\s*"[^"]*",\s*(\d+),\s*(\d+),'
              r'\s*(\d+),\s*([\d.]+)f,\s*([\d.]+)f')
    ups = {}
    for nome, ordem, maximo, base, cresc, efeito in re.findall(padrao, texto):
        ups[nome.lower()] = dict(order=int(ordem), maxLevel=int(maximo), baseCost=int(base),
                                 costGrowth=float(cresc), effectPerLevel=float(efeito))
    return ups


def _ler_yaml_simples(caminho):
    valores = {}
    for linha in Path(caminho).read_text(encoding="utf-8").splitlines():
        m = re.match(r"\s{2}(\w+):\s*(.*)$", linha)
        if m:
            valores[m.group(1)] = m.group(2).strip()
    return valores


def ler_assets(pasta_resources, balance, ups):
    """Sobrepõe os padrões com os .asset de Assets/Resources (o que o jogo carrega de verdade)."""
    pasta = Path(pasta_resources)
    arquivo = pasta / "Balance/BalanceData.asset"
    if arquivo.exists():
        for chave, valor in _ler_yaml_simples(arquivo).items():
            if chave in balance:
                balance[chave] = type(balance[chave])(float(valor))
    for arquivo in sorted((pasta / "Upgrades").glob("*.asset")):
        dados = _ler_yaml_simples(arquivo)
        uid = dados.get("id")
        if uid:
            ups[uid] = dict(order=int(dados.get("order", 0)), maxLevel=int(dados["maxLevel"]),
                            baseCost=int(dados["baseCost"]), costGrowth=float(dados["costGrowth"]),
                            effectPerLevel=float(dados["effectPerLevel"]))
    return balance, ups


def aplicar_ajustes(balance, ups, ajustes):
    """--set campo=valor (BalanceData) ou --set id.campo=valor (upgrade)."""
    for ajuste in ajustes or []:
        chave, valor = ajuste.split("=", 1)
        if "." in chave:
            uid, campo = chave.split(".", 1)
            ups[uid][campo] = type(ups[uid][campo])(float(valor))
        else:
            if chave not in balance:
                raise KeyError(f"BalanceData não tem o campo {chave}")
            balance[chave] = type(balance[chave])(float(valor))
    return balance, ups


def ler_mapa(caminho=MAPA_PADRAO):
    """Casas, Guarda e Armazéns com a distância pela rua até a entrada da Guarda."""
    dados = json.loads(Path(caminho).read_text(encoding="utf-8"))
    nos = {}
    for s in dados["structures"]:
        nos[f"{s['type']} {s['id']}"] = (s["entry"]["x"], s["entry"]["y"])
    for j in dados["junctions"]:
        nos[f"Junction {j['id']}"] = (j["position"]["x"], j["position"]["y"])
    vizinhos = {nome: [] for nome in nos}
    for link in dados["links"]:
        a, b = link["a"], link["b"]
        peso = math.dist(nos[a], nos[b])
        vizinhos[a].append((b, peso))
        vizinhos[b].append((a, peso))

    guarda = next(s for s in dados["structures"] if s["type"] == "Guard")
    origem = f"Guard {guarda['id']}"
    dist = {origem: 0.0}
    fila = [(0.0, origem)]
    while fila:
        d, no = heapq.heappop(fila)
        if d > dist[no]:
            continue
        for viz, peso in vizinhos[no]:
            if d + peso < dist.get(viz, math.inf):
                dist[viz] = d + peso
                heapq.heappush(fila, (d + peso, viz))

    def item(s):
        return dict(nome=f"{s['type']} {s['id']}", pos=(s["bottomCenter"]["x"], s["bottomCenter"]["y"]),
                    dist=dist[f"{s['type']} {s['id']}"])

    return dict(guarda=(guarda["bottomCenter"]["x"], guarda["bottomCenter"]["y"]),
                casas=[item(s) for s in dados["structures"] if s["type"] == "House"],
                armazens=sorted((item(s) for s in dados["structures"] if s["type"] == "Warehouse"),
                                key=lambda a: a["dist"]))


# ---------------------------------------------------------------- jogo

class Local:
    __slots__ = ("nome", "pos", "dist", "armazem", "estado", "dano", "fogo", "equipe", "trabalhando",
                 "pronta", "enviados", "pago", "desde")

    def __init__(self, dados, armazem=False):
        self.nome, self.pos, self.dist, self.armazem = dados["nome"], dados["pos"], dados["dist"], armazem
        self.estado, self.dano, self.fogo = INTACTA, 0, 0.0
        self.equipe = self.trabalhando = self.pronta = False
        self.enviados = self.pago = 0
        self.desde = 0.0  # quando o chamado abriu ou o trabalho terminou (para o tempo de reação)


class Jogo:
    def __init__(self, balance, ups, mapa, jogador, semente, passo=0.1, limite=1800.0):
        self.b, self.ups, self.jogador = balance, ups, jogador
        self.rng = random.Random(semente)
        self.passo, self.limite = passo, limite
        self.t = 0.0
        self.niveis = {uid: 0 for uid in ups}
        self.workers, self.material, self.money = balance["startWorkers"], balance["startMaterial"], balance["startMoney"]
        self.guarda = mapa["guarda"]
        self.casas = [Local(c) for c in mapa["casas"]]
        self.armazens = [Local(a, armazem=True) for a in mapa["armazens"]]
        self.eventos, self.seq = [], 0
        self.caos, self.pico_caos = 0.0, 0.0
        self.fim = None
        self.primeiro, self.desastre_rodando = True, False
        self.contagem = max(0.0, balance["firstDisasterDelay"] - balance["warningTime"])
        self.renda_timer, self.auto_cd = 0.0, 0.0
        self.consertos = self.ganho_total = self.renda_total = self.negados = 0
        self.compras = []  # (minuto, id, nível)
        self.desastres = []  # (minuto, tipo, casas)

    # --- valores (Stats)
    def efeito(self, uid):
        u = self.ups.get(uid)
        return u["effectPerLevel"] * self.niveis[uid] if u else 0.0

    def nivel(self, uid):
        return self.niveis.get(uid, 0)

    def custo(self, uid):
        u = self.ups[uid]
        return round(u["baseCost"] * u["costGrowth"] ** self.niveis[uid])

    def velocidade(self):
        return self.b["workerSpeed"] * (1 + self.efeito("speed"))

    def material_por_trabalhador(self):
        return self.b["materialPerWorker"] + round(self.efeito("cart"))

    def custo_material(self, c):
        return c.dano if (not c.armazem and c.estado == DANIFICADA) else 0

    def equipe_sugerida(self, c):
        return max(1, min(max(1, c.dano), self.workers))

    def tempo_trabalho(self, c):
        if c.armazem:
            return self.b["restockTime"]
        total = self.b["fireWorkTime"] if c.estado == FOGO else self.b["workTimePerDamage"] * c.dano
        total *= max(0.0, 1 - self.efeito("tools"))
        return max(self.b["minWorkTime"], total / max(1, c.enviados))

    def recompensa(self, c):
        base = self.b["fireReward"] if c.estado == FOGO else self.b["moneyPerDamage"] * c.dano
        return round(base * (1 + self.efeito("reward")))

    # --- agenda
    def agendar(self, quando, funcao, *args):
        self.seq += 1
        heapq.heappush(self.eventos, (quando, self.seq, funcao, args))

    # --- casas (House)
    def aplicar(self, c, problema, severidade):
        if c.estado != INTACTA:
            return False
        c.dano = max(self.b["minDamage"], severidade) if problema == DANIFICADA else 0
        c.fogo = self.b["fireDuration"] if problema == FOGO else 0.0
        c.equipe = c.trabalhando = c.pronta = False
        c.estado, c.desde = problema, self.t
        return True

    def negado(self, c, n):
        if n < 1 or n > self.workers:
            return "NoWorkers"
        if self.custo_material(c) > self.material:
            return "NoMaterial"
        return None

    def enviar(self, c, n, automatico=False):
        if c.equipe:
            return False
        if self.negado(c, n):
            if not automatico:
                self.negados += 1
            return False
        gasto = self.custo_material(c)
        self.workers -= n
        self.material -= gasto
        c.enviados, c.pago, c.equipe = n, gasto, True
        chegada = self.t + (n - 1) * self.b["dispatchInterval"] + c.dist / self.velocidade()
        self.agendar(chegada, self.comecar, c)
        return True

    def comecar(self, c):
        falta = self.custo_material(c) - c.pago
        if falta > 0:
            if falta > self.material:  # SendTeamBack: o fogo apagou com a equipe a caminho e falta material
                c.equipe = False
                c.desde = self.t
                self.negados += 1
                self.voltar(c, c.enviados)
                return
            self.material -= falta
            c.pago += falta
        c.trabalhando = True
        self.agendar(self.t + self.tempo_trabalho(c), self.terminar, c)

    def terminar(self, c):
        c.trabalhando, c.pronta, c.desde = False, True, self.t
        if self.nivel("automation") >= 3:
            self.coletar(c)

    def coletar(self, c):
        if not c.pronta:
            return
        c.pronta = False
        if c.armazem:
            self.material += c.enviados * self.material_por_trabalhador()
        else:
            ganho = self.recompensa(c)
            c.estado, c.dano = INTACTA, 0
            self.money += ganho
            self.ganho_total += ganho
            self.consertos += 1
        c.equipe = False
        self.voltar(c, c.enviados)

    def voltar(self, c, n):
        for i in range(n):
            self.agendar(self.t + i * self.b["dispatchInterval"] + c.dist / self.velocidade(), self._chegou_na_base)

    def _chegou_na_base(self):
        self.workers += 1

    # --- upgrades (UpgradesManager)
    def comprar(self, uid):
        u = self.ups.get(uid)
        if u is None or self.niveis[uid] >= u["maxLevel"] or self.money < self.custo(uid):
            return False
        self.money -= self.custo(uid)
        self.niveis[uid] += 1
        self.compras.append((self.t / 60, uid, self.niveis[uid]))
        if uid == "workers":
            self.workers += round(u["effectPerLevel"])
        elif uid == "automation" and self.niveis[uid] >= 3:
            for c in self.casas + self.armazens:
                self.coletar(c)
        elif uid == "goal":
            self.fim = "vitória"
        return True

    # --- desastres (DisasterManager)
    def rampa(self):
        dur = self.b["disasterRampDuration"]
        return 1.0 if dur <= 0 else min(1.0, self.t / dur)

    def casas_por_desastre(self):
        b = self.b
        return round(b["disasterHousesStart"] + (b["disasterHousesEnd"] - b["disasterHousesStart"]) * self.rampa())

    def sortear(self):
        if self.primeiro:
            return "terremoto"
        b = self.b
        pesos = [("terremoto", b["earthquakeWeight"]),
                 ("tempestade", b["stormWeight"] if self.t >= b["stormUnlockTime"] else 0),
                 ("incendio", b["fireWeight"] if self.t >= b["fireUnlockTime"] else 0)]
        escolhido, total = None, 0.0
        for tipo, peso in pesos:
            if peso <= 0:
                continue
            total += peso
            if self.rng.random() * total <= peso:
                escolhido = tipo
        return escolhido

    def tick_desastres(self):
        if self.desastre_rodando:
            return
        self.contagem -= self.passo
        if self.contagem > 0:
            return
        tipo = self.sortear()
        if tipo is None:
            self.proximo_desastre()
            return
        n = self.b["firstDisasterHouses"] if self.primeiro else self.casas_por_desastre()
        self.desastre_rodando = True
        self.agendar(self.t + self.b["warningTime"], self.executar, tipo, n, self.primeiro)
        self.primeiro = False

    def executar(self, tipo, n, perto_da_guarda):
        self.desastres.append((self.t / 60, tipo, n))
        if tipo == "terremoto":
            self.terremoto(n, perto_da_guarda)
        elif tipo == "tempestade":
            intervalo = self.b["stormDuration"] / n
            for i in range(n):
                self.agendar(self.t + intervalo * (i + 1), self.raio)
            self.agendar(self.t + intervalo * n, self.proximo_desastre)
        else:
            intactas = [c for c in self.casas if c.estado == INTACTA]
            k = min(self.rng.randint(self.b["fireMinHouses"], self.b["fireMaxHouses"]), len(intactas))
            for c in self.rng.sample(intactas, k):
                self.aplicar(c, FOGO, 0)
            self.proximo_desastre()

    def terremoto(self, n, perto_da_guarda):
        if perto_da_guarda:
            epicentro = self.guarda
        else:
            intactas = [c for c in self.casas if c.estado == INTACTA] or self.casas
            epicentro = self.rng.choice(intactas).pos
        alvos = sorted(self.casas, key=lambda c: math.dist(c.pos, epicentro))[:n]
        raio = math.dist(alvos[-1].pos, epicentro)
        maximo, minimo = self.b["maxDamage"], self.b["minDamage"]
        atraso = 0.0
        for c in alvos:
            d = math.dist(c.pos, epicentro)
            queda = math.floor(d / raio * (maximo - minimo + 1)) if raio > 0 else 0
            atraso = d * self.b["earthquakeWaveDelayPerUnit"]
            self.agendar(self.t + atraso, self.aplicar, c, DANIFICADA, max(minimo, min(maximo, maximo - queda)))
        self.agendar(self.t + atraso, self.proximo_desastre)

    def raio(self):
        intactas = [c for c in self.casas if c.estado == INTACTA]
        if not intactas:
            return
        c = self.rng.choice(intactas)
        if self.rng.random() < self.b["lightningFireChance"]:
            self.aplicar(c, FOGO, 0)
        else:
            self.aplicar(c, DANIFICADA, self.rng.randint(self.b["stormMinDamage"], self.b["stormMaxDamage"]))

    def proximo_desastre(self):
        b = self.b
        intervalo = b["disasterIntervalStart"] + (b["disasterIntervalEnd"] - b["disasterIntervalStart"]) * self.rampa()
        self.contagem = max(0.0, intervalo - b["warningTime"])
        self.desastre_rodando = False

    # --- relógios por quadro
    def tick_casas_e_caos(self):
        b, dt = self.b, self.passo
        peso = 0.0
        for c in self.casas:
            if c.estado == FOGO:
                if not c.trabalhando and not c.pronta:
                    c.fogo -= dt
                    if c.fogo <= 0:  # BurnOut
                        c.fogo, c.dano, c.estado = 0.0, b["fireBurnoutDamage"], DANIFICADA
                peso += b["chaosWeightFire"] if c.estado == FOGO else b["chaosWeightDamaged"]
            elif c.estado == DANIFICADA:
                peso += b["chaosWeightDamaged"]
        alvo = min(1.0, peso / b["chaosLimit"]) if b["chaosLimit"] > 0 else 0.0
        vel = b["chaosRisePerSecond"] if alvo > self.caos else b["chaosFallPerSecond"]
        if alvo > self.caos:
            self.caos = min(alvo, self.caos + vel * dt)
        else:
            self.caos = max(alvo, self.caos - vel * dt)
        self.pico_caos = max(self.pico_caos, self.caos)
        if self.caos >= 1.0:
            self.fim = "derrota"

    def tick_renda(self):
        if self.nivel("income") <= 0:
            self.renda_timer = 0.0
            return
        self.renda_timer += self.passo
        if self.renda_timer < self.b["incomeInterval"]:
            return
        self.renda_timer = 0.0
        intactas = sum(1 for c in self.casas if c.estado == INTACTA)
        valor = round(self.b["incomePerHouse"] * self.efeito("income") * intactas * (1 - self.caos))
        if valor > 0:
            self.money += valor
            self.ganho_total += valor
            self.renda_total += valor

    def tick_despacho_automatico(self):
        if self.nivel("automation") < 4:
            return
        self.auto_cd -= self.passo
        if self.auto_cd > 0:
            return
        self.auto_cd = self.b["autoDispatchInterval"]
        espera = [c for c in self.casas if c.estado != INTACTA and not c.equipe]
        espera.sort(key=lambda c: (0, c.fogo) if c.estado == FOGO else (1, c.dano))
        for c in espera:
            n = self.equipe_sugerida(c)
            if not self.negado(c, n) and self.enviar(c, n, automatico=True):
                return

    # --- partida
    def rodar(self):
        while self.fim is None and self.t < self.limite:
            self.t += self.passo
            while self.eventos and self.eventos[0][0] <= self.t and self.fim is None:
                _, _, funcao, args = heapq.heappop(self.eventos)
                funcao(*args)
            if self.fim:
                break
            self.tick_desastres()
            self.tick_renda()
            self.tick_despacho_automatico()
            if self.jogador is not None:
                self.jogador.agir(self)
            if self.fim:
                break
            self.tick_casas_e_caos()
        return self
