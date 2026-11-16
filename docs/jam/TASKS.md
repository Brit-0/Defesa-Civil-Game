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
| [ ] | T02 | Build WebGL de fumaça. Medir tempo de carga e FPS; relatar. É a U3. | A | U2 | Relatório curto com go/no-go em `DECISIONS.md`, pelo critério da U3. |
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
| [ ] | U3 | Build WebGL de fumaça em `Builds/WebGL/` (é a T02). Precisa do módulo Web no 6000.6.7f1 (Unity Hub → Installs → Add modules). Medir tempo de carga, FPS parado e FPS com a câmera em movimento. | Já, depois da U2 | Go/no-go registrado em `DECISIONS.md`, pelo critério que está lá. |
| [ ] | U4 | Capturas de referência em `docs/jam/reference/`: `01_hud_inicial.png` a `08_aba_upgrades.png` (lista na seção 4 do `MEGABRAIN.md`). Sem salvar a cena. | Já | 8 imagens commitadas. |
| [ ] | U5 | Compilar cada entrega do B, que trabalha sem Unity, e devolver os erros em "Pedidos entre agentes". A primeira é o T03. | A cada push do B | `jam` compila depois de cada merge. |
| [ ] | U6 | Dados: rodar `Tools/Jam/Create Default Data` a cada vez que o B aumentar o menu e commitar os `.asset` com os `.meta`. Ler na cena o `speed` do `GPS` e o `workTime` do `House.prefab` e conferir com a seção 13 do GDD. | A partir do T05 | Assets em `Assets/Resources/{Balance,Upgrades,Disasters}/`. |
| [ ] | U7 | Cena e prefabs (é o T14): GameObjects dos managers novos em `Scripts/`, `ChaosBar` e `EndScreen` no `UICanvas`, esconder o Kit do HUD, desligar a ação do Tab, `HouseFeedback` no `House.prefab`, campos do `UpgradeButton.prefab`, e o que estiver em `STATUS_B.md` e `STATUS_C.md`. | Depois do T03–T13 | `jam` roda o loop completo no Play. |
| [ ] | U8 | Play Mode: playtest de 10 minutos (T15). Conferir de propósito o B14 (abrir um segundo chamado enquanto a primeira equipe ainda sai da base) e o B16 (painel de informação do Armazém). | Depois da U7 | Lista de bugs em "Pedidos entre agentes". |

**Branch `pc-felipe`: integrada em 16/11 (merge `0edb447`).** Entrou sem conflito. Cena conferida no Editor antes e depois do merge: 1519 objetos, 221 árvores, 70 casas, 105 waypoints, 0 referência quebrada, 0 árvore sem sprite, console sem erro. A branch não ganhou commits depois do `2bb4426`. Observação: `Assets/_Project/Data/Cities/city_layout.json` ainda cita os dois sprites apagados (`Arvore_Quintal` e `Arvore_Quintal_Baixa`); só importa se a cidade for regerada. Falta avisar quem usa a `pc-felipe` que cena, daqui em diante, só na `jam`. Histórico: apareceu no remoto em 16/11 uma branch fora do combinado, saída da `main` (commit `2bb4426`, 09:10, "Removed wrong tree sprites"), que altera `Gameplay.unity` e apaga sprites de árvore. A `jam` ainda não tocou na cena: integrada agora, entra sem conflito; integrada depois da U7, vira conflito de cena. Não foi integrada na sessão sem Unity porque não dá para conferir a cena sem o Editor. Fazer no passo 3b do prompt do MEGABRAIN no Unity, e avisar quem usa essa branch que cena só se edita na `jam`.

## Pedidos entre agentes

- Do B para o MEGABRAIN: `STATUS_B.md`. Do C para o MEGABRAIN: `STATUS_C.md`.
- Do MEGABRAIN para B e C (bugs de playtest, erros de compilação, mudanças): aqui embaixo. Formato: `- [ ] (MEGABRAIN → B) pedido — contexto`

_(vazio)_
