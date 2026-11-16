# Decisões da jam

Registro curto. Uma linha por decisão: o que, quem decidiu. Valores provisórios levam **PLACEHOLDER**.
As decisões da etapa do gerador de cidade continuam em `docs/decisions.md`.

## 2026-11-16 — Abertura (respostas do usuário ao organizador)

**Processo**
- 3 agentes (3 assinaturas), **2 PCs, mesmo repositório, branches separadas**. — usuário
- Branch de integração `jam`, criada a partir da `main`; `jam-sistemas` e `jam-feel` para os outros agentes. Nada vai para a `main` até o fim. — organizador (padrão; pode ser renomeada)
- Um único dono da cena `Gameplay.unity` e dos prefabs existentes (Agente A). — proposta do organizador; confirmar
- Arte e áudio são feitos fora do projeto, pedidos por lista (`ART_REQUESTS.md`). — usuário

**Jogo**
- Conceito: incremental em que desastres (terremotos, tempestades) quebram casas e o jogador envia trabalhadores para consertar; upgrades aumentam a eficiência e o dinheiro. — usuário
- Despacho: **manter os 3 passos** (casa → base → equipe) e **ir reduzindo com upgrades de Automação** (nível 1, 2 etc.). — usuário
- **Há derrota:** muitas casas com problema geram caos na cidade. — usuário
- Casa não atendida **fica quebrada**; não vira ruína. Caos = proporção de casas com problema ao mesmo tempo; barra cheia = derrota. — usuário
- Escopo mantido: **Material + Armazém** e **Incêndio**. — usuário
- **Energia/postes: só se houver tempo**, como gestão paralela que adiciona um mínimo de profundidade. — usuário
- Kits de primeiros socorros: cortados e escondidos do HUD. — usuário (não marcado na pergunta de escopo)
- Dinheiro: **começa só por conserto**; renda passiva vem depois, por upgrades ou, de preferência, por um sistema próprio. — usuário. Qual sistema: pendente (P1).
- Fim sem derrota: **meta final + vitória**. — usuário
- Plataforma: **Windows e WebGL**. Isso **revoga** a decisão anterior "sem build WebGL" de `docs/decisions.md`. — usuário

**Respostas do segundo formulário (16/11, 08:30)**
- Renda passiva = **Repasse da Prefeitura**, ligado ao caos (P1). — usuário
- Incêndio com as regras propostas, sem propagação na v1 (P2). — usuário
- **1 a 2 bases extras compráveis** (P3). — usuário
- Haverá um agente "ultra" chamado **MEGABRAIN**, que organiza e delega todas as tarefas e **NÃO é o chat principal**. Papel: orquestrador acima dos agentes. — usuário
- O MEGABRAIN **usa uma das 3 assinaturas** (sobram 2 executores). Decisão do organizador para fechar a conta: **o papel de Agente A (cena, integração, builds) é absorvido pelo MEGABRAIN**; B e C seguem como estão. Confirmar. Distribuição por PC mantida (MEGABRAIN+B no PC 1, C no PC 2). — usuário (PCs) / organizador (fusão)
- Este chat fica como **consultor de design e docs**: GDD, balanceamento, lista de arte, decisões. A execução é do MEGABRAIN. — usuário
- Preferência de colaboração: perguntas em lote vão em **formulário curto**. — usuário

**Respostas do terceiro formulário (16/11, 08:35)**
- Objetivo final: **Centro de Operações** (P4). — usuário
- Tema oficial da jam: **"Citizen Tech"**; o usuário considera o jogo já totalmente no tema. Entrega **qui 19/11**; o time trabalha **até qua 18/11** por segurança, e o dia 19 fica só para envio. — usuário
- Idioma: **só português** (P6). — usuário
- Publicação: **itch.io**, com Windows para download e WebGL jogável na página (P5). — usuário

**Autoridade (16/11, 08:32)**
- **A partir de agora a decisão final é sempre do MEGABRAIN.** Ele pode rever [PROPOSTA] e [CONFIRMADO], registrando o motivo aqui. Isso substitui a regra antiga "dúvida = perguntar ao usuário" para os agentes. — usuário
- Master prompt em `docs/jam/MEGABRAIN.md`. — organizador

**Técnico**
- O "CLAUDE.md" do gerador de cidade é substituído, dentro da branch `jam`, pelo `docs/jam/CLAUDE-jam.md`. A regra antiga "só alterar `Assets/_Project/` e `docs/`" deixa de valer na jam; valem os donos de arquivos de `AGENTS.md`. — organizador; confirmar
- `CameraController.cs` continua sem alteração; tremor de câmera é componente separado. — mantém decisão anterior do usuário
- A cidade gerada (`[CG] ...`) não é regerada durante a jam sem pedido. — organizador

## 2026-11-16 — Revisão do MEGABRAIN (sessão sem Unity)

Feita lendo os docs e o código, sem abrir o Editor (o PC desta sessão não tem o Unity 6000.6.7f1). Nada aqui foi testado em Play Mode. Uma linha por decisão: o que, por quê.

