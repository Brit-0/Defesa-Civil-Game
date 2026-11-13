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
