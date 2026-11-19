# GDD — Please Fix the City (jam)

Legenda: **[CONFIRMADO]** = decidido pelo usuário. **[PROPOSTA]** = sugestão do organizador, vale como padrão de implementação. **[DECISÃO]** = decidido pelo MEGABRAIN na revisão de 16/11 (motivo em `DECISIONS.md`); vale como regra. **[PENDENTE]** = precisa de resposta antes de implementar (hoje não há nenhum).

Todos os números são valores iniciais de balanceamento e moram em `BalanceData` / ScriptableObjects, nunca no código. A lista completa dos valores iniciais está na seção 13.

## 1. Visão

Você comanda a Defesa Civil de uma cidade pequena. Desastres quebram casas. Você despacha trabalhadores, coleta a recompensa, compra upgrades e automatiza a operação. Se muitas casas ficarem com problema ao mesmo tempo, o caos toma a cidade e você perde. Compre o objetivo final para vencer. Partida de 10 a 15 minutos.

Pilares:
1. **Sempre tem algo se mexendo.** Toda ação tem resposta visual e sonora.
2. **De manual para automático.** O prazer do incremental é ver a própria operação andar sozinha.
3. **Pressão legível.** O jogador sempre entende por que o caos está subindo.

## 2. Loop principal

1. Um desastre acontece (com aviso alguns segundos antes) e casas ganham problemas.
2. O jogador abre o chamado e despacha uma equipe a partir de uma base. **[CONFIRMADO]** No início são 3 passos: clicar na casa → escolher a base → escolher o tamanho da equipe → enviar. Upgrades de Automação removem passos (seção 7).
3. Trabalhadores andam pelas ruas até a casa, trabalham (círculo de progresso) e a casa fica "pronta para coletar".
4. O jogador coleta: casa consertada, dinheiro entra, equipe volta para a base.
5. Dinheiro compra upgrades. Desastres ficam mais fortes e mais frequentes.

## 3. Recursos

| Recurso | Uso | Estado |
|---|---|---|
| Trabalhadores | Ficam na base; saem em equipe; voltam ao fim do chamado. Começa com 3. | [CONFIRMADO] já existe |
| Material | Consertar casa danificada custa `damageAmount` (1 a 3). Buscado no Armazém: cada trabalhador enviado traz 1. | [CONFIRMADO] mantido |
| Dinheiro | Recompensa por conserto; compra upgrades. | [CONFIRMADO] |
| Kits de primeiros socorros | Cortado. Esconder do HUD. | [CONFIRMADO] |

**Dinheiro [CONFIRMADO]:** no início vem só de consertos. Renda passiva chega depois, por upgrade ou por um sistema próprio (seção 8).

**Material inicial [DECISÃO]:** começa com 5, para o primeiro conserto não exigir ida ao Armazém antes de o jogador entender o loop.

**Material é gasto no envio, não na coleta [DECISÃO]:** ao enviar a equipe para uma casa `Damaged`, o material (`damageAmount`) sai do estoque na hora. Sem material suficiente, o envio é negado com aviso visual e sonoro (`ActionDenied`). Assim a coleta sempre conserta, e dois chamados não contam com o mesmo material.

**Trabalhadores saem do estoque no envio [DECISÃO]:** a equipe inteira é debitada quando o jogador confirma, não um a um conforme saem da base.

### 3.1 Tamanho da equipe [DECISÃO]

Hoje o tamanho da equipe não muda nada no conserto. Passa a mudar, para a escolha ter peso:

- Tempo de trabalho `= tempo por ponto de dano × dano ÷ trabalhadores enviados` (com um mínimo). Equipe maior conserta mais rápido, mas deixa menos gente para os outros chamados.
- Mínimo para enviar: 1 trabalhador. Equipe sugerida (a que a Automação nível 2 usa): 1 por ponto de dano, limitada aos disponíveis.
- Recompensa `= dinheiro por ponto de dano × dano`. Casa mais quebrada paga mais.
- Armazém: continua 1 material por trabalhador (mais com o upgrade `cart`), com tempo fixo.

## 4. Problemas das casas

| Estado | Como surge | Como resolve | Estado |
|---|---|---|---|
| `Damaged` | Terremoto, tempestade | Equipe + material (`damageAmount`) | [CONFIRMADO] |
| `OnFire` | Raio de tempestade, evento isolado | Equipe, sem material | [CONFIRMADO] que existe; regras abaixo são [PROPOSTA] |
| `PowerOutage` | — | — | **[DECISÃO] cortado da jam.** O valor continua no enum `State` (a cena guarda o estado como número; remover ou reordenar o enum corrompe as casas), mas nada o usa. O modo de visualização do Tab sai do jogo. |

