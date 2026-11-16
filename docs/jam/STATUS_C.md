# Status do Agente C (Feel & UI)

Só o Agente C edita este arquivo. O MEGABRAIN lê a cada integração, atende os pedidos na cena e passa o status para o `TASKS.md`.

**Sessão de 16/11 feita sem abrir o Unity.** O 6000.6.7f1 foi instalado no meio da sessão, mas o Editor não foi aberto nesta pasta (pedido do Pietro: o disco tem pouco espaço para mais uma `Library`). Nada aqui rodou em Play Mode e não existe nenhum `.prefab` do C ainda. O que foi conferido e o que falta está em "Pendente até o Unity abrir".

**As imagens `docs/jam/reference/feel/PREVIA_*.png` não são capturas do jogo:** são maquetes do layout (960x540), desenhadas fora do Unity com os mesmos números do código (posição, tamanho, cor e fonte), para o Pietro ver onde cada coisa fica. As capturas de verdade da regra 12 continuam pendentes.

## Tarefas

Formato: `- [x] T10 — o que entrou, hash do commit`

- [x] T10 — `Tweener`: um tween de escala por instância (mata só o próprio), sem IDs globais de texto; tempos em campos do Inspector com os valores antigos; roda com o jogo pausado. Métodos públicos iguais (`HoverEnter`, `HoverExit`, `Hold`, `Release`). `1e88c98`. Falta o teste no Play.
- [x] T13 — `AudioManager` (pool de 12 AudioSources, `PlaySfx(clip, volume, variação de pitch)`, música em 2 camadas com `SetTension(0..1)`, loops de ambiente com `SetLoop`), 18 sons em `Assets/Audio/` com os nomes finais e o menu `Tools/Jam/Create Feel Prefabs`, que monta o `AudioManager.prefab`. `e4d6f2f` (código) e `510db65` (sons). Falta rodar o menu e o teste no Play.

