# Consertos v7A: para-raio livre, anel fino e dificuldade que nunca para (19/11)

## Pedido do Pietro

1. Para-raio sem limite fixo de compras: o máximo é o número de quarteirões.
2. O anel que destaca as casas ainda é grosso e polui a tela: afinar bastante, sem perder a leitura.
3. O jogo está fácil: apertar um pouco a dificuldade e, ao chegar a um teto já difícil, seguir crescendo bem mais devagar, sem parar. Vitória: médio 15 min, avançado 10, inexperiente 20 com muito sufoco.

## 1. Para-raio: já era um por quarteirão (v5A), conferido no Play

`Rod.asset` tem `maxLevel` 99 e o `LightningRodManager` corta no número de quarteirões (`UpgradesManager.SetLevelCap`). No Play: 36 quarteirões, 36 compras pelo clique no quarteirão e a 37ª negada (MaxLevel). Quem segura na prática é o custo, 80 × 1,2^n: 80, 96, 115… 413 no 10º, 2.556 no 20º e 15.825 no 30º. As guaritas seguem igual (teto 36). Código sem mudança.

## 2. Anel fino

Eram dois anéis:

| Anel | Antes | Agora |
|---|---|---|
| Onda laranja do pino (`MapMarkers`, `Pin .../Ring`) | sprite `Mk_Ring` com traço de 3 pixels, abrindo até 2× com opacidade 0,9 | aro de 1 pixel do kit (`PixelFx.ThinRing`, mesmo diâmetro), abre até 1,45× (`waveMaxScale`) com 0,85 (`waveAlpha`) |
| Elipse no pé da casa (`HouseStatusFx`) | traço 2, que virava 3 no zoom afastado (o zoom normal do jogo), ondas até 1,55× com 0,9, halo 0,3/0,55 (× 1,4 longe) | traço 2 em qualquer zoom (1 abre buracos na elipse), ondas até 1,3× com 0,5 (`pingAlpha`), halo 0,18/0,35 (× 1,2 longe) |

O `waveScale` da cena (2) deixou de ser lido: o campo novo `waveMaxScale` vale pelo código, sem mexer na `Gameplay.unity`. Prints (Play, terremoto de 6 casas perto da Guarda): `analises-lilith/jogo-defesa-civil/v7a/anel_comparar.png` (antes × depois, recorte do 1280×720) e `anel_depois_1080.png`.

## 3. Dificuldade: teto macio

Regra pura em `DisasterPace` (`LateInterval`, `LateHouses`, `SoftFloor`, `SoftCeiling`), as mesmas contas do `docs/jam/sim/sim.py`:

- **Curva base:** depois do teto (9:00) o intervalo cai 0,4 s por minuto até 15 s (agora aos 21:30) e dali encolhe 2% por minuto (`disasterIntervalSoftPerMinute`), sem nunca chegar a 0. As casas sobem 0,15 por minuto até 9 (aos 29:00) e dali 0,05 por minuto (`disasterHousesSoftPerMinute`, uma casa a cada 20 min).
- **Ritmo adaptativo:** o que o ritmo empurraria além do piso (11 s) ou do teto (10 casas) entra com 30% (`paceSoftShare`), em vez de parar no limite.
- **Mais aperto:** `disasterIntervalEnd` 21 → 20; ritmo `paceChaosLow` 0,08 → 0,12, `paceChaosHigh` 0,25 → 0,35, `paceRisePerMinute` 0,35 → 0,45, `paceIntervalCut` 0,45 → 0,55, `paceIntervalFloor` 12 → 11, `paceHouseBoost` 0,6 → 0,8, `paceHousesMax` 9 → 10. Economia (separa os perfis): `moneyPerDamage` 52 → 49, `incomeMoneyShare` 0,15 → 0,21.

Curva no Normal (as dificuldades da v7B-1 multiplicam por cima):

| Minuto | Base | Ritmo 0,5 | Ritmo 1 |
|---|---|---|---|
| 9 | 20,0 s · 6 casas | 14,5 s · 8,4 | 10,4 s · 10,2 |
| 21:30 | 15,0 s · 7,9 | 11,0 s · 10,3 | 9,7 s · 11,3 |
| 35 | 11,4 s · 9,3 | 10,2 s · 10,9 | 9,2 s · 12,0 |
| 60 | 6,9 s · 10,6 | 6,3 s · 11,8 | 5,8 s · 13,1 |

Antes, a partir de uns 22 min tudo parava em 15 s e 9 casas (ritmo 1: 12 s e 9).

Simulador, 60 partidas por linha, valores dos `.asset` (`tabela.py --sementes 60 --assets Assets/Resources`):

| Jogador | Vence | Tempo até vencer (p10–p90) | Pico do caos |
|---|---|---|---|
| bom (avançado) | 100% (era 100%) | 10:27 (9:36–11:01), era 10:36 | 0,60 (era 0,56) |
| médio | 93% (era 100%) | 15:37 (14:53–16:55), era 15:47 | 0,72 (era 0,72) |
| ruim (inexperiente) | 61%, perde 38% (era 73%/26%) | 18:57 (17:34–20:46), era 18:56 | 0,93 (era 0,86) |
| muito eficiente | 100% | 9:56, era 10:24 | 0,53 |

Sem mini games, bom e médio agora perdem 5%. O fogo que pula da v7B-3 não está no simulador: quem erra o jato sofre um pouco mais do que a tabela mostra.

## Conferências

- EditMode: 236 de 236 na cópia `Defesa-Civil-Game-v3a` com a jam junta (3 testes novos no `DisasterPaceTests`; o `DebutTests` fixa as partes macias em 0 para conferir o piso e o teto duros da v5).
- Play: 0 erros; enchente e meteoro forçados viram incidente; guaritas e para-raios com teto 36.
- Scripts: `analises-lilith/jogo-defesa-civil/v7a/` (`exp.py` e `rodar.sh` da calibração, `serie.py`, `anel.cs`, `play.cs`, `cap.ps1`).
