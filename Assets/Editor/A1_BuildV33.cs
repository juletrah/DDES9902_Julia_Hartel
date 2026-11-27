// A1_BuildV33.cs
// Menue:  Assets > AED Build V33   (laeuft einmalig auch von selbst)
//
// Drei Aenderungen fuer v3, alle aus Testdaten abgeleitet:
//
// [s1] Zwei Testpersonen brauchten mehrere Anlaeufe, um die Pads zu
//      setzen. Die Abbildung aus v2 klaerte, WO sie hingehoeren, nicht
//      WIE man sie dorthin bekommt. Deshalb: Toleranz von 0,15 auf
//      0,25 m (in A2Flow.Start erzwungen) und die beiden Zielflaechen
//      werden als flache Markierungen direkt auf dem Koerper sichtbar.
//      Sie erscheinen mit den Pads und verschwinden einzeln, sobald ihre
//      Stelle belegt ist - Hilfe genau so lange, wie sie gebraucht wird.
//
// [s3] Notruf und AED liefen ueber verschiedene Gesten: Schild druecken
//      gegen Hingehen. Ein Klickfeld vor dem Schrank macht beides zum
//      selben Handgriff. Die Naehe-Aufnahme bleibt als stiller Ersatz.
//
// Die Uhr, die erst bei der ersten Eingabe startet [s2], steckt in
// A2Flow und braucht hier keine Szenenobjekte.

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

public static class A1_BuildV33
{
    const string MENU = "Assets/AED Build V33";
    const string DIR  = "Assets/__My Project/";

    // Pad ist 0,15 x 0,19 - die Markierung etwas groesser, damit sie als
    // Zielflaeche lesbar ist und nicht als Schatten des Pads.
    static readonly Vector3 MARK_SIZE = new Vector3(0.19f, 0.004f, 0.23f);

    // Vor dem Schrank an der Westwand (AED_Unit steht bei x -7,63).
    static readonly Vector3 GRAB_AT   = new Vector3(-7.40f, 1.12f, -4.00f);
    static readonly Vector3 GRAB_SIZE = new Vector3(0.10f, 0.45f, 0.55f);

    static Material matBase;

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V33_done";
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
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V33");

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null) { Debug.LogError("[V33] Kein Referenzmaterial - abgebrochen."); return; }

        GameObject area = Find("A2_Patient_Area");
        A2Flow flow = area != null ? area.GetComponent<A2Flow>() : null;
        if (flow == null) { Debug.LogError("[V33] A2Flow fehlt - abgebrochen."); return; }

        Material matMark = Mat("Mat_Zone_Mark", new Color(0.20f, 0.70f, 1.00f), new Color(0.10f, 0.35f, 0.55f));
        Material matGrab = Mat("Mat_Grab", new Color(0.20f, 0.80f, 0.35f), new Color(0.08f, 0.30f, 0.12f));

        // -- [s1] Zielflaechen -------------------------------------------
        flow.markerA = Marker(area, "Marker_Zone_A", flow.zoneA, matMark);
        flow.markerB = Marker(area, "Marker_Zone_B", flow.zoneB, matMark);

        // -- [s3] Klickfeld vor dem Schrank ------------------------------
        GameObject oldGrab = Find("AED_Grab");
        if (oldGrab != null) Undo.DestroyObjectImmediate(oldGrab);

        GameObject grab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grab.name = "AED_Grab";
        Undo.RegisterCreatedObjectUndo(grab, "AED_Grab");
        grab.transform.SetParent(area.transform, true);
        grab.transform.position = GRAB_AT;
        grab.transform.rotation = Quaternion.identity;
        grab.transform.localScale = GRAB_SIZE;

        Renderer gr = grab.GetComponent<Renderer>();
        if (gr != null) gr.sharedMaterial = matGrab;

        InteractableGeneral inter = grab.AddComponent<InteractableGeneral>();
        inter.hoverText = "TAKE AED";
        if (inter.onPrimaryInteract == null) inter.onPrimaryInteract = new UnityEvent();
        UnityEventTools.AddPersistentListener(inter.onPrimaryInteract, (UnityAction)flow.OnAedGrabClicked);

        flow.aedGrabButton = grab;

        Undo.RecordObject(flow, "A2Flow");
        EditorUtility.SetDirty(flow);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V33] Fertig. Zielflaechen auf dem Koerper, Klickfeld vor dem Schrank, " +
                  "Toleranz 0,25 m. Szene speichern mit Ctrl+S.");
    }

    static GameObject Marker(GameObject parent, string name, Transform zone, Material m)
    {
        GameObject old = Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old);

        if (zone == null) { Debug.LogWarning("[V33] Zone fuer " + name + " fehlt."); return null; }

        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.SetParent(parent.transform, true);
        g.transform.position = zone.position;
        g.transform.rotation = zone.rotation;
        g.transform.localScale = MARK_SIZE;

        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);   // darf das Pad nicht abfangen

        Renderer r = g.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = m;

        g.SetActive(false);
        Debug.Log("[V33] " + name + " bei " + zone.position.ToString("0.00") + ".");
        return g;
    }

    static Material Mat(string assetName, Color c, Color emission)
    {
        string path = DIR + assetName + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(matBase);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_EmissionColor"))
        {
            m.SetColor("_EmissionColor", emission);
            m.EnableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
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
