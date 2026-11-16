# Decisões

## Pós-auditoria do aaf6ab6 (ver `audit-meta.md`)

- **Manter os `.meta` novos** (guids novos). Os 536 `.png.meta` regenerados são os corretos.
- **PPU 32** para os sprites de `Assets/Arts/Houses`.
- **Compressão None** (Standalone), maxTextureSize 8192.
- **Override de WebGL removido** e mantido assim: o jogo não terá build WebGL.
- **Câmera e `CameraController` mantidos sem alteração.** Não foi adicionado `PixelPerfectCamera`.
- **Sprites de `House.prefab` e `Warehouse.prefab`: PLACEHOLDER**, sorteados entre os sprites existentes de `Assets/Arts/Houses`. Não são a arte final.
  - Sorteio: `awk srand(1794535202)` sobre 2154 sprites, excluindo as normal maps `*_n.png`. Dois índices distintos: o 1º foi para House, o 2º para Warehouse.
  - `House.prefab` -> `Rocha_Alta_J_NE` (`Nature/Rocha_Alta_J/Rocha_Alta_J.png`, guid `3486ad6b724c537fbc9f42fffcf1e0af`, fileID `1730322336`). **PLACEHOLDER.**
  - `Warehouse.prefab` -> `Casa_Q_N` (`Suburban/Casa_Q/Casa_Q.png`, guid `9f42e96fa88c5983a53bb9e87d0109d4`, fileID `1812150974`). **PLACEHOLDER.**
  - `Guard.prefab` herda de House e portanto mostra o mesmo placeholder.

## 2026-11-12 — Regras da cidade (Etapa 1)

Respostas do usuário aos formulários. As regras resultantes estão em `city-rules.md`.

**Composição e sprites**
- Casas básicas (House): **50–80**.
- Sprites das casas: **pode repetir**. Usar as pastas de estruturas, agrupando casas da mesma pasta em bairros (Comercial, Residencial etc.).
- Pastas das casas: `Suburban (Casa_*)`, `Commercial` (Predio_*, Arranha_Ceu_*, Fundo_*, exceto Guarda_Sol) e `Industrial (Fabrica_*)`.
- Proporção: **residencial maior**, ~60% Suburban, ~25% Commercial, ~15% Industrial, tolerância **±5 pontos (aviso)**.
- Agrupamento: **maioria por quarteirão** (≥ 80% das casas do polígono da mesma pasta).
- Sprites de Guard e dos 2 Warehouse: **aleatório `_S` de Industrial (Fabrica_*)**.
- Tamanho das estruturas: **nativo (PPU 32)**.
- Severidade: **"deve" = erro, "tente"/"priorize" = aviso**.

**Cena existente**
- House, Guard e Warehouse soltos em UIContainer: **apagar e gerar novos** dentro de Houses.
- Polygon antigo em PolygonLayer: **apagar**.
- 3 Waypoints existentes: **manter** (etapa do GPS).

**Decoração (Trees)**
- Pode ser quase tudo de `Nature/` (arbustos, pedras etc.).
- Excluídos: acampamento/objetos, pontes, horta/plantação. Monumentos permitidos.
- Direção: **só `_S`**. Tamanho: **nativo (PPU 32)**.
- Quantidade e posição:
  - Só sobre polígonos, **densas** (entre 2× e 3× o nº de casas).
  - Priorizar rente à rua: **≥ 70%** a ≤ 0,5 u da borda do polígono. O usuário pediu "meio termo" entre 60% e 80%.
- Posição em relação às estruturas:
  - **Base dentro do polígono, pode encostar/sobrepor** estruturas.
  - Polígonos exclusivos de decoração são permitidos, com **formas livres**.

