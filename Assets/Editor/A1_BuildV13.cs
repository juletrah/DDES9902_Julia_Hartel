// A1_BuildV13.cs
// Menue:  Tools > AED Prototype > Build V13
//
// Schliesst die Luecke bei 'Snap_Pad_B'.
//
// Das Gegenstueck fuer Pad A ist vollstaendig eingerichtet: countsAsPad,
// Anker Anchor_Pad_A, und seit V11 das Auflegen auf Patient_Body. Bei
// Pad B fehlt beides, obwohl 'Anchor_Pad_B' in der Szene existiert und
// sonst von nichts benutzt wird. Ein Pad, das dort abgelegt wird, bleibt
// deshalb in der Luft stehen und wird nicht gezaehlt.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV13
{
    const string MENU = "Tools/AED Prototype/Build V13";

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V13");

        GameObject snapB  = Find("Snap_Pad_B");
        GameObject anchorB = Find("Anchor_Pad_B");
        GameObject body   = Find("Patient_Body");

        if (snapB == null || anchorB == null || body == null)
        {
            Debug.LogError("[V13] 'Snap_Pad_B', 'Anchor_Pad_B' oder 'Patient_Body' " +
                           "nicht gefunden - abgebrochen.");
            return;
        }

        // Zum Abgleich: wie ist die Seite A eingerichtet?
        A1Sequence seq = Object.FindAnyObjectByType<A1Sequence>();
        AEDDisplay disp = Object.FindAnyObjectByType<AEDDisplay>();

        int fixedCount = 0;

        foreach (A1SnapLock l in snapB.GetComponents<A1SnapLock>())
        {
            if (l.anchor != null || l.reject || l.countsAsPad)
            {
                Debug.Log("[V13] 'Snap_Pad_B' hat bereits eine eingerichtete " +
                          "A1SnapLock - unveraendert gelassen.");
                continue;
            }

            Undo.RecordObject(l, "snap pad b");
            l.anchor = anchorB.transform;
            l.countsAsPad = true;
            l.reject = false;
            l.settleOnSurface = true;
            l.surface = body.transform;
            if (l.sequence == null) l.sequence = seq;
            if (l.display == null) l.display = disp;
            EditorUtility.SetDirty(l);
            fixedCount++;

            Debug.Log("[V13] 'Snap_Pad_B' eingerichtet: anchor=Anchor_Pad_B, " +
                      "countsAsPad=true, legt auf Patient_Body auf." +
                      (l.source == null
                          ? " ACHTUNG: 'source' (InteractableTrigger) ist leer - " +
                            "ohne die wird Snap() nie ausgeloest."
                          : " source=" + l.source.name));
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V13] Fertig. " + fixedCount + " Komponente(n) eingerichtet. " +
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
