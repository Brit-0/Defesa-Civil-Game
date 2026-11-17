# Como testar o jogo (branch `jam`)

Para quem vai abrir o projeto pela primeira vez e jogar no Editor do Unity.

## 1. O que precisa

- Unity Hub com o **Unity 6000.6.7f1** (outra versão pode reimportar tudo ou dar erro). Para gerar o build de navegador, marque o módulo **Web Build Support** na instalação.
- Git.

## 2. Baixar a branch `jam`

Primeira vez:

```
git clone https://github.com/Brit-0/Defesa-Civil-Game.git
cd Defesa-Civil-Game
git checkout jam
```

Se já tem o projeto:

```
git fetch origin
git checkout jam
git pull
```

## 3. Abrir no Unity

1. No Unity Hub: **Add → Add project from disk** e escolha a pasta `Defesa-Civil-Game`. Abra com o 6000.6.7f1.
2. A primeira importação demora alguns minutos.
3. Abra a cena `Assets/Scenes/Gameplay.unity` (é a única do jogo).

## 4. Preparar os dados (uma vez, depois de cada `git pull`)

No menu de cima do Unity:

1. **Tools → Jam → Create Default Data**: cria o que faltar de balanceamento, desastres e upgrades. Não apaga ajustes.
2. **Tools → Jam → Create Feel Prefabs**: monta os efeitos e sons.

Se o Console mostrar aviso `[FireEnergyData]` ou da expansão, rode também **Tools → Jam → Create Fire And Energy Data** e **Tools → Jam → Expansão → Criar dados**. Sem o dia e a noite (a cidade fica sempre igual), rode **Tools → Jam → Create Day Night Data**.

**Não rode** `Tools → Jam → Reset Data To Defaults`: ele reescreve o balanceamento que já foi ajustado.

## 5. Jogar

1. Na aba **Game**, escolha a resolução **16:9** (ou 1280x720).
2. Aperte **Play** e clique em **JOGAR**.
3. Na primeira partida aparece o tutorial (13 passos). **PULAR TUTORIAL** encerra.
4. Controles: arrastar com o botão do meio ou direito move o mapa, a roda do mouse dá zoom, para mandar uma equipe, clique na casa com problema, depois na Base e em **Enviar**.

O que olhar:

- **Mini games:** escorar a casa no aviso de terremoto, coleta no ponto certo, e o **jato** no incêndio (arraste o mouse apertado por cima das chamas antes do tempo acabar).
- **Ritmo (RITMO_V3):** um sistema por vez. Terremoto de 2 casas no começo; o primeiro incêndio vem logo depois de 1:30 e a primeira tempestade logo depois de 2:50 (o primeiro desastre depois de liberar um tipo é sempre desse tipo); o terremoto forte só a cada 8 desastres, perto de 6:10. O intervalo entre desastres cai de 50 s para 22 s em 12 minutos e a barra de caos enche com 20.
- **Energia:** liga sozinha aos 5:15 de partida, com um segundo tutorial de 3 passos (apagão e botão **RELIGAR**). No último passo (**VAMOS**) os desastres voltam a cair; antes ficavam parados para sempre.
- **Quadro de força (#536, ENERGIA_DIA_NOITE.md):** no apagão, **RELIGAR** abre o quadro ao lado do botão, com 4 chaves numeradas fora de ordem. Ligue de 1 a 4: cada chave sobe com um clique cada vez mais agudo e o LED fica verde; no último, "LUZ!", um clarão e a energia volta. Chave fora da ordem: zumbido, o quadro treme, todas descem, o LED fica vermelho e as chaves ficam cinza por meio segundo; depois é só recomeçar do 1. No tutorial de energia, o balão muda para a explicação do quadro e a seta aponta a próxima chave. Clicar em **RELIGAR** de novo fecha o quadro.
- **Dia e noite (#536):** um dia inteiro a cada 7 minutos, só na luz: amanhecer rosado, dia com a luz de antes, entardecer laranja e noite azul, escura mas legível. Ao escurecer, os postes, as luzes da Base e dos Armazéns e as janelas de 3 em cada 4 casas acendem aos poucos (a Base e os Armazéns acendem sempre). No apagão à noite a cidade pisca e apaga; ao religar, as luzes voltam numa onda saindo da Base. Para ver uma fase sem esperar: no Play, selecione o objeto **DayNight** e use **Jump To** (Amanhecer, Manhã, Dia, Tarde, Entardecer, Noite, Madrugada) ou **Set Phase** (0 a 1) no Inspector. O ciclo não muda nada da jogabilidade e para junto com a pausa.
- **Tempestade (TEMPESTADE.md):** a casa quebrada por raio fica destelhada (pino azul). Sem equipe, a chuva sobe o dano em 1 a cada 25 s, até 3, com respingo no telhado e o selo do dano pulsando; mandar a equipe para a piora na hora.
- **Setas só nos chamados:** parados, a Base e os Armazéns ficam só com a placa do nome; a seta, o anel e o pino de borda da Base aparecem quando é hora de escolher a base de um chamado.
- **Rastro:** cada trabalhador andando deixa uma fita clara e fina atrás que some em menos de 1 s; pegadas e a trilha da equipe aparecem por cima do chão.
- **Base nova:** a equipe da base comprada na expansão sai pela rua e chega no chamado no tempo normal (antes parecia nunca chegar).
- **Expansão:** na aba de melhorias, **Nova base** e **Novo armazém**; o terreno comprado mostra uma placa no mapa e vira obra quando você clica nela.
- **Painel de melhorias:** abre por cima do botão de melhorias (canto de baixo à esquerda), com abas na base: **EQUIPE**, **AUTOMAÇÃO**, **EXPANSÃO** e **ENERGIA** (a de energia libera quando o gerador aparece; o pontinho dourado na aba diz que dá para comprar algo nela). Passe o mouse numa melhoria: ela cresce e aparece ao lado a janelinha com a descrição, o nível e quanto falta. Lista comprida rola com a roda do mouse.
- **Trabalhadores:** o número no topo é **livres/total** (ex.: 2/3 com uma equipe na rua); contratar e voluntários aumentam o total.
- **Tela de fim:** perca deixando o caos encher (ou ganhe comprando o objetivo). O HUD some por trás, a janela mostra 4 cartões (tempo, casas consertadas, desastres enfrentados, dinheiro ganho sem contar o inicial) e dois botões: **JOGAR DE NOVO** (recomeça direto) e **MENU** (volta à tela inicial).
- **Câmera travada:** na tela inicial e na tela de fim, arrastar e a roda do mouse não mexem o mapa; ao clicar em **JOGAR** a câmera volta a responder.
- **Sombras:** todas caem para baixo e para a direita (sol de cima à esquerda) e começam coladas no pé do desenho. Casas, Base e Armazéns: uma faixa embaixo do tamanho da parede da frente e uma lateral à direita (o telhado não conta como altura). Árvores, postes e placas: até a ponta, com a mancha no pé. Base e armazém novos da expansão ganham a mesma sombra quando a obra termina, e a árvore que o terreno tira não deixa sombra nem canteiro no chão.
- **Incêndio:** dura 40 s se ninguém for (era 25); a casa em chamas não mostra número nem o fundo escuro dele.
- **Escolher a base:** clicar na Base depois do chamado faz o clique e o estalo da aba da equipe abrindo.
- **Sem trabalhadores:** com todos na rua, o aviso nasce na Base, não na casa.
- **Coleta:** as moedinhas pulam da casa e voam até o contador de dinheiro, sem o "+N" na tela; o contador pula a cada moeda que chega.
- **Chamados:** a casa com problema mostra um pino com o ícone do tipo em 64 px (conserto, fogo, sem energia, recursos). Com a equipe trabalhando, o anel verde em volta do ícone enche até a casa ficar pronta; pronta, o pino vira o check verde novo.
- **Base e Armazém:** placa com o nome, seta dourada quicando em cima e anel de 4 quadros no chão, no pé do prédio. Depois de clicar numa casa (hora de escolher a base), a seta da Base quica mais alto e mais rápido; o Armazém faz o mesmo quando o material acaba.
- **HUD:** a barra de **CAOS** fica em pé, na direita da tela; a faixa de desastre fica no topo e a dica (ex.: "Escolha a base encarregada para o chamado") logo abaixo dela, sem nada por cima. Com o painel da equipe aberto (canto de baixo à direita), o balão do tutorial e a barra de dicas sobem e não o cobrem. Trabalhadores, materiais e moedas têm folga entre o número e a borda do rótulo.
- **Aba da equipe (#531):** abre sempre com **0** trabalhadores, e o jogador escolhe quantos vão no **+**. No topo da aba aparecem dois tempos: **Chegada** (o caminho até a casa, mais a saída escalonada da equipe) e **Conserto** (ou **Apagar** no incêndio, **Coleta** no armazém), pela mesma conta do jogo. Os dois mudam e pulam a cada **+** ou **−**. Com a aba vazia, ela mostra "Escolha a equipe no +", e o **Enviar** dá o som de erro e sacode o **+**. No tutorial, a seta aponta o **+** até ter gente na equipe e depois o **Enviar**.
- **"Escolha a base" (#531):** fica por cima de tudo do HUD (setas, placas, contadores) e some só com a pausa aberta.
- **Melhorias (#531):** toda linha cresce no hover e afunda no clique, inclusive sem dinheiro; nesse caso o clique treme em vermelho.
- **Janela das estruturas (#531):** passe o mouse numa casa, na Base ou num Armazém. A janela de informação usa a janela pixel do HUD (moldura do tamanho do pixel da cidade), o nome em dourado e o texto em creme, com margem dentro da moldura.
- **Aro do tutorial (#531):** é o anel fino redondo, nunca esticado nem quadriculado. O lado sempre cai num múltiplo de 32 (de 96 a 256) e o anel pisca no brilho, sem mudar de tamanho. Num painel inteiro (a aba da equipe, a aba de melhorias), fica só a seta.
- **Margens (#531):** a **META**, a janelinha das melhorias, o balão e o cartão do tutorial e a barra de dicas deixam folga entre o texto e a moldura.
- **Pausa no Esc (#535):** **Esc** (ou **P**) pausa e o mesmo Esc continua. A tela **PAUSADO** fica por cima de tudo (até do balão do tutorial) e o mapa escurece; trabalhadores, fogo, contadores, aros e sons param; a música só abaixa e os cliques da tela de pausa tocam. Com um chamado aberto, o primeiro Esc fecha o chamado e o segundo pausa. No passo lento do tutorial, a volta da pausa continua lenta. Na tela de fim o Esc não faz nada.
- **Automação 1 (#535):** com a Central de rádio comprada, clicar num chamado abre direto a aba da equipe, sem o "Escolha a base" piscando no topo (sem a automação, ele aparece e fica até você escolher a base).
- **Incêndio (#535):** as chamas para arrastar, a barra de tempo e a dica "ARRASTE NAS CHAMAS!" (numa faixa escura) ficam no mapa, em volta da casa, do tamanho do pixel da cidade. Arraste a câmera para longe: elas ficam na casa e saem da tela (antes ficavam presas na borda).
- **Aba da equipe (#535):** o bloco de tempos tem mais folga dentro da moldura. No Armazém aparece uma terceira linha, **Material +N**: o material que a equipe traz (trabalhadores × material por trabalhador), mudando a cada **+** ou **−**.
- **META (#535):** um pouco maior, com a mesma folga em cima do título e embaixo do marco; a barra enche em dourado até o preço do Centro de Operações.
- **Aro do terremoto (#535):** o aro laranja nas casas para escorar é o mesmo anel fino do tutorial (Simple, lado múltiplo de 32), piscando no brilho, sem quadricular.

Para ver os tutoriais de novo: **Tools → Jam → Reset Tutorial** e **Tools → Jam → Reset Energy Tutorial**, com o Play parado.

## 6. Achou um problema?

Tire um print da aba Game, copie a mensagem vermelha do **Console** (se houver) e diga o que fez antes. Mande para o Pietro.
