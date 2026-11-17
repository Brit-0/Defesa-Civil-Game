"""Jogadores-robô do simulador: tempo de reação, tempo por clique e a ordem em que compram upgrades.

Custo de cada ação em cliques, como no jogo (GDD, seção 7, e GAMELOOP_REVISAO):
  despacho nível 0: casa + base + Enviar (a aba já abre com a equipe sugerida; cada '+'/'-' de ajuste é um
  clique a mais); nível 1 tira a base; nível 2 em diante, 1 clique
  Armazém: igual ao nível 0 com a equipe que repõe o que falta (no nível 1 em diante a base sai sozinha)
  mini games: ponto certo = mira + 1 clique; jato no fogo = meia mira + arrastar (meio clique por chama, fogo_energia.py);
  escorar = 1 clique por casa no aviso
  energia: RELIGAR no apagão = mira + 1 clique; gerador = compra (o robô compra antes de passar da capacidade)
  coleta: 1 clique (nível 3 coleta sozinho); compra: abrir a aba + clicar no botão
A cada ação soma-se a "mira" (achar a casa na tela, mover a câmera); escolher a base à mão (nível 0) é uma
segunda mira, porque a Guarda raramente está na mesma parte da tela que a casa.
"""
import math

from sim import DANIFICADA, FOGO, INTACTA


