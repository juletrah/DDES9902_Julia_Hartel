// A1_BuildV45.cs
// Menue:  Assets > AED Build V45   (laeuft einmalig auch von selbst)
//
// Der gruene START-Knopf im Raum wird entfernt.
//
// Er stammt aus v3, wo er den Durchlauf startete. Seit v4 uebernimmt das
// die Leertaste auf dem Start-Overlay, und zwei Wege in denselben Zustand
// sind einer zu viel: Der Knopf stand noch im Raum, waehrend die Uhr
// laengst lief, und las sich wie ein zweiter Start.
//
// Das Feld A2Flow.startButton bleibt im Code bestehen und wird nur
// geleert. Entfernen wuerde alle anderen Zuweisungen im Inspector
// durcheinanderbringen, und der Startweg ueber den Notruf bleibt
// ohnehin als Rueckfallebene erhalten.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV45
{
    const string MENU = "Assets/AED Build V45";

    // Der Knopf wurde ueber mehrere Versionen umgebaut, deshalb mehrere
    // moegliche Namen.
    static readonly string[] CANDIDATES =
    {
        "Start_Sign", "Button_Start", "Start_Button", "Btn_Start", "Start"
    };

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V45_done";
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
        GameObject area = Find("A2_Patient_Area");
        A2Flow flow = area != null ? area.GetComponent<A2Flow>() : null;

        int removed = 0;

        // Zuerst das Objekt, auf das A2Flow tatsaechlich zeigt - das ist
        // der verlaessliche Weg, unabhaengig vom Namen.
        if (flow != null && flow.startButton != null)
        {
            Undo.RecordObject(flow, "startButton leeren");
            Debug.Log("[V45] Entferne verknuepftes Objekt: " + flow.startButton.name);
            Undo.DestroyObjectImmediate(flow.startButton);
            flow.startButton = null;
            EditorUtility.SetDirty(flow);
            removed++;
        }
        else Debug.Log("[V45] A2Flow.startButton war bereits leer.");

        // Danach noch nach Namen suchen, falls eine aeltere Version ein
        // zweites Exemplar hinterlassen hat.
        foreach (string n in CANDIDATES)
        {
            GameObject go = Find(n);
            if (go == null) continue;
            Debug.Log("[V45] Entferne zusaetzlich gefundenes Objekt: " + n);
            Undo.DestroyObjectImmediate(go);
            removed++;
        }

        if (flow != null)
        {
            Undo.RecordObject(flow, "startButton leeren");
            flow.startButton = null;
            EditorUtility.SetDirty(flow);
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V45] Fertig, " + removed + " Objekt(e) entfernt. " +
                  "Start laeuft jetzt ausschliesslich ueber die Leertaste. " +
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
