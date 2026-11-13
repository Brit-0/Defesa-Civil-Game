using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    public static class Severity
    {
        public const string Error = "erro";
        public const string Warning = "aviso";

        static readonly HashSet<string> WarningRules = new HashSet<string> { "R09", "R10", "R21", "R32", "R33" };
        public static string Of(string ruleId) => WarningRules.Contains(ruleId) ? Warning : Error;
    }

    [Serializable]
    public class Violation
    {
        public string ruleId;
        public string severity;
        public string @object;
        public Vec2 position;
        public string message;
        public override string ToString() => $"[{ruleId}] {severity} {@object} {position}: {message}";
    }

    [Serializable]
    public class ValidationReport
    {
        public static readonly string DefaultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "reports", "validation_report.json"));

        public string mode = "";
        public string createdAt = "";
        public int errors;
        public int warnings;
        public List<string> notes = new List<string>();
        public List<Violation> violations = new List<Violation>();

        public void Add(string ruleId, string obj, Vector2 pos, string message)
        {
            violations.Add(new Violation { ruleId = ruleId, severity = Severity.Of(ruleId), @object = obj, position = pos, message = message });
        }

        public void Finish()
        {
            createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            violations = violations.OrderBy(v => v.ruleId, StringComparer.Ordinal).ThenBy(v => v.@object, StringComparer.Ordinal).ToList();
            errors = violations.Count(v => v.severity == Severity.Error);
            warnings = violations.Count(v => v.severity == Severity.Warning);
        }

        public bool Has(string ruleId) => violations.Any(v => v.ruleId == ruleId);

        public void Write(string path = null)
        {
            path ??= DefaultPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(this, true));
        }

        public void LogToConsole()
        {
            foreach (var v in violations)
            {
                if (v.severity == Severity.Error) Debug.LogError("[CityGen] " + v);
                else Debug.LogWarning("[CityGen] " + v);
            }
            foreach (var n in notes) Debug.Log("[CityGen] nota: " + n);
            Debug.Log($"[CityGen] Validate ({mode}): {errors} erro(s), {warnings} aviso(s).");
        }
    }
}
