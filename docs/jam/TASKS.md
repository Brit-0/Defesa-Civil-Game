# Backlog da jam

Status: `[ ]` a fazer · `[~]` em andamento · `[x]` pronto · `[!]` bloqueado.
**Só o MEGABRAIN edita este arquivo.** B e C registram status, pedidos e dúvidas no próprio arquivo (`STATUS_B.md`, `STATUS_C.md`); o MEGABRAIN passa para cá a cada integração.
Agentes: **A** MEGABRAIN (cena, integração, builds) · **B** Sistemas · **C** Feel & UI.

Revisado pelo MEGABRAIN em 16/11 (motivos em `DECISIONS.md`). O que depende do Unity Editor está marcado com **U1 a U8** e detalhado na seção "Pendências do Unity".

## Dia 1 (seg 16/11) — Fundação e loop completo, ainda feio

Meta do dia: jogar 5 minutos de ponta a ponta. Terremoto quebra casas, eu conserto, compro upgrade, o caos sobe e eu posso perder.

| Status | ID | Tarefa | Agente | Depende | Pronto quando |
|---|---|---|---|---|---|
| [x] | T00 | Kickoff de git: criar `jam` a partir da `main`, copiar `docs/jam/CLAUDE-jam.md` para `CLAUDE.md`, commit, push, criar `jam-sistemas` e `jam-feel`. Feito em 16/11 (conferido com `git ls-remote`: as 3 branches no remoto, `main` intacta). A verificação do MCP do Unity saiu daqui e virou a U1, porque depende do PC com o Unity. | A | — | 3 branches no remoto. |
| [x] | T01 | Corrigir bloqueios de build (B1, B2 de `ARCHITECTURE.md`). Código feito em 16/11 (commit `75ed78a`) e conferido sem o Unity: 24 arquivos de runtime varridos, 6 usos de API de Editor, todos dentro de `#if UNITY_EDITOR`, 0 fora. **Falta a prova real, que é o build Windows de fumaça: virou a U2.** | A | T00 | Nenhum `UnityEditor` fora de `#if UNITY_EDITOR` no runtime. |
| [x] | T02 | Build WebGL de fumaça. Medir tempo de carga e FPS; relatar. É a U3. Feito em 16/11: go (0,85 s de carga, 240 FPS parado e em movimento, 0 erro). | A | U2 | Relatório curto com go/no-go em `DECISIONS.md`, pelo critério da U3. |
| [ ] | T03 | `GameEvents` (contrato revisado da seção 6.1 de `ARCHITECTURE.md`), `GameState`, `DeniedReason`, `GameManager`, `LocationsManager`, `UpgradeData` e `DisasterData` mínimos. Com os `.meta` feitos à mão (seção 6.4.1). **Merge imediato na `jam`.** | B | T01 | Compila na `jam`; C já consegue assinar eventos. |
| [ ] | T04 | Refatorar `House` (`SetState`, `ApplyProblem`) e `GuardManager`; corrigir B4 a B8, B10 e B14 a B17; material e equipe saem do estoque no envio; tempo de trabalho e recompensa pela equipe e pelo dano (GDD 3.1); disparar os eventos de chamado e de trabalhador. | B | T03 | Fluxo de 3 passos funciona igual, com eventos saindo no log; dois chamados ao mesmo tempo não travam. |
| [ ] | T05 | `BalanceData` + `Stats` com os campos da seção 13 do GDD; remover números do código (`Workers = 3`, `workTime`, `speed`, `moneyPerCall`, o `0.8f` do envio). `ResourcesManager` dispara eventos. | B | T03 | Mudar um valor no asset muda o jogo sem recompilar. |
| [ ] | T06 | `DisasterManager` v1 com Terremoto: primeiro em ~6 s com 2 casas perto da Guarda, epicentro, raio, onda por distância, aviso, rampa no tempo. Só roda em `Playing`. | B | T04 | Terremotos acontecem sozinhos e ficam mais fortes. |
| [ ] | T07 | `ChaosManager` + derrota + reinício (recarrega a cena). | B | T04 | Ignorar chamados leva à derrota; reiniciar funciona. |
| [ ] | T08 | Upgrades: `UpgradesManager`, `UpgradeButton.cs`, aba preenchida em runtime. Upgrades `workers`, `speed`, `tools`, `cart`, `reward`. Compra negada dispara `ActionDenied`. | B | T05 | Comprar muda o jogo; preço sobe; botão desabilita sem dinheiro. |
| [ ] | T09 | Menu `Tools/Jam/Create Default Data` (gera `BalanceData`, upgrades e desastres com os valores do GDD). | B | T06, T08 | A roda o menu (U6) e os assets aparecem em `Resources/`. |
| [ ] | T10 | `Tweener`: IDs por instância (corrige B3). | C | T00 | Hover em uma casa não afeta as outras. |
| [ ] | T13 | `AudioManager` + sons provisórios **só CC0** para clique, envio, martelo, moeda, compra, erro, tremor, sirene. Não depende do T03. | C | T00 | Toda ação principal tem som; origem e licença de cada som listadas. |
| [ ] | T11 | HUD: contagem animada dos números, pulo do ícone, popup "+N" voando até o dinheiro. | C | T03 | Ganhar dinheiro é gostoso de ver. |
| [ ] | T12 | `HouseFeedback` (sacudida, fade da rachadura, poeira, pop no conserto), marcador de chamado pulsando, `CameraShaker` no terremoto. | C | T03 | Terremoto parece um terremoto. |
| [ ] | T17 | `ChaosBar` (escuta `ChaosChanged`) e `EndScreen` provisória (escuta `GameStateChanged`; mostra tempo, casas consertadas e dinheiro; botão Reiniciar). Prefabs novos. | C | T03 | A já consegue pôr os dois no `UICanvas` sem escrever código. |
| [ ] | T14 | Cena (U7): novos managers em `Scripts/`, `ChaosBar` e `EndScreen` no `UICanvas`, esconder Kit, desligar a ação do Tab (modo de energia), componentes novos nos prefabs, ligar referências, rodar o T09. | A | T03–T13, T17 | `jam` roda o loop completo. |
| [ ] | T15 | Playtest de 10 minutos (U8) e lista de bugs/ajustes em "Pedidos entre agentes". | A + usuário | T14 | Lista escrita. |
| [x] | T16 | Valores iniciais de balanceamento. Feito em 16/11 como a seção 13 do GDD (conta estimada, não planilha nem playtest); a calibragem é o T25. | A | GDD | Valores entregues para o T05 e o T09. |

