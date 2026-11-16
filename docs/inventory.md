# Inventário

Levantado em 2026-11-13 via MCP (Editor, cena Gameplay salva) e leitura de arquivos. Baseado em `CLAUDE.md`, `city-rules.md` e `decisions.md`.

Unidades:
- **u** = unidade de mundo (32 px).
- **cu** = unidade de Canvas. O Canvas `UI/Canvas` é WorldSpace com escala 0,01, então 1 cu = 0,01 u e 1 px = 3,125 cu.

## 1. Projeto

| Item | Valor |
|---|---|
| Layers | 0 Default, 1 TransparentFX, 2 Ignore Raycast, 3 **Houses**, 4 Water, 5 UI, 6 **Polygons** |
| Sorting layers | Default, UI |
| Tags | padrão do Unity (Untagged, MainCamera…), nenhuma custom |
| Import de sprites (`Assets/Arts/Houses`) | 2144 de 2154 sprites com PPU 32, Point, sem compressão, secondary texture `_NormalMap` (`*_n.png`). Exceção: `GenericHouses.png` (10 sprites, PPU 100, Bilinear, comprimido), fora de todos os pools. |

## 2. Prefabs

| Prefab | Caminho / guid | Estrutura e dimensões | Campos relevantes | Regras |
|---|---|---|---|---|
| **House** | `Assets/Systems/Houses/House.prefab` · `e871174820cb757499015537eb91b875` | Raiz: `Image` (sem sprite, raycast on), `BoxCollider2D` 1×1 cu (0,01 u), `House`. RectTransform 218,69×218,69 cu (2,19 u), pivot 0,5/0,5, layer Houses, Untagged. Filho `Visual`: `Image` + `Tweener`, stretch (âncoras 0–1), preserveAspect on, material `HouseMAT`, sprite PLACEHOLDER `Rocha_Alta_J_NE`. Também `Visual/Progress`, `Complete`, `Call` (off) e `Canvas` (WorldSpace, override sorting 10, off) com `Tab/InfoPanel`. | `sprite`=Visual, `tweener`, **`entry`=null**, `interactable`, `currentState`, `workTime`=10 | R01, R04, R05, R06, R11, R12, R14, R30, R31 |
| **Guard** | `Assets/Systems/Houses/Guard.prefab` · `43e32c91ec723554a990054769e5a142` | **Variant de House**, com o mesmo layout. Componente `CityGuard : Location : House`. `Canvas` ativo e `Tab` off. Visual herda `Rocha_Alta_J_NE`. | `data`=`Guarda` (LocationData), `infoPanel`=Tab, `entry`=null, `workTime`=0 | R02, R04, R05, R07, R11–R14, R30, R31 |
| **Warehouse** | `Assets/Systems/Houses/Warehouse.prefab` · `3836ca3b22b1c7c4b96d2f08bf736b63` | **Variant de House**, com o mesmo layout. Componente `Warehouse : Location : House`. Visual: PLACEHOLDER `Casa_Q_N`. | `data`=`Armazém`, `infoPanel`=Tab, `entry`=null | R03, R04, R05, R07, R11–R14, R30, R31 |
| **Polygon** | `Assets/Systems/Houses/Polygon.prefab` · `fdb8a814332cae343aebe0b2a3cc3ebe` | `PolygonCollider2D` (4 pontos, ~1,66×1,44 u), `MeshRenderer` (material `PolygonMAT`, sorting Default 0), `MeshFilter`, `PolygonMesh2D` (ExecuteInEditMode, refaz a mesh a partir do path 0 do collider). Layer Polygons, Untagged. | `zPosition`=0 | R22–R26 |
| **Waypoint** | `Assets/Systems/Navigation/Waypoint.prefab` · `7491c71e82d69c943babcd6767d72def` | Só `Transform` (não RectTransform), escala 108, z local −9720. Sob `Waypoints` (escala 0,01) a escala no mundo fica 1,08. Layer Default. | `neighbors`=[] | R14, R15 |
| Worker | `Assets/Systems/Resources/Worker.prefab` | `Image` 40,24×40,24 cu = **0,40 u**, layer UI. | — | R25 (referência da largura de rua) |
| UpgradeButton | `Assets/Systems/Upgrades/UpgradeButton.prefab` | UI, sem relação com a cidade. | — | — |

O pivot dos sprites é ignorado pela `Image`. Valem o pivot e o retângulo do RectTransform. Para R11, o tamanho nativo de uma estrutura é obtido com o `sizeDelta` da raiz = px do sprite × 3,125 cu, e o `Visual` estica junto.

## 3. ScriptableObjects

| Asset | guid | Campos | Usado por | Regras |
|---|---|---|---|---|
| `Assets/Resources/Locations/Guarda.asset` (LocationData) | `84b24396f6af6f34f90eeacd4c4a7b82` | description "Uma base que abriga os trabalhadores da guarda."; specific "Sua estrutura principal" | Guard.prefab `data` | R02 |
| `Assets/Resources/Locations/Armazém.asset` (LocationData) | `e6dbb8cfb8082fc47b58d188f019f385` | description "Envie trabalhadores para cá para coletar materiais."; specific "1 por trabalhador" | Warehouse.prefab `data` | R03 |

`LocationData` (`Assets/SO/LocationData.cs`) só tem `description` e `specific` (strings). Não existem outros SOs ligados à cidade.

## 4. Materiais, shaders e texturas

