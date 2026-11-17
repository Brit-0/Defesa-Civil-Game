# Para-raio por quarteirão (jam-v4c, #537, 17/11)

Pedido do Pietro: um para-raio que o jogador constrói num quarteirão escolhido. Ele absorve os raios das tempestades que
cairiam ali, para sempre. É comprado como upgrade, pelo mesmo fluxo e visual dos terrenos da expansão (EXPANSAO.md).

## O que o jogador vê

- **Compra:** na aba **EXPANSÃO** do painel de melhorias aparece **Para-raio**, com o ícone novo de 64 px (escudo com
  raio). O preço fica na linha da aba, como nos terrenos comprados por ela. Ao pagar, a aba fecha e a dica do topo diz
  "Para-raio pago: clique num quarteirão para proteger".
- **Escolha do quarteirão:**
  - Todo quarteirão livre com casas ganha uma borda tracejada dourada e uma placa no meio: ícone, "PARA-RAIO" e "N casas",
    com a seta dourada. As placas e as setas são as da expansão (`Lot_Plate`, `Lot_Arrow`). Fora do modo de escolha nada
    disso aparece: são 36 quarteirões.
  - **Prévia:** com o mouse no quarteirão ou na placa, a área fica azul (azul é sempre área protegida) e a borda pulsa.
    O para-raio aparece meio apagado no telhado onde vai ficar. A dica mostra "Protege N casas / atrai X% dos raios": X é
    a parte dos raios que cairia ali, contada na hora pelas casas intactas e pelos para-raios que já existem.
  - Os quarteirões já protegidos aparecem em azul durante a escolha.
  - O clique vale no quarteirão inteiro ou na placa. Enquanto o modo está aberto, o clique nas casas vai para o
    quarteirão, porque o para-raio já está pago e a escolha leva um clique.
- **Obra:** 3 s com marteladas e a placa "EM OBRA" com a barra, logo acima do mastro, que sobe aos poucos no telhado.
  Pronto, o mastro dá um pulo e toca o som de conserto mais um estalo elétrico, com faísca e a área piscando em azul.
- **Onde fica o mastro:** no telhado da casa mais alta do quarteirão. O pé fica a 30% da altura da casa abaixo do topo
  (até 0,7 u), e o mastro é filho da casa, por isso entra na ordem por Y dela e treme e apaga junto. A arte tem 16x46 px
  a PPU 32, como as casas: ponta e bola de cobre, isolador azul, mastro de aço e base.
- **Para-raio pronto:** a ponta brilha de 4 em 4 s.
- **Raio absorvido** (durante a tempestade):
  - Cai o mesmo raio do céu, com clarão, trovão e tremida (`GameEvents.LightningStruck`), mas na ponta do para-raio.
  - Por cima entram:
    - o arco em ziguezague, da casa que seria atingida até a ponta;
    - o mastro branco por um instante e o halo dourado na ponta;
    - as faíscas;
    - a descarga descendo até o chão, com o anel tracejado no pé;
    - a área acendendo em azul;
    - o "PROTEGIDO!" dourado subindo;
    - o estalo elétrico (`Rod_Zap.wav`).
  - A casa não sofre nada: nem dano, nem fogo, nem destelhada, nem chamado.

## Regras e números

| o quê | valor | onde |
|---|---|---|
| preço | 80, 120, 180, 270, 405… (×1,5 a cada um) | `Assets/Resources/Upgrades/Rod.asset` |
| máximo | v5A: sem limite fixo; um por quarteirão com casa (36 no mapa). O `maxLevel` do asset é 99 e o jogo corta no número de quarteirões (`UpgradesManager.SetLevelCap`, chamado no `LightningRodManager.Start`) | `Rod.asset` (`maxLevel`) |
| obra | 3 s | `LightningRodData.buildTime` |
| atração | 6 | `LightningRodData.attraction` **[PROPOSTA]** |
| quarteirões | 36 (os polígonos `Block` do `city_layout.json`, com as 153 casas) | `LightningRodData.blocks` |
| folga da casa | 0,4 u (o pé da casa pode sair até 0,4 u do retângulo do quarteirão) | `LightningRodData.houseMargin` |

