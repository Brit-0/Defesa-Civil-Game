# Plano do gerador de cidade

Status: **proposto, aguardando aprovação**. Nada foi implementado.

Base: `CLAUDE.md`, `city-rules.md` (R01–R33), `inventory.md` (L01–L11) e `decisions.md`. Unidades em **u** (mundo, 32 px) e **cu** (Canvas `UI/Canvas`, 1 cu = 0,01 u, 1 px = 3,125 cu).

Fluxo: `Tools/City/Generate` → `city_layout.json` (editável) → `Tools/City/Validate` → `Tools/City/Build` (aplica, roda o Validate na cena e salva se houver 0 erros).

## a. Formato do layout

Arquivo: `Assets/_Project/Data/Cities/city_layout.json`, serializado com **JsonUtility**. Classes `[Serializable]` em `Assets/_Project/Scripts/Runtime/CityData/` (namespace `DefesaCivil.CityGen`).

Todas as coordenadas são XY de mundo, medidas com a cena como salva.

```json
{
  "version": 1,
  "seed": 1794535202,
  "generatedAt": "2026-11-13T00:00:00Z",
  "streetWidth": 0.8,
  "polygons": [
    { "id": "001", "kind": "Block", "district": "Suburban",
      "points": [ {"x": 0, "y": 0}, {"x": 12, "y": 0}, {"x": 12, "y": 4.5}, {"x": 0, "y": 4.5} ] },
    { "id": "014", "kind": "Plaza", "district": "None", "points": [ ... ] }
  ],
  "structures": [
    { "id": "012", "type": "House", "district": "Commercial", "polygonId": "001",
      "sprite": { "guid": "…", "name": "Predio_C_S", "path": "Assets/Arts/Houses/Commercial/Predio_C/Predio_C.png" },
      "bottomCenter": {"x": 3.1, "y": 0.2},
      "entry": {"x": 3.1, "y": -0.4} }
  ],
  "decorations": [
    { "id": "045", "polygonId": "001", "sprite": { "guid": "…", "name": "Pinheiro_S", "path": "…" },
      "bottomCenter": {"x": 0.6, "y": 4.1} }
  ]
}
```

O que cada campo representa nas regras:

| Conceito das regras | Representação |
|---|---|
| House, Guard, Warehouse (R01–R03, R07, R32, R33) | `structures[].type`. A lista já aceita várias Guards no futuro. |
| Pasta / bairro (R06, R09, R10, R33) | `structures[].district` (Suburban/Commercial/Industrial). Conferido contra o caminho do sprite. |
| Sprite e sufixo `_S` (R05–R07, R17) | `sprite.guid` + `sprite.name` (`path` é só para leitura humana). |
| Semente (R08) | `seed` |
| Tamanho nativo (R11, R18, R31) | **Não armazenado**: derivado dos px do sprite / 32. |
| Borda inferior (R12, R13, R20, R21, R30) | `bottomCenter` + largura nativa → os 2 cantos inferiores. |
| Waypoint de entrada (R14) | `structures[].entry` |
| Quarteirão / praça (R23, R24) | `polygons[].kind` = `Block` / `Plaza`. |
| Ruas (R25) | Implícitas: área sem polígono. `streetWidth` registra o mínimo usado. |
| Limiter (R27) | **Não armazenado**: caixa envolvente dos `polygons`. |
| Ids e nomes (R26, R29) | `id` de 3 dígitos por tipo. Na cena, `[CG] <tipo> <id>` (ex.: `[CG] House 012`, `[CG] Tree 045`, `[CG] Polygon 001`, `[CG] Waypoint House 012`). |

