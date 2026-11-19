# Consertos v7B-1: três dificuldades no menu inicial (19/11)

## Pedido do Pietro

Escolher a dificuldade na tela inicial. Normal = o jogo como está (com o que a v7A ajusta nos números base). Fácil: um pouco menos desastres e o Centro de Operações um pouco mais barato. Difícil: mais desastres, a dificuldade crescendo mais rápido e o Centro um pouco mais caro. Nomes divertidos, com subtítulo que deixa óbvio qual é qual.

## O menu

O botão JOGAR virou três botões lado a lado, com "Escolha a dificuldade" em cima (`Assets/Systems/UI/MainMenu.cs`):

| Botão | Subtítulo | Cor |
|---|---|---|
| **GAROA** | Fácil · desastres mais calmos | dourado puxado para o verde |
| **TEMPORAL** | Normal · o jogo como ele é | dourado (pulsa: a sugestão) |
| **APOCALIPSE** | Difícil · caos sem trégua | dourado puxado para o vermelho |

Clicar guarda a escolha (`Difficulty.Set`) e começa a partida, com a mesma onda de antes. A escolha vale até o fim do Play (o recomeço mantém) e cada Play novo volta ao Normal.

## A regra (`Assets/Systems/Difficulty/Difficulty.cs`, pura)

Os números ficam em `Assets/SO/DifficultyData.cs` e `Assets/Resources/Difficulty/DifficultyData.asset` (um SO só da dificuldade, para não mexer no `BalanceData` que a v7A está ajustando). Tudo multiplica **por cima** dos números base:

| | Intervalo entre desastres | Velocidade da rampa | Custo do Centro de Operações |
|---|---|---|---|
| Fácil | × 1,15 (uns 13% menos desastres) | × 1 | × 0,85 (33.000 → 28.050) |
| Normal | × 1 | × 1 | × 1 |
| Difícil | × 0,9 (uns 11% mais desastres) | × 1,2 (o teto chega aos 7:30 em vez de 9:00 e segue subindo mais rápido) | × 1,15 (33.000 → 37.950) |

- **Onde age:** `DisasterManager.GetRampedInterval` e `GetRampedHouseCount` (a rampa enxerga o tempo × velocidade; o intervalo sai × multiplicador) e `UpgradesManager.GetCost` (só a meta). As funções estáticas da curva não mudaram.
- **O ritmo adaptativo (ADAPTATIVA_V6B)** continua agindo, em qualquer dificuldade.
- **Piso:** o multiplicador do intervalo nunca leva abaixo de 10 s (`intervalFloor`). Uma curva que já vinha abaixo disso fica como veio.
- **Ficam iguais:** o primeiro desastre (o tutorial), a hora em que cada tipo é liberado e o terremoto forte (fora a rampa mais rápida).

## Simulador (60 partidas por linha, números base do HEAD de 19/11)

Simulado com `--set` nos campos da curva (intervalo e rampa escalados) e `goal.baseCost`. É uma aproximação: no jogo, o multiplicador vem depois do corte do ritmo.

| Jogador | Fácil | Normal | Difícil |
|---|---|---|---|
| bom | 98%, 10:09 | 100%, 10:49 | 100%, 11:27 |
| médio | 100%, 15:29 | 100%, 15:54 | 48%, 18:51 |
| ruim | 78%, 17:48 | 51%, 19:13 | 0% (perde aos 9:03) |

- O primeiro rascunho do Difícil (× 0,85 e rampa × 1,25) derrubava o médio para 38%. Com × 0,9 e × 1,2, ele ficou em 48%.
- A derrota do bom no Fácil é 1 em 60 partidas: ruído.
- Os scripts estão em `analises-lilith/jogo-defesa-civil/v7b1/` (`sim_*.txt`).

## Testes

`Assets/Systems/Difficulty/Editor/DifficultyTests.cs` (6 testes, EditMode). Eles conferem:
- o Normal é o jogo como está (curva e Centro iguais);
- o Fácil tem intervalo maior e Centro mais barato;
- o Difícil tem intervalo menor, mais casas em algum momento, teto antes e Centro mais caro;
- os multiplicadores ficam a no máximo 25% do Normal;
- o piso do intervalo;
- a escolha atual chega ao custo do Centro.
