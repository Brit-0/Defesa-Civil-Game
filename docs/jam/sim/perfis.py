"""Jogadores-robô do simulador: tempo de reação, tempo por clique e a ordem em que compram upgrades.

Custo de cada ação em cliques, como no jogo (GDD, seção 7):
  despacho nível 0: casa + base + '+' por trabalhador + Enviar; nível 1 tira a base; nível 2 em diante, 1 clique
  Armazém: igual ao nível 0 (no nível 1 em diante a base sai sozinha; a equipe é sempre escolhida)
  coleta: 1 clique (nível 3 coleta sozinho); compra: abrir a aba + clicar no botão
A cada ação soma-se a "mira" (achar a casa na tela, mover a câmera); escolher a base à mão (nível 0) é uma
segunda mira, porque a Guarda raramente está na mesma parte da tela que a casa.
"""
import math

from sim import DANIFICADA, FOGO, INTACTA


class Jogador:
    def __init__(self, nome, reacao, clique, mira, plano, equipe="espalha", estoque_alvo=0, para_em=None):
        self.nome = nome
        self.reacao, self.clique, self.mira = reacao, clique, mira
        self.plano = list(plano)
        self.equipe = equipe              # "espalha", "sugerida" ou "um"
        self.estoque_alvo = estoque_alvo  # material que tenta manter no estoque nas horas calmas (0 = só repõe quando falta)
        self.para_em = para_em            # segundos de partida em que larga o mouse (None = joga até o fim)
        self.ocupado_ate = 0.0
        self.cliques = 0

    def copia(self):
        return Jogador(self.nome, self.reacao, self.clique, self.mira, self.plano, self.equipe, self.estoque_alvo, self.para_em)

    def _gastar(self, jogo, cliques, miras=1):
        self.cliques += cliques
        self.ocupado_ate = jogo.t + self.mira * miras + self.clique * cliques

    def agir(self, jogo):
        if jogo.t < self.ocupado_ate or (self.para_em is not None and jogo.t >= self.para_em):
            return
        if self._coletar(jogo) or self._comprar(jogo) or self._despachar(jogo) or self._repor(jogo):
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

    def _comprar(self, jogo):
        while self.plano and jogo.nivel(self.plano[0]) >= jogo.ups[self.plano[0]]["maxLevel"]:
            self.plano.pop(0)
        if not self.plano or not jogo.comprar(self.plano[0]):
            return False
        self.plano.pop(0)
        self._gastar(jogo, 2)
        return True

    def _despachar(self, jogo):
        if jogo.workers <= 0:
            return False
        espera = [c for c in jogo.casas if c.estado != INTACTA and not c.equipe and self._visivel(jogo, c)]
        if not espera:
            return False
        espera.sort(key=lambda c: (0, c.fogo) if c.estado == FOGO else (1, c.dano))
        automacao = jogo.nivel("automation")
        for c in espera:
            if jogo.custo_material(c) > jogo.material:
                continue
            if automacao >= 2:
                n = jogo.equipe_sugerida(c)
            elif self.equipe == "um":
                n = 1
            elif self.equipe == "sugerida":
                n = jogo.equipe_sugerida(c)
            else:  # espalha: divide a gente livre entre os chamados que esperam, até a equipe sugerida
                n = max(1, min(jogo.equipe_sugerida(c), jogo.workers // len(espera)))
            if jogo.enviar(c, n):
                if automacao >= 2:
                    self._gastar(jogo, 1)
                else:
                    self._gastar(jogo, (3 if automacao == 0 else 2) + n, miras=2 if automacao == 0 else 1)
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
        if not jogo.enviar(livre, n):
            return False
        nivel0 = jogo.nivel("automation") == 0
        self._gastar(jogo, (3 if nivel0 else 2) + n, miras=2 if nivel0 else 1)
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
}


def perfis(plano_bom="carrinho"):
    plano = PLANOS[plano_bom]
    return {
        "ignora": None,
        "bom": Jogador("bom", reacao=1.0, clique=0.35, mira=1.0, plano=plano, equipe="espalha", estoque_alvo=6),
        "médio": Jogador("médio", reacao=2.0, clique=0.5, mira=1.6, plano=plano, equipe="sugerida"),
        "bom, larga aos 6 min": Jogador("bom6", reacao=1.0, clique=0.35, mira=1.0, plano=plano, equipe="espalha",
                                        estoque_alvo=6, para_em=360),
    }
