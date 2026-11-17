using UnityEditor;
using UnityEngine;

// Dia e noite (docs/jam/ENERGIA_DIA_NOITE.md): cria o que falta, sem mexer no que já existe.
//   Resources/Balance/DayNightData.asset                  — fases do ciclo e luzes da cidade
//   Systems/DayNight/Resources/DayNight/WindowGlowMAT.mat — material das janelas acesas (shader DefesaCivil/Window Glow)
public static class DayNightMenu
{
    private const string DataPath = "Assets/Resources/Balance/DayNightData.asset";
    private const string MaterialFolder = "Assets/Systems/DayNight/Resources/DayNight";
    private const string MaterialPath = MaterialFolder + "/WindowGlowMAT.mat";
    private const string ShaderName = "DefesaCivil/Window Glow";

    [MenuItem("Tools/Jam/Create Day Night Data")]
    public static void Create()
    {
        int created = 0;

        if (AssetDatabase.LoadAssetAtPath<DayNightData>(DataPath) == null)
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DayNightData>(), DataPath);
            created++;
        }

        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[DayNightMenu] Shader {ShaderName} não encontrado.");
            }
            else
            {
                if (!AssetDatabase.IsValidFolder("Assets/Systems/DayNight/Resources")) AssetDatabase.CreateFolder("Assets/Systems/DayNight", "Resources");
                if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Systems/DayNight/Resources", "DayNight");
                AssetDatabase.CreateAsset(new Material(shader) { name = "WindowGlowMAT" }, MaterialPath);
                created++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[DayNightMenu] {created} criado(s); o que já existia ficou como estava.");
    }
}
