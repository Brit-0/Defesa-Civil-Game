#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds de entrega (Agente D). Os scripts de Tools/Build chamam em batchmode, com o Editor fechado:
//   Unity.exe -batchmode -quit -projectPath <pasta> -buildTarget Win64 -executeMethod BuildScript.BuildWindows
// Opcionais na linha de comando: -buildOutput <caminho>, -development, -webglNoFallback.
public static class BuildScript
{
    private const string GameplayScene = "Assets/Scenes/Gameplay.unity";
    private const string WindowsOutput = "Builds/Windows/PleaseFixTheCity.exe";
    private const string WebGLOutput = "Builds/WebGL";
    private const string OutputArg = "-buildOutput";
    private const string DevelopmentArg = "-development";
    private const string NoFallbackArg = "-webglNoFallback";
    private const string LogTag = "[BuildScript]";

    [MenuItem("Tools/Build/Windows (entrega)")]
    public static void BuildWindows()
    {
        bool ok = Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, ArgValue(OutputArg) ?? WindowsOutput);
        Finish(ok);
    }

    // O itch.io pode servir os .br sem o cabeçalho Content-Encoding, e aí a página trava no carregamento.
    // Com o fallback o loader descompacta sozinho, com ou sem cabeçalho. O valor do projeto volta depois do build.
    [MenuItem("Tools/Build/WebGL (entrega)")]
    public static void BuildWebGL()
    {
        bool projectFallback = PlayerSettings.WebGL.decompressionFallback;
        bool ok;
        try
        {
            PlayerSettings.WebGL.decompressionFallback = !HasArg(NoFallbackArg);
            Debug.Log($"{LogTag} WebGL: compressão {PlayerSettings.WebGL.compressionFormat}, fallback de descompressão {PlayerSettings.WebGL.decompressionFallback}");
            ok = Build(BuildTarget.WebGL, BuildTargetGroup.WebGL, ArgValue(OutputArg) ?? WebGLOutput);
        }
        finally
        {
            PlayerSettings.WebGL.decompressionFallback = projectFallback;
            AssetDatabase.SaveAssets();
        }
        Finish(ok);
    }

    private static bool Build(BuildTarget target, BuildTargetGroup group, string output)
    {
        string[] scenes = Scenes();
        if (scenes.Length == 0)
        {
            Debug.LogError($"{LogTag} FALHOU {target}: nenhuma cena para o build (esperava {GameplayScene})");
            return false;
        }
        if (!scenes.Contains(GameplayScene))
            Debug.LogWarning($"{LogTag} {GameplayScene} não está entre as cenas ativas do Build Settings");
        if (EditorUserBuildSettings.activeBuildTarget != target)
            Debug.LogWarning($"{LogTag} alvo ativo é {EditorUserBuildSettings.activeBuildTarget}; trocar para {target} reimporta o projeto (use -buildTarget na linha de comando)");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = target,
            targetGroup = group,
            options = HasArg(DevelopmentArg) ? BuildOptions.Development : BuildOptions.None,
        };

        Debug.Log($"{LogTag} começando {target} em {Path.GetFullPath(output)} ({options.options}); cenas: {string.Join(", ", scenes)}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        bool ok = summary.result == BuildResult.Succeeded;
        string line = $"{LogTag} {(ok ? "OK" : "FALHOU")} {target}: {summary.result}, {summary.totalErrors} erros, " +
                      $"{summary.totalWarnings} avisos, {summary.totalSize / (1024.0 * 1024.0):0.0} MB, {summary.totalTime.TotalSeconds:0} s";
        if (ok) Debug.Log(line);
        else Debug.LogError(line);
        return ok;
    }

    private static string[] Scenes()
    {
        string[] enabled = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
        if (enabled.Length > 0) return enabled;
        return File.Exists(GameplayScene) ? new[] { GameplayScene } : Array.Empty<string>();
    }

    private static void Finish(bool ok)
    {
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    private static bool HasArg(string name)
    {
        return Environment.GetCommandLineArgs().Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string ArgValue(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && !args[i + 1].StartsWith("-"))
                return args[i + 1];
        }
        return null;
    }
}
#endif