| Item | Caminho | Detalhes | Uso / regras |
|---|---|---|---|
| PolygonMAT | `Assets/Shaders & Materials/PolygonMAT.mat` · `f32a3a6b…` | Shader `Sprite Shaders Ultimate/GUI SSU`, cor (0,336; 0,336; 0,336), textura `SSU_Noise_1K` | Polygon.prefab · R22 |
| HouseMAT | `Assets/Shaders & Materials/HouseMAT.mat` · `bc1ab019…` | Shader `GUI SSU`. `House.Awake` instancia uma cópia e usa `_TextureLayer1Fade` para o estado "Damaged". | Visual das estruturas; AnalyticsManager `houseMat` |
| TextShadowMAT | `Assets/Shaders & Materials/TextShadowMAT.mat` | `Standard SSU` | sombra do contador de dano |
| MapRender | `Assets/Arts/MapRender.renderTexture` | 640×360, Point, R8G8B8A8 | alvo da PolygonCamera · R28 |

## 5. Câmeras e exibição dos chãos (R28, intocáveis)

| Objeto | Caminho na cena | Dados |
|---|---|---|
| Main Camera | `Essentials/Main Camera` | Mundo (−3,44; −0,15; −10), ortho 5, culling 0–5 (**não** vê Polygons), tag MainCamera, `CameraController` (pan com botão direito/meio, zoom, limita pelo `Limiter`) |
| PolygonCamera | `Essentials/Main Camera/PolygonCamera` | Mesma posição, ortho 5, culling só Polygons, alvo `MapRender`, fundo transparente |
| PolygonsDisplay | `UI/Canvas/UIContainer/PolygonsDisplay` | `RawImage` com `MapRender`, stretch. Retângulo no mundo de (−12,32; −5,15) a (5,45; 4,85) = 17,77×10 u, z 90 |

## 6. Pais da cena que o gerador usa

| Pai | Caminho | Mundo / escala | Estado atual | Regras |
|---|---|---|---|---|
| Houses | `UI/Canvas/UIContainer/Houses` | (−3,44; −0,15; 90), escala 0,01, layer Houses, sibling 1 | 0 filhos | R01–R04, R26, R30 |
| Trees | `UI/Canvas/UIContainer/Trees` | Idem, sibling 2 (desenhado sobre Houses) | 0 filhos | R16, R26, R30 |
| Waypoints | `UI/Canvas/UIContainer/Waypoints` | Idem, layer UI, sibling 5 | 3 Waypoints pré-existentes | R14, R15 |
| PolygonLayer | `Essentials/Main Camera/PolygonLayer` | (−3,44; −0,15; −10), escala 1, layer Polygons | `Polygon` (instância do prefab, 4 pts, antigo) + `Canvas/BG` (exceção fixa) | R22–R26 |
| Limiter | `Essentials/Limiter` | `SpriteRenderer` Square (PPU 256). Centro (−3,55; −0,09), tamanho 29,34×19,97 u | Lido por `CameraController.GetBounds` | R27 |
| UIContainer | `UI/Canvas/UIContainer` | Pai comum. Contém também `House`, `Guard` e `Warehouse` antigos (fora de Houses), além de `Pole` (off), `Workers` e `PolygonsDisplay` | — | R04, R26 |

## 7. Componentes e scripts com que a cidade se liga

| Sistema | Script / objeto | Ligação com a cidade | Regras |
|---|---|---|---|
| Houses | `House.cs`, `Location.cs` (abstrata), `CityGuard.cs`, `Warehouse.cs`, `InfoPanel.cs`, `LocationsManager.cs` (vazio) | Estruturas são `House` ou derivadas. `entry` (Waypoint) é a origem e o destino dos trabalhadores. | R01–R07, R14 |
| Navigation | `GPS.cs` (`Scripts/GPS`: `allWaypoints`=[0], `allLocations`=[0]), `Pathfinder.cs` (A* por `Waypoint.neighbors`), `Waypoint.cs` | `GuardManager` chama `GPS.FindShortestPath(guard.entry, house.entry)`. As listas do GPS estão vazias e a ligação entre Waypoints fica para a etapa do GPS. | R14 |
| Map Design | `WaypointCreator.cs` (`wpPf`=Waypoint, `waypointsParent`=Waypoints), `PolygonMesh2D.cs` | Ferramentas de edição manual. | R14, R22 |
| Guard | `GuardManager.cs` (`workerPf`=Worker, `workersParent`=Workers) | Instancia Workers em `guard.entry` e segue o caminho até `house.entry`. | R02, R14 |
| Resources | `ResourcesManager.cs` | Materiais e dinheiro creditados ao concluir chamados de House/Warehouse. | R03 |
| Eletricity | `EletricityManager.cs` (`allPoles`=[1]), `EletricPole.cs` | `Physics2D.OverlapCircleAll(range, layer "Houses")` → `House.Energize`. Depende do `BoxCollider2D` e da layer Houses das estruturas. | R31 |
| Analytics | `AnalyticsManager.cs` (`houseMat`=HouseMAT) | Modo de visualização sobre o material das casas. | — |
| Camera | `CameraController.cs` | Limita o pan ao `Limiter`. | R27, R28 |

## 8. Sprites de `Assets/Arts/Houses` por categoria

Total de 268 sprites `_S` (1 por textura). Cada textura tem 8 direções (`_E`, `_N`, `_NE`, `_NW`, `_S`, `_SE`, `_SW`, `_W`) numa folha vertical.

