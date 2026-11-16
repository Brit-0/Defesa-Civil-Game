# Agentes, branches e donos de arquivos

Contexto confirmado pelo usuário: **3 assinaturas (3 agentes), 2 PCs, mesmo repositório, branches separadas.**

## Atualização de 16/11 — MEGABRAIN

Há 3 assinaturas no total e uma delas é do **MEGABRAIN**, o agente que organiza e delega tudo. Ele **não é este chat de design**. Para fechar 3 assinaturas, **o papel de "Agente A" passa a ser do MEGABRAIN**:

| Agente | Assinatura | Papel |
|---|---|---|
| **MEGABRAIN** (= antigo A) | 1 | Orquestra e delega (lê `TASKS.md`, distribui, cobra), **e** é o único dono da cena e dos prefabs existentes: integra as branches, liga referências, importa arte, faz builds. Branch `jam`. |
| **B — Sistemas** | 2 | Igual ao texto abaixo. Branch `jam-sistemas`. |
| **C — Feel & UI** | 3 | Igual ao texto abaixo. Branch `jam-feel`. |

Onde o texto abaixo diz "Agente A", leia **MEGABRAIN**. Os prompts `A1` e `A2` de `PROMPTS.md` são os de kickoff e integração do MEGABRAIN. Distribuição por PC mantida: **MEGABRAIN + B no PC 1** (B numa segunda pasta via `git worktree`), **C no PC 2**.

Risco a vigiar: o MEGABRAIN acumula orquestração e integração, que são as tarefas que travam os outros dois. Regra: **toda integração vem antes de delegar tarefa nova**, e ele não escreve lógica de jogo nem juice (isso é de B e C).

## Por que a divisão é assim

`Gameplay.unity` é um YAML de 2 MB e os prefabs também são YAML com IDs internos. Dois agentes editando a mesma cena em branches diferentes geram conflitos que não dá para resolver à mão em tempo de jam. Por isso: **um único dono da cena**, e os outros dois entregam código e prefabs novos.

## Os três agentes

| Agente | Papel | Branch | Precisa de Unity? |
|---|---|---|---|
| **A — Integrador/Cena** | Dono da cena e dos prefabs existentes. Faz merge das outras branches, liga referências, roda os menus de dados, importa arte, faz builds e playtest. | `jam` | Sim, com MCP do Unity. |
| **B — Sistemas** | Lógica de jogo em C#: eventos, desastres, caos, upgrades, economia, automação, correções nos scripts existentes. | `jam-sistemas` | Recomendado para compilar; não salva cena. |
| **C — Feel & UI** | Tudo que o jogador vê e ouve em resposta: juice, VFX, áudio, HUD, telas. Entrega prefabs novos. | `jam-feel` | Sim (cria prefabs e testa visual). |

### Distribuição nos 2 PCs (recomendação; confirmar em P7)

- **PC 1:** Agente A com o Unity aberto na pasta principal + Agente B numa segunda pasta (`git worktree`), só código. A compila o trabalho de B a cada merge.
- **PC 2:** Agente C com o Unity aberto no seu clone.

Dois agentes no mesmo PC **nunca** compartilham a mesma pasta em branches diferentes. Criar a segunda pasta assim, a partir da pasta principal:

```
git worktree add "../Defesa-Civil-Game-sistemas" jam-sistemas
```

Se B também precisar compilar, abre um segundo Unity apontando para essa pasta (a primeira importação demora alguns minutos).

## Donos de arquivos

Regra: **só edite o que é seu.** Precisa de mudança em arquivo de outro dono? Anote em `TASKS.md` na seção "Pedidos entre agentes" e siga com outra tarefa.

