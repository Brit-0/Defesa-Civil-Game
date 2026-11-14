# Projeto: Defesa Civil Game — city builder top-down, pixel art (Unity)

## Contexto
- Unity 6000.6.7f1, URP 17.6.0 (Renderer2D), Input System 1.20.1 (Active Input Handling = Input System).
- Pixel art: PPU = 32, Filter Mode = Point, Compression = None. Sem build WebGL.
- Grid: sem grid (posições livres, 1 u = 32 px). Origem (0,0) = origem do mundo.
- Cena principal: `Assets/Scenes/Gameplay.unity`.
  - Estruturas em `UI/Canvas/UIContainer/Houses`.
  - Decoração em `.../Trees`.
  - Waypoints em `.../Waypoints`.
  - Chãos em `Main Camera/PolygonLayer`.

## Fonte da verdade
- Regras da cidade: `docs/city-rules.md` (cada regra tem ID Rxx). Leia antes de gerar ou alterar qualquer layout.
- Layout: `Assets/_Project/Data/Cities/city_layout.json`.
- Inventário de assets: `docs/inventory.md`.
- Decisões do usuário: `docs/decisions.md`.

## Dúvidas e incertezas (regra principal)
- NÃO definir nem deduzir nada que o usuário não tenha dito. Ao ter dúvida ou incerteza, pare e use AskUserQuestion com um formulário curto (múltipla escolha) e espere a resposta.
- Valores provisórios escolhidos a pedido do usuário são marcados como PLACEHOLDER em `docs/decisions.md`.

## Como trabalhar
- A cidade é gerada por dados: editar o JSON -> Tools/City/Validate -> Tools/City/Build. Não posicionar objetos manualmente na cena.
- O Build escreve nos pais existentes (Houses, Trees, Waypoints, PolygonLayer) e ajusta o Limiter. Sem root GeneratedCity. Build idempotente.
- No JSON (versão 3), `bottomCenter` é o centro da base VISÍVEL do sprite (sem a margem transparente) e `facing` é S ou N (fileira de baixo `_S`, de cima `_N`). Ruas têm largura única (R35), com rua de contorno. `junctions`/`links` descrevem o grafo de waypoints (R38).
- O Build revalida a cena reaberta do disco depois de salvar.
- Para avaliar um layout sem mexer na cena: `CityPreview.Render` gera um PNG (com `waypointGraph: true`, desenha o grafo).
- Só criar/alterar arquivos em `Assets/_Project/` e `docs/`, além da cena Gameplay via Build, salvo quando a tarefa pedir outra coisa. Não modificar assets existentes sem pedir.
- Executar sozinho tudo o que estiver ao alcance (git, MCP, testes, screenshots); não devolver passos manuais ao usuário.
- Antes de mudanças grandes: mostrar plano e esperar confirmação.
- Ao terminar uma etapa: rodar validador/testes e reportar violações por ID de regra.
- Git: branch city-gen, commits pequenos em português, nunca reescrever histórico.
- Não alterar a câmera nem o CameraController (nem PolygonCamera, MapRender, PolygonsDisplay). Filhos de PolygonLayer podem ser criados e removidos.

## Convenções de código
- C# 9.0, namespace `DefesaCivil.CityGen`, scripts de Editor em pasta `Editor/`.
