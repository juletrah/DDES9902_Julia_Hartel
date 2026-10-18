// A1_BuildV7.cs
// Menue:  Tools > AED Prototype > Build V7
//
//   1 Pad-Stellen deutlich groesser und hoeher anklickbar
//   2 Notruf-Rueckmeldung NUR beim Telefon, nicht zusaetzlich am AED
//   3 keine schwarze Tafel hinter der Notruf-Meldung

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

public static class A1_BuildV7
{
    const string MENU = "Tools/AED Prototype/Build V7";
    const string MSG_CALL = "HELP IS ON THE WAY. GET AED NOW.";

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V7");

        // ------------------------------------------- 1 Pad-Stellen vergroessern
        string[] spots = { "Spot_Correct_A", "Spot_Correct_B", "Spot_Wrong_Low",
                           "Spot_Wrong_Mid", "Spot_Wrong_High" };
        foreach (var n in spots)
        {
            var go = Find(n);
            if (go == null) continue;

            Undo.RecordObject(go.transform, n);
            go.transform.localScale = new Vector3(0.26f, 0.012f, 0.30f);

            var bc = go.GetComponent<BoxCollider>();
            if (bc != null)
            {
                Undo.RecordObject(bc, n + " collider");
                // Sehr hoher Trefferbereich: der Mausstrahl findet die Stelle
                // auch aus schraeger Haltung weit ueber der Brust.
                bc.size = new Vector3(1.1f, 30f, 1.1f);
                bc.center = new Vector3(0f, 10f, 0f);
            }
        }

        // ------------------------------------- 2 schwarze Tafel beim Telefon weg
        var plate = Find("Call_Feedback_Plate");
        if (plate != null)
        {
            Undo.RecordObject(plate, "plate aus");
            plate.SetActive(false);
        }

        // Meldung etwas groesser, damit sie ohne Tafel gut lesbar ist.
        var fbGo = Find("Call_Feedback_Text");
        if (fbGo != null)
        {
            var tmp = fbGo.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "fb text");
                tmp.text = MSG_CALL;
                tmp.fontSize = 0.34f;
                tmp.color = new Color(0.15f, 0.85f, 0.35f);
                Record(tmp);
            }
        }

        // ---------------------------- 3 Notruf schreibt nicht mehr aufs Display
        var call = Find("Button_Call_000");
        var unit = Find("AED_Unit");
        var disp = unit != null ? unit.GetComponent<AEDDisplay>() : null;

        if (call != null && disp != null)
        {
            foreach (var ig in call.GetComponentsInChildren<InteractableGeneral>(true))
            {
                Undo.RecordObject(ig, "call");
                Clear(ig.onPrimaryInteract);

                // Uhr starten, Meldung beim Telefon zeigen, Knopf ausblenden.
                UnityEventTools.AddVoidPersistentListener(ig.onPrimaryInteract,
                    new UnityAction(disp.StartClock));
                if (fbGo != null) AddSetActive(ig.onPrimaryInteract, fbGo, true);
                foreach (var part in new[] { "Graphics", "TMPCanvas", "InteractionBox", "Text (TMP)" })
                {
                    var child = FindRecursive(call.transform, part);
                    if (child != null) AddSetActive(ig.onPrimaryInteract, child, false);
                }
                // Bewusst KEIN disp.Show hier: der AED soll noch nichts sagen.
                Record(ig);
            }
        }

        Undo.CollapseUndoOperations(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[A1] Build V7 fertig.");
    }

    // ============================================================ Hilfsmittel

    static void Clear(UnityEventBase evt)
    {
        if (evt == null) return;
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(evt, i);
    }

    static void AddSetActive(UnityEvent evt, GameObject go, bool state)
    {
        if (evt == null || go == null) return;
        UnityEventTools.AddBoolPersistentListener(evt, new UnityAction<bool>(go.SetActive), state);
    }

    static void Record(Object o)
    {
        EditorUtility.SetDirty(o);
        PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }

    static GameObject Find(string n)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var r in scene.GetRootGameObjects())
        {
            var hit = FindRecursive(r.transform, n);
            if (hit != null) return hit;
        }
        return null;
    }

    static GameObject FindRecursive(Transform t, string n)
    {
        if (t.name == n) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindRecursive(t.GetChild(i), n);
            if (hit != null) return hit;
        }
        return null;
    }
}
