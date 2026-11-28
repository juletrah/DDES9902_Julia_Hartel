// A1_BuildV48.cs
// Menue:  Assets > AED Build V48   (laeuft einmalig auch von selbst)
//
// Warum die Farben nicht ankamen: TextMeshPro hat den Schalter
// overrideColorTags. Steht er auf true, verarbeitet die Komponente
// weiterhin <size> und <b>, ersetzt aber jeden <color>-Tag durch die
// Grundfarbe des Feldes. Genau dieses Bild hatten wir - Ueberschriften
// wurden groesser und fett, blieben aber weiss.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV48
{
    const string MENU = "Assets/AED Build V48";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V48_done";
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
        if (panel == null) { Debug.LogError("[V48] Intro_Panel fehlt."); return; }

        TextMeshProUGUI t = panel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (t == null) { Debug.LogError("[V48] Kein TextMeshProUGUI gefunden."); return; }

        Undo.RecordObject(t, "Farb-Tags zulassen");

        Debug.Log("[V48] overrideColorTags vorher: " + t.overrideColorTags +
                  ", Verlauf: " + t.enableVertexGradient +
                  ", richText: " + t.richText);

        t.overrideColorTags = false;   // der eigentliche Schalter
        t.enableVertexGradient = false;
        t.richText = true;
        t.ForceMeshUpdate();

        EditorUtility.SetDirty(t);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V48] Farb-Tags sind jetzt zugelassen. Szene speichern mit Ctrl+S.");
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
