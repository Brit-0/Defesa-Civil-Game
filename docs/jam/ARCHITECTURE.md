# Arquitetura — estado atual e alvo

Levantamento feito em 16/11/2026 sobre a `main` (commit do pull de 16/11, que já contém o trabalho da `city-gen`).

## 1. Base técnica

- Unity **6000.6.7f1**, URP 17.6 (Renderer2D), Input System 1.20.1, C# 9.
- Plugins no projeto: **DOTween** (`Assets/Plugins/Demigiant`), **Feel / MMFeedbacks** (`Assets/Feel`, ainda sem uso na cena), **Odin Inspector** (`Assets/Plugins/Sirenix`), **Text Animator** (Febucci), **Easy Save 3**, **Sprite Shaders Ultimate**, Hot Reload, vHierarchy/vInspector, Fullscreen Editor.
- Pixel art: PPU 32, Point, sem compressão. Fonte: `Assets/Fonts/ModernDOS`.
- Sem asmdef no código do jogo: tudo cai em `Assembly-CSharp` (e `Assembly-CSharp-Editor` para pastas `Editor/`).

## 2. Cena `Assets/Scenes/Gameplay.unity`

```
Scripts/                      managers (um GameObject por manager)
  FeedbacksManager, GPS, WaypointCreator, ResourcesManager,
  AnalyticsManager, EletricityManager, GuardManager
UI/
  Canvas  (World Space, câmera = Main Camera)      <- o "mundo" do jogo
    UIContainer/
      PolygonsDisplay   RawImage que mostra os chãos (via PolygonCamera -> MapRender)
      Houses/           73 instâncias: 70 "[CG] House n", 2 "[CG] Warehouse n", 1 "[CG] Guard n"
      Trees/            221 decorações (Image, raycast off)
      Pole              (desativado) protótipo de poste de energia
      Workers/          pai dos trabalhadores instanciados
      Waypoints/        105 waypoints: 73 de entrada + 32 de cruzamento
  UICanvas (Screen Space - Camera)                  <- HUD
    FeedbackText (off)  texto de instrução no topo
    GuardTab (off)      aba de despacho: +, -, Enviar
    Resources/          Workers, BuildMaterial, Kit (ícone + Lbl)
    Money/              Icon + Lbl
    OpenUpgrades        botão que liga/desliga UpgradeTab
    UpgradeTab (off)    Layout (VerticalLayoutGroup) com 3 UpgradeButton sem lógica
Essentials/
  EventSystem, Global Volume, Global Light 2D, Limiter (limites da câmera),
  Main Camera (CameraController) -> PolygonLayer (17 chãos) e PolygonCamera,
  Input (PlayerInput: Tab = ViewMode, vazio desde a v6A; Esc = Cancel)
```

Pontos que afetam qualquer implementação:

- **Casas, árvores e trabalhadores são `Image` de UI num Canvas World Space**, não SpriteRenderers. Clique e hover vêm de `IPointer*Handler`.
- Cada casa/árvore/trabalhador tem um **Canvas aninhado com Override Sorting** e o componente `YSortCanvas` (ordem por Y: base 5000, overlays 10000). A cena tem ~300 Canvas. Isso pesa no WebGL; medir cedo.
- Para um efeito aparecer acima da cidade: `sortingOrder` ≥ 10000 no Canvas World Space, ou colocar no `UICanvas`.
- Objetos gerados têm nome `[CG] <tipo> <id>` e são recriados pelo `Tools/City/Build`. **Não renomear nem mover.** Mudanças de comportamento entram no prefab (`House.prefab`), não nas instâncias.
- `House.prefab` → variantes `Guard.prefab` e `Warehouse.prefab`. Estrutura: `Visual` (Image + Tweener) e `Canvas` com `Tab` (InfoPanel), `Progress`, `Complete`, `Call` (ícone + quantidade de dano).

## 3. Código de gameplay (`Assets/Systems/`)

