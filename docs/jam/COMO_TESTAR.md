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

Para ver os tutoriais de novo: **Tools → Jam → Reset Tutorial** e **Tools → Jam → Reset Energy Tutorial**, com o Play parado.

## 6. Achou um problema?

Tire um print da aba Game, copie a mensagem vermelha do **Console** (se houver) e diga o que fez antes. Mande para o Pietro.