- [x] T11 — `HudJuice` (dinheiro, material e trabalhadores contam até o valor novo, o ícone pula e o número pisca verde ou vermelho), `MoneyPopups` ("+N" nasce na casa em `CallResolved` e voa até o contador; "+N Repasse" em `PassiveIncomePaid`), `GameAudio` (liga 15 eventos aos sons: clique, envio, martelo repetindo enquanto há equipe trabalhando, pronto, moeda, compra, erro, sirene, tremor, trovão, chuva e fogo em loop, vitória, derrota e a tensão da música pelo caos; todo botão de UI ganha som de clique sem componente), mais `FeelUI` (monta e acha UI por código) e `ButtonJuice` (hover de botão). `06ce469`. **Escrito contra o T03 e o T04 do B (`c4037be`, `8d1a821`), que ainda não estão na `jam`: a `jam-feel` sozinha só compila depois do merge deles.** Falta o teste no Play.
- [x] T12 — `HouseFeedback` (um por casa: ao quebrar, sacudida do `Visual`, `_TextureLayer1Fade` de 0 a 1 e poeira; ao consertar, pulo, brilho de faíscas e a rachadura some; ao pegar fogo, sacudida curta; liga `house.externalFeedback` no `Awake`; marcador de chamado e de "pronto" nascem com pop e ficam pulsando, pelo `MarkerPulse`; marcador avermelhado em incêndio), `CameraShaker` (tremor em `DisasterStarted` de terremoto, tranco a cada casa que quebra, que é a onda, e em `LightningStruck`; soma e desfaz um deslocamento no `LateUpdate`, sem tocar no `CameraController`), `FeelFx` (estouro de quadradinhos, provisório até `Dust_5x32.png` e `Sparkle_4x16.png`). `bd8a441`. Falta o teste no Play.
- [x] T17 — `ChaosBar` (topo do HUD; enche com `ChaosChanged`, vai de verde a amarelo a vermelho e pulsa acima de 60%, mais rápido perto de encher; some no menu) e `EndScreen` (em `Won` ou `Lost` escurece a tela, mostra título, tempo, casas consertadas e dinheiro contando de zero, lidos do `GameManager`, e o botão Reiniciar, que chama `GameManager.Instance.Restart()`; anima fora do `timeScale`). Os dois montam a própria UI por código. `9c4494a`. Falta o teste no Play.
- [x] T27 — `WorkerFeel` (um só na cena, no `Feel.prefab`: anima cada trabalhador que nasce em `WorkerSpawned`, sem componente no `Worker.prefab`; pop ao nascer, balanço de rotação, achatada e pulinho enquanto anda, vira para o lado em que anda, e ao chegar incha, some e solta um sopro de poeira dentro dos 0,1 s do `destroyDelay` do GPS). Não mexe na posição, que é do GPS: usa escala, rotação e pivô. `0102cb1`. Falta o teste no Play.
- [x] T28 — `DisasterBanner` (faixa no topo do HUD, abaixo da barra de caos: em `DisasterWarning` desce com o nome do desastre em onda, pelo Text Animator, a moldura piscando na cor do tipo e a contagem em segundos de jogo, com um tique e um pulo a cada segundo; em `DisasterStarted` troca para "NOME!" tremendo, dá um clarão e sai sozinha). A sirene já toca pelo `GameAudio`, no mesmo evento. Botões Odin `DebugWarning` e `DebugStarted` para testar sem desastre. `83d980e`. Falta o teste no Play.
- [x] T33 — `UpgradeButtonJuice` (brilho pulsando no botão enquanto `CanBuy` é verdadeiro, pulinho quando fica comprável, pulo e clarão branco na compra, tremida e clarão vermelho na compra negada) e `ActionFeedback` (em `ActionDenied`, texto curto no lugar do clique: "Sem trabalhadores!", "Sem material!", "Sem dinheiro!", "Nível máximo!"; e põe o `UpgradeButtonJuice` em todo `UpgradeButton` da cena, um frame depois do `Start`). Junto: o contador do HUD do recurso que faltou pula e pisca vermelho (`HudJuice`) e a casa do chamado negado treme (`HouseFeedback`). O som de erro já era do `GameAudio`. `c0bae7b`. Falta o teste no Play.
- [x] T29 — `MainMenu` (por cima da cidade, na mesma cena: título em onda pelo Text Animator, os 3 passos de como jogar e o botão Jogar, que chama `StartGame()`; aparece enquanto o estado é `Menu`), `PauseMenu` (botão "II" no canto inferior esquerdo e tecla P chamam `Pause()` e `Resume()`; a tela tem Continuar e Reiniciar e anima com o jogo parado; o Esc ficou de fora porque já cancela o chamado), `TutorialTips` (faixa embaixo com as dicas da primeira volta: casa com problema, chamado aberto, conserto pronto, pagamento recebido; uma vez por sessão) e o `FeedbacksManager` refeito (o texto de instrução agora escuta `CallOpened` e some em `TeamDispatched` ou no fim da partida; `ShowText` e `HideText` continuam públicos e não quebram sem a referência). As telas finais são o `EndScreen` do T17. `14f9e54`. Falta o teste no Play.
- [x] T26 — `WeatherFx` (chuva em overlay enquanto o desastre `Storm` dura: tinta azulada e 80 riscos caindo numa camada atrás do HUD; em `LightningStruck`, clarão em dois tempos, raio em ziguezague do topo da tela até a casa e faíscas), `HouseFire` (criado pelo `HouseFeedback` quando a casa entra em `OnFire`: chamas e fumaça subindo e a barra do temporizador embaixo da casa, lida de `FireTimeLeft01`, que amarela, avermelha e pisca no fim) e `ChaosVignette` (vinheta vermelha e cor lavada no `Global Volume` a partir de 40% de caos, pulsando acima de 60%). Tudo desenhado com `Image` e código, sem arte: `Rain_Tile.png` e `Fire_6x32.png` têm campo pronto (`rainTile` no `WeatherFx`, `fireFrames` no `HouseFeedback`). `b5fb8a1`. Falta o teste no Play.
- [x] T30 — música em 2 camadas pelo caos (já vinha do T13 e do T11: `Music_Calm` e `Music_Tense` tocam juntas e o `GameAudio` cruza as duas entre 35% e 70% de caos) e o fechamento do áudio: `AudioManager.SetMusicLevel` abaixa a música no menu (70%), na pausa (35%) e no fim da partida (30%, para a vinheta de vitória ou derrota aparecer); sirene baixa de 9 em 9 s enquanto o caos passa de 60% (GDD, seção 6); som de partida ao clicar em Jogar; chuva e fogo em loop param no fim da partida; o `GameAudio` do `AudioManager` duplicado (o da cena recarregada no Reiniciar) não assina eventos, para nada tocar em dobro. Os sons são os do pacote Gamemaster (tabela abaixo). `77ceec0`. Falta ouvir no Play.
- [x] Legibilidade (fora da lista, vista nas maquetes): contorno preto no rótulo da barra de caos e no texto de ação negada (`FeelUI.Outline`, fonte 38) e faixa escura atrás dos 3 passos do menu inicial, que ficavam direto por cima da cidade. `7492ebe`. Falta conferir no Play.

