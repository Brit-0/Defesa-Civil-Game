# Consertos v7D: equipe CryptCandle e builds novos para o itch.io

Ajudante 1 (#548), branch `jam`, 19/11/2026. Pedido do Pietro: pôr o nome da equipe, CryptCandle, no jogo, na página do itch.io e nos docs, e gerar builds novos de WebGL e de Windows (só gerar, sem publicar).

## O que mudou

| Onde | Antes | Agora |
|---|---|---|
| `companyName` (`ProjectSettings.asset`, só essa linha) | DefaultCompany | CryptCandle |
| Menu inicial (`MainMenu.cs`) | sem a equipe | "CryptCandle" pequeno (24, branco a 60%, contorno) no canto de baixo, à direita, abaixo da faixa dos passos |
| Créditos (`SettingsPanel.Rows.cs`) | CRIAÇÃO: Pietro Piccoli e Felipe Brito | CRIAÇÃO: CryptCandle, e embaixo Pietro Piccoli e Felipe Brito (mais ninguém) |
| Página do itch.io | sem texto na `jam` | `docs/jam/ITCH_PAGINA.md`: título, descrições curta e longa, como jogar, controles, créditos, configuração, tags e a lista de prints |
| Branch `jam-itch` (commit fe0a330) | equipe a confirmar, zips `PleaseFixTheCity_*.zip` em `Builds\zip` | `PAGINA.md`, `CHECKLIST.md` e `PLAYTEST.md` com CryptCandle; os scripts gravam `Please-Fix-the-City-WebGL.zip` e `Please-Fix-the-City-Windows.zip` em `Builds\itch` |

O nome fica num lugar só: `MainMenu.TeamName`, usado pelo menu e pelos créditos. Teste novo em `GameNameTests`: `companyName` igual ao `TeamName`, e a seção CRIAÇÃO dos créditos com exatamente "CryptCandle" e "Pietro Piccoli e Felipe Brito".

Efeito colateral: o Unity guarda os PlayerPrefs e o `Player.log` pela empresa. Com a troca, o Editor e os builds novos começam do zero uma vez (tutorial e configurações), e o log do Windows fica em `%USERPROFILE%\AppData\LocalLow\CryptCandle\Please Fix the City\Player.log`.

## Builds (fora do git)

Feitos no Editor principal (sem Play de ninguém), com a mesma regra do `BuildScript.cs` da `jam-itch`: o fallback de descompressão do WebGL fica ligado só durante o build (o projeto volta para desligado). Zips pela `New-ZipDaPasta` do `BuildComum.ps1`: barra normal nos caminhos e o `index.html` na raiz.

| Arquivo | Conteúdo | Tamanho |
|---|---|---|
| `Builds/itch/Please-Fix-the-City-WebGL.zip` | `Builds/WebGL` (Brotli com fallback, 17 arquivos, 25,7 MB) | 25,4 MB |
| `Builds/itch/Please-Fix-the-City-Windows.zip` | `Builds/Windows` (`PleaseFixTheCity.exe` + `PleaseFixTheCity_Data`, 225 arquivos, sem a pasta `BackUpThisFolder_ButDontShipItWithYourGame`) | 49,7 MB |

Build Windows: 0 erros, 2,6 min. Build WebGL: 0 erros, 5,6 min. O alvo ativo do Editor continua WebGL.

## Conferências

- **Testes EditMode:** 247 de 247 passando (245 + 2 novos de `GameNameTests`). O `city_ground.png` ficou igual (hash antes e depois).
- **Play no Editor, 0 erros no console:** menu com "CryptCandle" no canto, sem encostar na faixa dos passos; créditos com 14 linhas, 361 px de altura numa página de 424.
- **WebGL do zip** (extraído numa pasta nova, servidor local, Chrome sem janela, tamanho nativo 960x600): carrega em 4 s; aba "Unity Web Player | Please Fix the City"; menu com o título novo e CryptCandle no canto; engrenagem e aba CRÉDITOS certas. Console do navegador: 0 erros, 0 exceções e 2 avisos que já existiam (capacidade do DOTween e o shader FSR fora do build).
- **Windows do zip** (extraído numa pasta nova): janela "Please Fix the City" com o menu novo; `Player.log` na pasta da CryptCandle, 0 linhas com Exception ou Error. A chave de registro que o teste em janela criou foi apagada (o próximo início abre em tela cheia, o padrão).
- **Prints:** `analises-lilith/jogo-defesa-civil/v7d/` (`menu_v7d_1280x720.png`, `creditos_v7d_1280x720.png`, `web3_menu.png`, `web3_creditos.png`, `exe_menu.png`). Os scripts estão na mesma pasta (`build.cs`, `rodar.sh`, `navegador.ps1`, `exe.ps1`).
- **Página do itch.io:** prints 1280x720 e capa 630x500 em `analises-lilith/jogo-defesa-civil/itch/` (lista em `ITCH_PAGINA.md`).
