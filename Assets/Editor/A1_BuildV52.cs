// A1_BuildV52.cs
// Menue:  Assets > AED Build V52   (laeuft einmalig auch von selbst)
//
// Startzeile: "When you're ready, press SPACE to start".
// Die Einleitung uebernimmt damit die Formulierung der alten Wandtafel -
// und sie sagt ausdruecklich, dass niemand gedraengt wird.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV52
{
    const string MENU = "Assets/AED Build V52";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V52_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 7)]
    public static void Build()
    {
        GameObject panel = Find("Intro_Panel");
        if (panel == null) { Debug.LogError("[V52] Intro_Panel fehlt."); return; }

        Transform cardT = panel.transform.Find("Card");
        if (cardT == null) { Debug.LogError("[V52] Card fehlt."); return; }

        Transform startT = cardT.Find("Text_Start");
        if (startT == null) { Debug.LogError("[V52] Text_Start fehlt."); return; }

        TextMeshProUGUI t = startT.GetComponent<TextMeshProUGUI>();
        if (t == null) { Debug.LogError("[V52] Kein TextMeshProUGUI an Text_Start."); return; }

        Undo.RecordObject(t, "Startzeile");
        t.text = "When you're ready, press SPACE to start";
        // Etwas kleiner, damit die laengere Zeile nicht umbricht.
        t.fontSize = 36f;
        t.ForceMeshUpdate();
        EditorUtility.SetDirty(t);

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V52] Startzeile geaendert. Szene speichern mit Ctrl+S.");
    }

    static GameObject Find(string name)
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name != name) continue;
            if (t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded)
                return t.gameObject;
        }
        return null;
    }
}
