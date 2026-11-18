# Consertos v6G

## Trabalhadores 25% maiores no mapa (v6H)

`WorkerFeel.SizeFactor = 1.25f` (Assets/Systems/Feel/WorkerFeel.cs) é o único ajuste: o retângulo do trabalhador passa de 40,24 para 50,30 e o pivô sobe na mesma proporção, então os pés, a ordem de desenho por Y, o caminho, a colisão e a velocidade ficam como eram, e a sombra projetada, as pegadas, a fita, a poeira e o pulinho (todos em alturas do trabalhador) acompanham; o sprite segue Point, sem borrar. Conferido: 210 testes EditMode, `ValidateAll` 0 erros, Play curto sem exceções (prints antes/depois em `analises-lilith/jogo-defesa-civil/v6h/`).
