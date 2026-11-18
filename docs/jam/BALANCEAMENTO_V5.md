# Balanceamento v5 (17 e 18/11)

## Pedidos do Pietro

**Primeiro pedido:**
- O jogador médio zera em cerca de 15 min, o avançado em cerca de 10 e o inexperiente em cerca de 20, com muita dificuldade.
- A escalada fica um pouco mais forte e, depois do teto, continua subindo mais devagar.
- Entram na conta guaritas, para-raio, enchente (por volta dos 4 min) e meteoro (por volta dos 8 min).

**Segundo pedido, mantendo os tempos 10/15/20:**
1. Só o início é calmo. Depois o intervalo cai logo e a escalada fica mais rápida.
2. A Automação fica mais cara.
3. O objetivo final fica bem mais caro.
4. Coletar material demora mais.
5. Guarita e para-raio sobem menos de preço a cada compra.
6. O armazém novo fica mais caro.

Simulador: `docs/jam/sim`. Tabelas com 60 partidas por linha. Para refazer:

```
python -X utf8 docs/jam/sim/tabela.py --sementes 60                            # depois (padrões do C#)
python -X utf8 docs/jam/sim/tabela.py --sementes 60 --assets Assets/Resources  # lendo os .asset: mesmo resultado (±1 s)
python -X utf8 docs/jam/sim/tabela.py --curva
python -X utf8 docs/jam/sim/tabela.py --curto --plano carrinho                 # a tabela de antes, planos curtos do RITMO_V3
```

## Resultado

| Jogador | Antes (planos curtos) | Números de antes com os planos novos | Depois |
|---|---|---|---|
| bom (avançado) | 100%, 8:00 | 100%, 8:05 | **100%, 10:21** (10:09–10:32); pico do caos 0,43 |
| médio | 100%, 8:10 | 100%, 10:44 | **100%, 15:12** (14:43–15:43); pico 0,64 |
| ruim (inexperiente) | 93%, 9:55 | 91%, 14:45 | **75%, 19:36** (18:47–20:39); pico 0,89; perde 25% das vezes, metade delas perto dos 10:20 |
| ignora (não faz nada) | perde aos 5:37 | perde aos 5:36 | perde aos 6:10 |
| bom, sem mini games | 100%, 8:23 | 100%, 8:56 | 100%, 11:06 |
| médio, sem mini games | 100%, 9:22 | 88%, 11:58 | 100%, 16:32 |
| bom, larga aos 6 min | perde aos 8:50 | perde aos 8:47 | perde aos 8:58 |

**Antes:** os três empatavam perto dos 8 a 10 min. A meta custava 2500 e o plano era curto, e quase todo o dinheiro vinha do repasse, que não depende de jogar bem.

**Agora:** a meta custa 33000, e os perfis se separam pela ordem de compra e pelo caos.
- **Bom:** compra o Repasse cedo e segura o caos; 91% do dinheiro dele vem do repasse.
- **Ruim:** deixa o Repasse por último, conserta muito (162 consertos), apanha mais e chega a 0,89 de caos.

## Curva da dificuldade (intervalo / casas por desastre / casas no terremoto forte)

| Minuto | Antes | Depois |
|---|---|---|
| 0 | 50 s / 2 / 5 | 50 s / 2 / 4 |
| 1 | 47,7 s / 2 / 5 | 50 s / 2 / 4 (fim da calma) |
| 2 (incêndio) | 45,3 s / 3 / 8 | 46,4 s / 2 / 4 |
| 4 (enchente) | 40,7 s / 3 / 8 | 39,1 s / 4 / 8 |
| 6 | 36 s / 4 / 10 | 31,9 s / 4 / 8 |
| 8 (meteoro) | 31,3 s / 5 / 12 | 24,6 s / 6 / 12 |
| 9 | 29 s / 5 / 12 | **21 s / 6 / 12 (o teto)** |
| 12 | **22 s / 6 / 15 (o teto)** | 19,8 s / 6 / 12 |
| 15 | 22 s / 6 / 15 | 18,6 s / 7 / 14 |
| 20 | 22 s / 6 / 15 | 16,6 s / 8 / 16 |
| 24 | 22 s / 6 / 15 | 15 s / 8 / 16 |

- **Calma do começo:** o primeiro minuto fica parado em 50 s e 2 casas (`disasterRampDelay`).
- **Rampa:** de 1:00 a 9:00, o intervalo cai 3,6 s por minuto. Antes caía 2,3 s por minuto, até os 12:00.
- **Depois do teto:** antes ficava parado. Agora:
  - o intervalo cai 0,4 s por minuto, até o piso de 15 s, aos 24:00;
  - cada desastre ganha 0,15 casa por minuto, até 9 casas, aos 29:00.