- **Quem é protegido:** cada casa pertence ao quarteirão onde está o pé dela. O pé é `(x, YSortCanvas.BaseY)`, o mesmo
  `bottomCenter` do mapa. O teste confere as 153 casas contra o `polygonId` do layout.
- **Sorteio do raio** (`DisasterManager.Storm`): sem para-raio pronto, o sorteio é o de sempre (`Random.Range`), igual
  ao de antes. Com algum pronto, cada casa intacta pesa 1 e as protegidas pesam `attraction` (`LightningRodMath.PickIndex`).
  Se o raio sorteia casa protegida, `LightningRodManager.TryAbsorb` leva o raio para a ponta e o laço segue sem dano. O
  raio absorvido não entra na ocorrência.
- **Por que a atração [PROPOSTA]:** com atração 1, o para-raio só pega os raios que já cairiam no quarteirão. São cerca
  de 4% deles, meio raio por partida, e o jogador quase nunca veria o para-raio funcionar. Com 6, ele "puxa" os raios
  da vizinhança, como um para-raio de verdade: cerca de 20% dos raios no quarteirão de 6 casas. Para voltar ao
  comportamento puro, ponha `attraction = 1` no `LightningRodData.asset`; nada mais muda.
- **Energia (área da v4B, sem mexer):** o `EnergyManager` derruba a energia no primeiro raio de cada tempestade,
  ouvindo `LightningStruck`. O raio absorvido também dispara o evento, por isso o apagão acontece mesmo assim, na ponta
  do para-raio. O simulador faz igual. Se o MEGABRAIN quiser que o para-raio segure também o apagão, a mudança é no
  `EnergyManager`: ignorar o `LightningStruck` que vier logo antes de um `LightningRodEvents.StrikeAbsorbed`.

### Simulador (30 sementes, `python -X utf8 docs/jam/sim/pararaio.py`)

O robô compra os para-raios logo depois da base nova (`perfis.com_pararaio`) e põe cada um no quarteirão livre com mais
casas, que é o que a dica mostra. Sem para-raio, os números são os mesmos do RITMO_V3: bom 7:57, médio 8:12 (pico 0,45)
e ruim 90% em 10:27.

| jogador e compra | vence | tempo da vitória | pico de caos | raios por partida | absorvidos |
|---|---|---|---|---|---|
| bom, sem para-raio | 100% | 7:57 | 0,30 | 11,3 | - |
| bom, 1 para-raio | 100% | 8:06 | 0,25 | 11,3 | 1,3 (12%) |
| bom, 2 para-raios | 100% | 8:21 | 0,25 | 13,3 | 3,0 (23%) |
| médio, sem para-raio | 100% | 8:12 | 0,45 | 11,4 | - |
| médio, 1 para-raio | 100% | 8:22 | 0,43 | 11,7 | 1,2 (10%) |
| médio, 2 para-raios | 100% | 8:37 | 0,45 | 12,8 | 3,3 (26%) |
| ruim, sem para-raio | 90% | 10:27 | 0,64 | 20,4 | - |
| ruim, 1 para-raio | 96% | 10:30 | 0,60 | 21,0 | 3,0 (14%) |
| ruim, 2 para-raios | 100% | 10:40 | 0,59 | 21,0 | 4,2 (20%) |

- **Leitura:** é um seguro e não um atalho. Quem joga bem gasta 80 e vence uns 10 s depois, com menos caos. Quem joga
  mal ganha mais partidas: 90% sem para-raio, 96% com 1 e 100% com 2.
- **Atração 1** (médio, 1 para-raio): absorve 0,6 raio por partida (4%), contra 1,2 (11%) com a atração 6.
- O primeiro para-raio fica pronto perto dos 4:15. A primeira tempestade é aos 3:15, então ele pega da segunda em diante.

