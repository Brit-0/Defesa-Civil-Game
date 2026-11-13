# Relatório do Build (passo 11)

Data: 2026-11-13.

- **Layout:** `Assets/_Project/Data/Cities/city_layout.json`, semente `2066620142`.
- **Build:** `Tools/City/Build` (via MCP) na cena `Assets/Scenes/Gameplay.unity`.
- **1º Build:** 348 criados, 5 atualizados, 4 removidos.
  - Os removidos foram as estruturas antigas House, Guard e Warehouse, mais o Polygon antigo.
  - Os atualizados foram o Limiter, o enquadramento e as listas do GPS.
- **2º Build:** 0 criados, 0 atualizados, 0 removidos, 353 sem mudança. É idempotente (R29).
- **Validate de cena:** 0 erros, 0 avisos. A cena foi salva.
  - Revalidado depois de recarregar a cena do disco: 0 erros, 0 avisos.
  - Console sem erros.

## Conteúdo gerado

| Item | Qtd |
|---|---|
| House (Suburban / Commercial / Industrial) | 72 (43 / 18 / 11) → 59,7% / 25,0% / 15,3% |
| Guard / Warehouse | 1 / 2 |
| Decorações (Trees) | 182 (2,53× nº de House) |
| Polígonos | 17 = 13 quarteirões + 4 praças |
| Waypoints de entrada | 75 (+ 3 antigos mantidos) |
| Caixa da cidade (= Limiter) | 52,44 × 39,50 u, centrada em (−3,44; −0,15) = posição inicial da Main Camera |
| PolygonCamera ortho | 5 → 19,75 (vista 70,2 × 39,5 u). PolygonsDisplay com o mesmo retângulo. MapRender continua 640×360. |

## Resultado por regra (validador de cena)

| Regra | Sev. | Resultado |
|---|---|---|
| R01 50–80 House | erro | ✔ 72 |
| R02 1 Guard | erro | ✔ |
| R03 2 Warehouse | erro | ✔ |
| R04 estruturas só em Houses | erro | ✔ |
| R05 sufixo `_S` | erro | ✔ |
| R06 pool House | erro | ✔ |
| R07 pool Guard/Warehouse | erro | ✔ |
| R08 sorteio + semente | erro | ✔ parcial: semente registrada e sprites nos pools. "Não manual" não é automatizável. |
| R09 proporção ±5 | aviso | ✔ |
| R10 ≥ 80% mesma pasta por quarteirão | aviso | ✔ (100%: 1 bairro por quarteirão) |
| R11 tamanho nativo estruturas | erro | ✔ |
| R12 cantos inferiores no quarteirão | erro | ✔ |
| R13 rua ao sul | erro | ✔ |
| R14 waypoint de entrada | erro | ✔ |
| R15 3 Waypoints antigos | erro | ✔ (por GlobalObjectId) |
| R16 decoração em Trees com Image | erro | ✔ |
| R17 pool decoração | erro | ✔ |
| R18 tamanho nativo decoração | erro | ✔ |
| R19 2–3× nº de House | erro | ✔ 182 |
| R20 cantos inferiores da decoração no polígono | erro | ✔ |
| R21 ≥ 70% rente à borda | aviso | ✔ |
| R22 Polygon.prefab em PolygonLayer | erro | ✔ |
| R23 quarteirão retangular alinhado | erro | ✔ |
| R24 praça ≥ 3 vértices | erro | ✔ (octógonos) |
| R25 ruas ≥ 0,8 u, sem sobreposição | erro | ✔ |
| R26 só objetos do JSON (exceção Canvas/BG) | erro | ✔ |
| R27 Limiter = caixa dos polígonos | erro | ✔ |
| R28 câmera/CameraController/MapRender intocados | erro | ✔ (baseline de `ac210a1`; só o ortho da PolygonCamera e o retângulo do Display mudaram) |
| R29 JSON → Build idempotente | erro | ✔ (dump canônico igual no 2º Build) |
| R30 Y-sort | erro | ✔ |
| R31 collider = retângulo nativo | erro | ✔ |
| R32 Warehouses em cantos opostos | aviso | ✔ |
| R33 Guard no industrial | aviso | ✔ |
| R34 enquadramento | erro | ✔ |

## Screenshots (Play Mode, nada salvo)

Ficam em `docs/reports/build/`:

| Arquivo | Câmera |
|---|---|
| `01_visao_geral.png` | Cidade inteira (ortho 21) |
| `02_inicio_zoom_padrao.png` | Posição inicial, ortho 10 |
| `03_pan_noroeste.png` a `06_pan_sudeste.png` | Pan até os 4 quadrantes (ortho 8). Os chãos aparecem em todos. |
| `07_detalhe_centro.png` | Zoom no centro (ortho 4) |

As câmeras foram movidas só em Play Mode, como o pan do jogo, e descartadas ao sair.

## Observações e riscos restantes

1. **Decoração rente ao topo do quarteirão:** a base fica dentro do polígono, mas o sprite sobe sobre a rua de cima. Visualmente parece estar na rua, apesar de cumprir R20/R21.
2. **Praças densas:** as decorações das praças se sobrepõem bastante. As da borda de cima invadem a frente das casas do quarteirão logo acima, porque Trees é desenhado sobre Houses (R30).
3. **Ruído no `.unity`:** o `PolygonMesh2D` recria a mesh a cada frame no Editor. Todo salvamento da cena gera diff nas meshes. R29 é medida pelo dump canônico.
4. **Test Runner e Undo:** o Test Runner salva a cena aberta e desfaz o Undo ao terminar. O Builder roda sem Undo nos testes (`undo:false`), e o caminho com Undo só é exercitado no Build real.
5. **Ligações do GPS:** os `neighbors` dos waypoints de entrada estão vazios. A ligação com a malha de ruas fica para a etapa do GPS. Até lá, `Pathfinder` não acha caminho entre estruturas.
6. **R08:** é só parcialmente automatizável, porque edições manuais de sprite no JSON não são detectáveis.
