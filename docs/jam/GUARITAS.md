# Guaritas (v5A, pedido do Pietro, 17/11)

O nível 4 da **Automação** (despacho automático na cidade inteira) tirava a graça do jogo: a partir dele a cidade se
cuidava sozinha. Ele saiu. No lugar entrou a **Guarita**: uma estrutura comprada como o para-raio, **uma por
quarteirão**, e o jogador escolhe onde. Os chamados daquele quarteirão saem sozinhos; o resto da cidade continua no clique.

## Como joga

1. Aba **AUTOMAÇÃO** → **Guarita** (ícone 64 px da guarita com o guarda na janela).
2. A aba fecha e o topo diz "Guarita paga: clique num quarteirão para ela cuidar". Cada quarteirão com casa e sem guarita
   ganha borda verde tracejada e a placa "GUARITA / N casas".
3. Mouse num quarteirão: a área fica verde, a guarita aparece meio apagada no canto escolhido e a dica diz
   "Despacha sozinha os chamados de N casas".
4. Clique: 3 s de obra ("EM OBRA", barra e marteladas), depois a guarita aparece inteira com sombra, chiado de rádio,
   ondas e a dica única da sessão "GUARITA PRONTA! Os chamados das N casas daqui saem sozinhos, se houver gente livre."
5. Chamado numa casa do quarteirão: depois de **1,5 s** (tempo para o jogador ver o pino), a guarita manda a equipe
   sugerida da **base mais perto**: chiado de rádio, duas ondas verdes saindo da lâmpada, cinco pontos voando até o
   telhado da casa e "GUARITA!" em verde por cima dela. A lâmpada da guarita pisca verde; sem gente, material ou
   energia ela pisca em **âmbar** por 2,5 s e tenta de novo a cada 1 s.
6. **Esc** pausa tudo: a obra, a contagem do despacho, a lâmpada e a dica andam no `GameClock`.

## Regras e números

| o quê | valor | onde |
|---|---|---|
| preço | 90, 113, 141, 176, 220… (×1,25 a cada uma) | `Assets/Resources/Upgrades/Outpost.asset` |
| máximo | um por quarteirão com casa (36 no mapa); `maxLevel` 99 e o jogo corta no número de quarteirões | `UpgradesManager.SetLevelCap` |
| aba | Automação (`category: 1`, `order: 6`, logo depois da Automação) | `Outpost.asset` |
| olhar de novo | 1 s entre um despacho e o próximo da mesma guarita | `OutpostData.dispatchInterval` |
| reação | 1,5 s de espera do chamado antes de a guarita agir | `OutpostData.reactionDelay` |
| obra | 3 s | `OutpostData.buildTime` |
| canto | 0,9 u para dentro do quarteirão, no canto em que o desenho menos cobre casa e árvore | `OutpostData.cornerInset`, `OutpostMath.PickCorner` |
| dica | 7 s, uma vez por sessão | `OutpostData.tipTime` |

- **Quem é atendido:** as casas do quarteirão, pela mesma conta do para-raio (`LightningRodMath.BlockOf`, folga
  `houseMargin`). Só chamados de problema (dano e fogo); o Armazém continua manual. Quem espera equipe é decidido por
  `GuardManager.WaitsForTeam` (estado da casa, não `currentCall != null`): `Call` é `[Serializable]`, então toda casa
  intacta da cena tem um chamado vazio; sem esse filtro a guarita travava num NullReference (pego no Play da v5A).
- **Como despacha:** `GuardManager.TryAutoDispatch(house)` (o miolo do antigo `AutoDispatchNextCall`): equipe sugerida,
  base mais perto com energia (`EnergyManager.DeniesDispatch`), nega sem gente/material sem contar como erro do jogador.
  Por guarita, um despacho por intervalo, do mais urgente ao menos (`GuardManager.CompareUrgency`: fogo antes de dano).
- **Automação:** agora para no nível 3 (base, equipe e coleta automáticas). `AutomationLevels.AutoDispatch` saiu do código;
  `BalanceData.autoDispatchInterval` ficou sem uso (não foi apagado para não conflitar com a v5B).
- **Para-raio** também ficou sem limite fixo (um por quarteirão): `PARARAIO.md`.

## Simulador (30 sementes, `python -X utf8 docs/jam/sim/guarita.py --quantas 1,2,3,5`)

O robô compra as guaritas logo depois da última Automação e põe cada uma no quarteirão livre com mais casas.

| jogador e compra | vence | tempo da vitória | pico de caos | despachos sozinhos |
|---|---|---|---|---|
| bom, sem guarita | 100% | 7:57 | 0,30 | 0 |
| bom, 1 guarita | 100% | 8:06 | 0,30 | 0,2 |
| bom, 3 guaritas | 100% | 8:20 | 0,25 | 1,0 |
| médio, sem guarita | 100% | 8:12 | 0,45 | 0 |
| médio, 1 guarita | 100% | 8:26 | 0,45 | 1,1 |
| médio, 3 guaritas | 100% | 8:48 | 0,45 | 3,7 |
| ruim, sem guarita | 90% | 10:27 | 0,62 | 0 |
| ruim, 1 guarita | 93% | 10:32 | 0,62 | 1,2 |
| ruim, 3 guaritas | 80% | 10:46 | 0,65 | 3,9 |
| ruim, 5 guaritas | 53% | 11:29 | 0,90 | 7,0 |

Leitura: o robô reage em 1 a 2,5 s, então a guarita só ganha dele nos chamados que ele não viu a tempo; o valor dela
é tirar cliques de quem não dá conta (o ruim sobe de 90% para 93% com uma). Cada guarita cobre no máximo 6 das 153 casas,
então automatizar a cidade inteira fica caro de propósito (5 guaritas = 740, perto dos 640 do antigo nível 4, cobrindo
~20% das casas). Comprar muitas tarde atrasa a meta e o caos sobe (o ruim com 5 cai para 53%): é escolha, não regra.
Testados também 60/120 e ×1,35 (`--custos 60,90,120 --crescimentos 1.25,1.35`): 60 quase não custa nada ao médio
(+3 s) e deixa a cidade toda automatizável barato; 120 e ×1,35 derrubam o ruim para 86% já com 2.

## Arquivos

- Código: `Assets/Systems/Outpost/` (`OutpostManager`, `OutpostBlock`, `OutpostFeel`, `OutpostMath`, `OutpostEvents`,
  `OutpostPointer`, `OutpostBootstrap`), `Assets/SO/OutpostData.cs`, `GuardManager.TryAutoDispatch`,
  `UpgradesManager.GetMaxLevel/SetLevelCap`.
- Sistema sem cena: o `OutpostBootstrap` cria o objeto `[Guaritas]` quando existe um `GameManager` (a `Gameplay.unity`
  não mudou).
- Arte: `Assets/Systems/Outpost/Resources/Outpost/Outpost_Icon.png` (64x64) e `Outpost_Booth.png` (38x48, PPU 32, Point),
  feitos no Pixel Lab com a paleta do kit forçada (`analises-lilith/jogo-defesa-civil/a5a/gen5a.py`, 12 gerações ≈
  US$ 0,10) e tratados por `fin5a.py` (alfa binário, contorno #141218). Som: `Outpost_Radio.wav` (`radio5a.py`).
- Dados: `Tools → Jam → Guarita → Criar dados` recria o `Outpost.asset` e o `OutpostData.asset`.
- Testes: `Editor/OutpostTests.cs` (canto, reação, assets, Automação até 3, arte e som); `LightningRodTests` e
  `CallEstimateTests` atualizados.
