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
