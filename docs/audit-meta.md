# Auditoria do commit aaf6ab6

Somente leitura. Nada foi alterado, revertido ou apagado. BASE = `45f7a43` ("oi").
`git diff --stat BASE aaf6ab6`: 548 arquivos, +27702 / -84929.

## 1. Conteúdo do aaf6ab6

- 538 arquivos `.meta`: 536 modificados e 2 novos (`Guarda.asset.meta`, `Guard.prefab.meta`).
- 10 arquivos não-.meta:

| Arquivo | Mudança |
|---|---|
| `Assets/Scenes/Gameplay.unity` | +264/-14. Novo GuardTab (renomeado de "Tab"), ícone, referências a sprites de Fabrica_Q, Fabrica_T e Casa_H usando os **guids novos**. |
| `Assets/Systems/Guard/GuardManager.cs` | `public int moneyPerCall = 10;` |
| `Assets/Systems/Houses/House.cs` | +1 linha: `ResourcesManager.Money += GuardManager.Instance.moneyPerCall;` |
| `Assets/Systems/Resources/ResourcesManager.cs` | Novos `moneyLbl`, `moneylIcon` e propriedade estática `Money`. |
| `Assets/Systems/Houses/Guard.prefab` (novo) | Prefab da Guarda (225 linhas). |
| `Assets/Resources/Locations/Guarda.asset` (novo) | LocationData "Guarda". |
| `Packages/manifest.json` e `packages-lock.json` | Adicionado `com.unity.ai.assistant 2.21.0-pre.2` (traz `nuget.newtonsoft-json` 3.2.2 etc.). |
| `ProjectSettings/Packages/com.unity.ai.assistant/Settings.json` (novo) | Config do pacote de IA. |
| `Defesa-Civil-Game.slnx` | Regenerado pela IDE. Ruído. |

## 2. Classificação dos 536 .meta modificados

| Grupo | Qtd |
|---|---|
| (a) `guid` alterado | **536** (100%) |
| (b) só importer | 0 |
| (c) outro | 0 |

Todos os 536 também tiveram mudança de importer, ou seja, (a) sobrepõe (b). Todos são `.png.meta` de `Assets/Arts/Houses/**`.

Exemplos: `Carros/Ambulancia/Ambulancia.png.meta`, `Carros/Ambulancia/Ambulancia_n.png.meta`, `Carros/Caixa/Caixa.png.meta`, `Carros/Caminhao_Bau/Caminhao_Bau.png.meta`, `Suburban/Casa_D/Casa_D.png.meta`.

Campos alterados (antigo -> novo), 536 de 536 cada:

- `spritePixelsToUnits: 100 -> 32`
- `maxTextureSize: 2048 -> 8192`
- `textureCompression` na plataforma Standalone: `1 -> 0` (sem compressão)
- Removido o override da plataforma **WebGL** (compressão 1, 2048).
- Removidos `spriteTessellationMethod` e `spriteGeometrySubdivision`.

Em 533 dos 536, a tabela de sprites também foi trocada:

- **Antes**, a tabela era uma cópia do template "Ambulancia": sprites `Ambulancia_0…N`, grade 96x96, em qualquer arquivo (ex.: `Casa_D.png.meta` tinha sprites `Ambulancia_*`).
- **Agora**, tem os sprites reais da imagem (`Casa_D_S`, `Casa_D_SW`, `Casa_D_W`…), com `spriteID` e `internalID` coerentes.

Leitura: os metas do BASE estavam inconsistentes (cópias do template), e o Unity os regenerou com guids novos e dados corretos. As configurações novas (PPU 32, sem compressão, Point filter) são coerentes com pixel art.

## 3. Referências aos guids (536 arquivos)

Varridos 6920 arquivos de Assets/, ProjectSettings/, Packages/ e UserSettings/ (prefab, unity, asset, mat, anim, controller, meta, json, inputactions etc.).

| Situação | Qtd |
|---|---|
| Referenciado só pelo guid **novo** (ok) | 271 (268 por outros .meta, ex.: mapas normais `_n`; 3 pela cena: Fabrica_Q, Fabrica_T, Casa_H) |
| Referenciado só pelo guid **antigo** (QUEBRADO) | **2** |
| Por ambos | 0 |
| Por nenhum | 263 |

**Quebrados:**

1. `Assets/Arts/Houses/Suburban/Casa_D/Casa_D.png.meta`, guid antigo `fc041bba197ef744cb0bd87bd1491305` -> novo `fd8b113177d0504a84583004208125fa`.
   Referência: `Assets/Systems/Houses/House.prefab:147`, `m_Sprite: {fileID: -1725543945317942209, ...}`.
2. `Assets/Arts/Houses/Industrial/Fabrica_B/Fabrica_B.png.meta`, guid antigo `9f147dcae32429547839935045b2b7da` -> novo `c067ba3746c759018be707a7422ad173`.
   Referência: `Assets/Systems/Houses/Warehouse.prefab:98`, `m_Sprite` override com `fileID: -169266744`.

Além do guid, o **fileID** dessas referências também não existe mais. Eram IDs de sprites do template antigo (`Ambulancia_N`), e os sprites reais têm outros `internalID`. Restaurar só a linha `guid:` não resolveria.

