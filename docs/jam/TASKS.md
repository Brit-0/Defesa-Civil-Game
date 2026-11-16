# Backlog da jam

Status: `[ ]` a fazer · `[~]` em andamento · `[x]` pronto · `[!]` bloqueado.
Cada agente atualiza só a coluna Status das próprias tarefas e a seção "Pedidos entre agentes".
Agentes: **A** Integrador/Cena · **B** Sistemas · **C** Feel & UI · **Org** organizador (chat do Project).

## Dia 1 (seg 16/11) — Fundação e loop completo, ainda feio

Meta do dia: jogar 5 minutos de ponta a ponta. Terremoto quebra casas, eu conserto, compro upgrade, o caos sobe e eu posso perder.

| Status | ID | Tarefa | Agente | Depende | Pronto quando |
|---|---|---|---|---|---|
| [ ] | T00 | Kickoff: criar `jam` a partir da `main`, copiar `docs/jam/CLAUDE-jam.md` para `CLAUDE.md`, commit, push, criar `jam-sistemas` e `jam-feel`, verificar MCP do Unity. | A | — | 3 branches no remoto; MCP responde. |
| [ ] | T01 | Corrigir bloqueios de build (B1, B2 de `ARCHITECTURE.md`) e gerar build Windows de fumaça. | A | T00 | Executável abre e mostra a cidade. |
| [ ] | T02 | Build WebGL de fumaça. Medir FPS e tempo de carga; relatar. | A | T01 | Relatório curto com go/no-go em `DECISIONS.md`. |
| [ ] | T03 | `GameEvents`, `GameState`, `GameManager`, `LocationsManager` (registro de casas). **Merge imediato na `jam`.** | B | T01 | Compila; C já consegue assinar eventos. |
| [ ] | T04 | Refatorar `House` (`SetState`, `ApplyProblem`) e `GuardManager`; corrigir B4 a B8 e B10; disparar os eventos de chamado. | B | T03 | Fluxo atual funciona igual, com eventos saindo no log. |
| [ ] | T05 | `BalanceData` + `Stats`; remover números do código (trabalhadores e material iniciais, `workTime`, `speed`, `moneyPerCall`). `ResourcesManager` dispara eventos. | B | T03 | Mudar um valor no asset muda o jogo sem recompilar. |
| [ ] | T06 | `DisasterData` + `DisasterManager` v1 com Terremoto (epicentro, raio, aviso, escalada no tempo). | B | T04 | Terremotos acontecem sozinhos e ficam mais fortes. |
| [ ] | T07 | `ChaosManager` + condição de derrota + reinício da partida. | B | T04 | Ignorar chamados leva à derrota; reiniciar funciona. |
| [ ] | T08 | Upgrades: `UpgradeData`, `UpgradesManager`, `UpgradeButton.cs`, aba preenchida em runtime. Upgrades `workers`, `speed`, `tools`, `cart`, `reward`. | B | T05 | Comprar muda o jogo; preço sobe; botão desabilita sem dinheiro. |
| [ ] | T09 | Menu `Tools/Jam/Create Default Data` (gera os assets padrão). | B | T06, T08 | A roda o menu e os assets aparecem em `Resources/`. |
| [ ] | T10 | `Tweener`: IDs por instância (corrige B3). | C | T00 | Hover em uma casa não afeta as outras. |
| [ ] | T11 | HUD: contagem animada dos números, pulo do ícone, popup "+N" voando até o dinheiro. | C | T03 | Ganhar dinheiro é gostoso de ver. |
| [ ] | T12 | `HouseFeedback` (sacudida, fade da rachadura, poeira, pop no conserto), marcador de chamado pulsando, `CameraShaker` no terremoto. | C | T03 | Terremoto parece um terremoto. |
| [ ] | T13 | `AudioManager` + sons provisórios para clique, envio, martelo, moeda, compra, tremor, sirene. | C | T03 | Toda ação principal tem som. |
| [ ] | T14 | Cena: novos managers em `Scripts/`, barra de caos no HUD, esconder Kit, tela de derrota provisória, ligar referências, rodar T09. | A | T03–T13 | `jam` roda o loop completo. |
| [ ] | T15 | Playtest de 10 minutos e lista de bugs/ajustes em "Pedidos entre agentes". | A + usuário | T14 | Lista escrita. |
| [ ] | T16 | Modelo de balanceamento (planilha: custos, renda, ritmo de desastres, tempo até a meta). | Org | GDD | Valores iniciais entregues para o T09. |

