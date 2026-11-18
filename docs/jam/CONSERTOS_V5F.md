# Consertos v5F (#525, 18/11/2026)

Quatro problemas que o Pietro achou jogando. Preços e ritmo de desastres não foram mexidos; `Gameplay.unity` também não.

## 1. "Crash" do Unity

**O que os logs mostram:** não houve crash nativo do jogo. O Windows registrou às 22:59 de 17/11 (evento 10002) que o
`Unity.exe` "foi finalizado porque travou". Ou seja, a janela parou de responder e foi fechada. O único crash de verdade
(evento 1000, 11:01) foi de um worker de importação da cópia `-v3a`, fora do jogo. Nenhuma exceção antes do
travamento: o log que parou às 22:59 (cópia `-v3b`) termina logo depois de sair do Play, sem erro do jogo.

**O que pesa no Editor (fora do código do jogo):**
- O Unity Hub está fechado, mas o Editor foi aberto por ele (`-hubIPC`). Toda conversa com o Hub expira:
  55 avisos `IPCStream (hubIPCService): IPC stream failed to write (Timed out)` no log do Editor principal e 557 na
  cópia `-v3c`. Cada um segura o Editor por um tempo. **Abrir o Unity Hub (ou abrir o Editor pelo Hub de novo) acaba
  com isso.**
- Até 4 Editors abertos ao mesmo tempo (principal, `-feel`, `-v3a`, `-v3b`), com robôs das ajudantes no Play.

**O que o jogo fazia de errado e foi consertado:**
- `GameClock.DeltaTime` não tinha teto. Depois de um engasgo do Editor, o quadro seguinte trazia segundos de uma vez.
  Os laços de efeito por tempo (ondas da enchente no `FloodFx`, que criam GameObject e tween por onda) então criavam
  centenas de objetos num quadro só, o que segurava o quadro seguinte também. Agora o teto é o mesmo do `Time.deltaTime`
  (`Time.maximumDeltaTime`, 1/3 s).
- Meteoro anunciado num quarteirão sem casa livre (todas com equipe, prontas ou noutro incidente) não caía, e o
  `MeteorFx` nunca soltava o aviso. A rocha ficava parada no céu, soltando fogo, brasas e fumaça a cada 0,04 s até o
  fim da partida, e vários meteoros presos se somavam. Agora o `BlockDisasterManager.Run` dispara
  `GameEvents.MeteorCancelled` e o `MeteorFx.Cancel.cs` (parcial novo) descarta rocha, halo, cauda, sombra e alvo.

A revisão dos sistemas novos (enchente, meteoro, guaritas, som, SoundBank, para-raio, energia) não achou laço sem fim.
Ficou anotado, sem mexer: os canais de loop do `AudioManager` não encolhem (crescem por dono a cada reinício). Se travar
de novo com o Hub aberto: no travamento, conectar o Rider ou o VS, dar Pause e ver a pilha da thread principal.

## 2. Ponto certo escurecido

O `WorkRingLayout` põe uma trilha escura (85%) atrás do anel de progresso. Na 2ª obra da mesma casa (ou na 2ª ida ao
Armazém), `SetSiblingIndex(índice do anel)` com a trilha já antes do anel a punha **depois** dele. A trilha passava a
cobrir o anel e a janela dourada, como um véu preto. Agora `WorkRingLayout.PlaceBehind` deixa a trilha sempre logo
atrás (teste `WorkRingLayoutTests`). A fumaça do meteoro preso (item 1) também escurecia as casas do quarteirão.

## 3. Aba do Armazém

"1 por trabalhador" vinha fixo do `Armazém.asset`. Agora a aba lê `Stats.MaterialPerWorker` cada vez que abre
(`Location.SpecificText`, que o `Warehouse` sobrescreve), então acompanha o upgrade Carrinho.

## 4. Painel de energia

"SOBRECARGA" (e "APAGÃO!") ficava na mesma altura do número e cruzava "6/5". Agora fica na linha do título, no lugar
de "ENERGIA", que volta quando a rede normaliza. O número ganhou a largura toda do painel.

## Conferência

No Editor principal, 18/11 00:45–00:49:
- Testes EditMode: 187 de 187 (inclui os 2 do `WorkRingLayoutTests`). `city_ground.png` igual antes e depois.
- `ValidateAll`: 0 erros e 0 avisos. Compilação fora do Unity (runtime e editor): 0 erros.
- Play curto com robô (`analises-lilith/jogo-defesa-civil/v5f/robo.cs` + `rodar.ps1`), sem erro no console, ~280 fps:
  energia ligada com 3 Automações (7/5) mostra "SOBRECARGA" na linha do título e a tinta dos dois textos não se cruza;
  Carrinho nv 1 dá "2 por trabalhador" na aba do Armazém; na 2ª obra da mesma casa a trilha fica atrás do anel
  (índice 1 contra 2); meteoro forçado com as 4 casas do quarteirão ocupadas some sozinho depois do aviso (0 meteoros
  pendentes, 0 sombras, 0 rochas).
- Prints (1920x1080, Game view do Editor principal) em `analises-lilith/jogo-defesa-civil/v5f/`: `v5f_energia.png`,
  `v5f_armazem.png`, `v5f_anel_antes.png` (linha velha simulada: anel escurecido) e `v5f_anel_depois.png`.
