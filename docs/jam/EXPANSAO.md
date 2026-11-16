# Expansão: setas, bases e armazéns novos (jam-sistemas, #523, 16/11)

Pedido do Pietro: (1) setas na borda da tela apontando as casas que precisam de atendimento; (2) terrenos vazios onde
o jogador constrói bases novas da Guarda como upgrade comprado; (3) armazéns extras pelo mesmo mecanismo.

## O que o jogador vê

- **Setas na borda (item 1):** já existem no `MapMarkers` (jam): casa com problema fora da câmera vira um pino preso na
  borda, com seta, cor e ícone do tipo (fogo, dano, tempestade), e o clique leva a câmera até lá. Não dupliquei. As
  bases e armazéns novos entram nele com o gancho da seção "Como ligar" (passo 3).
- **Terrenos (itens 2 e 3):** quatro terrenos de terra batida, dois de cada lado da cidade, na fileira de baixo, de frente
  para a rua `y = -22,25` (mapa v6): **OESTE** (praça do polígono 009) e **LESTE** (mata do polígono 014). Em cada lado,
  um terreno para base (o de dentro) e um para armazém (o de fora, colado). As árvores e postes dentro do terreno somem.
  - Base OESTE fica a 25 u da Guarda, base LESTE a 32 u, e as duas a 46 u uma da outra.
  - Caminhada média de uma casa até a base (pela rua, simulador): 30,2 u só com a Guarda; 26,9 u com a OESTE; 25,0 u com as duas.
- **Placa do terreno (HUD):** ícone, "NOVA BASE" ou "NOVO ARMAZÉM", moeda e preço (amarelo se dá para pagar, vermelho
  se não dá, "PAGO" em verde se a obra já foi paga). Some quando o terreno está fora da tela, a não ser no modo de escolha.
- **Prévia do alcance:** com o mouse no terreno (ou no modo de escolha), aparece a dica e os pontos no mapa:
  - base: "Atende N casas / M com problema agora" e um anel de pontos azuis passando pela metade das casas que ela
    atenderia (raio 13 u no OESTE e 16 u no LESTE);
  - armazém: "Base mais perto a N s a pé" e uma linha de pontos laranja pela rua até a base mais perto.
- **Comprar, dois caminhos, o mesmo upgrade:**
  1. clicar na placa ou no chão do terreno: paga ali (`UpgradesManager.TryBuy`, com a mesma recusa e o mesmo som de
     sempre) e a obra começa naquele terreno;
  2. comprar "Nova base" ou "Novo armazém" na aba de melhorias: a aba fecha, aparece "Obra paga: clique num terreno
     para a nova base", as placas daquele tipo pulsam com "PAGO" e, fora da tela, ficam presas na borda com uma seta
     apontando o terreno. O clique no terreno escolhido começa a obra.
- **Obra:** "EM OBRA" com barra de progresso por 3 s e marteladas; depois o prédio sobe do chão com um estalo e o som de
  pronto. A base nova é uma cópia da Guarda e o armazém uma cópia do Armazém: mesmo comportamento, entram no
  `LocationsManager` e a base passa a atender as casas mais perto dela (`GetNearestGuard`, em linha reta).

Prévia (montagem sobre captura real, não é captura do jogo): `C:\Users\PIETRO\analises-lilith\defesa-civil\expansao\previa_expansao.png`
(1280x720, modo de escolha com as duas placas presas embaixo). Folha dos sprites: `...\expansao\kit.png`; mapa
esquemático dos terrenos: `...\expansao\mapa.png`.

## Números (balanceamento)

| upgrade | id | preço | 2ª obra | máximo | ordem na aba |
|---|---|---|---|---|---|
| Nova base | `base2` | 150 | 240 | 2 | 7 |
| Novo armazém | `depot2` | 90 | 144 | 2 | 7 |

Ficam nos `.asset` (`Assets/Resources/Upgrades/Base2.asset` e `Depot2.asset`); terrenos, tempo de obra (3 s) e a prévia
(`reachPercentile` 0,5, pontos de 0,3 u a cada 0,7 u) no `ExpansionData.asset`. Nada de número de jogo no código.

Simulador (`docs/jam/sim/expansao.py`, 30 sementes, plano "carrinho", obra comprada depois de 5 itens do plano):

| jogador e compra | vitória | tempo da vitória | pico de caos |
|---|---|---|---|
| bom, sem expansão | 100% | 6:31 | 0,23 |
| bom, 1 base | 100% | 6:42 | 0,23 |
| bom, 2 bases + 2 armazéns | 100% | 7:19 | 0,23 |
| médio, sem expansão | 100% | 7:33 | 0,41 |
| médio, 1 base | 100% | 7:52 | 0,45 |
| médio, 2 bases + 2 armazéns | 100% | 8:33 | 0,45 |

Leitura: como a vitória é comprar a meta de 2500, toda compra que não paga a si mesma atrasa a vitória. A base encurta
a caminhada em 11% a 17% e se paga devagar; a 400 (o preço do GDD) uma base atrasava a vitória uns 35 a 40 s, e duas
mais de 1 min. A 150 uma base atrasa 11 s (bom) a 19 s (médio) e nunca faz perder: é uma escolha de conforto e de ritmo
(menos andança, mais casas cobertas), não uma armadilha. A 250 o médio já perde 31 s. `python expansao.py --custos 150,250 --depositos 90,150` refaz a tabela.