## Pedidos ao MEGABRAIN

O que criar e ligar na cena e nos prefabs. Formato: `- [ ] objeto ou prefab, componente, cada campo e para onde aponta — por quê`

- [ ] **Integrar a `jam-sistemas` na `jam` antes ou junto da `jam-feel`** — do T11 em diante o código do C usa `GameEvents`, `GameManager`, `DisasterData` e `UpgradeData`, que só existem na `jam-sistemas`. Conferi a compilação com as duas branches juntas, fora do Unity.
- [ ] `Assets/Systems/Feel/Feel.prefab` (criado pelo menu) em `Scripts/` — um objeto só com os componentes de juice que escutam eventos. Nenhum campo para ligar: `HudJuice` acha `UICanvas/Money` e `UICanvas/Resources/{BuildMaterial,Workers}` pelo nome (o primeiro `TMP_Text` e o filho `Icon` de cada um); `MoneyPopups` cria os textos numa camada `FeelPopups` dentro do `UICanvas`. Se o HUD mudar de nome, os seis campos de `HudJuice` aceitam arrastar o rótulo e o ícone.
- [ ] `AudioManager.prefab` ganhou o componente `GameAudio` (rodar o menu de novo completa o prefab sem apagar ajustes) — nenhum campo para ligar.
- [ ] `House.prefab` (raiz, ao lado do componente `House`): adicionar `HouseFeedback` — nenhum campo para ligar: ele acha `Visual` (a `Image` com o `HouseMAT`), `Canvas/Call` e `Canvas/Complete` pelo nome e põe o `MarkerPulse` nos dois marcadores em runtime. As variantes `Guard` e `Warehouse` herdam e não fazem nada de errado (nunca mudam de estado). Sem ele a rachadura continua trocando a seco, pela `House`.
- [ ] (opcional) `Assets/Shaders & Materials/HouseMAT.mat`: ligar **Strong Tint** com Fade 0 e cor branca — com a variante do shader no build, o conserto ganha um clarão branco (`_StrongTintFade`). O shader é `shader_feature`: ligar a keyword só por código não funciona no build. Sem isso o brilho do conserto são as faíscas.
- [ ] `Feel.prefab` ganhou o `CameraShaker` (rodar o menu de novo) — nenhum campo. Acha a `Camera.main`.
- [ ] `Assets/Systems/UI/ChaosBar.prefab` e `Assets/Systems/UI/EndScreen.prefab` (criados pelo menu) como filhos diretos do `UICanvas`, o `EndScreen` por último — nenhum campo para ligar. Cada um se estica no Canvas e cria os próprios filhos no `Awake`; a posição e o tamanho ficam nos campos do componente (LAYOUT), não no RectTransform. A barra fica no topo, no centro, 560x44 a 26 px da borda: se bater em algo do HUD, mudar `topMargin` ou `size`. Soltos fora de um Canvas também funcionam (criam uma camada no `UICanvas`).
- [ ] `Feel.prefab` ganhou o `WorkerFeel` (rodar o menu de novo) — nenhum campo para ligar e nada no `Worker.prefab`. Se o sprite do trabalhador olhar para a esquerda, desmarcar `spriteFacesRight`.
- [ ] `Assets/Systems/UI/DisasterBanner.prefab` (criado pelo menu) como filho direto do `UICanvas` — nenhum campo para ligar. Usa `DisasterData.displayName`, `icon` (se houver) e `type` (cor da moldura). O texto leva o componente `TextAnimator_TMP` posto por código; as tags `<wave>` e `<shake>` vêm do banco de efeitos global do projeto (`Resources/TextAnimatorSettings`). Se aparecerem escritas na tela, o banco global não carregou: avisar.
- [ ] `Feel.prefab` ganhou o `ActionFeedback` (rodar o menu de novo) — nenhum campo. Ele já põe o `UpgradeButtonJuice` nos botões de upgrade em runtime; adicionar `UpgradeButtonJuice` à raiz do `UpgradeButton.prefab` é opcional (só para ajustar cores e tempos no Inspector). O brilho é uma `Image` filha chamada `Glow`, criada por código como primeiro filho, com o mesmo sprite do fundo do botão.
- [ ] `Assets/Systems/UI/MainMenu.prefab`, `Assets/Systems/UI/PauseMenu.prefab` e `Assets/Systems/Feedbacks/TutorialTips.prefab` (criados pelo menu) como filhos diretos do `UICanvas` — nenhum campo para ligar. Cada tela se traz para a frente quando aparece.
- [ ] `GameManager` na cena: desligar `autoStart` quando o `MainMenu` entrar — com ele ligado a partida começa sozinha e o menu nunca aparece (é o comportamento seguro enquanto o menu não está na cena).
- [ ] Conferir no Play se o botão de pausa (canto inferior esquerdo, 72x72 a 60 px das bordas) e a faixa de dicas (embaixo, no centro, a 110 px da borda) não cobrem nada do HUD: os campos `buttonAnchor`, `buttonPosition` e `bottomMargin` mudam o lugar.
- [ ] (para o B, quando puder) `GuardManager.OpenCall` não precisa mais chamar `FeedbacksManager.ShowText("Escolha a base...")`: o texto já aparece pelo `CallOpened`. Os `HideText()` do `SelectBase` e do `CloseCall` continuam necessários (não há evento para base escolhida nem para chamado cancelado). Deixar como está também funciona.
- [ ] `Feel.prefab` ganhou `WeatherFx` e `ChaosVignette` (rodar o menu de novo) — nenhum campo. O `ChaosVignette` acha o `Volume` global da cena e acrescenta Vignette e Color Adjustments à cópia do perfil em runtime.
- [ ] `Assets/Scenes/Gameplay/Global Volume Profile.asset`: acrescentar os overrides **Vignette** (Intensity 0) e **Color Adjustments** (Saturation 0) — o perfil hoje só tem Bloom; se o URP estiver com "Strip Unused Post Processing Variants" ligado, a vinheta posta só por código some no build (no Editor funciona de qualquer jeito). A `Main Camera` já está com Post Processing ligado.
- [ ] Quando a arte chegar: `Rain_Tile.png` (Wrap Mode Repeat) no campo `rainTile` do `WeatherFx`; os 6 sprites de `Fire_6x32.png` no campo `fireFrames` do `HouseFeedback` do `House.prefab`. Sem eles valem as gotas e as chamas de quadradinhos.

