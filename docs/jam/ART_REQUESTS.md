# Pedidos de arte e áudio

Feitos fora do projeto e entregues ao Agente A para importar (T31).

## Especificação geral

- Pixel art no mesmo estilo e paleta das casas atuais (`Assets/Arts/Houses`) e dos ícones atuais (`Assets/Arts/Icons`).
- PNG com transparência. Importação: PPU 32, Filter Point, sem compressão (usar o preset `Assets/Sprite_PixelArt.preset`).
- Ícones de HUD: **64×64**, como `Worker.png`, `Kit.png` e `BuildMaterial.png`.
- Animações: uma folha horizontal por animação, quadros do mesmo tamanho, nome `Nome_NxM.png` (N quadros, M px por quadro).
- Entregar em `Assets/Arts/<Pasta>/` com os nomes abaixo, para o código achar sem renomear.

Prioridade: **P1** = necessário para o jogo fechar · **P2** = melhora muito · **P3** = se sobrar tempo.

## Ícones (`Assets/Arts/Icons/`, 64×64)

| Prio | Arquivo | Descrição |
|---|---|---|
| P1 | `Money.png` | Moeda/dinheiro. Hoje o HUD mostra um triângulo provisório. |
| P1 | `Call_Damaged.png` | Marcador de chamado de casa danificada (martelo ou casa rachada). |
| P1 | `Call_Fire.png` | Marcador de chamado de incêndio (chama). |
| P1 | `Complete.png` | "Pronto para coletar" (check ou moeda saltando). |
| P1 | `Chaos.png` | Ícone da barra de caos (sirene ou megafone). |
| P1 | `Upg_Workers.png` | Upgrade: contratar trabalhador. |
| P1 | `Upg_Speed.png` | Upgrade: botas de corrida. |
| P1 | `Upg_Tools.png` | Upgrade: ferramentas. |
| P1 | `Upg_Cart.png` | Upgrade: carrinho de mão. |
| P1 | `Upg_Reward.png` | Upgrade: seguro municipal. |
| P1 | `Upg_Automation.png` | Upgrade: automação (antena/rádio). |
| P1 | `Upg_Goal.png` | Objetivo final (troféu ou prédio da Defesa Civil). |
| P2 | `Upg_Income.png` | Renda passiva (prefeitura). |
| P2 | `Disaster_Earthquake.png` | Aviso de terremoto. |
| P2 | `Disaster_Storm.png` | Aviso de tempestade. |
| P2 | `Disaster_Fire.png` | Aviso de incêndio. |
| P3 | `Call_Power.png` | Chamado de queda de energia (só se a energia entrar). |

## Efeitos (`Assets/Arts/VFX/`)

| Prio | Arquivo | Descrição |
|---|---|---|
| P1 | `Crack.png` | Textura de rachadura própria, 64×64 ou 128×128, repetível, branco sobre transparente. **Substitui a textura atual, que veio da internet (pngtree) e tem licença incerta.** |
| P1 | `Fire_6x32.png` | Chama em loop, 6 quadros de 32×32. Fica em cima da casa. |
| P1 | `Dust_5x32.png` | Nuvem de poeira/escombros, 5 quadros de 32×32, toca uma vez. |
| P2 | `Smoke_6x32.png` | Fumaça subindo em loop, 6 quadros de 32×32. |
| P2 | `Rain_Tile.png` | Riscos de chuva em diagonal, 64×64, repetível nos 4 lados. |
| P2 | `Lightning_4x64.png` | Raio caindo, 4 quadros de 32×64. |
| P2 | `Sparkle_4x16.png` | Brilho de conserto concluído, 4 quadros de 16×16. |
| P3 | `Soot.png` | Sobreposição de fuligem para casa que pegou fogo, repetível. |

## Personagens (`Assets/Arts/Characters/`)