**Incêndio [CONFIRMADO]:**
- Não gasta material e o trabalho é mais rápido que conserto.
- Tem um temporizador visível. Se zerar sem equipe trabalhando no local, o fogo apaga sozinho e a casa vira `Damaged` com dano 3. Equipe que já está trabalhando para o temporizador.
- Enquanto queima, pesa 2 no caos (casa danificada pesa 1).
- Sem propagação para vizinhas.

**Só casa intacta recebe problema novo [DECISÃO]:** desastre que cai em casa já quebrada ou em chamas não faz nada. Evita chamado sobrescrito e equipe presa.

**Casa não atendida [CONFIRMADO]:** fica quebrada indefinidamente. Não vira ruína.

## 5. Desastres

**[PROPOSTA]** `DisasterManager` sorteia desastres numa linha do tempo que acelera:

- Intervalo entre desastres começa em ~35 s e cai até ~12 s ao longo de 10 minutos.
- Intensidade (número de casas afetadas) cresce com o tempo de partida.
- Aviso 3 s antes: faixa na tela + sirene + ícone do tipo.
- **[DECISÃO]** O primeiro desastre vem cerca de 6 s depois de o jogador clicar em Jogar e é sempre um terremoto fraco (2 casas) perto da Guarda: é o tutorial. Vinte segundos sem nada para fazer eram um começo morto.
- **[DECISÃO]** O dano do terremoto chega em onda: cada casa quebra com um atraso proporcional à distância do epicentro. Quem faz o atraso é a lógica; a apresentação só reage a cada casa.

| Desastre | Efeito | Apresentação |
|---|---|---|
| Terremoto | Epicentro aleatório + raio. Casas dentro do raio viram `Damaged`; dano maior perto do centro. | Tremor de câmera, casas balançam em onda a partir do epicentro, poeira. |
| Tempestade | Dura ~8 s. Casas espalhadas viram `Damaged` uma a uma. Cada raio tem chance de iniciar `OnFire`. | Chuva em overlay, tela escurece, flashes de relâmpago. |
| Incêndio isolado | 1 a 2 casas `OnFire`. | Fogo animado, fumaça. |

Guarda e Armazéns nunca são afetados.

## 6. Caos e derrota

**[CONFIRMADO]** Muitas casas com problema ao mesmo tempo geram caos; caos cheio = derrota.

**[PROPOSTA]** de fórmula:
- `alvo = soma dos pesos das casas com problema / limite`, onde limite inicial = 20 (cerca de 28% das 70 casas). Toda casa que não está `Intact` conta, mesmo com equipe a caminho ou trabalhando.
- A barra de caos persegue o alvo a ~6% por segundo para cima e ~10% por segundo para baixo. Isso dá tempo de reagir a um pico.
- Barra em 100% = derrota. Tela de fim com tempo sobrevivido, casas consertadas e dinheiro total.
- A partir de 60% a apresentação muda: vinheta vermelha, música tensa, sirene ao fundo.

## 7. Upgrades

Comprados na aba de upgrades (já existe o botão e o prefab `UpgradeButton`). Custo `= custoBase × crescimento^nível`.

**[PROPOSTA]** de lista:

| ID | Nome | Efeito por nível | Níveis | Custo base | Cresc. |
|---|---|---|---|---|---|
| `workers` | Contratar trabalhador | +1 trabalhador | 12 | 30 | 1,45 |
| `speed` | Botas de corrida | +12% velocidade de caminhada | 8 | 25 | 1,5 |
| `tools` | Ferramentas melhores | −10% tempo de trabalho | 8 | 40 | 1,5 |
| `cart` | Carrinho de mão | +1 material por trabalhador no Armazém | 4 | 60 | 1,8 |
| `reward` | Seguro municipal | +25% dinheiro por conserto | 10 | 50 | 1,55 |
| `automation` | Automação | Remove passos do despacho (abaixo) | 3 (v5A; era 4) | 150 | 2,6 |
| `outpost` | Guarita (v5A) | Despacho sozinho num quarteirão escolhido (`GUARITAS.md`) | 1 por quarteirão | 90 | 1,25 |
| `income` | Coleta de Impostos | Nível 1 destrava o repasse; cada nível aumenta o valor (seção 8) | 5 | 200 | 2,0 |
| `base2` | Segunda base (stretch S2) | Liga a segunda Guarda | 1 | 400 | — |
| `goal` | Centro de Operações | Vitória | 1 | 5.000 **PLACEHOLDER** | — |