**Ordem e linha de corte do Dia 1.** O caminho crítico é T03 → T04 → T06 → T07 → T14. Se o dia apertar (o Unity só entra quando o PC dele estiver disponível), T08 e T09 escorregam para a manhã do Dia 2 e o Dia 1 fecha sem upgrades. T05 não escorrega: sem ele os números voltam para o código.

## Dia 2 (ter 17/11) — Conteúdo, automação e juice

Meta do dia: jogo completo com vitória e derrota, todos os desastres e a arte recebida integrada.

| Status | ID | Tarefa | Agente | Depende | Pronto quando |
|---|---|---|---|---|---|
| [ ] | T20 | Tempestade: dano espalhado ao longo de ~8 s, raios com chance de incêndio (`LightningStruck`). Liberada aos 90 s de partida. | B | T06 | Dois tipos de desastre alternando. |
| [ ] | T21 | Incêndio (`OnFire`): temporizador (`FireTimeLeft01`), sem material, vira `Damaged` 3 se ignorado, peso 2 no caos. Incêndio isolado liberado aos 180 s. | B | T04 | Incêndio jogável do início ao fim. |
| [ ] | T22 | Automação níveis 1 a 4 (GDD, seção 7). | B | T08 | Cada nível remove um passo. |
| [ ] | T23 | Repasse da Prefeitura (upgrade `income`, `PassiveIncomePaid`). | B | T08, T07 | Dinheiro entra sem clique após destravar e cai com o caos. |
| [ ] | T24 | Centro de Operações (upgrade `goal`) + vitória (lógica). | B | T08 | Comprar o objetivo encerra a partida. |
| [ ] | T25 | Calibrar os valores com partida jogada (botão de 4× do `GameManager`). | B + A + usuário | T15, T22–T24 | Partida fecha em 10 a 15 min. |
| [ ] | T26 | VFX: chuva em overlay, flash de relâmpago, fogo animado com fumaça e temporizador, vinheta pelo caos. | C | T12, arte | Tempestade e incêndio têm cara própria. |
| [ ] | T27 | Trabalhadores (`WorkerFeel`): balanço ao andar, virar para a direção, pop ao nascer (`WorkerSpawned`) e ao chegar (`WorkerArrived`). | C | T04 | A rua parece viva. |
| [ ] | T28 | Faixa de aviso de desastre (Text Animator) + contagem + sirene. | C | T06 | O jogador nunca é pego sem aviso. |
| [ ] | T29 | Menu inicial por cima da cidade (mesma cena), pausa, telas finais de vitória e derrota, dicas dos 3 primeiros passos (o texto de instrução passa a escutar `CallOpened`). | C | T07, T24 | Dá para jogar sem ninguém explicar. |
| [ ] | T30 | Música em 2 camadas (calma/tensa) com crossfade pelo caos + sons finais. | C | T13, áudio | Música reage ao caos. |
| [ ] | T33 | Botões de upgrade: brilho quando há dinheiro, punch na compra (`UpgradeButtonJuice`). Ação negada (`ActionDenied`): som de erro, tremida e texto curto no lugar do clique. | C | T08 | Comprar e errar têm resposta. |
| [ ] | T31 | Importar arte recebida; trocar placeholders (Guarda, Armazém, ícones, cursor, rachadura). | A | arte | Sem placeholder visível. |
| [ ] | T32 | Cena do dia: ligar tudo, builds Windows e WebGL, playtest. Decisão final sobre WebGL. | A | T20–T33 | Build jogável do início ao fim. |

