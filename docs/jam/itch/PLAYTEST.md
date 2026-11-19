# Roteiro de playtest de 15 minutos — Please Fix the City

Para o Pietro jogar (ou alguém que nunca viu o jogo, com o Pietro olhando). Serve para o T15, os 3 playtests do T40 e a conferência antes do envio. Escrito pelo Agente D em 16/11, com os tempos do `GDD.md` (seção 13); se o balanceamento mudar, os minutos abaixo mudam junto.

## Preparação (2 minutos, antes de começar)

- Jogar a **build** (Windows ou WebGL), não o Play do Editor: é o que o jogador vai ter.
- Gravar a tela: Barra de Jogo do Windows (`Win + Alt + R`) ou OBS. A gravação serve depois para os GIFs da página.
- Cronômetro do celular ao lado, começando no clique em **Jogar**.
- Se for outra pessoa jogando: **não explicar nada**. Só observar e anotar. Se ela travar por mais de 30 s, anotar onde e só então ajudar.
- Esta folha aberta para anotar os tempos na tabela do fim.

## Roteiro por minuto

### 0:00 a 1:00 — Menu e primeiro terremoto

- O menu aparece por cima da cidade viva? Os 3 passos são lidos ou pulados?
- Por volta de 0:06 vem o primeiro terremoto, fraco, em 2 casas perto da Guarda (é o tutorial).
- **Observar:** a faixa de aviso e a sirene chegam antes do tremor? O jogador entende onde clicar sem ajuda? Quanto tempo até o primeiro clique numa casa com problema?

### 1:00 a 3:00 — Loop manual

- Despacho em 3 passos: casa → base → tamanho da equipe → enviar.
- Trabalhadores andam até a casa, trabalham (círculo de progresso), a casa fica "pronta para coletar", clique para receber.
- O material começa em 5: lá pelo terceiro conserto ele acaba.
- **Observar:** o jogador percebe que equipe maior conserta mais rápido? Quando o material acaba, ele entende o aviso ("Sem material!") e descobre o Armazém sozinho? O "+N" voando até o dinheiro aparece?
- Por volta de 1:30 a tempestade passa a poder aparecer: chuva, tela escura, raios.

### 3:00 a 6:00 — Primeiras compras

- Primeiras melhorias: qual ele compra primeiro, e por quê?
- Por volta de 3:00 o incêndio isolado passa a aparecer: casa em chamas com temporizador, sem gasto de material, pesa o dobro no caos.
- **Observar:** o botão de melhorias foi achado sem ajuda? O brilho de "dá para comprar" chama a atenção? O incêndio é tratado como urgente?

### 6:00 a 10:00 — Automação e repasse

- Automação nível 1 (base escolhida sozinha) e 2 (equipe sozinha: despachar vira 1 clique); talvez o 3 (coleta automática).
- Repasse da Prefeitura: dinheiro entra a cada 10 s, menos quando o caos está alto.
- **Observar:** o jogador sente a diferença de cada nível? O repasse é notado ("+N Repasse")? Os desastres já estão vindo a cada 20 a 25 s; ele dá conta?

### 10:00 a 15:00 — Pressão e fim

- Desastres a cada ~12 s, com 6 a 9 casas.
- Caos acima de 60%: vinheta vermelha, música tensa, sirene ao fundo.
- Fim: comprar o **Centro de Operações** (vitória) ou caos em 100% (derrota). Tela de fim com tempo, casas consertadas e dinheiro; botão **Reiniciar**.
- **Observar:** o jogador sabe por que o caos subiu? Ele vê o Centro de Operações como meta antes do fim? A partida acabou entre 10 e 15 min? Reiniciar volta ao começo limpo, com a música sem cortar?

## Conferências de defeitos conhecidos (fazer de propósito, 1 minuto no total)

- **B14 / B15:** abrir um segundo chamado enquanto a primeira equipe ainda está saindo da base. A primeira casa tem que receber a equipe inteira; o número de trabalhadores nunca fica negativo.
- **B16:** clicar no Armazém e ler o painel de informação (nome, descrição, "1 por trabalhador").
- **B3:** passar o mouse numa casa e ver se só ela reage (as outras não podem pular junto).
- **Ação negada:** tentar enviar sem gente, sem material e comprar sem dinheiro: cada uma tem som de erro, tremida e texto curto.
- **Pausa:** `P` no meio de um terremoto e despausar: nada anda durante a pausa.

## Tabela de tempos (preencher durante o jogo)

| Momento | Tempo (mm:ss) |
|---|---|
| Primeiro clique numa casa com problema | |
| Primeiro envio de equipe | |
| Primeira coleta | |
| Primeira ida ao Armazém | |
| Primeira melhoria comprada (qual?) | |
| Automação nível 1 | |
| Repasse da Prefeitura | |
| Caos passou de 60% pela primeira vez | |
| Fim da partida (vitória ou derrota?) | |
| Dinheiro total e casas consertadas na tela de fim | |

Bugs: uma linha cada, com o tempo da gravação: `mm:ss — o que aconteceu — o que era esperado`.

## Perguntas ao Pietro (depois de jogar, 5 minutos)

Responder curto, de 1 a 5 onde houver número. As respostas vão para o MEGABRAIN (T25 e T40).

1. Nos primeiros 30 segundos, o que você entendeu que tinha que fazer? Algo ficou confuso?
2. Escolher o tamanho da equipe pareceu uma decisão de verdade ou um clique a mais?
3. O Armazém e o material ficaram claros, ou acabar o material pegou você de surpresa?
4. Qual melhoria pareceu a melhor? Alguma pareceu inútil ou cara demais?
5. A Automação chegou cedo, tarde ou na hora certa? Cada nível fez diferença?
6. Você sempre sabia por que o caos estava subindo? (1 = nunca, 5 = sempre)
7. Dificuldade geral (1 = fácil demais, 3 = certa, 5 = impossível). Em que minuto ficou difícil?
8. A partida teve o tamanho certo? Jogaria de novo logo em seguida?
9. Algum som cansou ou ficou alto demais (martelo, sirene, moeda)? A música acompanhou a tensão?
10. Alguma coisa na tela parece provisória ou feia (ícone, sprite, texto)?
11. Se desse para mudar uma coisa só antes da entrega, qual seria?

Perguntas da página do itch.io (respostas vão para o `PAGINA.md`):

12. Título: **Please Fix the City** (como no menu, decidido em 19/11).
13. Nomes e funções da equipe nos créditos (o git mostra Pietro Piccoli e Brit-0, Felipe C. Brito). Respondida em 19/11: equipe CryptCandle, Pietro Piccoli e Felipe Brito.
14. A arte da cidade veio do lote GS4 feito a partir dos kits da Kenney (CC0)? Quem deve ser creditado?
15. A jam pede para declarar uso de IA? Fica a linha nos créditos?
16. Nome da empresa no jogo (`Company Name`): respondida em 19/11, `CryptCandle`.
17. Qual o nome da jam e a hora exata do fim do envio na quinta?
