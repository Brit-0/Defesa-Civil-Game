# Dificuldade adaptativa v6B (18/11)

## Pedido do Pietro

Quando o jogador fica muito eficiente (poucos chamados pendentes, caos baixo, muito dinheiro e upgrades), o ritmo do jogo não acompanha. A partir de agora, o ritmo e o tamanho dos desastres sobem junto com a eficiência medida. A subida é suave e tem teto, e o iniciante não é punido: os tempos para zerar continuam ~20 min (inexperiente), ~15 (médio) e ~10 (avançado).

**O que o simulador mostrava antes:** o bom compra quase tudo até os 3 min. Dos 4 aos 8 min ele fica com caos de 0,03 a 0,07 e fila quase vazia, só juntando dinheiro para a meta.

## A regra (`Assets/Systems/Disasters/DisasterPace.cs`, regra pura)

O `DisasterManager` guarda um **ritmo** de 0 a 1. Ele é medido a cada quadro em `GameState.Playing`, também durante o desastre, com `GameClock.DeltaTime`. A pausa e o tutorial (`Hold`) não contam.

1. **Médias de 45 s** (`paceAverageTime`, média que esquece devagar e não depende do fps):
   - do caos da barra;
   - dos chamados esperando equipe (`GuardManager.WaitsForTeam`; a enchente e o meteoro contam um chamado só).
2. **Alvo = calma × fila curta × poder:**
   - **calma:** 1 com caos médio até 0,08 e 0 a partir de 0,25;
   - **fila curta:** 1 até 0,5 chamado na média e 0 a partir de 3;
   - **poder:** o maior entre os níveis de upgrade comprados (sem a meta), de 16 a 30, e o dinheiro guardado, de 0 a 4000.
   - Quem apanha (caos ou fila altos) ou ainda não cresceu fica em 0, com a curva de sempre.
3. **Subida suave:** o ritmo sobe no máximo 0,35 por minuto (de 0 a 1 em quase 3 min). Quando o jogador começa a apanhar, desce 1 por minuto.
4. **Efeito, com teto:**
   - intervalo × (1 − 0,45 × ritmo), nunca abaixo de 12 s;
   - casas por desastre × (1 + 0,6 × ritmo), nunca acima de 9.
   - Ficam de fora: o primeiro desastre, o terremoto forte (que sai das casas da rampa sem o ritmo, então o pico não cresce) e a hora em que cada tipo é liberado.

O `sim.py` faz as mesmas contas (`eficiencia_alvo`, `passo_eficiencia`, `intervalo_adaptado`, `casas_adaptadas`, `Jogo.tick_ritmo`) e lê os números do `BalanceData.cs`.

## Curva com o ritmo (intervalo / casas por desastre)

| Minuto | Ritmo 0 (como antes) | Ritmo 0,5 | Ritmo 1 (o teto) |
|---|---|---|---|
| 2 | 46,4 s / 2 | 35,9 s / 3 | 25,5 s / 4 |
| 4 | 39,1 s / 4 | 30,3 s / 5 | 21,5 s / 6 |
| 6 | 31,9 s / 4 | 24,7 s / 6 | 17,5 s / 7 |
| 9 | 21 s / 6 | 16,3 s / 8 | 12 s / 9 |
| 15 | 18,6 s / 7 | 14,4 s / 9 | 12 s / 9 |
| 24 | 15 s / 8 | 12 s / 9 | 12 s / 9 |

## Resultado no simulador (60 partidas por linha)

- "Antes" é a mesma conta com o efeito desligado (`--set paceIntervalCut=0 --set paceHouseBoost=0`), igual à v5.
- O **muito eficiente** é um perfil novo do `perfis.py`: reação de 0,6 s, cliques mais rápidos que os do bom e 95% de acerto no ponto certo.

| Jogador | Antes | Depois |
|---|---|---|
| bom (avançado) | 100%, 10:21 (10:09–10:32); pico do caos 0,43 | **100%, 10:31** (10:19–10:48); pico 0,56 |
| médio | 100%, 15:13 (14:45–15:41); pico 0,64 | **100%, 15:12** (14:44–15:53); pico 0,68 |
| ruim (inexperiente) | 75%, 19:36 (18:47–20:46); pico 0,90 | **76%, 19:10** (18:26–20:57); pico 0,84 |
| muito eficiente | 100%, 10:09; pico 0,36 | 100%, 10:20; pico 0,52 |
| bom, sem mini games | 100%, 11:06 | 98%, 11:18 |
| médio, sem mini games | 100%, 16:32 | 100%, 16:20 |
| bom, larga aos 6 min | perde aos 8:58 | perde aos 8:51 |
| ignora | perde aos 6:10 | perde aos 6:10 |

