# Regras da Cidade

Fonte: pedido do usuário (2026-11-12) e respostas registradas em `decisions.md`.
Revisão de 2026-11-13 (v3): fileira norte com sprites `_N`, nenhum sprite da pasta Carros e waypoints ligados pela rua. Mudaram R05, R12, R13, R14 e R17; entrou R38.
Revisão de 2026-11-13 (v2): o usuário pediu uma cidade bonita e natural, com ruas de largura única, e liberou ignorar regras anteriores. Mudaram R12, R17, R19, R20, R25 e R27; R21 foi revogada; entraram R35, R36 e R37.
Severidade: **erro** = obrigatório ("deve"); **aviso** = preferência ("tente", "priorize").

## Definições

- **u**: unidade de mundo. 1 u = 32 px (PPU 32). Sem grid: posições livres. Origem = (0,0) do mundo.
- **Medição**: no Editor, cena Gameplay como salva (Main Camera na posição salva), coordenadas de mundo XY.
- **Estrutura**: instância dos prefabs `House`, `Guard` ou `Warehouse` (`Assets/Systems/Houses/`).
- **Decoração**: GameObject com `Image` dentro de `Trees`.
- **Retângulo do sprite**: retângulo do `Visual` (estrutura) ou da `Image` (decoração) no mundo.
- **Parte visível**: retângulo que envolve os pixels opacos do sprite (sem a margem transparente).
- **Base** (ou borda inferior): lado de baixo da parte visível. "Cantos inferiores" são as 2 extremidades dela. No `city_layout.json`, `bottomCenter` é o centro da base.
- **Tamanho nativo**: largura = largura do sprite em px / 32 u; altura = altura em px / 32 u.
- **Polígono**: instância do prefab `Polygon` em `Main Camera/PolygonLayer`.
  - **Quarteirão**: polígono que contém estruturas.
  - **Polígono decorativo**: polígono que só contém decoração.
- **Rua**: área dentro do Limiter que não pertence a nenhum polígono. Inclui a rua de contorno entre os quarteirões da borda e o Limiter.

## Composição

| ID | Severidade | Regra (critério mensurável) |
|---|---|---|
| R01 | erro | Nº de `House` em `UI/Canvas/UIContainer/Houses` entre 50 e 80, inclusive. |
| R02 | erro | Existe exatamente 1 `Guard`. |
| R03 | erro | Existem exatamente 2 `Warehouse`. |
| R04 | erro | Todas as estruturas são filhas diretas de `UI/Canvas/UIContainer/Houses`. Há 0 estruturas fora dele. |

## Sprites das estruturas

| ID | Severidade | Regra |
|---|---|---|
| R05 | erro | O `Visual/Image.sprite` de toda estrutura termina com o sufixo da sua fileira: `_S` na fileira de baixo do quarteirão (fachada para a rua ao sul) e `_N` na fileira de cima (fachada para a rua ao norte). |
| R06 | erro | O sprite de cada `House` pertence ao pool abaixo. Repetição permitida. Nenhum outro sprite é aceito, incluindo `Guarda_Sol`. Pool: `Suburban/Casa_*`, `Commercial/Predio_*`, `Commercial/Arranha_Ceu_*`, `Commercial/Fundo_*` (inclui `Fundo_Largo_*`) e `Industrial/Fabrica_*`. |
| R07 | erro | O sprite de `Guard` e dos 2 `Warehouse` é `Industrial/Fabrica_*_S`. |
| R08 | erro | Os sprites de estruturas e decorações vêm de sorteio, não de escolha manual. A semente do sorteio fica registrada em `city_layout.json`. |
| R09 | aviso | Proporção das `House` por pasta: Suburban 60%, Commercial 25%, Industrial 15%, cada uma com ±5 pontos. |
| R10 | aviso | Em cada quarteirão, ≥ 80% das `House` são da mesma pasta (bairros Residencial/Comercial/Industrial). |
| R11 | erro | Toda estrutura está em tamanho nativo (tolerância 0,01 u). |

## Posicionamento das estruturas

| ID | Severidade | Regra |
|---|---|---|
| R12 | erro | A parte visível de cada estrutura está inteira dentro do mesmo quarteirão. |
| R13 | erro | Toda estrutura tem rua na sua fachada: abaixo do quarteirão (fileira S) ou acima dele (fileira N), dentro do Limiter. |
| R14 | erro | Toda estrutura tem `entry` ≠ nulo, apontando para um `Waypoint` filho de `UI/Canvas/UIContainer/Waypoints`, na linha central da rua da fachada (meia largura de rua além da borda do quarteirão) e na vertical do centro da base (tolerância 0,01 u). |
| R15 | — | **Revogada em 2026-11-13** a pedido do usuário: os 3 Waypoints antigos foram removidos. (Era: os 3 Waypoints pré-existentes em `Waypoints` são mantidos.) |

## Decoração (Trees)

