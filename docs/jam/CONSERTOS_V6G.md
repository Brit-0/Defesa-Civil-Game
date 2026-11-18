# Consertos v6G

## Trabalhadores obrigatórios no chamado

Chamado com mínimo de equipe (hoje a enchente pede 2 e o meteoro 8, `BlockDisasterData`) diz isso em quatro lugares; o número é sempre o `Call.workersNeeded` do líder, então qualquer chamado novo com mínimo aparece igual. A regra e os textos ficam em `Assets/Systems/UI/CallCrew.cs` (sem números de balanceamento).

- **Pino do mapa** (`MapMarkers.Crew.cs`, parcial do `MapMarkers`): placa dourada com o bonequinho do HUD e o número, embaixo do pino, enquanto o chamado espera equipe; some quando a equipe sai.
- **Painel da casa** (`CallCrewInfo.cs`): passar o mouse em qualquer casa do quarteirão abre a janela pixel da própria casa com o desastre, o que fazer e "Exige N trabalhadores" em dourado. Posto em runtime pelo `GameEvents.BlockIncidentStarted`, sem editar a cena nem o House.prefab.
- **Aba do chamado** (`CallEstimate.cs` e `GuardManager.TeamSizeText`): primeira linha "Exige N trabalhadores", o número ao lado do + vira "escolhidos/N" (vermelho até completar) e a dica conta quantos faltam ("Faltam 5 trabalhadores").
- **Aviso**: ENVIAR com menos que o mínimo não envia (como antes) e agora sobe "Exige 8: faltam 5!" acima da aba; + além do estoque troca "Sem trabalhadores" por "Exige 8 trabalhadores!" (`ActionFeedback`).

Testes: `CallCrewTests` (6, EditMode) cobrem mínimo, faltantes iguais à regra do `GuardManager`, textos, aviso, texto do + e os números reais da enchente e do meteoro. Conferido: 220 testes EditMode, `ValidateAll` 0 erros e 0 avisos, Play com enchente e meteoro forçados sem erro no console (prints `v6g_*` em `analises-lilith/jogo-defesa-civil/v6g/`).

## Trabalhadores 25% maiores no mapa (v6H)

`WorkerFeel.SizeFactor = 1.25f` (Assets/Systems/Feel/WorkerFeel.cs) é o único ajuste: o retângulo do trabalhador passa de 40,24 para 50,30 e o pivô sobe na mesma proporção, então os pés, a ordem de desenho por Y, o caminho, a colisão e a velocidade ficam como eram, e a sombra projetada, as pegadas, a fita, a poeira e o pulinho (todos em alturas do trabalhador) acompanham; o sprite segue Point, sem borrar. Conferido: 210 testes EditMode, `ValidateAll` 0 erros, Play curto sem exceções (prints antes/depois em `analises-lilith/jogo-defesa-civil/v6h/`).
