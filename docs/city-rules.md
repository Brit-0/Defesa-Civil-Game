# Regras da Cidade

Fonte: pedido do usuário (2026-11-12) e respostas registradas em `decisions.md`.
Severidade: **erro** = obrigatório ("deve"); **aviso** = preferência ("tente", "priorize").

## Definições

- **u**: unidade de mundo. 1 u = 32 px (PPU 32). Sem grid: posições livres. Origem = (0,0) do mundo.
- **Medição**: no Editor, cena Gameplay como salva (Main Camera na posição salva), coordenadas de mundo XY.
- **Estrutura**: instância dos prefabs `House`, `Guard` ou `Warehouse` (`Assets/Systems/Houses/`).
- **Decoração**: GameObject com `Image` dentro de `Trees`.
- **Retângulo do sprite**: retângulo do `Visual` (estrutura) ou da `Image` (decoração) no mundo.
- **Borda inferior**: lado de baixo do retângulo do sprite. "Cantos inferiores" são as 2 extremidades dela.
- **Tamanho nativo**: largura = largura do sprite em px / 32 u; altura = altura em px / 32 u.
- **Polígono**: instância do prefab `Polygon` em `Main Camera/PolygonLayer`.
  - **Quarteirão**: polígono que contém estruturas.
  - **Polígono decorativo**: polígono que só contém decoração.
- **Rua**: área dentro do Limiter que não pertence a nenhum polígono.

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
| R05 | erro | O `Visual/Image.sprite` de toda estrutura tem nome terminado em `_S`. |
| R06 | erro | O sprite de cada `House` pertence ao pool abaixo. Repetição permitida. Nenhum outro sprite é aceito, incluindo `Guarda_Sol`. Pool: `Suburban/Casa_*`, `Commercial/Predio_*`, `Commercial/Arranha_Ceu_*`, `Commercial/Fundo_*` (inclui `Fundo_Largo_*`) e `Industrial/Fabrica_*`. |
| R07 | erro | O sprite de `Guard` e dos 2 `Warehouse` é `Industrial/Fabrica_*_S`. |
| R08 | erro | Os sprites de estruturas e decorações vêm de sorteio, não de escolha manual. A semente do sorteio fica registrada em `city_layout.json`. |
| R09 | aviso | Proporção das `House` por pasta: Suburban 60%, Commercial 25%, Industrial 15%, cada uma com ±5 pontos. |
| R10 | aviso | Em cada quarteirão, ≥ 80% das `House` são da mesma pasta (bairros Residencial/Comercial/Industrial). |
| R11 | erro | Toda estrutura está em tamanho nativo (tolerância 0,01 u). |

## Posicionamento das estruturas

| ID | Severidade | Regra |
|---|---|---|
| R12 | erro | Os 2 cantos inferiores de cada estrutura estão dentro do mesmo quarteirão. Sobreposição visual entre estruturas é permitida. |
| R13 | erro | Toda estrutura tem rua ao sul: um raio vertical para baixo, saindo do centro da borda inferior, deixa o quarteirão e atravessa um trecho de rua antes de sair do Limiter. |
| R14 | erro | Toda estrutura tem `entry` ≠ nulo, apontando para um `Waypoint` filho de `UI/Canvas/UIContainer/Waypoints`. O Waypoint fica fora de qualquer polígono, com x = centro da borda inferior e y = ponto médio do primeiro trecho de rua do raio de R13 (tolerância 0,01 u). |
| R15 | erro | Os 3 Waypoints pré-existentes em `Waypoints` são mantidos. |

## Decoração (Trees)

| ID | Severidade | Regra |
|---|---|---|
| R16 | erro | Toda decoração é um GameObject com `Image`, filho direto de `UI/Canvas/UIContainer/Trees`. |
| R17 | erro | O sprite de cada decoração vem de `Nature/`, termina em `_S` e não é de uma pasta excluída. Monumentos são permitidos. Excluídos: `Barraca*`, `Saco_Dormir`, `Canoa`, `Remo`, `Fogueira*`, `Lenha`, `Placa_Madeira`, `Ponte_*`, `Horta_*`, `Milho_*`, `Trigo_*`, `Abobora`, `Cenoura`, `Melancia`, `Nabo`, `Vitoria_Regia*`. Repetição de sprite permitida. |
| R18 | erro | Toda decoração está em tamanho nativo (tolerância 0,01 u). |
| R19 | erro | Nº de decorações entre 2× e 3× o nº de `House`, inclusive. |
| R20 | erro | Os 2 cantos inferiores de cada decoração estão dentro do mesmo polígono (quarteirão ou decorativo). Sobreposição com estruturas é permitida. |
| R21 | aviso | ≥ 70% das decorações têm o centro da borda inferior a ≤ 0,5 u da borda do polígono que as contém (árvores de calçada). |

## Chãos e ruas

| ID | Severidade | Regra |
|---|---|---|
| R22 | erro | Todo chão é instância de `Assets/Systems/Houses/Polygon.prefab`, filho direto de `Main Camera/PolygonLayer`. |
| R23 | erro | Todo quarteirão tem exatamente 4 vértices e lados paralelos aos eixos X/Y. |
| R24 | erro | Polígonos decorativos têm forma livre, com ≥ 3 vértices. |
| R25 | erro | Polígonos não se sobrepõem. A distância mínima entre quaisquer dois polígonos é ≥ 0,8 u (largura mínima de rua). |
| R26 | erro | `PolygonLayer`, `Houses` e `Trees` contêm somente objetos que constam em `city_layout.json`. O Polygon e as estruturas antigos não permanecem. **Exceção fixa:** `PolygonLayer/Canvas` (com `BG`) permanece e não é tocado. |
| R27 | erro | Os bounds XY do `Essentials/Limiter` são iguais à caixa envolvente de todos os polígonos (tolerância 0,01 u). |

## Restrições e fluxo

| ID | Severidade | Regra |
|---|---|---|
| R28 | erro | Ficam sem alteração em relação ao commit `ac210a1`: componentes da Main Camera, `CameraController.cs`, PolygonCamera, `MapRender` e `PolygonsDisplay`. Sob a Main Camera, só mudam os filhos de `PolygonLayer`. |
| R29 | erro | O layout é definido em `Assets/_Project/Data/Cities/city_layout.json` e aplicado por `Tools/City/Build`. Rodar o Build 2 vezes sem mudar o JSON não gera diferença na cena (idempotente). |
| R30 | erro | Ordem de desenho por Y dentro de cada pai: em `Houses` e em `Trees`, a ordem de irmãos segue o y da borda inferior. Quem tem y menor (mais ao sul) vem depois e é desenhado na frente. `Trees` continua desenhado sobre `Houses`. |
| R31 | erro | O `BoxCollider2D` de cada estrutura gerada cobre o retângulo do sprite em tamanho nativo (tolerância 0,01 u). |
