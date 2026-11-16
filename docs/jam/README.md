# Defesa Civil Game — documentação da game jam

Incremental de gestão de desastres: terremotos, tempestades e incêndios quebram casas; o jogador envia trabalhadores para consertá-las, compra upgrades e segura o caos da cidade até cumprir a meta final.

**Prazo:** 3 dias (seg 16/11 → qui 19/11 de manhã; confirmar a hora exata da entrega).
**Regra de escopo:** escopo pequeno, muito polido. Tudo que não estiver em `TASKS.md` está fora.

## Leia nesta ordem

| Arquivo | Para quê |
|---|---|
| `MEGABRAIN.md` | Master prompt do MEGABRAIN, o agente que orquestra e tem a decisão final. Comece por aqui se você é ele. |
| `GDD.md` | O jogo: loop, desastres, caos, upgrades, automação. Marca o que é CONFIRMADO e o que é PROPOSTA. |
| `ARCHITECTURE.md` | Como o código está hoje, convenções de estilo, bugs conhecidos e a arquitetura-alvo (contratos entre sistemas). |
| `AGENTS.md` | Os 3 agentes, branches, quem é dono de quais arquivos, fluxo de git. |
| `TASKS.md` | Backlog por dia, com agente responsável e critério de pronto. |
| `PROMPTS.md` | Prompts originais do organizador (A1, A2, B1, C1). B1 e C1 foram substituídos. |
| `PROMPTS_PRONTOS.md` | **Prompts em uso**, revisados pelo MEGABRAIN: B, C, o do MEGABRAIN no Unity, e a pasta do B (`git worktree`). |
| `STATUS_B.md`, `STATUS_C.md` | Status, pedidos e dúvidas de cada agente para o MEGABRAIN. |
| `ART_REQUESTS.md` | Lista de arte e áudio a produzir fora do projeto. |
| `DECISIONS.md` | Registro de decisões da jam (data, quem decidiu). |
| `CLAUDE-jam.md` | Novo `CLAUDE.md`. O kickoff copia para a raiz **dentro da branch `jam`**. |

## Papéis

- **Organizador (chat do Project "Defesa Civil Game"):** escreveu estes docs e segue como consultor de design. Desde 16/11 quem mantém os docs, quebra tarefas, escreve prompts e decide o balanceamento é o MEGABRAIN.
- **MEGABRAIN** (orquestra, delega e é o antigo "Agente A": cena, integração, builds), **Agente B — Sistemas**, **Agente C — Feel & UI:** ver `AGENTS.md`. O MEGABRAIN não é este chat de design.

## Docs antigos (gerador de cidade)

`docs/city-rules.md`, `inventory.md`, `plan-citygen.md`, `audit-meta.md`, `reports/` e o `docs/decisions.md` descrevem a etapa anterior (geração da cidade por JSON). Continuam válidos **só** para quem for regerar a cidade. Para a jam, a cidade está pronta e não deve ser regerada sem pedido do usuário.