**Ordem de corte do Dia 2**, se atrasar: S2 (base extra) → incêndio isolado como desastre (o fogo fica só pelo raio) → nível 4 da automação. O Dia 3 não é cortado.

## Dia 3 (qua 18/11) — Polimento e entrega

Meta do dia: nada novo depois do meio-dia. Só ajuste, correção e entrega.

| Status | ID | Tarefa | Agente | Pronto quando |
|---|---|---|---|---|
| [ ] | T40 | Balanceamento final com 3 playtests completos. | B + A + usuário | Curva aprovada pelo usuário. |
| [ ] | T41 | Correção de bugs do playtest. | A, B, C | Lista zerada ou cortada. |
| [ ] | T42 | **Congelamento de features às 12h.** | todos | — |
| [ ] | T43 | Passada de polimento: tempos de animação, volumes, textos, bordas de tela. | C | Aprovado pelo usuário. |
| [ ] | T44 | Desempenho no WebGL (se aprovado no T32). | A | 60 FPS estável ou WebGL cortado. |
| [ ] | T45 | Página do itch.io: descrição, controles, capturas, GIF. | A + usuário | Página pronta. |
| [ ] | T46 | Builds finais, teste em máquina limpa, envio. Merge `jam` → `main`. | A | Entregue. |

## Stretch (só se o Dia 2 terminar no prazo)

| ID | Tarefa | Agente |
|---|---|---|
| ~~S1~~ | **Cortado em 16/11.** Energia e postes. | — |
| S2 | Uma base extra comprável (upgrade `base2` liga a segunda Guarda; trabalhadores continuam um estoque único). | B + A |
| ~~S3~~ | **Cortado em 16/11.** Propagação de incêndio. | — |
| S4 | Melhor tempo salvo (PlayerPrefs) e exibido no menu. | B + C |

## Pendências do Unity (U1 a U8)

Nada disto pôde ser feito na sessão de 16/11, porque o PC dela não tem o Unity 6000.6.7f1 (só 6000.0 e 6000.4; abrir o projeto numa versão diferente reimporta e altera arquivos, então não foi aberto). Tudo é do MEGABRAIN, no PC onde o Unity está aberto no projeto. O prompt pronto está em `PROMPTS_PRONTOS.md`, seção 5.