**Generate** (`Tools/City/Generate`):
- Semente nova a cada execução, gravada no JSON.
- Se o JSON já existe, pede confirmação e guarda um backup `city_layout.json~` (o Unity ignora arquivos terminados em `~`).
- Etapas determinísticas a partir da semente:
  1. Malha de quarteirões retangulares alinhados a X/Y (1 ou 2 fileiras, largura sorteada) com ruas de 0,8 u.
  2. 2–4 praças de forma livre.
  3. Bairros em posição sorteada.
  4. Nº de casas sorteado em 50–80, na proporção 60/25/15.
  5. Casas ao longo das fileiras, com espaço sorteado de 0–0,5 u.
  6. Guard num quarteirão industrial; Warehouses nos quarteirões de cantos opostos.
  7. Decoração com sprites de Nature `_S` (2–3× o nº de casas), ≥ 70% rente à borda.
  8. Waypoint de entrada pela fórmula de R14.
- A fileira de quarteirões mais ao sul precisa ter rua dentro do Limiter (R13). Por isso o Generate não põe estruturas em quarteirões sem rua ao sul; aí só entra decoração ou praça.

## b. Validador

`Tools/City/Validate` tem dois modos:
- **Layout**: lê o JSON. Roda sozinho ou antes do Build.
- **Cena**: lê a hierarquia da Gameplay. Roda depois do Build.

O relatório vai para o Console (1 linha por violação) e para `Assets/_Project/Data/Cities/validation_report.json`. Cada item tem `ruleId`, `severity` (erro/aviso), `object` (id ou nome `[CG]`), `position` (x, y em u) e `message`. Termina com o total de erros e avisos.

Legenda: **L** = checável no layout · **C** = checável na cena após o Build · **N** = não automatizável.

| Regra | L | C | Como |
|---|---|---|---|
| R01 50–80 House | ✔ | ✔ | Conta `type=House` / filhos `[CG] House` em Houses (excluindo as Variants). |
| R02 1 Guard · R03 2 Warehouse | ✔ | ✔ | Contagem por tipo / componente `CityGuard`, `Warehouse`. |
| R04 estruturas só em Houses | — | ✔ | Varre a cena: todo `House` (e derivados) tem pai = Houses. |
| R05 sufixo `_S` | ✔ | ✔ | Nome do sprite (JSON / `Visual.Image.sprite`). |
| R06 pool House · R07 pool Guard/Warehouse | ✔ | ✔ | Caminho da textura contra os pools do catálogo. |
| R08 sorteio + semente | parcial | — | **L**: `seed` presente e sprites nos pools. **N**: "não foi escolhido à mão" não é detectável, porque o JSON é editável. |
| R09 proporção ±5 p.p. | ✔ | ✔ | % por `district`. |
| R10 ≥ 80% mesma pasta por quarteirão | ✔ | ✔ | Agrupa por `polygonId` / por polígono que contém a borda. |
| R11 tamanho nativo estruturas | ✔ | ✔ | **L**: implícito (não armazenado). **C**: `sizeDelta` da raiz = px × 3,125 cu (tol. 0,01 u). |
| R12 cantos inferiores no mesmo quarteirão | ✔ | ✔ | Point-in-polygon dos 2 cantos. |
| R13 rua ao sul | ✔ | ✔ | Raio para baixo do centro inferior: sai do polígono e acha um trecho fora de polígonos antes do fim do Limiter. |
| R14 entry | ✔ | ✔ | **L**: `entry` = fórmula de R13. **C**: `House.entry` ≠ null, pai Waypoints, posição (tol. 0,01). |
| R15 3 Waypoints antigos mantidos | — | ✔ | Os 3 não-`[CG]` em Waypoints continuam presentes (referência guardada por GlobalObjectId na 1ª execução). |
| R16 decoração em Trees, com Image | — | ✔ | Filhos `[CG] Tree` com `Image`. |
| R17 pool decoração | ✔ | ✔ | Nature `_S`, fora da lista de exclusões. |
| R18 tamanho nativo decoração | ✔ | ✔ | Como R11. |
| R19 2–3× nº de House | ✔ | ✔ | Contagem. |
| R20 cantos inferiores num polígono | ✔ | ✔ | Point-in-polygon. |
| R21 ≥ 70% a ≤ 0,5 u da borda | ✔ | ✔ | Distância ponto–segmento. |
| R22 Polygon.prefab em PolygonLayer | — | ✔ | `PrefabUtility.GetCorrespondingObjectFromSource` = Polygon.prefab e pai = PolygonLayer. |
| R23 quarteirão retangular alinhado · R24 praça ≥ 3 vértices | ✔ | ✔ | Vértices e paralelismo (tol. 0,001). |
| R25 sem sobreposição, distância ≥ 0,8 u | ✔ | ✔ | Distância mínima entre polígonos (segmento–segmento) e teste de interseção. |
| R26 só objetos do JSON (exceção Canvas/BG) | — | ✔ | Filhos de PolygonLayer/Houses/Trees: todo filho é `[CG]` com id no JSON, exceto `PolygonLayer/Canvas`. |
| R27 Limiter = caixa dos polígonos | ✔ (calculado) | ✔ | `SpriteRenderer.bounds` contra a caixa (tol. 0,01). |
| R28 câmera/MapRender/Display intocados | — | ✔ | Snapshot dos campos serializados (Main Camera, PolygonCamera, CameraController, PolygonsDisplay) contra o valor de `ac210a1`, mais hash de `CameraController.cs` e `MapRender.renderTexture`. |
| R29 JSON → Build idempotente | — | ✔ | Build 2× e comparação de um **dump canônico** dos objetos `[CG]` (nome, posição, sprite, tamanho, path). O arquivo `.unity` não serve, ver Riscos. |
| R30 Y-sort | — | ✔ | Ordem de irmãos em Houses e Trees decrescente em y da borda inferior. |
| R31 collider = retângulo nativo | — | ✔ | `BoxCollider2D.size` = `sizeDelta`, offset 0 (tol. 0,01 u). |
| R32 Warehouses em cantos opostos (aviso) | ✔ | ✔ | Quarteirão de cada Warehouse = o mais próximo de cantos opostos do Limiter. |
| R33 Guard no industrial (aviso) | ✔ | ✔ | Quarteirão da Guard com ≥ 80% de House Industrial. |