**Chãos, ruas e área**
- Quarteirões: **retangulares, 4 vértices, alinhados a X/Y**.
- Rua: largura mínima **0,8 u** (o Worker mede ~0,4 u, logo 2 pessoas lado a lado).
- Estruturas: **cantos inferiores dentro do mesmo polígono, podem encostar/sobrepor**.
- Grid: **sem grid**, posições livres. Origem = (0,0) do mundo.
- Área: **ampliar o Limiter**, que deve ser **exatamente a caixa dos polígonos** (sem margem).
- Toda estrutura **deve ter rua ao sul** dentro da cidade.
- Pode criar e remover filhos de `PolygonLayer`. Componentes da câmera e `CameraController` continuam intocados.
- Área visível dos chãos:
  - Fato verificado: os polígonos só são exibidos via `PolygonCamera` (ortho 5) → `MapRender` (640×360) → `PolygonsDisplay` (RawImage fixa, ~17,8×10 u).
  - Posição do usuário: "não é um problema, os polígonos são renderizados e exibidos pela Raw Image PolygonsDisplay". A cidade pode ficar fora da área inicialmente renderizada.
  - Nenhuma alteração em PolygonCamera, MapRender ou PolygonsDisplay foi autorizada.
  - **Pendência:** verificar com screenshot na etapa de Build.

**Navegação**
- Criar **1 Waypoint de entrada por estrutura** (`House.entry`), na **rua imediatamente abaixo** do centro da borda inferior.
- A ligação entre Waypoints fica para a etapa do GPS.

**Fluxo e código**
- **Fluxo por JSON**: `city_layout.json` → Tools/City/Validate → Tools/City/Build.
- **Usar os pais existentes** (Houses, Trees, PolygonLayer). Sem root `GeneratedCity`.
- Namespace: **`DefesaCivil.CityGen`**.

## 2026-11-13 — Pendências e inventário (Etapa 2)

- `Polygon.prefab` e `PolygonMAT.mat` commitados sem alteração.
- As alterações não salvas da cena Gameplay eram do usuário. Foram salvas e commitadas, e `vHierarchy Data.asset` foi **commitado junto da cena**.
- Decorações: **podem repetir sprite**. O pool de R17 tem 96 sprites, menos que o mínimo de 100 de R19.
- `PolygonLayer/Canvas` (BG): **exceção fixa** a R26. Permanece e não é tocado.
- Ordem de desenho: **Y-sort dentro de cada pai** (Houses e Trees), com Trees sobre Houses (R30).
- `BoxCollider2D` das estruturas geradas: **ajustar ao retângulo do sprite nativo** na instância (R31).

## 2026-11-13 — Correção do moneyLbl e plano do gerador (Etapa 3)

- `ResourcesManager.moneyLbl` → `UI/UICanvas/Money/Lbl`, o único candidato. Verificado em Play Mode: `House.ResolveCompletion()` levou Money de 0 a 10 e o label mostrou 10, sem exceções.
- **GPS:** `GPS.allWaypoints` e `GPS.allLocations` são preenchidas pelo Build do gerador.
  - `allWaypoints`: **só os waypoints de entrada** gerados. Os 3 antigos ficam de fora.
  - `allLocations`: **só Locations** (Guard e Warehouses).

**Layout (JSON)**
- **Layout explícito** em `city_layout.json`: cada polígono, estrutura, decoração e waypoint de entrada tem posição e sprite.
- `Tools/City/Generate` sorteia e escreve o JSON, que pode ser editado à mão. O Build só aplica.
- Biblioteca: **JsonUtility**.
- Semente: **nova a cada Generate**, gravada no JSON.
- Generate sobre JSON existente: **pede confirmação** e guarda um backup.

**Build**
- Objetos gerados são identificados por **convenção de nome `[CG] <tipo> <id>`**.
- Estratégia: **atualizar por id** (cria, atualiza e remove só o que mudou).
- Ao fim, **salva a cena só se o Validate passar** (0 erros). Com erros, deixa a cena alterada sem salvar e reporta.
- Decorações geradas com **Raycast Target off**.

**Generate**
- Ruas **todas de 0,8 u**.
- Bairros com **posição livre/sorteada**.
- Quarteirões **sorteados**: 1 ou 2 fileiras, largura variável.
- Nº de casas **sorteado entre 50 e 80**, com **2 a 4 praças** (polígonos só de decoração).
- Espaço entre estruturas vizinhas: **sorteado entre 0 e 0,5 u**.
- **Guard:** a primeira fica no bairro industrial (haverá mais Guards no futuro; nesta etapa R02 continua exigindo **exatamente 1**).
- **Warehouses:** afastados, perto de **cantos diagonalmente opostos** da cidade. Isso virou regra (**aviso**), assim como a Guard no industrial: **R32 e R33**.

