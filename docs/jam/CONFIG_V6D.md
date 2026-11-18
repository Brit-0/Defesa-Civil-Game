# Configurações v6D (#529, 18/11/2026)

Janela de configurações no estilo pixel do HUD (FeelUI/PixelSkin). Nasce por código na inicialização
(`SettingsApply.Ensure`, objeto `[Configurações]`); `Gameplay.unity`, câmera e balanceamento não foram mexidos.

## Como abrir
- Engrenagem no canto de cima à direita, ao lado da pausa (no jogo e no menu inicial).
- Pausa (Esc) > CONFIGURAÇÕES. O Esc fecha a janela e volta para a pausa.
- Aberta pela engrenagem, a janela pausa o jogo (`GameManager.Pause` → `GameClock.Freeze`); ao fechar, o jogo continua.

## Abas
- **SOM:** volume geral, música, efeitos e ambiente (grupos do `MixerV5`), cada um com mudo no alto-falante.
  Os sons da interface seguem os efeitos.
- **TELA:** tela cheia (onde houver), tamanho da interface (pequeno, médio, grande: escala dos Canvas de tela),
  partículas (reduzido = `VfxLevel.Low`), mostrar FPS (canto de baixo à direita) e modo daltônico.
- **JOGO:** tremor de tela com intensidade (`CameraShaker`), vibração do controle (Nice Vibrations, só no PC),
  dicas e tutoriais (desligar marca as dicas como vistas; religar devolve como estavam) e "rever tutoriais".
- **CRÉDITOS.** No rodapé: RESTAURAR PADRÕES, versão e VOLTAR.

## Modo daltônico
Paleta Okabe-Ito nos pinos de chamado (`CallPalette`); o "pronto" verde vira azul-celeste
(`Resources/SettingsColorblind/`), sem a dupla vermelho e verde.

## Código
- `Assets/Systems/Settings/`:
  - `GameSettings` é a regra pura: padrões, limites, dB e leitura e gravação.
  - `SettingsStore` guarda nos PlayerPrefs com o prefixo `DefesaCivil.Settings.` e avisa pelo evento `Changed`.
  - `SettingsApply` aplica nos sistemas.
  - `SettingsPanel` é a janela.
  - `SettingsIcons` faz a engrenagem e os ícones de alto-falante 16x16 por código.
- Ligações em outros arquivos:
  - `CameraShaker.Shake`: multiplicador do tremor e vibração.
  - `House` e `MapMarkers`: ícones e cores do modo daltônico.
  - `PauseMenu`: botão CONFIGURAÇÕES e Esc.
  - `TutorialView.Skip`.
- Os padrões repetem o som da v5D: volume 1 é 0 dB e tudo começa ligado, menos o FPS.

## Conferência
- 208 testes EditMode passando, 15 deles novos em `SettingsStoreTests`.
- ValidateAll: 0 erros.
- Robô no Play em 1920x1080 e 1280x720:
  - 0 erros no console.
  - 0 textos maiores que a caixa, e o painel cabe na tela também no tamanho grande.
  - Mixer conferido: geral 50% = -6 dB, mudo = -80 dB.
  - Esc conferido entre a janela e a pausa.
