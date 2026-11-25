// A1_BuildV16.cs
// Menue:  Tools > AED Prototype > Build V16
//
// Macht aus dem massiven Schrankwuerfel ein offenes Wandfach.
//
// V15 hat den Schrank gross genug gemacht und den AED hineingesetzt -
// nur ist 'AED_Cabinet' ein geschlossener Wuerfel, das Geraet war damit
// unsichtbar. V16 ersetzt die sichtbare Huelle durch fuenf duenne
// Platten (Rueckwand, Boden, Decke, zwei Seiten) und laesst die Vorderseite
// offen, sodass der AED im Fach steht und anklickbar bleibt.
//
// Der Collider des alten Wuerfels wird abgeschaltet: er haette sonst
// jeden Klick auf das Geraet abgefangen.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV16
{
    const string MENU = "Tools/AED Prototype/Build V16";
    const string FRAME = "AED_Cabinet_Frame";

    const float T = 0.04f;   // Plattenstaerke

    // Innenmasse des Fachs, Vorderseite offen Richtung Raum (+x).
    const float X_BACK  = -7.90f;
    const float X_FRONT = -7.35f;
    const float Y_LOW   =  0.925f;
    const float Y_HIGH  =  1.675f;
    const float Z_LOW   = -4.325f;
    const float Z_HIGH  = -3.675f;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V16");

        GameObject cab = Find("AED_Cabinet");
        if (cab == null)
        {
            Debug.LogError("[V16] 'AED_Cabinet' nicht gefunden - abgebrochen.");
            return;
        }

        Material mat = null;
        Renderer cabRend = cab.GetComponent<Renderer>();
        if (cabRend != null)
        {
            mat = cabRend.sharedMaterial;
            Undo.RecordObject(cabRend, "cabinet renderer");
            cabRend.enabled = false;
            Debug.Log("[V16] Sichtbare Huelle von 'AED_Cabinet' abgeschaltet.");
        }

        foreach (Collider c in cab.GetComponents<Collider>())
        {
            Undo.RecordObject(c, "cabinet collider");
            c.enabled = false;
            Debug.Log("[V16] Collider von 'AED_Cabinet' abgeschaltet - " +
                      "sonst faengt er jeden Klick auf den AED ab.");
        }

        GameObject frame = Find(FRAME);
        if (frame == null)
        {
            frame = new GameObject(FRAME);
            Undo.RegisterCreatedObjectUndo(frame, FRAME);
            GameObject parent = Find("====A1 CHANGES====");
            if (parent != null) frame.transform.SetParent(parent.transform, true);
            Debug.Log("[V16] Objekt '" + FRAME + "' angelegt.");
        }
        frame.transform.position = Vector3.zero;
        frame.transform.rotation = Quaternion.identity;
        frame.transform.localScale = Vector3.one;

        float w = X_FRONT - X_BACK;            // Tiefe des Fachs
        float h = Y_HIGH - Y_LOW;              // Hoehe
        float d = Z_HIGH - Z_LOW;              // Breite
        float cx = (X_BACK + X_FRONT) / 2f;
        float cy = (Y_LOW + Y_HIGH) / 2f;
        float cz = (Z_LOW + Z_HIGH) / 2f;

        Panel(frame, mat, "Cab_Back",   new Vector3(X_BACK + T / 2f, cy, cz), new Vector3(T, h, d));
        Panel(frame, mat, "Cab_Bottom", new Vector3(cx, Y_LOW + T / 2f, cz),  new Vector3(w, T, d));
        Panel(frame, mat, "Cab_Top",    new Vector3(cx, Y_HIGH - T / 2f, cz), new Vector3(w, T, d));
        Panel(frame, mat, "Cab_Side_N", new Vector3(cx, cy, Z_LOW + T / 2f),  new Vector3(w, h, T));
        Panel(frame, mat, "Cab_Side_S", new Vector3(cx, cy, Z_HIGH - T / 2f), new Vector3(w, h, T));

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V16] Fertig. Offenes Fach mit 5 Platten, Vorderseite zum Raum. " +
                  "Szene speichern mit Ctrl+S.");
    }

    static void Panel(GameObject parent, Material mat, string name, Vector3 pos, Vector3 scale)
    {
        GameObject g = null;
        foreach (Transform t in parent.transform)
            if (t.name == name) { g = t.gameObject; break; }

        if (g == null)
        {
            g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObjectUndo(g, name);
        }

        Undo.RecordObject(g.transform, name);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.identity;
        g.transform.localScale = scale;

        if (mat != null)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r != null) { Undo.RecordObject(r, "mat"); r.sharedMaterial = mat; }
        }
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
