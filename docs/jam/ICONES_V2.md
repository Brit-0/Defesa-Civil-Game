# Ícones v2: chamados, conclusão e realce da Base e do Armazém

Pedido do Pietro em 17/11 (#527). Arte nova em pixel art, 64 px, no estilo e na paleta dos ícones das melhorias (`Assets/Arts/UI/Pixel/Icons/`), com contorno escuro grosso e alto contraste. **Só a arte está pronta: nenhuma cena nem script foi mexido.** Quem liga é a ajudante da UI, por esta lista.

Pasta: **`Assets/Arts/UI/Chamados/`** (18 PNGs com `.meta`). Importação igual à dos vizinhos do kit (`Px_IconCoin`, `upgrade_*`): Sprite único, **Filter Point, sem compressão, sem mipmap, PPU 32**, alfa binário.

## O que substitui o quê

| PNG | Substitui | Quem usa (arquivo, objeto ou campo) |
|---|---|---|
| `call_repair.png` | a caixinha `crate_0` (sprite de exemplo do Text Animator) e `Mk_Icon_Damage.png` | balão `House/Canvas/Call/Icon` do `House.prefab` quando a casa está `Damaged`; `House.damagedIcon` (vazio no prefab: hoje o ícone do `Progress` some); `MapMarkers.damageIcon` |
| `call_fire.png` | a caixinha `crate_0` e `Mk_Icon_Fire.png` | `Call/Icon` com a casa `OnFire`; `House.fireIcon` (vazio no prefab); `MapMarkers.fireIcon`. **Fogo único:** o mesmo PNG em todo chamado de fogo (balão, progresso e pino) |
| `call_storm.png` | `Mk_Icon_Storm.png` | `MapMarkers.stormIcon` (casa danificada pela tempestade, `Kind.Storm`); `Call/Icon` nesse caso |
| `call_power.png` | nada ainda (ícone novo) | casa em `State.PowerOutage`: hoje o `MapMarkers.KindOf` a trata como tempestade e mostra o `stormIcon`; com este PNG o apagão ganha ícone próprio (campo novo no script) |
| `call_supply.png` | `Assets/Arts/Icons/BuildMaterial.png` no papel de ícone de chamado | `ResourcesManager.materialIcon` (na cena), que o `House.GetIconByCallType` usa no chamado `Restock` do Armazém; `Call/Icon` do Armazém |
| `call_done.png` | o quadrado preto do `Complete/Icon` (aponta para um sprite que não existe mais, GUID `3ac1672d…`, cor preta) e `Mk_Icon_Done.png` | `House/Canvas/Complete` + `Complete/Icon` do `House.prefab`; `MapMarkers.doneIcon`. É o selo inteiro (disco verde com check creme): sozinho, sem o `Circle.png` verde atrás, e com a cor da `Image` em branco |
| `call_done_check.png` | alternativa ao `call_done.png` | só o check, para ficar **por cima** de um disco verde que já exista (o `Complete` com `Circle.png`, ou o pino com `fill` verde). Use um ou outro |
| `landmark_base.png` | `Mk_Icon_Guard.png` | `MapMarkers.guardIcon`: placa "BASE DA GUARDA" e pino de borda da Base |
| `landmark_depot.png` | `Mk_Icon_Depot.png` | `MapMarkers.depotIcon`: placa "ARMAZÉM" e pino de borda do Armazém |
| `highlight_arrow.png` | nada (realce novo) | seta dourada que quica em cima da Base e do Armazém. O `Mk_Arrow.png` continua: é a seta de borda que aponta para fora da tela |
| `highlight_halo_base_0..3.png` | o halo que o `LandmarkFx` desenha por código (`PixelFx.Glow`) na Base | `LandmarkFx` (Base, `CityGuard`): anel laranja no chão, 4 quadros |
| `highlight_halo_depot_0..3.png` | o mesmo halo do `LandmarkFx` no Armazém | `LandmarkFx` (Armazém, `Warehouse`): anel dourado no chão, 4 quadros |

Hoje o balão `Call/Icon` mostra a mesma caixinha para todo tipo de chamado (o sprite é fixo no prefab; o `GetIconByCallType` só alimenta o `Progress/Icon`). Para o balão mostrar o tipo, o `House` precisa trocar o sprite do `Call/Icon` pelo mesmo `GetIconByCallType`: é mudança de script, da ajudante da UI.

## Tamanho na tela (sem isso o pixel quebra)

- **Ícones 64x64:** mostrar a **64 px numa tela de 1080 de altura (1:1)** ou a 128 px (2x). Nunca encolher com filtro Point: pixel some e o contorno abre (o defeito da decoração encolhida, `e32072c`). Se precisar de ícone menor, peça uma versão gerada em 32 px.
- **`MapMarkers` multiplica o tamanho do sprite** por `pixelScale` (3) e `landmarkPixelScale` (2), feitos para os `Mk_*` de 14 px. Com estes PNGs isso daria 192 e 128 px: os ícones novos precisam de escala 1. O disco do pino (`Mk_Disc`, 26 x 3 = 78 px) comporta o ícone de 64 px.
- **Halos 64x32:** desenhados no pixel do kit; mostrar a **3x (192x96 px)** no pé do prédio, como os botões (pixel de 3 unidades).
- **Seta:** 64x64 a 1:1, quicando em passos de pixel inteiro (0, 2, 4, 2 px).

## Animação do realce

- Halo: quadros `0 → 1 → 2 → 3` em loop, ~0,16 s cada (o anel abre e afina; o 3 é o mais aberto e fino, e o loop recomeça pequeno). Base e Armazém defasados em 2 quadros para não piscarem juntos. Para o pulso forte do `LandmarkFx` (escolher a base, material zerado), 0,08 s por quadro.
- Seta: quique de 4 px, ~0,64 s por ciclo, sempre em pixel inteiro (DOTween com `snapping`).

## Conferência (17/11)

- 18 de 18 PNGs: 0 pixel fora da paleta do kit (18 cores do `ICONES_FALTANDO.md`), 0 pixel semitransparente, contorno `#141218` fechado em todos; `.meta` com Point, compressão 0 em todas as plataformas, sem mipmap, PPU 32.
- Folha de contato a 3x (fundo claro e fundo do painel) e montagem a 1:1 sobre uma captura do mapa: ícones legíveis nos dois fundos; mandadas ao Telegram do Pietro como PRÉVIA.

## Como foi feito

Pixel Lab pela API (`create-image-pixflux` 64x64, paleta do kit forçada por `color_image`, fundo transparente): 76 gerações para 9 temas, cerca de US$ 0,61 (a conta é por assinatura; o crédito em dólar continuou 0). De 6 a 14 candidatos por ícone; escolhidos pela silhueta a 1:1 e pelo contraste nos dois fundos. Retoque por script: recorte justo centrado no 64x64, sem reescalar, e 2 px de contorno externo `#141218` (fica de 2 a 3 px com o contorno da geração). Os halos são elipses desenhadas por script, porque o Pixel Lab não fecha anel geométrico. Scripts e candidatos ficam fora do repositório, em `analises-lilith/jogo-defesa-civil/a527/`.
