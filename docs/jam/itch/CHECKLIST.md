# Checklist de entrega — Defesa Civil

Marque na ordem. Item com **[TRAVA]** impede o envio até ser resolvido. Escrito pelo Agente D em 16/11; os builds de verdade são do MEGABRAIN (T46), na pasta principal, com o Unity fechado.

## 1. Antes do build (quarta 18/11, depois do congelamento das 12h)

- [ ] **[TRAVA]** Branch `jam-itch` integrada na `jam` (traz `Assets/Editor/BuildScript.cs`, `Tools/Build/` e esta pasta).
- [ ] **[TRAVA]** `jam` atualizada com as últimas entregas de B e C; console do Editor com 0 erro; uma partida jogada no Play do começo ao fim.
- [ ] Player Settings conferidos (decisão do MEGABRAIN; o build não muda nenhum deles de vez):
  - `Company Name` hoje é `DefaultCompany`. Aparece na pasta de log e de PlayerPrefs (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\Defesa Civil Game\`). Sugestão: o nome da equipe.
  - `Product Name` `Defesa Civil Game`: é o título da janela no Windows e da aba no navegador. Sugestão: `Defesa Civil`, igual ao menu.
  - `Version` `1.0`.
  - Ícone do executável e cursor (`ART_REQUESTS.md`).
  - Resolução padrão Windows: 1920 x 1080, tela cheia (`fullscreenMode: 1`). WebGL: 960 x 600.
- [ ] Pacote `com.unity.pipeline` (ferramenta do `unity` CLI, `DECISIONS.md`): se o build ficar maior por causa dele, tirar do `manifest.json` antes do build final.
- [ ] Nenhum botão ou texto de debug visível (botões Odin só aparecem no Editor; conferir que nada de debug ficou na tela).
- [ ] Kits de primeiros socorros escondidos do HUD; modo de energia (Tab) desligado.

## 2. Build

O Unity Editor **não pode estar aberto no mesmo projeto**: o script confere a trava `Temp\UnityLockfile` e a linha de comando dos processos `Unity.exe` e para com a mensagem "O Unity Editor está aberto neste projeto". Outro projeto aberto (outra pasta) não atrapalha.

Na pasta do projeto, num PowerShell:

```
powershell -ExecutionPolicy Bypass -File Tools\Build\build_windows.ps1
powershell -ExecutionPolicy Bypass -File Tools\Build\build_webgl.ps1
```

Opções: `-ProjectPath <pasta>` (padrão: a raiz do repositório onde o script está), `-Development` (build de desenvolvimento; o zip ganha `_dev`), `-SemZip`, `-UnityExe <caminho>`, e no WebGL `-SemFallback`.

O que sai:

| Arquivo | O que é |
|---|---|
| `Builds\Windows\DefesaCivil.exe` + pastas | Build Windows 64 bits (a pasta é apagada e refeita a cada build) |
| `Builds\WebGL\index.html` + `Build\` + `TemplateData\` | Build WebGL |
| `Builds\zip\DefesaCivil_Windows.zip` | Conteúdo de `Builds\Windows`, sem `*_BurstDebugInformation_DoNotShip` e `*_BackUpThisFolder_ButDontShipItWithYourGame` |
| `Builds\zip\DefesaCivil_WebGL.zip` | Conteúdo de `Builds\WebGL`, com `index.html` na raiz do zip (exigência do itch.io) |
| `Builds\logs\<alvo>_<data>.log` | Log completo do Unity; o script mostra as linhas `[BuildScript]`, erros de compilação e, se falhar, as últimas 20 linhas |

`Builds\` está no `.gitignore`: nada disso vai para o git.

Os zips são feitos com barra normal (`/`) nos caminhos internos. O `Compress-Archive` do PowerShell 5.1 grava barra invertida, que o itch.io e o `unzip` do Linux leem como parte do nome do arquivo.

- [ ] `build_windows.ps1` terminou com "Build Windows OK" e o zip.
- [ ] `build_webgl.ps1` terminou com "Build WebGL OK" e o zip.
- [ ] Nenhum dos dois mostrou "AVISO: ProjectSettings.asset mudou durante o build". Se mostrou: `git diff ProjectSettings` e decidir antes de commitar.

**Compressão do WebGL (decisão para o MEGABRAIN):** o `DECISIONS.md` diz que o itch.io entrega `.br` com o cabeçalho certo, mas há relato na comunidade da Unity de página travada no carregamento porque o itch.io serviu `.br` sem `Content-Encoding: br` ([fórum da Unity](https://discussions.unity.com/t/webgl-build-doesnt-load-in-browser/803777)). Por isso o `build_webgl.ps1` liga o **Decompression Fallback** só durante o build e devolve o valor do projeto depois: o loader descompacta sozinho com ou sem o cabeçalho, e o mesmo zip abre num servidor local simples. O custo é um loader um pouco maior e sem compilação do WebAssembly em streaming, irrelevante num jogo de 18 MB. Para usar o valor do projeto (Brotli sem fallback), rode com `-SemFallback`.

## 3. Windows em máquina limpa

"Limpa" = sem Unity e sem o projeto. Opções, da melhor para a mais simples: o PC 2 (do Agente C) ou o de outra pessoa; a **Área Restrita do Windows** (Windows Sandbox, existe no Windows 11 Pro: "Ativar ou desativar recursos do Windows" → "Área Restrita do Windows"); no mínimo, outra conta de usuário no mesmo PC.

- [ ] Copiar só o `DefesaCivil_Windows.zip` para lá e extrair numa pasta com espaço e acento no caminho (ex.: `Área de Trabalho\Defesa Civil`).
- [ ] Abrir o `DefesaCivil.exe`. Se o SmartScreen avisar ("O Windows protegeu o computador"), anotar: a página precisa da frase "Clique em Mais informações → Executar assim mesmo".
- [ ] Abre em tela cheia em até 10 s, com o menu e a cidade.
- [ ] Som toca no menu ou no primeiro clique; música e efeitos em volume certo.
- [ ] Jogar uma partida até vencer ou perder; **Reiniciar** funciona e a música não corta.
- [ ] `Alt + Enter` alterna janela e tela cheia; `Alt + F4` fecha. Se o menu tiver botão de sair, ele fecha o jogo.
- [ ] Resolução menor (1366 x 768 ou janela): HUD não corta nem sobrepõe.
- [ ] `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\Player.log` sem `Exception` nem `Error` (procurar as duas palavras).

## 4. WebGL em Chrome e Firefox

Teste local primeiro, com a build que vai no zip:

```
python -m http.server 8000 --directory Builds\WebGL
```

e abrir `http://localhost:8000`. (Funciona porque o fallback de descompressão está ligado; com `-SemFallback` o servidor simples não serve o Brotli com o cabeçalho.)

Fazer **em cada navegador** (Chrome e Firefox; Edge se sobrar tempo), numa janela anônima:

- [ ] Carrega até o menu em menos de 20 s (critério da U3); anotar o tempo.
- [ ] **Áudio só depois do primeiro clique:** antes de clicar, silêncio e nenhum erro no console; depois de clicar em **Jogar**, a música e os efeitos tocam. Nenhum som "atrasado" acumulado tocando de uma vez.
- [ ] `F12` → Console: 0 erro vermelho do começo ao fim da partida (avisos amarelos: anotar).
- [ ] Roda do mouse dá zoom **sem rolar a página** do itch.io.
- [ ] Arrastar com o botão direito move a câmera **sem abrir o menu de contexto** do navegador.
- [ ] `Esc` em tela cheia: o navegador sai da tela cheia (é dele). Conferir se também cancela o chamado aberto e se isso confunde; se confundir, anotar para o MEGABRAIN.
- [ ] `P` pausa e despausa.
- [ ] Botão de tela cheia do itch.io (ou do template) funciona e a imagem fica nítida (pixel art sem borrão).
- [ ] Trocar de aba por 30 s e voltar: o jogo pausou e voltou sem quebrar (`runInBackground` está desligado).
- [ ] Partida inteira de 10 a 15 min sem travar nem estourar memória; FPS estável (critério da U3: 50 ou mais parado, 40 ou mais com a câmera em movimento).
- [ ] Recarregar a página: a segunda carga é mais rápida (cache de dados ligado).

## 5. Tamanho dos zips

Anotar aqui depois do build:

| Zip | Tamanho | Arquivos |
|---|---|---|
| `DefesaCivil_Windows.zip` | | |
| `DefesaCivil_WebGL.zip` | | |

Referências: build WebGL de fumaça de 16/11 = 18 MB (Brotli); build Windows de **desenvolvimento** de 16/11 = 235 MB na pasta (o de entrega é menor).

- [ ] WebGL: menos de 50 MB é o alvo (carga rápida); o `build_webgl.ps1` avisa se passar dos limites do itch.io para HTML5 (1000 arquivos, 200 MB por arquivo, 500 MB descompactado — limites da página de ajuda deles; confirmar na hora do envio).
- [ ] Windows: menos de 1 GB (limite padrão de upload do itch.io); alvo abaixo de 150 MB.
- [ ] Dentro do zip Windows não há `*_DoNotShip` nem `*_ButDontShipItWithYourGame` (o script já tira; conferir abrindo o zip).

## 6. Licenças

- [ ] **[TRAVA]** Textura de rachadura de licença incerta: `Assets/Arts/Texture/pngtree-crack-texture-png-image_15379851-export.png` apagada do projeto, ou confirmado que nada a usa. Conferência (no Git Bash, na pasta do projeto): `git grep -l 1b22d8567d928854eb84c97e41aee76a -- Assets` deve listar só o `.meta` dela, ou nada depois de apagar. Em 16/11 só aparecia o `.meta`, em todas as branches.
- [ ] **[TRAVA]** Se o `Crack.png` próprio entrou: o `HouseMAT.mat` aponta para ele (ou continua no `LightningSheet.png` do Sprite Shaders Ultimate, que é da licença do plugin).
- [ ] Sons: todos de `Assets/Audio/` vêm do pacote Gamemaster (lista no `STATUS_C.md` da `jam-feel`) ou são CC0 com origem anotada. Nenhum arquivo de som solto dentro dos zips (o Unity empacota os sons dentro dos dados do jogo; conferir que não há `.wav`/`.ogg` soltos).
- [ ] **[TRAVA]** O repositório continua privado e a página não tem link para ele (os sons pagos estão em `Assets/Audio/`).
- [ ] Fonte ModernDOS: CC0 (conferido na fonte). Se outra fonte entrou, anotar licença.
- [ ] Arte da cidade: origem confirmada com o Pietro e com quem desenhou (ver `PAGINA.md`, créditos).
- [ ] Regras da jam sobre uso de IA lidas; linha de IA nos créditos mantida ou tirada conforme a regra.
- [ ] Créditos da página iguais aos do jogo (se o jogo tiver tela de créditos).

## 7. Página do itch.io (rascunho)

Textos e campos em `PAGINA.md`. Envio na quinta 19/11, feito por uma pessoa da equipe (nenhum agente sobe nada).

- [ ] Projeto criado como **Draft**; campos de `PAGINA.md`, seção "Configuração da página".
- [ ] Upload do WebGL marcado "This file will be played in the browser"; embed 960 x 600; botão de tela cheia ligado.
- [ ] Upload do Windows marcado "Windows".
- [ ] Capa 630 x 500, 6 a 8 capturas e 2 a 3 GIFs (lista em `PAGINA.md`).
- [ ] Abrir a página em rascunho (o itch.io mostra para o dono) em Chrome e Firefox e repetir a seção 4 **na página do itch.io**: carga, áudio após o clique, zoom sem rolar a página, tela cheia.
- [ ] Baixar o zip Windows pela página, extrair e abrir (o arquivo que o jogador baixa é o que conta).
- [ ] Hora exata do fim do envio conferida na página da jam (pendência P5 do `GDD.md`) e anotada aqui: ______.
- [ ] Página publicada (**Public**) e enviada para a jam antes do prazo.
- [ ] Depois de publicar: abrir a página numa janela anônima, deslogado, e jogar 2 minutos.