## Dia 2 (ter 17/11) — Conteúdo, automação e juice

Meta do dia: jogo completo com vitória e derrota, todos os desastres e a arte recebida integrada.

| Status | ID | Tarefa | Agente | Depende | Pronto quando |
|---|---|---|---|---|---|
| [ ] | T20 | Tempestade: dano espalhado ao longo de ~8 s, raios com chance de incêndio. | B | T06 | Dois tipos de desastre alternando. |
| [ ] | T21 | Incêndio (`OnFire`): temporizador, sem material, vira `Damaged` 3 se ignorado, peso 2 no caos. | B | T04, P2 | Incêndio jogável do início ao fim. |
| [ ] | T22 | Automação níveis 1 a 4. | B | T08, P3 | Cada nível remove um passo. |
| [ ] | T23 | Renda passiva. | B | T08, **P1** | Dinheiro entra sem clique após destravar. |
| [ ] | T24 | Objetivo final + vitória (lógica). | B | T08, P4 | Comprar o objetivo encerra a partida. |
| [ ] | T25 | Ajustar valores com o modelo de balanceamento. | B + Org | T16, T15 | Partida fecha em 10 a 15 min. |
| [ ] | T26 | VFX: chuva em overlay, flash de relâmpago, fogo animado com fumaça, vinheta pelo caos. | C | T12, arte | Tempestade e incêndio têm cara própria. |
| [ ] | T27 | Trabalhadores: balanço ao andar, virar para a direção, pop ao sair/entrar. | C | T10 | A rua parece viva. |
| [ ] | T28 | Faixa de aviso de desastre (Text Animator) + contagem + sirene. | C | T06 | O jogador nunca é pego sem aviso. |
| [ ] | T29 | Menu inicial, pausa, telas de vitória e derrota, dicas dos 3 primeiros passos. | C | T07, T24, P6 | Dá para jogar sem ninguém explicar. |
| [ ] | T30 | Música em 2 camadas (calma/tensa) com crossfade pelo caos + sons finais. | C | T13, áudio | Música reage ao caos. |
| [ ] | T31 | Importar arte recebida; trocar placeholders (Guarda, Armazém, ícones, cursor, rachadura). | A | arte | Sem placeholder visível. |
| [ ] | T32 | Cena do dia: ligar tudo, builds Windows e WebGL, playtest. Decisão final sobre WebGL. | A | T20–T31 | Build jogável do início ao fim. |

## Dia 3 (qua 18/11) — Polimento e entrega

Meta do dia: nada novo depois do meio-dia. Só ajuste, correção e entrega.

| Status | ID | Tarefa | Agente | Pronto quando |
|---|---|---|---|---|
| [ ] | T40 | Balanceamento final com 3 playtests completos. | B + Org + usuário | Curva aprovada pelo usuário. |
| [ ] | T41 | Correção de bugs do playtest. | A, B, C | Lista zerada ou cortada. |
| [ ] | T42 | **Congelamento de features às 12h.** | todos | — |
| [ ] | T43 | Passada de polimento: tempos de animação, volumes, textos, bordas de tela. | C | Aprovado pelo usuário. |
| [ ] | T44 | Desempenho no WebGL (se aprovado no T32). | A | 60 FPS estável ou WebGL cortado. |
| [ ] | T45 | Página da jam: descrição, controles, capturas, GIF. | Org + usuário | Página pronta. |
| [ ] | T46 | Builds finais, teste em máquina limpa, envio. Merge `jam` → `main`. | A | Entregue. |

## Stretch (só se o Dia 2 terminar no prazo)

| ID | Tarefa | Agente |
|---|---|---|
| S1 | Energia: queda de energia + postes como gestão paralela. | B + C |
| S2 | Bases extras compráveis. | B + A |
| S3 | Propagação de incêndio para casas vizinhas. | B |
| S4 | Melhor tempo salvo e exibido no menu. | B |

Recomendação do organizador: tratar S1 como cortado. O sistema atual é um protótipo e, para ficar bom, pede UI de construção de postes, regras e arte próprias. Isso compete direto com o polimento do núcleo.

## Pedidos entre agentes

Formato: `- [ ] (de → para) pedido — contexto`

_(vazio)_
