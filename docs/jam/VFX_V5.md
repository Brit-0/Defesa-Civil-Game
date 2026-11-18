# Efeitos visuais v5C (jam-v5c, #523, 17/11)

Pedido do Pietro: efeitos que impressionem na enchente, no meteoro, nas guaritas, no para-raio e na obra pronta.
Tudo é apresentação: escuta `GameEvents` e os eventos de cada sistema. Não muda regra nem número de balanceamento, e
não mexe na `Gameplay.unity`. O tempo vem do `GameClock` (para no Esc), as partículas da `FeelParticles` passam pelo
`VfxQuality` e as cores e sprites vêm do `PixelFx`.

## O que o jogador vê

- **Enchente** (`FloodFx`):
  - **Bueiro:** uma grade de ferro no meio do quarteirão.
  - **Entupido:** o bueiro cospe jatos de água com névoa e anel de espuma. Enquanto a água sobe, ondas saem dele.
  - **Superfície:** ondinhas e respingos na água, e as faixas das casas boiam ±1 px.
  - **Válvula girando:** o jato para e a água desce num redemoinho tracejado (azul-escuro e espuma) em volta do
    volante, que gira mais rápido com mais pressão.
  - **Erros e coleta:** soltar a válvula (`ValveSlip`) dá um jato forte. Na coleta a água escoa num jato final, com
    respingo no pé de cada casa e a grade sumindo.
- **Meteoro** (`MeteorFx` + `MeteorFx.Impact`):
  - **Queda (1,3 s finais do aviso):** a pedra vem de 6,5 casas acima, quase em velocidade constante, e fica ~0,5 s
    na tela. Atrás dela vão um halo e um rastro de 9 bolas, do creme ao vermelho até a fumaça. Brasas e fumaça saem
    da pedra, e o brilho laranja no quarteirão cresce até o impacto.
  - **Impacto:** clarão branco que vira o laranja da queimada, bola de fogo (dois halos), entulho, faíscas e uma
    coluna de fumaça.
  - **Onda de choque:** um anel grosso no chão (aparece nas ruas) e um anel tracejado no ar, com poeira na frente.
    As casas vizinhas levam um tranco quando o anel passa por elas.
  - **Cratera:** um núcleo em brasa que pulsa e esfria em 25 s, com brasas e três respiros de fumaça. Na obra a
    fumaça fica mais lenta.
  - **Fim:** "RECONSTRUÍDO!" com festa em escada nas casas.
- **Guarita** (`OutpostFeel`):
  - **Despacho:** a lâmpada acende e um radar segmentado sai do pé da guarita. Depois um anel de mira fecha no pé
    da casa.
  - **Chegada:** um brilho verde em cada trabalhador. Quando a obra começa sobe uma coluna de luz verde, com halo,
    anel e faíscas.
- **Para-raio** (`LightningRodFeel`):
  - **Raio absorvido:** além do raio normal do `WeatherFx`, cai um super raio do alto da tela até a ponta do mastro.
    Ele tem três passadas (brilho, borda, miolo), ramos e pisca três vezes.
  - **Em volta:** clarão dourado, halo creme, estrela de raios e um tremor da câmera.
- **Obra pronta** (`BuildDoneFx`, nasce sozinho em toda cena com `GameManager`):
  - **No pé da obra:** anel dourado tracejado, coluna de luz, halo, poeira, brilhos e confete.
  - **Texto:** "OBRA PRONTA!" no terreno e "PARA-RAIO PRONTO!" no para-raio. A guarita fica sem texto, porque já
    tem a placa "GUARITA PRONTA!".

## Escala (o erro que a v5B tinha)

Nas camadas `PixelFx.Ground` e `PixelFx.Sky`, uma casa mede ~218 unidades locais. A escala das `FeelParticles` está
nessas unidades, por isso as partículas da v5B (escala 1 a 3) nem apareciam. Agora todo efeito mede em alturas de casa
(`VfxKit.House(layer)`).

## Arquivos

- `Assets/Systems/Feel/VfxKit.cs`: anel, redemoinho, halo, coluna de luz, raio em segmentos e oito `FxPreset` próprios
  (jato, respingo, névoa, bola de fogo, rastro, coluna de fumaça, brasa, poeira da onda).
- `FeelParticles.EmitPreset`: emite um preset do código sem passar pelo Inspector.
- `BlockDisasterArt.Grate`: o sprite da grade do bueiro.
- Os números de cada efeito (tempo, tamanho, cor) ficam em campos `[SerializeField]` com `[Header]` nos próprios
  componentes, no Inspector, como os outros feels.

## Conferir

- **Inspector:** os botões "Forçar enchente" e "Forçar meteoro" no objeto `[Enchente e meteoro]`.
- **Robô de conferência** (fora do repositório): `analises-lilith/jogo-defesa-civil/scripts/v5c/robo.cs` e
  `rodar.sh`. Ele faz um print de cada efeito, mede FPS por trecho e o pico de partículas, e deixa os prints em
  `resultados/v5c/`.

## Resultado da conferência (18/11)

- Testes EditMode: 187 de 187. `ValidateAll`: 0 erros e 0 avisos. Play do robô: 16 prints e 0 erros no console.
- FPS no Editor a 1920x1080, por trecho: parado 207; obras 160; guarita 161; enchente 152; aviso do meteoro 149;
  impacto 236; obra do meteoro 250. Pico de 246 partículas vivas (limite 500).

