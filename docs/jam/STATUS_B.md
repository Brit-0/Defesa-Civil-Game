# Status do Agente B (Sistemas)

Só o Agente B edita este arquivo. O MEGABRAIN lê a cada integração, atende os pedidos na cena e passa o status para o `TASKS.md`.

**Como o B confere sem o Unity:** além de reler o diff, cada entrega passa por uma checagem de tipos com o compilador C# do .NET, fora do Unity, contra as DLLs do Unity 6000.4.6f1 (o 6000.6.7f1 ainda não tinha DLLs instaladas), o DOTween, o Odin, a UI, o TextMeshPro e o Input System. Roda duas vezes: com `UNITY_EDITOR` e sem (como o build do jogador, sem as pastas `Editor/`). Pega erro de sintaxe, de tipo e API obsoleta; **não substitui a compilação no Unity 6000.6.7f1 (U5)** e não testa nada em Play.

## Tarefas

Formato: `- [x] T03 — o que entrou, hash do commit`

- [x] T03 — contrato: `GameEvents` (20 eventos, um `Raise...` por evento, limpeza em `SubsystemRegistration`), `GameState`, `DeniedReason`, `UpgradeData`, `DisasterData` + `DisasterType`, `GameManager` (estado, tempo, contadores, pausa, vitória, derrota, reinício, `autoStart`, botão de tempo acelerado só no Editor), `LocationsManager` (listas e consultas), `GameEventsLogger`. Checagem de tipos: 0 erros e 0 avisos nos dois modos. **Pedido: integrar já na `jam`, o Agente C depende disto.** Hash: `c4037be`.
- [x] T04 — `House` e `GuardManager`: `SetState` (único lugar que muda `currentState`, dispara `HouseStateChanged`), `ApplyProblem` (só casa comum e intacta), `externalFeedback`, equipe inteira e material debitados no envio, envio negado com `ActionDenied`, coleta que sempre conserta, tempo e recompensa pela equipe e pelo dano (números em campos provisórios do `GuardManager` até o T05), correções B4, B5, B6, B7, B8, B10, B14, B15, B16 e B17, eventos `CallOpened`, `TeamDispatched`, `WorkStarted`, `WorkCompleted`, `CallResolved`, `WorkerSpawned` e `WorkerArrived`. Checagem de tipos: 0 erros e 0 avisos nos dois modos. Hash: `8d1a821`.

## Como testar no Play

**T04 (um chamado e dois chamados em seguida).** Com `GameManager`, `LocationsManager` e `GameEventsLogger` na cena:
1. Material: até o T05 o estoque começa em 0. Clique no Armazém, na Guarda, `+`, Enviar; quando o círculo fechar, clique no Armazém para coletar (material sobe 1 por trabalhador). Repita até ter 4 ou mais.
2. Um chamado: selecione uma casa `[CG] House n`, botão `GenerateCall` do Odin. Clique na casa, na Guarda, `+`, Enviar. Esperado no console, nesta ordem: `HouseStateChanged Intact -> Damaged`, `CallOpened`, `TeamDispatched`, `WorkerSpawned`, `WorkerArrived`, `WorkStarted`, `WorkCompleted`; ao clicar na casa pronta: `HouseStateChanged Damaged -> Intact`, `CallResolved` com dinheiro = 10 × dano, e os trabalhadores voltando (`WorkerSpawned`/`WorkerArrived` de novo). O contador de trabalhadores cai de uma vez no envio e volta um a um.
3. Dois chamados em seguida (B14 e B15): `GenerateCall` em duas casas. Monte o primeiro com 2 trabalhadores e clique em Enviar; **antes de o segundo trabalhador sair da base**, clique na segunda casa, na Guarda, `+` e Enviar. Esperado: saem 2 trabalhadores para a primeira casa e 1 para a segunda, as duas casas chegam a "pronta para coletar", e o contador de trabalhadores nunca fica abaixo de 0. Um segundo `+` no segundo chamado é negado (`ActionDenied NoWorkers`).
4. Envio negado (B17): com material 0, monte um chamado de casa danificada e clique em Enviar: `ActionDenied NoMaterial`, nada sai do estoque e a aba continua aberta. Enviar com equipe 0: `ActionDenied NoWorkers`.
5. Esc (B5): com um chamado aberto, uma tecla Esc fecha uma vez só.
6. Armazém (B16): passar o mouse no Armazém mostra o painel com nome e descrição preenchidos.

