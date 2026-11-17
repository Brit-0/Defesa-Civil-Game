# Casa destelhada (tempestade) — #532, 17/11/2026

Pergunta do Pietro: existe diferença mecânica entre a casa quebrada normal e a casa quebrada por tempestade?

## Resposta (como estava, branch `jam` 0e7ca50)

Não. Depois de quebrada, a casa da tempestade era igual à do terremoto: o mesmo estado `Damaged`, o mesmo custo
(`damageAmount` materiais), o mesmo tempo (`workTimePerDamage` × dano = 5 s por ponto) e a mesma recompensa
(`moneyPerDamage` × dano = 15 por ponto). A tempestade só diferia **na chegada**:

| | Terremoto | Tempestade |
|---|---|---|
| Onde quebra | casas em volta do epicentro | casas intactas sorteadas pelo mapa todo, uma a uma em ~8 s |
| Dano | 3 / 2 / 1 pela distância do epicentro | 1 ou 2 (`stormMinDamage`/`stormMaxDamage`) |
| Extra | — | 20% de chance de incêndio por raio; apagão depois de liberar a energia |
| Visual | rachaduras, pin laranja | pin azul, chuva, relâmpagos |

## Regra nova: a chuva entra pelo telhado [PROPOSTA]

A casa quebrada pela tempestade fica **destelhada** (`House.IsRoofless`). Enquanto espera **sem equipe**, a chuva entra
e o dano sobe **+1 a cada 25 s**, até **3**. Mandar a equipe para a piora na hora. Como custo, tempo e recompensa já
seguem o dano, a casa que esperou pede mais material e mais tempo (e paga mais).

- Prioridade: casa da tempestade primeiro; a do terremoto não piora.
- Números (em `BalanceData`, cabeçalho STORM): `stormLeakInterval = 25` (s; 0 desliga) e `stormLeakMaxDamage = 3`.
- Acaba quando a casa sai de `Damaged` (consertada, incendiada ou destruída) ou quando a equipe chega/está a caminho.
- Feedback: a cada subida, respingo de chuva no telhado e o selo de dano da casa pulsa (`MapMarkers`), e o número do
  selo sobe. O pin continua azul (o `MapMarkers` e o `HouseStatusFx` usam `IsRoofless` para saber a causa).

## Código

- `Assets/Systems/Disasters/StormLeak.cs`: a regra pura (`Tick`), testada sem cena.
- `Assets/Systems/Houses/House.cs`: `ApplyProblem(..., storm: true)` marca `IsRoofless`; `TickLeak` sobe o dano e
  dispara `GameEvents.StormLeaked(house, dano)` (`GameEvents.Storm.cs`).
- `Assets/Systems/Disasters/DisasterManager.cs`: a tempestade chama `ApplyProblem` com `storm: true`.
- Testes: `Assets/Systems/Disasters/Editor/StormTests.cs` (2 testes EditMode).

## Como testar no Play

Esperar a primeira tempestade (logo depois de 2:50 desde o RITMO_V3; era 90 s) e não mandar equipe para uma casa de pin azul: o selo vai de 1 ou 2 para 3 em até
50 s, com respingo e pulso a cada subida. Mandar a equipe para a subida na hora.
