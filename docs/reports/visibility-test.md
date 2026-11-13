# Teste de visibilidade dos chãos (passo 0)

Executado em 2026-11-13 em **Play Mode**. Todas as mudanças foram descartadas ao sair, e a cena não foi salva. Ao final, `git status` não mostrava alteração em `Assets/`. Câmeras, PolygonsDisplay, PolygonCamera e MapRender não foram alterados em nenhum asset.

## Método

1. Instanciados 5 `Polygon.prefab` temporários (`TMP_VIS_*`, quadrados de 2×2 u) em `PolygonLayer`, com centro deslocado de dx = 0 (controle), 7, 10, 15 e 25 u à direita da câmera.
   - A área do PolygonsDisplay vai de −8,89 a +8,89 u na horizontal.
   - A mesh foi montada no script, porque o `PolygonMesh2D` só a gera fora do Play Mode.
2. **Vista A:** render da Main Camera na posição inicial (−3,44; −0,15).
3. **Vista B:** a Main Camera foi movida até cada dx, simulando o pan (só em Play Mode), com um render por posição.
4. Em cada render, também salvei o conteúdo de `MapRender` (o que a PolygonCamera vê).
5. Arquivos em `docs/reports/visibility/`: `A_cam_start*.png` e `B_cam_dx<dx>*.png`. O `_MapRender` é a textura.

Obs.: no Play Mode o `CameraController` ajusta o zoom, e a Main Camera estava com ortho ≈ 4, ou seja, vista de ~14,2×8 u.

## Resultado

| Polígono (dx) | Dentro do PolygonsDisplay? | Aparece na vista A (câmera inicial) | Aparece com a câmera sobre ele (vista B) |
|---|---|---|---|
| 0 (controle) | sim | **sim**, no centro | **sim** (dx 0) |
| 7 | sim (6–8 u, perto da borda) | **sim**, na borda direita, parcial | **sim**: com a câmera em dx 7, aparece no centro |
| 10 | não (9–11 u) | não | **não** |
| 15 | não | não | **não**: só se vê a faixa do dx 7 na borda esquerda |
| 25 | não | não | **não** |

## O que observei

- A textura `MapRender` é **idêntica em todas as posições da câmera**. Os polígonos são filhos da Main Camera e andam junto com ela, e a PolygonCamera também. Por isso a PolygonCamera sempre vê o mesmo conteúdo: o controle, o dx 7 (parcial) e o Polygon antigo.
- O `PolygonsDisplay` (RawImage) fica parado no mundo e mostra essa textura sempre no mesmo retângulo (−12,32…5,45 × −5,15…4,85).
- Com o pan, as casas (Canvas WorldSpace) se deslocam na tela e os chãos também, como parte do mundo fixo. Mas **só existem chãos dentro desse retângulo**.
- Um polígono cujo deslocamento em relação à câmera passa de ~8,89 u (horizontal) ou ~5 u (vertical) **nunca aparece**, em nenhuma posição de câmera.
- Os chãos visíveis ficam no mundo na posição **(posição inicial da câmera + deslocamento local)**. Dentro do retângulo, o que se constrói é exibido no lugar certo.

## Conclusão

Chãos fora da área ~17,8×10 u do PolygonsDisplay **não aparecem**. A cidade planejada (50–80 casas em tamanho nativo) é bem maior que essa área.
