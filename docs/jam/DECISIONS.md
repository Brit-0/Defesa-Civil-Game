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

## Pendências

Ver a tabela "Pendências de design" no fim do `GDD.md` (P1 a P7).
