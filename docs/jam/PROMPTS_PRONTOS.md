# Prompts prontos para colar (revisados pelo MEGABRAIN em 16/11)

Substitui os prompts B1 e C1 de `PROMPTS.md`. Cada prompt é o bloco inteiro dentro da moldura: copie tudo e cole na sessão indicada.

## 1. Ordem e onde colar

| Ordem | O quê | Onde | Precisa do Unity? |
|---|---|---|---|
| 1 | Criar a pasta do B (seção 2) | Qualquer PC com o repositório, na pasta principal | Não |
| 2 | **Prompt B** (seção 3) | Terminal na pasta `Defesa-Civil-Game-sistemas`, comando `claude` | Não. Pode começar agora. |
| 3 | **Prompt C** (seção 4) | PC 2, terminal na pasta do clone, com o Unity 6000.6.7f1 aberto no projeto | Sim |
| 4 | **Prompt do MEGABRAIN no Unity** (seção 5) | PC onde o Unity está aberto, terminal na pasta principal, branch `jam` | Sim |

B e C não dependem um do outro para começar, e nenhum dos dois depende do passo 4. O passo 4 é o que falta do kickoff e deve rodar assim que o PC com o Unity estiver livre, porque é lá que o trabalho do B é compilado pela primeira vez.

## 2. Pasta do Agente B (`git worktree`)

Dois agentes no mesmo PC nunca usam a mesma pasta. O `git worktree` cria uma segunda pasta do mesmo repositório, presa à branch `jam-sistemas`, sem baixar tudo de novo.

No PowerShell, a partir da pasta principal do projeto (a que está na branch `jam`):

```powershell
git fetch
git worktree add "../Defesa-Civil-Game-sistemas" jam-sistemas
cd ..\Defesa-Civil-Game-sistemas
git status
```

O `git status` tem que responder `On branch jam-sistemas`. Depois, nessa pasta, rode `claude` e cole o prompt da seção 3.

- Se o git responder que `jam-sistemas` não existe (PC que só tem o clone, sem a branch local): `git worktree add --track -b jam-sistemas "../Defesa-Civil-Game-sistemas" origin/jam-sistemas`.
- Não abra o Unity nessa pasta. O B só escreve código; quem compila é o MEGABRAIN, na pasta principal.
- A pasta principal não consegue trocar para `jam-sistemas` enquanto o worktree existir. É o esperado.
- Para apagar no fim da jam: `git worktree remove "../Defesa-Civil-Game-sistemas"`, a partir da pasta principal.

## 3. Prompt B — Sistemas (branch `jam-sistemas`, sem Unity)

````
Você é o Agente B (Sistemas) da game jam "Defesa Civil Game" (Unity 6000.6.7f1, entrega na quarta 18/11). Você escreve a lógica do jogo em C#, só na branch `jam-sistemas`, e trabalha SEM o Unity aberto: você não compila nem testa em Play. Quem compila, monta a cena e integra é o MEGABRAIN, na branch `jam`. A decisão final é sempre dele. Execute tudo sozinho, na ordem, sem me pedir aprovação.

PASSO 0 — sincronizar e ler
- `git fetch`, depois `git status` (tem que estar em `jam-sistemas`; se não estiver, `git switch jam-sistemas`), depois `git merge origin/jam`.
- Leia inteiros: `CLAUDE.md`, `docs/jam/GDD.md`, `docs/jam/ARCHITECTURE.md`, `docs/jam/AGENTS.md`, `docs/jam/TASKS.md`. Em `docs/jam/DECISIONS.md`, leia a seção "Revisão do MEGABRAIN".
- Leia o código antes de mexer: tudo em `Assets/Systems/Houses/`, `Guard/GuardManager.cs`, `Navigation/GPS.cs`, `Resources/ResourcesManager.cs`, `Masters/GameManager.cs`.