class Jogador:
    def __init__(self, nome, reacao, clique, mira, plano, equipe="espalha", estoque_alvo=0, para_em=None,
                 mini=True, acerto=0.8):
        self.nome = nome
        self.mini, self.acerto = mini, acerto  # joga os mini games? chance de acertar o ponto certo
        self.reacao, self.clique, self.mira = reacao, clique, mira
        self.plano = list(plano)
        self.equipe = equipe              # "espalha", "sugerida" ou "um"
        self.estoque_alvo = estoque_alvo  # material que tenta manter no estoque nas horas calmas (0 = só repõe quando falta)
        self.para_em = para_em            # segundos de partida em que larga o mouse (None = joga até o fim)
        self.ocupado_ate = 0.0
        self.cliques = 0

    def copia(self):
        return Jogador(self.nome, self.reacao, self.clique, self.mira, self.plano, self.equipe, self.estoque_alvo, self.para_em,
                       self.mini, self.acerto)

    def _gastar(self, jogo, cliques, miras=1):
        self.cliques += cliques
        self.ocupado_ate = jogo.t + self.mira * miras + self.clique * cliques

    def _livre(self, jogo):
        return jogo.t >= self.ocupado_ate and (self.para_em is None or jogo.t < self.para_em)

    def tenta_mini(self, jogo, cliques):
        """Mini game na hora certa: só se estiver livre. Cliques de mutirão são rápidos (metade do tempo)."""
        if not self.mini or not self._livre(jogo):
            return False
        self.cliques += cliques
        self.ocupado_ate = jogo.t + self.mira + self.clique * (cliques if cliques == 1 else cliques * 0.5)
        return True

    def escorar(self, jogo, alvos, aviso):
        """No aviso do terremoto: escora primeiro as casas que ficam de pé (dano <= redução), com o material que há."""
        if not self.mini or (self.para_em is not None and jogo.t >= self.para_em):
            return
        custo = jogo.b["shoreMaterialCost"]
        por_casa = self.clique + self.mira * 0.5
        tempo = aviso - self.reacao - max(0.0, self.ocupado_ate - jogo.t)
        feitas = 0
        for c, _dano, _atraso in sorted(alvos, key=lambda a: a[1]):
            if tempo < por_casa or jogo.material < custo:
                break
            if c.estado != INTACTA or c.escorada:
                continue
            c.escorada = True
            jogo.material -= custo
            jogo.escoradas += 1
            tempo -= por_casa
            feitas += 1
        if feitas:
            self.cliques += feitas
            self.ocupado_ate = max(self.ocupado_ate, jogo.t) + self.reacao + feitas * por_casa

    def agir(self, jogo):
        if not self._livre(jogo):
            return
        if self._religar(jogo) or self._coletar(jogo) or self._comprar(jogo) or self._despachar(jogo) or self._repor(jogo):
            return
        self.ocupado_ate = jogo.t + 0.25  # nada a fazer: olha de novo daqui a pouco

    def _visivel(self, jogo, c):
        return jogo.t - c.desde >= self.reacao

    def _coletar(self, jogo):
        if jogo.nivel("automation") >= 3:
            return False
        prontas = [c for c in jogo.casas + jogo.armazens if c.pronta and self._visivel(jogo, c)]
        if not prontas:
            return False
        prontas.sort(key=lambda c: (c.estado != FOGO, c.desde))
        jogo.coletar(prontas[0])
        self._gastar(jogo, 1)
        return True

    def _religar(self, jogo):
        rede = jogo.rede
        if not rede.apagao or jogo.t - rede.apagao_desde < self.reacao:
            return False
        rede.religar(jogo, pelo_jogador=True)
        self._gastar(jogo, 1)
        return True

    def _gerador(self, jogo, proximo):
        """Compra o gerador quando a rede já passou do limite ou a próxima compra passaria."""
        rede, b = jogo.rede, jogo.b
        if not rede.ligada or "generator" not in jogo.ups:
            return False
        if proximo == "goal" and rede.demanda <= rede.capacidade:  # RITMO_V3: em sobrecarga compra mesmo antes da meta
            return False
        extra = {"automation": b["automationDemandPerLevel"], "base2": b["baseDemand"], "depot2": b["depotDemand"]}.get(proximo, 0)
        if rede.demanda + extra <= rede.capacidade or jogo.niveis["generator"] >= jogo.ups["generator"]["maxLevel"]:
            return False
        if jogo.comprar("generator"):
            self._gastar(jogo, 2)
        return True  # sem dinheiro: junta para o gerador antes da próxima compra

    def _comprar(self, jogo):
        while self.plano and jogo.niveis[self.plano[0]] >= jogo.teto(self.plano[0]):
            self.plano.pop(0)
        if self._gerador(jogo, self.plano[0] if self.plano else None):
            return jogo.t < self.ocupado_ate
        if not self.plano or not jogo.comprar(self.plano[0]):
            return False
        self.plano.pop(0)
        self._gastar(jogo, 2)
        return True

    def _despachar(self, jogo):
        if jogo.workers <= 0:
            return False
        espera = [c for c in jogo.casas if c.estado != INTACTA and not c.equipe and self._visivel(jogo, c)
                  and jogo.blocos.chamado(c)]
        if not espera:
            return False
        espera.sort(key=lambda c: (0, c.fogo) if c.estado == FOGO else (1, c.dano))
        automacao = jogo.nivel("automation")
        for c in espera:
            if jogo.custo_material(c) > jogo.material:
                continue
            if c.inc:  # enchente e meteoro: equipe fixa (a aba já abre nela); sem gente para ela, espera
                n = jogo.equipe_sugerida(c)
                if n > jogo.workers:
                    continue
            elif automacao >= 2:
                n = jogo.equipe_sugerida(c)
            elif self.equipe == "um":
                n = 1
            elif self.equipe == "sugerida":
                n = jogo.equipe_sugerida(c)
            else:  # espalha: divide a gente livre entre os chamados que esperam, até a equipe sugerida
                n = max(1, min(jogo.equipe_sugerida(c), jogo.workers // len(espera)))
            ajuste = abs(n - jogo.equipe_sugerida(c))  # a aba abre com a sugerida; '+'/'-' para mudar
            if jogo.enviar(c, n):
                if automacao >= 2:
                    self._gastar(jogo, 1)
                else:
                    self._gastar(jogo, (3 if automacao == 0 else 2) + ajuste, miras=2 if automacao == 0 else 1)
                return True
        return False

    def _repor(self, jogo):
        if jogo.workers <= 0:
            return False
        falta = sum(jogo.custo_material(c) for c in jogo.casas
                    if c.estado == DANIFICADA and not c.equipe and self._visivel(jogo, c))
        a_caminho = sum(a.enviados * jogo.material_por_trabalhador() for a in jogo.armazens if a.equipe)
        precisa = max(falta, self.estoque_alvo) - jogo.material - a_caminho
        if precisa <= 0:
            return False
        livre = next((a for a in jogo.armazens if not a.equipe), None)
        if livre is None:
            return False
        n = min(jogo.workers, max(1, math.ceil(precisa / jogo.material_por_trabalhador())))
        ajuste = abs(n - jogo.equipe_sugerida(livre))
        if not jogo.enviar(livre, n):
            return False
        nivel0 = jogo.nivel("automation") == 0
        self._gastar(jogo, (3 if nivel0 else 2) + ajuste, miras=2 if nivel0 else 1)
        return True


# Planos de compra (a meta "goal" sempre por último). Repetir o id = comprar mais um nível.
PLANOS = {
    # Achado pela busca (tabela.py --busca): carrinho cedo resolve o material, depois gente e ferramentas.
    "carrinho": ["workers", "cart", "cart", "income", "tools", "workers", "workers", "cart", "tools", "workers",
                 "reward", "workers", "automation", "automation", "speed", "goal"],
    "renda": ["workers", "income", "workers", "cart", "income", "speed", "workers", "income",
              "automation", "automation", "automation", "income", "goal"],
    "equilibrado": ["workers", "cart", "workers", "speed", "income", "workers", "reward", "speed",
                    "automation", "automation", "income", "automation", "income", "goal"],
    "automacao": ["workers", "cart", "workers", "cart", "automation", "workers", "income", "automation", "workers",
                  "automation", "tools", "workers", "automation", "income", "goal"],
    "gente": ["workers", "workers", "workers", "cart", "speed", "workers", "workers", "speed",
              "income", "income", "workers", "income", "goal"],
    # RITMO_V3: quem não conhece o jogo compra o que é barato, na ordem das abas, e só depois a renda e a automação.
    "ingenuo": ["workers", "speed", "workers", "tools", "cart", "workers", "reward", "speed", "workers", "income",
                "automation", "tools", "workers", "automation", "income", "goal"],
}


def com_base(plano):
    """RITMO_V3: o jogador usa a expansão; uma base nova logo depois da primeira automação (ou antes da meta)."""
    plano = [p for p in plano if p not in ("base2", "depot2")]
    i = plano.index("automation") + 1 if "automation" in plano else len(plano) - 1
    return plano[:i] + ["base2"] + plano[i:]


def com_pararaio(plano, quantos=1):
    """PARARAIO.md: para-raios logo depois da base nova (ou antes da meta), um seguido do outro."""
    plano = [p for p in plano if p != "rod"]
    i = plano.index("base2") + 1 if "base2" in plano else len(plano) - 1
    return plano[:i] + ["rod"] * quantos + plano[i:]


def com_guaritas(plano, quantas=1):
    """GUARITAS.md: guaritas logo depois da última automação (o despacho sozinho era o nível 4 dela)."""
    plano = [p for p in plano if p != "outpost"]
    i = len(plano) - 1 - plano[::-1].index("automation") + 1 if "automation" in plano else len(plano) - 1
    return plano[:i] + ["outpost"] * quantas + plano[i:]


def perfis(plano_bom="carrinho", expansao=True):
    plano = com_base(PLANOS[plano_bom]) if expansao else PLANOS[plano_bom]
    ingenuo = com_base(PLANOS["ingenuo"]) if expansao else PLANOS["ingenuo"]
    return {
        "ignora": None,
        "bom": Jogador("bom", reacao=1.0, clique=0.35, mira=1.0, plano=plano, equipe="espalha", estoque_alvo=6),
        "médio": Jogador("médio", reacao=2.0, clique=0.5, mira=1.6, plano=plano, equipe="sugerida", acerto=0.55),
        # RITMO_V3: demora para ver o chamado e para mirar, acerta pouco os mini games e compra sem plano.
        "ruim": Jogador("ruim", reacao=2.5, clique=0.6, mira=2.0, plano=ingenuo, equipe="sugerida", acerto=0.4),
        "bom, sem mini games": Jogador("bom-inc", reacao=1.0, clique=0.35, mira=1.0, plano=plano, equipe="espalha",
                                       estoque_alvo=6, mini=False),
        "médio, sem mini games": Jogador("médio-inc", reacao=2.0, clique=0.5, mira=1.6, plano=plano,
                                         equipe="sugerida", mini=False),
        "bom, larga aos 6 min": Jogador("bom6", reacao=1.0, clique=0.35, mira=1.0, plano=plano, equipe="espalha",
                                        estoque_alvo=6, para_em=360),
    }