## Arquivos (todos novos, menos o `DisasterManager.cs` e o simulador)

- `Assets/SO/LightningRodData.cs`: os dados (quarteirões, folga, atração, obra, tracejado, transparência da prévia).
- `Assets/Systems/LightningRod/`:
  - `LightningRodManager.cs`: monta os quarteirões, a compra, o modo de escolha, a obra e o sorteio e a absorção do raio;
  - `RodBlock.cs`: um quarteirão, com a área, a borda, a placa, a dica, o mastro e a prévia (o clique e o hover chegam pelo `RodPointer.cs`);
  - `LightningRodMath.cs`: as contas puras testadas (quarteirão do ponto, sorteio com peso, tracejado da borda);
  - `LightningRodEvents.cs`, `LightningRodFeel.cs` (só escuta: som, faísca, arco, texto) e `LightningRodBootstrap.cs`.
- `Assets/Systems/LightningRod/Resources/LightningRod/`: `LightningRodData.asset`, `Rod_Pole.png`, `Rod_Icon.png` e
  `Rod_Zap.wav`. O ícone veio do Pixel Lab (8 candidatos, 8 gerações da assinatura, US$ 0). O mastro e o estalo são
  feitos por `Tools/LightningRod/rod_kit.py`, com GUID fixo.
- `Assets/Resources/Upgrades/Rod.asset`: o upgrade `rod`.
- `Assets/Systems/LightningRod/Editor/`:
  - `LightningRodDataMenu.cs`: **Tools → Jam → Para-raio → Criar dados**, que regera os quarteirões depois de um mapa novo;
  - `LightningRodTests.cs`: 7 testes.
- `Assets/Systems/Disasters/DisasterManager.cs`: 2 linhas no laço da tempestade (`PickStrikeTarget` e `TryAbsorb`).
- `docs/jam/sim/`: `sim.py` (quarteirão da casa, `alvo_do_raio`, para-raio na compra do `rod`), `dados.py`, `perfis.py`
  e `pararaio.py` (a tabela acima).

## Como ligar na cena

Nada no `Gameplay.unity`. O `LightningRodBootstrap` cria o objeto `[Para-raio]`, com o `LightningRodManager` e o
`LightningRodFeel`, em toda cena que tem `GameManager`. O para-raio procura na cena:

- o `LocationsManager` com as casas;
- o `UpgradesManager`;
- o `UICanvas` do HUD, onde as placas ficam na camada `LightningRodSigns`, atrás do resto;
- o `UpgradeTab`, que fecha ao comprar.

Sem o `Rod.asset` ou sem quarteirões no `LightningRodData`, ele desliga com um aviso no console.

## Conferências feitas

- **Unity 6000.6.7f1** (cópia v3c): compila sem erro.
- **Testes EditMode:** 129 de 129 passam, 7 deles do para-raio. O `city_ground.png` ficou igual (md5 antes e depois).
- **Validador da cena** (`CityGenMenu.ValidateAll`): 0 erros e 0 avisos.
- **Play** (prints 1280x720 em `analises-lilith/jogo-defesa-civil/resultados/`):
  - `82_escolha.png`: placas, borda, prévia azul, mastro fantasma e dica "Protege 6 casas / atrai 20% dos raios";
  - `82_em_obra.png` e `82_obra.png`: o mastro no telhado da casa 080;
  - `82_raio.png`: uma tempestade de verdade do `DisasterManager` (6 raios), com 1 raio absorvido pelo para-raio, sem
    forçar.
- **As 153 casas:** caem cada uma no quarteirão do seu `polygonId`, conferido no teste e no Play.
- **Scripts do Play:** `analises-lilith/jogo-defesa-civil/scripts/a537/` (`p1_preparar.cs` a `p4_raio.cs`, mais o
  `cap.sh`). Eles pulam os tutoriais destruindo os componentes no Play, sem mexer nos PlayerPrefs.
