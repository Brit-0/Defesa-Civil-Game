# Consertos v7B-3: dica do jato e fogo que pula

Ajudante 5 (#543), branch `jam`, 19/11/2026. Pedido do Pietro: dois ajustes do fogo, só eles. Contexto do jato em `ENERGIA_FOGO.md`.

## 1. A dica do jato espera o jogador

**Antes:** a DICA do jato aparecia quando a equipe chegava no primeiro incêndio e sumia sozinha em 6 s, às vezes antes de o jogador entender o minigame.

**Agora:** a dica fica na tela, com o botão **ENTENDI**, até uma destas duas coisas:
- o jogador apagar todas as chamas a tempo ("APAGOU!");
- o jogador clicar em ENTENDI.

Apagar uma chama só, o fogo crescer ou o fogo pular não tiram a dica. A dica do terremoto forte continua sumindo sozinha.

**Código:** `Tutorial.cs`.
- `ShowTip(..., untilDone)`: sem tempo de sumir e com o botão `tipOkLabel`.
- Ouve `GameEvents.SkillCheck`. A regra fica em `Tutorial.FireTipEnds`: só o `FireOut` encerra.
- Se o tutorial de energia tomar o balão no meio, o APAGOU não esconde o balão dele.

## 2. Fogo que pula

Quando o jato falha (sobrou chama ao fim do tempo), além de "O FOGO CRESCEU!", o fogo tem **35% de chance** de pular para a casa intacta mais perto, a até 5 unidades. Ele pula 0,8 s depois, para os dois avisos não se cobrirem. A casa nova pega fogo no nível 1 e mostra "O FOGO PULOU PARA A VIZINHA!" com som.

**Números no `BalanceData`:**

| Campo | Valor | Para quê |
|---|---|---|
| `fireSpreadChance` | 0,35 | chance de pular a cada jato que falha |
| `fireSpreadRadius` | 5 | alcance do pulo |
| `fireSpreadDelay` | 0,8 s | espera entre o "CRESCEU" e o pulo |

O alcance foi medido no mapa de 153 casas: a vizinha mais perto de cada casa fica entre 2,2 e 4,2, e cada casa tem cerca de 3 vizinhas a até 5.

**Por que 35% é moderado:** o jogador médio acerta 2 de 3 jatos (`ENERGIA_FOGO.md`). Então cerca de 1 em cada 9 incêndios atendidos pula (1/3 × 0,35 ≈ 12%).

**Código:**
- regra pura `FireSpread` (`Rolls`, `PickNearest`) em `Assets/Systems/Fire/FireSpread.cs`;
- `House.TickJet` sorteia e `House.SpreadFireLater` acende a vizinha, só com a partida rodando;
- `SkillResult.FireSpread` entrou no fim do enum (B18);
- o `MiniGameFeel` mostra o aviso.

## 3. Conferências

- **Testes EditMode:** 7 testes novos em `Assets/Systems/Fire/Editor/FireSpreadTests.cs`:
  - sorteio, frequência em 20 mil sorteios, casa mais perto, nenhuma no alcance, empate;
  - os dados do `BalanceData`;
  - a regra de fim da dica.

  Os 7 passaram. No total do projeto passaram 227 de 233. As 6 falhas são todas `DifficultyTests`, da v7B-1, que ainda estava sendo feita na mesma pasta.
- **Compilação:** 0 erros novos fora do Unity (Roslyn) e no Editor.
- **Play com robô** (`analises-lilith/jogo-defesa-civil/v7b3/robo.cs` + `rodar.sh`), com 0 erros no console:

| Momento | Dica na tela? |
|---|---|
| A equipe chega e o jato abre (print `resultados/v7b3_dica.png`) | sim, com ENTENDI |
| O jato falha e o fogo pula para a vizinha a 3,38, 0,81 s depois (print `resultados/v7b3_pulou.png`) | sim |
| APAGOU no jato da vizinha | não |
| Outro incêndio com a dica de novo, depois de clicar ENTENDI | não (o jato segue aberto) |

  O robô devolveu os PlayerPrefs e a chance (0,35) no fim.

## 4. Falta

- O simulador (`docs/jam/sim/fogo_energia.py`) ainda não modela o pulo.
