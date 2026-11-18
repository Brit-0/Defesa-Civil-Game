"""Simulador de balanceamento da jam Defesa Civil (Agente B, T25; revisão do gameloop B3), sem o Unity.

Reproduz as regras do código C# (GuardManager, House, Warehouse, DisasterManager, ChaosManager,
ResourcesManager, UpgradesManager, GameManager, Stats). Os números vêm dos próprios arquivos C# (dados.py):
os padrões de Assets/SO/BalanceData.cs e a tabela de upgrades de JamDataMenu.cs (ou, com --assets, dos .asset).
O mapa vem do city_layout.json: posição de cada casa e o grafo de ruas; a caminhada é a distância pela rua.

O jogador é um robô com tempo de reação e tempo por clique (perfis em perfis.py). Passo de 0,1 s.
Revisão do gameloop: terremoto forte, bônus de ocorrência encerrada, marcos com voluntários e os três
mini games (ponto certo, jato no incêndio, escorar no aviso), que disputam a atenção do robô.
Fogo e energia (ENERGIA_FOGO.md, fogo_energia.py): o fogo cresce de nível; jato na chegada; rede de energia.
Enchente e meteoro (ENCHENTE_METEORO.md, quarteirao.py): o quarteirão inteiro num chamado só, no líder.
"""
import heapq
import math
import random

from dados import (BALANCE_CS, MAPA_PADRAO, MENU_CS, RAIZ, aplicar_ajustes, ler_assets,  # noqa: F401
                   ler_balance, ler_guarita, ler_mapa, ler_pararaio, ler_upgrades)
from fogo_energia import Rede, abrir_jato, tick_fogo

INTACTA, DANIFICADA, FOGO = "I", "D", "F"
PARARAIO = ler_pararaio()  # LightningRodData.asset (PARARAIO.md)
GUARITA = ler_guarita()  # OutpostData (GUARITAS.md)
POR_QUARTEIRAO = ("rod", "outpost")  # v5A: um por quarteirão com casa, sem teto fixo no .asset
import quarteirao  # noqa: E402  (depois dos estados: o quarteirao.py os importa daqui)
from quarteirao import ALAGADA, DESTRUIDA  # noqa: E402


def vazar(dano, espera, dt, intervalo, maximo):
    """StormLeak.Tick: casa destelhada sem equipe sobe +1 de dano a cada intervalo, até o máximo (0 desliga)."""
    if intervalo <= 0 or dano >= maximo:
        return dano, 0.0
    espera += max(0.0, dt)
    while espera >= intervalo and dano < maximo:
        espera -= intervalo
        dano += 1
    return dano, (0.0 if dano >= maximo else espera)

# ---------------------------------------------------------------- jogo

