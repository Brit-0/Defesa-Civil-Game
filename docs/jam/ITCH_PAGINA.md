# Página do itch.io: Please Fix the City

Texto pronto para colar na página do jogo, conferido no jogo de 19/11 (v7D, equipe CryptCandle). A versão antiga, de 16/11, está na branch `jam-itch` (`docs/jam/itch/PAGINA.md`), com as notas de licença. **Nada foi enviado ao itch.io.**

Arquivos para subir (gerados em 19/11, ver `CONSERTOS_V7D.md`):

| Arquivo | Como marcar no itch.io |
|---|---|
| `Builds/itch/Please-Fix-the-City-WebGL.zip` | "This file will be played in the browser" (o `index.html` está na raiz do zip) |
| `Builds/itch/Please-Fix-the-City-Windows.zip` | "Windows" |

Prints e capa: `analises-lilith/jogo-defesa-civil/itch/` (a lista está no fim deste arquivo).

---

## Título

**Please Fix the City**

Autor/equipe: **CryptCandle**

URL sugerida: `<usuario>.itch.io/please-fix-the-city`

## Descrição curta (campo "Short description or tagline")

> Terremotos, incêndios, enchentes e meteoros quebram a cidade. Despache equipes e conserte antes que o caos vença.

(113 caracteres; o itch.io aceita no máximo 120.) Tema da página: BG #1b1a24, BG2 #2b2936, texto #f3ecd6, link #ffc83d, fonte Pixel, fundo `fundo_itch.png`.

## Descrição longa

> **A cidade treme. Você despacha.**
>
> Você comanda a Defesa Civil de uma cidade pequena. Terremotos, tempestades, incêndios, enchentes e meteoros danificam as casas, e cada casa com problema vira um chamado. Escolha a base, monte a equipe e mande os trabalhadores pelas ruas. Eles chegam, consertam, e você recebe o pagamento.
>
> Com o dinheiro, contrate mais gente, compre ferramentas, botas e carrinhos, abra bases e armazéns novos, ponha para-raios e guaritas nos quarteirões e invista em **Automação**: primeiro a base é escolhida sozinha, depois a equipe, e no fim a recompensa entra sem clique. Você deixa de correr atrás de cada casa e passa a gerir a operação.
>
> Mas os desastres ficam mais fortes e mais frequentes. Se casas demais ficarem com problema ao mesmo tempo, a barra de **caos** enche e a cidade se perde. Compre o **Centro de Operações** para vencer.
>
> - Três dificuldades: **Garoa**, **Temporal** e **Apocalipse**
> - Partidas de 10 a 20 minutos
> - Cinco desastres com aviso antes: terremoto em onda, tempestade com raios, incêndio, enchente e meteoro
> - Minijogos rápidos: apagar as chamas com o jato, girar a válvula do bueiro na enchente, acertar o ponto certo do conserto
> - Energia, Coleta de Impostos e upgrades que mudam o jeito de jogar
> - Pixel art, sons e música que ficam tensos junto com a cidade
>
> Feito em 3 dias para a Global Solution (GS) da **FIAP**, com o tema **Citizen Tech**, pela equipe **CryptCandle**.

## Como jogar

> 1. Escolha a dificuldade no menu. **Temporal** é o jogo como ele é.
> 2. Quando um desastre chega, as casas atingidas ganham um marcador. Clique numa casa com problema para abrir o chamado.
> 3. Escolha a base de onde a equipe sai e quantos trabalhadores vão. Equipe maior conserta mais rápido, mas sobra menos gente para os próximos chamados.
> 4. O conserto gasta **material**. Quando acabar, mande trabalhadores ao **Armazém** buscar mais.
> 5. Durante o conserto, clique quando o ponteiro passar no arco dourado: o trabalho acelera.
> 6. No incêndio, arraste o mouse por cima das chamas para apagar. Na enchente, segure o clique na válvula e gire o mouse em círculos.
> 7. Compre melhorias na aba do canto da tela (ou na tecla **Tab**).
> 8. Fique de olho na barra de **caos**: cheia, você perde. Compre o **Centro de Operações** para vencer.