| Categoria | Texturas | No pool de alguma regra | Fora dos pools |
|---|---|---|---|
| Suburban | 26 | 21 `Casa_A..U` (House, R06) | Arvore_Quintal, Arvore_Quintal_Baixa, Cerca_Alta, Cercado, Jardineira |
| Commercial | 34 | 33 (`Predio_A..N`, `Arranha_Ceu_A/C/E`, `Fundo_A..N`, `Fundo_Largo_A/B`), House R06 | Guarda_Sol |
| Industrial | 31 | 20 `Fabrica_A..T` (House R06, Guard/Warehouse R07) | Caixa_Dagua, Chamine, Chamine_Baixa, Chamine_Listrada, Conteiner, Painel_Solar, Painel_Solar_Plano, Tanque, Tanque_Grande, Torre_Resfriamento, Turbina_Eolica |
| Nature | 125 | 96 (decoração, R17) | 29 excluídos por R17: Abobora, Barraca, Barraca_Aberta, Canoa, Cenoura, Fogueira, Fogueira_Pedras, Fogueira_Tabuas, Fogueira_Tijolos, Horta_1, Horta_2, Lenha, Melancia, Milho_1–4, Nabo, Placa_Madeira, Ponte_Madeira, Ponte_Madeira_Arco, Ponte_Pedra, Ponte_Pedra_Arco, Remo, Saco_Dormir, Trigo_1, Trigo_2, Vitoria_Regia, Vitoria_Regia_Flor |
| Carros | 32 | — | todas (nenhuma regra usa) |
| Roads | 20 | — | todas (nenhuma regra usa) |

Dimensões dos pools (`_S`, tamanho nativo):

| Pool | Nº | Largura (u) | Altura (u) | Média (u) | Pivot Y do sprite |
|---|---|---|---|---|---|
| House/Suburban | 21 | 3–4 | 3,5–4 | 3,50×3,69 | 0,277–0,384 |
| House/Commercial | 33 | 1,5–3,5 | 3–4 | 2,32×3,65 | 0,125–0,357 |
| House/Industrial (também Guard/Warehouse) | 20 | 3–4 | 3,5–4 | 3,48×3,77 | 0,25–0,402 |
| Decoração/Nature | 96 | 1–3 | 1,5–3 | 2,07×2,44 | 0,125–0,453 |

### Tabelas por sprite (pools das regras)

#### Suburban — House (R06, R09) — 21 sprites

| Sprite | px | u | pivot (x,y) | guid / fileID |
|---|---|---|---|---|
| Casa_A_S | 112×112 | 3.5×3.5 | 0.5, 0.357 | a2fd02e26a8f5b31a65f44cdce840855 / 843905557 |
| Casa_B_S | 112×128 | 3.5×4 | 0.5, 0.336 | 1490485c04fa5141834eb1c102c9d124 / 541634334 |
| Casa_C_S | 112×112 | 3.5×3.5 | 0.5, 0.366 | 22e9f40575865a418fa5c2a4ffb651c2 / 1609521399 |
| Casa_D_S | 112×128 | 3.5×4 | 0.5, 0.297 | fd8b113177d0504a84583004208125fa / 1940706880 |
| Casa_E_S | 112×128 | 3.5×4 | 0.5, 0.289 | 2d48160284a85bc7907d64846d44ee6c / 1532176661 |
| Casa_F_S | 112×112 | 3.5×3.5 | 0.5, 0.366 | cdb141a29e745e78bd8569bf16372218 / 1075222614 |
| Casa_G_S | 128×112 | 4×3.5 | 0.5, 0.375 | 12a5ff60b497520fb294fe99dff22e74 / 820517663 |
| Casa_H_S | 112×112 | 3.5×3.5 | 0.5, 0.348 | 446678dcd6f452c79da5de4818414d8a / 1275587325 |
| Casa_I_S | 112×112 | 3.5×3.5 | 0.5, 0.366 | c26445edf71058359399bf5a07be8f07 / 1757483822 |
| Casa_J_S | 112×112 | 3.5×3.5 | 0.5, 0.33 | 74febb45d8cb5090b453bcd2475564c5 / 1664568099 |
| Casa_K_S | 96×112 | 3×3.5 | 0.5, 0.277 | 820241118f51527eafbe72f39bfa110a / 1137648778 |
| Casa_L_S | 96×112 | 3×3.5 | 0.5, 0.33 | 0d29af9a23595152b2cb1a2f02fb43df / 191894665 |
| Casa_M_S | 128×112 | 4×3.5 | 0.5, 0.384 | 111349d72c795d8ba7ddc287a059d113 / 1326517771 |
| Casa_N_S | 128×128 | 4×4 | 0.5, 0.352 | 288651ee31cf5161a42b2f7bd50a11ed / 1699042998 |
| Casa_O_S | 112×128 | 3.5×4 | 0.5, 0.297 | 27840e1f7d06522f9b02f84651dff460 / 1125730821 |
| Casa_P_S | 112×112 | 3.5×3.5 | 0.5, 0.348 | 8288a3eefb6250c08c8edfb8e68ef44b / 159951510 |
| Casa_Q_S | 96×112 | 3×3.5 | 0.5, 0.33 | 9f42e96fa88c5983a53bb9e87d0109d4 / 694461547 |
| Casa_R_S | 112×128 | 3.5×4 | 0.5, 0.281 | 7b81246b0a825bd188a3f04b1c5c5407 / 1001716742 |
| Casa_S_S | 112×128 | 3.5×4 | 0.5, 0.32 | 577560e5014155f382263a2ae1f264ca / 627606367 |
| Casa_T_S | 112×128 | 3.5×4 | 0.5, 0.328 | 17f81f34157a5ac5a5872b60b11c8a76 / 1795143866 |
| Casa_U_S | 112×112 | 3.5×3.5 | 0.5, 0.348 | fe01414fa49b5b4b8217dec223ce8d20 / 156420721 |

#### Commercial — House (R06, R09) — 33 sprites

