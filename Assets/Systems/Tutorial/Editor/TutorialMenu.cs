using UnityEditor;
using UnityEngine;

// Para testar o tutorial de novo: apaga o "já visto" do tutorial e das dicas de contexto.
public static class TutorialMenu
{
    [MenuItem("Tools/Jam/Reset Tutorial")]
    private static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(Tutorial.DoneKey);
        PlayerPrefs.DeleteKey(Tutorial.FireTipKey);
        PlayerPrefs.DeleteKey(Tutorial.StrongTipKey);
        PlayerPrefs.Save();
        Debug.Log("Tutorial: na próxima partida ele aparece de novo (dicas de contexto também).");
    }
}
