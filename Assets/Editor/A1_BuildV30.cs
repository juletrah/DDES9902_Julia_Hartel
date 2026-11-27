// A1_BuildV30.cs
// Menue:  Assets > AED Build V30   (laeuft einmalig auch von selbst)
//
// Pad-Zone B wandert hoeher an den Brustkorb - kurz unterhalb der Brust
// statt auf Huefthoehe, wie auf der Abbildung.
//
//   Patient_Body: Mitte z 3,40, Laenge 0,8  ->  z 3,00 (unten) bis 3,80
//   (Schulter). Die Brust liegt also bei etwa z 3,45..3,70; knapp
//   darunter heisst z 3,37.
//
//   Hoehe an der Flanke: der Rumpf reicht von y 0,00 bis 0,30, das Pad
//   ist nach der 90-Grad-Drehung 0,15 hoch. y 0,20 setzt es in die obere
//   Haelfte der Flanke.
//
// Die Drehung bleibt wie in V27: 90 Grad um Z, damit die flache Seite
// an der Koerperseite anliegt.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV30
{
    const string MENU = "Assets/AED Build V30";

    static readonly Vector3 ZONE_B_AT = new Vector3(0.26f, 0.20f, 3.37f);
    static readonly Vector3 ZONE_B_EULER = new Vector3(0f, 0f, 90f);

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V30_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 4)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V30");

        GameObject z = Find("Zone_Pad_B_SE");
        if (z == null) { Debug.LogWarning("[V30] Zone_Pad_B_SE nicht gefunden."); return; }

        Undo.RecordObject(z.transform, "Zone B");
        z.transform.position = ZONE_B_AT;
        z.transform.rotation = Quaternion.Euler(ZONE_B_EULER);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V30] Zone B jetzt bei " + ZONE_B_AT.ToString("0.00") +
                  " - knapp unter der Brust, seitlich. Szene speichern mit Ctrl+S.");
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