| Sprite | px | u | pivot (x,y) | guid / fileID |
|---|---|---|---|---|
| Arranha_Ceu_A_S | 80×128 | 2.5×4 | 0.5, 0.203 | d1ff1aedda5d52c0ba2f9ae6587a90e4 / 248706753 |
| Arranha_Ceu_C_S | 64×128 | 2×4 | 0.5, 0.188 | a1a0f3e85bb05e06902386f78cc0d34c / 942352068 |
| Arranha_Ceu_E_S | 64×128 | 2×4 | 0.5, 0.18 | 6123836799145e518aa23e0de70003c8 / 1029678330 |
| Fundo_A_S | 48×112 | 1.5×3.5 | 0.5, 0.125 | e37655b7ce8a531589420643f2b6aac7 / 765527774 |
| Fundo_B_S | 48×112 | 1.5×3.5 | 0.5, 0.161 | 5b39c65e4acd5c7b8e01bf61b247201c / 148246 |
| Fundo_C_S | 48×112 | 1.5×3.5 | 0.5, 0.125 | 976b7da8d8a75d17930880df0eee9de3 / 596096119 |
| Fundo_D_S | 64×112 | 2×3.5 | 0.5, 0.179 | 55afda66602756f9a432d92334b452e1 / 925591835 |
| Fundo_E_S | 48×112 | 1.5×3.5 | 0.5, 0.161 | 7cf10ff13a1e51bba819ca3fc2f25c72 / 406690516 |
| Fundo_F_S | 48×112 | 1.5×3.5 | 0.5, 0.161 | 1327fc871e1e57a1b60acd2762b14715 / 1537907547 |
| Fundo_G_S | 48×112 | 1.5×3.5 | 0.5, 0.152 | 4d1cb619dca1562bbbf3dac3798c022f / 318952549 |
| Fundo_H_S | 48×112 | 1.5×3.5 | 0.5, 0.125 | 1867631b9f72535bb3a8cb20c10d23a0 / 1835165726 |
| Fundo_I_S | 64×112 | 2×3.5 | 0.5, 0.179 | dc1e42c9ad3258c188c4a37e551c6590 / 694675017 |
| Fundo_J_S | 48×112 | 1.5×3.5 | 0.5, 0.17 | d6d32c4815b0530e85741c5ac9ad6036 / 1507233780 |
| Fundo_K_S | 64×112 | 2×3.5 | 0.5, 0.179 | ddc3e47bb88c5d338af341323cad2e8f / 1076540836 |
| Fundo_L_S | 64×112 | 2×3.5 | 0.5, 0.17 | 20633eb2f726553880f02ef82d235f27 / 606583463 |
| Fundo_Largo_A_S | 80×112 | 2.5×3.5 | 0.5, 0.232 | 37e604420f885d9082b392c17c766047 / 1572764578 |
| Fundo_Largo_B_S | 80×112 | 2.5×3.5 | 0.5, 0.241 | 6c0910dc81ac587e8434a44e7afd8232 / 1493550223 |
| Fundo_M_S | 64×112 | 2×3.5 | 0.5, 0.143 | bedf42964b6f566ba9feacd0035c5410 / 759787232 |
| Fundo_N_S | 80×96 | 2.5×3 | 0.5, 0.302 | ba57a7512d8552ccbda6c857bc0f2470 / 1515239051 |
| Predio_A_S | 80×112 | 2.5×3.5 | 0.5, 0.277 | 1d6c0329376253c69e069c1b9fb1a9fb / 2756679 |
| Predio_B_S | 96×128 | 3×4 | 0.5, 0.258 | fb6a82b03c295f54bca63c45b3d24e0d / 717686242 |
| Predio_C_S | 96×112 | 3×3.5 | 0.5, 0.33 | dc404e3517c950ac9e71d67c78c19806 / 1869396514 |
| Predio_D_S | 80×112 | 2.5×3.5 | 0.5, 0.259 | 35f55f00d3a454f2ac60adb86cba7d46 / 288263608 |
| Predio_E_S | 112×112 | 3.5×3.5 | 0.5, 0.357 | 614166b908095d11bd1b37c426fce65a / 1679687400 |
| Predio_F_S | 80×112 | 2.5×3.5 | 0.5, 0.241 | ff0107185c9255d58374e29a6c000b74 / 904694436 |
| Predio_G_S | 80×128 | 2.5×4 | 0.5, 0.219 | 3eb493182a7f5811a7ceb5299671f326 / 1796478564 |
| Predio_H_S | 80×112 | 2.5×3.5 | 0.5, 0.277 | 26f6a7ce051154f6926e845445958fbe / 1933755152 |
| Predio_I_S | 96×128 | 3×4 | 0.5, 0.258 | 3e447c3df4c25bc7a3e5765cd267d4d7 / 909717681 |
| Predio_J_S | 96×128 | 3×4 | 0.5, 0.289 | 603c4188acc755669b3da99426960312 / 1885889133 |
| Predio_K_S | 112×128 | 3.5×4 | 0.5, 0.305 | 237bd50f33eb590682112d469c5e1688 / 86978828 |
| Predio_L_S | 96×128 | 3×4 | 0.5, 0.258 | 902b34d879775cdc9c5b9bdffdeffd38 / 487958705 |
| Predio_M_S | 80×128 | 2.5×4 | 0.5, 0.219 | 5f53e9bcb6ab5922853b19ed949f395e / 364652007 |
| Predio_N_S | 112×128 | 3.5×4 | 0.5, 0.313 | c59dae3f1a0d5e9e96b64888236306a5 / 1389217546 |

#### Industrial — House, Guard, Warehouse (R06, R07, R09) — 20 sprites