Fora das regras, o Validate informa também quais polígonos ficam fora do retângulo do `PolygonsDisplay` (L11).

## c. Builder

`Tools/City/Build` faz o seguinte, numa única operação de Undo:
1. Lê o JSON.
2. Roda o Validate de layout. Com erros, para.
3. Aplica as mudanças nos pais que já existem, localizados por caminho fixo:
   - `UI/Canvas/UIContainer/Houses`, `…/Trees`, `…/Waypoints`
   - `Essentials/Main Camera/PolygonLayer`
   - `Essentials/Limiter`
   - `Scripts/GPS`
4. Roda o Validate de cena. Com 0 erros, salva a Gameplay. Com erros, deixa a cena alterada, sem salvar, e reporta.

**Idempotência sem root** (convenção de nome + atualizar por id):
- Em cada pai, monta um mapa `(tipo, id) → filho` a partir dos nomes `[CG] <tipo> <id>`.
- Para cada item do JSON: cria se faltar (`PrefabUtility.InstantiatePrefab`, mantendo o vínculo com o prefab) ou atualiza só os campos que diferem.
- Remove os `[CG]` sem item no JSON.
- Quem não é `[CG]` não é tocado. Exceção: **migração única**, que apaga, se existirem, `UIContainer/House`, `UIContainer/Guard`, `UIContainer/Warehouse` e `PolygonLayer/Polygon` (decisões 2026-11-12). `PolygonLayer/Canvas` nunca é tocado.