REGRAS QUE VALEM PARA TODAS AS TAREFAS
1. Você não compila, então um erro seu trava os outros dois agentes. Use só C# 9 e APIs do Unity 6 que você tem certeza que existem. Antes de cada push, releia o diff inteiro procurando erro de compilação: `using` faltando, nome de método trocado, tipo errado, chave sem fechar.
2. Todo arquivo e toda pasta que você criar leva um `.meta` escrito à mão, no mesmo commit (modelo na seção 6.4.1 de ARCHITECTURE.md). GUID novo por arquivo: `[guid]::NewGuid().ToString('N')` no PowerShell. Nunca reaproveite nem troque um GUID depois do push. Não crie `.asset`, `.prefab` nem `.unity`.
3. Pode editar: `Assets/Systems/{Houses,Guard,Navigation,Resources,Masters,Disasters,Chaos,Upgrades,Analytics,Eletricity}/**/*.cs`, `Assets/SO/**` e `docs/jam/STATUS_B.md`. Não pode editar: cenas, prefabs, `ProjectSettings`, `Packages`, `Assets/Systems/{Feel,Audio,UI,Feedbacks,Camera}/**`, `Assets/_Project/**`, nem os outros arquivos de `docs/jam/` (o `TASKS.md` é só do MEGABRAIN).
4. Nenhum número de balanceamento no código. Tudo vem do `BalanceData` e dos ScriptableObjects. Os valores padrão dos campos do `BalanceData` são os da seção 13 do GDD; valor padrão de campo de ScriptableObject não conta como número no código.
5. Lógica dispara `GameEvents` e nunca chama efeito, som ou animação nova. O que o código atual já faz de visual pode ficar.
6. WebGL: nada de `System.Threading`, `System.IO` em disco ou `UnityEditor` fora de pasta `Editor/` ou de `#if UNITY_EDITOR`.
7. A cena guarda `House.currentState` como número: nunca reordene nem remova valores do enum `State`. Não renomeie campos serializados que já existem (`workTime`, `sprite`, `entry` etc.): a cena perde a referência. Se precisar mesmo, use `[FormerlySerializedAs]`.
8. Você não edita a cena. Manager novo tem que funcionar só de o MEGABRAIN criar um GameObject e adicionar o componente, sem arrastar nada: use `Instance`, `FindObjectsByType` e `Resources.Load`. Se um campo arrastado for inevitável, o código tem que rodar sem erro com o campo vazio, e você escreve o pedido em `docs/jam/STATUS_B.md`, na seção "Pedidos ao MEGABRAIN": nome do objeto, componente, cada campo e para onde aponta.
9. Estilo do código existente: sem namespace, `public static X Instance` atribuído no `Awake`, `[Header("MAIÚSCULAS")]`, `[SerializeField] private`, coroutines para fluxo, DOTween com `SetLink(gameObject)`, identificadores em inglês, textos em português.
10. Dúvida de jogabilidade que o GDD não responde: escreva em `STATUS_B.md`, em "Dúvidas para o MEGABRAIN", junto com o que você assumiu, e siga para a próxima tarefa. Não pare para esperar.
11. Um commit por tarefa, em português (`feat:`, `fix:`), com `git add` só dos seus caminhos e dos `.meta`. `git push` a cada tarefa. Marque a tarefa em `STATUS_B.md` com o hash. Nunca rebase, amend em commit publicado nem force push.

TAREFAS DO DIA 1, NESTA ORDEM

