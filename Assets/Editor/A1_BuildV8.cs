// A1_BuildV8.cs
// Menue:  Tools > AED Prototype > Build V8
//
//   1 Das Display des AED (Panel und Bildschirmflaechen) erscheint erst,
//     wenn der AED neben dem Patienten abgelegt wurde. Vorher traegt man
//     ein Geraet, keinen schwebenden Monitor.
//   2 Die Pads erscheinen ebenfalls erst in diesem Moment.
//   3 Sobald beides erscheint, sagt das Display, was zu tun ist.
//
// Die Aufgabentafel im Raum (Instructions_Plate / Instructions_Texts)
// bleibt von Anfang an sichtbar - sie ist die Briefing-Tafel, nicht das
// Geraetedisplay.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV8
{
    const string MENU = "Tools/AED Prototype/Build V8";

    const string MSG =
        "PLACE THE PADS ON THE RIGHT SPOTS ON THE BODY.\nTRY IT AND GET FEEDBACK.";

    // Alles, was erst nach dem Ablegen des AED erscheinen soll.
    static readonly string[] HIDE_UNTIL_PLACED =
    {
        "AED_Panel_Pivot",
        "AED_Panel",
        "AED_Screen",
        "AED_Screen_L",
        "AED_Screen_R",
        "AED_Screen_Top",
        "Pad_1",
        "Pad_2",
    };

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V8");

        GameObject snap = Find("Snap_AED");
        if (snap == null)
        {
            Debug.LogError("[V8] 'Snap_AED' nicht gefunden - abgebrochen.");
            return;
        }

        AEDDisplay display = Object.FindAnyObjectByType<AEDDisplay>();
        if (display == null)
            Debug.LogWarning("[V8] Kein AEDDisplay gefunden - Meldung wird nicht gesetzt.");

        GameObject host = Find("A1_Reveal");
        A1Reveal rev = host != null ? host.GetComponent<A1Reveal>() : null;

        // ------------------------------------- frueheren Lauf zurueckdrehen
        if (rev != null && rev.revealObjects != null)
        {
            foreach (GameObject g in rev.revealObjects)
            {
                if (g == null) continue;
                Undo.RecordObject(g, "show " + g.name);
                g.SetActive(true);
                Debug.Log("[V8] Wieder sichtbar gemacht: " + g.name);
            }
        }

        // ------------------------------------- neue Liste zusammenstellen
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (string n in HIDE_UNTIL_PLACED)
        {
            GameObject g = Find(n);
            if (g == null) { Debug.LogWarning("[V8] '" + n + "' nicht gefunden."); continue; }
            list.Add(g);
        }

        if (list.Count == 0)
        {
            Debug.LogError("[V8] Keines der Zielobjekte gefunden - abgebrochen.");
            return;
        }

        if (host == null)
        {
            host = new GameObject("A1_Reveal");
            GameObject parent = Find("====A1 CHANGES====");
            if (parent != null) host.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObjectUndo(host, "A1_Reveal");
            Debug.Log("[V8] Objekt 'A1_Reveal' angelegt.");
        }

        if (rev == null) rev = Undo.AddComponent<A1Reveal>(host);

        Undo.RecordObject(rev, "A1Reveal");
        rev.trigger = snap;
        rev.revealObjects = list.ToArray();
        rev.display = display;
        rev.messageOnReveal = MSG;
        rev.revealed = false;
        EditorUtility.SetDirty(rev);

        foreach (GameObject g in list)
        {
            Undo.RecordObject(g, "hide " + g.name);
            g.SetActive(false);
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V8] Fertig. Erscheint erst nach dem Ablegen des AED: " +
                  string.Join(", ", list.ConvertAll(g => g.name)) +
                  ". Szene speichern mit Ctrl+S.");
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
