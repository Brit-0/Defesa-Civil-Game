# Fogo com jato e gerenciamento de energia

Ajudante 3 (#524), branch `jam-sistemas`, 16/11/2026. Pedido do Pietro: (1) o incêndio ganha um minigame próprio, curto, cuja falha faz o fogo crescer; (2) energia para bases, armazéns e automação, que entra mais tarde com um segundo tutorial curto (SimCity, Cities: Skylines, Jurassic World Evolution), simples e fluida.

## 1. Incêndio: o jato

**Para o jogador.** A equipe chega no fogo e aparece um anel de chamas em volta da casa, com uma barra de tempo. O jogador **arrasta o mouse apertado** (ou toca) por cima das chamas. Cada chama que apaga solta vapor e adianta o conserto.
- **Todas apagadas a tempo:** o fogo apaga na hora, com "APAGOU! +50%" e o som de herói.
- **Sobrou chama quando o tempo acaba:** "O FOGO CRESCEU!". O fogo sobe um nível e ganha mais uma parte de trabalho.

Enquanto a janela está aberta, o trabalho automático espera.

**O fogo tem nível (1 a 3).** Ele pega no nível 1. Sem equipe, sobe um nível a cada terço do `fireDuration`. Quando o tempo acaba, a casa queima como antes (`fireBurnoutDamage`). Chamas pequenas embaixo da casa mostram o nível e dão um soco ao subir.

| Nível | Chamas no jato | Trabalho (× `fireWorkTime`) |
|---|---|---|
| 1 | 3 | 1 |
| 2 | 4 | 2 |
| 3 | 5 | 3 |

Este jato substitui o mutirão (cliques rápidos). Os valores `FireClick` e `FireHero` do enum `SkillResult` continuam lá, sem uso, para não mudar a ordem.

**Código.**
- `Systems/Fire/FireJet.cs`: regras puras (`FireJet`, `FireRules`), testáveis fora do Unity.
- `House.cs`:
  - `FireLevel`, `Jet`, `OpenJet`, `TickJet`, `DouseFlame`;
  - o trabalho do fogo passa a ser `fireWorkTime × partes / equipe`;
  - `FireClick` saiu.
- `Systems/Fire/FireMinigame.cs` (apresentação):
  - monta o anel, a barra, as gotas, o vapor e as chamas de nível;
  - lê o ponteiro pelo Input System;
  - com a casa fora da tela, o anel fica preso na borda.
- `MiniGameFeel.cs`: textos e sons de `FireDouse`, `FireOut` e `FireGrew`.

## 2. Energia

**Para o jogador.** Aos 2:30 de partida (tempo sem pausa e sem tutorial segurando), depois do primeiro tutorial, aparece embaixo dos contadores uma plaquinha: **raio, consumo/capacidade e uma barra** que fica verde, amarela (≥ 80%) ou vermelha. Clicar nela abre o **gerador**:
- o nível do gerador;
- o que está sem energia;
- o botão **MELHORAR $custo (+3)**.

Só há duas decisões:
1. **Melhorar o gerador antes de crescer.** É o gerador do SimCity e do Skylines: cada coisa nova gasta, e a capacidade tem de vir antes.
2. **Religar no apagão.** É a queda de energia do Jurassic World Evolution, que quebra a repetição.

| Gasta | Energia |
|---|---|
| Base (Guarda) | 2 |
| Armazém | 1 |
| Cada nível de automação comprado | 1 |
| **Gerador** | 5 no começo, +3 por nível (4 níveis: $60, $108, $194, $350) |

**Sobrecarga** (consumo maior que a capacidade). A rede raciona em vez de desligar tudo:
1. a automação perde primeiro, do nível mais alto para o mais baixo;
2. depois os armazéns;
3. por último as bases; a mais nova desliga antes da mais antiga.

A plaquinha mostra "SOBRECARGA", treme e toca um alarme.

**Apagão.** Toda tempestade (com a energia ligada) tem 50% de chance de derrubar a rede no primeiro raio. Tudo desliga e a plaquinha mostra "APAGÃO!". Embaixo dela pisca o botão dourado **RELIGAR**, com uma linha que mostra quanto falta para a rede voltar sozinha (15 s). Base e armazém sem energia ganham uma tomada riscada piscando por cima.

**Efeito de ficar sem energia.**
- **Base:** não despacha. O despacho escolhe a base ligada mais perto; sem nenhuma, nega com "SEM ENERGIA!" e o som de curto (`DeniedReason.NoEnergy`).
- **Armazém:** não repõe material.
- **Automação:** fica limitada aos níveis ligados (`Stats.AutomationLevel`). Quando o nível 3 volta, coleta o que ficou pronto.

As bases e os armazéns novos da Expansão (ajudante 2) entram sozinhos: a rede procura `CityGuard` e `Warehouse` na cena a cada segundo.

**Código.**
- `Systems/Energy/EnergyGrid.cs`: distribuição pura, testável.
- `EnergyManager.cs`: liberação, recontagem, apagão, `IsPowered`, `NearestPoweredGuard`, `DeniesDispatch` e `[Button] Unlock` para debug.
- `EnergyHud.cs`: plaquinha, gerador, RELIGAR e avisos.
- `EnergyMarkers.cs`: tomadas em cima dos prédios.
- `FireEnergyArt.cs`: ícones e sons de `Resources/FireEnergy`.
- Mudanças em código existente:
  - `GuardManager.GetNearestGuard` e `TrySendTeam`;
  - `Stats.AutomationLevel`;
  - `GameEvents`: `DeniedReason.NoEnergy`, `SkillResult.FireDouse`, `FireOut` e `FireGrew`, todos no fim; a classe passou a ser `partial`.
- Eventos novos em `GameEvents.FireEnergy.cs`:
  - `FireLevelChanged`, `FireJetOpened`;
  - `EnergyUnlocked`, `EnergyChanged`, `PowerChanged`, `AutomationPowerChanged`, `BlackoutChanged`.

## 3. Segundo tutorial (energia)

`EnergyTutorial.cs` usa o mesmo balão e a mesma seta do primeiro (`TutorialView`). Os desastres esperam enquanto ele roda.
1. **ENERGIA 1/3:** o que gasta energia e onde ver (a seta aponta a plaquinha).
2. **ENERGIA 2/3:** um apagão de verdade. O passo só termina quando o jogador clica em **RELIGAR**, ou a rede volta sozinha.
3. **ENERGIA 3/3:** gerador e sobrecarga, com o botão "VAMOS!".

Ele roda uma vez só (`PlayerPrefs DefesaCivil.EnergyTutorialDone`). `Tools > Jam > Reset Energy Tutorial` mostra de novo. Nas partidas seguintes aparece só o aviso "A CIDADE AGORA USA ENERGIA!" ao lado da plaquinha.

## 4. Dados (sem número no código)

- `Assets/SO/FireEnergyData.cs` → `Assets/Resources/Balance/FireEnergyData.asset`:
  - jato: `fireMaxLevel` 3, `jetTime` 4,5 s, `jetFlamesBase` 3, `jetFlamesPerLevel` 1, `jetOutBonus` 0,5, `jetHitRadius` 46;
  - energia: `unlockTime` 150 s, `startCapacity` 5, `baseDemand` 2, `depotDemand` 1, `automationDemandPerLevel` 1, `stormBlackoutChance` 0,5, `blackoutAutoRestore` 15 s.
- `Assets/Resources/Upgrades/Generator.asset`: id `generator`, ordem 7, 4 níveis, $60 × 1,8^nível, +3 de capacidade por nível, ícone `upgrade_generator`. A linha do gerador na aba de melhorias só aparece depois que a energia entra.
- Arte em `Assets/Systems/Energy/Resources/FireEnergy/`:
  - 4 ícones do Pixel Lab (pixflux, paleta do kit, contorno #141218): `fire_flame`, `energy_bolt`, `energy_off`, `upgrade_generator`. Custaram 20 gerações (≈ US$ 0,16).
  - 5 sons sintetizados (`sons.py`): `sfx_blackout`, `sfx_restore`, `sfx_overload`, `sfx_energy_on`, `sfx_no_energy`.

## 5. Ligação na cena (sem editar a Gameplay.unity)

`FireEnergyLink` é o componente de ligação. Em toda cena com `GameManager`, se ainda não houver um, o bootstrap carrega o prefab `Resources/FireEnergy/FireEnergy.prefab`. O prefab traz:
- `FireEnergyLink`, `EnergyManager`, `EnergyHud`, `EnergyMarkers`, `FireMinigame` e `EnergyTutorial`.

Se o MEGABRAIN puser o prefab na cena, o da cena vale e nada é duplicado. `Tools > Jam > Create Fire And Energy Data` refaz o asset, o upgrade ou o prefab que tiver sido apagado.

**Para a ajudante 1 / MEGABRAIN no Unity:**
1. Puxar `jam-sistemas` e deixar importar.
2. Conferir no console se aparece algum aviso `[FireEnergyData]`.
3. Testar no Play:
   - no Inspector do `FireEnergy`, o botão **Unlock** liga a energia na hora;
   - para ver o jato, esperar um incêndio ou despachar para uma casa pegando fogo.
4. **Trocar a dica do fogo no `Tutorial.cs`.** Ela ainda fala do mutirão (`fireTip`). Texto novo:
   - `"JATO: quando a equipe chegar no incêndio, <bounce>arraste o mouse apertado</bounce> por cima das chamas antes do tempo acabar. Sobrou chama? O fogo cresce!"`
   - O `{0}` pode sair; o `string.Format` continua funcionando.

## 6. Conferências

- **Compilação** fora do Unity (Roslyn + DLLs do 6000.6.7f1 + Text Animator), modos jogador e editor: 0 erros. Uma sonda com erro proposital foi pega.
- **Testes:** 11 testes NUnit em `Systems/Energy/Editor/FireEnergyTests.cs` (jato, nível, crescer, rede, sobrecarga, apagão). Rodados fora do Unity: 11 passaram.
- **Simulador** (`docs/jam/sim`). O jato e a rede estão em `fogo_energia.py`, ligados ao `sim.py`. O robô religa no apagão e compra o gerador antes de passar da capacidade. Mapa de 153 casas, 30 partidas por linha:

| Jogador | Antes (mutirão, sem energia) | Agora (jato + energia) |
|---|---|---|
| ignora | perde em 3:53 | perde em 3:53 |
| bom | vence em 6:31 | vence em 6:32 |
| bom, sem mini games | vence em 7:11 | vence em 7:21 |
| médio | vence em 7:33 | vence em 7:38 |
| médio, sem mini games | vence em 8:14 | vence em 8:23 |
| bom, larga aos 6 min | perde em 8:28 | perde em 8:30 |

Mediana por partida:
- O bom apaga 2 jatos. O jogador sem mini games deixa 2 ou 3 fogos crescerem e perde cerca de 10 s.
- Sobre a janela do jato: com 3,5 s o médio (reação de 2 s) nunca acertava; com 4,5 s acerta 2 de 3.
- Todos compram 1 nível de gerador. Com a Expansão compram mais (`expansao.py` continua rodando).
- Há 0 a 1 apagão por partida, com 1 a 6 s sem energia. Ninguém fica em sobrecarga.
- O equilíbrio do jogo não mudou: os tempos mudaram no máximo 10 s.

- **Prévias** (montagem parada sobre a captura real, com a faixa "PRÉVIA 1280x720"), em `analises-lilith/jogo-defesa-civil/resultados/`:
  - `previa524_energia.png`, `previa524_sobrecarga.png`, `previa524_apagao.png`, `previa524_jato.png`.
  - Na conferência, as chamas e a tomada ficaram pequenas demais. Os ícones foram recortados, a chama passou para 72 e a tomada ganhou um círculo escuro.
- **Defeitos que já voltaram:**
  - `.meta` à mão em todo arquivo e pasta nova, sem GUID repetido no projeto;
  - enums só ganharam valores no fim (B18);
  - DOTween sempre com `SetLink`;
  - nada de `UnityEditor` fora de `Editor/`.

## 7. Falta

- **Play no Unity**, que é da ajudante 1. Nada foi aberto no Editor: o primeiro import cria os `.meta` de textura e de som a partir dos que foram escritos à mão (Point, sem compressão, como os ícones do kit).
- A dica antiga do mutirão no `Tutorial.cs` (seção 5).
- O poste elétrico antigo (`EletricPole`, problema B13) continua desligado; esta energia é outra e não usa ele.
