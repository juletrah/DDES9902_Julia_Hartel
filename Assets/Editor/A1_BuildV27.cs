// A1_BuildV27.cs
// Menue:  Assets > AED Build V27   (laeuft einmalig auch von selbst)
//
// Drei Aenderungen am Patientenbereich:
//
// 1. Pad B klebt jetzt seitlich am Brustkorb statt oben drauf.
//    Der Rumpf (Patient_Body) steht bei x 0, Breite 0,5 - seine rechte
//    Flanke liegt also bei x 0,25. Die Zone wandert auf x 0,26 und wird
//    um 90 Grad um Z gedreht, damit die flache Seite des Pads an der
//    Flanke anliegt statt waagerecht in der Luft zu schweben.
//
// 2. Ein Knopf auf dem AED. Er haengt NICHT unter AED_Unit, sondern
//    unter A2_Patient_Area - sonst wuerde A2Flow.Freeze() beim Ablegen
//    auch seinen Collider abschalten und der Knopf waere tot.
//    Geklickt wird er ueber InteractableGeneral.onPrimaryInteract, also
//    ueber den EZPZ-Strahl. Unitys OnMouseDown funktioniert hier nicht,
//    weil der Spielercontroller den Mauszeiger sperrt.
//
// 3. Die neuen Felder am A2Flow werden verdrahtet.

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

public static class A1_BuildV27
{
    const string MENU = "Assets/AED Build V27";
    const string DIR  = "Assets/__My Project/";

    // Rumpf: Mitte x 0, Breite 0,5 -> Flanke bei 0,25. Pad ist 0,02 dick.
    static readonly Vector3 ZONE_B_SIDE = new Vector3(0.26f, 0.17f, 3.18f);
    static readonly Vector3 ZONE_B_EULER = new Vector3(0f, 0f, 90f);

    // AED_Body ist 0,42 x 0,26 x 0,34 und sitzt auf AED_Target (y 0,13).
    // Oberkante also y 0,26 - der Knopf liegt knapp darueber.
    static readonly Vector3 BUTTON_AT = new Vector3(0.85f, 0.29f, 3.35f);
    static readonly Vector3 BUTTON_SIZE = new Vector3(0.16f, 0.05f, 0.12f);

    static Material matBase;

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V27_done_b";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 1)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V27");

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null) { Debug.LogError("[V27] Kein Referenzmaterial - abgebrochen."); return; }

        GameObject area = Find("A2_Patient_Area");
        if (area == null) { Debug.LogError("[V27] A2_Patient_Area fehlt - abgebrochen."); return; }

        A2Flow flow = area.GetComponent<A2Flow>();
        if (flow == null) { Debug.LogError("[V27] A2Flow fehlt - abgebrochen."); return; }

        // -- 1 Pad B an die Flanke ---------------------------------------
        GameObject zoneB = Find("Zone_Pad_B_SE");
        if (zoneB != null)
        {
            Undo.RecordObject(zoneB.transform, "Zone B");
            zoneB.transform.position = ZONE_B_SIDE;
            zoneB.transform.rotation = Quaternion.Euler(ZONE_B_EULER);
            Debug.Log("[V27] Pad B liegt jetzt seitlich bei x " +
                      ZONE_B_SIDE.x.ToString("0.00") + ", y " + ZONE_B_SIDE.y.ToString("0.00") +
                      ", um 90 Grad gekippt.");
        }
        else Debug.LogWarning("[V27] Zone_Pad_B_SE nicht gefunden.");

        // -- 2 Knopf -----------------------------------------------------
        GameObject old = Find("AED_Button");
        if (old != null) Undo.DestroyObjectImmediate(old);

        Material matOff = Mat("Mat_Button_Off", new Color(0.35f, 0.07f, 0.07f), Color.black);
        Material matOn  = Mat("Mat_Button_On",  new Color(0.90f, 0.12f, 0.12f), new Color(0.80f, 0.05f, 0.05f));

        GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btn.name = "AED_Button";
        Undo.RegisterCreatedObjectUndo(btn, "AED_Button");
        btn.transform.SetParent(area.transform, true);
        btn.transform.position = BUTTON_AT;
        btn.transform.rotation = Quaternion.identity;
        btn.transform.localScale = BUTTON_SIZE;

        Renderer br = btn.GetComponent<Renderer>();
        br.sharedMaterial = matOff;

        // Collider bleibt - ueber ihn findet der Strahl den Knopf.
        InteractableGeneral inter = btn.AddComponent<InteractableGeneral>();
        inter.hoverText = "PRESS";

        // Bei einer gerade per AddComponent angelegten Komponente sind die
        // UnityEvent-Felder noch null - Unity fuellt sie erst beim
        // Deserialisieren. Ohne diese Zeile faellt AddPersistentListener um.
        if (inter.onPrimaryInteract == null) inter.onPrimaryInteract = new UnityEvent();

        UnityAction call = flow.OnAedClicked;
        UnityEventTools.AddPersistentListener(inter.onPrimaryInteract, call);

        btn.SetActive(false);   // erst beim Schockschritt sichtbar

        // -- 3 A2Flow verdrahten -----------------------------------------
        Undo.RecordObject(flow, "A2Flow");
        flow.aedButton = btn;
        flow.aedButtonRenderer = br;
        flow.matButtonOff = matOff;
        flow.matButtonOn = matOn;
        EditorUtility.SetDirty(flow);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V27] Fertig. Knopf auf dem AED bei y " + BUTTON_AT.y.ToString("0.00") +
                  ", erscheint erst wenn beide Pads sitzen. Szene speichern mit Ctrl+S.");
    }

    // ---------------------------------------------------------- Helfer

    static Material Mat(string assetName, Color c, Color emission)
    {
        string path = DIR + assetName + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(matBase);
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[V27] Material '" + path + "' angelegt.");
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_EmissionColor"))
        {
            m.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0.01f) m.EnableKeyword("_EMISSION");
            else m.DisableKeyword("_EMISSION");
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