## 4. Cena e assets via MCP (Unity sem importar, cena Gameplay já aberta, `isDirty=false`, nada salvo)

- Raízes Essentials, Scripts e UI: 375 componentes, **0 Missing Script, 0 referência perdida, 0 Missing Prefab**. 402 campos nulos, só informativo (campos opcionais).
- `Resources/Locations/Armazém.asset` e `Guarda.asset` carregam normalmente. `LocationData` só tem strings. Em `Assets/SO/` só existe o script.
- **Prefabs:** `House.prefab`, `Warehouse.prefab` e `Guard.prefab` têm `Visual/Image.m_Sprite` **ausente**. O `Guard.prefab` parece herdar do House, então deve ser consequência.
- Console (Error/Warning): vazio no momento da leitura, sem avisos de GUID/importação. O console foi limpo por reload de domínio, então a evidência forte é a análise estática da seção 3.

## 5. PixelPerfectCamera

- Está no **URP 17.6.0**: `UnityEngine.Rendering.Universal.PixelPerfectCamera` em `Runtime/2D/PixelPerfectCamera.cs`, com editor e conversor. Não precisa do pacote `com.unity.2d.pixel-perfect`.
- `UniversalRP.asset` usa o **Renderer2D** (`Assets/Settings/Renderer2D.asset`), que é o renderer suportado.
- Atualmente não há `PixelPerfectCamera` em nenhuma cena.
- Como adicionar (sem aplicar agora): na Main Camera da Gameplay, Add Component -> *Pixel Perfect Camera (URP)*, com Assets PPU = 32 e Reference Resolution conforme o alvo.
- **Conflito:** `CameraController.cs` altera `cam.orthographicSize` (zoom suave nas linhas 28 e 84). O Pixel Perfect controla esse valor, então o zoom precisaria mudar para passos inteiros de PPU/upscale, ou o Pixel Perfect usar só `Upscale Render Texture`. Decisão sua.

## 6. PLANO DE CORREÇÃO (proposto, nada aplicado)

**Opção A (recomendada): manter os .meta novos e corrigir as 2 referências quebradas.**

1. `House.prefab:147`: trocar o guid para `fd8b113177d0504a84583004208125fa` e o `fileID` para o sprite escolhido de Casa_D. Sugestão: `Casa_D_S` (`internalID 1940706880`).
2. `Warehouse.prefab:98`: trocar o guid para `c067ba3746c759018be707a7422ad173` e o `fileID` para o sprite escolhido de Fabrica_B. Sugestão: `Fabrica_B_S` (`internalID 1928123429`).
3. Abrir House, Warehouse e Guard no Unity e confirmar que `Visual/Image` mostra o sprite. Salvar só esses prefabs.
4. Não reverter os 536 .meta. Fazer isso quebraria as 3 referências novas da cena e traria de volta a tabela de sprites errada.

**Opção B (não recomendada):** restaurar os .meta do BASE com `git checkout 45f7a43 -- <png.meta>`.

- Quebra a cena (Fabrica_Q, Fabrica_T, Casa_H).
- Perde PPU 32 e a tabela correta de sprites.
- Os `Ambulancia_*` continuam inconsistentes.

**Decisões que dependem de você:**

1. Qual sprite/direção usar em House (Casa_D_S?) e Warehouse (Fabrica_B_S?). Não dá para deduzir a direção original, pois o fileID antigo era do template.
2. Manter a remoção do override WebGL e a compressão "None" (8192 máx.)? Alvo WebGL pode pedir compressão de volta.
3. Pixel Perfect: aceitar mudar o zoom do `CameraController`?
4. Confirmar que os guids novos vieram de reimportação do Unity e não de outra máquina. Se algum colaborador ainda usa o guid antigo, haverá conflito.

## 7. Status: RESOLVIDO (Opção A aplicada)

- `House.prefab` -> `Visual/Image.m_Sprite` = `Rocha_Alta_J_NE` (guid `3486ad6b…`, fileID `1730322336`). **PLACEHOLDER**, sorteado.
- `Warehouse.prefab` -> `Visual/Image.m_Sprite` = `Casa_Q_N` (guid `9f42e96f…`, fileID `1812150974`). **PLACEHOLDER**, sorteado.
- Sorteio: `awk srand(1794535202)` sobre 2154 sprites (sem `*_n.png`). Os guids e fileIDs foram conferidos contra os `.meta`.
- Decisões registradas em `decisions.md`: .meta novos mantidos, WebGL fora, câmera inalterada.

**Verificação (via MCP):**
- Console sem erros ou avisos após a importação.
- Cena Gameplay (não salva, `isDirty=false`): 375 componentes, 0 Missing Script, 0 referência perdida, 0 Missing Prefab.
- House, Warehouse e Guard: 0 referências ausentes. `Guard.prefab` exibe `Rocha_Alta_J_NE`, herdado de House.
- `git status`: somente `House.prefab` e `Warehouse.prefab` modificados.
- Efeito colateral do Unity em `House.prefab`: removida a linha `canvasGroup: {fileID: 0}`, um campo serializado antigo e nulo que não existe mais em nenhum script. Inofensivo e mantido.