**Código e testes**
- **Sem asmdef**:
  - Dados em `Assets/_Project/Scripts/Runtime/CityData`.
  - Editor em `Assets/_Project/Scripts/Editor/CityGen`.
  - Testes em `Assets/_Project/Tests/EditMode/Editor/`.
- Verificado: `Assembly-CSharp-Editor` já referencia `nunit.framework` e o TestRunner.

**Relatório do Validate**
- Saída no **Console + `Assets/_Project/Data/Cities/validation_report.json`**.

## 2026-11-13 — Plano aprovado

- `docs/plan-citygen.md` **aprovado** para implementação.
- **Teste cedo** da visibilidade dos chãos fora do PolygonsDisplay antes do Build (passo 0 do plano).
- `validation_report.json` fica **fora de `Assets/`**, em `docs/reports/validation_report.json`, e é **ignorado pelo git** (`.gitignore`).
- **Execução seguida:** um commit por passo, sem pausas. Só paro para:
  - o resultado do teste cedo;
  - falhas que eu não consiga resolver;
  - dúvidas (formulário);
  - o passo 11.

## 2026-11-13 — Resultado do teste de visibilidade (passo 0)

- O teste (`docs/reports/visibility-test.md`) confirmou que chãos fora de ~17,8×10 u nunca aparecem.
- Decisão: **ampliar a captura e o display**:
  - aumentar o `orthographicSize` da PolygonCamera;
  - ajustar o retângulo do `PolygonsDisplay`;
  - **sem mudar a resolução do MapRender** (640×360), que dá o efeito pixelizado.
- R28 foi liberada só para esses dois pontos, e foi criada a R34.
- Enquadramento **automático pelo layout** (o Build calcula).
- **Centro da cidade = posição inicial da Main Camera**. PolygonCamera e Display continuam centrados nela.
- Texels maiores no chão (~0,1 u, ~3 px de sprite): **aceito, é o efeito**.

## 2026-11-13 — Cidade v2: bonita e natural (carta branca do usuário)

Pedido do usuário, sem formulários: "deixe as ruas todas com a mesma largura, independente do número de casas", "faça a cidade parecer natural", "esqueça um pouco as regras", "diminua a distância da base das estruturas até a rua ao sul", "estude os sprites", "se necessário, ignore qualquer regra pré-estabelecida", "apenas siga a organização dos objetos nos empties corretos", "faça todo o processo sem pedir autorização".

Escolhas minhas dentro dessa liberdade (nenhuma é PLACEHOLDER; todas podem ser revistas):

- **Ruas:** largura única de 0,8 u em toda a cidade, com rua de contorno. O Limiter passa a ser a caixa dos polígonos + 0,8 u (muda R27). Nova R35 (erro) mede a largura única.
- **Posicionamento pela base visível** do sprite (sem a margem transparente). Estruturas a 0,12 u da rua ao sul. O JSON foi para a versão 2.
- **Estruturas não se sobrepõem** (nova R37, aviso). Antes a sobreposição era permitida.
- **Decoração usa todas as pastas** de `Assets/Arts/Houses` (carros, postes, semáforos, tanques, chaminés, contêineres, cercas, canteiros), não só `Nature/` (muda R17). As exclusões que você pediu em Nature (acampamento, pontes, horta) continuam.
- **Quantidade de decoração:** só mínimo de 2× as casas (muda R19). R21 (70% rente à borda) foi revogada.
- **Oclusão coerente** (nova R36, aviso): decoração só cobre estrutura se estiver à frente dela.
- **Desenho urbano:** 6 faixas, duas ruas norte-sul atravessando, parque central, centro comercial em volta, indústria e Guarda no nordeste, depósitos no noroeste e sudeste, bosque no sudoeste.
- **Mantido das decisões anteriores:**
  - sprites `_S`;
  - 50–80 casas;
  - 1 Guard e 2 Warehouses em cantos opostos;
  - Guard no bairro industrial;
  - proporção ~60/25/15;
  - tamanho nativo;
  - objetos em Houses, Trees, Waypoints e PolygonLayer;
  - câmera e CameraController intocados;
  - MapRender 640×360.
