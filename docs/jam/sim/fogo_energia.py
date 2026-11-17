"""Jato no incêndio e rede de energia no simulador (docs/jam/ENERGIA_FOGO.md), como no C#.

Jato (House.OpenJet/TickJet/DouseFlame): a equipe chega no fogo e abre uma janela de jetTime segundos com
jetFlamesBase + (nível - 1) chamas; o trabalho espera. Todas apagadas: fogo apagado na hora e +jetOutBonus.
Sobrou chama: o fogo cresce um nível (até fireMaxLevel) e ganha mais uma parte de trabalho (FireRules.Grow).
Esperando equipe, o nível sobe em partes iguais do fireDuration (FireRules.LevelFromTimer).

Energia (EnergyManager, EnergyGrid): liga em unlockTime. Base gasta baseDemand, armazém depotDemand, cada nível de
automação automationDemandPerLevel. Capacidade = startCapacity + efeito do upgrade generator. O que não cabe desliga:
automação primeiro, depois armazéns, depois bases. Tempestade: chance stormBlackoutChance de apagão no primeiro raio;
tudo desliga até o jogador clicar RELIGAR ou passar blackoutAutoRestore segundos.
"""
FOGO = "F"


def nivel_pelo_tempo(restante01, maximo):
    if maximo < 1:
        return 1
    queimado = 1.0 - min(1.0, max(0.0, restante01))
    return min(maximo, 1 + int(queimado * maximo))


def chamas(b, nivel):
    return max(1, b["jetFlamesBase"] + (nivel - 1) * b["jetFlamesPerLevel"])


# ---------------------------------------------------------------- jato

def abrir_jato(jogo, c):
    """Equipe chegou no fogo: abre a janela; o robô tenta se der tempo."""
    b = jogo.b
    c.partes, c.apagadas = max(1, c.nivel_fogo), 0
    n = chamas(b, c.nivel_fogo)
    fim = jogo.t + b["jetTime"]
    jogo.agendar(fim, fechar_jato, jogo, c, c.trabalho)
    j = jogo.jogador
    if j is None or not j.mini or (j.para_em is not None and jogo.t >= j.para_em):
        return
    # O anel aparece em volta da casa com som: meia mira para achar, arrastar é rápido (meio clique por chama).
    comeco = max(jogo.t + j.reacao, j.ocupado_ate)
    gasto = j.mira * 0.5 + j.clique * n * 0.5
    if comeco + gasto > fim:
        return
    j.cliques += n
    j.ocupado_ate = comeco + gasto
    if jogo.rng.random() < min(0.95, j.acerto + 0.15):
        jogo.agendar(comeco + gasto, apagou, jogo, c, c.trabalho)
    else:  # errou o arraste: apagou metade e a janela fecha com chama sobrando
        c.apagadas = n // 2


def apagou(jogo, c, trabalho):
    if trabalho != c.trabalho or not c.trabalhando:
        return
    c.trabalho += 1
    c.bonus += jogo.b["jetOutBonus"]
    jogo.herois += 1
    jogo.terminar(c)


def fechar_jato(jogo, c, trabalho):
    if trabalho != c.trabalho or not c.trabalhando:
        return  # já apagou
    b = jogo.b
    c.trabalho += 1
    progresso = c.apagadas / chamas(b, c.nivel_fogo)
    nivel = c.partes
    progresso *= nivel / (nivel + 1)  # FireRules.Grow
    c.partes += 1
    c.nivel_fogo = min(b["fireMaxLevel"], c.nivel_fogo + 1)
    jogo.fogos_cresceram += 1
    jogo.agendar(jogo.t + jogo.tempo_trabalho(c) * (1 - progresso), jogo.terminar, c, c.trabalho)


def tick_fogo(jogo, c):
    """Fogo esperando equipe: o nível sobe com o tempo."""
    if not c.trabalhando and not c.pronta and jogo.b["fireDuration"] > 0:
        c.nivel_fogo = max(c.nivel_fogo, nivel_pelo_tempo(c.fogo / jogo.b["fireDuration"], jogo.b["fireMaxLevel"]))


# ---------------------------------------------------------------- energia

class Rede:
    def __init__(self, jogo):
        self.ligada = False
        self.apagao, self.apagao_desde = False, 0.0
        self.demanda = self.capacidade = 0
        self.bases_ligadas = self.armazens_ligados = self.auto_ligada = 0
        self.relogio = 0.0
        self.apagoes = self.religados = self.segundos_sem_energia = 0
        self.sobrecarga_s = 0.0
        self._rolados = set()
        self.desligada = jogo.b.get("unlockTime", -1) < 0

    def bases(self, jogo):
        return len(getattr(jogo, "bases", None) or [None])

    def automacao(self, comprada):
        return comprada if not self.ligada else min(comprada, self.auto_ligada)

    def nega(self, jogo, c):
        if not self.ligada:
            return False
        if self.apagao or self.bases_ligadas == 0:
            return True
        return c.armazem and jogo.armazens.index(c) >= self.armazens_ligados

    def recontar(self, jogo):
        b = jogo.b
        cap = b["startCapacity"] + round(jogo.efeito("generator"))
        bases, depositos, auto = self.bases(jogo), len(jogo.armazens), jogo.niveis.get("automation", 0)
        self.capacidade = cap
        self.demanda = bases * b["baseDemand"] + depositos * b["depotDemand"] + auto * b["automationDemandPerLevel"]
        antes = self.auto_ligada
        if self.apagao:
            self.bases_ligadas = self.armazens_ligados = self.auto_ligada = 0
            return
        usado = 0
        self.bases_ligadas = self.armazens_ligados = self.auto_ligada = 0
        for _ in range(bases):
            if usado + b["baseDemand"] <= cap:
                usado += b["baseDemand"]
                self.bases_ligadas += 1
        for _ in range(depositos):
            if usado + b["depotDemand"] <= cap:
                usado += b["depotDemand"]
                self.armazens_ligados += 1
        for _ in range(auto):
            if usado + b["automationDemandPerLevel"] > cap:
                break
            usado += b["automationDemandPerLevel"]
            self.auto_ligada += 1
        if antes < 3 <= self.auto_ligada:  # EnergyManager: automação voltou e coleta o que ficou pronto
            for c in jogo.casas + jogo.armazens:
                jogo.coletar(c)

    def tick(self, jogo):
        if self.desligada:
            return
        if not self.ligada:
            if jogo.t < jogo.b["unlockTime"]:
                return
            self.ligada = True
            jogo.estreias.setdefault("energia", jogo.t)
            self.recontar(jogo)
        if self.apagao:
            self.segundos_sem_energia += jogo.passo
            if jogo.t - self.apagao_desde >= jogo.b["blackoutAutoRestore"]:
                self.religar(jogo)
        elif self.demanda > self.capacidade:
            self.sobrecarga_s += jogo.passo
        self.relogio -= jogo.passo
        if self.relogio <= 0:
            self.relogio = 1.0
            self.recontar(jogo)

    def raio(self, jogo, o):
        if not self.ligada or id(o) in self._rolados:
            return
        self._rolados.add(id(o))
        if not self.apagao and jogo.rng.random() < jogo.b["stormBlackoutChance"]:
            self.apagao, self.apagao_desde = True, jogo.t
            self.apagoes += 1
            self.recontar(jogo)

    def religar(self, jogo, pelo_jogador=False):
        if not self.apagao:
            return
        self.apagao = False
        self.religados += pelo_jogador
        self.recontar(jogo)
