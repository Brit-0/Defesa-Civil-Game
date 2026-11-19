# Página do itch.io — Please Fix the City

Textos prontos para colar na página do jogo no itch.io. Escritos pelo Agente D em 16/11, a partir do `GDD.md`, do código das branches `jam`, `jam-feel` e `jam-sistemas` e dos `STATUS_*.md`. **Nada foi enviado ao itch.io.**

Antes de publicar, confira a seção "Conferir na build final" no fim deste arquivo: a página descreve o jogo do GDD, e o que for cortado até quarta tem que sair do texto.

---

## Título

**Please Fix the City**

Subtítulo (é o mesmo do menu do jogo, `MainMenu.cs`): **Mantenha a cidade de pé.**

Nome decidido em 19/11: **Please Fix the City**, igual à tela do jogo (o repositório e as pastas seguem `Defesa-Civil-Game`).

Equipe: **CryptCandle** (decidido em 19/11). É o `Company Name` do jogo, fica discreto no canto do menu inicial e abre a seção CRIAÇÃO dos créditos.

URL sugerida: `<usuario>.itch.io/please-fix-the-city`

## Descrição curta (campo "Short description or tagline")

> Terremotos, tempestades e incêndios quebram a cidade. Despache equipes, conserte casas e automatize a Defesa Civil antes que o caos vença.

(138 caracteres.)

## Descrição longa

> **A cidade treme. Você despacha.**
>
> Você comanda a Defesa Civil de uma cidade pequena. Terremotos, tempestades e incêndios danificam as casas, e cada casa com problema vira um chamado. Escolha a base, monte a equipe e mande os trabalhadores pelas ruas. Eles chegam, consertam, e você recebe o pagamento.
>
> Com o dinheiro, contrate mais gente, compre ferramentas, botas e carrinhos, e invista em **Automação**: primeiro a central de rádio escolhe a base sozinha, depois o protocolo padrão monta a equipe, a coleta fica automática e, no fim, os chamados são atendidos sem nenhum clique. Você deixa de correr atrás de cada casa e passa a gerir a operação.
>
> Mas os desastres ficam mais fortes e mais frequentes. Se casas demais ficarem com problema ao mesmo tempo, a barra de **caos** enche e a cidade se perde. Compre o **Centro de Operações** para vencer.
>
> - Partidas de 10 a 15 minutos
> - Três desastres com aviso antes: terremoto em onda a partir do epicentro, tempestade com raios e incêndios com tempo para apagar
> - Material buscado no Armazém, equipe que conserta mais rápido quanto maior for
> - Repasse da Prefeitura: renda que cai quando o caos sobe
> - Pixel art, sons e música que ficam tensos junto com a cidade
>
> Feito em 3 dias para a jam **[nome da jam]**, com o tema **Citizen Tech**.

## Como jogar

> 1. Quando um desastre chega, as casas atingidas ganham um marcador. Clique numa casa com problema para abrir o chamado.
> 2. Escolha a base de onde a equipe sai e quantos trabalhadores vão. Equipe maior conserta mais rápido, mas sobra menos gente para os próximos chamados.
> 3. Casa danificada gasta **material** no envio. Quando o material acabar, mande trabalhadores ao **Armazém**: cada um volta com material.
> 4. Quando o trabalho termina, clique na casa para receber o pagamento. A equipe volta para a base.
> 5. Compre melhorias no botão do canto inferior esquerdo. **Automação** tira passos do despacho; o **Repasse da Prefeitura** paga por casa intacta.
> 6. Fique de olho na barra de **caos** no topo: ela sobe com as casas com problema (incêndio pesa o dobro). Cheia, você perde.
> 7. Compre o **Centro de Operações** para vencer.

## Controles

> | Ação | Controle |
> |---|---|
> | Abrir chamado, escolher, enviar, coletar, comprar | Clique esquerdo |
> | Mover a câmera | Segurar o botão direito (ou o do meio) e arrastar |
> | Zoom | Roda do mouse |
> | Cancelar o chamado aberto | Esc |
> | Pausar | P ou o botão **II** no canto da tela |
>
> Precisa de mouse. No touchpad, o zoom é a rolagem com dois dedos.
> No navegador, o som começa depois do primeiro clique (no botão **Jogar**): é regra do navegador.

Fontes: `CameraController.cs` (botão direito ou do meio, roda), ação `Cancel` do `.inputactions` (Esc), `PauseMenu.cs` da `jam-feel` (P e botão "II").

## Tema: Citizen Tech

> A Defesa Civil é tecnologia a serviço de quem mora na cidade: rádio, protocolo, despacho e uma central que vê tudo. No jogo você começa no braço, atendendo cada chamado à mão, e vai comprando essa tecnologia pública: a central de rádio, o protocolo padrão, o despacho automático, o repasse da Prefeitura e, no fim, o Centro de Operações. A cidade só se salva quando a operação passa a funcionar sozinha.

## Créditos e licenças