T03 — contrato (entregue primeiro e sozinho: destrava o Agente C)
- `Assets/Systems/Masters/GameEvents.cs` exatamente como a seção 6.1 de ARCHITECTURE.md: os enums `GameState` e `DeniedReason`, todos os eventos, um método `Raise...` por evento, e a limpeza de todos os eventos em `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- `Assets/SO/UpgradeData.cs` e `Assets/SO/DisasterData.cs` (com o enum `DisasterType`), já com os membros que a tabela "o que a apresentação pode ler" da seção 6.1 lista.
- `GameManager` (em `Masters/GameManager.cs`, mantendo `BlockInput`/`UnblockInput` estáticos): `Instance`, `State`, `ElapsedTime` (só conta em `Playing`), `HousesRepaired`, `TotalMoneyEarned`, `StartGame()`, `Pause()`, `Resume()`, `Win()`, `Lose()`, `Restart()`. Pausa com `Time.timeScale = 0`. `Restart` mata os tweens (`DOTween.KillAll()`), volta `Time.timeScale` a 1, zera os estáticos de jogo (`GuardManager.selectBaseMode`) e recarrega a cena. Campo `autoStart` (padrão ligado): enquanto o menu do Agente C não existe, a partida entra em `Playing` sozinha no `Start`. Botão de debug do Odin que alterna o tempo em 4×, dentro de `#if UNITY_EDITOR`.
- `LocationsManager` (em `Houses/LocationsManager.cs`): `Instance`; listas `Houses` (só casas comuns), `Guards`, `Warehouses`, preenchidas no `Awake` com `FindObjectsByType<House>`. Atenção: `CityGuard` e `Warehouse` herdam de `House`; separe por tipo. Consultas: `GetHousesInRadius(Vector3, float)`, `GetIntactHouses()`, `GetTroubledHouses()`, `GetNearestGuard(Vector3)`.
- `Masters/GameEventsLogger.cs`: componente opcional de debug que assina todos os eventos e faz `Debug.Log` de cada um.
- Commit, push, marque em STATUS_B.md e peça lá a integração imediata.
Pronto quando: os arquivos estão no remoto com `.meta`, e nenhum código existente mudou de comportamento.

T04 — `House` e `GuardManager`
- `House.SetState(State)` é o único lugar que muda `currentState`, e dispara `HouseStateChanged`. `House.ApplyProblem(State problema, int severidade)` substitui o sorteio de `GenerateCall`: só age em casa comum e `Intact`; cria o chamado e mostra o marcador. O botão `GenerateCall` do Odin continua existindo e chama `ApplyProblem(State.Damaged, 1 a 3)`.
- Campo público `externalFeedback` em `House`: quando ligado (o `HouseFeedback` do Agente C liga), `House` não mexe mais em `_TextureLayer1Fade`.
- Envio (GDD, seção 3): material (`damageAmount`) e trabalhadores (a equipe inteira) saem do estoque no momento do envio. Envio negado dispara `ActionDenied` com o motivo e a posição da casa, em vez de sair em silêncio. Na coleta a casa sempre é consertada.
- Tempo de trabalho e recompensa pela equipe e pelo dano (GDD 3.1). Nesta tarefa pode ler os números de campos provisórios; no T05 eles passam para `Stats`.
- Correções (seção 5 de ARCHITECTURE.md): B4, B5 (inclusive `CancelCall` só em `context.performed`), B6, B7, B8, B10 (caminho nulo: log de erro, devolve equipe e material, libera a casa), B14, B15, B16, B17.
- Eventos: `CallOpened`, `TeamDispatched`, `WorkStarted`, `WorkCompleted`, `CallResolved`, e em `GPS` `WorkerSpawned` e `WorkerArrived`.
- O `GuardManager` continua chamando `FeedbacksManager.ShowText`/`HideText` como hoje.
Pronto quando: o fluxo de 3 passos se comporta como antes para um chamado, e dois chamados montados em seguida não travam casa nem deixam o estoque negativo (confira lendo o código, passo a passo, e descreva em STATUS_B.md como o MEGABRAIN testa no Play).

