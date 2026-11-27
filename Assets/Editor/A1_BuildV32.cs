// A1_BuildV32.cs
// Menue:  Assets > AED Build V32   (laeuft einmalig auch von selbst)
//
// Zwei Umhaengungen, beide noetig, damit die Messung stimmt:
//
// 1. Die Wanduhr. AEDDisplay zaehlt erst ab dem Notruf hoch - gewuenscht
//    ist aber ab Spielbeginn. Die Anzeigen wandern deshalb von
//    AEDDisplay.clockTexts nach A2Flow.clockTexts, und AEDDisplays Liste
//    wird geleert, damit nicht beide dieselben Textfelder beschreiben.
//    AEDDisplay.clockRunning bleibt als reines Signal "Notruf abgesetzt"
//    erhalten - daran erkennt A2Flow weiterhin den Anruf.
//
// 2. Das Abschluss-Panel. A1RunMetrics zeigte Striche und Nullen, weil es
//    noch aus A1Sequence und A1Reveal las. Die steuern den Ablauf nicht
//    mehr. Jetzt liest es aus A2Flow.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV32
{
    const string MENU = "Assets/AED Build V32";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V32_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 6)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V32");

        A2Flow flow = Object.FindFirstObjectByType<A2Flow>();
        if (flow == null) { Debug.LogError("[V32] A2Flow fehlt - abgebrochen."); return; }

        // -- 1 Wanduhr ---------------------------------------------------
        AEDDisplay clock = flow.clock;
        if (clock == null) clock = Object.FindFirstObjectByType<AEDDisplay>();

        if (clock != null && clock.clockTexts != null && clock.clockTexts.Length > 0)
        {
            TMP_Text[] moved = new TMP_Text[clock.clockTexts.Length];
            for (int i = 0; i < clock.clockTexts.Length; i++) moved[i] = clock.clockTexts[i];

            Undo.RecordObject(flow, "A2Flow");
            flow.clockTexts = moved;
            EditorUtility.SetDirty(flow);

            Undo.RecordObject(clock, "AEDDisplay");
            clock.clockTexts = new TextMeshPro[0];
            EditorUtility.SetDirty(clock);

            Debug.Log("[V32] " + moved.Length + " Uhr-Anzeige(n) an A2Flow uebergeben. " +
                      "Die Uhr laeuft jetzt ab Spielbeginn.");
        }
        else if (flow.clockTexts != null && flow.clockTexts.Length > 0)
        {
            Debug.Log("[V32] Uhr war schon umgehaengt, nichts zu tun.");
        }
        else
        {
            Debug.LogWarning("[V32] Keine Uhr-Anzeigen gefunden - Wanduhr bleibt wie sie war.");
        }

        // -- 2 Abschluss-Panel -------------------------------------------
        A1RunMetrics m = Object.FindFirstObjectByType<A1RunMetrics>();
        if (m != null)
        {
            Undo.RecordObject(m, "A1RunMetrics");
            m.flow = flow;
            EditorUtility.SetDirty(m);
            Debug.Log("[V32] Abschluss-Panel liest jetzt aus A2Flow.");
        }
        else Debug.LogWarning("[V32] A1RunMetrics nicht gefunden.");

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V32] Fertig. Szene speichern mit Ctrl+S.");
    }
}
