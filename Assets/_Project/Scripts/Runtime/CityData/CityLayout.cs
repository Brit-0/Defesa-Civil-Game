using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Raiz do city_layout.json. Coordenadas em unidades de mundo (u), XY.
    /// Versão 2: bottomCenter é o centro da BASE VISÍVEL do sprite (sem a margem transparente).
    /// Versão 3: estruturas com orientação (facing S/N), waypoints de cruzamento (junctions) e ligações (links).
    /// </summary>
    [Serializable]
    public class CityLayout
    {
        public const int CurrentVersion = 3;

        public int version = CurrentVersion;
        public int seed;
        public string generatedAt = "";
        public float streetWidth = 0.8f;
        /// <summary>Faixa de rua entre a caixa dos polígonos e o Limiter (rua de contorno). 0 = Limiter colado na caixa.</summary>
        public float limiterMargin;
        /// <summary>Posição inicial da Main Camera no momento do Generate (centro da cidade, R34).</summary>
        public Vec2 center;
        public List<PolygonData> polygons = new List<PolygonData>();
        public List<StructureData> structures = new List<StructureData>();
        public List<DecorationData> decorations = new List<DecorationData>();
        /// <summary>Waypoints de cruzamento das ruas (além dos de entrada das estruturas).</summary>
        public List<WaypointNode> junctions = new List<WaypointNode>();
        /// <summary>Ligações entre waypoints vizinhos na mesma rua. Chaves: "House 012" (entrada) ou "Junction 003".</summary>
        public List<WaypointLink> links = new List<WaypointLink>();

        public string ToJson() => JsonUtility.ToJson(this, true);

        public static CityLayout FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON vazio.");
            var layout = JsonUtility.FromJson<CityLayout>(json);
            if (layout == null) throw new ArgumentException("JSON inválido.");
            if (layout.version != CurrentVersion) throw new ArgumentException($"Versão {layout.version} não suportada (esperado {CurrentVersion}).");
            if (!json.Contains("\"seed\"")) throw new ArgumentException("Campo 'seed' ausente (R08).");
            return layout;
        }
    }

    [Serializable]
    public struct Vec2
    {
        public float x, y;
        public Vec2(float x, float y) { this.x = x; this.y = y; }
        public static implicit operator Vector2(Vec2 v) => new Vector2(v.x, v.y);
        public static implicit operator Vec2(Vector2 v) => new Vec2(v.x, v.y);
        public override string ToString() => $"({x:0.###}, {y:0.###})";
    }

    public static class PolygonKind
    {
        public const string Block = "Block";
        public const string Plaza = "Plaza";
    }

    public static class District
    {
        public const string Suburban = "Suburban";
        public const string Commercial = "Commercial";
        public const string Industrial = "Industrial";
        public const string None = "None";
    }

    public static class Facing
    {
        public const string South = "S";
        public const string North = "N";
        public static string Of(string f) => f == North ? North : South;
    }

    [Serializable]
    public class WaypointNode
    {
        public string id = "";
        public Vec2 position;
    }

    [Serializable]
    public class WaypointLink
    {
        public string a = "", b = "";
    }

    public static class StructureType
    {
        public const string House = "House";
        public const string Guard = "Guard";
        public const string Warehouse = "Warehouse";
    }

    [Serializable]
    public class SpriteRef
    {
        public string guid = "";
        public string name = "";
        /// <summary>Só para leitura humana.</summary>
        public string path = "";
    }

    [Serializable]
    public class PolygonData
    {
        public string id = "";
        public string kind = PolygonKind.Block;
        public string district = District.None;
        public List<Vec2> points = new List<Vec2>();
    }

    [Serializable]
    public class StructureData
    {
        public string id = "";
        public string type = StructureType.House;
        /// <summary>Para onde a fachada aponta: S (fileira de baixo, rua ao sul) ou N (fileira de cima, rua ao norte).</summary>
        public string facing = Facing.South;
        public string district = District.None;
        public string polygonId = "";
        public SpriteRef sprite = new SpriteRef();
        public Vec2 bottomCenter;
        public Vec2 entry;
    }

    [Serializable]
    public class DecorationData
    {
        public string id = "";
        public string polygonId = "";
        public SpriteRef sprite = new SpriteRef();
        public Vec2 bottomCenter;
    }
}