**Custo da meta [DECISÃO]:** os ~15.000 da proposta não fecham com o resto da tabela. Conta de padaria (não é playtest): uma partida boa de 12 minutos rende perto de 9 a 10 mil (uns 150 consertos a ~40 e o repasse a partir do minuto 3), e os upgrades que o jogador precisa para aguentar o ritmo custam perto de 5 mil. Sobram 4 a 5 mil para a meta. O valor de verdade sai do T25, com partida jogada.

**Automação [CONFIRMADO o conceito; níveis são DECISÃO]:**

| Nível | Nome | O que muda |
|---|---|---|
| 0 | — | Casa → base → tamanho da equipe → enviar. Coleta manual. |
| 1 | Central de rádio | A base é escolhida sozinha (a mais próxima da casa). Some o passo "escolher a base". |
| 2 | Protocolo padrão | A equipe é dimensionada sozinha (a equipe sugerida da seção 3.1). Despachar vira 1 clique na casa. |
| 3 | Coleta automática | A recompensa entra sozinha quando o trabalho termina. Vale também para o Armazém. |
| ~~4~~ | ~~Despacho automático~~ | **Removido na v5A (pedido do Pietro: tirava a graça).** Virou as **Guaritas** (`GUARITAS.md`): compradas uma por quarteirão, o jogador escolhe onde; só os chamados daquele quarteirão saem sozinhos. |

Com as guaritas o jogador vira gestor aos poucos e escolhendo onde: decide upgrades, reabastece material e cobre o resto da cidade no clique.

**Trabalhadores são um estoque único da cidade [DECISÃO]:** não existe "gente por base". A base só define de onde a equipe sai e para onde volta. Isso mantém o código atual e deixa a base extra barata de fazer.

**Base extra [DECISÃO]:** no máximo 1, como upgrade `base2` de nível único que liga uma segunda Guarda já posicionada na cena (o MEGABRAIN escolhe o lugar, no lado oposto da cidade). É a primeira coisa a cair se o Dia 2 apertar (S2).

## 8. Renda passiva [CONFIRMADO em 16/11: opção A]

O usuário preferiu um sistema a um upgrade simples e escolheu a opção A. A opção B fica só como registro.

- **A. Coleta de Impostos (ESCOLHIDA).** Destravado pelo upgrade `income`. A cada 10 s a Prefeitura paga `valor por casa × nível do upgrade × casas intactas × (1 − caos)`. Usa a mesma barra de caos: cidade em ordem rende mais, cidade em crise rende menos. Um medidor a mais para o jogador gerir, sem tela nova. Cada pagamento dispara `PassiveIncomePaid`, para a apresentação mostrar de onde veio o dinheiro.
- **B. Imposto por casa.** Upgrade simples: cada casa intacta paga X por segundo.

## 9. Fim de jogo

- **Vitória [CONFIRMADO]:** comprar o "Centro de Operações", com uma animação curta da cidade comemorando.
- **Derrota [CONFIRMADO]:** caos em 100%.
- **Save [DECISÃO]:** sem save. Partida única e curta. Melhor tempo em PlayerPrefs é stretch (S4).
- **Menu na mesma cena [DECISÃO]:** não há cena de menu. A partida abre em `GameState.Menu`, com a cidade viva ao fundo e o painel do menu por cima; desastres só rodam em `Playing`. O clique em Jogar também libera o áudio no WebGL.
- **Reinício [DECISÃO]:** recarrega a cena (`SceneManager.LoadScene`), depois de matar os tweens e voltar `Time.timeScale` a 1.

## 10. Feel e apresentação (obrigatório, não opcional)

Já existe: hover/press com DOTween nas casas (`Tweener`), shader com camada de rachadura (`_TextureLayer1Fade`), círculo de progresso.

A implementar, em ordem de impacto:
1. Tremor de câmera e onda de balanço nas casas no terremoto.
2. Rachadura entra com fade + sacudida + poeira quando a casa quebra; "pop" + brilho quando é consertada.
3. Popup "+10" que voa até o contador de dinheiro; contador conta animado e o ícone dá um pulo.
4. Marcador de chamado pulsando; cor/ícone por tipo de problema.
5. Trabalhadores com balanço ao andar, virando para a direção do movimento, com pop ao sair e entrar.
6. Som em tudo: clique, envio, martelo, moeda, compra, sirene, trovão, chuva, fogo. Música que tensiona com o caos.
7. Chuva em overlay, flash de relâmpago, fogo animado com fumaça.
8. Vinheta e saturação reagindo ao caos (o `Global Volume` já está na cena).
9. Botões de upgrade: brilho quando há dinheiro para comprar, punch na compra.
10. Aviso de desastre com faixa animada (Text Animator já está no projeto).

