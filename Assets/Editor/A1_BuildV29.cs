// A1_BuildV29.cs
// Menue:  Assets > AED Build V29   (laeuft einmalig auch von selbst)
//
// Pads: 'dropOnKeyLift' aus.
//
// Mit dem Haken an faellt das Pad, sobald man die Maustaste loslaesst -
// man muesste sie also die ganze Zeit gedrueckt halten. Ohne den Haken
// nimmt der erste Klick das Pad auf, es bleibt in der Hand, und der
// zweite Klick legt es ab. So ist es gewuenscht.
//
// Nebenbei: genau dieser Haken hat heute den Interactor abstuerzen
// lassen, weil OnFireLift nach DropHoldable() weiter auf das eigene,
// inzwischen genullte Feld zugreift. Die Stelle ist in
// RaycastInteractor.cs abgesichert, der Haken ist jetzt zusaetzlich weg.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV29
{
    const string MENU = "Assets/AED Build V29";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V29_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 3)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V29");

        int n = 0;
        n += Set("Pad_A");
        n += Set("Pad_B");
        n += Set("Pad_1");
        n += Set("Pad_2");

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V29] Fertig. " + n + " Pad(s) auf Klick-Aufnehmen / Klick-Ablegen " +
                  "umgestellt. Szene speichern mit Ctrl+S.");
    }

    static int Set(string name)
    {
        GameObject g = Find(name);
        if (g == null) return 0;

        Holdable h = g.GetComponent<Holdable>();
        if (h == null) return 0;

        Undo.RecordObject(h, "Holdable");
        h.dropOnKeyLift = false;
        EditorUtility.SetDirty(h);
        Debug.Log("[V29] '" + name + "': dropOnKeyLift aus.");
        return 1;
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