**Por estrutura** (House, Guard, Warehouse):
- **Sprite:** sobrescrito na instância em `Visual/Image.sprite` (o prefab continua com o PLACEHOLDER).
- **Tamanho (R11):** raiz `sizeDelta` = px × 3,125 cu. O `Visual` estica junto (stretch + preserveAspect). Posição: `bottomCenter` convertido com `Houses.InverseTransformPoint` + meia altura.
- **Collider (R31):** `BoxCollider2D.size` = `sizeDelta`, `offset` 0. O espaço local da raiz já é cu.
- **Entrada (R14):** um `[CG] Waypoint House 012` (instância de `Waypoint.prefab`) em Waypoints, na posição `entry`, mantendo z e escala do prefab. Fica atribuído a `House.entry`. Os `neighbors` ficam vazios até a etapa do GPS.

**Decorações:** `[CG] Tree <id>` com `Image` (sprite, tamanho nativo, `raycastTarget = false`) em Trees.

**Ordenação (R30):** depois de criar e atualizar, `SetSiblingIndex` em Houses e em Trees, ordenando por y da borda inferior decrescente. Quem está mais ao sul fica por último e é desenhado na frente. Trees continua depois de Houses.

**Polígonos:** `[CG] Polygon <id>` (instância de Polygon.prefab) em PolygonLayer.
- O path do `PolygonCollider2D` é convertido de mundo para local (`InverseTransformPoint`).
- A mesh é regenerada pelo próprio `PolygonMesh2D` (ExecuteInEditMode). O Builder chama `OnColliderUpdate()` depois de definir o path, para não esperar um frame.

**Limiter (R27):** `position` = centro da caixa dos polígonos (z mantido) e `localScale` XY = tamanho. O sprite Square tem 1 u, e o z da escala é mantido.

**GPS:** `allWaypoints` = waypoints de entrada gerados e `allLocations` = Guard + Warehouses, gravados via `SerializedObject`.

**Visibilidade dos chãos (L11):** o Builder não altera câmeras, MapRender nem PolygonsDisplay (R28).
- Depois do Build, o Validate lista os polígonos fora do retângulo do PolygonsDisplay.
- Também tiro screenshots via MCP (Scene/Camera capture) na posição inicial e com a câmera deslocada.
- Se houver chão invisível, **paro e pergunto** antes de seguir.

## d. Testes (EditMode)

Ficam em `Assets/_Project/Tests/EditMode/Editor/`, sem asmdef. `Assembly-CSharp-Editor` já referencia `nunit.framework` e `UnityEditor/UnityEngine.TestRunner` (Test Framework 1.8.0). O passo 1 confirma que o Test Runner descobre os testes ali. Rodo via MCP (`TestRunnerApi`).

| Arquivo | Cobre |
|---|---|
| `LayoutSerializationTests` | Ida e volta JsonUtility sem perda; `version` e `seed` obrigatórios. |
| `GeometryTests` | Point-in-polygon, distância entre polígonos, interseção, raio para o sul até a rua, caixa envolvente, retângulo alinhado. |
| `SpriteCatalogTests` | Pools: Suburban 21, Commercial 33, Industrial 20, Nature 96. Exclusões de R17. Tamanho nativo = px/32. |
| `LayoutValidatorTests` | Para cada regra L: um layout mínimo válido (0 violações) e um inválido que gera exatamente o ID esperado com a severidade certa. |
| `GeneratorTests` | Mesma semente gera o mesmo JSON. Para 20 sementes, o layout gerado tem 0 erros no Validate de layout. Contagens dentro das faixas. |
| `BuilderTests` | Numa cena temporária (`EditorSceneManager.NewScene`) com a hierarquia mínima (Houses/Trees/Waypoints/PolygonLayer com Canvas/Limiter/GPS) e os prefabs reais, verifica: build de um layout pequeno; 2º build sem diferença no dump canônico (R29); mudar 1 sprite altera só aquele objeto (mesmo instanceID nos demais); remover um id apaga só ele; R30, R31, R14, GPS e Canvas intocado. |
| `SceneValidatorTests` | Regras C sobre a cena temporária montada pelo Builder, com violações provocadas. |

