# Backlog da jam

Status: `[ ]` a fazer · `[~]` em andamento · `[x]` pronto · `[!]` bloqueado.
Cada agente atualiza só a coluna Status das próprias tarefas e a seção "Pedidos entre agentes".
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

## Pedidos entre agentes

Formato: `- [ ] (de → para) pedido — contexto`

_(vazio)_