**Decisão a confirmar com o MEGABRAIN:** o GDD (seção de upgrades) diz "base extra: no máximo 1, `base2` a 400". O
pedido do Pietro (16/11) é 1 ou 2 bases e 1 ou 2 armazéns; segui o pedido (máximo 2 de cada). Para voltar a uma base
só, `maxLevel` 1 no `Base2.asset`.

## O que mudou (arquivos, todos novos)

- `Assets/SO/ExpansionData.cs`: terrenos (`LotSlot`: rótulo, tipo, entrada na linha central da rua), tempo de obra e prévia.
- `Assets/Systems/Expansion/`
  - `ExpansionManager.cs`: regra. Cria os terrenos no `Start` (modelos copiados da Guarda e do Armazém da cena, entrada
    nova ligada às duas pontas da rua mais perto no grafo de `Waypoint`), compra, crédito da aba, obra, prédio novo.
  - `BuildLot.cs`: o que se vê e o clique de um terreno (chão, placa, seta, dica, pontos); `LotPointer` no mesmo arquivo.
  - `ExpansionFeel.cs`: só escuta e responde (som do modo de escolha, marteladas, estalo e som de pronto).
  - `ExpansionEvents.cs`: `BuildStarted`, `BuildFinished`, `LocationBuilt`, `PlacementChanged`. Fora do `GameEvents.cs`
    para não disputar o arquivo do contrato; a compra continua em `GameEvents.UpgradePurchased`/`ActionDenied`.
  - `ExpansionMath.cs`: contas sem cena (rua mais perto, casas atendidas, anel, borda da tela).
  - `ExpansionBootstrap.cs`: o componente de ligação (abaixo).
  - `Editor/ExpansionDataMenu.cs`: `Tools/Jam/Expansão/Criar dados`. `Editor/ExpansionTests.cs`: 9 testes de editor.
  - `Resources/Expansion/`: 8 sprites pixel art (32 PPU, Point, sem compressão) e o `ExpansionData.asset`.
- `Assets/Resources/Upgrades/Base2.asset` e `Depot2.asset` (+ `.meta` escritos à mão, GUID novo).
- `Tools/Expansion/lots_kit.py`: gera os sprites com a paleta do `pixel_kit.py` (`--previa` faz a folha).
- `docs/jam/sim/expansao.py`: o simulador acima, em arquivo próprio (não mexe no `sim.py`).

Nenhum arquivo existente foi editado.

## Como ligar na cena

1. **Nada obrigatório no `Gameplay.unity`.** O `ExpansionBootstrap` cria o objeto `[Expansão]` (com `ExpansionManager` e
   `ExpansionFeel`) em toda cena que tem `GameManager`. Se preferir o objeto na cena, ponha os dois componentes num
   objeto vazio; o bootstrap não cria outro.
2. **Dados:** os `.asset` vêm no commit. Se o Unity não importar algum (ícone vazio, upgrade faltando na aba), rode
   `Tools/Jam/Expansão/Criar dados`. Sem `base2` e `depot2` em `Resources/Upgrades`, a expansão desliga com um aviso no console.
3. **Marcadores das bases novas (ajudante 1, depois do merge da jam-sistemas):** no `MapMarkers.cs`,
   ```csharp
   // OnEnable
   ExpansionEvents.LocationBuilt += OnLocationBuilt;
   // OnDisable
   ExpansionEvents.LocationBuilt -= OnLocationBuilt;

   private void OnLocationBuilt(Location location)
   {
       if (canvas == null || location == null || pins.Exists(p => p.house == location)) return;
       pins.Add(MakePin(location));
   }
   ```
   Sem isso a base nova funciona, mas fica sem a placa "BASE DA GUARDA" e sem o pino na borda.
4. O que a expansão procura na cena (tudo já existe): `LocationsManager` com a Guarda e a entrada dela, `UpgradesManager`,
   o `UICanvas` (as placas ficam numa camada `ExpansionSigns` atrás do resto do HUD), o objeto `UpgradeTab` (fecha ao
   comprar pela aba), `Houses` e `Trees` como irmãos no mundo e um `TMP_Text` no HUD para copiar a fonte.

## Conferências feitas

- Compila fora do Unity contra as DLLs do 6000.6.7f1 (Roslyn do 6000.4.6f1): 0 erros e 0 avisos nos arquivos da expansão,
  modos jogador (WebGL, sem `UnityEditor`) e editor; a sonda com erro de propósito acusou os 2 erros.
- Testes de editor: 9 de 9 passaram, rodados fora do Unity por um executor próprio (a sonda com `Assert` errado falhou
  como devia). Entre eles: as 4 entradas caem em cima da rua, nenhum prédio do mapa ocupa os terrenos, bases a mais de
  30 u uma da outra e a mais de 20 u da Guarda.
- Não abri o Unity nesta pasta (sem `Library`, e o Editor aberto é o da jam): falta ver no Play a ordem de desenho do
  chão do terreno, o tamanho da placa e o clique. Os testes rodam também no Test Runner (EditMode).