T05 — `BalanceData` + `Stats`
- `Assets/SO/BalanceData.cs` com os campos da seção 13 do GDD (os nomes de lá são sugestão) e esses valores como padrão. `Stats` (estático, em `Masters/`) carrega `Resources.Load<BalanceData>("Balance/BalanceData")` e, se o asset ainda não existir, usa `ScriptableObject.CreateInstance<BalanceData>()` com um aviso no log. `Stats` devolve os valores já com o efeito dos upgrades: `WorkerSpeed`, `GetWorkTime(Call)`, `GetReward(Call)`, `MaterialPerWorker`, `AutomationLevel` etc.
- Tire os números do código: `Workers = 3`, material inicial, `workTime`, `speed`, `moneyPerCall`, o `0.8f` do envio. Os setters do `ResourcesManager` disparam `MoneyChanged`, `MaterialChanged`, `WorkersChanged`.
- Comece aqui o menu do T09: `Assets/Systems/Masters/Editor/JamDataMenu.cs`, com `Tools/Jam/Create Default Data` criando (ou atualizando, sem duplicar) `Assets/Resources/Balance/BalanceData.asset`. A cada tarefa seguinte que tiver dados, você aumenta esse menu.
Pronto quando: não sobra número de balanceamento em `Assets/Systems/**/*.cs` (confira com grep por literais numéricos nos arquivos que você tocou).

T06 — `DisasterManager` v1, Terremoto (GDD, seções 5 e 13)
- `Assets/Systems/Disasters/DisasterManager.cs`: só roda em `GameState.Playing`; primeiro desastre em `firstDisasterDelay`, terremoto fraco perto da Guarda; depois intervalo e número de casas pela rampa; aviso (`DisasterWarning`) `warningTime` segundos antes; `DisasterStarted` com o epicentro; cada casa do raio recebe `ApplyProblem` com atraso proporcional à distância (a onda) e dano 3, 2 ou 1 conforme a distância; `DisasterEnded` no fim. Guarda e Armazéns nunca são afetados. Tipos carregados com `Resources.LoadAll<DisasterData>("Disasters")`; sem assets, avisa no log e não faz nada.
- O menu `Tools/Jam/Create Default Data` passa a criar o `DisasterData` do terremoto.
Pronto quando: lendo o código, um terremoto por vez acontece sozinho e a rampa aumenta frequência e tamanho.

T07 — `ChaosManager` + derrota (GDD, seção 6)
- `Assets/Systems/Chaos/ChaosManager.cs`: `Instance`, `Chaos` (0 a 1); alvo pela soma dos pesos das casas com problema dividido pelo limite; sobe e desce nas velocidades do `BalanceData`; dispara `ChaosChanged` quando muda; em 1 chama `GameManager.Instance.Lose()`.
Pronto quando: ignorar chamados leva a `GameState.Lost`, e `Restart()` volta ao começo.

T08 — Upgrades (GDD, seção 7): `workers`, `speed`, `tools`, `cart`, `reward`
- `Assets/Systems/Upgrades/UpgradesManager.cs`: `Instance`, nível por upgrade, `GetLevel`, `GetCost` (`baseCost × costGrowth^nível`, arredondado), `CanBuy`, `TryBuy` (debita, sobe o nível, dispara `UpgradePurchased`; negado dispara `ActionDenied` com `NoMoney` ou `MaxLevel`). Carrega `Resources.LoadAll<UpgradeData>("Upgrades")` e preenche a aba em runtime instanciando o prefab do botão.
- `Assets/Systems/Upgrades/UpgradeButton.cs`: mostra ícone, nome, nível e custo; desabilita sem dinheiro ou no nível máximo; clique chama `TryBuy`. O prefab `UpgradeButton.prefab` é do MEGABRAIN: escreva em STATUS_B.md quais campos ele liga, e para onde.
- `Stats` passa a aplicar os efeitos. `workers` soma 1 trabalhador na hora.
- O menu de dados passa a criar os 5 `UpgradeData` com a tabela do GDD.
Pronto quando: lendo o código, comprar muda o valor que `Stats` devolve e o preço seguinte sobe.

T09 — fechar o menu `Tools/Jam/Create Default Data`
- Rodar duas vezes não duplica nem apaga o que foi ajustado à mão em campos que o menu não conhece. Escreva em STATUS_B.md a lista dos assets que ele cria, para o MEGABRAIN conferir.

