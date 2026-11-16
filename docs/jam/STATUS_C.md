# Status do Agente C (Feel & UI)

Só o Agente C edita este arquivo. O MEGABRAIN lê a cada integração, atende os pedidos na cena e passa o status para o `TASKS.md`.

**Sessão de 16/11 feita sem o Unity** (o 6000.6.7f1 não estava instalado no PC; a instalação pelo Hub falhou com "The install destination was invalid or missing"). Nada aqui rodou em Play Mode. O que foi conferido e o que falta está em "Pendente até o Unity abrir".

## Tarefas

Formato: `- [x] T10 — o que entrou, hash do commit`

- [x] T10 — `Tweener`: um tween de escala por instância (mata só o próprio), sem IDs globais de texto; tempos em campos do Inspector com os valores antigos; roda com o jogo pausado. Métodos públicos iguais (`HoverEnter`, `HoverExit`, `Hold`, `Release`). `1e88c98`. Falta o teste no Play.
- [x] T13 — `AudioManager` (pool de 12 AudioSources, `PlaySfx(clip, volume, variação de pitch)`, música em 2 camadas com `SetTension(0..1)`, loops de ambiente com `SetLoop`), 18 sons em `Assets/Audio/` com os nomes finais e o menu `Tools/Jam/Create Feel Prefabs`, que monta o `AudioManager.prefab`. `e4d6f2f` (código) e `510db65` (sons). Falta rodar o menu e o teste no Play.

- [x] T11 — `HudJuice` (dinheiro, material e trabalhadores contam até o valor novo, o ícone pula e o número pisca verde ou vermelho), `MoneyPopups` ("+N" nasce na casa em `CallResolved` e voa até o contador; "+N Repasse" em `PassiveIncomePaid`), `GameAudio` (liga 15 eventos aos sons: clique, envio, martelo repetindo enquanto há equipe trabalhando, pronto, moeda, compra, erro, sirene, tremor, trovão, chuva e fogo em loop, vitória, derrota e a tensão da música pelo caos; todo botão de UI ganha som de clique sem componente), mais `FeelUI` (monta e acha UI por código) e `ButtonJuice` (hover de botão). `06ce469`. **Escrito contra o T03 e o T04 do B (`c4037be`, `8d1a821`), que ainda não estão na `jam`: a `jam-feel` sozinha só compila depois do merge deles.** Falta o teste no Play.
- [x] T12 — `HouseFeedback` (um por casa: ao quebrar, sacudida do `Visual`, `_TextureLayer1Fade` de 0 a 1 e poeira; ao consertar, pulo, brilho de faíscas e a rachadura some; ao pegar fogo, sacudida curta; liga `house.externalFeedback` no `Awake`; marcador de chamado e de "pronto" nascem com pop e ficam pulsando, pelo `MarkerPulse`; marcador avermelhado em incêndio), `CameraShaker` (tremor em `DisasterStarted` de terremoto, tranco a cada casa que quebra, que é a onda, e em `LightningStruck`; soma e desfaz um deslocamento no `LateUpdate`, sem tocar no `CameraController`), `FeelFx` (estouro de quadradinhos, provisório até `Dust_5x32.png` e `Sparkle_4x16.png`). `bd8a441`. Falta o teste no Play.
- [x] T17 — `ChaosBar` (topo do HUD; enche com `ChaosChanged`, vai de verde a amarelo a vermelho e pulsa acima de 60%, mais rápido perto de encher; some no menu) e `EndScreen` (em `Won` ou `Lost` escurece a tela, mostra título, tempo, casas consertadas e dinheiro contando de zero, lidos do `GameManager`, e o botão Reiniciar, que chama `GameManager.Instance.Restart()`; anima fora do `timeScale`). Os dois montam a própria UI por código. Hash na próxima atualização. Falta o teste no Play.

## Pedidos ao MEGABRAIN

O que criar e ligar na cena e nos prefabs. Formato: `- [ ] objeto ou prefab, componente, cada campo e para onde aponta — por quê`

