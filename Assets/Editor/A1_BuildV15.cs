// A1_BuildV15.cs
// Menue:  Tools > AED Prototype > Build V15
//
// Der AED haengt zwar schon auf 1,20 m an der Westwand, steht dort aber
// NEBEN seinem Schrank statt darin: der Schrank ist mit 0,25 m Tiefe
// flacher als das Geraet mit 0,42 m. Aus der Spielerperspektive sieht
// das aus, als stuende das Geraet frei im Raum.
//
// V15 macht aus dem Schrank ein echtes Wandfach, in das der AED
// hineinpasst, und setzt das Geraet mittig hinein.
//
// Ausserdem: Pruefung des Ablege-Ankers neben dem Patienten. Der liegt
// derzeit auf 2,22 m Hoehe - dort wuerde der AED nach dem Ablegen in
// der Luft schweben.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV15
{
    const string MENU = "Tools/AED Prototype/Build V15";

    // Westwand: Innenseite liegt bei x = -7,90.
    static readonly Vector3 CAB_POS   = new Vector3(-7.63f, 1.30f, -4.00f);
    static readonly Vector3 CAB_SCALE = new Vector3(0.55f, 0.75f, 0.65f);

    // Mittig im Fach.
    static readonly Vector3 AED_POS   = new Vector3(-7.63f, 1.30f, -4.00f);

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V15");

        GameObject cab  = Find("AED_Cabinet");
        GameObject unit = Find("AED_Unit");

        if (cab == null || unit == null)
        {
            Debug.LogError("[V15] 'AED_Cabinet' oder 'AED_Unit' nicht gefunden - abgebrochen.");
            return;
        }

        Vector3 cabBefore = cab.transform.localScale;
        Undo.RecordObject(cab.transform, "cabinet");
        cab.transform.position = CAB_POS;
        cab.transform.localScale = CAB_SCALE;
        Debug.Log(string.Format(
            "[V15] Wandfach: Tiefe {0:0.00} -> {1:0.00} m, damit das Geraet (0,42 m) hineinpasst. " +
            "Rueckwand buendig an der Westwand bei x -7,90.",
            cabBefore.x, CAB_SCALE.x));

        Vector3 aedBefore = unit.transform.position;
        Undo.RecordObject(unit.transform, "aed");
        unit.transform.position = AED_POS;
        Debug.Log(string.Format(
            "[V15] AED: ({0:0.00} | {1:0.00} | {2:0.00}) -> ({3:0.00} | {4:0.00} | {5:0.00}), " +
            "jetzt mittig im Fach.",
            aedBefore.x, aedBefore.y, aedBefore.z, AED_POS.x, AED_POS.y, AED_POS.z));

        // ------------------------------------------- Ablege-Anker pruefen
        GameObject anchor = Find("Anchor_AED");
        if (anchor != null)
        {
            float y = anchor.transform.position.y;
            if (y > 0.6f)
                Debug.LogWarning(string.Format(
                    "[V15] 'Anchor_AED' liegt auf y {0:0.00}. Dort landet der AED nach dem " +
                    "Ablegen neben dem Patienten - also frei schwebend. Sinnvoll waeren " +
                    "etwa 0,10 bis 0,15 m, also auf dem Boden neben dem Koerper. " +
                    "Nicht automatisch geaendert, weil die Stelle zu deinem Raumaufbau passen muss.",
                    y));
            else
                Debug.Log(string.Format("[V15] 'Anchor_AED' auf y {0:0.00} - plausibel.", y));
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V15] Fertig. Szene speichern mit Ctrl+S.");
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