DIA 2 (comece assim que o Dia 1 estiver no remoto, sem esperar prompt novo), nesta ordem:
T24 (upgrade `goal` + `Win()`), T23 (Repasse da Prefeitura, upgrade `income`, `PassiveIncomePaid`), T22 níveis 1 a 3 da automação, T21 (incêndio: `FireTimeLeft01`, temporizador, vira `Damaged` 3), T20 (tempestade, `LightningStruck`, liberada aos 90 s; incêndio isolado aos 180 s), T22 nível 4. Antes de cada uma: `git fetch` e `git merge origin/jam`, e leia em `docs/jam/TASKS.md` a seção "Pedidos entre agentes", onde o MEGABRAIN devolve erros de compilação e bugs do playtest: esses vêm antes de tarefa nova.

AO TERMINAR CADA TAREFA: commit, push, STATUS_B.md atualizado. No fim da sessão, um relatório curto: tarefas entregues com hash, pedidos de cena que você deixou, dúvidas e o que assumiu.
````

## 4. Prompt C — Feel & UI (branch `jam-feel`, PC 2, com Unity)

Antes de colar, no PC 2: clonar o repositório (`git clone https://github.com/Brit-0/Defesa-Civil-Game`), abrir a pasta no Unity Hub com o **6000.6.7f1** (a primeira importação leva alguns minutos) e deixar o Editor aberto. Depois, terminal na pasta do clone, `claude`, e colar:

````
Você é o Agente C (Feel & UI) da game jam "Defesa Civil Game" (Unity 6000.6.7f1, entrega na quarta 18/11). Você faz tudo que o jogador vê e ouve em resposta ao jogo: juice, VFX, áudio, HUD e telas. Trabalha só na branch `jam-feel`, com o Unity aberto nesta pasta. A exigência do dono do jogo é: jogo dinâmico, com muita animação e movimento; toda ação tem resposta visual e sonora. Quem monta a cena e integra é o MEGABRAIN, na branch `jam`, e a decisão final é sempre dele. Execute tudo sozinho, na ordem, sem me pedir aprovação.

PASSO 0 — sincronizar e ler
- `git fetch`, `git switch jam-feel`, `git merge origin/jam`.
- Leia inteiros: `CLAUDE.md`, `docs/jam/GDD.md`, `docs/jam/ARCHITECTURE.md`, `docs/jam/AGENTS.md`, `docs/jam/TASKS.md`, `docs/jam/ART_REQUESTS.md`. Em `docs/jam/DECISIONS.md`, leia a seção "Revisão do MEGABRAIN".
- Confira o MCP do Unity com uma chamada de verdade (nome da cena aberta e filhos do GameObject `Scripts`). Se não responder, diagnostique e me diga exatamente o que eu preciso fazer.
- Olhe as imagens de `docs/reports/build/v3_*.png` para ver a cidade e o HUD de hoje.