| Status | ID | O quê | Quando | Pronto quando |
|---|---|---|---|---|
| [x] | U1 | **Feito em 16/11 pelo `unity` CLI** (commit `60693d2`): faltava neste projeto o pacote `com.unity.pipeline`, instalado com `unity pipeline install`; o Editor só carrega o pacote quando a janela dele ganha foco. O MCP `unity-editor` da sessão continua apontado para o outro projeto, então os comandos vão por `unity command ... --project-path`. Chamada real: cena `Gameplay`, filhos de `Scripts` = FeedbacksManager, GPS, WaypointCreator, ResourcesManager, AnalyticsManager, EletricityManager, GuardManager. Console: 0 erros, 26 avisos. Histórico: MCP do Unity respondendo neste projeto. Na sessão sem Unity ele respondeu `No Pipeline instance found for project: C:\UnityProjects\My project (1)`: estava apontado para outro projeto e sem Editor aberto. O pacote `com.unity.ai.assistant` já está no `manifest.json`. | Já | Uma chamada real devolve a cena `Gameplay` e os filhos de `Scripts`. |
| [x] | U2 | Build Windows de fumaça em `Builds/Windows/` (pasta ignorada pelo git). É a prova real da T01. **Feito em 16/11:** build de desenvolvimento `Builds/Windows/DefesaCivil.exe`, 0 erros, 25 avisos, 72 s, 235 MB; o executável abriu em 1280x720 e mostrou a cidade e o HUD (3 trabalhadores, 0 material, 0 kits); `Player.log` sem erro nem exceção. | Já | O executável abre e mostra a cidade; console sem erro de compilação. |
| [x] | U3 | **Feito em 16/11: GO.** Build sem erros (473 s, 18 MB), carga em 0,85 s, 240 FPS parado e 240 FPS com a câmera em movimento (travado pelo monitor de 240 Hz), 0 erro no console do Chrome; detalhes e ressalvas em `DECISIONS.md`. Repetir no T32 com o jogo completo. Histórico: build WebGL de fumaça em `Builds/WebGL/` (é a T02). Precisa do módulo Web no 6000.6.7f1 (Unity Hub → Installs → Add modules). Medir tempo de carga, FPS parado e FPS com a câmera em movimento. | Já, depois da U2 | Go/no-go registrado em `DECISIONS.md`, pelo critério que está lá. |
| [x] | U4 | Capturas de referência em `docs/jam/reference/`: `01_hud_inicial.png` a `08_aba_upgrades.png` (lista na seção 4 do `MEGABRAIN.md`). Sem salvar a cena. **Feito em 16/11**, em Play Mode, 1280x720, antes de integrar B e C, com cliques pelo sistema de eventos (casa, base, botões). No mesmo Play: **B14 confirmado** (2 enviados para a casa A e um segundo chamado aberto no mesmo quadro: saiu 1 trabalhador, a casa A ficou com `interactable = false`, sem progresso e sem coleta depois de 30 s, e o trabalhador que saiu não voltou para o estoque). **B16 confirmado pela metade:** o painel do Armazém mostra o texto certo (nome, descrição e "1 por trabalhador", iguais aos dados), mas o `House.Awake` não roda no Armazém: os dois usam o material `HouseMAT` do asset, sem cópia própria. Lidos no Editor: `GPS.speed` = 1; `workTime` = 10 s no `House.prefab`, 2 s no `Warehouse.prefab`, 0 no `Guard.prefab`; `moneyPerCall` = 10. O trabalhador usa o sprite provisório `FeedbacksDemoGhost`. | Já | 8 imagens commitadas. |
| [ ] | U5 | Compilar cada entrega do B, que trabalha sem Unity, e devolver os erros em "Pedidos entre agentes". A primeira é o T03. | A cada push do B | `jam` compila depois de cada merge. |
| [ ] | U6 | Dados: rodar `Tools/Jam/Create Default Data` a cada vez que o B aumentar o menu e commitar os `.asset` com os `.meta`. Ler na cena o `speed` do `GPS` e o `workTime` do `House.prefab` e conferir com a seção 13 do GDD. | A partir do T05 | Assets em `Assets/Resources/{Balance,Upgrades,Disasters}/`. |
| [ ] | U7 | Cena e prefabs (é o T14): GameObjects dos managers novos em `Scripts/`, `ChaosBar` e `EndScreen` no `UICanvas`, esconder o Kit do HUD, desligar a ação do Tab, `HouseFeedback` no `House.prefab`, campos do `UpgradeButton.prefab`, e o que estiver em `STATUS_B.md` e `STATUS_C.md`. | Depois do T03–T13 | `jam` roda o loop completo no Play. |
| [ ] | U8 | Play Mode: playtest de 10 minutos (T15). Conferir de propósito o B14 (abrir um segundo chamado enquanto a primeira equipe ainda sai da base) e o B16 (painel de informação do Armazém). | Depois da U7 | Lista de bugs em "Pedidos entre agentes". |