| Arquivo | Papel |
|---|---|
| `Houses/House.cs` | Estado da casa (`State`: Intact, PowerOutage, Damaged, OnFire), chamado atual, progresso de trabalho, coleta, `Break()`/`Repair()`. Recebe os eventos de ponteiro. |
| `Houses/Location.cs` | `abstract : House`. Estruturas especiais com `LocationData` + `InfoPanel` no hover. |
| `Houses/CityGuard.cs` | `: Location`. Base dos trabalhadores. Clique em modo de seleção escolhe a base. |
| `Houses/Warehouse.cs` | `: Location`. Chamado fixo do tipo `Restock` (+1 material por trabalhador). |
| `Houses/InfoPanel.cs` | Painel de nome/descrição das Locations. |
| `Houses/LocationsManager.cs` | Classe vazia (fora da cena). |
| `Guard/GuardManager.cs` | Singleton. Fluxo do chamado: `OpenCall` → `SelectBase` → `IncreaseTeamSize`/`DecreaseTeamSize` → `SendTeam` → `RetrieveTeam`. Define `Call` e `CallType`. `moneyPerCall = 10`. |
| `Navigation/GPS.cs` | Singleton `main`. `FollowPath` (coroutine que move o trabalhador e dispara `StartWorking` no último), `FindShortestPath`. `speed = 1`. |
| `Navigation/Pathfinder.cs` | A* estático sobre `Waypoint.neighbors`. |
| `Navigation/Waypoint.cs` | Nó do grafo + `PathNode`. |
| `Resources/ResourcesManager.cs` | Singleton. Propriedades estáticas `Workers`, `FirstAid`, `BuildMaterial`, `Money` que atualizam o label no setter. `Workers = 3` no `Start`. |
| `Feedbacks/FeedbacksManager.cs` | `ShowText`/`HideText` do texto de instrução com fade. |
| `UI/Tweener.cs` | Hover/press com DOTween (escala). |
| `UI/CursorController.cs` | Troca de cursor (fora da cena, sem texturas). |
| `Camera/CameraController.cs` | Pan (botão direito/meio), zoom (scroll, 2 a 8), `Focus(pos)`, limites pelo `Limiter`. |
| `Analytics/AnalyticsManager.cs` | Modo de visualização com holograma no material das casas. Fora do Tab desde a v6A: o Tab abre e fecha a aba de melhorias (`UI/UpgradePanelShortcuts.cs`). |
| `Eletricity/*` | Protótipo de postes com raio de alcance. Poste desativado na cena. |
| `Masters/GameManager.cs` | `BlockInput`/`UnblockInput` vazios (fora da cena). |
| `Map Design/*` | Ferramentas de edição (`PolygonMesh2D`, `WaypointCreator`). |

Fora de `Systems/`: `Assets/SO/LocationData.cs` (ScriptableObject), assets em `Assets/Resources/Locations/`, gerador de cidade em `Assets/_Project/` (namespace `DefesaCivil.CityGen`, 81 testes EditMode).

### Fluxo atual de um chamado

```
House.GenerateCall()            (hoje só pelo botão do Odin)
  -> sorteia PowerOutage/Damaged/OnFire; só Damaged chama Break()
  -> callDisplay ligado
clique na casa  -> GuardManager.OpenCall(call)     selectBaseMode = true
clique na Guarda-> GuardManager.SelectBase(guard)  abre GuardTab
+ / - / Enviar  -> GuardManager.SendTeam()
  -> exige TeamSize >= workersNeeded e material >= damageAmount
  -> instancia Worker.prefab a cada 0,8 s; Workers--
  -> GPS.FollowPath(...) até house.entry
último chega    -> House.StartWorking()            círculo por workTime (10 s)
fim do círculo  -> isComplete; ícone "Complete"
clique na casa  -> House.ResolveCompletion()
  -> gasta material, Repair(), Money += moneyPerCall
  -> GuardManager.RetrieveTeam(call): trabalhadores voltam; Workers++
```

## 4. Convenções de estilo (seguir no código novo)

- **Sem namespace** no código de gameplay (o namespace `DefesaCivil.CityGen` é só do gerador).
- Um sistema por pasta em `Assets/Systems/<Sistema>/`, com scripts e prefabs do sistema juntos.
- Managers são `MonoBehaviour` singleton com `public static X Instance;` atribuído no `Awake`. (Código antigo usa `main` em `GPS` e `EletricityManager`; não renomear, mas código novo usa `Instance`.)
- Recursos expostos como **propriedade estática** que encapsula o campo da instância e atualiza a UI no setter.
- `[Header("MAIÚSCULAS")]` para agrupar; `[SerializeField] private` para referências; sufixos `Lbl` (TMP_Text), `Pf` (prefab).
- Odin `[Button]` para ações de debug no Inspector.
- Fluxos temporais com **coroutines**; animação com **DOTween**.
- Identificadores em inglês; textos de UI em português.
- Dados de design em `ScriptableObject`: classe em `Assets/SO/`, assets em `Assets/Resources/<Tipo>/`.
- Input pelo componente `PlayerInput` com Unity Events (`CallbackContext`).
- Código só de Editor: pasta `Editor/` ou `#if UNITY_EDITOR`.
- Commits pequenos, em português.