REGRAS QUE VALEM PARA TODAS AS TAREFAS
1. Pode editar: `Assets/Systems/{Feel,Audio,UI,Feedbacks}/**`, `Assets/Audio/**`, `docs/jam/STATUS_C.md` e capturas em `docs/jam/reference/feel/`. Não pode commitar mudança em: `Assets/Scenes/Gameplay.unity`, prefabs que já existem (`House.prefab`, `Guard.prefab`, `Warehouse.prefab`, `Worker.prefab`, `UpgradeButton.prefab`, `Waypoint.prefab`), `ProjectSettings`, `Packages`, scripts de `Assets/Systems/{Houses,Guard,Navigation,Resources,Masters,Disasters,Chaos,Upgrades}/**`, `CameraController.cs`, nem os outros arquivos de `docs/jam/` (o `TASKS.md` é só do MEGABRAIN).
2. Para testar, use uma cena sua em `Assets/Systems/Feel/Test/` ou mudanças locais na cena e nos prefabs que você NÃO commita. Antes de cada commit rode `git status` e confira que só entram arquivos seus. Se o Unity alterou sozinho um arquivo que não é seu, deixe fora do commit e anote em STATUS_C.md.
3. Você só escuta `GameEvents` e lê o que a tabela "o que a apresentação pode ler" (seção 6.1 de ARCHITECTURE.md) lista. Nunca mude regra de jogo. Assine em `OnEnable` e desassine em `OnDisable`.
4. Entregue prefabs novos que funcionem só de o MEGABRAIN soltar na cena ou adicionar o componente a um prefab: ache o que precisa por `Instance`, `FindObjectsByType` ou `Resources.Load`. Para cada entrega escreva em `docs/jam/STATUS_C.md`, em "Pedidos ao MEGABRAIN": em qual objeto ou prefab entra, e cada campo que precisa ser ligado e para onde aponta.
5. O mundo é um Canvas World Space com cerca de 300 Canvas aninhados (ordem 5000 por Y, overlays em 10000). Efeito que precisa aparecer acima da cidade usa `sortingOrder` de 10000 para cima, ou vai no `UICanvas`. Confira em Play que aparece mesmo.
6. DOTween sempre com `SetLink(gameObject)` (ou `SetId(this)`), para não sobrar tween quando a cena reinicia. O que anima com o jogo pausado (menu, pausa, telas finais) usa `SetUpdate(true)`, porque a pausa é `Time.timeScale = 0`.
7. Sem `UnityEditor` em runtime, sem threads, sem disco (WebGL). No WebGL o áudio só toca depois do primeiro clique.
8. Textos em português, fonte `ModernDOS` (já tem os acentos). Pixel art: PPU 32, filtro Point, sem compressão. Não instale pacotes novos. DOTween é o padrão; Feel (MMFeedbacks) e Text Animator já estão no projeto e podem ser usados.
9. Nada de número de regra de jogo no seu código. Tempos e intensidades de animação são seus: deixe em campos `[SerializeField]` com `[Header]`, para ajustar no Inspector.
10. Dúvida que o GDD não responde: escreva em STATUS_C.md, em "Dúvidas para o MEGABRAIN", junto com o que você assumiu, e siga para a próxima tarefa. Não pare para esperar.
11. Um commit por tarefa, em português (`feat:`, `fix:`), com os `.meta`. `git push` a cada tarefa. Marque a tarefa em STATUS_C.md com o hash. Nunca rebase, amend em commit publicado nem force push. Conflito em `.unity`, `.prefab` ou `.asset`: pare e me avise.
12. Para cada efeito pronto, salve uma captura pequena do Play em `docs/jam/reference/feel/` (ex.: `T12_terremoto.png`). O MEGABRAIN revisa por elas.

TAREFAS DO DIA 1

Não dependem do Agente B. Comece por elas:

T10 — `Assets/Systems/UI/Tweener.cs`
Hoje os tweens usam IDs globais de texto (`"HoverEnter"`, `"HoverExit"`, `"TweenHold"`), então `DOTween.Complete(id)` numa casa completa os tweens de todas, e `Hold` completa um ID que não existe (`"TweenHover"`). Troque por tweens da própria instância (mate ou complete só os do próprio `transform`), mantendo a sensação atual de hover e de clique. Não mude os nomes dos métodos públicos: `House` os chama.
Pronto quando: em Play, passar o mouse rápido por várias casas não faz nenhuma outra pular nem travar em escala errada.

T13 — `AudioManager` em `Assets/Systems/Audio/`
Pool de AudioSources, `PlaySfx(AudioClip, volume, variação de pitch)`, música em 2 camadas com `SetTension(0..1)` e crossfade. Entregue como prefab `AudioManager.prefab` que funciona só de soltar na cena. Sons provisórios **só de licença CC0** (por exemplo os pacotes de áudio da Kenney), em `Assets/Audio/`, já com os nomes finais de ART_REQUESTS.md (`Sfx_Click.wav`, `Sfx_Dispatch.wav`, `Sfx_Hammer.wav`, `Sfx_Coin.wav`, `Sfx_Buy.wav`, `Sfx_Error.wav`, `Sfx_Siren.wav`, `Sfx_Earthquake.wav`), para o som final entrar só trocando o arquivo. Liste origem e licença de cada um em STATUS_C.md, em "Sons provisórios".
Pronto quando: numa cena de teste, cada som toca pelo `PlaySfx` e a música cruza com `SetTension`.

