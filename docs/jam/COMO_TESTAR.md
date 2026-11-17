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

Se o Console mostrar aviso `[FireEnergyData]` ou da expansão, rode também **Tools → Jam → Create Fire And Energy Data** e **Tools → Jam → Expansão → Criar dados**.

**Não rode** `Tools → Jam → Reset Data To Defaults`: ele reescreve o balanceamento que já foi ajustado.

## 5. Jogar

1. Na aba **Game**, escolha a resolução **16:9** (ou 1280x720).
2. Aperte **Play** e clique em **JOGAR**.
3. Na primeira partida aparece o tutorial (13 passos). **PULAR TUTORIAL** encerra.
4. Controles: arrastar com o botão do meio ou direito move o mapa, a roda do mouse dá zoom, para mandar uma equipe, clique na casa com problema, depois na Base e em **Enviar**.

O que olhar:

- **Mini games:** escorar a casa no aviso de terremoto, coleta no ponto certo, e o **jato** no incêndio (arraste o mouse apertado por cima das chamas antes do tempo acabar).
- **Energia:** liga sozinha aos 2:30 de partida, com um segundo tutorial de 3 passos (apagão e botão **RELIGAR**).
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

Para ver os tutoriais de novo: **Tools → Jam → Reset Tutorial** e **Tools → Jam → Reset Energy Tutorial**, com o Play parado.

## 6. Achou um problema?

Tire um print da aba Game, copie a mensagem vermelha do **Console** (se houver) e diga o que fez antes. Mande para o Pietro.