- [ ] **Integrar a `jam-sistemas` na `jam` antes ou junto da `jam-feel`** — do T11 em diante o código do C usa `GameEvents`, `GameManager`, `DisasterData` e `UpgradeData`, que só existem na `jam-sistemas`. Conferi a compilação com as duas branches juntas, fora do Unity.
- [ ] `Assets/Systems/Feel/Feel.prefab` (criado pelo menu) em `Scripts/` — um objeto só com os componentes de juice que escutam eventos. Nenhum campo para ligar: `HudJuice` acha `UICanvas/Money` e `UICanvas/Resources/{BuildMaterial,Workers}` pelo nome (o primeiro `TMP_Text` e o filho `Icon` de cada um); `MoneyPopups` cria os textos numa camada `FeelPopups` dentro do `UICanvas`. Se o HUD mudar de nome, os seis campos de `HudJuice` aceitam arrastar o rótulo e o ícone.
- [ ] `AudioManager.prefab` ganhou o componente `GameAudio` (rodar o menu de novo completa o prefab sem apagar ajustes) — nenhum campo para ligar.
- [ ] `House.prefab` (raiz, ao lado do componente `House`): adicionar `HouseFeedback` — nenhum campo para ligar: ele acha `Visual` (a `Image` com o `HouseMAT`), `Canvas/Call` e `Canvas/Complete` pelo nome e põe o `MarkerPulse` nos dois marcadores em runtime. As variantes `Guard` e `Warehouse` herdam e não fazem nada de errado (nunca mudam de estado). Sem ele a rachadura continua trocando a seco, pela `House`.
- [ ] (opcional) `Assets/Shaders & Materials/HouseMAT.mat`: ligar **Strong Tint** com Fade 0 e cor branca — com a variante do shader no build, o conserto ganha um clarão branco (`_StrongTintFade`). O shader é `shader_feature`: ligar a keyword só por código não funciona no build. Sem isso o brilho do conserto são as faíscas.
- [ ] `Feel.prefab` ganhou o `CameraShaker` (rodar o menu de novo) — nenhum campo. Acha a `Camera.main`.
- [ ] `Assets/Systems/UI/ChaosBar.prefab` e `Assets/Systems/UI/EndScreen.prefab` (criados pelo menu) como filhos diretos do `UICanvas`, o `EndScreen` por último — nenhum campo para ligar. Cada um se estica no Canvas e cria os próprios filhos no `Awake`; a posição e o tamanho ficam nos campos do componente (LAYOUT), não no RectTransform. A barra fica no topo, no centro, 560x44 a 26 px da borda: se bater em algo do HUD, mudar `topMargin` ou `size`. Soltos fora de um Canvas também funcionam (criam uma camada no `UICanvas`).

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

Conferido sem o Unity: os scripts do C compilam junto com os de `Assets/Systems` e `Assets/SO` no compilador do Unity 6000.4.6f1 (Roslyn, C# 9, mesmas DLLs de UnityEngine, UI, TextMeshPro, Input System, URP, DOTween e Odin), em dois modos: jogador (sem `UnityEditor`) e Editor. Isso pega erro de tipo e de nome; não pega nada de importação, de cena nem de comportamento.

- [ ] Abrir o projeto no 6000.6.7f1 e confirmar console sem erros (a API pode ter mudado entre o 6000.4 e o 6000.6).
- [ ] Rodar `Tools/Jam/Create Feel Prefabs` e commitar os prefabs.
- [ ] Testar cada tarefa no Play pelo critério de pronto dela e salvar as capturas em `docs/jam/reference/feel/` (regra 12): nenhuma captura foi feita.
- [ ] T10: passar o mouse rápido por várias casas e conferir que nenhuma outra pula nem trava em escala errada.
- [ ] T13: botão `DebugPlayAllSfx` do `AudioManager` (Odin, em Play) toca os 14 efeitos em fila; `DebugSetTension` com 0 e 1 cruza as músicas. Ouvir os sons: foram escolhidos sem escuta.
