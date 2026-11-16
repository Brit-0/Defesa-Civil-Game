# Revisão do gameloop (Agente B, jam-sistemas, 16/11)

Objetivo: o jogo ficar mais divertido e mais fácil de entender, com estratégia de verdade, mini games que pagam
mais que o automático (nunca obrigatórios) e um retorno visual e sonoro em cada ganho. Prazo: build na quarta 18/11.

## 1. O que confundia ou cansava (por prioridade)

| # | Problema | Efeito no jogador |
|---|---|---|
| 1 | Os `.asset` (BalanceData e Upgrades) ainda tinham os números do GDD | ninguém vencia: o simulador perdia todos os jogadores por volta de 4:07 |
| 2 | Não havia objetivo visível nos primeiros 30 s; a meta (Centro de Operações) só aparecia na aba de melhorias | "o que eu faço?" |
| 3 | Despachar pede 3 cliques (casa, base, Enviar) e só uma barra de texto (TutorialTips) explicava | o primeiro chamado travava |
| 4 | Entre um desastre e outro sobrava espera: mandar a equipe e olhar a barra encher | "é só esperar e comprar" |
| 5 | O ritmo era uma rampa lisa: todo desastre parecia igual e chegava sem dar tempo de reagir | sem pico, sem respiro |
| 6 | A recompensa era só o pagamento de cada casa: nada de marco, sequência ou bônus de rodada | incremental sem "número subindo" |

## 2. O que mudou (feito, por prioridade)

1. **Balanço que fecha** (`BalanceData.asset` e `Upgrades/*.asset` iguais aos padrões da classe):
   - trabalho 2.5 s, reposição 3 s, 15 por dano, 20 no incêndio;
   - limite de caos 22;
   - desastres de 45 s a 20 s em 13 min, de 3 a 6 casas, aviso de 4 s;
   - Automação 80 (crescimento 2), Carrinho 25, Renda 150, Centro de Operações 2500.
2. **Objetivo claro nos primeiros 30 s:**
   - cartão inicial com 4 linhas (quebra, conserta, compra, CAOS) e COMEÇAR ou PULAR;
   - painel **META** fixo no canto (Centro de Operações, barra dourada, valor contando e "PRÓXIMO MARCO: 3/5 CONSERTOS");
   - quando dá para comprar, fica verde, pulsa e diz "PRONTO! COMPRE EM MELHORIAS".
3. **Tutorial de 13 passos (cerca de 1,5 min):**
   - seta e aro apontam o próximo clique; quando o alvo sai da tela, a seta fica na borda e aponta para ele;
   - os desastres ficam parados enquanto o jogador lê, e esse tempo não conta na rampa;
   - um botão PULAR fica sempre à vista e o tutorial só roda na primeira partida;
   - depois dele vêm duas dicas de contexto, uma vez cada: mutirão no primeiro incêndio e terremoto forte.
4. **Três mini games** (seção 5) que preenchem a espera e pagam mais que o caminho automático.
5. **Ritmo com picos** (seção 4):
   - aviso com as casas na mira;
   - terremoto forte a cada 5 desastres, com aviso longo e calma depois.
6. **Recompensas de incremental** (seção 6): marcos com voluntários, bônus por fechar a ocorrência, sequência com tom subindo e números contando.

Tudo liga por código (`GameloopBootstrap`), **sem mexer em `Gameplay.unity`**. O `TutorialTips` de C é desligado em tempo de execução, porque o tutorial novo faz o papel dele.

## 3. Referências (jogo, mecânica, como entra no nosso)

| Jogo | Mecânica que copiamos | Como entra no nosso | Estado |
|---|---|---|---|
| They Are Billions | a onda é anunciada e mostra de onde vem; a preparação no aviso decide a defesa | `DisasterForecasted` traz as casas-alvo durante o aviso; no terremoto, aros laranja nelas e **escorar** com 1 material | feito |
| Frostpunk | a grande tempestade anunciada com antecedência: o jogador se prepara sob pressão e respira depois | **terremoto forte** a cada 5 desastres: casas x2, aviso de 6 s, 20 s a mais de calma; banner próprio (pedido a C) | feito |
| Incrementais clássicos (Cookie Clicker, AdVenture Capitalist) | marcos que destravam bônus, custo exponencial, número sempre subindo, som que sobe em sequência | **marcos** de consertos (5, 15, 30, 50, 80) com +1 voluntário; sequência de coletas com tom subindo; META contando | feito |
| Mini Metro | a cada "semana", uma recompensa à escolha (duas opções) | hoje o marco dá sempre 1 voluntário; escolher entre voluntário e material fica como ideia (seção 9) | parcial |
| Plague Inc., Kingdom Two Crowns, Dorfromantik, Rimworld | — | lidos e deixados de fora: pedem mapa, economia ou narrativa maiores que 3 dias | fora |