| Prio | Arquivo | Descrição |
|---|---|---|
| P2 | `Worker_Walk_4x32.png` | Trabalhador andando, 4 quadros de 32×32, vista de lado (o código espelha para a outra direção). Hoje o trabalhador na rua é o ícone do HUD, parado. |
| P3 | `Worker_Walk_Up_4x32.png`, `Worker_Walk_Down_4x32.png` | Variações subindo e descendo a rua. |
| P3 | `Firefighter_Walk_4x32.png` | Variante para chamados de incêndio. |

## Estruturas (`Assets/Arts/Houses/Special/`)

| Prio | Arquivo | Descrição |
|---|---|---|
| P2 | `Guard_S.png` | Base da Defesa Civil, fachada para o sul, no tamanho das fábricas atuais. Hoje usa um sprite de fábrica sorteado. |
| P2 | `Warehouse_S.png` | Armazém de materiais, fachada para o sul. Mesmo caso. |
| P3 | `Goal_S.png` | Centro de Operações (aparece na vitória). |

## Mobiliário urbano (`Assets/Arts/Houses/Roads/<Nome>/`, pedido do MEGABRAIN para o mapa v6)

Cada sprite com o normal map ao lado (`<Nome>_n.png`, pela Forja ou pelo Lighter): a cena agora tem luz 2D e usa o relevo.

| Prio | Arquivo | Descrição |
|---|---|---|
| P2 | `Banco/Banco.png` | Banco de praça, ~1 u de largura. A praça da Guarda e o parque só têm postes e canteiros. |
| P2 | `Lixeira/Lixeira.png` | Lixeira de calçada, ~0,5 u. Para as esquinas do comércio. |
| P3 | `Hidrante/Hidrante.png` | Hidrante vermelho, ~0,5 u. Combina com o tema. |
| P3 | `Ponto_Onibus/Ponto_Onibus.png` | Ponto de ônibus, ~1,5 u. |

## Interface (`Assets/Arts/UI/`)

| Prio | Arquivo | Descrição |
|---|---|---|
| P1 | `Logo.png` | Logo/título do jogo para o menu. |
| P2 | `Cursor.png`, `Cursor_Hover.png` | Cursor 32×32 (seta e mão). |
| P2 | `ChaosBar_Frame.png`, `ChaosBar_Fill.png` | Moldura e preenchimento da barra de caos, 9-slice. |
| P2 | `Panel.png` | Painel 9-slice para abas e telas (vitória, derrota, pausa). |
| P3 | `Banner.png` | Faixa de aviso de desastre, 9-slice. |

## Áudio (`Assets/Audio/`)

Formato: `.ogg` (ou `.wav` curto para efeitos), mono para efeitos, estéreo para música. Loops sem estalo na emenda.

| Prio | Arquivo | Descrição |
|---|---|---|
| P1 | `Music_Calm.ogg` | Loop calmo, ~60 a 90 s. |
| P1 | `Music_Tense.ogg` | Mesma base e mesmo andamento, mais tenso, para cruzar com o calmo conforme o caos. |
| P1 | `Sfx_Click.wav`, `Sfx_Hover.wav` | Interface. |
| P1 | `Sfx_Dispatch.wav` | Equipe enviada. |
| P1 | `Sfx_Hammer.wav` | Trabalho em andamento (curto, para repetir). |
| P1 | `Sfx_Repaired.wav` | Conserto concluído. |
| P1 | `Sfx_Coin.wav` | Dinheiro coletado. |
| P1 | `Sfx_Buy.wav`, `Sfx_Error.wav` | Compra de upgrade; ação negada. |
| P1 | `Sfx_Siren.wav` | Aviso de desastre. |
| P1 | `Sfx_Earthquake.wav` | Tremor, ~3 s. |
| P2 | `Sfx_Thunder.wav`, `Amb_Rain.ogg` | Trovão; chuva em loop. |
| P2 | `Amb_Fire.ogg`, `Sfx_Extinguish.wav` | Fogo em loop; fogo apagado. |
| P2 | `Sfx_Victory.wav`, `Sfx_Defeat.wav` | Fim de jogo. |

Enquanto o áudio final não chega, o Agente C usa sons provisórios (T13). Se forem usados sons de bancos gratuitos, anotar origem e licença em `DECISIONS.md`.
