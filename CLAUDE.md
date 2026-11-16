# Projeto: Defesa Civil Game — incremental de desastres (game jam, Unity)

## O jogo
Desastres (terremoto, tempestade, incêndio) quebram casas. O jogador despacha trabalhadores, coleta dinheiro, compra upgrades e automatiza. Caos cheio = derrota; objetivo final comprado = vitória. Prazo: 3 dias. Escopo pequeno, muito polido.

## Leia antes de trabalhar
- `docs/jam/GDD.md` — regras do jogo. [CONFIRMADO] e [PROPOSTA] podem ser implementados; **[PENDENTE] não**.
- `docs/jam/ARCHITECTURE.md` — mapa do código, convenções, bugs conhecidos, contratos (`GameEvents`).
- `docs/jam/AGENTS.md` — seu papel, sua branch, **quais arquivos você pode editar**.
- `docs/jam/TASKS.md` — sua tarefa e o critério de pronto.

## Contexto técnico
- Unity 6000.6.7f1, URP 2D, Input System, C# 9. Alvos: Windows e WebGL.
- Cena única: `Assets/Scenes/Gameplay.unity`. O mundo é um Canvas World Space: casas, árvores e trabalhadores são `Image` de UI. HUD em `UI/UICanvas`.
- Disponíveis: DOTween, Feel (MMFeedbacks), Odin, Text Animator, Easy Save 3, Sprite Shaders Ultimate.
- Pixel art: PPU 32, Point, sem compressão.

## Regras
1. **A decisão final é sempre do MEGABRAIN** (o agente da branch `jam`). Agentes B e C: dúvida de jogabilidade ou de sistema vai para ele, registrada em "Pedidos entre agentes" no `TASKS.md`; não deduza, e siga para a próxima tarefa desbloqueada enquanto espera. O MEGABRAIN decide e registra em `docs/jam/DECISIONS.md`.
2. **Só edite arquivos do seu papel** (`AGENTS.md`). Somente o Agente A edita `Gameplay.unity` e prefabs existentes. Precisa de algo de outro dono? Registre em "Pedidos entre agentes" no `TASKS.md`.
3. **Git:** trabalhe só na sua branch (`jam`, `jam-sistemas` ou `jam-feel`). Commits pequenos em português. Nunca reescreva histórico. `git add` só dos caminhos da tarefa, com os `.meta` (sem o Unity aberto, escreva o `.meta` à mão: `ARCHITECTURE.md`, seção 6.4.1). Conflito em `.unity`/`.prefab`/`.asset`: pare e avise.
4. **Sem números de balanceamento no código.** Valores ficam em `BalanceData` e nos ScriptableObjects.
5. **Sistemas disparam `GameEvents`; apresentação (feel, áudio, HUD) só escuta.** Não chame efeitos de dentro da lógica.
6. **Nada de `UnityEditor` em runtime.** Código de Editor em pasta `Editor/` ou `#if UNITY_EDITOR`. Nada de threads ou disco (WebGL).
7. Não alterar `CameraController.cs` nem regerar a cidade (`Tools/City/*`, objetos `[CG] ...`) sem pedido do usuário.
8. Execute sozinho o que estiver ao alcance (git, MCP do Unity, compilar, Play Mode, capturas). Ao terminar: console sem erros, `TASKS.md` atualizado, push.
9. Tudo que o jogador faz precisa de resposta visual e sonora. Se uma feature entra sem feedback, ela não está pronta.

## Convenções de código
- Sem namespace no gameplay. Pastas `Assets/Systems/<Sistema>/` com scripts e prefabs juntos.
- Managers: `MonoBehaviour` com `public static X Instance;` atribuído no `Awake`.
- `[Header("MAIÚSCULAS")]`, `[SerializeField] private`, sufixos `Lbl` e `Pf`, Odin `[Button]` para debug.
- Coroutines para fluxo, DOTween para animação (sempre `SetId(this)` ou `SetLink(gameObject)`).
- Identificadores em inglês, textos de UI em português.
- ScriptableObjects: classe em `Assets/SO/`, assets em `Assets/Resources/<Tipo>/`.