## Pedidos ao MEGABRAIN

O que criar e ligar na cena e nos prefabs. Formato: `- [ ] objeto, componente, cada campo e para onde aponta — por quê`

- [ ] (T03) `Scripts/GameManager`: GameObject novo com o componente `GameManager`. Nenhum campo para arrastar. `autoStart` fica ligado até o menu do C (T29) entrar; depois desligar. — sem ele a partida não tem estado, e nada bloqueia: o código antigo segue funcionando.
- [ ] (T03) `Scripts/LocationsManager`: GameObject novo com o componente `LocationsManager`. Nenhum campo. — desastres, caos e automação consultam as casas por ele.
- [ ] (T03, opcional) `Scripts/GameEventsLogger`: GameObject com o componente `GameEventsLogger`, só durante os testes. — imprime cada evento no console com o prefixo `[GameEvents]`.
- [ ] (T04, opcional) `House.prefab`, componente `House`, campos novos `damagedIcon` e `fireIcon` (Sprite): ícone que aparece dentro do círculo de progresso em casa danificada e em casa pegando fogo. — vazios, o ícone do círculo fica escondido (antes aparecia um quadrado branco, B7). Entram com a arte do T31.
- [ ] (T04) `GPS` ganhou dois campos com valor padrão, sem nada para arrastar: `arrivalDistance` (0,01) e `destroyDelay` (0,1 s, o tempo entre `WorkerArrived` e o trabalhador sumir). — se o pop de chegada do C (T27) precisar de mais tempo, é só aumentar `destroyDelay` na cena.

## Dúvidas para o MEGABRAIN

Formato: `- [ ] pergunta — o que eu assumi para seguir trabalhando`

- [ ] (T03) `FindObjectsByType<T>(FindObjectsSortMode)` está obsoleto nas DLLs do 6000.4.6f1 (aviso CS0618: o parâmetro de ordenação foi descontinuado). — usei `FindObjectsByType<House>()`, sem parâmetro, que existe a partir do 6000.4. Se o 6000.6.7f1 reclamar, é a única chamada em `LocationsManager.Refresh`.
- [ ] (T03) Em `Won` e `Lost` o tempo continua correndo (`Time.timeScale = 1`), para as telas finais do C animarem e a cidade seguir viva ao fundo; o que para é a lógica (desastres e caos só rodam em `Playing`) e o clique no mundo (`GameManager.InputBlocked`, que as casas passam a consultar no T04). — assumi isso; se a tela final precisar do jogo congelado, é uma linha em `Win()`/`Lose()`.
- [ ] (T03) `GameManager.State` é uma propriedade com o mesmo nome do enum `State` das casas. Dentro de `GameManager` o nome `State` passa a ser a propriedade; fora dele nada muda. — mantive porque o contrato (seção 6.1) pede `GameManager.Instance.State`. `HousesRepaired` conta `CallResolved` de chamado `Problem`; `TotalMoneyEarned` soma todo aumento de `MoneyChanged` (consertos e repasse).
- [ ] (T03) `UpgradeData` ganhou dois campos além do contrato: `order` (posição na aba) e `effectPerLevel` (o número do efeito, por exemplo 0,12 no `speed`), para o efeito não virar número no código. `DisasterData` vai ganhar peso do sorteio e tempo de liberação no T06/T20. — o C não precisa deles.
- [ ] (T04) A aba de despacho abre com equipe 0, como antes: todo envio pede pelo menos um `+`. — mantive para o fluxo de 3 passos não mudar; abrir já com 1 é uma linha em `GuardManager.SelectBase`.
- [ ] (T04) `+` sem trabalhador livre também dispara `ActionDenied(NoWorkers)`, com a posição da casa do chamado. — o GDD só fala do envio; assumi que todo clique negado merece resposta.
- [ ] (T04) Com a casa travada (equipe a caminho), o mouse saindo dela agora sempre desfaz o hover. — antes a casa ficava presa na escala 1,1 se fosse travada com o mouse em cima.
- [ ] (T04) O jogador não consegue cancelar uma equipe já enviada, e casa com equipe a caminho não aceita novo chamado. — o GDD não fala em cancelamento.