## Controles

> | Ação | Controle |
> |---|---|
> | Abrir chamado, escolher, enviar, comprar | Clique esquerdo |
> | Apagar chamas, girar a válvula | Segurar o clique esquerdo e arrastar |
> | Mover a câmera | Segurar o botão direito (ou o do meio) e arrastar |
> | Zoom | Roda do mouse |
> | Aba de melhorias | Tab |
> | Cancelar o chamado aberto | Esc |
> | Pausar | P ou Esc |
> | Configurações (som, tela, créditos) | Engrenagem no canto da tela |
>
> Precisa de mouse. No navegador, o som começa depois do primeiro clique: é regra do navegador.

## Créditos

> **Criação: CryptCandle**
> Pietro Piccoli e Felipe Brito
>
> **Ferramentas:** Unity 6, DOTween (Demigiant), Feel e Nice Vibrations (More Mountains), Text Animator (Febucci), Odin Inspector (Sirenix)
>
> **Som e fonte:** Gamemaster Audio Pro Sound Collection, Sonniss GDC 2026 Bundle, música de síntese própria, fonte Modern DOS (Jayvee Enaguas, CC0)

É o mesmo texto da aba CRÉDITOS do jogo (`SettingsPanel.Rows.cs`). Se a jam pedir declaração de uso de IA, acrescentar a linha (ver `jam-itch`, `docs/jam/itch/PAGINA.md`).

## Configuração da página

| Campo do itch.io | Valor |
|---|---|
| Kind of project | HTML (o jogo roda na página; o Windows vai como download) |
| Classification | Games |
| Release status | Released |
| Pricing | No payments |
| Embed options | Embed in page; manually set size **960 x 600** |
| Frame options | Fullscreen button ligado; "Mobile friendly" desligado; "Automatically start on page load" desligado (o clique libera o áudio) |
| SharedArrayBuffer | Desligado (o jogo não usa threads) |
| Genre | Strategy (alternativa: Simulation) |
| Made with | Unity |
| Inputs | Mouse, Keyboard |
| Languages | Portuguese (Brazil) |
| Average session | A few minutes (uma partida dura 10 a 20 min) |

**Tags** (até 10): `incremental`, `management`, `strategy`, `disaster`, `pixel-art`, `2d`, `city`, `singleplayer`, `short`, `brazil`

## Texto do envio na página da jam

> Please Fix the City, da CryptCandle, é um incremental de gestão de desastres: despache equipes, conserte a cidade e automatize a operação antes que o caos vença. Jogável no navegador e para Windows, em português. Tema: Citizen Tech.

## Prints e capa

Em `analises-lilith/jogo-defesa-civil/itch/`, na ordem da página, todos em 1280 x 720:

1. `01_menu.png`: menu inicial sobre a cidade, as três dificuldades e CryptCandle no canto.
2. `02_terremoto_chegando.png`: faixa "TERREMOTO CHEGANDO!" com a contagem e a dica de escorar.
3. `03_despacho.png`: casas rachando com poeira e o painel do chamado (chegada, conserto, equipe, Enviar).
4. `04_conserto.png`: equipe trabalhando, círculo de progresso com o arco dourado.
5. `05_incendio.png`: casa pegando fogo, chamas em roda e a barra do jato.
6. `06_enchente.png`: quarteirão alagado com a válvula do bueiro.
7. `07_meteoro.png`: cratera do meteoro e o chamado da obra (exige 8 trabalhadores).
8. `08_melhorias.png`: aba de melhorias na Expansão (nova base, novo armazém, Centro de Operações, para-raio).

Capa: `capa_630x500.png` (recorte do incêndio, reduzido pela metade sem borrar, com o título na fonte do jogo). Os prints 2 a 5 são do jogo de 19/11; os 6 a 8, de 18/11 (a arte do mapa é a mesma).
