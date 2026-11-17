# Quadro de força e dia e noite

Ajudante 2 (#536), branch `jam-v4b`, 17/11/2026. Pedido do Pietro: (1) um minigame simples e rápido no lugar do botão **RELIGAR** direto, com pixel art, som e animação, dentro do segundo tutorial de energia; (2) um ciclo de dia e noite só de luz, suave, de 6 a 8 minutos, noite escura mas legível, com as luzes das casas, dos postes e das bases acendendo à noite, sem mudar a jogabilidade e sem pesar no navegador (meta de 50+ FPS).

Nada disso mexe na `Gameplay.unity` nem em prefab: o `FireEnergyLink` cria o `FuseBoxMinigame` e o `DayNightCycle` se cria sozinho depois da cena carregar (`RuntimeInitializeOnLoadMethod`).

## 1. Quadro de força (o RELIGAR)

**Para o jogador.** No apagão, **RELIGAR** abre um quadro de força ao lado do botão. Ele tem uma faixa amarela e preta, o raio, o título "QUADRO DE FORÇA" e a linha "LIGUE NA ORDEM DE 1 A 4". Embaixo ficam 4 chaves, cada uma com o seu número em cima e um LED embaixo. Os números saem embaralhados e nunca em fila da esquerda para a direita.
- **Chave certa:** a alavanca sobe com um soco, o LED fica verde e solta faíscas douradas. O clique fica mais agudo a cada chave (tom `1 + 0,12 × chave`).
- **Chave fora da ordem:** zumbido, o quadro treme e todas as chaves descem. O LED da errada fica vermelho, aparece "FORA DA ORDEM!" e as chaves ficam cinza por `wrongLock` (0,6 s). Depois é só recomeçar do 1. É uma falha leve: custa menos de um segundo.
- **Última chave:** "LUZ!", os LEDs pulam, um clarão cobre o quadro e, `restoreDelay` (0,4 s) depois, a rede volta (`EnergyManager.Restore`) e o quadro fecha.
- Nas duas primeiras vezes que o quadro abre (`hintTimes`), o número da próxima chave pulsa.
- **RELIGAR** de novo fecha o quadro. Se o apagão acabar sozinho, ou se a partida acabar, ele fecha sem festa.

Uma partida no quadro leva de 2 a 4 segundos.

**No tutorial de energia (passo 2/3).** O balão começa com o texto de antes (clique em RELIGAR). Com o quadro aberto, ele troca para "Quadro de força: ligue as chaves na ordem dos números, 1, 2, 3... Errou? Elas descem e você tenta de novo." A seta e o aro passam do RELIGAR para a próxima chave. A camada do tutorial fica por cima da camada do quadro, senão a seta ficava escondida atrás dele.

**Código.**
- `Systems/Energy/FuseBox.cs`: as regras puras (`FuseBox`, `FusePress`). Inclui a ordem embaralhada, o erro que derruba tudo e trava, e a chave repetida que é ignorada.
- `Systems/Energy/FuseBoxMinigame.cs`: a apresentação, numa camada `FuseBox` do HUD:
  - API: `Toggle`, `Open`, `Press`, `IsOpen`, `NextSwitch`;
  - escuta `GameEvents.BlackoutChanged` e `GameEvents.GameStateChanged` (vitória e derrota).
- `EnergyHud.Restore`: abre o quadro quando ele existe; sem ele, religa direto como antes.
- `EnergyTutorial`: o texto `fuseText` e a seta na próxima chave.
- Arte e som em `Systems/Energy/Resources/FireEnergy/`:
  - `fuse_switch_off/on`, `fuse_led_off/on/bad`, `fuse_spark`, `fuse_stripe` (PPU 32, Point, sem compressão);
  - `sfx_fuse_open/switch/wrong/done.wav`.
  - Gerados por `analises-lilith/jogo-defesa-civil/scripts/v4b/arte_quadro.py` (`--previa` faz a folha PRÉVIA).

**Dados (`Resources/Balance/FuseBoxData.asset`).** Ficam fora do `FireEnergyData` para não conflitar com a v4C.

| Campo | Valor | O que faz |
|---|---|---|
| `switches` | 4 | chaves no quadro (3 a 5 é o que o teste aceita) |
| `wrongLock` | 0,6 s | trava depois do erro |
| `restoreDelay` | 0,4 s | tempo da festa antes da rede voltar |
| `hintTimes` | 2 | aberturas com o número da próxima chave pulsando |

## 2. Dia e noite

**Para o jogador.** Um dia inteiro a cada `cycleSeconds` (420 s, 7 min) de partida. A partida começa de manhã (`startPhase` 0,1), então o primeiro tutorial roda de dia. As fases vão de uma para a outra com suavidade (smoothstep). Só a luz muda: tempo, dinheiro e desastres seguem iguais. O ciclo para junto com a pausa e com o fim da partida.

| Fase | Posição | Ambiente | Sol | Noite |
|---|---|---|---|---|
| Amanhecer | 0 | rosado, 0,52 | laranja, 0,3 | 0,45 |
| Manhã | 0,08 | neutro, 0,64 | 0,45 | 0 |
| Dia | 0,25 | **a luz que a cena já tinha** (0,88; 0,91; 1) × 0,68 | 0,5 | 0 |
| Tarde | 0,4 | 0,65 | quente, 0,48 | 0 |
| Entardecer | 0,5 | laranja, 0,52 | 0,42 | 0,55 |
| Noite | 0,58 | azul, 0,38 | luar, 0,13 | 1 |
| Madrugada | 0,9 | azul, 0,36 | 0,12 | 1 |

A primeira versão tinha o dia em 0,84. Nos prints, a praça e as calçadas estouravam, e o dia voltou para a luz já aprovada da cena.

**O que acende à noite** (o valor "Noite" da fase manda):
- **Postes:** a luz `[CG] Light Lamp*` e a lâmpada `[G]` do mesmo poste acendem juntas. De dia ficam apagadas (`lampDay` 0); na noite cheia vão a `lampNight` (1,1 × a luz da cena).
- **Base e Armazéns:** de `baseDay` (0,3) a `baseNight` (1,15). Base e armazém novos da expansão ganham uma luz igual.
- **Janelas:** 3 em cada 4 casas (`windowsLitShare`); a Base e os Armazéns acendem sempre.
- **Uma de cada vez:** cada luz tem o seu momento dentro de `lightsSpread` (0,45 da subida da noite), sorteado pela posição.

**Janelas sem uma luz por casa.** Cada casa ganha um filho `[W]`. É um SpriteRenderer com o mesmo sprite da casa e o material `DayNight/WindowGlowMAT` (shader `DefesaCivil/Window Glow`, aditivo).
- O shader acende só o vidro claro azulado e o amarelo quente das janelas, com cor `windowColor` e brilho `windowIntensity` (1,5, para o bloom pegar).
- Nos Armazéns (`warmKeyOff`), o amarelo é telhado, então só o vidro acende.
- A calibração da máscara está em `scripts/v4b/mascara_janelas.py` (folha PRÉVIA com as janelas de cada sprite).

**Apagão.** Com a rede caída, as luzes da cidade piscam (até `blackoutFlicker`, 0,35 s, cada uma no seu tempo) e apagam. Ao religar, voltam numa onda que sai da Base (`restoreWaveSpeed`, 30 unidades por segundo). Isso dá a resposta do quadro de força à noite.

**Custo.** A luz é desligada quando chega a zero: de dia, só 5 luzes 2D ficam ligadas (global, sol e 3 bases); na noite cheia, 25. Uma luz ou janela só é mexida quando o valor muda mais que 0,004. As janelas são sprites aditivos, não luzes.

Medido no Editor, Game view 1920x1080, `scripts/v4b/seq.cs`:

| Momento | Draw calls | SetPass | Luzes ligadas | Janelas acesas |
|---|---|---|---|---|
| Dia | 537 | 214 | 5 | 0 de 156 |
| Noite | 574 | 256 | 25 | 121 de 156 |
| Apagão à noite | 505 | 208 | 2 | 0 de 156 |

O Editor rodou entre 240 e 370 FPS nas duas pontas do ciclo.

O build WebGL não foi medido: o disco não tinha espaço para o build nesta tarefa. Confira os 50+ FPS no próximo build do itch.io, de noite e de dia.

**Código.**
- `Systems/DayNight/DayNightModel.cs`: contas puras (`PhaseAt`, `Evaluate`, `LightOn`, `Slot`, `PhaseOf`).
- `Systems/DayNight/DayNightCycle.cs`:
  - acha as luzes da cena;
  - cria as janelas e as luzes das bases novas (procura de 2 em 2 s);
  - aplica a fase.
  - Botões Odin `Jump To` (nome da fase) e `Set Phase` (0 a 1) no objeto **DayNight** durante o Play.
- `Systems/DayNight/WindowGlow.shader` e `Resources/DayNight/WindowGlowMAT.mat`.
- `SO/DayNightData.cs` e `Resources/Balance/DayNightData.asset`: tudo o que está nas tabelas acima. **Tools → Jam → Create Day Night Data** cria o que faltar.

## 3. Testes

EditMode (**Window → General → Test Runner**), 12 novos:
- `FuseBoxTests` (6): ordem embaralhada e nunca em fila, certo até o fim, erro que derruba e trava, chave repetida ignorada, mínimo de uma chave, dados curtos e leves.
- `DayNightTests` (6): fase dando a volta, fases em ordem, sem degrau ao longo do dia, noite escura mas legível, luzes uma a uma, dados com um dia de 6 a 8 min.

No Play, o robô `scripts/v4b/seq.cs` (com `seq.ps1`) faz o segundo tutorial com cliques de mouse de verdade e grava os prints `resultados/81_*.png`. Ele passa por ENTENDI, RELIGAR, chave 1, uma chave errada, as 4 chaves e VAMOS. Depois tira as fases do dia, o apagão à noite e a cidade reacendendo.