Bloco para a página (o que está entre colchetes precisa de confirmação; ver a nota abaixo):

> **Criação: CryptCandle**
> - Pietro Piccoli e Felipe Brito
>
> **Som e música**
> - Gamemaster Audio — Pro Sound Collection v1.3 (licença comercial do pacote, comprada pela equipe). Efeitos, ambientes de chuva e fogo e as duas músicas (*Green Lake Serenade* e *Darkness Falls*) vêm desse pacote.
>
> **Fonte**
> - Modern DOS 8x16, de Jayvee Enaguas (HarvettFox96) — domínio público, CC0 1.0.
>
> **Arte**
> - [Sprites da cidade: pixel art da equipe a partir dos kits 3D da Kenney (kenney.nl), CC0.]
> - Padrões de efeito do Sprite Shaders Ultimate.
>
> **Ferramentas e plugins**
> - Unity 6 (URP 2D)
> - DOTween (Demigiant), Feel (More Mountains), Odin Inspector (Sirenix), Text Animator for Unity (Febucci), Sprite Shaders Ultimate (Ekincan Tas), Easy Save 3 (Moodkie) — licenças da Unity Asset Store.
>
> [Uso de IA: código escrito com ajuda de agentes de IA (Claude, da Anthropic).]

Notas para quem publica:

- **Nomes da equipe:** decidido em 19/11 pelo Pietro: equipe CryptCandle, com Pietro Piccoli e Felipe Brito, mais ninguém. É o mesmo bloco da aba CRÉDITOS do jogo (`SettingsPanel.Rows.cs`). Não publicar e-mails.
- **Sons Gamemaster:** a licença permite usar dentro do jogo, mas os arquivos soltos não podem ser redistribuídos (`STATUS_C.md`, seção "Sons provisórios"). O jogo compilado está liberado; **não publicar o repositório nem pôr link para ele** enquanto `Assets/Audio/` tiver os arquivos do pacote. A lista de cada som e a faixa de origem estão no `STATUS_C.md` da `jam-feel`.
- **Fonte ModernDOS:** CC0 conferido na própria fonte (`Assets/Fonts/ModernDOS8x16.ttf`, tabela de nomes: "Public Domain, Creative Commons Zero (CC0) 1.0"). Crédito não é obrigatório, mas é bom dar.
- **Arte da cidade:** os nomes dos sprites (`Casa_A`, `Fabrica_A`, `Predio_A`, carros, natureza) batem com o lote GS4 que o Pietro fez a partir dos kits CC0 da Kenney. **Confirmar com o Pietro e com quem desenhou** antes de publicar a linha. CC0 não exige crédito; o crédito à Kenney é cortesia.
- **Textura de rachadura de licença incerta (pngtree):** em 16/11 ela **não é usada** por nenhum material, cena ou prefab nas branches `jam`, `jam-feel`, `jam-sistemas` e `main` (procurado pelo GUID `1b22d8567d928854eb84c97e41aee76a`: só aparece no próprio `.meta`). A rachadura das casas vem do `LightningSheet.png` do Sprite Shaders Ultimate, pelo `HouseMAT.mat`. Mesmo assim, o arquivo `Assets/Arts/Texture/pngtree-crack-texture-png-image_15379851-export.png` deve ser apagado do projeto antes da entrega (ou trocado pelo `Crack.png` próprio do `ART_REQUESTS.md`), para não entrar no jogo por engano. Decisão e commit são do MEGABRAIN. Conferência no `CHECKLIST.md`.
- **Plugins da Asset Store:** a licença padrão da Unity Asset Store deixa usar em jogo publicado; o crédito não é obrigatório. vHierarchy e vInspector são só de Editor e não entram no jogo.
- **Liberation Sans** (SIL OFL 1.1) é a fonte padrão do TextMesh Pro: só entra nos créditos se algum texto do jogo usar.
- **Uso de IA:** muitas jams pedem que o uso de IA seja declarado. Ler as regras da jam e manter ou tirar a linha.

## Configuração da página

| Campo do itch.io | Valor |
|---|---|
| Kind of project | HTML (o jogo roda na página; o Windows vai como download) |
| Classification | Games |
| Release status | Released |
| Pricing | No payments (ou "$0 or donate", a critério da equipe) |
| Uploads | `Please-Fix-the-City-WebGL.zip` marcado "This file will be played in the browser"; `Please-Fix-the-City-Windows.zip` marcado "Windows" |
| Embed options | Embed in page; manually set size **960 x 600** (o tamanho do canvas no Player Settings) |
| Frame options | Fullscreen button ligado; "Mobile friendly" desligado; "Automatically start on page load" desligado (o clique libera o áudio) |
| SharedArrayBuffer | Desligado (o jogo não usa threads) |
| Genre | Strategy (alternativa: Simulation) |
| Made with | Unity |
| Inputs | Mouse |
| Languages | Portuguese (Brazil) |
| Average session | A few minutes (uma partida dura 10 a 15 min) |
| Visibility | Draft até o envio; Public no envio |

