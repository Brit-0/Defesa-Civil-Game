# Prompts para os agentes

O usuário só precisa: abrir o Unity com o projeto (agentes A e C), abrir o terminal na pasta certa, rodar `claude` e colar o prompt. Cada prompt já inclui git, verificação e relatório.

Ordem do Dia 1: **A1 primeiro e sozinho** (cria as branches e conserta o build). Depois **B1** e **C1** em paralelo. Quando B avisar que o T03 foi para o remoto, A integra.

---

## A1 — Kickoff (Agente A, PC com o Unity, pasta principal do repositório)

```
Você é o Agente A (Integrador/Cena) de uma game jam de 3 dias. Execute tudo sozinho, na ordem, e só pare se algo falhar ou exigir decisão minha.

1. Estado do git: rode `git status` e `git branch -a`. Devemos estar na `main`, com a pasta `docs/jam/` nova e não rastreada (foi escrita pelo organizador). Se houver outras mudanças não commitadas, pare e me mostre.
2. Rode `git pull` na main. Crie a branch: `git switch -c jam`.
3. Leia `docs/jam/README.md`, `GDD.md`, `ARCHITECTURE.md`, `AGENTS.md` e `TASKS.md`.
4. Substitua o `CLAUDE.md` da raiz pelo conteúdo de `docs/jam/CLAUDE-jam.md` (o bloco de aviso do topo pode sair). Commit: "docs: base de documentação da jam" incluindo `docs/jam/` e `CLAUDE.md`. `git push -u origin jam`.
5. Crie e publique as branches dos outros agentes a partir da jam, sem trocar de branch: `git branch jam-sistemas`, `git branch jam-feel`, `git push -u origin jam-sistemas jam-feel`.
6. MCP do Unity: rode `claude mcp list` e chame de fato uma ferramenta do unity-mcp (nome da cena aberta e filhos do GameObject `Scripts`). Se falhar, diagnostique e me diga o que depende de mim.
7. T01 — correção de build (itens B1 e B2 de ARCHITECTURE.md):
   - `Assets/Systems/Analytics/AnalyticsManager.cs`: remover `using UnityEditor.Build;`.
   - `Assets/Systems/Navigation/Waypoint.cs`: envolver `using UnityEditor;` e todo o `OnDrawGizmos` em `#if UNITY_EDITOR`.
   - `Assets/Systems/Map Design/WaypointCreator.cs`: `using UnityEditor;` dentro de `#if UNITY_EDITOR`.
   - `Assets/Systems/Navigation/GPS.cs`: remover `using static UnityEngine.Rendering.DebugUI;`.
   - `Assets/Systems/Houses/InfoPanel.cs`: remover `using Unity.VisualScripting;`.
   Não mude mais nada nesses arquivos. Espere compilar e confira o console pelo MCP.
8. Gere um build Windows de desenvolvimento em `Builds/Windows/` (pasta já ignorada pelo git). Se o build falhar por outro código de Editor em runtime, corrija só o necessário e liste o que mudou. Commit: "fix: remove código de Editor do runtime".
9. T02 — build WebGL de fumaça em `Builds/WebGL/`. As configurações de importação removeram o override de WebGL dos sprites; não mexa nos .meta. Se o módulo WebGL não estiver instalado, pare e me avise. Se o build sair, sirva localmente, abra, e meça: tempo de carga, FPS parado e FPS com a câmera em movimento. Registre o resultado e uma recomendação go/no-go em `docs/jam/DECISIONS.md`.
10. Atualize o status de T00, T01 e T02 em `docs/jam/TASKS.md`, commit e push.
11. Relatório final curto: branches criadas, resultado do MCP, o que mudou no T01, resultado dos dois builds, hash do último commit.

Regras: não reescrever histórico; não alterar CameraController; não regerar a cidade; não salvar mudanças na cena nesta etapa.
```

---

## B1 — Fundação dos sistemas (Agente B, branch `jam-sistemas`)

```
Você é o Agente B (Sistemas) de uma game jam de 3 dias. Trabalha só na branch `jam-sistemas` e só nos arquivos do seu papel.

0. `git fetch`, `git switch jam-sistemas`, `git merge origin/jam`. Leia `CLAUDE.md` e, em `docs/jam/`: `GDD.md`, `ARCHITECTURE.md` (seções 3 a 6 inteiras), `AGENTS.md`, `TASKS.md`. Me diga se você tem um Unity aberto nesta pasta (para compilar) ou se trabalha só com código.

1. T03 — entregue primeiro e sozinho, porque destrava o Agente C:
   - `Assets/Systems/Masters/GameEvents.cs` exatamente com o contrato da seção 6.1 de ARCHITECTURE.md (eventos + um método `Raise...` por evento + limpeza em RuntimeInitializeOnLoadMethod(SubsystemRegistration)).
   - `GameManager` (singleton `Instance`, `GameState`, tempo de partida, `StartGame`, `Pause`, `Resume`, `Win`, `Lose`, `Restart`), mantendo os métodos estáticos `BlockInput`/`UnblockInput`.
   - `LocationsManager` (singleton; listas `Houses` só com casas comuns, `Guards`, `Warehouses`, preenchidas no Awake com FindObjectsByType; consultas `GetHousesInRadius`, `GetIntactHouses`, `GetTroubledHouses`). Atenção: Guard e Warehouse herdam de House; filtre por tipo.
   - `DisasterData` e `UpgradeData` podem ser criados já como ScriptableObjects mínimos em `Assets/SO/` para o contrato compilar.
   Commit, `git push`, marque T03 em TASKS.md e me avise para o Agente A integrar.