Dependem do contrato do Agente B. A cada tarefa terminada rode `git fetch` e `git merge origin/jam`; quando `Assets/Systems/Masters/GameEvents.cs` aparecer, elas estão liberadas (não espere eu avisar):

T11 — HUD
`HudJuice`: contagem animada dos números em `MoneyChanged`, `MaterialChanged` e `WorkersChanged`, e pulo do ícone. `MoneyPopups`: prefab de texto "+N" que nasce na casa em `CallResolved` e voa até o contador de dinheiro. Ligue os sons do T13 aos eventos (`TeamDispatched`, `WorkStarted`, `CallResolved`, `UpgradePurchased`, `ActionDenied`, `DisasterWarning`, `DisasterStarted`) num componente só de áudio.
Pronto quando: ganhar dinheiro é gostoso de ver e de ouvir.

T12 — casas e terremoto
`HouseFeedback` (um por casa; o MEGABRAIN adiciona ao `House.prefab`): escuta `HouseStateChanged` da própria casa; ao quebrar, sacudida, `_TextureLayer1Fade` subindo de 0 a 1 no material da casa e poeira; ao consertar, pop e brilho. No `Awake`, liga `house.externalFeedback` (o campo chega com o T04 do Agente B; até lá, só sobrescreva o valor do shader). Marcador de chamado pulsando. `CameraShaker`: componente separado que aplica e desfaz um deslocamento na Main Camera em `DisasterStarted`, sem brigar com os limites do `CameraController` e sem alterar esse arquivo.
Pronto quando: um terremoto parece um terremoto, com a onda passando pelas casas.

T17 — barra de caos e tela de fim provisória
`ChaosBar.prefab` (escuta `ChaosChanged`; muda de cor e pulsa acima de 60%) e `EndScreen.prefab` (escuta `GameStateChanged`; em `Lost` ou `Won` mostra tempo de partida, casas consertadas e dinheiro total, lidos do `GameManager`, e um botão Reiniciar que chama `GameManager.Instance.Restart()`). Os dois entram no `UICanvas`.
Pronto quando: o MEGABRAIN consegue pôr os dois na cena sem escrever código.

DIA 2 (comece assim que o Dia 1 estiver no remoto, sem esperar prompt novo), nesta ordem:
T27 (`WorkerFeel`: balanço ao andar, virar para a direção, pop em `WorkerSpawned` e `WorkerArrived`), T28 (faixa de aviso de desastre com Text Animator, contagem e sirene), T33 (botões de upgrade: brilho quando há dinheiro, punch na compra; ação negada com som de erro e tremida), T29 (menu inicial por cima da cidade na mesma cena, pausa, telas finais, dicas dos 3 primeiros passos), T26 (chuva, relâmpago em `LightningStruck`, fogo com fumaça e temporizador por `FireTimeLeft01`, vinheta pelo caos no `Global Volume`), T30 (música em 2 camadas pelo caos e sons finais). Antes de cada uma: `git fetch`, `git merge origin/jam`, e leia em `docs/jam/TASKS.md` a seção "Pedidos entre agentes", onde o MEGABRAIN devolve bugs do playtest: esses vêm antes de tarefa nova.

AO TERMINAR CADA TAREFA: console do Unity sem erros, commit, push, STATUS_C.md atualizado. No fim da sessão, um relatório curto: tarefas entregues com hash, o que o MEGABRAIN precisa ligar na cena, dúvidas e o que assumiu.
````