- [ ] Rodar `Tools/Jam/Create Feel Prefabs` a cada entrega do C e commitar os `.prefab` criados com os `.meta` — os prefabs do C não foram escritos à mão; o menu cria o que falta e completa o que já existe (componentes novos), sem apagar ajustes feitos no Inspector.
- [ ] `Assets/Systems/Audio/AudioManager.prefab` em `Scripts/` (raiz da cena também serve) — nenhum campo para ligar: os clipes entram por nome a partir de `Assets/Audio/`. Ele se solta do pai e fica vivo entre recargas da cena (`persistAcrossReload`), para a música não cortar no Reiniciar. Sem o prefab: GameObject vazio + componente `AudioManager` dá no mesmo (o `Reset` liga os clipes; ou menu de contexto "Preencher clipes de Assets/Audio").
- [ ] Depois de importar os sons, conferir em `Assets/Audio/` que `Music_Calm` e `Music_Tense` ficaram com Load Type "Compressed In Memory" (ou Streaming no Windows) e os `Sfx_*` com "Decompress On Load" — os `.meta` foram escritos só com o GUID; o Unity completa com o padrão ao importar.

## Sons provisórios

Formato: `- arquivo — origem (link) — licença`

**Mudança do Pietro em 16/11:** em vez de sons CC0, usar o pacote pago dele, **Gamemaster Audio - Pro Sound Collection v1.3 (16bit 48k)**, em `C:\Users\PIETRO\Projetos\sons-gamemaster\`. Licença: a do pacote comprado (uso em jogos; os arquivos soltos não podem ser redistribuídos). O repositório é privado; **se um dia ficar público, tirar `Assets/Audio/` do histórico ou trocar os sons**. Todos foram escolhidos pelo nome e por medida (duração, pico, energia entre 300 Hz e 5 kHz), não por escuta.

Efeitos convertidos para mono, 44,1 kHz, 16 bit; música e ambiente em Ogg Vorbis estéreo. Pasta inteira: 4,0 MB.

| Arquivo | Origem no pacote | Ajuste |
|---|---|---|
| `Sfx_Click.wav` | `User_Interface_Menu/ui_button_simple_click_02.wav` | — |
| `Sfx_Hover.wav` | `User_Interface_Menu/ui_button_simple_click_06.wav` | — |
| `Sfx_Dispatch.wav` | `User_Interface_Menu/ui_menu_button_confirm_02.wav` | — |
| `Sfx_Hammer.wav` | `Metal/metal_hit_small_04.wav` | — |
| `Sfx_Repaired.wav` | `Collectibles_Items_Powerup/chime_bell_positive_ring_01.wav` | — |
| `Sfx_Coin.wav` | `Collectibles_Items_Powerup/collect_coin_05.wav` | — |
| `Sfx_Buy.wav` | `Collectibles_Items_Powerup/cash_register_open_coins_cha_ching_01.wav` | — |
| `Sfx_Error.wav` | `User_Interface_Menu/ui_menu_button_error_06.wav` | — |
| `Sfx_Siren.wav` | `Alarms_Beeps_Siren/alarm_siren_loop_01.wav` | cortado em 3,0 s com fade no fim |
| `Sfx_Earthquake.wav` | `Impacts_Smashable/rock_earthquake_impact_01.wav` | cortado em 3,6 s com fade no fim |
| `Sfx_Thunder.wav` | `Animals_Nature_Ambiences/thunder_lightning_strike_rumble_01.wav` | cortado em 4,5 s com fade no fim |
| `Sfx_Extinguish.wav` | `Explosion_Fire_Gas/fuse_stop_burn_out_bomb_dynamite_01.wav` | — |
| `Sfx_Victory.wav` | `Collectibles_Items_Powerup/jingle_chime_06_positive.wav` | — |
| `Sfx_Defeat.wav` | `Collectibles_Items_Powerup/game_over_dark_bell_chime_01.wav` | — |
| `Amb_Rain.ogg` | `Animals_Nature_Ambiences/rain_medium_loop_01.wav` | loop de 6 s |
| `Amb_Fire.ogg` | `Explosion_Fire_Gas/fire_burning_flames_crackle_loop_02.wav` | loop de 8 s |
| `Music_Calm.ogg` | `zzzBonus_Music_16bit44kOnlyzzz/music_calm_green_lake_serenade.wav` | 92 s, nivelada a -18 LUFS |
| `Music_Tense.ogg` | `zzzBonus_Music_16bit44kOnlyzzz/music_cinematic_darkness_falls.wav` | 48 s, nivelada a -18 LUFS |

As duas músicas são faixas diferentes (o pacote não tem a mesma base em versão calma e tensa), então não têm o mesmo andamento: o `SetTension` cruza o volume das duas. Para a música final de ART_REQUESTS.md basta trocar os dois arquivos.

## Dúvidas para o MEGABRAIN

Formato: `- [ ] pergunta — o que eu assumi para seguir trabalhando`

- [ ] Os prefabs do C podem nascer de um menu de Editor, em vez de virem prontos no commit? — assumi que sim: sem o Unity não dá para criar `.prefab` com segurança, e é o mesmo padrão do `Tools/Jam/Create Default Data` do B. Os componentes também funcionam sem prefab (acham o que precisam sozinhos).

## Pendente até o Unity abrir

Conferido sem o Unity: os scripts do C compilam junto com os de `Assets/Systems` e `Assets/SO` da `jam-sistemas` (até `213baae`), em dois modos: jogador (sem `UnityEditor`) e Editor, com 0 erros e 0 avisos de API obsoleta. Do T30 em diante a conferência usa as DLLs do motor do **6000.6.7f1** (instalado no meio da sessão) e as de UI, TextMeshPro, Input System, URP e Text Animator já compiladas na `Library` do projeto principal, mais DOTween e Odin; o compilador é o Roslyn do 6000.4.6f1, C# 9. Isso pega erro de tipo, de nome e de assinatura; não pega nada de importação, de cena nem de comportamento.

- [ ] Abrir o projeto no 6000.6.7f1, já com a `jam-sistemas` integrada, e confirmar console sem erros (a conferência de fora não pega importação de asset, `.meta` nem serialização).
- [ ] Rodar `Tools/Jam/Create Feel Prefabs` e commitar os prefabs.
- [ ] Testar cada tarefa no Play pelo critério de pronto dela e salvar as capturas em `docs/jam/reference/feel/` (regra 12): nenhuma captura foi feita, só as 5 maquetes `PREVIA_*.png`.
- [ ] T10: passar o mouse rápido por várias casas e conferir que nenhuma outra pula nem trava em escala errada.
- [ ] T13: botão `DebugPlayAllSfx` do `AudioManager` (Odin, em Play) toca os 14 efeitos em fila; `DebugSetTension` com 0 e 1 cruza as músicas. Ouvir os sons: foram escolhidos sem escuta.