class Local:
    __slots__ = ("nome", "pos", "dist", "armazem", "estado", "dano", "fogo", "equipe", "trabalhando",
                 "pronta", "enviados", "pago", "desde", "bonus", "critico", "trabalho", "ocorrencia", "escorada",
                 "nivel_fogo", "partes", "apagadas", "destelhada", "vazamento", "quarteirao", "inc")

    def __init__(self, dados, armazem=False):
        self.nome, self.pos, self.dist, self.armazem = dados["nome"], dados["pos"], dados["dist"], armazem
        self.estado, self.dano, self.fogo = INTACTA, 0, 0.0
        self.equipe = self.trabalhando = self.pronta = self.critico = self.escorada = False
        self.enviados = self.pago = self.trabalho = 0
        self.nivel_fogo = self.partes = self.apagadas = 0  # fogo: nível 1-3, partes de trabalho, chamas do jato
        self.destelhada, self.vazamento = False, 0.0  # TEMPESTADE.md: a chuva sobe o dano esperando equipe
        self.quarteirao = dados.get("quarteirao", "")  # polígono do city_layout.json (para-raio, PARARAIO.md)
        self.inc = None  # enchente ou meteoro em que a casa está (quarteirao.py)
        self.bonus = 0.0       # Call.rewardBonus (crítico, herói)
        self.ocorrencia = None  # o desastre que quebrou a casa (bônus quando todas voltam)
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
        self.renda_timer = 0.0
        self.consertos = self.ganho_total = self.renda_total = self.negados = 0
        self.compras = []  # (minuto, id, nível)
        self.desastres = []  # (minuto, tipo, casas)
        # Revisão do gameloop
        self.n_desastres, self.calma, self.marcos = 0, 0.0, 0
        self.bonus_ocorrencia = self.criticos = self.erros = self.herois = self.escoradas = self.de_pe = 0
        self.fogos_cresceram = 0
        # Ritmo (RITMO_V3.md): segundo em que cada sistema aparece pela primeira vez, chamados e caos por minuto.
        self.estreias = {}
        self.chamados_min, self.caos_min = {}, {}
        self.rede = Rede(self)
        # Para-raio (PARARAIO.md): quarteirões protegidos, raios absorvidos e os que caíram em casa
        self.pararaio = dict(PARARAIO)
        self.para_raios, self.absorvidos, self.raios = [], 0, 0
        # Guaritas (GUARITAS.md): quarteirão -> segundos até olhar de novo; despachos feitos sozinhos
        self.guarita = dict(GUARITA)
        self.guaritas, self.auto_despachos = {}, 0
        self.quarteiroes = len({c.quarteirao for c in self.casas if c.quarteirao})
        self.blocos = quarteirao.Quarteiroes(self, mapa)  # enchente e meteoro (ENCHENTE_METEORO.md)

    # --- valores (Stats)
    def efeito(self, uid):
        u = self.ups.get(uid)
        return u["effectPerLevel"] * self.niveis[uid] if u else 0.0

    def nivel(self, uid):
        n = self.niveis.get(uid, 0)
        return self.rede.automacao(n) if uid == "automation" else n  # Stats.AutomationLevel

    def teto(self, uid):
        """UpgradesManager.GetMaxLevel: para-raio e guarita param no número de quarteirões com casa (SetLevelCap)."""
        maximo = self.ups[uid]["maxLevel"]
        return min(maximo, self.quarteiroes) if uid in POR_QUARTEIRAO else maximo

    def custo(self, uid):
        u = self.ups[uid]
        return round(u["baseCost"] * u["costGrowth"] ** self.niveis[uid])

    def velocidade(self):
        return self.b["workerSpeed"] * (1 + self.efeito("speed"))

    def material_por_trabalhador(self):
        return self.b["materialPerWorker"] + round(self.efeito("cart"))

    def custo_material(self, c):
        if c.inc:
            return self.blocos.material(c)
        return c.dano if (not c.armazem and c.estado == DANIFICADA) else 0

    def equipe_sugerida(self, c):
        if c.inc:  # equipe fixa, sem limite pela gente livre: com menos o envio é negado
            return self.blocos.equipe(c)
        if c.armazem:  # Stats.GetRestockNeed: material que falta para as casas sem equipe, menos o estoque
            falta = sum(x.dano for x in self.casas if x.estado == DANIFICADA and not x.equipe) - self.material
            return max(1, min(math.ceil(max(1, falta) / self.material_por_trabalhador()), self.workers))
        return max(1, min(max(1, c.dano), self.workers))

    def tempo_trabalho(self, c):
        if c.inc:
            return self.blocos.tempo(c, c.enviados)
        if c.armazem:
            return self.b["restockTime"]
        total = self.b["fireWorkTime"] * max(1, c.partes) if c.estado == FOGO else self.b["workTimePerDamage"] * c.dano
        total *= max(0.0, 1 - self.efeito("tools"))
        return max(self.b["minWorkTime"], total / max(1, c.enviados))

    def recompensa(self, c):
        base = self.b["fireReward"] if c.estado == FOGO else self.b["moneyPerDamage"] * c.dano
        if c.inc:
            base = self.blocos.recompensa(c)
        return round(base * (1 + self.efeito("reward") + c.bonus))

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
        c.nivel_fogo, c.partes = (1 if problema == FOGO else 0), 0
        c.equipe = c.trabalhando = c.pronta = c.critico = c.destelhada = False
        c.estado, c.desde, c.bonus, c.vazamento = problema, self.t, 0.0, 0.0
        if problema == FOGO:
            self.estreias.setdefault("fogo", self.t)
        minuto = int(self.t // 60)
        self.chamados_min[minuto] = self.chamados_min.get(minuto, 0) + 1
        return True

    def negado(self, c, n):
        if n < 1 or n > self.workers or (c.inc and n < self.blocos.equipe(c)):
            return "NoWorkers"
        if self.custo_material(c) > self.material:
            return "NoMaterial"
        if self.rede.nega(self, c):
            return "NoEnergy"
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
        if c.armazem:  # cada ida ao Armazém é um chamado novo
            c.bonus, c.critico = 0.0, False
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
        c.trabalho += 1
        if c.inc:  # válvula da enchente ou obra do meteoro
            self.blocos.comecar(c)
            return
        if c.estado == FOGO:  # House.OpenJet: o trabalho espera a janela do jato
            abrir_jato(self, c)
            return
        duracao = self.tempo_trabalho(c)
        self.agendar(self.t + duracao, self.terminar, c, c.trabalho)
        if self.jogador is None:
            return
        # Mini game do ponto certo: o robô tenta se estiver livre na hora (disputa a atenção com despachos e coletas).
        janela = self.rng.uniform(self.b["critWindowMin"], self.b["critWindowMax"]) + self.b["critWindowSize"] / 2
        self.agendar(self.t + duracao * janela, self.mini_ponto, c, c.trabalho, janela, duracao)

    def terminar(self, c, trabalho=None):
        if trabalho is not None and (trabalho != c.trabalho or not c.trabalhando):
            return  # o mini game já adiantou ou atrasou este trabalho
        c.trabalhando, c.pronta, c.desde = False, True, self.t
        if self.nivel("automation") >= 3:
            self.coletar(c)

    # --- mini game do ponto certo (House: TryCritical); o jato está em fogo_energia.py
    def mini_ponto(self, c, trabalho, janela, duracao):
        if trabalho != c.trabalho or not c.trabalhando or not self.jogador.tenta_mini(self, 1):
            return
        c.trabalho += 1
        if self.rng.random() < self.jogador.acerto:
            c.critico, c.bonus = True, c.bonus + self.b["critRewardBonus"]
            self.criticos += 1
            self.terminar(c)
        else:
            self.erros += 1
            self.agendar(self.t + duracao * (1 - janela + self.b["missPenalty"]), self.terminar, c, c.trabalho)

    def coletar(self, c):
        if not c.pronta:
            return
        c.pronta = False
        if c.armazem:
            trazido = c.enviados * self.material_por_trabalhador()
            self.material += trazido + (math.ceil(trazido * self.b["critRewardBonus"]) if c.critico else 0)
        else:
            ganho = self.recompensa(c)
            casas = self.blocos.resolver(c) if c.inc else [c]  # enchente e meteoro: o quarteirão inteiro volta
            for x in casas:
                x.estado, x.dano = INTACTA, 0
            self.ganhar(ganho)
            self.consertos += 1
            self.marco()
            for x in casas:
                self.ocorrencia_consertada(x)
        c.equipe = False
        self.voltar(c, c.enviados)

    def ganhar(self, valor):
        self.money += valor
        self.ganho_total += valor

    # --- revisão do gameloop (GameManager.CheckMilestone, DisasterManager.CheckOccurrences)
    def marco(self):
        marcos = self.b.get("milestoneRepairs", [])
        if self.marcos < len(marcos) and self.consertos >= marcos[self.marcos]:
            self.marcos += 1
            self.workers += self.b["volunteersPerMilestone"]

    def ocorrencia_consertada(self, c):
        o, c.ocorrencia = c.ocorrencia, None
        if o is None:
            return
        o["consertadas"] += 1
        self.fechar_ocorrencia(o)

    def fechar_ocorrencia(self, o):
        if not o["terminou"] or o["pago"] or o["consertadas"] < len(o["casas"]):
            return
        o["pago"] = True
        rapido = self.t - o["inicio"] <= self.b["fastClearTime"]
        bonus = self.b["clearBonusPerHouse"] * len(o["casas"]) * (self.b["fastClearMultiplier"] if rapido else 1)
        bonus = round(bonus * (1 + self.efeito("reward")))
        self.ganhar(bonus)
        self.bonus_ocorrencia += bonus

    def nova_ocorrencia(self):
        return dict(casas=[], consertadas=0, inicio=self.t, terminou=False, pago=False)

    def atingir(self, o, c, problema, severidade):
        if self.aplicar(c, problema, severidade):
            o["casas"].append(c)
            c.ocorrencia = o

    def encerrar(self, o):
        o["terminou"] = True
        self.fechar_ocorrencia(o)

    def voltar(self, c, n):
        for i in range(n):
            self.agendar(self.t + i * self.b["dispatchInterval"] + c.dist / self.velocidade(), self._chegou_na_base)

    def _chegou_na_base(self):
        self.workers += 1

    # --- upgrades (UpgradesManager)
    def comprar(self, uid):
        u = self.ups.get(uid)
        if u is None or self.niveis[uid] >= self.teto(uid) or self.money < self.custo(uid):
            return False
        self.money -= self.custo(uid)
        self.niveis[uid] += 1
        self.compras.append((self.t / 60, uid, self.niveis[uid]))
        if uid in ("base2", "depot2"):
            self.estreias.setdefault("expansão", self.t)
        if uid == "rod":
            self.agendar(self.t + self.pararaio["obra"], self._erguer_pararaio)
        if uid == "outpost":
            self.agendar(self.t + self.guarita["obra"], self._erguer_guarita)
        if uid == "workers":
            self.workers += round(u["effectPerLevel"])
        elif uid == "automation" and self.niveis[uid] >= 3:
            for c in self.casas + self.armazens:
                self.coletar(c)
        elif uid == "goal":
            self.fim = "vitória"
        return True

    # --- desastres (DisasterManager)
    def tempo_rampa(self):
        """DisasterManager.GetRampProgressTime: a rampa só começa depois da calma do começo (disasterRampDelay)."""
        return max(0.0, self.t - max(0.0, self.b.get("disasterRampDelay", 0.0)))

    def rampa(self):
        dur = self.b["disasterRampDuration"]
        return 1.0 if dur <= 0 else min(1.0, self.tempo_rampa() / dur)

    def minutos_depois_do_teto(self):
        """BALANCEAMENTO_V5: minutos desde o fim da rampa (o teto); dali a dificuldade sobe mais devagar."""
        return max(0.0, self.tempo_rampa() - max(0.0, self.b["disasterRampDuration"])) / 60

    def intervalo_desastre(self):
        """DisasterManager.GetInterval: rampa de Start a End; depois do teto, cai disasterLateIntervalPerMinute por
        minuto, sem passar de disasterIntervalFloor."""
        b = self.b
        intervalo = b["disasterIntervalStart"] + (b["disasterIntervalEnd"] - b["disasterIntervalStart"]) * self.rampa()
        tarde = self.minutos_depois_do_teto()
        if tarde <= 0:
            return intervalo
        piso = b.get("disasterIntervalFloor", intervalo)
        return max(min(intervalo, piso), intervalo - b.get("disasterLateIntervalPerMinute", 0.0) * tarde)

    def casas_por_desastre(self):
        """DisasterManager.GetHouseCount: rampa de Start a End; depois do teto, sobe disasterLateHousesPerMinute por
        minuto, até disasterHousesMax (arredonda como o Mathf.RoundToInt: metade para o par)."""
        b = self.b
        casas = b["disasterHousesStart"] + (b["disasterHousesEnd"] - b["disasterHousesStart"]) * self.rampa()
        tarde = self.minutos_depois_do_teto()
        if tarde > 0:
            teto = b.get("disasterHousesMax", casas)
            casas = min(max(casas, teto), casas + b.get("disasterLateHousesPerMinute", 0.0) * tarde)
        return round(casas)

    # RITMO_V3, igual ao DisasterManager.TryGetDebut (#534): o primeiro sorteio depois de liberar um tipo é esse tipo.
    estreia_garantida = True

    def sortear(self):
        if self.primeiro:
            return "terremoto"
        b = self.b
        if self.estreia_garantida:
            for tipo, libera in (("incendio", b["fireUnlockTime"]), ("tempestade", b["stormUnlockTime"])) + self.blocos.estreias():
                if self.t >= libera and tipo not in self.estreias and self.blocos.permite(tipo):
                    return tipo
        pesos = [("terremoto", b["earthquakeWeight"]),
                 ("tempestade", b["stormWeight"] if self.t >= b["stormUnlockTime"] else 0),
                 ("incendio", b["fireWeight"] if self.t >= b["fireUnlockTime"] else 0)] + self.blocos.pesos()
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
        b = self.b
        forte = False
        if not self.primeiro:  # IsStrongTurn: a cada N desastres, um terremoto forte com aviso longo
            self.n_desastres += 1
            forte = b["strongDisasterEvery"] > 0 and self.n_desastres % b["strongDisasterEvery"] == 0
        n = b["firstDisasterHouses"] if self.primeiro else self.casas_por_desastre()
        if forte:
            tipo, n = "terremoto", round(n * b["strongDisasterHouseMultiplier"])
        aviso = self.blocos.aviso(tipo, b["strongDisasterWarningTime"] if forte else b["warningTime"])
        alvos = self.alvos_terremoto(n, self.primeiro) if tipo == "terremoto" else None
        if alvos and self.jogador is not None:  # Forecast: o aviso já mostra as casas; o robô escora no aviso
            self.jogador.escorar(self, alvos, aviso)
        self.desastre_rodando = True
        self.agendar(self.t + aviso, self.executar, tipo, n, alvos, forte)
        self.primeiro = False

    def executar(self, tipo, n, alvos, forte):
        self.desastres.append((self.t / 60, tipo + (" forte" if forte else ""), n))
        self.estreias.setdefault(tipo + (" forte" if forte else ""), self.t)
        if forte:
            self.calma = self.b["strongDisasterCalm"]
        o = self.nova_ocorrencia()
        if tipo == "terremoto":
            self.terremoto(alvos, o)
        elif tipo == "tempestade":
            intervalo = self.b["stormDuration"] / n
            for i in range(n):
                self.agendar(self.t + intervalo * (i + 1), self.raio, o)
            self.agendar(self.t + intervalo * n, self.fim_desastre, o)
        elif tipo in ("enchente", "meteoro"):
            self.blocos.executar(tipo, o)
            self.fim_desastre(o)
        else:
            intactas = [c for c in self.casas if c.estado == INTACTA]
            k = min(self.rng.randint(self.b["fireMinHouses"], self.b["fireMaxHouses"]), len(intactas))
            for c in self.rng.sample(intactas, k):
                self.atingir(o, c, FOGO, 0)
            self.fim_desastre(o)

    def alvos_terremoto(self, n, perto_da_guarda):
        """(casa, dano) do mais perto ao mais longe do epicentro, escolhidos já no aviso."""
        if perto_da_guarda:
            epicentro = self.guarda
        else:
            intactas = [c for c in self.casas if c.estado == INTACTA] or self.casas
            epicentro = self.rng.choice(intactas).pos
        casas = sorted(self.casas, key=lambda c: math.dist(c.pos, epicentro))[:n]
        raio = math.dist(casas[-1].pos, epicentro)
        maximo, minimo = self.b["maxDamage"], self.b["minDamage"]
        alvos = []
        for c in casas:
            d = math.dist(c.pos, epicentro)
            queda = math.floor(d / raio * (maximo - minimo + 1)) if raio > 0 else 0
            alvos.append((c, max(minimo, min(maximo, maximo - queda)), d * self.b["earthquakeWaveDelayPerUnit"]))
        return alvos

    def terremoto(self, alvos, o):
        atraso = 0.0
        for c, dano, atraso in alvos:
            self.agendar(self.t + atraso, self.onda, o, c, dano)
        self.agendar(self.t + atraso, self.fim_desastre, o)

    def onda(self, o, c, dano):
        if c.escorada:  # House.ResolveShoring: a escora tira dano; se zera, a casa fica de pé e paga
            c.escorada = False
            dano -= self.b["shoreDamageReduction"]
            if dano <= 0:
                if c.estado == INTACTA:
                    self.de_pe += 1
                    self.ganhar(self.b["shoreHeldReward"])
                return
        self.atingir(o, c, DANIFICADA, dano)

    def _erguer_pararaio(self):
        """O robô protege o quarteirão livre com mais casas (empate: o de id menor), como a dica do jogo mostra."""
        conta = {}
        for c in self.casas:
            if c.quarteirao and c.quarteirao not in self.para_raios:
                conta[c.quarteirao] = conta.get(c.quarteirao, 0) + 1
        if conta:
            self.para_raios.append(min(conta, key=lambda q: (-conta[q], q)))
            self.estreias.setdefault("para-raio", self.t)

    def _erguer_guarita(self):
        """O robô põe a guarita no quarteirão livre com mais casas (empate: o de id menor)."""
        conta = {}
        for c in self.casas:
            if c.quarteirao and c.quarteirao not in self.guaritas:
                conta[c.quarteirao] = conta.get(c.quarteirao, 0) + 1
        if conta:
            self.guaritas[min(conta, key=lambda q: (-conta[q], q))] = 0.0
            self.estreias.setdefault("guarita", self.t)

    def alvo_do_raio(self, intactas):
        """LightningRodMath.PickIndex: casa de quarteirão protegido pesa 'atracao' no sorteio (sem para-raio, o sorteio
        de sempre, com a mesma sequência de números)."""
        if not self.para_raios:
            return self.rng.choice(intactas)
        pesos = [self.pararaio["atracao"] if c.quarteirao in self.para_raios else 1.0 for c in intactas]
        x = self.rng.random() * sum(pesos)
        for c, peso in zip(intactas, pesos):
            x -= peso
            if x < 0:
                return c
        return intactas[-1]

    def raio(self, o):
        self.rede.raio(self, o)  # apagão no primeiro raio da tempestade
        intactas = [c for c in self.casas if c.estado == INTACTA]
        if not intactas:
            return
        c = self.alvo_do_raio(intactas)
        self.raios += 1
        if c.quarteirao in self.para_raios:  # o para-raio do quarteirão leva o raio para o chão: nenhum dano
            self.absorvidos += 1
            return
        if self.rng.random() < self.b["lightningFireChance"]:
            self.atingir(o, c, FOGO, 0)
        else:
            self.atingir(o, c, DANIFICADA, self.rng.randint(self.b["stormMinDamage"], self.b["stormMaxDamage"]))
            c.destelhada = c.estado == DANIFICADA  # House.ApplyProblem(..., storm: true)

    def fim_desastre(self, o):
        self.encerrar(o)
        for c in self.casas:
            c.escorada = False  # ClearShoring
        self.proximo_desastre()

    def proximo_desastre(self):
        b = self.b
        self.contagem = max(0.0, self.intervalo_desastre() - b["warningTime"]) + self.calma
        self.calma = 0.0
        self.desastre_rodando = False

    # --- relógios por quadro
    def tick_casas_e_caos(self):
        b, dt = self.b, self.passo
        peso = 0.0
        for c in self.casas:
            if c.estado == FOGO:
                tick_fogo(self, c)
                if not c.trabalhando and not c.pronta:
                    c.fogo -= dt
                    if c.fogo <= 0:  # BurnOut
                        c.fogo, c.dano, c.estado = 0.0, b["fireBurnoutDamage"], DANIFICADA
                peso += b["chaosWeightFire"] if c.estado == FOGO else b["chaosWeightDamaged"]
            elif c.estado in (ALAGADA, DESTRUIDA):  # cada casa do quarteirão pesa (Stats.ChaosWeight)
                peso += self.blocos.caos(c)
            elif c.estado == DANIFICADA:
                peso += b["chaosWeightDamaged"]
                if c.destelhada and not c.equipe:  # House.TickLeak / StormLeak.Tick
                    c.dano, c.vazamento = vazar(c.dano, c.vazamento, dt, b.get("stormLeakInterval", 0),
                                                b.get("stormLeakMaxDamage", 0))
        alvo = min(1.0, peso / b["chaosLimit"]) if b["chaosLimit"] > 0 else 0.0
        vel = b["chaosRisePerSecond"] if alvo > self.caos else b["chaosFallPerSecond"]
        if alvo > self.caos:
            self.caos = min(alvo, self.caos + vel * dt)
        else:
            self.caos = max(alvo, self.caos - vel * dt)
        self.pico_caos = max(self.pico_caos, self.caos)
        minuto = int(self.t // 60)
        self.caos_min[minuto] = max(self.caos_min.get(minuto, 0.0), self.caos)
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

    def tick_guaritas(self):
        """OutpostManager.Update/Dispatch (v5A, no lugar do antigo nível 4 da Automação): cada guarita olha só o
        próprio quarteirão a cada 'intervalo'; manda a equipe sugerida ao chamado mais urgente que já esperou a reação."""
        for q in self.guaritas:
            self.guaritas[q] -= self.passo
            if self.guaritas[q] > 0:
                continue
            espera = [c for c in self.casas if c.quarteirao == q and c.estado != INTACTA and not c.equipe
                      and self.t - c.desde >= self.guarita["reacao"] and self.blocos.chamado(c)]
            if not espera:
                continue
            self.guaritas[q] = self.guarita["intervalo"]
            espera.sort(key=lambda c: (0, c.fogo) if c.estado == FOGO else (1, c.dano))
            for c in espera:
                n = self.equipe_sugerida(c)
                if not self.negado(c, n) and self.enviar(c, n, automatico=True):
                    self.auto_despachos += 1
                    break

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
            self.rede.tick(self)
            self.tick_renda()
            self.tick_guaritas()
            if self.jogador is not None:
                self.jogador.agir(self)
            if self.fim:
                break
            self.tick_casas_e_caos()
        return self