| Sprite | px | u | pivot (x,y) | guid / fileID |
|---|---|---|---|---|
| Fabrica_A_S | 112×128 | 3.5×4 | 0.5, 0.313 | fd8c59f5a7c850a9a6e40ca9a22feeed / 232963410 |
| Fabrica_B_S | 112×128 | 3.5×4 | 0.5, 0.32 | c067ba3746c759018be707a7422ad173 / 1928123429 |
| Fabrica_C_S | 128×112 | 4×3.5 | 0.5, 0.402 | 0057b3e8b7d85df994b8ce00f1ffbfa5 / 1751243244 |
| Fabrica_D_S | 96×128 | 3×4 | 0.5, 0.273 | 222f91c23ae35265877692a87e7c453d / 409129086 |
| Fabrica_E_S | 96×128 | 3×4 | 0.5, 0.289 | 02c0dcccd4685143aef8c84cdb03d3b6 / 1347683829 |
| Fabrica_F_S | 96×128 | 3×4 | 0.5, 0.289 | ba8aa1430b0352c98a383d2d58449836 / 1084664516 |
| Fabrica_G_S | 112×128 | 3.5×4 | 0.5, 0.305 | 749705cfb95e5c099b140054a0d26bc8 / 1054506658 |
| Fabrica_H_S | 112×112 | 3.5×3.5 | 0.5, 0.366 | 9e2f34282c865ba69d2205eec2433c6f / 1245381580 |
| Fabrica_I_S | 112×112 | 3.5×3.5 | 0.5, 0.366 | 83609ff08c605984ae8d19a7eac6afd2 / 973786026 |
| Fabrica_J_S | 112×112 | 3.5×3.5 | 0.5, 0.366 | 0eeff8358c005907a1e594c094de0a44 / 1375397786 |
| Fabrica_K_S | 112×112 | 3.5×3.5 | 0.5, 0.339 | 4f9da46e05ca5d8ca7845c2600c653cb / 1717258300 |
| Fabrica_L_S | 128×128 | 4×4 | 0.5, 0.344 | e9f54b200f7d5efcbb2bcd65fe4c95f8 / 956136832 |
| Fabrica_M_S | 112×112 | 3.5×3.5 | 0.5, 0.357 | 17b4c12c337252148aa71c262b073344 / 805828156 |
| Fabrica_N_S | 96×128 | 3×4 | 0.5, 0.25 | 013fe3b17fb9502f8cf200593dcfb23b / 1123487718 |
| Fabrica_O_S | 96×112 | 3×3.5 | 0.5, 0.33 | bd92e1520cc9575ba7351e99f8bb50be / 554458460 |
| Fabrica_P_S | 112×112 | 3.5×3.5 | 0.5, 0.384 | a7b85cc4f06751378e02f16fa2af92a5 / 244664989 |
| Fabrica_Q_S | 128×128 | 4×4 | 0.5, 0.359 | 947720a5782c517facb531cafd04ceb5 / 519443429 |
| Fabrica_R_S | 128×128 | 4×4 | 0.5, 0.344 | 4b6678e9f2865c9a95a6d60e95106278 / 351996244 |
| Fabrica_S_S | 112×112 | 3.5×3.5 | 0.5, 0.384 | 3187fa9621b2597298552febf5f32967 / 1765044658 |
| Fabrica_T_S | 112×128 | 3.5×4 | 0.5, 0.336 | 80505bffeefa5c539d07c429b2198070 / 528640552 |

#### Nature — decoração (R17) — 96 sprites