- **Semente:** 293574367, escolhida por mim entre 5 sementes sorteadas, pelas prévias.
- **Ferramenta nova:** `CityPreview` gera um PNG do layout sem tocar na cena.

## 2026-11-13 — Cidade v3: duas fileiras, sem carros, waypoints ligados

Pedido do usuário:
- "faça os polygons conterem também uma fileira superior de casas (usando os sprites final _N), com os waypoints na rua de cima";
- "tire também todos os sprites de carros (TODOS) e substitua por estruturas normais";
- "linke todos os waypoints (...) colocando-os na lista neighbors. dois waypoints ligados diretamente devem ter um ao outro nas próprias listas (mas nunca eles mesmos)".

Como ficou:
- **Duas fileiras por quarteirão:**
  - Embaixo: fachada para a rua ao sul, sprites `_S`, waypoint na rua de baixo.
  - Em cima: fachada para a rua ao norte, sprites `_N`, waypoint na rua de cima.
  - Quintal entre elas: cerca ou sebe, árvores e flores.
  - Vale para residencial, comercial, industrial e depósitos. Parque e bosque não têm estruturas.
- **Nenhum sprite da pasta `Carros/`** (carros, caminhões, viaturas, caixas, destroços). Onde havia carros entram estruturas comuns do bairro. O pátio dos depósitos usa contêineres e tanque, da pasta Industrial.
- **Waypoints:**
  - Um de entrada por estrutura e um em cada cruzamento de ruas (`[CG] Waypoint Junction ###`).
  - Ao longo de cada rua, cada waypoint fica ligado ao vizinho imediato, nos dois sentidos, nunca a si mesmo.
  - O JSON guarda `junctions` e `links` (versão 3); o Build preenche `neighbors`.
  - Os 3 Waypoints antigos (R15) não fazem parte da malha nova e ficaram como estavam.
- **Escolhas minhas para caber tudo:**
  - 4 faixas de quarteirões com 7,2–7,5 u de profundidade.
  - Cidade com ~47–49 u de largura.
  - 70–78 casas planejadas (o resultado fica em 50–80, R01).
  - Espaço entre casas calculado por bairro para respeitar a proporção.
- **GPS:** `allWaypoints` agora inclui também os cruzamentos (antes só entradas), porque o caminho passa por eles.
- **Semente:** 509173474, escolhida por mim entre 12 sorteadas (sem avisos e mais casas).

## 2026-11-13 — Waypoints antigos removidos

- Pedido do usuário: **remover os 3 Waypoints antigos** (`Waypoint`, `Waypoint(Clone)` ×2), que não estavam em nenhuma rua da cidade nova.
- O Build faz isso como migração (remove de `Waypoints` tudo o que não é `[CG]`).
- R15 foi revogada e a R26 passou a valer também para `Waypoints`.
- Removida também a lista deles no `scene_baseline.json`.
- Efeito colateral: o campo `selectedWp` da ferramenta `WaypointCreator` (cena, `Scripts/WaypointCreator`) apontava para um deles e ficou vazio.

## 2026-11-13 — Camada dos Workers por Y (mesclar Houses + Trees + Workers)

- Pedido: os Workers (spawnados em `Workers`) devem ficar na frente ou atrás conforme o y, como casas e árvores.
- Escolha do usuário (formulário): **mesclar Houses + Trees + Workers** numa só ordem de desenho.
- Como: Canvas aninhado com Override Sorting em cada estrutura, decoração e Worker, com o componente `YSortCanvas` (`Assets/_Project/Scripts/Runtime/Sorting`).
  - Os Workers recalculam a ordem todo frame (`dynamic`).
  - O prefab `Worker.prefab` recebeu o Canvas e o componente.
  - Os empties continuam como estavam. A ordem entre eles deixou de importar para o desenho.
- Efeito: `Trees` deixa de ficar sempre sobre `Houses`; agora vale o y da base (R30 reescrita).
- Também: o `scene_baseline.json` foi recapturado porque a cor de fundo da Main Camera na cena salva já era preta (alteração feita no Editor, não pelo gerador).