2. T04 — `House` e `GuardManager`: `SetState`, `ApplyProblem(State, int)`, eventos de chamado, correções B4 a B8 e B10. O fluxo atual de 3 passos tem que continuar funcionando igual.
3. T05 — `BalanceData` + `Stats`; tirar números do código; `ResourcesManager` dispara eventos e lê valores iniciais do BalanceData.
4. T06 — `DisasterManager` v1 com Terremoto (seção 5 do GDD).
5. T07 — `ChaosManager` + derrota + reinício (seção 6 do GDD).
6. T08 — Upgrades (seção 7 do GDD, exceto `automation`, `income` e `goal`, que são do Dia 2).
7. T09 — menu de Editor `Tools/Jam/Create Default Data` em `Assets/Systems/Masters/Editor/JamDataMenu.cs`, com os valores da tabela de upgrades do GDD.

Um commit por tarefa, push a cada tarefa, TASKS.md atualizado. Novos managers precisam de um GameObject na cena e de referências ligadas: você NÃO edita a cena; escreva em "Pedidos entre agentes" exatamente o que o Agente A deve criar e ligar (nome do objeto, componente, cada campo e para onde aponta). Prefira `Instance`, FindObjectsByType e Resources.Load a campos arrastados.

Itens [PENDENTE] do GDD não são implementados. Dúvida de jogabilidade: a decisão é do MEGABRAIN; registre em "Pedidos entre agentes" no TASKS.md e siga para a próxima tarefa desbloqueada.
```

---

## C1 — Fundação do feel (Agente C, branch `jam-feel`, PC com o Unity)

```
Você é o Agente C (Feel & UI) de uma game jam de 3 dias. Trabalha só na branch `jam-feel` e só nos arquivos do seu papel. O jogo precisa ser dinâmico: tudo que acontece tem resposta visual e sonora.

0. `git fetch`, `git switch jam-feel`, `git merge origin/jam`. Leia `CLAUDE.md` e, em `docs/jam/`: `GDD.md` (seção 10 inteira), `ARCHITECTURE.md` (seções 2, 4 e 6), `AGENTS.md`, `TASKS.md`. Verifique o MCP do Unity com uma chamada real.

1. T10 — `Assets/Systems/UI/Tweener.cs`: trocar os IDs globais de string por IDs por instância (`SetId(this)` e `DOTween.Kill(this)`/`Complete(this)`), mantendo a mesma sensação de hover e press. Teste em Play Mode passando o mouse por várias casas.

2. Enquanto o `GameEvents` do Agente B não chega na `jam`, faça o que não depende dele: T13 (`AudioManager` em `Assets/Systems/Audio/`: pool de AudioSources, `PlaySfx(AudioClip, volume, variação de pitch)`, música em 2 camadas com `SetTension(0..1)`), com sons provisórios. Liste a origem e a licença de cada som provisório em "Pedidos entre agentes" no TASKS.md (o organizador passa para o DECISIONS.md).

3. Quando eu avisar que o T03 foi integrado: `git merge origin/jam` e siga com:
   - T11 — `HudJuice` (contagem animada dos números em MoneyChanged/MaterialChanged/WorkersChanged, pulo do ícone) e `MoneyPopups` (prefab de texto "+N" que nasce na casa em CallResolved e voa até o contador de dinheiro).
   - T12 — `HouseFeedback` (escuta HouseStateChanged da própria casa: sacudida, fade de `_TextureLayer1Fade` de 0 a 1 em vez de troca seca, poeira, pop no conserto), marcador de chamado pulsando, `CameraShaker` (componente separado que aplica e desfaz um offset na Main Camera em DisasterStarted; NÃO altere CameraController.cs).

Regras de entrega: scripts em `Assets/Systems/Feel/`, `Audio/` e `UI/`; efeitos como prefabs NOVOS. Você não edita `Gameplay.unity` nem prefabs existentes (House.prefab etc.): para testar, use uma cena de teste sua em `Assets/Systems/Feel/Test/` ou mudanças locais não commitadas, e escreva em "Pedidos entre agentes" o que o Agente A deve adicionar a qual prefab/objeto e quais campos ligar. Efeitos precisam aparecer acima da cidade: o mundo é um Canvas World Space com Canvas aninhados (ordem 5000 a 10000), veja a seção 2 de ARCHITECTURE.md.

Um commit por tarefa, push a cada tarefa, TASKS.md atualizado. Dúvida de jogabilidade: a decisão é do MEGABRAIN; registre em "Pedidos entre agentes" no TASKS.md e siga para a próxima tarefa desbloqueada.
```

---

## A2 — Integração (Agente A, repetir a cada entrega de B ou C)

```
Integração na `jam`. Execute sozinho:
1. `git fetch`. `git merge --no-ff origin/jam-sistemas` e `git merge --no-ff origin/jam-feel`. Conflito em .unity/.prefab/.asset: pare e me mostre.
2. Espere compilar; console sem erros pelo MCP.
3. Leia "Pedidos entre agentes" em docs/jam/TASKS.md e execute os que são para você: criar GameObjects de managers em `Scripts/`, adicionar componentes a prefabs, ligar campos, rodar `Tools/Jam/Create Default Data` quando existir. Marque cada pedido atendido.
4. Entre em Play Mode, jogue o loop (gerar problema, despachar, coletar, comprar upgrade) e capture uma tela. Liste erros e comportamentos estranhos.
5. Salve a cena, commit ("feat: integra <o que entrou>"), push. Atualize TASKS.md.
6. Relatório curto: o que entrou, o que foi ligado na cena, bugs encontrados e para qual agente vão.
```
