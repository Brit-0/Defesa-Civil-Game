# Balanceamento v6F: repasse pelo caixa, carrinho e armazém mais caros, seta da válvula (#531, 18/11)

Pedido do Pietro:
1. A ponta da seta da válvula fica no começo do círculo e mostra o sentido do giro.
2. Material ainda sai fácil demais: Carrinho de mão e armazéns novos mais caros.
3. O Repasse da Prefeitura dava dinheiro demais já no começo. Agora ele depende do dinheiro que o jogador tem.

## Repasse da Prefeitura

- **Antes:** a cada 10 s, `1,2 × nível × casas intactas × (1 − caos)`, com ou sem dinheiro no caixa. O nível 1 custava R$ 150 e pagava R$ 184 a cada 10 s, então se pagava em 10 s.
- **Agora:** `nível × (0,3 × casas intactas + 15% do caixa)`, até o teto de `14 × casas intactas` (o mesmo em todo nível), vezes `(1 − caos)³`.
  - Com o caixa vazio, paga só o piso.
  - Quem junta dinheiro para a meta chega ao teto. O nível encurta o caminho: no nível 5, uns R$ 2.550 no caixa já bastam.
  - O `(1 − caos)³` corta forte quando a cidade está em crise e separa quem segura o caos.

Quanto paga a cada 10 s, com as 153 casas intactas:

| Nível | Caixa | Caos | Antes | Agora |
|---|---|---|---|---|
| 1 | 0 | 0 | 184 | 46 |
| 1 | 500 | 0 | 184 | 121 |
| 3 | 500 | 0 | 551 | 363 |
| 5 | 500 | 0 | 918 | 604 |
| 5 | 5.000 | 0,2 | 734 | 1.097 |
| 5 | 5.000 | 0,5 | 459 | 268 |

Campos novos no `BalanceData`: `incomeFloorPerHouse` (0,3), `incomeMoneyShare` (0,15) e `incomeChaosPower` (3). O `incomePerHouse` virou o teto (era 1,2 por nível, agora 14). A conta pura está em `Stats.PassiveIncome`, com testes em `PassiveIncomeTests`, e o `sim.py` faz a mesma conta em `repasse()`.

## Carrinho de mão e Novo armazém

| Upgrade | Antes (base × crescimento) | Níveis antes | Agora | Níveis agora |
|---|---|---|---|---|
| Carrinho de mão (`cart`) | 25 × 1,8 | 25 / 45 / 81 / 146 (total 297) | 50 × 2,5 | 50 / 125 / 312 / 781 (total 1.268, 4,3×) |
| Novo armazém (`depot2`) | 300 × 1,6 | 300 / 480 | 750 × 2 | 750 / 1.500 (2,5× e 3,1×) |

## Compensação

- `moneyPerDamage` passou de 18 para 52. Agora o dinheiro do começo vem do conserto, não do repasse. Sem essa mudança, o médio e o ruim perdiam entre 10 e 13 min, porque a escalada dos desastres pede um dinheiro que eles já não tiravam do repasse.
- A meta continua em 33.000. O `fireReward` continua em 20: com 58, o ruim vencia 90% e ficava fácil demais.

## Simulador

`python -I -X utf8 docs/jam/sim/tabela.py --assets Assets/Resources --sementes 60`. O "antes" é o mesmo comando sobre um `git archive` do HEAD. O começo do jogo foi medido com `analises-lilith/jogo-defesa-civil/v6f/exp.py`.

| Jogador | Vence | Tempo até vencer, mediana | Do repasse na partida | Repasse até 5 min | Caixa aos 5 min | Fim das compras |
|---|---|---|---|---|---|---|
| bom | 100% → 100% | 10:34 → 10:37 | 34.788 → 32.384 | 10.985 → 3.452 | 5.549 → 146 | 5:16 → 6:06 |
| médio | 100% → 100% | 15:12 → 15:47 | 33.825 → 26.930 | 3.085 → 707 | 293 → 200 | 6:45 → 10:58 |
| ruim | 73% → 75% | 19:20 → 18:53 | 31.167 → 20.146 | 0 → 364 | 84 → 254 | 11:16 → 11:58 |
| muito eficiente | 100% → 100% | 10:20 → 10:24 | 34.770 → 33.117 | 11.104 → 3.524 | 5.654 → 162 | 5:16 → 6:02 |

- **Antes:** aos 5 min o bom já tinha comprado o plano inteiro, e o dinheiro sobrava.
- **Agora:** ele termina as compras aos 6 min, sem dinheiro parado, e o repasse grande só aparece quando ele junta para a meta.
- **Tempos:** os perfis continuam perto de 10, 15 e 20 min. O ruim varia uns 4 pontos de vitória entre duas rodadas iguais do simulador (71% e 75% com as mesmas 60 sementes).

## Seta da válvula

- **Antes:** em `BlockDisasterArt.TurnArrow`, a ponta ficava na ponta direita do vão do arco, no rabo do giro. Como a dica gira no sentido horário, a seta parecia andar de ré.
- **Agora:** a ponta sai da ponta esquerda do arco e aponta para o vão, na frente do giro.

## Conferências (18/11)

- Testes EditMode: 220 passaram, 0 falharam, com os 4 novos de `PassiveIncomeTests`.
- `ValidateAll`, fora do Play: 0 erros e 0 avisos.
- Play curto: guarita comprada, enchente forçada, equipe no bueiro, válvula girada pelo mouse até escoar. Nenhum erro no console. Com o jogo pausado e o mouse solto, o print mostra a ponta da seta na frente do giro horário, e a loja mostra o Carrinho de mão a 50. O print está em `analises-lilith/jogo-defesa-civil/v6f/seta_seta.png` (1920×1080), com o recorte em `seta_zoom.png`.

## Fora daqui

A seção 8 do `GDD.md` ainda descreve a conta antiga do repasse. Fica para o MEGABRAIN atualizar.