**Tags** (o itch.io aceita até 10): `incremental`, `management`, `strategy`, `disaster`, `pixel-art`, `2d`, `city`, `singleplayer`, `short`, `brazil`

Cores sugeridas para o tema da página (as da UI do jogo, `STATUS_C.md`): fundo `#29262F`, painel `#594C63`, texto `#FFFFFF`, links no amarelo do aviso de desastre do jogo (confirmar o tom na build).

## Capa (cover image, 630 x 500)

Composição: a cidade em pixel art vista de cima, com uma casa rachada e poeira no meio, uma equipe de 3 trabalhadores andando na rua até ela, o marcador de chamado pulsando e a barra de caos no topo, meio cheia, em amarelo. Céu e chão nas cores da cena. Pixel art sem filtro (escala inteira, sem suavizar).

Texto da capa, em ModernDOS, contorno preto, como no jogo:

- Linha grande: **PLEASE FIX THE CITY**
- Linha menor: **Mantenha a cidade de pé.**

Alternativas de linha menor: "A cidade treme. Você despacha." · "Do braço à automação."

Como fazer sem desenhar do zero: captura do jogo em 1260 x 1000 (o dobro da capa), com zoom que mostre 3 a 4 quadras, no momento do primeiro terremoto; reduzir para 630 x 500 em vizinho mais próximo; o texto por cima na fonte do jogo.

## Capturas (screenshots)

Seis a oito capturas da build final, em 1280 x 720 ou 1920 x 1080, sem a interface do Editor. Em ordem:

1. **Menu sobre a cidade viva**: título em onda, os 3 passos e o botão Jogar.
2. **Primeiro terremoto**: faixa de aviso no topo, casas rachando em onda, poeira.
3. **Despacho**: chamado aberto, painel de base e tamanho da equipe.
4. **Equipe na rua**: trabalhadores andando, círculo de progresso numa casa, "+N" voando até o dinheiro em outra.
5. **Tempestade**: chuva, tela escurecida, clarão de relâmpago, uma casa pegando fogo com o temporizador.
6. **Aba de melhorias** cheia, com botões brilhando (dinheiro para comprar).
7. **Caos alto**: barra vermelha pulsando, vinheta vermelha, várias casas marcadas.
8. **Vitória**: a cidade comemorando com o Centro de Operações; ou a tela de fim com tempo, casas consertadas e dinheiro.

Dica de captura: na build Windows, `Win + Shift + S` ou a Barra de Jogo (`Win + Alt + PrtScn`); no Editor, `Game` em 1920 x 1080 e o botão de debug de 4x do `GameManager` para chegar mais rápido aos minutos finais (só no Editor).

## GIFs

Dois ou três GIFs curtos chamam mais atenção na página do que capturas paradas. Cada um com 4 a 8 s, 640 px de largura, 20 fps, até uns 5 MB:

1. **Terremoto em onda**: aviso, tremor de câmera, casas rachando a partir do epicentro, poeira (o melhor "gancho" do jogo).
2. **Ciclo completo de um conserto**: clique na casa, envio, trabalhadores andando, progresso, clique de coleta, "+N" voando e contador pulando.
3. **Antes e depois da automação**: 3 s despachando à mão, corte, 3 s com o nível 4 atendendo sozinho.

Gravação: Barra de Jogo do Windows (`Win + Alt + R`) ou OBS, em 1920 x 1080 e 60 fps. Conversão com ffmpeg (paleta própria, sem borrar a pixel art):

```
ffmpeg -ss 00:00:05 -t 6 -i gravacao.mp4 -vf "fps=20,scale=640:-1:flags=neighbor,split[a][b];[a]palettegen=max_colors=128[p];[b][p]paletteuse=dither=none" terremoto.gif
```

Se passar de 5 MB: diminuir `-t`, usar `fps=15` ou `scale=480:-1`.

## Texto do envio na página da jam

> Please Fix the City é um incremental de gestão de desastres: despache equipes, conserte a cidade e automatize a operação antes que o caos vença. Jogável no navegador e para Windows, em português. Tema: Citizen Tech.

## Conferir na build final

A página promete o que o GDD planejou. Antes de publicar, jogar a build final e tirar do texto o que não entrou:

- [ ] Tempestade com raios e incêndio isolado entraram? (ordem de corte do Dia 2 no `TASKS.md`: o incêndio isolado é o segundo a cair)
- [ ] Automação nível 4 (despacho automático) entrou? (terceiro a cair)
- [ ] Repasse da Prefeitura entrou?
- [ ] Vitória com "a cidade comemorando" existe? Se for só a tela de fim, ajustar a captura 8.
- [ ] Pausa com P e botão "II" entraram na `jam`? (hoje estão só na `jam-feel`)
- [ ] O botão de melhorias continua no canto inferior esquerdo?
- [ ] Base extra (S2) **não** é citada na página: é stretch. Se entrar, acrescentar uma linha.
- [ ] Nome da jam no fim da descrição longa.
