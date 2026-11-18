# Enchente e meteoro (jam-v5b, #521, 17/11)

Pedido do Pietro: dois desastres novos que pegam **um quarteirão inteiro** e se resolvem com **um chamado só**. A
enchente traz o minigame de desentupir o bueiro; o meteoro traz a reconstrução longa, acelerada pelo ponto certo.
Os dois entram no sorteio do `DisasterManager` como os outros (`DisasterType.Flood` e `DisasterType.Meteor`, no fim do
enum, para não mudar os números já salvos).

## O que o jogador vê

### Enchente (entra perto de 4 min)

- Faixa "ENCHENTE CHEGANDO!" com o ícone de 64 px do Pixel Lab e o aviso normal.
- **Água:** o quarteirão alaga devagar (6 s). A água em ladrilho pixel sai do meio do quarteirão, se espalha até um
  pouco das ruas e anda em passos de 1 texel, no `GameClock`, por isso para no Esc. A borda é de espuma.
- **Casas:** cada casa ganha uma faixa de água com espuma no pé do desenho, que sobe junto com a água.
- **Fogo:** casa em chamas do quarteirão apaga na hora, com vapor e o som de apagar. A casa entra na enchente.
- **Chamado:** um só, no **bueiro**, que é a casa livre mais perto do meio do quarteirão.
  - O balão do chamado mostra o ícone de enchente de 64 px.
  - O pino do mapa tem ícone próprio: casa na água (`Assets/Arts/UI/Chamados/call_flood_pin.png`, Pixel Lab, #524),
    no disco azul da tempestade. O aro em volta da casa continua o da tempestade.
  - Clicar em qualquer casa alagada vale como clique no bueiro.
- **Equipe:** 2 trabalhadores; com menos, o envio é negado.
- **DESENTUPIR O BUEIRO** (quando a equipe chega):
  - A válvula vermelha aparece no meio do quarteirão, no mapa: anda com a câmera e sai da tela junto com ele.
  - Em cima, a placa "SEGURE E GIRE!"; atrás, uma seta girando como dica. Embaixo, a barra de pressão.
  - Segurar o clique perto da válvula e girar o mouse em círculos (em qualquer sentido) sobe a pressão. O volante gira
    junto, range a cada 1/4 de volta, e a água desce com o som de escoar mais forte.
  - O primeiro giro escolhe o sentido: sacudir o mouse para a frente e para trás não enche.
  - **Falha leve:** soltar o clique com pressão. A pressão zera, a água volta um pouco, a válvula pisca vermelha e
    treme, e aparece "SOLTOU! A ÁGUA VOLTOU".
  - Girando sem parar, desentope em uns 4 s (6 s com uma falha). Sem girar, a água escoa sozinha em 30 s.
- **Coleta:** com o check verde no bueiro, um clique devolve o quarteirão inteiro: as casas voltam intactas, a água
  some com respingos e o dinheiro sai (12 por casa).

### Meteoro (entra perto de 8 min, raro)

- **Aviso (7 s)**, mais longo que o dos outros:
  - faixa "METEORO CHEGANDO!";
  - um **alvo vermelho** pulsando no quarteirão inteiro desde o primeiro instante;
  - a **sombra** escura no meio dele, crescendo até cobrir o alvo e piscando vermelho cada vez mais rápido;
  - o **trem de tremor**: 5 trancos de câmera do fraco ao forte, com poeira e ronco.
- **Queda:** no último 1,1 s a rocha em brasa desce girando, com rastro de brasas e fumaça, e assobia.
- **Impacto:**
  - clarão branco na tela e estouro de destroços, faíscas, brasas, fumaça e poeira;
  - a tremida mais forte do jogo e o som de impacto;
  - as casas somem e fica a **cratera** do tamanho do quarteirão;
  - o para-raio do quarteirão, se houver, some junto e volta com a obra (não protege do meteoro).
- **Chamado de reconstrução:** um só, no canteiro (a casa livre mais perto do meio). Pede **8 trabalhadores** e
  **6 de material**; com 7, o envio é negado. A obra leva **90 s** com 8 (gente a mais divide o tempo).
- **Ponto certo da obra:**
  - É o mesmo do conserto: o arco dourado no círculo de progresso. Mas aqui o acerto não acaba a obra: soma 2% e
    **acelera**, e a próxima janela já aparece mais à frente.
  - No meio da cratera fica a barra **"OBRA x1,0"**, com 4 setas que acendem com a aceleração (até x3).
  - O acerto dá "NO PONTO! OBRA x2,4", martelada, a barra pisca e pula. O erro tira metade do embalo, volta 2% e a
    barra treme.
  - O embalo cai sozinho devagar, então acertos seguidos rendem mais.
  - Na conferência (9 acertos seguidos), a obra levou 35 a 38 s em vez de 90.
- **Durante a obra:** fumaça subindo da cratera, soldas cada vez mais rápidas com a aceleração, e a cratera clareia com
  o progresso.
- **Coleta:** as casas voltam quicando, com confete, e o para-raio volta. Paga 45 por casa.

### Dicas da primeira vez

Uma dica curta por assunto, no balão do tutorial ("DICA", botão ENTENDI, some sozinha em 9 s):

- enchente: aponta o bueiro;
- válvula: aponta a válvula;
- meteoro: diz quantos trabalhadores a obra pede;
- reconstrução: aponta o canteiro.

Cada dica sai uma vez e fica guardada em PlayerPrefs (`DefesaCivil.FloodTipDone`, `ValveTipDone`, `MeteorTipDone`,
`RebuildTipDone`). Não aparecem durante o tutorial inicial nem no de energia.

## Regras e números

Tudo em `BlockDisasterData` (`Assets/SO/BlockDisasterData.cs`; o asset fica em
`Assets/Systems/BlockDisasters/Resources/BlockDisasters/BlockDisasterData.asset`). Sem números no código.

| | Enchente | Meteoro |
|---|---|---|
| Libera aos | 225 s (`floodUnlockTime`) | 470 s (`meteorUnlockTime`) |
| Peso no sorteio | 25 (`floodWeight`) | 6 (`meteorWeight`), com 180 s entre um e outro (`meteorCooldown`) |
| Quarteirão | ≥ 3 casas livres, sem Base nem Armazém | ≥ 3 casas livres, sem Base nem Armazém |
| Quais casas entram | intactas e em chamas, sem equipe | todas sem equipe |
| Caos por casa | 0,6 | 0,75 (uma casa danificada pesa 1) |
| Equipe fixa | 2 | 8, mais 6 de material |
| Tempo | 30 s sozinha; uns 4 s girando | 90 s; menos com o ponto certo |
| Recompensa | 12 por casa | 45 por casa |

- **Estreia:** como os outros tipos, o primeiro sorteio depois de liberar é desse tipo (`TryGetDebut`), mas só se
  houver quarteirão livre (`BlockDisasterManager.Allows`). Só uma enchente e um meteoro por vez.
- **Válvula** (`BlockDisasterRules.ValveStep`):
  - cada volta sobe 0,55 de pressão; a pressão vaza 0,3 por segundo;
  - a água escoa 0,28 por segundo × pressão, além do escoamento sozinho;
  - soltar com pressão ≥ 0,2 é a falha: tira 8% do progresso;
  - o alcance é de 300 px do HUD em volta da válvula.
- **Obra** (`RebuildHit/Miss/Decay`, `NextWindow`):
  - a obra anda (1 + embalo) vezes mais rápido;
  - cada acerto soma 0,5 de embalo (até 2) e 2% de progresso;
  - o erro corta o embalo pela metade e volta 2%;
  - o embalo cai 0,04 por segundo;
  - a janela tem 6% da obra e a próxima abre de 4% a 9% à frente;
  - janela que passa sem clique só dá lugar à próxima, sem castigo.
- **Pausa:** água, sombra, tremores, válvula e obra andam no `GameClock` e param no Esc.

## Simulador

`docs/jam/sim/quarteirao.py` repete as regras acima: sorteio, estreia, quarteirão, caos, equipe fixa, válvula e
obra janela a janela. O robô fica preso na válvula enquanto gira e clica nas janelas da obra quando está livre. Os
ganchos estão no `sim.py`, no `perfis.py` e no `dados.py` (bloco da Base).

```
python -X utf8 docs/jam/sim/quarteirao.py --sementes 30
```

10 sementes, com e sem os dois desastres (peso 0):

| jogador | com os dois | vence | tempo da vitória | pico de caos | enchentes | meteoros | estreia enchente | estreia meteoro |
|---|---|---|---|---|---|---|---|---|
| bom | não | 100% | 7:57 | 0,30 | 0 | 0 | - | - |
| bom | sim | 100% | 7:59 | 0,25 | 1,2 | 0,2 | 4,1 min | 8,2 min (2 de 10) |
| médio | não | 100% | 8:10 | 0,45 | 0 | 0 | - | - |
| médio | sim | 100% | 8:02 | 0,43 | 1,3 | 0,2 | 4,1 min | 8,4 min (2 de 10) |
| ruim | não | 90% | 10:36 | 0,64 | 0 | 0 | - | - |
| ruim | sim | 80% | 10:03 | 0,62 | 2,2 | 1,0 | 4,1 min | 8,3 min (10 de 10) |

Com 30 sementes, o jogador ruim vence 93% com e sem os dois desastres (10:27 contra 9:55). Nas partidas dele, a obra
do meteoro leva 80 s.

O que a tabela diz:

- a enchente cai uma ou duas vezes por partida e ajuda: paga bem e quase não pesa no caos;
- o meteoro quase não aparece para quem vence antes dos 8 min; para quem joga mais tempo vem uma vez.

Para deixar o meteoro mais frequente, suba `meteorWeight` ou desça `meteorUnlockTime` no asset.

## Código

- **Dados e regras:**
  - `Assets/SO/BlockDisasterData.cs`;
  - `Assets/Systems/BlockDisasters/BlockDisasterRules.cs`: contas puras, testadas em `Editor/BlockDisasterTests.cs`;
  - `BlockIncident.cs`: o `IWorkRule` do chamado único.
- **Sistema:** `BlockDisasterManager.cs`:
  - quarteirões, sorteio, entrada das casas e resolução;
  - eventos em `GameEvents.BlockDisasters.cs`.
- **Apresentação (só escuta eventos):**
  - `FloodFx.cs` (água);
  - `ValveMinigame.cs` (válvula);
  - `MeteorFx.cs` (aviso, queda, cratera, obra);
  - `RebuildBar.cs` (barra da obra);
  - `BlockDisasterAudio.cs` (sons em `Resources/BlockDisasters`, gerados por `Tools/BlockDisasters/sons_quarteirao.py`,
    tocados pelo SoundBank da v5D com a posição do quarteirão: ids `flood_*`, `valve_*`, `meteor_*`, ver `SFX_V5.md`);
  - `BlockDisasterTips.cs` (dicas);
  - `BlockDisasterArt.cs` (pixel art feita por código).
- **Sem cena:** o `BlockDisasterBootstrap` cria o objeto `[Enchente e meteoro]` ao rodar. A `Gameplay.unity` não muda.
- **Integração:**
  - `DisasterManager` (sorteio, aviso, `Run`);
  - `House` (`Flooded`, `Destroyed`, líder e membros, regra de trabalho);
  - `GuardManager` (`Call.rule`);
  - `Stats` (caos, tempo, recompensa);
  - `MapMarkers`, `HouseStatusFx`, `MiniGameFeel`, `DisasterBanner`.
- **Dados novos:** **Tools → Jam → Enchente e meteoro → Criar dados** cria:
  - o `BlockDisasterData` com os 36 quarteirões do mapa;
  - `Resources/Disasters/Flood.asset` e `Meteor.asset`.
  Não apaga ajustes.