| Sprite | px | u | pivot (x,y) | guid / fileID |
|---|---|---|---|---|
| Arbusto_Folhas_S | 80×80 | 2.5×2.5 | 0.5, 0.263 | d25d8de4ac3459a3933e4c338b17a698 / 1763734945 |
| Arbusto_Grande_S | 64×64 | 2×2 | 0.5, 0.313 | cc96d9866d0f5d66a042e77f141cde39 / 1570075672 |
| Arbusto_Pequeno_S | 64×64 | 2×2 | 0.5, 0.313 | 51893f4e1d4c5e109a19dac9fab52507 / 487105088 |
| Arbusto_Pontudo_Grande_S | 80×64 | 2.5×2 | 0.5, 0.359 | 95562f08bb055a0881cb2df63eefb476 / 1145135561 |
| Arbusto_Pontudo_S | 80×80 | 2.5×2.5 | 0.5, 0.25 | 757d1a4b52995c8b81f10864253d4372 / 2139123637 |
| Arbusto_S | 64×64 | 2×2 | 0.5, 0.313 | 7878d2b7c9035bb69cd5c580ee54c55c / 233404546 |
| Arvore_Alta_S | 48×96 | 1.5×3 | 0.5, 0.125 | c3eb22a9a8615bb785c88451ffd60cb7 / 421310470 |
| Arvore_Blocos_S | 64×96 | 2×3 | 0.5, 0.135 | 0c033361293a583f9c5496cc30431977 / 294240692 |
| Arvore_Chapeu_S | 64×96 | 2×3 | 0.5, 0.146 | 820fdf6b3a2458e0a68feb4593b3cb1e / 579367329 |
| Arvore_Cone_S | 48×96 | 1.5×3 | 0.5, 0.146 | 45e91a18f6b45a1098f89e1abe41b7f5 / 329313038 |
| Arvore_Fina_S | 64×96 | 2×3 | 0.5, 0.135 | 93cb8dcde9f850e4b337cb0ea79d173a / 1726998636 |
| Arvore_Galhos_S | 80×96 | 2.5×3 | 0.5, 0.135 | e280a7fa49ae5fea82a4d3d8a415b17b / 1773044825 |
| Arvore_Gorda_S | 80×96 | 2.5×3 | 0.5, 0.146 | a035a3c8b56d5121aefc03c4caedd501 / 1589970166 |
| Arvore_Redonda_S | 48×96 | 1.5×3 | 0.5, 0.167 | 6c76afdd0dba5e4da5206df01d29ac73 / 1251708043 |
| Arvore_S | 64×96 | 2×3 | 0.5, 0.125 | dda21776010c5cf6a84cdd6a528cd18f / 25030798 |
| Arvore_Simples_S | 48×96 | 1.5×3 | 0.5, 0.146 | dba5570073295df5b4fbf58cb4c2198b / 1461892012 |
| Bambu_1_S | 64×80 | 2×2.5 | 0.5, 0.25 | a4b9375663e85fe0bd421bd216696dbb / 1097180422 |
| Bambu_2_S | 48×96 | 1.5×3 | 0.5, 0.167 | 4758303115b65f16868d572c35e425d9 / 189815986 |
| Cacto_Alto_S | 64×96 | 2×3 | 0.5, 0.177 | 47f3facfacff55f296ee7daa3e255cd3 / 1006904328 |
| Cacto_Baixo_S | 64×80 | 2×2.5 | 0.5, 0.25 | dc42052c91a654d4a030da21b699dbd2 / 701742189 |
| Capim_S | 64×64 | 2×2 | 0.5, 0.313 | de1d49015167519a8d5684f5d393660e / 2039504835 |
| Carvalho_S | 64×96 | 2×3 | 0.5, 0.135 | 7aec8a11510d5c9d9b81895160ed3d2b / 713271649 |
| Cerca_Rustica_S | 80×80 | 2.5×2.5 | 0.5, 0.388 | 8a1af1add4a65811bd43a9884692e30f / 1064051229 |
| Cerca_Tabuas_Dupla_S | 80×80 | 2.5×2.5 | 0.5, 0.388 | e578f64fdec55e96afcef0169bd39761 / 185229145 |
| Cerca_Tabuas_S | 80×80 | 2.5×2.5 | 0.5, 0.35 | 60f05f93e01558b4a6dabd79d99b70fb / 255366985 |
| Cerca_Trilho_S | 96×80 | 3×2.5 | 0.5, 0.388 | bef279afc7655803a6dbe9a35a172d15 / 1790727879 |
| Cogumelo_Alto_S | 32×48 | 1×1.5 | 0.5, 0.417 | efde3a00c673587f9d5b54529adf3024 / 1332653660 |
| Cogumelo_Chapeu_S | 32×48 | 1×1.5 | 0.5, 0.417 | 343ae10cc4dd55f0ab777ae1a4864333 / 874161450 |
| Cogumelo_Grupo_S | 48×64 | 1.5×2 | 0.5, 0.313 | 377ce3b5f3605e1b81b47948a225ec5f / 932191299 |
| Cogumelo_S | 32×48 | 1×1.5 | 0.5, 0.417 | 871e19c9e1e25b41bf7530778ed8c0ef / 1272946778 |
| Coluna_Quebrada_S | 48×80 | 1.5×2.5 | 0.5, 0.225 | 11d28103a0465e7487d56aee08e0b253 / 1215263773 |
| Coluna_S | 48×80 | 1.5×2.5 | 0.5, 0.188 | 573aebaac9195e5ebc66a4e7caccc3b9 / 529256987 |
| Estatua_Anel_S | 64×96 | 2×3 | 0.5, 0.26 | e76a3a1ebc4d5606ace57313dd847732 / 1365227307 |
| Estatua_Bloco_S | 64×80 | 2×2.5 | 0.5, 0.263 | eb763e1d58bd59c087d6bf4539c48109 / 1209985222 |
| Estatua_Cabeca_S | 64×96 | 2×3 | 0.5, 0.208 | 4f4b9afbe47652b985042c111b68e54f / 2027571530 |
| Flor_Margarida_S | 32×48 | 1×1.5 | 0.5, 0.417 | 64ad9230fac755d3a1fccbcc46a094d8 / 1374571299 |
| Flor_Sino_S | 48×48 | 1.5×1.5 | 0.5, 0.417 | cdb3a6927d3252e786074e8de93d212c / 1339784369 |
| Flor_Tulipa_S | 48×64 | 1.5×2 | 0.5, 0.313 | e692b1f8c94256f18e25f66861285f1d / 255512757 |
| Folhagem_S | 48×48 | 1.5×1.5 | 0.5, 0.417 | 8745a898d6125118af857ab5ba79b42b / 840882045 |
| Obelisco_S | 48×80 | 1.5×2.5 | 0.5, 0.225 | 033ad584d1d85c84a49ec880efcfb702 / 1623638383 |
| Palmeira_Cheia_S | 80×96 | 2.5×3 | 0.5, 0.125 | 9aab406c6adc57ddaab557df06f60545 / 951441689 |
| Palmeira_Cruz_S | 80×96 | 2.5×3 | 0.5, 0.125 | ea3ce57a368d54feac793d6bd39f13d4 / 139576670 |
| Palmeira_Curva_S | 80×96 | 2.5×3 | 0.5, 0.125 | a47429b223635f038ea15852dc38e7d3 / 1227183695 |
| Palmeira_S | 80×96 | 2.5×3 | 0.5, 0.125 | c417425b9a1f59219a00a267f5719bd1 / 518560612 |
| Pedra_A_S | 64×64 | 2×2 | 0.5, 0.375 | aa4f7587504551d0b7ca4fe7661ece7c / 574222811 |
| Pedra_B_S | 80×64 | 2.5×2 | 0.5, 0.422 | 1ce631a1b56d592bb524577787a226dc / 1553786949 |
| Pedra_C_S | 64×48 | 2×1.5 | 0.5, 0.417 | a84c2829f12c54bfbcf567f2f6c6d9c4 / 1360158811 |
| Pedra_Chata_A_S | 80×64 | 2.5×2 | 0.5, 0.406 | 469780c1c727507e9756fb57b1ade963 / 1697330834 |
| Pedra_Chata_B_S | 80×64 | 2.5×2 | 0.5, 0.453 | c80f2896a04854b8b717341f2a13f9ff / 1266930800 |
| Pedra_Chata_C_S | 80×64 | 2.5×2 | 0.5, 0.391 | 765309e0158350b0a20765b766b8af0a / 666628314 |
| Pedra_D_S | 64×64 | 2×2 | 0.5, 0.359 | 6e437b85a71e5b4e8226ea79b6d27b6e / 823564718 |
| Pedra_E_S | 64×64 | 2×2 | 0.5, 0.313 | 200855c4a8565d5680f0ea5edd65c368 / 664362687 |
| Pedra_F_S | 64×64 | 2×2 | 0.5, 0.313 | aa120f70292d57e2a98cd1f5be275fe6 / 3619318 |
| Pedra_G_S | 80×64 | 2.5×2 | 0.5, 0.391 | a4bb1caa97d4574e8187dd0fe89b3243 / 1223065836 |
| Pedra_Grande_A_S | 96×80 | 3×2.5 | 0.5, 0.375 | 888f58c554e3593d908500f905550dc3 / 736324182 |
| Pedra_Grande_B_S | 96×80 | 3×2.5 | 0.5, 0.338 | 2f5aa69e20815b7b8b75e5a20ccda7e3 / 856173906 |
| Pedra_Grande_C_S | 96×80 | 3×2.5 | 0.5, 0.363 | dc24d60835c45ab1ac0089213fea26c3 / 299578643 |
| Pedra_Grande_D_S | 96×80 | 3×2.5 | 0.5, 0.35 | 6df2f947023358fb97a20c6a58a59bd1 / 2107317890 |
| Pedra_Grande_E_S | 96×80 | 3×2.5 | 0.5, 0.35 | 9c8f4858d92755b2be1f675c54c62f3b / 885890491 |
| Pedra_Grande_F_S | 96×80 | 3×2.5 | 0.5, 0.338 | 59c13091169c5178ad4835bbf9212364 / 1425651208 |
| Pedra_H_S | 64×80 | 2×2.5 | 0.5, 0.25 | 7f4bacbae70e51918e7820144e085df3 / 912550926 |
| Pedra_I_S | 64×64 | 2×2 | 0.5, 0.313 | 6f1812417b1059c48c2d015b508bb77d / 1122983472 |
| Pedra_Topo_A_S | 80×64 | 2.5×2 | 0.5, 0.453 | d351618628155b0e8007a2f3c4966ba4 / 2020765550 |
| Pedra_Topo_B_S | 96×80 | 3×2.5 | 0.5, 0.4 | 7ec2e413310455eb98bc744c1d0f623b / 209610526 |
| Pinheiro_Alto_A_S | 48×96 | 1.5×3 | 0.5, 0.146 | 8830db7624c05b85b0d5301e367f9b0f / 706173264 |
| Pinheiro_Alto_C_S | 48×96 | 1.5×3 | 0.5, 0.135 | 954ca49979ec5cd8b36c348ffd656cf7 / 692570825 |
| Pinheiro_Baixo_S | 64×80 | 2×2.5 | 0.5, 0.263 | 784531e9311a58aaafa91d17216a0021 / 1809248365 |
| Pinheiro_Pequeno_A_S | 64×80 | 2×2.5 | 0.5, 0.2 | 0502dea0ebec50f892bc9d0cd9fd8cf7 / 467937627 |
| Pinheiro_Pequeno_B_S | 64×80 | 2×2.5 | 0.5, 0.2 | d66f43bb3a375746ac5ae661fc7a47a9 / 648005778 |
| Pinheiro_Pequeno_C_S | 64×96 | 2×3 | 0.5, 0.156 | 9e01a589adc75148ad2cca7ceb7ce2a7 / 491154583 |
| Pinheiro_Redondo_A_S | 64×96 | 2×3 | 0.5, 0.146 | 3080aefc65a8537592b17aed55d61b85 / 1960595884 |
| Pinheiro_Redondo_B_S | 64×96 | 2×3 | 0.5, 0.146 | c6a506bf791654b587b358c7c7a11a87 / 963891644 |
| Pinheiro_Redondo_C_S | 64×96 | 2×3 | 0.5, 0.146 | fb1a59c7e64251dca011494c9a2a01df / 468536248 |
| Pinheiro_Redondo_E_S | 64×96 | 2×3 | 0.5, 0.156 | 12796fe1f7ec52e1b9572ac5c7399c22 / 929277100 |
| Pinheiro_S | 48×96 | 1.5×3 | 0.5, 0.146 | 81942115808c53d8ad8253187490be86 / 1048741452 |
| Planta_Alta_S | 48×64 | 1.5×2 | 0.5, 0.313 | 302e2834d3bc511ca27e5f8a746986d6 / 2079076457 |
| Planta_Baixa_S | 48×48 | 1.5×1.5 | 0.5, 0.417 | 8a7628b57ffd5b62bb666a6409dcba55 / 1883040557 |
| Porteira_S | 80×80 | 2.5×2.5 | 0.5, 0.388 | ffeaf08efec459b59518f51b84be0057 / 1268079929 |
| Rocha_Alta_A_S | 80×96 | 2.5×3 | 0.5, 0.281 | 10cc8611ce5e56c1b1f862cd166df2c5 / 1705733681 |
| Rocha_Alta_B_S | 80×96 | 2.5×3 | 0.5, 0.302 | 7ae64422c8a256c6a59c4c551705e8c7 / 1353880343 |
| Rocha_Alta_C_S | 64×80 | 2×2.5 | 0.5, 0.263 | d492dd5eb7a55a2a9ea25cc208e38f34 / 1340703986 |
| Rocha_Alta_D_S | 64×80 | 2×2.5 | 0.5, 0.25 | 35f68cfb0df95d888d3407eb122e2074 / 733095202 |
| Rocha_Alta_E_S | 64×80 | 2×2.5 | 0.5, 0.313 | f2954da288025879bd668f942e6dd4bf / 8597729 |
| Rocha_Alta_F_S | 64×80 | 2×2.5 | 0.5, 0.275 | 95c13891251b508dac2db2362ca518e5 / 152348739 |
| Rocha_Alta_G_S | 48×80 | 1.5×2.5 | 0.5, 0.238 | dae662ddd5c150d2abc1ad9e3a66c937 / 919949497 |
| Rocha_Alta_H_S | 64×80 | 2×2.5 | 0.5, 0.3 | 4c165f2a2c5a5612b00a499d9f87d334 / 1120718711 |
| Rocha_Alta_I_S | 64×80 | 2×2.5 | 0.5, 0.25 | f548d38c362e5a6797f8c0ed218ec41a / 910596552 |
| Rocha_Alta_J_S | 64×80 | 2×2.5 | 0.5, 0.275 | 3486ad6b724c537fbc9f42fffcf1e0af / 1703203970 |
| Toco_Quadrado_S | 64×80 | 2×2.5 | 0.5, 0.313 | 1f2ef6e6443152608e1cfb0bab67ba75 / 1908612812 |
| Toco_Raizes_S | 80×64 | 2.5×2 | 0.5, 0.406 | 3fce1597c7a45861bf6a0a02c5887c8e / 301800027 |
| Toco_Redondo_S | 64×64 | 2×2 | 0.5, 0.328 | ab5602c898665a92b0ae5c9e60238765 / 1490416464 |
| Toco_Velho_S | 64×64 | 2×2 | 0.5, 0.328 | a82ebcab71c35ec5aee438801354181c / 1340652596 |
| Tronco_Oco_S | 80×80 | 2.5×2.5 | 0.5, 0.388 | 93f933eb60ad513da55f11de33fbbdb3 / 1248612150 |
| Tronco_S | 80×80 | 2.5×2.5 | 0.5, 0.363 | 85f0a9fbe73150608c7f08d8d0df2851 / 425419135 |
| Vaso_Grande_S | 80×64 | 2.5×2 | 0.5, 0.313 | dc06fd4474665fabbfc11f5620213c4e / 422481750 |
| Vaso_S | 48×64 | 1.5×2 | 0.5, 0.313 | 8a61bda120e251d9b9a8d1344bfdc88b / 727861475 |