## e. Cobertura das lacunas

| Lacuna | Resolvida por |
|---|---|
| L01 sem `city_layout.json` | a (formato) + Generate, passo 6 |
| L02 sem Validate/Build/testes | b, c, d, passos 1–10 |
| L03 estruturas ausentes / antigas soltas | c: Builder de estruturas + migração, passos 8, 11 |
| L04 Polygon antigo / sem quarteirões | c: Builder de polígonos + migração, passo 7 |
| L05 Trees vazio | c: decorações, passo 9 |
| L06 sprite PLACEHOLDER dos prefabs | c: sprite por instância, passo 8 |
| L07 `sizeDelta` / collider fixos | c: tamanho e collider por instância, passo 8 |
| L08 `entry` nulo | c: waypoint de entrada, passo 8 |
| L09 Limiter | c: Limiter, passo 7 |
| L10 sem Y-sort | c: ordenação, passos 8–9 |
| L11 visibilidade | c: relatório + screenshots + parar e perguntar, passo 11 |

## f. Ordem de implementação

Cada passo vira 1 commit em português na `city-gen`.

| # | Passo | Critério verificável |
|---|---|---|
| 1 | Esqueleto: classes vazias no namespace `DefesaCivil.CityGen`, menus `Tools/City/*` (stub) e 1 teste trivial | Compila sem erros. O Test Runner lista e passa o teste em `Tests/EditMode/Editor`. |
| 2 | Modelo de dados + JsonUtility | `LayoutSerializationTests` verdes. |
| 3 | Geometria | `GeometryTests` verdes. |
| 4 | Catálogo de sprites | `SpriteCatalogTests` verdes (21/33/20/96). |
| 5 | Validate de layout + relatório | `LayoutValidatorTests` verdes. Rodar no Console gera `validation_report.json`. |
| 6 | Generate (+ confirmação/backup) e 1º `city_layout.json` | `GeneratorTests` verdes. O JSON gerado tem 0 erros. JSON commitado. |
| 7 | Builder: polígonos, Limiter, migração do Polygon antigo | `BuilderTests` (parte polígonos) verdes. |
| 8 | Builder: estruturas, sprite, tamanho, collider, Y-sort, waypoints, GPS, migração das 3 antigas | `BuilderTests` (estruturas) verdes. |
| 9 | Builder: decorações + Y-sort | `BuilderTests` (decoração) verdes. |
| 10 | Validate de cena (incl. R28 snapshot, R29 dump) + salvar se passar | `SceneValidatorTests` verdes. |
| 11 | Build real na Gameplay | Validate de cena: 0 erros (avisos listados). Screenshots de visibilidade. Só o `.unity` e o relatório mudam. Commit da cena. |

## Riscos

1. **Visibilidade (L11):** a cidade vai ocupar bem mais que 17,8×10 u. Pela configuração atual, os chãos fora do PolygonsDisplay tendem a não aparecer. O passo 11 verifica isso e para antes de seguir.
2. **Ruído na cena:** o `PolygonMesh2D` recria a mesh a cada frame no Editor. Todo salvamento gera diff na mesh (novo fileID), como já aconteceu no fix do moneyLbl. Por isso R29 é medida pelo dump canônico, não pelo `.unity`.
3. **R13 × Limiter exato:** quarteirões da fileira sul não podem ter estruturas. O Generate precisa respeitar isso, o que reduz a área útil.
4. **R08 com JSON editável:** edições manuais de sprite não são detectáveis. R08 fica parcial (N).
5. **Relatório em `Assets/`:** `validation_report.json` gera `.meta` e reimport a cada Validate, além de diff no git.
6. **Raycast:** decorações ficam sem raycast. Estruturas sobrepostas (R12 permite) continuam disputando cliques pela ordem do Y-sort.
7. **Sem asmdef:** o código do gerador compila junto do projeto. Um erro de compilação em qualquer script bloqueia o gerador e vice-versa.
