"""Leitura dos dados do simulador: BalanceData.cs, FireEnergyData.cs, JamDataMenu.cs, os .asset de Resources e o mapa."""
import heapq
import json
import math
import re
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[3]
BALANCE_CS = RAIZ / "Assets/SO/BalanceData.cs"
MENU_CS = RAIZ / "Assets/Systems/Masters/Editor/JamDataMenu.cs"
FIRE_ENERGY_CS = RAIZ / "Assets/SO/FireEnergyData.cs"  # jato e energia (ENERGIA_FOGO.md)
GENERATOR_ASSET = RAIZ / "Assets/Resources/Upgrades/Generator.asset"  # fora do JamDataMenu: vem do .asset
EXPANSION_ASSETS = [RAIZ / "Assets/Resources/Upgrades/Base2.asset", RAIZ / "Assets/Resources/Upgrades/Depot2.asset"]
ROD_ASSET = RAIZ / "Assets/Resources/Upgrades/Rod.asset"  # para-raio (PARARAIO.md)
ROD_DATA = RAIZ / "Assets/Systems/LightningRod/Resources/LightningRod/LightningRodData.asset"
OUTPOST_ASSET = RAIZ / "Assets/Resources/Upgrades/Outpost.asset"  # guarita (GUARITAS.md)
OUTPOST_CS = RAIZ / "Assets/SO/OutpostData.cs"
OUTPOST_DATA = RAIZ / "Assets/Systems/Outpost/Resources/Outpost/OutpostData.asset"
MAPA_PADRAO = RAIZ / "Assets/_Project/Data/Cities/city_layout.json"


def ler_balance(caminho=BALANCE_CS):
    """Padrões dos campos do BalanceData.cs (int, float ou int[]), mais os do FireEnergyData.cs."""
    texto = Path(caminho).read_text(encoding="utf-8")
    if Path(caminho) == BALANCE_CS and FIRE_ENERGY_CS.exists():
        texto += FIRE_ENERGY_CS.read_text(encoding="utf-8")
    valores = {}
    for tipo, nome, valor in re.findall(r"public\s+(int|float)\s+(\w+)\s*=\s*(-?[\d.]+)f?\s*;", texto):
        valores[nome] = int(valor) if tipo == "int" else float(valor)
    for nome, corpo in re.findall(r"public\s+int\[\]\s+(\w+)\s*=\s*\{([^}]*)\}\s*;", texto):
        valores[nome] = [int(x) for x in re.findall(r"-?\d+", corpo)]
    return valores


def ler_upgrades(caminho=MENU_CS):
    """Tabela de upgrades do JamDataMenu.cs: id -> maxLevel, baseCost, costGrowth, effectPerLevel."""
    texto = Path(caminho).read_text(encoding="utf-8")
    padrao = (r'CreateUpgrade\("\w+",\s*UpgradeIds\.(\w+),\s*"[^"]*",\s*"[^"]*",\s*(\d+),\s*(\d+),'
              r'\s*(\d+),\s*([\d.]+)f,\s*([\d.]+)f')
    ups = {}
    for nome, ordem, maximo, base, cresc, efeito in re.findall(padrao, texto):
        ups[nome.lower()] = dict(order=int(ordem), maxLevel=int(maximo), baseCost=int(base),
                                 costGrowth=float(cresc), effectPerLevel=float(efeito))
    for arquivo in [GENERATOR_ASSET] + EXPANSION_ASSETS + [ROD_ASSET, OUTPOST_ASSET]:  # base2, depot2, rod e outpost só pelo .asset
        if arquivo.exists() and arquivo.stem.lower() not in ups:
            ups.update(_upgrade_do_asset(arquivo))
    return ups


def ler_pararaio(caminho=ROD_DATA):
    """LightningRodData.asset: atração do raio pelo quarteirão protegido e tempo de obra (padrões do C# sem o asset)."""
    dados = _ler_yaml_simples(caminho) if Path(caminho).exists() else {}
    return dict(atracao=float(dados.get("attraction", 1.0)), obra=float(dados.get("buildTime", 3.0)))


