// A1_FixNavigation.cs
// Menue:  Tools > AED Prototype > Fix Navigation
//
// Setzt die Spielerfigur und die Kamera auf die Standardhaltung des
// EZPZ-Prefabs zurueck. Behebt:
//   1 Neigung der Spielerfigur (Pitch/Roll) -> Horizont schief, Laufrichtung kippt
//   2 Spielerfigur schwebt ueber dem Boden
//   3 Augenhoehe-Knoten minimal verschoben
//   4 verdrehte UI-Platte am Spieler
//
// Alles in einem Undo-Schritt: Ctrl+Z macht es komplett rueckgaengig.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_FixNavigation
{
    const string MENU = "Tools/AED Prototype/Fix Navigation";

    // Augenhoehe im Standard-Prefab
    static readonly Vector3 EYE = new Vector3(0f, 1.6f, 0f);

    [MenuItem(MENU)]
    public static void Fix()
    {
        GameObject root = GameObject.FindWithTag("Player");
        if (root == null) root = GameObject.Find("EZPZ Player Flat Screen WASD");
        if (root == null)
        {
            Debug.LogError("[FixNavigation] Spielerfigur nicht gefunden. " +
                           "Erwartet: Objekt mit Tag 'Player' oder Name " +
                           "'EZPZ Player Flat Screen WASD'.");
            return;
        }

        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fix Navigation");
        int changes = 0;

        // --------------------------------------------- 1 + 2  Wurzel der Figur
        Transform rt = root.transform;
        Undo.RecordObject(rt, "player root");

        Vector3 eul = rt.localEulerAngles;
        if (Mathf.Abs(Mathf.DeltaAngle(eul.x, 0f)) > 0.01f ||
            Mathf.Abs(Mathf.DeltaAngle(eul.z, 0f)) > 0.01f)
        {
            Debug.Log(string.Format(
                "[FixNavigation] Figur war geneigt: Pitch {0:0.00} Grad, Roll {1:0.00} Grad " +
                "-> auf 0 gesetzt, Blickrichtung (Yaw {2:0.0} Grad) bleibt.",
                Mathf.DeltaAngle(eul.x, 0f), Mathf.DeltaAngle(eul.z, 0f), eul.y));
            changes++;
        }
        rt.localRotation = Quaternion.Euler(0f, eul.y, 0f);

        Vector3 pos = rt.localPosition;
        if (Mathf.Abs(pos.y) > 0.001f)
        {
            Debug.Log(string.Format(
                "[FixNavigation] Figur stand {0:0.000} m ueber dem Boden -> auf y = 0 gesetzt.",
                pos.y));
            changes++;
        }
        rt.localPosition = new Vector3(pos.x, 0f, pos.z);

        // --------------------------------------------- 3  Augenhoehe-Knoten
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == rt) continue;
            if (t is RectTransform) continue;

            if (Vector3.Distance(t.localPosition, EYE) < 0.15f &&
                t.localPosition != EYE)
            {
                Undo.RecordObject(t, "eye node " + t.name);
                t.localPosition = EYE;
                t.localRotation = Quaternion.identity;
                Debug.Log("[FixNavigation] Augenhoehe zurueckgesetzt: " + t.name);
                changes++;
            }
        }

        // --------------------------------------------- 4  UI am Spieler gerade stellen
        foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (Quaternion.Angle(r.localRotation, Quaternion.identity) > 0.01f)
            {
                Undo.RecordObject(r, "ui " + r.name);
                r.localRotation = Quaternion.identity;
                Debug.Log("[FixNavigation] UI-Element gerade gestellt: " + r.name);
                changes++;
            }
        }

        // --------------------------------------------- Kamera waagerecht starten
        Camera cam = root.GetComponentInChildren<Camera>(true);
        if (cam != null &&
            Quaternion.Angle(cam.transform.localRotation, Quaternion.identity) > 0.01f)
        {
            Undo.RecordObject(cam.transform, "camera");
            cam.transform.localRotation = Quaternion.identity;
            Debug.Log("[FixNavigation] Kamera auf waagerechten Start gesetzt.");
            changes++;
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[FixNavigation] Fertig. " + changes + " Korrektur(en). " +
                  "Szene speichern mit Ctrl+S.");
    }
}