**Processo**
- Itens marcados "confirmar" acima ficam confirmados: dono único da cena, `CLAUDE.md` da jam, fusão do Agente A no MEGABRAIN. — sem dono único da cena o merge não fecha em tempo de jam.
- `docs/jam/**` passa a ser do MEGABRAIN; B e C só mexem em Status e "Pedidos entre agentes" do `TASKS.md`. — o master prompt entrega os docs a ele; três agentes reescrevendo o backlog dão conflito.
- Quem cria arquivo sem o Unity escreve o `.meta` à mão no mesmo commit. — o B trabalha sem Unity; sem `.meta`, cada PC gera um GUID diferente para o mesmo script e as referências da cena quebram no merge.
- O B não precisa do PC com o Unity: pode começar já, em qualquer PC com o repositório. — o Unity não está disponível agora e o T03 é o que destrava o C.
- T00 e T01 foram divididos: a parte de git e código está pronta; MCP, builds, capturas, menu de dados e cena viraram as pendências U1 a U8 do `TASKS.md`. — não dava para fazer sem o Editor, e misturar deixava o status mentiroso.
- T16 (planilha do organizador) vira a seção 13 do GDD, feita pelo MEGABRAIN, com todos os valores PLACEHOLDER. — o B precisa de números hoje para o `BalanceData`; a calibragem de verdade é o T25, jogando.
- Sons provisórios só CC0, com origem anotada. — a textura de rachadura já entrou com licença incerta; não repetir no áudio.

**Jogo**
- Material inicial 5. — o primeiro conserto não pode exigir ida ao Armazém.
- Material e trabalhadores saem do estoque no envio, não na coleta nem um a um. — resolve o B6 (coleta que paga sem consertar) e o B15 (estoque negativo com dois chamados).
- O tamanho da equipe passa a valer: tempo = tempo por dano × dano ÷ equipe; recompensa = dinheiro por dano × dano. — hoje escolher 1 ou 3 trabalhadores não muda nada, e o passo "escolher a equipe" é [CONFIRMADO]; passo sem consequência é clique morto.
- Só casa intacta recebe problema novo. — evita chamado sobrescrito e equipe presa numa casa que mudou de estado.
- Primeiro desastre em ~6 s (era 20 s), terremoto fraco de 2 casas perto da Guarda. — vinte segundos parado no começo matam a primeira impressão; o primeiro terremoto é o tutorial.
- Tempestade liberada aos 90 s e incêndio isolado aos 180 s. — um tipo novo de cada vez deixa a pressão legível.
- Onda do terremoto feita pela lógica (atraso por distância ao epicentro). — a apresentação reage casa a casa sem precisar conhecer o epicentro.
- Meta final cai de ~15.000 para 5.000 PLACEHOLDER. — conta estimada: uma partida de 12 min rende 9 a 10 mil e os upgrades necessários custam perto de 5 mil; com 15.000 a partida passaria muito de 15 min. Ajustar no T25.
- Upgrade `income`: 5 níveis, base 200, crescimento 2,0; repasse = 0,5 × nível × casas intactas × (1 − caos) a cada 10 s. — estava "a definir" e bloqueava o T23.
- Trabalhadores são um estoque único da cidade, não por base. — é como o código já funciona; "gente por base" pediria refatorar `ResourcesManager`, HUD e despacho.
- Base extra: no máximo 1 (upgrade `base2`), como stretch S2 e primeira a cair. — o usuário confirmou 1 a 2; com estoque único uma base a mais custa pouco, duas não acrescentam decisão nova.
- Energia e postes (S1) e propagação de incêndio (S3): cortados. `State.PowerOutage` fica no enum sem uso e a ação do Tab é desligada na cena. — competem com o polimento do núcleo; o enum não pode perder valores porque a cena guarda o estado como número.
- Menu na mesma cena, por cima da cidade; reinício recarrega a cena. — a regra é cena única; o clique em Jogar libera o áudio no WebGL.
- Sem save; melhor tempo em PlayerPrefs é stretch (S4). — partida curta.
- Contrato `GameEvents` ganhou `PassiveIncomePaid`, `WorkerSpawned`, `WorkerArrived`, `ActionDenied` e `LightningStruck`, e a lista do que o C pode ler dos sistemas. — repasse, trabalhadores, ação negada e raio ficariam sem resposta visual e sonora; fechar isso antes de B e C começarem evita mudar contrato no meio.
- `ChaosBar` e `EndScreen` provisória passam para o C (T17), como prefabs novos. — estavam no T14 do MEGABRAIN, que não escreve UI; a derrota do Dia 1 precisa ser legível.
- Entrou o T33 (juice dos botões de upgrade e da ação negada). — o item 9 da lista de feel do GDD não tinha tarefa.
- `GuardManager` segue chamando `FeedbacksManager` direto no Dia 1; troca por evento no T29. — já funciona; trocar agora cria dependência entre B e C sem ganho.

**Achados na leitura do código (viraram B14 a B18 em `ARCHITECTURE.md`)**
- Abrir outro chamado enquanto uma equipe ainda está saindo trava a casa (B14) e deixa o estoque de trabalhadores negativo (B15).
- No Armazém, `Awake` e `Start` da classe base não rodam (B16). A confirmar no Play.
- Envio negado não tem resposta nenhuma (B17).
- A fonte `ModernDOS` tem todos os acentos do português (207 caracteres no asset). — tira o risco de texto com quadradinhos.

**WebGL: critério de go/no-go (para a U3)**
- Go: carrega em até 20 s servido localmente, 50 FPS ou mais com a câmera parada e 40 ou mais com a câmera em movimento no PC do usuário, sem erro no console do navegador.
- No-go no Dia 1 não cancela: repetir a medição no fim do Dia 2 (T32). Windows é a entrega garantida.

## Pendências

Design: nenhuma (P1 a P7 resolvidas; ver o fim do `GDD.md`). Do Unity: U1 a U8 no `TASKS.md`.