def ler_guarita(caminho=OUTPOST_DATA):
    """OutpostData (GUARITAS.md): intervalo do despacho, reação e obra. Do .asset; sem ele, os padrões do OutpostData.cs."""
    padrao = {}
    if OUTPOST_CS.exists():
        for nome, valor in re.findall(r"public\s+float\s+(\w+)\s*=\s*(-?[\d.]+)f?\s*;", OUTPOST_CS.read_text(encoding="utf-8")):
            padrao[nome] = float(valor)
    if Path(caminho).exists():
        padrao.update({k: float(v) for k, v in _ler_yaml_simples(caminho).items() if k in padrao})
    return dict(intervalo=padrao.get("dispatchInterval", 1.0), reacao=padrao.get("reactionDelay", 1.5),
                obra=padrao.get("buildTime", 3.0))


def _ler_yaml_simples(caminho):
    valores = {}
    for linha in Path(caminho).read_text(encoding="utf-8").splitlines():
        m = re.match(r"\s{2}(\w+):\s*(.*)$", linha)
        if m:
            valores[m.group(1)] = m.group(2).strip()
    return valores


def ler_assets(pasta_resources, balance, ups):
    """Sobrepõe os padrões com os .asset de Assets/Resources (o que o jogo carrega de verdade)."""
    pasta = Path(pasta_resources)
    for nome in ("Balance/BalanceData.asset", "Balance/FireEnergyData.asset"):
        arquivo = pasta / nome
        if not arquivo.exists():
            continue
        for chave, valor in _ler_yaml_simples(arquivo).items():
            if chave in balance and not isinstance(balance[chave], list):
                balance[chave] = type(balance[chave])(float(valor))
    for arquivo in sorted((pasta / "Upgrades").glob("*.asset")):
        ups.update(_upgrade_do_asset(arquivo))
    return balance, ups


def _upgrade_do_asset(arquivo):
    dados = _ler_yaml_simples(arquivo)
    uid = dados.get("id")
    if not uid:
        return {}
    return {uid: dict(order=int(dados.get("order", 0)), maxLevel=int(dados["maxLevel"]),
                      baseCost=int(dados["baseCost"]), costGrowth=float(dados["costGrowth"]),
                      effectPerLevel=float(dados["effectPerLevel"]))}


def aplicar_ajustes(balance, ups, ajustes):
    """--set campo=valor (BalanceData) ou --set id.campo=valor (upgrade)."""
    for ajuste in ajustes or []:
        chave, valor = ajuste.split("=", 1)
        if "." in chave:
            uid, campo = chave.split(".", 1)
            ups[uid][campo] = type(ups[uid][campo])(float(valor))
        else:
            if chave not in balance:
                raise KeyError(f"BalanceData não tem o campo {chave}")
            balance[chave] = type(balance[chave])(float(valor))
    return balance, ups


def ler_mapa(caminho=MAPA_PADRAO):
    """Casas, Guarda e Armazéns com a distância pela rua até a entrada da Guarda."""
    dados = json.loads(Path(caminho).read_text(encoding="utf-8"))
    nos = {}
    for s in dados["structures"]:
        nos[f"{s['type']} {s['id']}"] = (s["entry"]["x"], s["entry"]["y"])
    for j in dados["junctions"]:
        nos[f"Junction {j['id']}"] = (j["position"]["x"], j["position"]["y"])
    vizinhos = {nome: [] for nome in nos}
    for link in dados["links"]:
        a, b = link["a"], link["b"]
        peso = math.dist(nos[a], nos[b])
        vizinhos[a].append((b, peso))
        vizinhos[b].append((a, peso))

    guarda = next(s for s in dados["structures"] if s["type"] == "Guard")
    origem = f"Guard {guarda['id']}"
    dist = {origem: 0.0}
    fila = [(0.0, origem)]
    while fila:
        d, no = heapq.heappop(fila)
        if d > dist[no]:
            continue
        for viz, peso in vizinhos[no]:
            if d + peso < dist.get(viz, math.inf):
                dist[viz] = d + peso
                heapq.heappush(fila, (d + peso, viz))

    def item(s):
        return dict(nome=f"{s['type']} {s['id']}", pos=(s["bottomCenter"]["x"], s["bottomCenter"]["y"]),
                    dist=dist[f"{s['type']} {s['id']}"], quarteirao=s.get("polygonId", ""))

    return dict(guarda=(guarda["bottomCenter"]["x"], guarda["bottomCenter"]["y"]),
                casas=[item(s) for s in dados["structures"] if s["type"] == "House"],
                armazens=sorted((item(s) for s in dados["structures"] if s["type"] == "Warehouse"),
                                key=lambda a: a["dist"]))