## 11. Plataforma

**[CONFIRMADO]** Windows e WebGL. Windows é a entrega garantida. WebGL passa por teste de fumaça no Dia 1; se não rodar bem até o fim do Dia 2, fica só Windows.

## 12. Pendências de design

| ID | Pergunta | Bloqueia |
|---|---|---|
| ~~P1~~ | RESOLVIDA: Coleta de Impostos (opção A). | — |
| ~~P2~~ | RESOLVIDA: regras do incêndio da seção 4 aprovadas, sem propagação na v1. | — |
| ~~P3~~ | RESOLVIDA: bases extras compráveis. O MEGABRAIN reduziu para no máximo 1 (seção 7), como stretch S2. | — |
| ~~P4~~ | RESOLVIDA: "Centro de Operações". | — |
| ~~P5~~ | RESOLVIDA: tema "Citizen Tech"; trabalho até qua 18/11, envio qui 19/11; itch.io com Windows e WebGL. Falta só a hora exata do envio, que o usuário confere na página da jam. | — |
| ~~P6~~ | RESOLVIDA: só português. A fonte `ModernDOS` já tem os acentos (conferido no asset: 207 caracteres, nenhum acento do português faltando). | — |
| ~~P7~~ | RESOLVIDA: MEGABRAIN e B no PC 1, C no PC 2. O B não precisa de Unity e pode rodar em qualquer PC com o repositório. | — |

## 13. Valores iniciais de balanceamento [DECISÃO, todos PLACEHOLDER]

Substitui a planilha do T16. São o ponto de partida do `BalanceData` e do menu `Tools/Jam/Create Default Data`; a calibragem de verdade é o T25, com partida jogada. Nomes dos campos são sugestão para o Agente B. Valores que estavam na cena e nos prefabs em 16/11, antes do `BalanceData` (lidos na U4): `GPS.speed` 1; `workTime` 10 s fixo no `House.prefab` e 2 s no `Warehouse.prefab`; `moneyPerCall` 10; material inicial 0.

| Campo | Valor | Nota |
|---|---|---|
| `startWorkers` / `startMaterial` / `startMoney` | 3 / 5 / 0 | |
| `workerSpeed` | 1 | Lido no `GPS` da cena em 16/11 (U4): `speed` = 1. Da Guarda até uma casa do centro são 13,3 unidades, cerca de 13 s de caminhada. |
| `dispatchInterval` | 0,8 s | Intervalo entre um trabalhador e outro ao sair. Hoje é número solto no `GuardManager`. |
| `workTimePerDamage` / `minWorkTime` | 5 s / 2 s | Tempo = 5 × dano ÷ equipe, nunca menos de 2 s. |
| `fireWorkTime` | 4 s | Dividido pela equipe, mesmo mínimo. |
| `restockTime` | 5 s | Armazém. |
| `moneyPerDamage` / `fireReward` | 10 / 15 | |
| `fireDuration` | 25 s | Temporizador do incêndio. |
| `chaosWeightDamaged` / `chaosWeightFire` | 1 / 2 | |
| `chaosLimit` | 20 | |
| `chaosRisePerSecond` / `chaosFallPerSecond` | 0,06 / 0,10 | |
| `firstDisasterDelay` / `firstDisasterHouses` | 6 s / 2 | |
| `disasterIntervalStart` / `disasterIntervalEnd` | 35 s / 12 s | Cai em linha reta ao longo de `disasterRampDuration`. |
| `disasterRampDuration` | 600 s | |
| `disasterHousesStart` / `disasterHousesEnd` | 3 / 9 | Casas por desastre, mesma rampa. |
| `warningTime` | 3 s | |
| `earthquakeWaveDelayPerUnit` | 0,15 s | Atraso da onda por unidade de distância. |
| `stormDuration` / `lightningFireChance` | 8 s / 20% | |
| `stormUnlockTime` / `fireUnlockTime` | 90 s / 180 s | Antes disso só terremoto: um tipo novo de cada vez. |
| Pesos do sorteio (terremoto / tempestade / incêndio) | 50 / 35 / 15 | Entre os tipos já liberados. |
| `incomeInterval` / `incomePerHouse` | 10 s / 0,5 | Repasse = 0,5 × nível × casas intactas × (1 − caos), arredondado. |

Dano por tipo: terremoto dá 3 perto do epicentro, 2 no meio do raio e 1 na borda; tempestade dá 1 ou 2.

Para o playtest: o `GameManager` tem um botão de debug (Odin) que acelera o tempo em 4×, só no Editor.
