# Consertos v7C: o jogo se chama Please Fix the City

Ajudante 1 (#545), branch `jam`, 19/11/2026. Pedido do Pietro: trocar o nome do jogo, só o nome. "Defesa Civil" fica onde é o tema (a defesa civil de verdade: a central do Centro de Operações, o emblema, o laranja, "A Defesa Civil deu conta do recado.") e no nome do repositório e das pastas.

## O que mudou

| Onde | Antes | Agora |
|---|---|---|
| Menu inicial (`MainMenu.prefab` e `MainMenu.cs`) | DEFESA CIVIL, fonte 150, emblemas a ±600 | PLEASE FIX THE CITY, fonte 110, emblemas a ±660 |
| Cartão do tutorial (`Tutorial.cs`) | DEFESA CIVIL | PLEASE FIX THE CITY |
| Créditos nas configurações (`SettingsPanel.Rows.cs`) | DEFESA CIVIL | PLEASE FIX THE CITY |
| `productName` (`ProjectSettings.asset`, só essa linha) | Defesa Civil Game | Please Fix the City (título da janela no Windows e da aba no navegador) |
| `README.md`, `GDD.md`, `CLAUDE-jam.md`, `MEGABRAIN.md`, `PROMPTS_PRONTOS.md`, `CLAUDE.md` | Defesa Civil Game | Please Fix the City |

O título novo tem 19 letras contra 12. Em 150 ele mede 1425 px e passava por cima dos emblemas. Em 110 mede 1045 px. Os emblemas foram para ±660, com 77 px de folga de cada lado. Assim cabe também na tela 16:10 do itch.io (960x600), onde a faixa encolhe para 1548 px.

Na branch `jam-itch` (commit 0952478), onde estão a página do itch.io e os scripts de build:
- `docs/jam/itch/PAGINA.md`, `CHECKLIST.md` e `PLAYTEST.md` levam o nome novo.
- O build sai como `PleaseFixTheCity.exe`, `PleaseFixTheCity_Windows.zip` e `PleaseFixTheCity_WebGL.zip` (`BuildScript.cs`, `build_webgl.ps1`, `build_windows.ps1`).

O template WebGL é o Default, que já usa o `productName`: o próximo build sai com "Please Fix the City" no título da página. No build local de 17/11 (`Builds/WebGL/index.html`, fora do git), o título da página foi trocado à mão. O menu dentro dele ainda é o antigo, até o próximo build.

Ficou como estava:
- `companyName` (`DefaultCompany`): a equipe não tem nome decidido.
- `metroPackageName` e `metroApplicationDescription`: são da UWP, que não é alvo do jogo.
- O menu do Editor "Defesa Civil/Sound Bank": o jogador não vê.
- Shaders `DefesaCivil/...`, namespace `DefesaCivil.CityGen` e chaves `DefesaCivil.*` dos PlayerPrefs: são identificadores do código.

Efeito colateral: o Unity guarda os PlayerPrefs pelo `companyName` e pelo `productName`. Por isso, no Editor e nos builds novos, o tutorial e as configurações começam do zero uma vez.

## Conferências

- **Testes EditMode:** 245 de 245 passando. Entre eles estão os 3 novos de `GameNameTests`: `productName`, título do menu e do cartão, e título cabendo entre os emblemas na 16:10.
- **Play no Editor principal (Game View 1920x1080), 0 erros e 0 avisos no console:**
  - Menu: "PLEASE FIX THE CITY" em fonte 110, 1045 px de largura numa faixa de 1740, emblemas a ±660 (120 px).
  - Cartão do tutorial depois do TEMPORAL: 608 px de 868.
- **Prints:** `analises-lilith/jogo-defesa-civil/v7c/menu_v7c.png` e `cartao_v7c.png`. Os scripts estão na mesma pasta (`medir.cs`, `produto.cs`, `play.cs`, `card.cs`, `card2.cs`).
- **Grep de "Defesa Civil"** em `Assets`, `ProjectSettings` e `docs/jam`: sobra só o tema e o que está listado acima.