Fontes:
- [PC Gamer: They Are Billions, um RTS de defesa](https://www.pcgamer.com/they-are-billions-is-an-rts-thats-all-about-defense)
- [Game Informer: análise de Frostpunk](https://www.gameinformer.com/games/frostpunk/b/pc/archive/2018/04/23/frostpunk-review-a-frigid-unrelenting-survival-success.aspx)
- [Game Developer: The Math of Idle Games, parte III](https://gamedeveloper.com/design/the-math-of-idle-games-part-iii)
- [Envato Tuts+: a matemática dos incrementais](https://code.tutsplus.com/numbers-getting-bigger-the-design-and-math-of-incremental-games--cms-24023a)
- [Melhores upgrades no Mini Metro](https://hamy.xyz/blog/2024-03_mini-metro-best-upgrades)

## 4. Estratégia: decisões com troca

| Decisão | De um lado | Do outro |
|---|---|---|
| Escorar no aviso ou guardar material | 1 material por casa evita 2 de dano e paga 40 se a casa ficar de pé | cada trabalhador leva 1 material ao conserto; sem estoque a equipe não sai |
| Incêndio ou casa danificada primeiro | o fogo paga 20, mas depois de 25 s queima e vira dano 3 | a casa danificada enche o CAOS enquanto espera |
| Fechar a ocorrência ou espalhar | consertar todas as casas de um desastre dá 8 por casa, **x2 se em até 60 s** | espalhar a equipe atende mais gente de uma vez |
| Preparar o terremoto forte | guardar material e gente livre para o pico (o aviso longo dá tempo) | gastar tudo agora rende mais cedo |
| Ordem das melhorias | Carrinho resolve o material; Trabalhadores, mais equipes; Automação (80, x2) tira cliques | a meta custa 2500: cada compra atrasa a vitória |
| Tamanho da equipe | a aba já abre com a equipe sugerida | `+`/`-` troca velocidade por gente livre |

## 5. Mini games (nunca obrigatórios)

| Mini game | Quando | Acerto | Erro | Valores (`BalanceData`) |
|---|---|---|---|---|
| **Ponto certo** | qualquer trabalho em casa danificada ou no Armazém | clicar quando o círculo passa na faixa dourada: termina na hora e paga +50% (no Armazém, material a mais) | o progresso volta 20%; uma tentativa por trabalho | `critWindowMin/Max` .35–.7, `critWindowSize` .18, `critRewardBonus` .5, `missPenalty` .2 |
| **Mutirão** | equipe trabalhando em casa em chamas | cada clique adianta 10%; 5 cliques no mesmo fogo = herói, +50% | — | `fireClickProgress` .1, `fireHeroClicks` 5, `fireHeroBonus` .5 |
| **Escorar** | aviso de terremoto, nas casas marcadas | 1 material tira 2 de dano; se a casa fica de pé, +40 | — | `shoreMaterialCost` 1, `shoreDamageReduction` 2, `shoreHeldReward` 40 |

A automação segue sozinha: quem não joga os mini games vence do mesmo jeito, só que mais tarde (tabela da seção 7).

Retorno visual e sonoro de cada acerto (`MiniGameFeel`):
- faixa dourada no próprio círculo de progresso, que pulsa quando é a hora de clicar;
- popups "NO PONTO! +50%", "HEROICO!", "ESCORADA!" e "FICOU DE PÉ! +40", que empilham quando duas casas vizinhas pontuam juntas;
- 7 sons do Gamemaster, com o tom subindo 7% por acerto seguido (até 1,5) e voltando ao normal no erro.

## 6. Satisfação de incremental

- **Marcos:** +1 voluntário (trabalhador grátis) em 5, 15, 30, 50 e 80 consertos. A META mostra o próximo e dá um soco no marco.
- **Ocorrência encerrada:** `OccurrenceCleared` com o bônus.
- **Sequência:** coletas com até 5 s de intervalo formam a `RepairStreak`.
- **Números contando** na META (0,4 s), barra enchendo, pulso verde quando a meta fica pronta.

Pedidos a C: fanfarra do marco, popup e jingle da ocorrência, tom da sequência, círculo do aviso e banner do terremoto forte. Todos ficam no `TASKS.md`, em "Pedidos entre agentes", só escutando eventos, sem duplicar o `Feel`.

## 7. Simulador: antes e depois (30 sementes, mapa v5 com 69 casas)

Rodar: `cd docs/jam/sim` e depois `python -X utf8 tabela.py`. Com os `.asset` do jogo, acrescente `--assets ../../../Assets/Resources`; dá o mesmo resultado.

Antes, com os padrões da classe (903ee33):

| Jogador | Resultado |
|---|---|
| ignora | perde em 4:33 |
| bom | vence em 12:01 (11:42–12:17), 78 consertos |
| médio | vence em 13:24 (13:02–13:43) |
| bom, larga aos 6 min | perde em 8:36 |

Antes, com os `.asset` (números do GDD): todos perdiam em cerca de 4:07, e o "ignora" em 3:00.

Depois (os padrões e os `.asset` dão o mesmo):

| Jogador | Vence | Tempo até vencer ou perder | Consertos | Pico do caos | Pontos certos (erros) | Escoradas (de pé) | Bônus de ocorrência | Marcos |
|---|---|---|---|---|---|---|---|---|
| ignora | 0% | perde em 4:00 (3:52–4:56) | 0 | 1.00 | — | — | — | — |
| bom | 100% | 8:06 (7:52–8:34) | 23 | 0.23 | 7 (1) | 30 (28) | 448 | 2 |
| bom, sem mini games | 100% | 8:43 (8:29–8:56) | 54 | 0.45 | — | — | 1012 | 4 |
| médio | 100% | 9:27 (8:57–9:42) | 51 | 0.45 | 4 (3) | 8 (7) | 756 | 4 |
| médio, sem mini games | 100% | 9:59 (9:31–10:28) | 61 | 0.50 | — | — | 888 | 4 |
| bom, larga aos 6 min | 0% | perde em 8:37 (8:10–8:57) | 14 | 1.00 | 5 (1) | 19 (17) | 248 | 1 |

Leitura:
- A partida caiu de 12–13 min para 8–10 min, um tamanho melhor para a jam.
- Jogar os mini games adianta a vitória em cerca de 35 s (bom) e 30 s (médio), e o caos fica na metade (0.23 contra 0.45).
- Escorar troca conserto por casa de pé: menos consertos, mesmo dinheiro.
- O `shoreHeldReward` foi calibrado no simulador. Com 10, o bom com mini games vencia em 9:51, mais tarde que sem eles (8:46). Com 30, vencia em 8:42; com 45, em 7:53. Ficou 40.
- Quem larga o mouse aos 6 min ainda perde, como antes: a Automação ajuda, mas não substitui o jogador.

## 8. Prévias (montagem, não captura do Play)

O worktree não tem `Library`, e o Unity aberto é do MEGABRAIN. Por isso as imagens são uma **PRÉVIA 1920x1080** montada com os sprites reais (RoundRectangle, Circle, Button2) e a fonte ModernDOS sobre capturas do jogo. Ficam em `analises-lilith/jogo-defesa-civil/resultados/`:
- `B3_previa_1_cartao_inicial.png`
- `B3_previa_2_passo_clique_na_casa.png`
- `B3_previa_3_ponto_certo.png`
- `B3_previa_4_escorar_e_meta_pronta.png`

Correções feitas depois de olhar as prévias:
- o texto do cartão quebrava no meio da frase;
- os popups estavam grandes demais e se cobriam;
- o aviso de escorar era texto branco direto sobre a cidade e ganhou fundo escuro.

A captura do Play fica para depois do merge (pedido no `TASKS.md`).

## 9. Ideias que ficaram de fora (só se sobrar tempo)

- Marco com escolha à Mini Metro: voluntário ou +10 de material.
- Mini game de evacuação, ou de ligar fios no Armazém.
- Prestígio (recomeçar com bônus) e ganho offline: não cabem numa partida de 10 min.
