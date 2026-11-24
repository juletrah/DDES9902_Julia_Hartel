// A1_BuildV11.cs
// Menue:  Tools > AED Prototype > Build V11
//
// Sorgt dafuer, dass Pads auf dem Koerper aufliegen, egal auf welchem
// der beiden Wege sie dort hinkommen:
//   - Klick auf eine Stelle          -> A1PadTarget
//   - Ablegen in ein Snap-Volumen    -> A1SnapLock mit countsAsPad
//
// Beide bekommen Patient_Body als Zielflaeche. Ohne diese Angabe sucht
// der Strahl die erste Flaeche unter dem Anker und trifft dabei leicht
// eines der unsichtbaren Klickvolumen, die im Raum uebereinanderliegen.
//
// Schreibt ausserdem die aktuelle Verdrahtung ins Log, damit sichtbar
// wird, an welcher Stelle ein Anker fehlt.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV11
{
    const string MENU = "Tools/AED Prototype/Build V11";

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V11");

        GameObject body = Find("Patient_Body");
        if (body == null)
        {
            Debug.LogError("[V11] 'Patient_Body' nicht gefunden - abgebrochen.");
            return;
        }

        int touched = 0;

        // ------------------------------------------------- Klick-Stellen
        foreach (A1PadTarget t in Resources.FindObjectsOfTypeAll<A1PadTarget>())
        {
            if (!InScene(t)) continue;

            Undo.RecordObject(t, "pad target");
            t.settleOnSurface = true;
            t.surface = body.transform;
            EditorUtility.SetDirty(t);
            touched++;

            Debug.Log("[V11] A1PadTarget '" + t.name + "': correct=" + t.correct +
                      ", anchor=" + (t.anchor != null ? t.anchor.name : "FEHLT") +
                      ", surface=Patient_Body");
        }

        // ------------------------------------------------- Snap-Volumen
        foreach (A1SnapLock l in Resources.FindObjectsOfTypeAll<A1SnapLock>())
        {
            if (!InScene(l)) continue;

            string note;
            if (l.countsAsPad && !l.reject)
            {
                Undo.RecordObject(l, "snap lock");
                l.settleOnSurface = true;
                l.surface = body.transform;
                EditorUtility.SetDirty(l);
                touched++;
                note = "-> legt jetzt auf Patient_Body auf";
            }
            else
            {
                note = l.reject ? "(Reject-Stelle, unveraendert)"
                                : "(kein Pad, unveraendert)";
            }

            Debug.Log("[V11] A1SnapLock '" + l.name + "': countsAsPad=" + l.countsAsPad +
                      ", reject=" + l.reject +
                      ", anchor=" + (l.anchor != null ? l.anchor.name : "FEHLT") +
                      " " + note);
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V11] Fertig. " + touched + " Komponente(n) angepasst. " +
                  "Szene speichern mit Ctrl+S.");
    }

    static bool InScene(Component c)
    {
        return c != null && c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded;
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