- **Terremoto forte:** 2× as casas da rampa, em vez de 2,5×. O segundo forte matava quem passava dos 10 min.

## Números que mudaram (antes = último commit)

| Arquivo | Campo | Antes | Depois |
|---|---|---|---|
| `Assets/SO/BalanceData.cs` e `Resources/Balance/BalanceData.asset` | `disasterRampDelay` (novo) | — | 60 |
| | `disasterRampDuration` | 720 | 480 |
| | `disasterIntervalEnd` | 22 | 21 |
| | `disasterLateIntervalPerMinute` / `disasterIntervalFloor` (novos) | — | 0,4 / 15 |
| | `disasterLateHousesPerMinute` / `disasterHousesMax` (novos) | — | 0,15 / 9 |
| | `restockTime` (ida ao Armazém, em s) | 3 | 5 (volta ao GDD) |
| | `chaosLimit` | 20 | 25 |
| | `moneyPerDamage` | 15 | 18 |
| | `incomePerHouse` | 0,5 | 1,2 |
| `BalanceData.cs` (o .asset não guarda este campo) | `strongDisasterHouseMultiplier` | 2,5 | 2 |
| `Resources/Upgrades/Goal.asset` e `JamDataMenu.cs` | `baseCost` | 2500 | 33000 |
| `Automation.asset` e `JamDataMenu.cs` | `baseCost` × `costGrowth` | 80 × 2 (80, 160, 320) | 150 × 2,4 (150, 360, 864) |
| `Income.asset` e `JamDataMenu.cs` | `costGrowth` | 2 | 1,4 |
| `Workers.asset` e `JamDataMenu.cs` | `costGrowth` | 1,45 | 1,6 |
| `Rod.asset` e `LightningRodDataMenu.cs` | `costGrowth` | 1,5 (80, 120, 180) | 1,2 (80, 96, 115) |
| `Outpost.asset` e `OutpostDataMenu.cs` | `costGrowth` | 1,25 (90, 112, 141) | 1,1 (90, 99, 109) |
| `Depot2.asset` e `ExpansionDataMenu.cs` | `baseCost` | 90 (90, 144) | 300 (300, 480) |

Compensações:
- Caos 25, R$ 18 por dano e repasse 1,2 compensam o resto do pedido.
- Sozinhos, a Automação cara, o restock de 5 s ou o armazém de 300 derrubavam o ruim de 81% para 38–46% de vitórias.
- A rampa curta sem compensação levava o ruim a 0%.
- O repasse mais forte paga a meta mais cara sem mudar o tempo de quem joga bem.

Mudanças no código e nos testes:
- **`DisasterManager`:**
  - `GetRampProgressTime` desconta a calma;
  - `GetCeilingTime` dá a hora do teto;
  - `GetInterval` e `GetHouseCount` fazem a rampa e a subida depois do teto.
  - Com as taxas e a calma em 0, a curva volta à de antes.
- **Testes:** `Assets/Systems/Disasters/Editor/DebutTests.cs` tem dois novos, `RampReachesCeilingThenKeepsRisingSlowly` e `LateRiseOffKeepsTheCeiling`. Eles conferem os mesmos números da curva: calma, meio da rampa, teto, 5 min depois, piso e máximo.

## Simulador

- **`sim.py`:**
  - `tempo_rampa` (a calma);
  - `intervalo_desastre`/`casas_por_desastre` com a subida depois do teto, como o C#.
- **`perfis.py`:** partidas de 10 a 20 min pedem planos longos.
  - Os três compram a base nova, o para-raio, **um armazém novo**, as guaritas e uma continuação antes da meta.
  - Bom: `renda`. Médio: `carrinho`. Ruim: `ingenuo`, com a continuação ingênua (Repasse por último).
  - `--curto` volta aos planos do RITMO_V3.
- **`tabela.py`:**
  - fases de 4 em 4 minutos até 24;
  - estreias de para-raio, enchente, guarita e meteoro;
  - `--curva` com o terremoto forte e o tipo novo;
  - opção nova: `--plano-medio`.

Ressalvas:
- O simulador não reproduz o jogador de verdade; os tempos valem para os perfis dos robôs.
- A calibragem final é com partida jogada. Os botões para ajustar:
  - `goal.baseCost`: o tempo de todos;
  - `chaosLimit`: a folga do inexperiente;
  - `incomePerHouse`: a vantagem de quem compra Repasse cedo;
  - as taxas depois do teto: o aperto do fim.