| ID | Severidade | Regra |
|---|---|---|
| R16 | erro | Toda decoração é um GameObject com `Image`, filho direto de `UI/Canvas/UIContainer/Trees`. |
| R17 | erro | O sprite de cada decoração termina em `_S`, vem de qualquer pasta de `Assets/Arts/Houses`, exceto `Carros/` (Nature, Roads e os objetos de Suburban, Commercial e Industrial) e não é sprite de estrutura (R06). Continuam excluídos de `Nature/`: `Barraca*`, `Saco_Dormir`, `Canoa`, `Remo`, `Fogueira*`, `Lenha`, `Placa_Madeira`, `Ponte_*`, `Horta_*`, `Milho_*`, `Trigo_*`, `Abobora`, `Cenoura`, `Melancia`, `Nabo`, `Vitoria_Regia*`. Repetição de sprite permitida. |
| R18 | erro | Toda decoração está em tamanho nativo (tolerância 0,01 u). |
| R19 | erro | Nº de decorações ≥ 2× o nº de `House`. Sem limite superior. |
| R20 | erro | Os 2 cantos inferiores (da parte visível) de cada decoração estão dentro do mesmo polígono (quarteirão ou decorativo). |
| R21 | — | **Revogada em 2026-11-13.** (Era: ≥ 70% das decorações a ≤ 0,5 u da borda do polígono.) |

## Chãos e ruas

| ID | Severidade | Regra |
|---|---|---|
| R22 | erro | Todo chão é instância de `Assets/Systems/Houses/Polygon.prefab`, filho direto de `Main Camera/PolygonLayer`. |
| R23 | erro | Todo quarteirão tem exatamente 4 vértices e lados paralelos aos eixos X/Y. |
| R24 | erro | Polígonos decorativos têm forma livre, com ≥ 3 vértices. |
| R25 | erro | Polígonos não se sobrepõem. A distância mínima entre quaisquer dois polígonos é ≥ `streetWidth` (0,8 u). |
| R26 | erro | `PolygonLayer`, `Houses`, `Trees` e `Waypoints` contêm somente objetos que constam em `city_layout.json`. O Polygon e as estruturas antigos não permanecem. **Exceção fixa:** `PolygonLayer/Canvas` (com `BG`) permanece e não é tocado. |
| R27 | erro | Os bounds XY do `Essentials/Limiter` são iguais à caixa envolvente de todos os polígonos expandida por `limiterMargin` (= largura da rua) em cada lado, formando a rua de contorno (tolerância 0,01 u). |

## Restrições e fluxo

| ID | Severidade | Regra |
|---|---|---|
| R28 | erro | Ficam sem alteração em relação ao commit `ac210a1`: componentes da Main Camera, `CameraController.cs` e `MapRender` (incluindo a resolução de 640×360). Na PolygonCamera, só o `orthographicSize` pode mudar; no `PolygonsDisplay`, só o retângulo (âncoras, `sizeDelta` e posição). Ambos são ajustados pelo Build (R34). Sob a Main Camera, só mudam os filhos de `PolygonLayer` e o ortho da PolygonCamera. |
| R29 | erro | O layout é definido em `Assets/_Project/Data/Cities/city_layout.json` e aplicado por `Tools/City/Build`. Rodar o Build 2 vezes sem mudar o JSON não gera diferença na cena (idempotente). |
| R30 | erro | Ordem de desenho por Y dentro de cada pai: em `Houses` e em `Trees`, a ordem de irmãos segue o y da borda inferior. Quem tem y menor (mais ao sul) vem depois e é desenhado na frente. `Trees` continua desenhado sobre `Houses`. |
| R31 | erro | O `BoxCollider2D` de cada estrutura gerada cobre o retângulo do sprite em tamanho nativo (tolerância 0,01 u). |
| R32 | aviso | Os 2 `Warehouse` ficam em cantos diagonalmente opostos: cada um no quarteirão mais próximo de um canto do Limiter (SW/NE ou NW/SE). |
| R33 | aviso | A `Guard` fica em um quarteirão do bairro Industrial (≥ 80% das `House` dele são de `Industrial/`, como em R10). |
| R34 | erro | Enquadramento dos chãos: o centro XY da caixa dos polígonos coincide com a posição inicial da Main Camera (tolerância 0,5 u). A vista da PolygonCamera (2·ortho·16/9 × 2·ortho) e o retângulo do `PolygonsDisplay` no mundo têm o mesmo centro (a Main Camera) e o mesmo tamanho (tolerância 0,01 u), e ambos contêm a caixa dos polígonos inteira. |
| R35 | erro | Ruas de largura única: de qualquer ponto de um lado de polígono, andando para fora na perpendicular, encontra-se outro polígono ou a borda do Limiter a exatamente `streetWidth` (tolerância 0,02 u). Exceções: o lado está na borda do Limiter, ou o ponto está na boca de uma rua transversal que também mede `streetWidth`. |
| R36 | aviso | Oclusão coerente: como `Trees` é desenhado sobre `Houses`, uma decoração só pode cobrir a parte visível de uma estrutura se estiver à frente dela (base da decoração com y ≤ base da estrutura). |
| R37 | aviso | As partes visíveis das estruturas não se sobrepõem. |
| R38 | erro | Waypoints ligados pela rua: além das entradas, há um waypoint em cada cruzamento de ruas (`[CG] Waypoint Junction ###`). Ao longo de cada rua, cada waypoint é ligado ao seguinte, e só a ele: os dois têm um ao outro em `neighbors`. Ninguém está na própria lista, não há repetição nem referência nula, e todos os waypoints gerados formam um grafo conexo. |