**Branch `pc-felipe`: integrada em 16/11 (merge `0edb447`).** Entrou sem conflito. Cena conferida no Editor antes e depois do merge: 1519 objetos, 221 árvores, 70 casas, 105 waypoints, 0 referência quebrada, 0 árvore sem sprite, console sem erro. A branch não ganhou commits depois do `2bb4426`. Observação: `Assets/_Project/Data/Cities/city_layout.json` ainda cita os dois sprites apagados (`Arvore_Quintal` e `Arvore_Quintal_Baixa`); só importa se a cidade for regerada. Falta avisar quem usa a `pc-felipe` que cena, daqui em diante, só na `jam`. Histórico: apareceu no remoto em 16/11 uma branch fora do combinado, saída da `main` (commit `2bb4426`, 09:10, "Removed wrong tree sprites"), que altera `Gameplay.unity` e apaga sprites de árvore. A `jam` ainda não tocou na cena: integrada agora, entra sem conflito; integrada depois da U7, vira conflito de cena. Não foi integrada na sessão sem Unity porque não dá para conferir a cena sem o Editor. Fazer no passo 3b do prompt do MEGABRAIN no Unity, e avisar quem usa essa branch que cena só se edita na `jam`.

## Mapa novo e visual (pedidos do usuário, 16/11)

| Status | ID | O quê | Pronto quando |
|---|---|---|---|
| [x] | M1 | Mapa v5 do zero, Guarda no centro, chão assado, vista inicial na Guarda (commit `1a0af4d`). | 0 erro de validação, testes EditMode passando. |
| [x] | M2 | Mapa v6 muito maior (7 x 6 quarteirões, 155 casas, 570 decorações), câmera mais aberta (zoom 8, de 3 a 16). | 0 erro e 0 aviso de validação; 2º Build sem mudança. |
| [x] | M3 | Luz 2D com normal map em casas, árvores e chão; sol, postes, Guarda, Armazéns; bloom leve. | Relevo visível no Play; testes passando. |
| [x] | M4 | Densidade: miudezas no chão assado e props novos. | Conferido no Play. |
| [x] | M4b | Sombras: projetadas e assadas no chão para tudo o que fica parado; `ProjectedShadow` nos trabalhadores. | Conferido no Play (capturas 14 e 15). |
| [x] | M5 | Playtest do loop no mapa v6 com capturas em `analises-lilith/jogo-defesa-civil/resultados/` (14_ em diante). | Loop inteiro jogado; bugs aqui embaixo. — tutorial de 13 passos, 3 mini games e PULAR jogados como iniciante (`04e97dd`, capturas 23 a 26). |
| [x] | M7 | Escala real das plantas e props, canteiro de terra no pé de planta em chão duro, sombra de contato no pé, zoom inicial 12; `Tools/City/Auditar Escala` + `ScaleAuditTests` (`f803e45`, `bada1eb`). | Árvore maior 0,59 da casa média, arbusto 0,32, flor 0,15; capturas 27 a 29. |
| [x] | M8 | Expansão do B (`Assets/Systems/Expansion`): 2 bases e 2 armazéns comprados no upgrade, placa no terreno, obra, marcador na base nova (`bada1eb`). | Comprado, construído e com marcador no Play; capturas 30 e 31. |
| [x] | M9 | Fogo e energia do B (`Assets/Systems/Fire`, `Assets/Systems/Energy`): jato, energia com Unlock, apagão, RELIGAR e 2º tutorial; dica do jato no `Tutorial.cs` (`bada1eb`). | Jato apagou 4 chamas, RELIGAR fecha o apagão, balão do 2º tutorial fica na tela com incêndio; capturas 32 a 37. |
| [x] | M6 | WebGL com o mapa v6: medir FPS (meta 60). | Número registrado em `DECISIONS.md`. — 152 a 235 FPS no PC do Pietro; 51 a 59 com a CPU 2x mais lenta (era 31 a 37), depois de juntar o Canvas das árvores e do atlas das decorações. |

## Pedidos entre agentes

- Do B para o MEGABRAIN: `STATUS_B.md`. Do C para o MEGABRAIN: `STATUS_C.md`.
- (C → MEGABRAIN, 16/11, pedido do Pietro, só esta linha) VFX legíveis da `jam-feel` (P8 do `STATUS_C.md`): nada para ligar na cena, os componentes nascem sozinhos no `Feel.prefab` ao carregar; depois do merge, conferir no Play e, se quiser, rodar `Tools/Jam/Create Feel Prefabs`. Detalhes e 2 ajustes opcionais no `STATUS_C.md`.
- Do MEGABRAIN para B e C (bugs de playtest, erros de compilação, mudanças): aqui embaixo. Formato: `- [ ] (MEGABRAIN → B) pedido — contexto`