## 9. Lacunas

O que as regras exigem e não existe no projeto, ou existe incompleto. Nada foi criado.

| # | Lacuna | Regras |
|---|---|---|
| L01 | Não existe `Assets/_Project/Data/Cities/city_layout.json` (layout, semente do sorteio). | R08, R26, R29 |
| L02 | Não existem as ferramentas `Tools/City/Validate` e `Tools/City/Build`. `Scripts/Editor/CityGen`, `Scripts/Runtime/CityData` e `Tests/EditMode` estão vazios. | R29 (e a verificação de todas) |
| L03 | A cena tem 0 estruturas em `Houses` e 1 House, 1 Guard e 1 Warehouse antigos soltos em `UIContainer`. | R01–R04, R26 |
| L04 | O `Polygon` antigo (4 pts) continua em `PolygonLayer`. Não há quarteirões. | R22–R26 |
| L05 | `Trees` está vazio. | R16–R21 |
| L06 | Os sprites padrão dos prefabs violam R05 (`Rocha_Alta_J_NE`, `Casa_Q_N`, PLACEHOLDER). As instâncias geradas precisam sobrescrever o sprite do `Visual`. | R05–R07 |
| L07 | Os prefabs têm `sizeDelta` fixo de 218,69 cu e `BoxCollider2D` de 1×1 cu. Tamanho nativo e collider precisam ser definidos por instância. | R11, R31 |
| L08 | `entry` é nulo em todos os prefabs. Não existe Waypoint de entrada por estrutura. | R14 |
| L09 | O Limiter (29,34×19,97 u) não corresponde à caixa dos polígonos. | R27 |
| L10 | Não há ordenação de desenho por Y: hoje a ordem é a dos irmãos. | R30 |
| L11 | Visibilidade dos chãos: o `PolygonsDisplay` cobre só 17,77×10 u, e chãos fora dessa área podem não aparecer. Pendência registrada em `decisions.md`, a verificar com screenshot no Build. Nenhuma alteração autorizada. | R22, R28 |

Observações fora das regras (não tratadas):
- `ResourcesManager.moneyLbl` = null na cena. Atribuir `Money` (ao concluir um chamado) gera NullReferenceException.
- `GPS.allWaypoints` e `GPS.allLocations` estão vazios (etapa do GPS).

## 10. Dúvidas

Todas respondidas pelo usuário em 2026-11-13 e registradas em `decisions.md` e `city-rules.md`:

1. Decorações podem repetir sprite? → **Pode repetir.** O pool de 96 é menor que o mínimo de R19, que é 100.
2. `Canvas/BG` em `PolygonLayer` diante de R26? → **Exceção fixa.**
3. Ordem de desenho com sobreposição? → **Y-sort dentro de cada pai** (nova R30).
4. `BoxCollider2D` 1×1 cu das estruturas? → **Ajustar à estrutura** na instância (nova R31).

Não restam dúvidas abertas sobre a cumpribilidade das regras.
