# Status do Agente B (Sistemas)

Só o Agente B edita este arquivo. O MEGABRAIN lê a cada integração, atende os pedidos na cena e passa o status para o `TASKS.md`.

**Como o B confere sem o Unity:** além de reler o diff, cada entrega passa por uma checagem de tipos com o compilador C# do .NET, fora do Unity, contra as DLLs do Unity 6000.4.6f1 (o 6000.6.7f1 ainda não tinha DLLs instaladas), o DOTween, o Odin, a UI, o TextMeshPro e o Input System. Roda duas vezes: com `UNITY_EDITOR` e sem (como o build do jogador, sem as pastas `Editor/`). Pega erro de sintaxe, de tipo e API obsoleta; **não substitui a compilação no Unity 6000.6.7f1 (U5)** e não testa nada em Play.

## Tarefas

Formato: `- [x] T03 — o que entrou, hash do commit`

- [x] T03 — contrato: `GameEvents` (20 eventos, um `Raise...` por evento, limpeza em `SubsystemRegistration`), `GameState`, `DeniedReason`, `UpgradeData`, `DisasterData` + `DisasterType`, `GameManager` (estado, tempo, contadores, pausa, vitória, derrota, reinício, `autoStart`, botão de tempo acelerado só no Editor), `LocationsManager` (listas e consultas), `GameEventsLogger`. Checagem de tipos: 0 erros e 0 avisos nos dois modos. **Pedido: integrar já na `jam`, o Agente C depende disto.** Hash: ver a linha do T03 em "Hashes".

## Hashes

_(preenchido a cada push)_

## Pedidos ao MEGABRAIN

O que criar e ligar na cena e nos prefabs. Formato: `- [ ] objeto, componente, cada campo e para onde aponta — por quê`

- [ ] (T03) `Scripts/GameManager`: GameObject novo com o componente `GameManager`. Nenhum campo para arrastar. `autoStart` fica ligado até o menu do C (T29) entrar; depois desligar. — sem ele a partida não tem estado, e nada bloqueia: o código antigo segue funcionando.
- [ ] (T03) `Scripts/LocationsManager`: GameObject novo com o componente `LocationsManager`. Nenhum campo. — desastres, caos e automação consultam as casas por ele.
- [ ] (T03, opcional) `Scripts/GameEventsLogger`: GameObject com o componente `GameEventsLogger`, só durante os testes. — imprime cada evento no console com o prefixo `[GameEvents]`.

## Dúvidas para o MEGABRAIN

Formato: `- [ ] pergunta — o que eu assumi para seguir trabalhando`

- [ ] (T03) `FindObjectsByType<T>(FindObjectsSortMode)` está obsoleto nas DLLs do 6000.4.6f1 (aviso CS0618: o parâmetro de ordenação foi descontinuado). — usei `FindObjectsByType<House>()`, sem parâmetro, que existe a partir do 6000.4. Se o 6000.6.7f1 reclamar, é a única chamada em `LocationsManager.Refresh`.
- [ ] (T03) Em `Won` e `Lost` o tempo continua correndo (`Time.timeScale = 1`), para as telas finais do C animarem e a cidade seguir viva ao fundo; o que para é a lógica (desastres e caos só rodam em `Playing`) e o clique no mundo (`GameManager.InputBlocked`, que as casas passam a consultar no T04). — assumi isso; se a tela final precisar do jogo congelado, é uma linha em `Win()`/`Lose()`.
- [ ] (T03) `GameManager.State` é uma propriedade com o mesmo nome do enum `State` das casas. Dentro de `GameManager` o nome `State` passa a ser a propriedade; fora dele nada muda. — mantive porque o contrato (seção 6.1) pede `GameManager.Instance.State`. `HousesRepaired` conta `CallResolved` de chamado `Problem`; `TotalMoneyEarned` soma todo aumento de `MoneyChanged` (consertos e repasse).
- [ ] (T03) `UpgradeData` ganhou dois campos além do contrato: `order` (posição na aba) e `effectPerLevel` (o número do efeito, por exemplo 0,12 no `speed`), para o efeito não virar número no código. `DisasterData` vai ganhar peso do sorteio e tempo de liberação no T06/T20. — o C não precisa deles.