**O que o eficiente enfrenta a mais:**

| Jogador | Desastres na partida | Casas atingidas por minuto | Casas por minuto, min 4–8 | Ritmo médio, min 4–8 | Pico do caos, min 4–8 |
|---|---|---|---|---|---|
| muito eficiente | 17 → **20** | 3,0 → **5,0** | 2,8 → **6,2** | 0,73 | 0,20 → 0,34 |
| bom | 17 → **20** | 3,9 → **5,5** | 3,8 → **7,1** | 0,66 | 0,20 → 0,36 |
| médio | 28 → 30 | 7,0 → 7,7 | 5,5 → 6,1 | 0,25 | 0,33 → 0,36 |
| ruim | 40 → 39 | 8,7 → 8,6 | 5,9 → 5,5 | 0,00 | 0,52 → 0,49 |

- **O ruim não sente nada:** o ritmo dele fica em 0,00 em todas as fases, porque o caos e a fila dele passam dos limites.
- **O médio sente pouco:** 0,25 entre 4 e 8 min, que cai a 0,09 depois dos 8.
- **O eficiente:** entre 4 e 8 min recebe mais que o dobro de casas por minuto. A regra se segura sozinha: quando o caos dele chega perto de 0,25, o ritmo para de subir.
- **Os tempos:** dependem da economia (o repasse). Por isso quase não mudam, e ninguém precisou de compensação.

## Números novos (`Assets/SO/BalanceData.cs` e `Resources/Balance/BalanceData.asset`)

| Campo | Valor | O que faz |
|---|---|---|
| `paceAverageTime` | 45 | segundos da média do caos e da fila |
| `paceChaosLow` / `paceChaosHigh` | 0,08 / 0,25 | calma total / nenhuma |
| `paceQueueLow` / `paceQueueHigh` | 0,5 / 3 | fila curta / longa |
| `paceLevelsStart` / `paceLevelsFull` | 16 / 30 | níveis de upgrade (sem a meta) para o poder |
| `paceMoneyFull` | 4000 | dinheiro guardado que já vale poder total |
| `paceRisePerMinute` / `paceFallPerMinute` | 0,35 / 1 | subida suave / descida rápida |
| `paceIntervalCut` / `paceIntervalFloor` | 0,45 / 12 | intervalo até 45% mais curto, piso de 12 s (0 desliga) |
| `paceHouseBoost` / `paceHousesMax` | 0,6 / 9 | até 60% mais casas, teto de 9 (0 desliga) |

## Código e testes

- **`DisasterManager`:**
  - `TickPace` (médias e passo);
  - `Pace` (só leitura);
  - `GetInterval(balance, t, pace)` e `GetHouseCount(balance, t, pace)`. Com ritmo 0, a curva é a do BALANCEAMENTO_V5.
  - O terremoto forte usa o ritmo 0.
- **`Assets/Systems/Disasters/Editor/DisasterPaceTests.cs`:** 6 testes novos, que conferem:
  - o iniciante e quem apanha ficam em 0;
  - o eficiente chega a 1;
  - a subida é de 0,35 por minuto e a descida de 1;
  - a média não depende do fps;
  - os tetos e o piso;
  - a curva com o ritmo.
- **Conferências de 18/11:**
  - EditMode: **193/193** (eram 187);
  - validador da cena: **0 erros, 0 avisos**;
  - chão assado com o mesmo hash depois dos testes;
  - Play curto sem exceções:
    - o ritmo subiu de 0 a 0,350 em 60 s, com R$ 6000 e a cidade calma;
    - o intervalo seguinte caiu de 50 para 42 s (a contagem depois do terremoto foi de 37 s);
    - 0 erros no console.

## Para refazer e ajustar

```
python -X utf8 docs/jam/sim/tabela.py --sementes 60                                               # depois
python -X utf8 docs/jam/sim/tabela.py --sementes 60 --set paceIntervalCut=0 --set paceHouseBoost=0  # antes
```

- `paceChaosHigh` e `paceQueueHigh`: até onde o jogador aguenta antes de o ritmo parar de subir (o aperto do eficiente).
- `paceIntervalCut` e `paceHouseBoost`: a força do efeito. `paceRisePerMinute`: a rapidez com que ele chega.
- `paceLevelsStart` e `paceChaosHigh`: a proteção do iniciante. No primeiro rascunho (10 níveis e caos 0,30), o ruim já recebia ritmo aos 5 min e caía de 71% para 66% de vitórias. Com 16 e 0,25, ele ficou em 78%.

**Ressalvas:**
- O `GameClock` não acelera com o `debugTimeScale`: no modo rápido de teste, o ritmo sobe em tempo real.
- O simulador não reproduz o jogador de verdade; a calibragem final é com partida jogada.
