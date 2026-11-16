# Ícones que faltam na UI (Pixel Lab, 64x64)

Pedido do Pietro em 16/11: todo ícone que o script do kit (`Tools/PixelUI/pixel_kit.py`) não desenha é gerado com a extensão **Pixel Lab do Aseprite em 64x64 px**, escolhendo a melhor geração de cada um, e entra no jogo por estes caminhos. Quem gera: a Lilith, no Aseprite do Pietro.

## Como entra no jogo

1. Salvar o PNG em **`Assets/Arts/UI/Pixel/Icons/<nome>.png`** (nome exato da tabela, minúsculo, sem espaço). 64x64, fundo transparente.
2. Ao abrir o Unity, o `PixelIconImporter` (`Assets/Systems/Feel/Editor/`) importa como sprite com **Filter Point, sem compressão e sem mipmap** e liga o arquivo no `FeelUITheme.asset` pela chave (o nome do arquivo). Ninguém precisa mexer no Inspector; o console mostra "FeelUITheme: ícones do Pixel Lab atualizados".
3. No jogo, o botão de melhoria com aquele `id` troca o ícone provisório pelo novo (`UpgradeButtonJuice`, por `overrideSprite`). Sem o PNG fica o provisório de hoje, sem erro.
4. Commitar o PNG **com o `.meta`** que o Unity criar (ou rodar `Tools/Jam/Create Feel Prefabs` depois).

O ícone aparece com 64x64 unidades na tela de 1080p (1 pixel do PNG = 1 unidade). A moldura e os botões do kit usam pixel de 3 unidades; por isso o desenho deve ser **grosso e simples** (contorno de 2 a 3 px, poucas cores), para não parecer de outro jogo ao lado deles.

## Os ícones

Hoje os 8 botões de melhoria usam arte emprestada de outras coisas (ambulância, prédio, casa, a seta da UI). Esses são os que faltam:

| Arquivo | O que representa (melhoria no jogo) | Provisório de hoje | Prompt curto para o Pixel Lab |
|---|---|---|---|
| `upgrade_workers.png` | Contratar trabalhador (+1 trabalhador) | `Worker.png` | `civil defense worker with orange vest and yellow hard hat, waving, plus sign badge` |
| `upgrade_speed.png` | Botas de corrida (equipe anda mais rápido) | `Arrow.png` | `orange running boot with speed lines, game upgrade icon` |
| `upgrade_tools.png` | Ferramentas melhores (conserto mais rápido) | `Kit.png` | `crossed hammer and wrench, steel heads, wooden handle, sparkle` |
| `upgrade_cart.png` | Carrinho de mão (mais material por viagem) | `BuildMaterial.png` | `wheelbarrow full of bricks and planks, side view` |
| `upgrade_reward.png` | Seguro municipal (pagamento maior por conserto) | `Casa_A.png` | `small house inside a shield with a gold coin, insurance icon` |
| `upgrade_automation.png` | Automação (despacho automático) | `Ambulancia.png` | `gear with a lightning bolt and a radio antenna, automation icon` |
| `upgrade_income.png` | Repasse da Prefeitura (dinheiro a cada intervalo) | `Predio_A.png` | `city hall building with stack of gold coins` |
| `upgrade_goal.png` | Centro de Operações (objetivo final, vence o jogo) | `Arranha_Ceu_A.png` | `emergency operations center tower with siren light and radar dish, trophy feeling` |

As chaves que o código procura estão em `FeelUITheme.LabIconKeys` (`upgrade_` + o `id` do `UpgradeData`). Melhoria nova do B com outro `id` = mais uma linha aqui e um PNG com `upgrade_<id>.png`; o código não muda.

## Estilo para todos

Acrescentar ao fim de cada prompt: `pixel art game icon, 64x64, bold 2px dark outline, flat shading with one highlight, top-left light, transparent background, no text, centered, fills 80% of the canvas`.

- Vista: frente ou 3/4, um objeto só, silhueta que se lê a 32 px.
- Contorno: `#141218` (quase preto arroxeado), 2 a 3 px.
- Luz em cima e à esquerda, sombra embaixo e à direita, sem degradê, sem antisserrilhado, sem brilho suave.
- Na hora de escolher a melhor geração: silhueta legível pequena, cores dentro da paleta, contorno fechado.

### Paleta da UI (21 cores do kit)

Usar de preferência estas; no Aseprite, dá para carregar como paleta e reduzir a geração a ela.

| Cor | Hex | Uso |
|---|---|---|
| contorno | `#141218` | todo contorno |
| painel escuro | `#1F1C25` | sombra funda |
| painel | `#29262F` | fundo dos painéis |
| painel médio | `#3A3442` | sombra |
| botão | `#594C63` | metal escuro, sombra de cinza |
| botão claro | `#7A6A87` | metal |
| lilás | `#A895B5` | brilho de metal |
| creme | `#F2EBDD` | branco do jogo (nunca `#FFFFFF` puro) |
| dourado | `#FFD14D` | moeda, capacete, destaque |
| dourado escuro | `#C98A2B` | sombra do dourado |
| marrom | `#7A4A22` | madeira, cabo |
| laranja | `#F28C28` | colete e cor da Defesa Civil |
| vermelho | `#D9412F` | sirene, telhado |
| vermelho escuro | `#8A2424` | sombra do vermelho |
| verde | `#5CC45E` | dinheiro, positivo |
| verde escuro | `#2E7A3C` | sombra do verde |
| azul | `#3F7FE0` | vidro, água, escudo |
| azul escuro | `#24448A` | sombra do azul |

(As três restantes, `#FFFFFF`, `#D0D0D0` e `#9A9A9A`, são só para sprites que o Unity tinge; não usar nos ícones.)
