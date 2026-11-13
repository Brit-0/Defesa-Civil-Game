# Relatório do Build — cidade v2 (2026-11-13)

Pedido do usuário: ruas com a mesma largura, cidade bonita e natural, estruturas mais perto da rua ao sul, liberdade para ignorar regras anteriores. Só a organização nos empties foi mantida obrigatória.

- **Layout:** `Assets/_Project/Data/Cities/city_layout.json` (versão 2), semente `293574367`.
  - Escolhida por mim entre 5 sementes sorteadas, olhando as prévias.
- **Build:** `Tools/City/Build` na cena `Assets/Scenes/Gameplay.unity`.
  - 1º Build: 207 criados, 347 atualizados, 6 removidos.
  - 2º Build: nenhuma mudança (554 sem alteração). É idempotente (R29).
- **Validate de cena:** 0 erros, 0 avisos. Revalidado com a cena recarregada do disco: 0 erros, 0 avisos, console limpo.
- **Testes EditMode:** 76 passando.

## Como a cidade foi desenhada

Estudei os sprites em folhas de contato. São quase vista de cima: telhado grande e uma faixa de fachada ao sul. Por isso a parte visível do sprite é praticamente o terreno que ele ocupa. Disso saíram as decisões:

- **Base visível:** tudo é posicionado pela base da parte visível do sprite, sem a margem transparente. As casas ficam a 0,12 u da rua ao sul (antes ficavam a ~0,8 u).
- **Sem sobreposição de estruturas:** uma fileira por quarteirão, todas de frente para a rua ao sul.
- **Malha:** 6 faixas de quarteirões com todas as ruas em 0,8 u, mais uma rua de contorno.
  - Duas ruas norte-sul atravessam a cidade inteira e ladeiam o parque.
  - As demais ruas verticais são desencontradas entre faixas (cruzamentos em T), para não parecer grade.
- **Bairros:**
  - Parque central, com 2 faixas de altura: monumento, colunas, canteiro de flores, alameda de árvores no contorno, 4 entradas com postes, 2 quiosques e bosques nos canteiros.
  - Centro comercial em volta do parque, com estacionamento.
  - Indústria no nordeste: tanques, chaminés, turbina, contêineres e painéis solares nos fundos.
  - Guarda na indústria, com caminhão de bombeiros, ambulância e viatura estacionados ao lado.
  - Depósitos nos cantos noroeste e sudeste: contêineres, caminhão, caixas e cerca alta.
  - Bosque de pinheiros e rochas no canto sudoeste.
  - Residencial no restante: quintais com árvores atrás das casas, cercas ou sebes nos fundos, canteiros, flores, carros na garagem.
- **Mobiliário:** semáforos nas esquinas do centro, postes e placas de pare em algumas esquinas.
- **Oclusão:** `Trees` é desenhado sobre `Houses`. O gerador só deixa uma decoração cobrir uma estrutura se ela estiver à frente (R36). Árvores de quintal ficam atrás do telhado, sem cobri-lo.

## Conteúdo gerado

| Item | Qtd |
|---|---|
| House (Suburban / Commercial / Industrial) | 69 (41 / 18 / 10) → 59% / 26% / 14% |
| Guard / Warehouse | 1 / 2 |
| Decorações (Trees) | 380, com 71 sprites diferentes (Nature 329, Carros 14, Roads 13, Industrial 11, Suburban 11, Commercial 2) |
| Sprites de estrutura diferentes | 42 |
| Polígonos | 25 = 23 quarteirões + parque + bosque |
| Waypoints de entrada | 72 (+ 3 antigos mantidos) |
| Caixa dos polígonos | 52,6 × 33,4 u, centrada na posição inicial da Main Camera |
| Limiter | 54,2 × 35,0 u (caixa + rua de contorno de 0,8 u) |
| PolygonCamera ortho | 16,7. PolygonsDisplay com o mesmo retângulo. MapRender continua 640×360. |

## Resultado por regra

Todas as regras ativas passam: R01–R20, R22–R37 (R21 foi revogada). R08 segue parcial: a semente fica registrada, mas "não escolhido à mão" não é automatizável.

Regras que mudaram nesta revisão (detalhes em `city-rules.md` e `decisions.md`):

| Regra | Antes | Agora |
|---|---|---|
| R12, R20 | cantos do retângulo do sprite | cantos da parte visível |
| R17 | só `Nature/` | qualquer pasta, menos sprites de estrutura e as exclusões de Nature |
| R19 | 2× a 3× o nº de casas | ≥ 2× |
| R21 | 70% rente à borda | revogada |
| R25 | ≥ 0,8 u | ≥ `streetWidth` |
| R27 | Limiter = caixa dos polígonos | caixa + rua de contorno |
| R35 | — | ruas de largura única (erro) |
| R36 | — | oclusão coerente (aviso) |
| R37 | — | estruturas não se sobrepõem (aviso) |

## Imagens

Em `docs/reports/build/`:

| Arquivo | O que mostra |
|---|---|
| `v2_00_previa_layout.png` | Prévia do layout em PNG (ferramenta `CityPreview`, sem a cena) |
| `v2_01_visao_geral.png` | Cidade inteira no jogo (Play Mode) |
| `v2_02_centro_parque.png` | Parque e centro |
| `v2_03_noroeste.png` a `v2_06_sudeste.png` | Os 4 quadrantes |
| `v2_07_detalhe_residencial.png` | Zoom: casas rente à rua, quintal, cerca |

## Observações e riscos

1. **Ligações do GPS:** os `neighbors` dos waypoints continuam vazios. A malha de ruas fica para a etapa do GPS.
2. **Ruído no `.unity`:** o `PolygonMesh2D` recria as meshes no Editor, então todo salvamento gera diff. R29 é medida pelo dump canônico.
3. **Undo:** o Build é grande e o Unity avisou que o histórico de Undo estourou. Desfazer um Build inteiro com Ctrl+Z pode não funcionar; use o git.
4. **Copas sobre a rua:** árvores de fundo passam até 0,6 u da borda norte do quarteirão. É intencional (parece copa sobre a calçada), e não chega às casas do outro lado.
5. **Formato do JSON mudou (versão 2):** `bottomCenter` agora é a base visível. Um layout da versão 1 é recusado.