## 5. Problemas conhecidos

| # | Onde | Problema | Tarefa |
|---|---|---|---|
| ~~B1~~ | `AnalyticsManager.cs`, `Waypoint.cs`, `WaypointCreator.cs` | CORRIGIDO em 16/11 (`75ed78a`): `using UnityEditor` / `Handles` / `Selection` agora só dentro de `#if UNITY_EDITOR`. Falta confirmar com o build Windows (U2). | T01 |
| ~~B2~~ | `GPS.cs`, `InfoPanel.cs` | CORRIGIDO em 16/11 (`75ed78a`): `using static UnityEngine.Rendering.DebugUI` e `using Unity.VisualScripting` removidos. | T01 |
| B3 | `Tweener.cs` | IDs globais de DOTween (`"HoverEnter"`, `"TweenHold"`): `DOTween.Complete(id)` afeta todas as casas. `Hold` completa `"TweenHover"`, que não existe. | T10 |
| B4 | `GuardManager.RetrieveTeam` | Usa `teamSize` (valor da UI) em vez de `call.workersSent` para marcar o último trabalhador. | T04 |
| B5 | `GuardManager` | Um único `currentCall`/`selectedGuard`. `CancelCall` não confere `context.performed`, então dispara 3 vezes por tecla. | T04 |
| B6 | `House.ResolveCompletion` | Sem material na coleta: paga o dinheiro, não conserta, e a casa fica `Damaged` sem novo marcador de chamado. | T04 |
| B7 | `House.GenerateCall` | Sorteia estados sem lógica; `GetIconByCallType` devolve `null` para problemas. Pode ser chamado em Guarda/Armazém (herdam de `House`). | T04 |
| B8 | `House.StartWorking` | O tween do círculo não é morto se o objeto for destruído/reiniciado. | T04 |
| B9 | `ResourcesManager` | Valores iniciais no código (`Workers = 3`), material começa em 0, `moneylIcon` sem sprite. | T05 |
| B10 | `GPS.FollowPath` | `path == null` só imprime e depois quebra no `foreach`. | T04 |
| B11 | Projeto | Sem áudio (nenhum AudioSource/AudioClip). | T13 |
| B12 | `Arts/Texture/pngtree-crack-texture-...png` | Textura de rachadura baixada da internet; conferir licença ou substituir por arte própria. | Arte |
| B13 | `EletricPole.Update` | `OverlapCircleAll` todo frame. Energia cortada da jam; o poste segue desativado na cena. | — |
| B14 | `GuardManager.SendTeam` | O laço de saída usa os campos `teamSize` e `selectedGuard` enquanto espera 0,8 s entre trabalhadores. Se o jogador abre outro chamado nesse meio, `SelectBase` zera `TeamSize`: saem menos trabalhadores que `workersSent`, nenhum é o "último" e a casa fica travada com `interactable = false`. Copiar tamanho e base para variáveis locais antes do laço. | T04 |
| B15 | `GuardManager.SendTeam` | `Workers--` um a um durante a saída: dá para montar um segundo chamado com gente que já está comprometida e o estoque fica negativo. Debitar a equipe inteira no envio. | T04 |
| B16 | `Warehouse.cs`, `Location.cs`, `House.cs` | `Awake` e `Start` privados com o mesmo nome em classe base e derivada: o Unity só chama o da classe mais derivada. No Armazém, `House.Awake` (material próprio) e `Location.Start` (`infoPanel.Set`) não rodam. Tornar `protected virtual` e chamar `base`. Achado na leitura do código; confirmar no Play (U8). | T04 |
| B17 | `GuardManager.SendTeam` | Envio negado (equipe menor que o mínimo, material insuficiente) sai em silêncio com `yield break`. Disparar `ActionDenied`. | T04 |
| B18 | `State` (enum em `House.cs`) | A cena guarda `currentState` como número. **Nunca reordenar nem remover valores do enum**; valor novo só no fim. | regra |

## 6. Arquitetura-alvo

Objetivo: três agentes trabalhando em paralelo sem editar os mesmos arquivos. A peça central é um barramento de eventos estático: sistemas disparam, apresentação escuta.

### 6.1 `GameEvents` (contrato; dono: Agente B)

`Assets/Systems/Masters/GameEvents.cs`

