# Ritmo v3: dificuldade e introdução mais lentas (#533, 17/11/2026)

Pedido do Pietro: o scaling de dificuldade e o ritmo um pouco mais lentos, para o jogador se acostumar com cada sistema
aos poucos (casas quebradas, incêndio com jato, tempestade, expansão, energia) até lidar com tudo junto. A partida média
continua em 8 a 10 minutos e o jogador médio consegue vencer.

Só mudaram dados de balanceamento (`BalanceData`, `FireEnergyData`, padrões do C# e os `.asset` de `Resources/Balance`)
e o simulador (`docs/jam/sim`). Nenhuma regra de jogo, UI ou mundo.

## Resumo

| | Antes | Depois |
|---|---|---|
| Ordem em que os sistemas aparecem (médio, mediana) | tempestade 1:57, energia 2:30, terremoto forte 3:48, base nova 3:59, fogo 4:41 | fogo 2:31, tempestade 3:37, base nova 4:05, energia 5:15, terremoto forte 6:13 |
| Janela em que tudo chega | entre 1:35 e 4:00 (quatro sistemas novos em 2 min e meio) | um sistema novo a cada 1 a 1,5 min, tudo junto a partir de 6:00 |
| Chamados por minuto nos minutos 2 a 4 (médio) | 7,0 | 2,5 |
| Médio: vence / tempo (mediana) | 100% / 7:58 | 100% / 8:14 |
| Ruim: vence / tempo | 73% / 11:02 | 83% / 10:08 |
| Bom: vence / tempo | 100% / 6:49 | 100% / 7:30 |
| Quem não joga perde aos | 3:53 | 5:32 |

No jogo de verdade somam-se o tempo do tutorial e o do tutorial da energia (os desastres esperam e a rampa não conta,
mas o relógio da tela de fim conta): a partida média fica perto de 9 a 10 minutos.

## A curva nova: quando cada sistema entra

| Sistema | Antes | Depois | Dado que controla |
|---|---|---|---|
| Casas quebradas (terremoto) | 0:06 (tutorial, 2 casas); depois 3 casas a cada 45 s | 0:06 (tutorial, 2 casas); depois 2 casas a cada 50 s até 1:30 e 3 casas até 4:30 | `disasterHousesStart` 3 → 2, `disasterIntervalStart` 45 → 50 |
| Incêndio com jato | liberado aos 3:00, mas o raio da tempestade já punha fogo desde 1:35 | liberado aos 1:30, antes da tempestade; estreia mediana 2:31 (o jato e a dica aparecem sozinhos, num fogo de 1 ou 2 casas) | `fireUnlockTime` 180 → 90, `fireWeight` 15 → 30 |
| Tempestade | liberada aos 1:30 | liberada aos 2:50; estreia mediana 3:37 (p10 3:15) | `stormUnlockTime` 90 → 170, `stormWeight` 35 → 50 |
| Expansão (base nova) | compra do jogador (sem trava) | igual; o médio do simulador ergue a base perto de 4:05, antes da energia | preços sem mudança (150 e 90) |
| Energia (e apagões) | 2:30 | 5:15, depois da tempestade e da base nova: o apagão chega numa tempestade que o jogador já conhece | `FireEnergyData.unlockTime` 150 → 315 |
| Terremoto forte | 5º desastre, perto de 3:48, 8 casas | 8º desastre, perto de 6:10, 10 casas: o primeiro teste com tudo junto | `strongDisasterEvery` 5 → 8, `strongDisasterHouseMultiplier` 2 → 2,5 |

Depois que todos os tipos estão liberados, o sorteio fica em terremoto 33%, tempestade 42% e incêndio 25%
(era 50%, 35% e 15%): pesos maiores para o tipo novo aparecer logo depois de liberado.

## Frequência de chamados e tamanho da escalada

A curva dos dados, minuto a minuto (`python docs/jam/sim/tabela.py --curva`; chamados nominais = casas por desastre ÷
intervalo, sem o sorteio). A curva nova fica abaixo da antiga em todos os minutos e chega ao mesmo fim (6 casas a cada
20 a 22 s), só que mais devagar no começo.

| Minuto | Intervalo (s) antes → depois | Casas por desastre | Chamados/min | Tipos no sorteio antes / depois | Energia antes / depois |
|---|---|---|---|---|---|
| 0 | 45 → 50 | 3 → 2 | 4,0 → 2,4 | terremoto / terremoto | não / não |
| 1 | 43 → 48 | 3 → 2 | 4,2 → 2,5 | terremoto / terremoto | não / não |
| 2 | 41 → 45 | 3 → 3 | 4,4 → 4,0 | + tempestade / + incêndio | não / não |
| 3 | 39 → 43 | 4 → 3 | 6,1 → 4,2 | todos / todos | sim / não |
| 4 | 37 → 41 | 4 → 3 | 6,4 → 4,4 | todos / todos | sim / não |
| 5 | 35 → 38 | 4 → 4 | 6,8 → 6,3 | todos / todos | sim / não |
| 6 | 33 → 36 | 4 → 4 | 7,2 → 6,7 | todos / todos | sim / sim |
| 7 | 32 → 34 | 5 → 4 | 9,5 → 7,1 | todos / todos | sim / sim |
| 8 | 30 → 31 | 5 → 5 | 10,1 → 9,6 | todos / todos | sim / sim |
| 9 | 28 → 29 | 5 → 5 | 10,8 → 10,3 | todos / todos | sim / sim |
| 10 | 26 → 27 | 5 → 5 | 11,6 → 11,3 | todos / todos | sim / sim |

Fim da rampa: antes aos 13:00 (20 s, 6 casas); depois aos 12:00 (22 s, 6 casas).

## Caos

`chaosLimit` 22 → 20 (o valor do GDD). Com menos casas quebradas no começo a barra fica baixa até o minuto 6; o limite
menor devolve a pressão na fase com tudo junto, para o jogo não virar passeio. Subida e descida da barra sem mudança.

Pico do caos em cada fase (mediana das partidas ainda em jogo):

| Jogador | min 0–2 | min 2–4 | min 4–6 | min 6–8 | min 8–10 |
|---|---|---|---|---|---|
| bom, antes | 0,05 | 0,18 | 0,18 | — | — |
| bom, depois | 0,05 | 0,15 | 0,20 | 0,30 | — |
| médio, antes | 0,14 | 0,38 | 0,36 | 0,45 | — |
| médio, depois | 0,10 | 0,20 | 0,25 | 0,45 | — |
| ruim, antes | 0,23 | 0,50 | 0,55 | 0,73 | 0,68 |
| ruim, depois | 0,20 | 0,23 | 0,35 | 0,55 | 0,55 |

## Números que mudaram

| Campo | Antes | Depois | Arquivo |
|---|---|---|---|
| `disasterHousesStart` | 3 | 2 | `BalanceData` (.cs e .asset) |
| `disasterIntervalStart` | 45 | 50 | `BalanceData` |
| `disasterIntervalEnd` | 20 | 22 | `BalanceData` |
| `disasterRampDuration` | 780 | 720 | `BalanceData` |
| `fireUnlockTime` | 180 | 90 | `BalanceData` |
| `stormUnlockTime` | 90 | 170 | `BalanceData` |
| `earthquakeWeight` / `stormWeight` / `fireWeight` | 50 / 35 / 15 | 40 / 50 / 30 | `BalanceData` |
| `strongDisasterEvery` | 5 | 8 | `BalanceData.cs` (o .asset não guarda o campo; vale o padrão da classe) |
| `strongDisasterHouseMultiplier` | 2 | 2,5 | `BalanceData.cs` (idem) |
| `chaosLimit` | 22 | 20 | `BalanceData` |
| `unlockTime` (energia) | 150 | 315 | `FireEnergyData` (.cs e .asset) |

Sem mudança: meta (2500), dinheiro e tempos de trabalho, fogo (40 s), aviso (4 s), capacidade inicial da energia (5),
gerador, preços da expansão (150 e 90) e das melhorias. Com o robô comprando o gerador quando passa do limite, a
capacidade 5 e a 6 deram o mesmo resultado (sobrecarga de 1 s na mediana).

## Simulador: 30 partidas por perfil

`python docs/jam/sim/tabela.py` (os padrões do C# e os `.asset` dão a mesma tabela). Perfis: **bom** (reage em 1 s),
**médio** (2 s, mira 1,6 s, acerta 55% dos mini games) e **ruim** (novo: 2,5 s, mira 2 s, acerta 40%, compra o que é
barato na ordem das abas). Os três agora erguem uma base nova logo depois da primeira automação. O "antes" é o mesmo
simulador com os `.asset` do HEAD da `jam` (0e7ca50).

| Jogador | Vence | Perde | Tempo até vencer, mediana (p10–p90) | Tempo até perder | Consertos | Pico do caos |
|---|---|---|---|---|---|---|
| ignora, antes | 0% | 100% | — | 3:53 (3:46–4:47) | 0 | 1,00 |
| ignora, depois | 0% | 100% | — | 5:32 (4:56–6:07) | 0 | 1,00 |
| bom, antes | 100% | 0% | 6:49 (6:31–7:09) | — | 16 | 0,23 |
| bom, depois | 100% | 0% | 7:30 (7:17–7:48) | — | 18 | 0,25 |
| médio, antes | 100% | 0% | 7:58 (7:20–8:19) | — | 36 | 0,43 |
| médio, depois | 100% | 0% | 8:14 (7:52–8:45) | — | 31 | 0,45 |
| ruim, antes | 73% | 26% | 11:02 (10:15–11:43) | 12:01 (11:15–12:29) | 63 | 0,86 |
| ruim, depois | 83% | 16% | 10:08 (9:27–10:52) | 11:09 (11:04–11:12) | 49 | 0,60 |

O ruim que perde, perde no segundo terremoto forte (perto de 11:05, 15 casas): quem não fechou a meta até ali encontra o
pico maior. Sem mini games, o bom vence em 8:12 e o médio em 8:52 (100% os dois).

Estreia de cada sistema (minuto de partida, mediana e p10–p90; "fogo" é a primeira casa em chamas, de incêndio ou de raio):

| Jogador | fogo | incêndio | tempestade | base nova | energia | terremoto forte |
|---|---|---|---|---|---|---|
| médio, antes | 4:41 (1:38–6:18) | 5:59 (4:41–6:47), em 63% | 1:57 (1:35–4:40) | 3:59 (3:20–4:17) | 2:30 | 3:48 (3:41–3:55) |
| médio, depois | 2:31 (1:44–4:47) | 2:31 (1:44–4:06), em 93% | 3:37 (3:15–5:20) | 4:05 (3:41–4:33) | 5:15 | 6:13 (6:00–6:20) |
| ruim, antes | 4:01 (1:38–5:55) | 6:47 (4:47–9:13) | 2:39 (1:35–5:53) | 6:23 (5:53–6:50) | 2:30 | 3:48 (3:41–3:55) |
| ruim, depois | 2:08 (1:44–4:06) | 2:08 (1:44–7:10) | 3:37 (3:15–5:20) | 6:04 (5:35–6:31) | 5:15 | 6:06 (6:00–6:20) |

Chamados por minuto (casas que quebraram ou pegaram fogo; o bom escora muitas e por isso vê menos):

| Jogador | min 0–2 | min 2–4 | min 4–6 | min 6–8 | min 8–10 |
|---|---|---|---|---|---|
| bom, antes | 1,0 | 4,0 | 2,5 | — | — |
| bom, depois | 1,0 | 1,8 | 3,0 | 5,0 | — |
| médio, antes | 2,8 | 7,0 | 4,0 | 7,5 | — |
| médio, depois | 2,0 | 2,5 | 4,5 | 7,0 | — |
| ruim, antes | 4,0 | 8,0 | 4,0 | 8,5 | 7,8 |
| ruim, depois | 3,2 | 3,0 | 4,5 | 8,5 | 7,5 |

Mini games e energia (mediana): o médio apaga 3 fogos com o jato (eram 2) e o ruim deixa 8 crescerem (eram 6), porque
há mais incêndio no sorteio; os dois compram 1 nível de gerador; negados 0.

## O que os dados não garantem (sugestão de código, não aplicada)

O tipo novo só entra no sorteio; a primeira aparição ainda é sorte. Na mediana a ordem sai certa, mas em 1 de cada 10
partidas a primeira tempestade passa de 5:20 e cai junto da energia (5:15). O simulador tem a opção
`--estreia-garantida`, que imita uma regra de poucas linhas no `DisasterManager.PickDisaster`: o primeiro sorteio depois de
liberar um tipo é esse tipo. Com ela o fogo estreia sempre em 1:45 e a tempestade em 3:15, o médio vence em 8:16 e o ruim
vence 96% em 10:23. Fica para o MEGABRAIN decidir, porque a tarefa era só de dados.

## Partida no Play

Uma partida inteira no Editor da cópia `jam-v3c` (17/11), com os dados novos, jogada por um robô de nível médio. O robô usa
as mesmas chamadas do jogo: despacho pela base com energia mais perto e equipe sugerida, jato no fogo, coleta do
conserto, compras pelo plano carrinho com base nova, gerador quando a rede passa do limite e religar no apagão. Os dois
tutoriais (`Tutorial` e `EnergyTutorial`) foram tirados só durante o Play: o da energia segura os desastres e força um
apagão, o que esticaria a partida. Nada foi gravado em PlayerPrefs nem na cena.

| Tempo | O que aconteceu |
|---|---|
| 0:06 e 0:56 | terremoto de 2 casas (só casas quebradas no primeiro minuto e meio) |
| 1:44 | **primeiro incêndio** (1 casa, aviso aos 1:40) |
| 2:30 | terremoto de 3 casas |
| 3:15 | **primeira tempestade** (3 casas) |
| 4:05 e 4:46 | incêndios de 2 casas |
| 4:57 | **base nova** comprada (terreno oeste) |
| 5:15 | **energia liberada**; gerador comprado aos 5:17 |
| 5:25 | terremoto de 4 casas |
| 6:05 | **primeiro terremoto forte** (10 casas) |
| 7:02, 7:35, 8:07 | incêndios de 1 e 2 casas |
| 8:38 | tempestade de 5 casas com apagão; religado aos 8:42 |
| 9:16 | terremoto de 5 casas |
| 9:40 | meta comprada: **vitória** |

Resultado: vitória aos **9:40**, 40 casas consertadas, 14 desastres, 3903 de dinheiro ganho, pico do caos em 65% da
barra, 4 focos apagados no jato e 9 que cresceram. Casas atingidas por minuto: 4, 1, 3, 3, 4, 4, 10, 3, 7, 5. Os
sistemas entraram um por vez, com 40 s a 1:30 entre um e outro, e só depois dos 6 minutos o jogador teve tudo junto. A
partida no Play durou cerca de 1:30 a mais que a média do simulador para o médio (8:14), porque o robô do Play anda e
coleta no tempo real do jogo. Ainda assim, fica dentro dos 8 a 10 minutos.

Prints (1280x720) em `analises-lilith/jogo-defesa-civil/resultados/`:
- `62_comeco.png` (1:05): começo calmo, 2 casas quebradas e o chamado na borda da tela;
- `62_tempestade.png` (3:18): a primeira tempestade com chuva;
- `62_meio.png` (5:45): energia 8/8 no HUD, 3/10 trabalhadores, casas rachadas com os pinos e a base nova no oeste;
- `62_fim.png` (9:40): CIDADE SALVA, com tempo 09:40, 40 consertos, 14 desastres e 3903 de dinheiro.

Console: nenhum erro do jogo. Os 5 erros do console vieram das ferramentas: o servidor do Unity CLI e o índice de busca
do Editor ao abrir.

## Mudanças no simulador

- `perfis.py`: perfil **ruim** e plano **ingenuo**; `com_base` põe uma base nova nos planos (desliga com
  `--sem-expansao`); o robô compra o gerador quando a rede já passou do limite, mesmo com a meta como próxima compra
  (antes ficava 200 s com a automação desligada).
- `tabela.py`: usa a partida com os terrenos da expansão (`expansao.py`) quando há `base2`/`depot2`; tabelas de estreia
  de cada sistema, chamados por minuto e pico do caos por fase; `--curva` e `--estreia-garantida`.
- `sim.py` e `fogo_energia.py`: guardam a estreia de cada sistema, os chamados e o caos por minuto.
- `dados.py`: lê `Base2.asset` e `Depot2.asset` como já lia o gerador.

Para repetir: `python -X utf8 docs/jam/sim/tabela.py` (depois), `--curva`, `--linha médio`, `--estreia-garantida`.
