# Consertos v6A (#526, 18/11/2026)

Três pedidos do Pietro: seta no sentido contrário, Tab para abrir a aba de melhorias e a aba voltar depois de
posicionar a guarita ou o para-raio. Preços, balanceamento e ritmo não foram mexidos; `Gameplay.unity` também não.

## 1. Seta no sentido contrário

Dois lugares, com a mesma causa: a margem de cima da tela (230 px, onde fica o HUD) empurra o marcador para baixo e a
seta passava a apontar para longe do alvo, que estava à vista.
- **Pino de chamado** (`MapMarkers`): casa logo abaixo da margem de cima. O pino descia para cima do telhado e a seta,
  que mira o topo da casa, virava para cima. Agora, com o pino abaixo do topo, a seta mira o meio da casa
  (`MapMarkers.ArrowAim`).
- **Placa NOVA BASE / NOVO ARMAZÉM** no modo de escolher o terreno (`BuildLot`): terreno à vista e placa na faixa da
  margem. A placa ia para a borda, com a seta apontando para fora (↗). Agora ela só vai para a borda com o terreno
  fora da tela; com o terreno à vista, ela desce para dentro da área livre e a seta aponta o terreno
  (`ExpansionMath.PlaceSign`).

Pino de borda, guarita, para-raio, tutorial e botão da aba já apontavam certo.

## 2. Tab abre e fecha a aba de melhorias

`UpgradePanelShortcuts` (no HUD, ligado pelo `UpgradePanelBoot`). O Tab funciona como um clique no botão da aba: o
botão afunda, toca o som de clique e só vale quando o mouse também alcançaria o botão (fora da pausa, do fim de jogo e
de janela por cima). O Esc continua só com a pausa. O Tab do protótipo (segurar para ver as casas em holograma, ligado
no `PlayerInput` da cena) saiu do `AnalyticsManager.SwitchViewMode`.

## 3. A aba volta depois de posicionar

Comprar guarita, para-raio ou base/armazém pela aba fecha a aba para o jogador escolher o lugar. Depois de posicionar,
a aba abre de novo sozinha, 0,45 s depois (`GameClock`, para na pausa), para dar tempo de ver a obra começar. Compra
pelo mapa não abre a aba.

## Conferência

- Testes EditMode: 195 passaram, 0 falharam (2 novos em `MarkerArrowTests`). `ValidateAll`: 0 erros, 0 avisos.
- Play com robô no Editor principal, 0 erros no console, 144 a 191 fps:
  - Tab abre, Tab fecha; Esc com a aba aberta pausa e o Tab na pausa não mexe; Esc volta ao jogo.
  - Guarita e para-raio: aba fechada ao comprar, ainda fechada 0,2 s depois, aberta 1,2 s depois de posicionar. Nova
    Base: igual.
  - Seta do pino com a casa sob a margem: para baixo (-90°). Placa com o terreno sob a margem: placa em 310 (topo da
    área livre), seta para baixo. Sem o conserto, a placa estaria em 341, dentro da margem, e iria para a borda com a
    seta para fora.
- Prints em `analises-lilith/jogo-defesa-civil/v6a/`: antes (`_c_pino.png`, `_c_exp.png`) e depois
  (`_c_depois_pino.png`, `_c_depois_placa2.png`).

## 4. Câmera: pan devolvido

Corrige a nota anterior ("Câmera sem pan"): o arrastar com botão direito e do meio voltou, `CameraController.cs` idêntico ao de antes do commit 9ce3e66; a roda só dá zoom, sem deslocar a vista, e o hash R28 no `scene_baseline.json` voltou ao original.