| Caminho | Dono |
|---|---|
| `Assets/Scenes/**` | A |
| Prefabs que já existem: `Assets/Systems/Houses/*.prefab`, `Resources/Worker.prefab`, `Upgrades/UpgradeButton.prefab`, `Navigation/Waypoint.prefab` | A |
| `ProjectSettings/**`, `Packages/**`, `Assets/Settings/**` | A |
| `Assets/_Project/**` (gerador de cidade) | A, e só com pedido do usuário |
| `Assets/Arts/**` (importação de arte recebida) | A |
| `Assets/Resources/Upgrades|Disasters|Balance/**` (assets gerados pelo menu) | A roda o menu; B define o conteúdo |
| `Assets/Systems/**/*.cs` de gameplay: `Houses`, `Guard`, `Navigation`, `Resources`, `Masters`, `Disasters`, `Chaos`, `Upgrades`, `Analytics`, `Eletricity` | B |
| `Assets/SO/**` | B |
| `Assets/Systems/Feel/**`, `Assets/Systems/Audio/**` | C |
| `Assets/Systems/UI/**`, `Assets/Systems/Feedbacks/**` | C |
| `Assets/Audio/**` | C |
| Prefabs novos de UI/VFX (em `Systems/UI/` e `Systems/Feel/`) | C |
| `Assets/Systems/Camera/CameraController.cs` | Ninguém, sem pedido do usuário |
| `docs/jam/**` | Organizador. Agentes só editam `TASKS.md` (status e pedidos). |
| `CLAUDE.md` | A no kickoff; depois, só o organizador pede mudanças |

Exceções combinadas:
- **T01** (correção de build) é feita pelo A no kickoff, antes de B começar, mesmo tocando em `.cs` de B.
- Componentes novos de C que precisam ficar dentro de `House.prefab` (ex.: `HouseFeedback`): C escreve o script, A adiciona ao prefab.

## Fluxo de git

- `main` não recebe nada até o fim da jam. `city-gen` fica como está.
- `jam` é a branch de integração e sempre deve compilar e rodar.
- B e C trabalham em `jam-sistemas` e `jam-feel`.

Ciclo de cada agente (B e C):

1. Antes de começar uma tarefa: `git fetch` e `git merge origin/jam`.
2. Trabalhar, com commits pequenos em português: `feat: ...`, `fix: ...`, `docs: ...`, `chore: ...`.
3. Ao terminar a tarefa: `git push`, marcar em `TASKS.md` e avisar que está pronto para integrar.

Ciclo do A:

1. `git fetch`, depois `git merge --no-ff origin/jam-sistemas` e `git merge --no-ff origin/jam-feel`.
2. Esperar compilar, conferir o console pelo MCP, ligar o que precisar na cena, entrar em Play e testar.
3. Commit e `git push` na `jam`.

Regras fixas:
- **Nunca reescrever histórico** (sem rebase, sem force push, sem amend em commit publicado).
- `git add` só dos caminhos da tarefa; nunca `git add .` às cegas. Sempre incluir os `.meta` dos arquivos novos.
- Não commitar `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj` (já estão no `.gitignore`).
- Conflito em `.unity`, `.prefab` ou `.asset`: **parar e avisar**. Não resolver à mão.
- Integrar cedo e sempre: pelo menos uma integração na `jam` a cada 2 a 3 horas de trabalho.

## Regra de dúvidas

**A decisão final é sempre do MEGABRAIN** (decisão do usuário em 16/11). Dúvida de B ou C que não esteja respondida no `GDD.md`: registrar em "Pedidos entre agentes" no `TASKS.md`, endereçada ao MEGABRAIN, e seguir para a próxima tarefa desbloqueada. O MEGABRAIN decide sem pedir aprovação, registra em `DECISIONS.md` e só aciona o usuário para o que exige um humano. Itens [PROPOSTA] do GDD podem ser implementados como estão.

## Definição de pronto

- Compila sem erros e sem warnings novos no console.
- Testado em Play Mode (A e C) ou coberto por teste/log claro (B sem Unity).
- Nenhum número de balanceamento no código.
- Nada de `UnityEditor` em runtime.
- `TASKS.md` atualizado e push feito.