Revisado pelo MEGABRAIN em 16/11: entraram `PassiveIncomePaid`, `WorkerSpawned`, `WorkerArrived`, `ActionDenied` (com `DeniedReason`) e `LightningStruck`. Sem eles o repasse, os trabalhadores na rua, a ação negada e o raio ficariam sem resposta visual e sonora.

```csharp
public enum GameState { Menu, Playing, Paused, Won, Lost }
public enum DeniedReason { NoWorkers, NoMaterial, NoMoney, MaxLevel }

public static class GameEvents
{
    // Economia
    public static event Action<int, int> MoneyChanged;          // (antes, depois)
    public static event Action<int> MaterialChanged;
    public static event Action<int> WorkersChanged;
    public static event Action<int> PassiveIncomePaid;          // valor do Repasse da Prefeitura

    // Casas e chamados
    public static event Action<House, State, State> HouseStateChanged; // (casa, antes, depois)
    public static event Action<Call> CallOpened;       // jogador abriu o chamado
    public static event Action<Call> TeamDispatched;   // equipe saiu da base
    public static event Action<Call> WorkStarted;      // último trabalhador chegou
    public static event Action<Call> WorkCompleted;    // pronto para coletar
    public static event Action<Call, int> CallResolved; // (chamado, dinheiro ganho)
    public static event Action<Transform, Call> WorkerSpawned;  // trabalhador nasceu na rua
    public static event Action<Transform, Call> WorkerArrived;  // chegou ao destino, antes de sumir
    public static event Action<DeniedReason, Vector3> ActionDenied; // (motivo, posição em mundo)

    // Desastres
    public static event Action<DisasterData, float> DisasterWarning;   // (tipo, segundos até começar)
    public static event Action<DisasterData, Vector3> DisasterStarted; // (tipo, epicentro em mundo)
    public static event Action<Vector3> LightningStruck;               // raio da tempestade (T20)
    public static event Action<DisasterData> DisasterEnded;

    // Progressão
    public static event Action<UpgradeData, int> UpgradePurchased;     // (upgrade, novo nível)
    public static event Action<float> ChaosChanged;                    // 0..1
    public static event Action<GameState> GameStateChanged;

    // Um método Raise... por evento. Só os sistemas (Agente B) chamam Raise.
}
```

