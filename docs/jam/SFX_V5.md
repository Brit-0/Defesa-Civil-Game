# Som v5D: lista dos sons e como ajustar

Branch `jam-v5d`. 47 sons no `SoundBank` (76 arquivos com as variações), mixer com grupos, música em 4 camadas que
entram com o caos, ambiente de cidade de dia e de noite. Créditos e licenças: `docs/jam/CREDITOS_AUDIO.md`.

## Como tocar (para as outras frentes: enchente, meteoro, guaritas)

```csharp
AudioManager.Play("thunder_near", posicaoNoMundo);          // variação sorteada, tom e volume variam, lado pela tela
AudioManager.Play("ui_buy", null, .8f);                     // sem posição: no meio
AudioManager.PlayDelayed("quake_hit", .7f, posicao);        // atraso pelo GameClock (para junto com a pausa)
AudioManager.StartLoop("fire_jet_loop", casa, .75f, .15f);  // loop com dono; chamar de novo muda o volume
AudioManager.StopLoop("fire_jet_loop", casa, .4f);          // StopAllLoops(id) para todos os donos
```

Sons da enchente, do meteoro e da guarita (#524) ficam nas pastas deles e entram no banco pela lista `LegacyEntries` do
`SoundBankBuilder` (clipe -> id, com padrões na `Table`). Som novo: receita em `Tools/Audio/sons_v5.py` (`@receita('Desastres', alvo_LUFS)`, arquivo `<id>_NN.wav`), rodar o
script, depois **Tools > Audio > Montar SoundBank v5** no Unity. O montador acha o id pelo nome do arquivo, aplica a
importação e cria a entrada com os padrões da tabela `SoundBankBuilder.Table` (sem linha na tabela: Gameplay, volume
0,9). Quem ainda toca um clipe antigo por `AudioManager.PlaySfx(clip)` ganha o som novo pela lista "clipes antigos" do
banco (moedas, martelo da obra, raio absorvido do para-raio...), sem mexer no script de quem toca.

## Onde ajustar

- **Volume, tom, vozes, intervalo, escada de cada som**: `Assets/Resources/Audio/SoundBank.asset` (Inspector). O
  montador não apaga o que foi ajustado; só troca os clipes.
- **Quanto e quando cada evento toca** (volumes por evento, intervalo das ferramentas, trovões ao longe, grilos, a
  urgência que acelera com o caos): componente `GameAudio` no `AudioManager.prefab`.
- **Mix geral**: `Assets/Audio/V5/MixerV5.mixer`, grupos Musica, SFX, Ambiente e UI. Parâmetros expostos para um
  menu de opções: `MasterVolume`, `MusicVolume`, `SfxVolume`, `AmbienceVolume`, `UiVolume` (`AudioManager.SetMixerVolume`).
  Os volumes por categoria do `AudioManager` continuam valendo por cima.
- **Música**: `musica_v5.py` (100 bpm, 16 compassos em dó maior, 38,4 s). As camadas têm o mesmo número de amostras e
  começam juntas (`PlayScheduled`). Camada 0 base (sempre), 1 ritmo (desastre no ar ou tensão), 2 tensão (caos de 35% a
  70%), 3 perigo (caos de 70% a 100%). Tempo da troca: `layerFadeTime` no `AudioManager`.
- **Pausa (Esc/P)**: o `GameClock` pausa o `AudioListener` (efeitos, chuva, fogo param); música e interface seguem. A
  música abafa pelo passa-baixa do grupo Musica (`MusicCutoff`, 22 kHz para `pauseCutoffHz` = 800 Hz em 0,3 s). No WebGL
  o mixer só aceita volume (efeitos como o passa-baixa não rodam no navegador), então lá a música só abaixa
  (`webglPauseLevel`).
- **Ducking**: som com `ducks` (avisos, trovão perto, tremor, raio absorvido, vinhetas) abaixa a música e, menos
  (`ambienceDuckShare`), o ambiente.

## Sons por evento

| Evento | Som (variações) |
|---|---|
| Clique em botão / hover | `ui_click` (2) / `ui_hover` (antigo) |
| Abrir chamado, escolher base | `ui_tab` (3) + `ui_popup` (2) |
| Compra / negado / dinheiro | `ui_buy` / `ui_denied` (2) / `ui_coin_tick` (2, escada de tom: sobe 1 semitom por moeda seguida) |
| Casa quebra (chamado novo) | `call_new_quake` (2), `call_new_storm` (2), `call_new_fire` (2), `call_new_power` |
| Caos alto / fogo no fim | `call_urgent`: de 9 s a 3,5 s entre avisos, mais agudo com o caos; uma vez por casa com fogo quase no fim |
| Equipe sai / chega | `team_depart` (4) / `team_arrive` (2) |
| Equipe trabalhando | `repair_tool` (6: martelo e prego, catraca, furadeira), cada obra no próprio ritmo, até 3 obras soando |
| Conserto pronto / coleta | `repair_done` (3 arpejos) / `call_collect`, sobe de tom com a sequência de consertos |
| Fogo pega / jato / apaga | `fire_ignite` (2), `fire_loop` (antigo, cresce com as casas) / `fire_jet_start` + `fire_jet_loop` por casa / `fire_out` |
| Tempestade | `warning_storm`; `rain_light_loop` + `wind_loop` na hora, `rain_heavy_loop` cresce em 8 s; `thunder_far` (2) a cada 5-12 s de um lado; `thunder_near` (3) no raio, começando no estalo |
| Terremoto | `warning_quake`; `quake_rumble` + `quake_hit` (3) no epicentro e 2 réplicas |
| Apagão e religar | arquivos da energia trocados no lugar (`sfx_blackout`, `sfx_restore`, `sfx_energy_on`, `sfx_fuse_*`...) |
| Obra (terreno, para-raio) / raio absorvido | `build_start` / `build_done` / `rod_absorb` |
| Vitória / derrota | `jingle_win` / `jingle_lose` |
| Enchente (#524) | `siren` no aviso; `flood_rush` + `call_new_storm` no bueiro; `valve_creak` a cada 1/4 de volta, `valve_drain_loop` (fonte própria, tom pela pressão), `valve_slip` na falha; `fire_out` no fogo que a água apaga; `flood_drained` na coleta |
| Meteoro (#524) | `siren` no aviso; `meteor_rumble` por tremor, `meteor_whoosh` 1,1 s antes; `meteor_impact` + `quake_hit` + `quake_rumble` e 2 réplicas; `call_new_quake` 1,4 s depois; `repair_tool` no acerto, `ui_denied` baixo no erro; `build_done` + `ui_confetti` na volta |
| Guarita (#524) | `ui_popup` ao escolher; `build_start`, `repair_tool` a cada 0,6 s, `build_done`; `outpost_radio` pronta e a cada despacho sozinho |
| Cidade | `amb_day` cruza com `amb_night` pelo `DayNightCycle`; `amb_cricket` (4) soltos de noite; tudo mais baixo com chuva |

## Conferência (17/11)

- `Tools/Audio/conferir_v5.py`: 80 arquivos (com os 9 da energia), 0 reprovados (pico entre amostras abaixo de -0,5 dB,
  sem amostra cortada, sem DC, emenda dos loops, pré-ataque de no máximo 30 ms nos sons presos a evento, camadas da
  música do mesmo tamanho). Tabela em `docs/jam/SFX_V5_conferencia.txt`; folha de contato (forma de onda, espectro,
  medidas) em `docs/jam/sfx_v5_folha.png`. Loudness alvo por grupo: interface -21, chamados -18, desastres -17,
  energia -16, vinhetas -14,5 (máximo momentâneo); loops pelo integrado.
- Sons graves (tremor, trovão, aviso de terremoto) ganharam harmônicos acima de 220 Hz (`presenca`) para aparecerem no
  alto-falante de notebook; o aviso de terremoto ainda tem pouca energia na faixa do celular (5%), compensada por
  chacoalho de louça e cascalho.
- Testes de editor `SoundBankTests` (12) e validador `ValidateAll` sem erros. Robô no Play
  (`analises-lilith/jogo-defesa-civil/scripts/s5d/som.cs`): 27 sons diferentes disparados por eventos reais, pausa com
  corte de 22000 para 800 Hz e volta, as 4 camadas em 1,0 com o caos no máximo, 56 fontes de som todas num grupo do
  mixer, 0 erros no console. Não dá para escutar daqui: falta o ouvido do Pietro.

Regenerar tudo: `python -X utf8 Tools/Audio/sons_v5.py`, `musica_v5.py`, `creditos_md.py` (venv de vídeo do
`analises-lilith`) e a conferência com
`uv run --no-project --with numpy --with scipy --with pyloudnorm --with pillow python -X utf8 Tools/Audio/conferir_v5.py`.
