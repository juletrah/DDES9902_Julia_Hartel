// A1_BuildV9.cs
// Menue:  Tools > AED Prototype > Build V9
//
//   1 Das Anweisungsdisplay des AED dreht sich immer zum Spieler
//     (A1Billboard auf AED_Panel_Pivot, nur um die Hochachse).
//   2 Die Schrift darauf wird deutlich groesser und damit aus
//     Steh-Entfernung lesbar.
//   3 Das AEDDisplay beschriftet ab jetzt alle drei Textflaechen, damit
//     die Anweisung sichtbar ist, egal welche Flaeche gerade zu sehen ist.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV9
{
    const string MENU = "Tools/AED Prototype/Build V9";

    // Weltmass, keine Punktgroesse: 0,26 war etwa daumennagelgross.
    const float FONT_SIZE = 0.5f;

    static readonly string[] PANEL_TEXTS =
    {
        "AED_Panel_Text",
        "AED_Panel_Text_L",
        "AED_Panel_Text_R",
    };

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V9");

        // ------------------------------------------- 1 Display zum Spieler drehen
        GameObject pivot = Find("AED_Panel_Pivot");
        if (pivot == null)
        {
            Debug.LogWarning("[V9] 'AED_Panel_Pivot' nicht gefunden - Billboard uebersprungen.");
        }
        else
        {
            A1Billboard bb = pivot.GetComponent<A1Billboard>();
            if (bb == null)
            {
                bb = Undo.AddComponent<A1Billboard>(pivot);
                Debug.Log("[V9] A1Billboard auf 'AED_Panel_Pivot' gesetzt.");
            }
            Undo.RecordObject(bb, "billboard");
            bb.yawOnly = true;
            EditorUtility.SetDirty(bb);
        }

        // ------------------------------------------- 2 Schrift groesser
        var texts = new List<TextMeshPro>();

        foreach (string n in PANEL_TEXTS)
        {
            GameObject g = Find(n);
            if (g == null) { Debug.LogWarning("[V9] '" + n + "' nicht gefunden."); continue; }

            TextMeshPro t = g.GetComponent<TextMeshPro>();
            if (t == null) { Debug.LogWarning("[V9] '" + n + "' hat keinen TextMeshPro."); continue; }

            Undo.RecordObject(t, "font " + n);
            float before = t.fontSize;
            t.enableAutoSizing = false;
            t.fontSize = FONT_SIZE;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = true;
            EditorUtility.SetDirty(t);

            texts.Add(t);
            Debug.Log("[V9] '" + n + "': Schriftgroesse " + before + " -> " + FONT_SIZE);
        }

        // ------------------------------------------- 3 Alle Flaechen beschriften
        AEDDisplay display = Object.FindAnyObjectByType<AEDDisplay>();
        if (display == null)
        {
            Debug.LogWarning("[V9] Kein AEDDisplay gefunden - Textziele nicht gesetzt.");
        }
        else if (texts.Count > 0)
        {
            Undo.RecordObject(display, "display targets");
            display.instructionTexts = texts.ToArray();
            EditorUtility.SetDirty(display);
            Debug.Log("[V9] AEDDisplay beschriftet jetzt " + texts.Count + " Flaeche(n).");
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V9] Fertig. Szene speichern mit Ctrl+S.");
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
