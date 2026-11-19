# Consertos v7B-2: abas, tutorial, meta, pausa e textos (19/11/2026)

Pedido do Pietro (Instruções MEGABRAIN 5), só a parte de UI. Testes em `Assets/Systems/Masters/Editor/HudV7B2Tests.cs`.

| Item | Antes | Agora | Onde |
|---|---|---|---|
| a) Abas acima dos efeitos do mapa | Seta e aro do tutorial numa casa (e as camadas do HUD criadas depois, como números e partículas) desenhavam por cima da aba de melhorias | Cada aba tem Canvas próprio na ordem 12000 (melhorias, equipe, meta, energia); balão do tutorial 12010; seta e aro numa casa 200 (abaixo das abas); seta e aro num botão do HUD 12020 (acima, para apontar dentro da aba). Resposta a clique que cai numa aba (confete da compra, "Sem dinheiro!") 12030. Engrenagem 14000, pausa 15000 e fim 20000 seguem por cima | `UI/HudTabs.cs`, `TutorialView`, `GoalTracker`, `EnergyHud`, `RewardFx`, `ActionFeedback` |
| b) Margens do balão do tutorial | PASSO 1/15 a 20 do topo e PULAR a 12 da base, colados na moldura | PASSO a 30 do topo, PULAR a 26 da base; balão 12 mais alto (172 → 184) para o texto não encolher | `TutorialView` (`stepTop`, `skipBottom`) |
| c) Barra da meta | Fundo era o painel arredondado, desenhado mais baixo que o retângulo; o amarelo enchia o retângulo inteiro e cobria o fundo | Moldura lisa de 3 px, trilho escuro por dentro e o amarelo com o mesmo retângulo do trilho | `GoalTracker` (`barEdge`, `Track`) |
| d) Sair na pausa | Continuar, Configurações, Reiniciar | + SAIR (Application.Quit; no Editor para o Play). No WebGL o botão não aparece e o painel fica com 3 opções | `PauseMenu.QuitAvailable` |
| e) Textos | Automação: "Cada nível tira um passo do despacho..." (128); o antigo Repasse: "A Prefeitura repassa parte do seu caixa..." (117) | Automação diz o que cada nível faz (126); **Coleta de Impostos**: "A cidade paga impostos de tempos em tempos: rende mais com casas inteiras e dinheiro em caixa. Caos alto corta." (111). Número que sobe: "+N Impostos" | `Upgrades/Automation.asset`, `Income.asset`, `JamDataMenu`, `Feel.prefab`, docs |

Ficou de fora: o comentário do `GameEvents.PassiveIncomePaid` ainda diz "Repasse" (arquivo em edição por outra ajudante);
o termo técnico "repasse" segue nos campos `income*` do BalanceData (não aparece para o jogador).

A aba de melhorias e o painel da equipe começam desligados, e o Unity não guarda o Override Sorting de um Canvas
desligado: o `HudTabSorting` reaplica a ordem toda vez que a aba liga.

Conferência (19/11): EditMode 239/239 (6 novos em `HudV7B2Tests`); Play com robô sem erro no console; prints antes e
depois de cada item em `analises-lilith/jogo-defesa-civil/v7b2/folha_*.png` (inclui o "Sem dinheiro!" por cima da aba).