- [x] (MEGABRAIN → B) `BalanceData.cs`: padrão de `workerSpeed` de 1 para 2. — feito pelo B no T25 (2,5 na classe e no asset, `903ee33`).
- [ ] (MEGABRAIN → C) Fogo com luz: uma `Light2D` laranja piscando em cada casa pegando fogo (a cena agora tem luz 2D e bloom leve, então ela vai brilhar). Raio perto de 3 u, intensidade de 1 a 1,5.
- [ ] (MEGABRAIN → C) Bolhas de chamado legíveis no zoom aberto: o zoom agora vai até 16; manter a bolha com tamanho constante na tela (escala pelo `orthographicSize`).

Pedidos da revisão do gameloop (`GAMELOOP_REVISAO.md`; o Pietro pediu que ficassem aqui, 16/11):

- [x] (MEGABRAIN → B, informativo) Corrigido na integração (`bada1eb`), nos arquivos do B: `ExpansionManager.MakeSignLayer` achava o `UICanvas` do mundo (as placas iam para o mapa e a aba não fechava), agora pega o `Canvas` raiz que não é World Space; `EnergyTutorial.Running` (novo) e o `Tutorial` não mostra nem esconde dica enquanto o 2º tutorial usa o balão (a dica do fogo sumia com o passo ENERGIA 1/3).
- [x] (B → MEGABRAIN) **Ligar o tutorial, a META e os mini games: nada a fazer na cena.** — feito em `04e97dd`, capturas 23 a 26. O `GameloopBootstrap` (`Assets/Systems/Tutorial/`) cria `MiniGameFeel`, `GoalTracker` e `Tutorial` sozinho em todo carregamento de cena que tenha `GameManager`, e o `Tutorial` desliga o `TutorialTips` de C em tempo de execução. Pedido:
  1. Fazer o merge da `jam-sistemas`, compilar no Unity e mandar os erros para cá (U5). Pelo Roslyn, com as DLLs do 6000.6.7f1, deu 0 erros.
  2. Rodar `Tools/Jam/Reset Tutorial` e dar Play.
  3. Tirar quatro capturas e pôr em `analises-lilith/jogo-defesa-civil/resultados/`, no lugar das prévias `B3_previa_*`:
     - o cartão inicial;
     - o passo 2, com a seta na casa;
     - um "NO PONTO!";
     - os aros de escorar num aviso de terremoto.
  4. Conferir no Play:
     - os aros e a faixa dourada ficam no centro da casa, e não no pé dela (o pivô das casas não foi visto);
     - a seta aponta para a borda quando a casa sai da tela;
     - o PULAR encerra tudo, e uma segunda partida (Restart) não mostra o tutorial.

  Opcional:
  - pôr os três componentes num objeto `[Gameloop]` da cena (o bootstrap os acha e não duplica);
  - apagar o objeto do `TutorialTips`.
- [ ] (B → MEGABRAIN) **`BalanceData.asset` e `Upgrades/*.asset` editados à mão** (YAML) com os valores novos. Os 21 campos novos não estão no `.asset` e usam o padrão da classe. **Não rodar Reset Data To Defaults** (o pedido antigo do T25 caiu): os `.asset` e a classe já dão o mesmo no simulador. Se o Inspector mostrar outro número, vale o `.asset`.
- [ ] (B → C) **Som e efeito dos eventos novos, só escutando** (os mini games já têm som e popup próprios em `MiniGameFeel`; não duplicar):
  - **`RepairStreak(n, pos)`:** som da coleta com o tom subindo, `1 + 0.06 × (n−1)`, até 1,5. Pede um `AudioManager.PlaySfx` com tom fixo, porque hoje só há `pitchVariation`.
  - **`MilestoneReached(consertos, voluntários)`:**
    - fanfarra `Collectibles_Items_Powerup/jingle_chime_05_positive.wav`;
    - texto grande "MARCO! +1 VOLUNTÁRIO" no centro;
    - punch no contador de trabalhadores. A META já dá o soco dela.
  - **`OccurrenceCleared(occ)`:**
    - popup dourado com `occ.bonus` em `occ.position`;
    - som `Collectibles_Items_Powerup/points_ticker_bonus_score_reward_jingle_01.wav`;
    - quando `occ.fast` (x2), escrever "RÁPIDO! x2".
  - **`DisasterForecasted(f)`:** círculo vermelho no chão em `f.position` com raio `f.radius` (0 fora do terremoto; aí, marcar `f.targets`) durante os `f.seconds` do aviso. Os aros laranja de escorar nas casas já são do `MiniGameFeel`.
  - **`f.isStrong`:** banner "TERREMOTO FORTE" maior e mais longo, com um som grave.
