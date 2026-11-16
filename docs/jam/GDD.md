# GDD — Defesa Civil Game (jam)

Legenda: **[CONFIRMADO]** = decidido pelo usuário. **[PROPOSTA]** = sugestão do organizador, vale como padrão de implementação até o usuário mudar. **[PENDENTE]** = precisa de resposta antes de implementar.

Todos os números são valores iniciais de balanceamento e moram em `BalanceData` / ScriptableObjects, nunca no código.

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

**Material inicial [PROPOSTA]:** começar com 5, para o primeiro conserto não exigir ida ao Armazém antes de o jogador entender o loop.

## 4. Problemas das casas

| Estado | Como surge | Como resolve | Estado |
|---|---|---|---|
| `Damaged` | Terremoto, tempestade | Equipe + material (`damageAmount`) | [CONFIRMADO] |
| `OnFire` | Raio de tempestade, evento isolado | Equipe, sem material | [CONFIRMADO] que existe; regras abaixo são [PROPOSTA] |
| `PowerOutage` | Tempestade | Sistema de energia/postes | [CONFIRMADO] só se sobrar tempo (stretch) |

**Incêndio [PROPOSTA]:**
- Não gasta material e o trabalho é mais rápido que conserto.
- Tem um temporizador visível. Se zerar sem equipe no local, o fogo apaga sozinho e a casa vira `Damaged` com dano 3.
- Enquanto queima, pesa 2 no caos (casa danificada pesa 1).
- Sem propagação para vizinhas na v1. Entra só se sobrar tempo.

**Casa não atendida [CONFIRMADO]:** fica quebrada indefinidamente. Não vira ruína.

## 5. Desastres

**[PROPOSTA]** `DisasterManager` sorteia desastres numa linha do tempo que acelera:

- Intervalo entre desastres começa em ~35 s e cai até ~12 s ao longo de 10 minutos.
- Intensidade (número de casas afetadas) cresce com o tempo de partida.
- Aviso 3 s antes: faixa na tela + sirene + ícone do tipo.
- Os primeiros 20 s da partida não têm desastre; o primeiro é sempre um terremoto fraco (tutorial implícito).

| Desastre | Efeito | Apresentação |
|---|---|---|
| Terremoto | Epicentro aleatório + raio. Casas dentro do raio viram `Damaged`; dano maior perto do centro. | Tremor de câmera, casas balançam em onda a partir do epicentro, poeira. |
| Tempestade | Dura ~8 s. Casas espalhadas viram `Damaged` uma a uma. Cada raio tem chance de iniciar `OnFire`. | Chuva em overlay, tela escurece, flashes de relâmpago. |
| Incêndio isolado | 1 a 2 casas `OnFire`. | Fogo animado, fumaça. |

Guarda e Armazéns nunca são afetados.

## 6. Caos e derrota

**[CONFIRMADO]** Muitas casas com problema ao mesmo tempo geram caos; caos cheio = derrota.

**[PROPOSTA]** de fórmula:
- `alvo = soma dos pesos das casas com problema / limite`, onde limite inicial = 20 (cerca de 28% das 70 casas).
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
| `automation` | Automação | Remove passos do despacho (abaixo) | 4 | 150 | 2,6 |
| `income` | Renda passiva | Ver seção 8 | a definir | a definir | a definir |
| `goal` | Objetivo final | Vitória | 1 | ~15.000 | — |

**Automação [CONFIRMADO o conceito; níveis são PROPOSTA]:**

| Nível | Nome | O que muda |
|---|---|---|
| 0 | — | Casa → base → tamanho da equipe → enviar. Coleta manual. |
| 1 | Central de rádio | A base é escolhida sozinha (a mais próxima com gente). |
| 2 | Protocolo padrão | A equipe é dimensionada sozinha (mínimo necessário). Despachar vira 1 clique. |
| 3 | Coleta automática | A recompensa entra sozinha quando o trabalho termina. |
| 4 | Despacho automático | Chamados são atendidos sem clique enquanto houver gente e material. |

No nível 4 o jogador vira gestor: decide upgrades, reabastece material e cobre picos manualmente.

## 8. Renda passiva [CONFIRMADO em 16/11: opção A]

O usuário preferiu um sistema a um upgrade simples e escolheu a opção A. A opção B fica só como registro.

- **A. Repasse da Prefeitura (ESCOLHIDA).** Destravado por upgrade. A cada 10 s a Prefeitura paga `base × casas intactas × (1 − caos)`. Usa a mesma barra de caos: cidade em ordem rende mais, cidade em crise rende menos. Um medidor a mais para o jogador gerir, sem tela nova.
- **B. Imposto por casa.** Upgrade simples: cada casa intacta paga X por segundo.

## 9. Fim de jogo

- **Vitória [CONFIRMADO]:** comprar o objetivo final. Nome e tema em aberto (P4). Proposta: "Centro de Operações da Defesa Civil", com uma animação curta da cidade comemorando.
- **Derrota [CONFIRMADO]:** caos em 100%.
- **Save [PROPOSTA]:** sem save. Partida única e curta. Guardar só o melhor tempo em PlayerPrefs.

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
| ~~P1~~ | RESOLVIDA: Repasse da Prefeitura (opção A). | — |
| ~~P2~~ | RESOLVIDA: regras do incêndio da seção 4 aprovadas, sem propagação na v1. | — |
| ~~P3~~ | RESOLVIDA: 1 a 2 bases extras compráveis (novas Guardas em locais fixos). Falta definir quais estruturas viram Guarda extra e a arte (`Guard_S.png` serve de base). | T22 (Dia 2) |
| P4 | Nome e tema do objetivo final. | T24 (Dia 2) |
| P5 | Tema da jam, data/hora exata da entrega e onde será publicada. | Dia 3 |
| P6 | Idioma: só português ou também inglês? | T29 (Dia 2) |
| P7 | Em qual PC roda cada agente (ver `AGENTS.md`). | Kickoff |
