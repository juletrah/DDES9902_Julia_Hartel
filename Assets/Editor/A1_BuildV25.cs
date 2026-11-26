// A1_BuildV25.cs
// Menue:  Tools > AED Prototype > Build V25
//
// Raeumt den gesamten Bereich um den Patienten ab und baut ihn neu:
// eine Anzeige, zwei Pads, zwei unsichtbare Zielzonen, ein Ablegepunkt
// fuer den AED und eine Sicherheitszone fuer den Schock.
//
// Weg kommen: beide alten Displays samt Textflaechen, alle Spot- und
// Reject-Volumen, saemtliche Anker, Snap-Punkte, Marker, Lampen und
// Zonen-Prefabs rund um den Patienten. Die hatten sich ueber mehrere
// Iterationen zu doppelten und teils unkonfigurierten Komponenten
// verwachsen, die sich gegenseitig den Klickstrahl weggenommen haben.
//
// Bleiben: Patient, AED-Geraet und Wandfach, Telefon und Notruf,
// Wanduhr, Aufgabentafel, Bueroeinrichtung.
//
// Die Logik steckt in A2Flow, nicht mehr in der Szene verteilt.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV25
{
    const string MENU = "Tools/AED Prototype/Build V25";
    const string ROOT = "A2_Patient_Area";
    const string DIR  = "Assets/__My Project/";

    // Patient: Rumpf x -0,25..0,25, z 3,00..3,80, Oberseite y 0,30
    static readonly Vector3 ZONE_A = new Vector3(-0.13f, 0.305f, 3.62f); // Nordwest, unter dem Schluesselbein
    static readonly Vector3 ZONE_B = new Vector3( 0.17f, 0.305f, 3.18f); // Suedost, seitlich unter der Brust
    static readonly Vector3 AED_AT = new Vector3( 0.85f, 0.13f,  3.35f);

    const float ZONE_RADIUS = 0.15f;
    const float CLEAR_DIST  = 2.00f;

    // Was aus frueheren Iterationen verschwindet.
    static readonly string[] REMOVE =
    {
        "Pad_1", "Pad_2",
        "Spot_Correct_A", "Spot_Correct_B",
        "Spot_Wrong_Low", "Spot_Wrong_Mid", "Spot_Wrong_High",
        "Anchor_Spot_Correct_A", "Anchor_Spot_Correct_B",
        "Anchor_Pad_A", "Anchor_Pad_B", "Anchor_AED",
        "Reject_TooLow", "Reject_TooCentral", "Reject_TooHigh",
        "Wrong_TooLow", "Wrong_TooCentral", "Wrong_TooHigh",
        "Snap_AED", "Snap_Pad_B", "s",
        "Marker_Pad_A", "Marker_Pad_B", "Marker_Chest",
        "Lamp_Pad_Good", "Lamp_Pad_TooLow", "Lamp_Shock_Done",
        "Zone_Chest", "Zone_Patient", "Zone_AED_Placed",
        "Zone_Pad_TooLow", "Zone_Access", "Zone_Safety",
        "Target_Pad_A", "Target_Pad_B",
        "A1_Reveal",
        "AED_Panel", "AED_Panel_Pivot", "AED_Panel_Text",
        "AED_Panel_Text_L", "AED_Panel_Text_R",
        "AED_Screen", "AED_Screen_L", "AED_Screen_R", "AED_Screen_Top",
        "AED_Text", "AED_Text_L", "AED_Text_R",
        "AED_Text_Clock", "AED_Text_Instruction",
        "AED_Display", "AED_Device", "AED_Carry", "AED_Case_Lid",
        "Button_AED_On", "Button_Shock",
        "Info_Call",
    };

    static Material matBase, matPad, matPadOk, matPadBad, matZoneBad, matZoneOk, matPanel;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V25");

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null) { Debug.LogError("[V25] Kein Referenzmaterial - abgebrochen."); return; }

        matPad     = Mat("Mat_Pad",      new Color(0.90f, 0.90f, 0.88f));
        matPadOk   = Mat("Mat_Pad_OK",   new Color(0.22f, 0.70f, 0.30f));
        matPadBad  = Mat("Mat_Pad_Bad",  new Color(0.80f, 0.20f, 0.18f));
        matZoneBad = Mat("Mat_Zone_Bad", new Color(0.72f, 0.24f, 0.22f));
        matZoneOk  = Mat("Mat_Zone_OK",  new Color(0.28f, 0.64f, 0.34f));
        matPanel   = Mat("Mat_AED_Panel",new Color(0.09f, 0.11f, 0.12f));

        // ------------------------------------------------- abraeumen
        int removed = 0;
        foreach (string n in REMOVE)
        {
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null || t.name != n) continue;
                if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded) continue;
                Debug.Log("[V25] entfernt: " + n);
                Undo.DestroyObjectImmediate(t.gameObject);
                removed++;
                break;
            }
        }
        Debug.Log("[V25] " + removed + " Altobjekt(e) entfernt.");

        GameObject old = Find(ROOT);
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject aed = Find("AED_Unit");
        GameObject patient = Find("Patient_Body");
        if (aed == null || patient == null)
        {
            Debug.LogError("[V25] 'AED_Unit' oder 'Patient_Body' fehlt - abgebrochen.");
            return;
        }

        // Das alte AEDDisplay treibt nur noch die Wanduhr. Seine
        // Instruktionsflaechen sind soeben geloescht worden, die
        // Referenzen muessen also leer sein, sonst laeuft es ins Nichts.
        AEDDisplay clock = Object.FindAnyObjectByType<AEDDisplay>();
        if (clock != null)
        {
            Undo.RecordObject(clock, "clock only");
            clock.instructionTexts = new TextMeshPro[0];
            EditorUtility.SetDirty(clock);
            Debug.Log("[V25] AEDDisplay behaelt nur noch die Uhr.");
        }

        // ------------------------------------------------- neu bauen
        GameObject root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, ROOT);

        // Ablegepunkt fuer den AED
        GameObject aedTarget = Empty(root, "AED_Target", AED_AT);

        // Zielzonen, unsichtbar - nur Punkte, keine Collider
        GameObject zA = Empty(root, "Zone_Pad_A_NW", ZONE_A);
        GameObject zB = Empty(root, "Zone_Pad_B_SE", ZONE_B);

        // Anzeige ueber dem AED, dreht sich zum Betrachter
        GameObject disp = Empty(root, "AED_Display", new Vector3(AED_AT.x, 1.22f, AED_AT.z));
        A1Billboard bb = disp.AddComponent<A1Billboard>();
        bb.yawOnly = true;

        Box(disp, "Panel", new Vector3(0f, 0f, 0f), new Vector3(1.05f, 0.52f, 0.03f), matPanel, false);

        GameObject txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(disp.transform, false);
        txtGo.transform.localPosition = new Vector3(0f, 0f, -0.025f);
        // Kein 180-Grad-Dreh: A1Billboard richtet die Vorderseite schon so
        // aus, wie TextMeshPro es braucht. Mit zusaetzlicher Drehung sieht
        // man die Rueckseite, der Text steht dann spiegelverkehrt.
        txtGo.transform.localRotation = Quaternion.identity;
        TextMeshPro txt = txtGo.AddComponent<TextMeshPro>();
        txt.text = "AED READY";
        txt.fontSize = 1.1f;
        txt.enableAutoSizing = false;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.85f, 0.95f, 0.80f);
        RectTransform rt = txtGo.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(1.0f, 0.48f);

        // Pads
        GameObject padsRoot = Empty(root, "Pads", Vector3.zero);
        Holdable pA = Pad(padsRoot, "Pad_A", new Vector3(AED_AT.x, 0.29f, AED_AT.z + 0.16f));
        Holdable pB = Pad(padsRoot, "Pad_B", new Vector3(AED_AT.x, 0.29f, AED_AT.z - 0.16f));

        // Sicherheitszone auf dem Boden
        GameObject zone = Empty(root, "Clear_Zone", new Vector3(patient.transform.position.x, 0.012f, patient.transform.position.z));
        Prim(PrimitiveType.Cylinder, zone, "Ring", Vector3.zero,
             new Vector3(CLEAR_DIST * 2f, 0.008f, CLEAR_DIST * 2f), matZoneBad, false);
        // Im Editor ausgeblendet, sonst liegt eine rote Scheibe ueber dem
        // halben Raum. Zur Laufzeit schaltet A2Flow sie im richtigen Moment ein.
        zone.SetActive(false);

        // ------------------------------------------------- verdrahten
        A2Flow flow = root.AddComponent<A2Flow>();
        flow.aed = aed.transform;
        flow.aedHoldable = aed.GetComponent<Holdable>();
        flow.aedTarget = aedTarget.transform;
        flow.displayRoot = disp;
        flow.displayText = txt;
        flow.padsRoot = padsRoot;
        flow.padA = pA;
        flow.padB = pB;
        flow.zoneA = zA.transform;
        flow.zoneB = zB.transform;
        flow.zoneRadius = ZONE_RADIUS;
        flow.matPad = matPad;
        flow.matPadOk = matPadOk;
        flow.matPadBad = matPadBad;
        flow.clock = clock;
        flow.patient = patient.transform;
        flow.clearDistance = CLEAR_DIST;
        flow.clearZone = zone;
        flow.matZoneBad = matZoneBad;
        flow.matZoneOk = matZoneOk;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = Find("EZPZ Player Flat Screen WASD");
        if (player != null) flow.player = player.transform;
        else Debug.LogWarning("[V25] Spieler nicht gefunden - 'player' im Inspector nachtragen.");

        EditorUtility.SetDirty(flow);

        // Klick auf den AED
        A2Click click = aed.GetComponent<A2Click>();
        if (click == null) click = Undo.AddComponent<A2Click>(aed);
        Undo.RecordObject(click, "click");
        click.flow = flow;
        EditorUtility.SetDirty(click);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(string.Format(
            "[V25] Fertig. Zone A (Nordwest) bei x {0:0.00} / z {1:0.00}, " +
            "Zone B (Suedost) bei x {2:0.00} / z {3:0.00}, Toleranz {4:0.00} m. " +
            "AED-Ablage bei x {5:0.00} / z {6:0.00}. Sicherheitsabstand {7:0.0} m. " +
            "Szene speichern mit Ctrl+S.",
            ZONE_A.x, ZONE_A.z, ZONE_B.x, ZONE_B.z, ZONE_RADIUS, AED_AT.x, AED_AT.z, CLEAR_DIST));
    }

    // ---------------------------------------------------------- Bausteine

    static Holdable Pad(GameObject parent, string name, Vector3 at)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent.transform, false);
        g.transform.position = at;
        g.transform.localScale = new Vector3(0.15f, 0.02f, 0.19f);

        Renderer r = g.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = matPad;

        Rigidbody rb = g.AddComponent<Rigidbody>();
        rb.mass = 0.2f;

        Holdable h = g.AddComponent<Holdable>();
        h.dropOnKeyLift = true;
        h.freezeRotation = true;

        Undo.RegisterCreatedObjectUndo(g, name);
        return h;
    }

    static GameObject Empty(GameObject parent, string name, Vector3 at)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        g.transform.position = at;
        g.transform.rotation = Quaternion.identity;
        return g;
    }

    static void Box(GameObject p, string n, Vector3 pos, Vector3 sc, Material m, bool collider)
    { Prim(PrimitiveType.Cube, p, n, pos, sc, m, collider); }

    static void Prim(PrimitiveType type, GameObject parent, string name,
                     Vector3 localPos, Vector3 scale, Material m, bool keepCollider)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = scale;

        if (!keepCollider)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        if (m != null)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }

    static Material Mat(string assetName, Color c)
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