Regras:
- Limpar todos os eventos em `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, para funcionar com Domain Reload desligado.
- Assinar em `OnEnable`, desassinar em `OnDisable`.
- Mudança no contrato = avisar no `TASKS.md` e fazer merge imediato na `jam`.

O que a apresentação (C) pode **ler** dos sistemas, além dos eventos. O B mantém estes nomes:

| Onde | Membro |
|---|---|
| `House` | `currentState`, `damageAmount`, `currentCall`, `entry`, `FireTimeLeft01` (1 = acabou de pegar fogo, 0 = apagou), `externalFeedback` (o `HouseFeedback` do C liga; com ele ligado, `House` não mexe mais na rachadura do shader) |
| `Call` | `house`, `guard`, `type`, `workersSent` |
| `GameManager.Instance` | `State`, `ElapsedTime`, `HousesRepaired`, `TotalMoneyEarned`, `StartGame()`, `Pause()`, `Resume()`, `Restart()` |
| `ChaosManager.Instance` | `Chaos` (0..1) |
| `UpgradesManager.Instance` | `GetLevel(UpgradeData)`, `GetCost(UpgradeData)`, `CanBuy(UpgradeData)`, `TryBuy(UpgradeData)` |
| `UpgradeData` | `id`, `displayName`, `description`, `icon`, `maxLevel`, `baseCost`, `costGrowth` |
| `DisasterData` | `id`, `type` (`DisasterType { Earthquake, Storm, Fire }`), `displayName`, `icon` |

### 6.2 Sistemas novos

| Sistema | Pasta | Dono | Resumo |
|---|---|---|---|
| `GameManager` | `Systems/Masters/` | B | Estado da partida (`GameState`), tempo de partida, pausa, reinício. |
| `LocationsManager` | `Systems/Houses/` | B | Registro: `Houses` (só casas comuns), `Guards`, `Warehouses`, preenchido no `Awake` por `FindObjectsByType`. Consultas: casas num raio, casas intactas, casas com problema. |
| `BalanceData` + `Stats` | `SO/`, `Systems/Masters/` | B | SO único com os valores base. `Stats.WorkerSpeed`, `Stats.WorkTime`, `Stats.MoneyPerCall`, `Stats.MaterialPerWorker`, `Stats.AutomationLevel` = base + efeito dos upgrades. Nenhum número de balanceamento no código. |
| `DisasterManager` + `DisasterData` | `Systems/Disasters/` | B | Linha do tempo, aviso, execução por tipo (coroutine). Assets em `Resources/Disasters/`. |
| `ChaosManager` | `Systems/Chaos/` | B | Calcula o alvo, anima a barra lógica, dispara `ChaosChanged`, declara derrota. |
| `UpgradesManager` + `UpgradeData` + `UpgradeButton` | `Systems/Upgrades/` | B | Níveis, custo, compra. A aba é preenchida em runtime com `Resources.LoadAll<UpgradeData>("Upgrades")`: **adicionar upgrade = criar um asset, sem tocar na cena**. |
| `AudioManager` | `Systems/Audio/` | C | Pool de AudioSources, `PlaySfx(clip)`, música em 2 camadas com crossfade pelo caos. |
| `Feel` | `Systems/Feel/` | C | Componentes que só escutam `GameEvents`: `CameraShaker`, `HouseFeedback`, `MoneyPopups`, `HudJuice`, `DisasterBanner`, `ChaosVignette`, `WeatherOverlay`. |
| Telas | `Systems/UI/` | C | Menu, pausa, vitória, derrota, barra de caos, como **prefabs novos**. |

### 6.3 Mudanças em código existente (dono: B)

- `House`: um único `SetState(State novo)` que dispara `HouseStateChanged`. `ApplyProblem(State problema, int severidade)` substitui o sorteio de `GenerateCall`. `Break()`/`Repair()` passam a ser detalhes internos.
- `House` deixa de mexer direto em visual além do que já faz hoje; efeitos novos ficam em `HouseFeedback` (C), no mesmo prefab.
- `GuardManager`: lê o nível de automação em `Stats` para pular passos; corrige B4, B5, B14, B15, B17. Material e trabalhadores saem do estoque no envio (GDD, seção 3).
- `ResourcesManager`: setters disparam `MoneyChanged`/`MaterialChanged`/`WorkersChanged`; valores iniciais vêm do `BalanceData`.
- `GPS`: `speed` vem de `Stats.WorkerSpeed`; dispara `WorkerSpawned`/`WorkerArrived`.
- `GuardManager` continua chamando `FeedbacksManager.ShowText`/`HideText` direto no Dia 1 (já funciona). A troca por evento fica para o T29, junto com as dicas.
- `CameraController`: **não alterar** sem pedir ao usuário. Tremor de câmera é um componente separado que aplica offset e devolve a posição.

### 6.4 Dados criados por menu de Editor

Agentes sem Unity não conseguem criar `.asset` com segurança. Padrão (igual ao do gerador de cidade): B escreve `Assets/Systems/Masters/Editor/JamDataMenu.cs` com `Tools/Jam/Create Default Data`, que cria/atualiza os `UpgradeData`, `DisasterData` e o `BalanceData` padrão. O Agente A roda o menu e commita os assets.

### 6.4.1 `.meta` de quem trabalha sem Unity

O GUID de um script mora no `.meta`, e a cena e os prefabs apontam para o script por esse GUID. Se um `.cs` novo chega sem `.meta`, cada Unity que importar o projeto inventa um GUID diferente, e as referências quebram ou viram conflito no merge. Por isso **quem cria arquivo sem o Unity aberto cria o `.meta` à mão, no mesmo commit**, com um GUID novo de 32 caracteres hexadecimais minúsculos (`[guid]::NewGuid().ToString('N')` no PowerShell).

`Arquivo.cs.meta`:

```
fileFormatVersion: 2
guid: <32 hex>
```

`Pasta.meta` (para cada pasta nova, inclusive `Editor`):

```
fileFormatVersion: 2
guid: <32 hex>
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

Um GUID por arquivo, nunca repetido, nunca trocado depois do primeiro push.

### 6.5 Regras para a cena

- **Só o Agente A edita `Gameplay.unity` e os prefabs que já existem.**
- B e C entregam scripts e prefabs **novos**; A posiciona na cena e liga referências.
- Preferir `Instance`/`FindObjectsByType`/`Resources.Load` a referências arrastadas na cena, para reduzir o trabalho de ligação.

### 6.6 WebGL

- Proibido: `System.Threading`, `System.IO` em disco, `UnityEditor` em runtime.
- Áudio só toca depois do primeiro clique do jogador: o menu inicial resolve.
- ~300 Canvas aninhados e 73 materiais instanciados (`House.Awake`) são o principal risco de desempenho. Medir no teste de fumaça (T02).
