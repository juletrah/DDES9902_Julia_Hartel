// A1_BuildV34.cs
// Menue:  Assets > AED Build V34   (laeuft einmalig auch von selbst)
//
// "TAKE AED" wird eine feste Beschriftung am Klickfeld, statt nur beim
// Darueberfahren zu erscheinen. Grund: die Beschriftung des Notrufs ist
// dauerhaft sichtbar, und eine Hilfe, die man erst findet, wenn man
// schon weiss wohin man zeigen muss, hilft genau dann nicht, wenn man
// sie braucht.
//
// Ausrichtung: TextMeshPro ist von seiner MINUS-z-Seite lesbar. Der
// Schrank steht an der Westwand, die Testperson schaut also von Osten
// darauf - die Schrift wird um 270 Grad gedreht, damit ihre lesbare
// Seite nach Osten zeigt.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV34
{
    const string MENU = "Assets/AED Build V34";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V34_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 8)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V34");

        GameObject grab = Find("AED_Grab");
        if (grab == null) { Debug.LogError("[V34] AED_Grab fehlt - abgebrochen."); return; }

        GameObject old = Find("AED_Grab_Label");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject label = new GameObject("AED_Grab_Label");
        Undo.RegisterCreatedObjectUndo(label, "AED_Grab_Label");
        label.transform.SetParent(grab.transform, false);

        // Der Wuerfel ist ungleichmaessig skaliert (0,10 x 0,45 x 0,55).
        // Das wuerde die Schrift verzerren, deshalb wird die Skalierung
        // des Elternobjekts hier wieder herausgerechnet.
        Vector3 p = grab.transform.lossyScale;
        label.transform.localScale = new Vector3(1f / p.x, 1f / p.y, 1f / p.z);
        label.transform.localPosition = new Vector3(0.75f, 0f, 0f);   // knapp vor der Ostflaeche
        label.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

        TextMeshPro t = label.AddComponent<TextMeshPro>();
        t.text = "TAKE AED";
        t.fontSize = 2.2f;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;

        RectTransform rt = label.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(2.2f, 0.5f);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V34] Fertig. 'TAKE AED' steht jetzt dauerhaft am Klickfeld. " +
                  "Szene speichern mit Ctrl+S.");
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
