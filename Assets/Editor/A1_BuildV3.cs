// A1_BuildV3.cs
// Menue:  Tools > AED Prototype > Build V3
//
// Behebt die Punkte aus dem Playtest:
//   - Uhr an die Wand hinter dem Patienten, gross und lesbar
//   - alle Beschriftungen auf Englisch, auf BEIDEN Seiten lesbar
//   - AED rastet beim Ablegen ein und ist danach fixiert (kein Wieder-Aufheben)
//   - Pads liegen im geoeffneten Koffer neben dem Geraet, nicht unter ihm
//   - Pads rasten an den Brustmarken ein und bleiben dort
//   - Schockknopf erst, wenn beide Pads sitzen UND der Spieler zurueckgetreten ist
//   - Stuhl und Zugangszone entfernt (waren unverstaendlich)
//   - alle Events werden vorher geleert, damit keine alte Verdrahtung stoert

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;
using System.Collections.Generic;

public static class A1_BuildV3
{
    const string MENU = "Tools/AED Prototype/Build V3";

    const string P_AREA =
        "Assets/EZPZ Interaction Toolkit/Prefabs/_General Interactables/EZPZ Area Trigger.prefab";
    const string P_BUTTON =
        "Assets/EZPZ Interaction Toolkit/Prefabs/_General Interactables/EZPZ Button Interactable.prefab";

    // Wo der AED nach dem Ablegen steht.
    static readonly Vector3 AED_ANCHOR = new Vector3(0.9f, 0.18f, 3.0f);
    // Geoeffneter Kofferdeckel mit den Pads, deutlich neben dem Geraet.
    static readonly Vector3 LID_POS = new Vector3(1.8f, 0.09f, 3.0f);
    static readonly Vector3 PAD1_POS = new Vector3(1.8f, 0.23f, 3.25f);
    static readonly Vector3 PAD2_POS = new Vector3(1.8f, 0.23f, 2.75f);
    // Brustmarken.
    static readonly Vector3 PAD_A_ANCHOR = new Vector3(0.14f, 0.318f, 3.62f);
    static readonly Vector3 PAD_B_ANCHOR = new Vector3(-0.22f, 0.30f, 3.30f);

    static GameObject root;
    static Material matRoom, matFloor, matMarker, matGreen, matRed, matDark, matDevice;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V3");

        var areaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P_AREA);
        var btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P_BUTTON);
        if (areaPrefab == null || btnPrefab == null)
        {
            Debug.LogError("[A1] EZPZ-Prefabs nicht gefunden.");
            return;
        }

        root = Find("====A1 CHANGES====");
        if (root == null)
        {
            root = new GameObject("====A1 CHANGES====");
            Undo.RegisterCreatedObjectUndo(root, "A1 root");
        }

        matFloor = FindMaterial("Mat_Floor");
        matRoom = FindMaterial("Mat_Room");
        matMarker = FindMaterial("Mat_Marker");
        matGreen = FindMaterial("Mat_A1_Good");
        matRed = FindMaterial("Mat_A1_TooLow");
        matDark = FindMaterial("Mat_A1_Dark");
        matDevice = FindMaterial("Mat_A1_Device");

        RemoveConfusingBits();
        var display = RebuildDevice();
        var clock = BuildWallClock();
        BuildCaseAndPads();
        var anchors = BuildAnchors();
        var zones = BuildZones(areaPrefab);
        var buttons = BuildButtons(btnPrefab);
        var seq = BuildSequence(display, buttons.shock);
        LinkDisplay(display, clock);
        WireAll(display, seq, zones, buttons, anchors);

        Undo.CollapseUndoOperations(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[A1] Build V3 fertig.");
    }

    // ===================================================== Verwirrendes entfernen

    static void RemoveConfusingBits()
    {
        // Stuhl und Zugangszone: fuer Nutzer nicht erklaerbar, daher raus.
        // Alte Lampen und der alte AED-Traeger ebenfalls.
        string[] off = { "Obstacle_Chair", "Zone_Access", "Lamp_Pad_Good",
                         "Lamp_Pad_TooLow", "Lamp_Shock_Done", "AED_Carry",
                         "Table_AED", "Zone_Chest", "AED_Device", "AED_Display",
                         "Zone_Patient", "Marker_Chest" };
        foreach (var n in off)
        {
            var go = Find(n);
            if (go != null && go.activeSelf)
            {
                Undo.RecordObject(go, n + " aus");
                go.SetActive(false);
            }
        }
    }

    // ============================================================= Geraet

    static AEDDisplay RebuildDevice()
    {
        var unit = Find("AED_Unit");
        if (unit == null)
        {
            unit = new GameObject("AED_Unit");
            Undo.RegisterCreatedObjectUndo(unit, "AED_Unit");
        }
        Undo.RecordObject(unit.transform, "AED_Unit transform");
        unit.transform.SetParent(root.transform, true);
        unit.transform.position = new Vector3(-7.45f, 1.2f, -4f);
        unit.transform.rotation = Quaternion.identity;
        unit.transform.localScale = Vector3.one;

        Shape("AED_Body", Vector3.zero, new Vector3(0.45f, 0.3f, 0.36f), matDevice, unit, true);

        // Zwei Displayplatten, eine je Seite, damit man von ueberall lesen kann.
        var sR = Shape("AED_Screen_R", new Vector3(0.235f, 0.17f, 0f),
                       new Vector3(0.02f, 0.22f, 0.32f), matDark, unit, true);
        var sL = Shape("AED_Screen_L", new Vector3(-0.235f, 0.17f, 0f),
                       new Vector3(0.02f, 0.22f, 0.32f), matDark, unit, true);
        StripCollider(sR); StripCollider(sL);

        // Text liest nach +z lokal. yaw 90 -> schaut nach +x, yaw -90 -> nach -x.
        var tR = Label("AED_Text_R", unit.transform, new Vector3(0.252f, 0.17f, 0f),
                       90f, "", 0.5f, Color.white, new Vector2(0.30f, 0.20f));
        var tL = Label("AED_Text_L", unit.transform, new Vector3(-0.252f, 0.17f, 0f),
                       -90f, "", 0.5f, Color.white, new Vector2(0.30f, 0.20f));

        var col = unit.GetComponent<BoxCollider>();
        if (col == null) col = Undo.AddComponent<BoxCollider>(unit);
        Undo.RecordObject(col, "AED collider");
        col.size = new Vector3(0.5f, 0.36f, 0.42f);
        col.center = Vector3.zero;

        var rb = unit.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(unit);
        Undo.RecordObject(rb, "AED rigidbody");
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var hold = Ensure<Holdable>(unit);
        Undo.RecordObject(hold, "AED holdable");
        hold.groundPlace = false;
        hold.dropOnKeyLift = false;
        hold.freezeRotation = true;

        EnsureFilter(unit, "AED");

        var disp = Ensure<AEDDisplay>(unit);
        Undo.RecordObject(disp, "AED display");
        disp.instructionTexts = new[] { tR, tL };
        disp.currentMessage = "";
        return disp;
    }

    // ========================================================== Wanduhr

    static TextMeshPro BuildWallClock()
    {
        // Nordwand liegt bei z = 6, der Patient mit dem Kopf bei z = 4.
        // Die Uhr steht damit im Blick, wenn man am Patienten arbeitet.
        var plate = Shape("Clock_Plate", new Vector3(0f, 2.15f, 5.86f),
                          new Vector3(2.2f, 0.95f, 0.06f), matDark, root);
        StripCollider(plate);

        Label("Clock_Caption", plate.transform, new Vector3(0f, 0.30f, -0.55f), 180f,
              "TIME SINCE COLLAPSE", 0.30f, new Color(0.75f, 0.78f, 0.82f),
              new Vector2(2.0f, 0.3f));

        return Label("Clock_Value", plate.transform, new Vector3(0f, -0.10f, -0.55f), 180f,
                     "0:00", 1.1f, new Color(1f, 0.85f, 0.3f), new Vector2(2.0f, 0.6f));
    }

    static void LinkDisplay(AEDDisplay disp, TextMeshPro clock)
    {
        Undo.RecordObject(disp, "display links");
        disp.clockTexts = new[] { clock };
        EditorUtility.SetDirty(disp);
    }

    // ================================================= Kofferdeckel und Pads

    static void BuildCaseAndPads()
    {
        var lid = Shape("AED_Case_Lid", LID_POS, new Vector3(0.7f, 0.18f, 1.1f), matDark, root);
        SetActiveStart(lid, false);

        MakePad("Pad_1", PAD1_POS);
        MakePad("Pad_2", PAD2_POS);

        var mA = Shape("Marker_Pad_A", new Vector3(0.14f, 0.305f, 3.62f),
                       new Vector3(0.20f, 0.01f, 0.20f), matMarker, root);
        var mB = Shape("Marker_Pad_B", new Vector3(-0.238f, 0.30f, 3.30f),
                       new Vector3(0.01f, 0.16f, 0.26f), matMarker, root);
        StripCollider(mA); StripCollider(mB);
        SetActiveStart(mA, false);
        SetActiveStart(mB, false);
    }

    static void MakePad(string name, Vector3 pos)
    {
        var pad = Shape(name, pos, new Vector3(0.17f, 0.03f, 0.22f), matGreen, root);

        var rb = pad.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(pad);
        Undo.RecordObject(rb, name + " rb");
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var h = Ensure<Holdable>(pad);
        Undo.RecordObject(h, name + " holdable");
        h.groundPlace = false;
        h.freezeRotation = true;

        EnsureFilter(pad, "PAD");
        SetActiveStart(pad, false);
    }

    // ============================================================== Anker

    class Anchors { public Transform aed, padA, padB; }

    static Anchors BuildAnchors()
    {
        var a = new Anchors();
        a.aed = Anchor("Anchor_AED", AED_ANCHOR);
        a.padA = Anchor("Anchor_Pad_A", PAD_A_ANCHOR);
        a.padB = Anchor("Anchor_Pad_B", PAD_B_ANCHOR);
        return a;
    }

    static Transform Anchor(string name, Vector3 pos)
    {
        var go = Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
        }
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(root.transform, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        return go.transform;
    }

    // ============================================================== Zonen

    class Zones { public InteractableTrigger aedPlaced, padA, padB, padLow, safety; }

    static Zones BuildZones(GameObject prefab)
    {
        var z = new Zones();
        z.aedPlaced = Area(prefab, "Zone_AED_Placed", new Vector3(0.9f, 0.3f, 3f),
                           new Vector3(1.1f, 0.8f, 1.5f), "AED");
        z.padA = Area(prefab, "Target_Pad_A", new Vector3(0.14f, 0.36f, 3.62f),
                      new Vector3(0.34f, 0.28f, 0.34f), "PAD");
        z.padB = Area(prefab, "Target_Pad_B", new Vector3(-0.26f, 0.32f, 3.30f),
                      new Vector3(0.30f, 0.30f, 0.40f), "PAD");
        z.padLow = Area(prefab, "Zone_Pad_TooLow", new Vector3(0f, 0.34f, 2.95f),
                        new Vector3(0.54f, 0.22f, 0.50f), "PAD");
        z.safety = Area(prefab, "Zone_Safety", new Vector3(0f, 1.0f, 3.1f),
                        new Vector3(2.8f, 2.0f, 3.0f), "PLAYER");

        var player = Find("EZPZ Player Flat Screen WASD");
        if (player != null) EnsureFilter(player, "PLAYER");
        else Debug.LogWarning("[A1] Player nicht gefunden.");

        return z;
    }

    // ============================================================ Buttons

    class Buttons { public GameObject aedOn, shock; }

    static Buttons BuildButtons(GameObject prefab)
    {
        var b = new Buttons();
        b.aedOn = Button(prefab, "Button_AED_On", new Vector3(0.9f, 0.62f, 3.0f), "TURN ON AED");
        b.shock = Button(prefab, "Button_Shock", new Vector3(1.75f, 0.72f, 3.0f), "DELIVER SHOCK");
        SetActiveStart(b.aedOn, false);
        SetActiveStart(b.shock, false);
        return b;
    }

    static GameObject Button(GameObject prefab, string name, Vector3 pos, string label)
    {
        var go = Find(name);
        if (go == null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, name);
        }
        Undo.RecordObject(go.transform, name + " transform");
        go.transform.SetParent(root.transform, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;

        // Vorhandene Beschriftung der Prefab auf Englisch setzen.
        foreach (var t in go.GetComponentsInChildren<TextMeshPro>(true))
        {
            if (t.gameObject.name.StartsWith("A1_")) continue;
            Undo.RecordObject(t, "button label");
            t.text = label;
            Record(t);
        }
        foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(t, "button label");
            t.text = label;
            Record(t);
        }

        // Zusaetzliche Beschriftung auf beiden Seiten, damit sie immer lesbar ist.
        Label("A1_" + name + "_Front", go.transform, new Vector3(0f, 0.16f, 0.14f), 0f,
              label, 0.25f, Color.white, new Vector2(0.9f, 0.25f));
        Label("A1_" + name + "_Back", go.transform, new Vector3(0f, 0.16f, -0.14f), 180f,
              label, 0.25f, Color.white, new Vector2(0.9f, 0.25f));

        return go;
    }

    // =========================================================== Ablauf

    static A1Sequence BuildSequence(AEDDisplay disp, GameObject shockButton)
    {
        var go = Find("A1_Sequence");
        if (go == null)
        {
            go = new GameObject("A1_Sequence");
            Undo.RegisterCreatedObjectUndo(go, "A1_Sequence");
            go.transform.SetParent(root.transform, true);
        }
        var seq = Ensure<A1Sequence>(go);
        Undo.RecordObject(seq, "sequence");
        seq.display = disp;
        seq.shockButton = shockButton;
        seq.padsNeeded = 2;
        EditorUtility.SetDirty(seq);
        return seq;
    }

    static A1SnapLock SnapLock(string name, InteractableTrigger source, Transform anchor,
                               AEDDisplay disp, A1Sequence seq, string message, bool pad)
    {
        var go = Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(root.transform, true);
        }
        var sl = Ensure<A1SnapLock>(go);
        Undo.RecordObject(sl, name);
        sl.source = source;
        sl.anchor = anchor;
        sl.display = disp;
        sl.sequence = seq;
        sl.message = message;
        sl.countsAsPad = pad;
        sl.done = false;
        EditorUtility.SetDirty(sl);
        return sl;
    }

    // ======================================================== Verdrahtung

    static void WireAll(AEDDisplay disp, A1Sequence seq, Zones z, Buttons b, Anchors a)
    {
        var pad1 = Find("Pad_1");
        var pad2 = Find("Pad_2");
        var lid = Find("AED_Case_Lid");
        var mA = Find("Marker_Pad_A");
        var mB = Find("Marker_Pad_B");
        var call = Find("Button_Call_000");

        var snapAed = SnapLock("Snap_AED", z.aedPlaced, a.aed, disp, seq, "TURN ON THE AED", false);
        var snapA = SnapLock("Snap_Pad_A", z.padA, a.padA, disp, seq, "PAD 1 IN POSITION", true);
        var snapB = SnapLock("Snap_Pad_B", z.padB, a.padB, disp, seq, "PAD 2 IN POSITION", true);

        // --- Notruf
        if (call != null)
        {
            SetButtonLabel(call, "CALL 000");
            foreach (var ig in call.GetComponentsInChildren<InteractableGeneral>(true))
            {
                RevertOverrides(ig);
                Undo.RecordObject(ig, "call wiring");
                Clear(ig.onPrimaryInteract);
                AddVoid(ig.onPrimaryInteract, disp.StartClock);
                AddString(ig.onPrimaryInteract, disp.Show, "GET THE AED - FOLLOW THE SIGN");
                Record(ig);
            }
        }

        // --- AED abgelegt: einrasten, Koffer und Pads erscheinen, Knopf frei
        Undo.RecordObject(z.aedPlaced, "aed zone");
        Clear(z.aedPlaced.onTriggerEnter);
        Clear(z.aedPlaced.onTriggerExit);
        AddVoid(z.aedPlaced.onTriggerEnter, snapAed.Snap);
        AddSetActive(z.aedPlaced.onTriggerEnter, lid, true);
        AddSetActive(z.aedPlaced.onTriggerEnter, pad1, true);
        AddSetActive(z.aedPlaced.onTriggerEnter, pad2, true);
        AddSetActive(z.aedPlaced.onTriggerEnter, b.aedOn, true);
        Record(z.aedPlaced);

        // --- Einschalten: Brustmarken zeigen
        foreach (var ig in b.aedOn.GetComponentsInChildren<InteractableGeneral>(true))
        {
            RevertOverrides(ig);
            Undo.RecordObject(ig, "aedOn wiring");
            Clear(ig.onPrimaryInteract);
            AddSetActive(ig.onPrimaryInteract, mA, true);
            AddSetActive(ig.onPrimaryInteract, mB, true);
            AddString(ig.onPrimaryInteract, disp.Show, "ATTACH BOTH PADS TO THE MARKS");
            AddSetActive(ig.onPrimaryInteract, b.aedOn, false);
            Record(ig);
        }

        // --- Pads
        Undo.RecordObject(z.padA, "padA");
        Clear(z.padA.onTriggerEnter); Clear(z.padA.onTriggerExit);
        AddVoid(z.padA.onTriggerEnter, snapA.Snap);
        Record(z.padA);

        Undo.RecordObject(z.padB, "padB");
        Clear(z.padB.onTriggerEnter); Clear(z.padB.onTriggerExit);
        AddVoid(z.padB.onTriggerEnter, snapB.Snap);
        Record(z.padB);

        Undo.RecordObject(z.padLow, "padLow");
        Clear(z.padLow.onTriggerEnter); Clear(z.padLow.onTriggerExit);
        AddString(z.padLow.onTriggerEnter, disp.Show, "PAD TOO LOW - MOVE IT HIGHER");
        Record(z.padLow);

        // --- Sicherheitszone steuert nur noch den Ablauf
        Undo.RecordObject(z.safety, "safety");
        Clear(z.safety.onTriggerEnter); Clear(z.safety.onTriggerExit);
        AddVoid(z.safety.onTriggerExit, seq.PlayerClear);
        AddVoid(z.safety.onTriggerEnter, seq.PlayerBack);
        Record(z.safety);

        // --- Schock
        foreach (var ig in b.shock.GetComponentsInChildren<InteractableGeneral>(true))
        {
            RevertOverrides(ig);
            Undo.RecordObject(ig, "shock wiring");
            Clear(ig.onPrimaryInteract);
            AddVoid(ig.onPrimaryInteract, disp.StopClock);
            AddString(ig.onPrimaryInteract, disp.Show, "SHOCK DELIVERED - RESUME CPR");
            AddSetActive(ig.onPrimaryInteract, b.shock, false);
            Record(ig);
        }
    }

    static void SetButtonLabel(GameObject go, string label)
    {
        foreach (var t in go.GetComponentsInChildren<TextMeshPro>(true))
        {
            if (t.gameObject.name.StartsWith("A1_")) continue;
            Undo.RecordObject(t, "label"); t.text = label; Record(t);
        }
        foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(t, "label"); t.text = label; Record(t);
        }
    }

    // ========================================================= Hilfsmittel

    static void Clear(UnityEventBase evt)
    {
        if (evt == null) return;
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(evt, i);
    }

    // Alte Overrides einer Prefab-Instanz wegwerfen. Ohne das bleiben
    // Listener frueherer Durchlaeufe in der Modifikationsliste stehen und
    // werden beim Szenenladen wieder angewandt - auch wenn das Event
    // zwischendurch geleert wurde.
    static void RevertOverrides(Component c)
    {
        if (c == null) return;
        if (PrefabUtility.IsPartOfPrefabInstance(c))
            PrefabUtility.RevertObjectOverride(c, InteractionMode.AutomatedAction);
    }

    static void AddSetActive(UnityEvent evt, GameObject go, bool state)
    {
        if (evt == null || go == null) return;
        UnityEventTools.AddBoolPersistentListener(evt, new UnityAction<bool>(go.SetActive), state);
    }

    static void AddString(UnityEvent evt, UnityAction<string> call, string arg)
    {
        if (evt == null) return;
        UnityEventTools.AddStringPersistentListener(evt, call, arg);
    }

    static void AddVoid(UnityEvent evt, UnityAction call)
    {
        if (evt == null) return;
        UnityEventTools.AddVoidPersistentListener(evt, call);
    }

    static void Record(Object o)
    {
        EditorUtility.SetDirty(o);
        PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }

    static GameObject Shape(string name, Vector3 pos, Vector3 scale, Material mat,
                            GameObject parent, bool local = false)
    {
        var go = Find(name);
        if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, name);
            var r0 = go.GetComponent<Renderer>();
            if (mat != null && r0 != null) r0.sharedMaterial = mat;
        }
        Undo.RecordObject(go.transform, name + " transform");
        go.transform.SetParent(parent != null ? parent.transform : null, !local);
        if (local) go.transform.localPosition = pos; else go.transform.position = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        if (!go.activeSelf) { Undo.RecordObject(go, name); go.SetActive(true); }
        if (go.GetComponent<Collider>() == null) Undo.AddComponent<BoxCollider>(go);
        return go;
    }

    static TextMeshPro Label(string name, Transform parent, Vector3 localPos, float yaw,
                             string text, float size, Color col, Vector2 box)
    {
        var go = Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
        }
        Undo.RecordObject(go.transform, name + " transform");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        // Elterliche Skalierung herausrechnen, sonst wird der Text verzerrt.
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        go.transform.localScale = new Vector3(
            ps.x != 0 ? 1f / ps.x : 1f,
            ps.y != 0 ? 1f / ps.y : 1f,
            ps.z != 0 ? 1f / ps.z : 1f);

        var tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null) tmp = Undo.AddComponent<TextMeshPro>(go);
        Undo.RecordObject(tmp, name + " text");
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = box;

        Record(tmp);
        return tmp;
    }

    static InteractableTrigger Area(GameObject prefab, string name, Vector3 pos,
                                    Vector3 scale, string filter)
    {
        var go = Find(name);
        if (go == null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, name);
        }
        Undo.RecordObject(go.transform, name + " transform");
        go.transform.SetParent(root.transform, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = scale;
        if (!go.activeSelf) { Undo.RecordObject(go, name); go.SetActive(true); }

        var it = Ensure<InteractableTrigger>(go);
        RevertOverrides(it);
        Undo.RecordObject(it, name + " trigger");
        it.filterString = filter;
        it.allowUnfiltered = false;
        it.excludeMode = false;
        it.deleteOnEnter = false;
        it.disableOnEnter = false;
        it.visibleAtRuntime = false;
        return it;
    }

    static void StripCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Undo.DestroyObjectImmediate(c);
    }

    static void EnsureFilter(GameObject go, string s)
    {
        var f = Ensure<TriggerFilter>(go);
        Undo.RecordObject(f, go.name + " filter");
        f.filterString = s;
        Record(f);
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    static void SetActiveStart(GameObject go, bool state)
    {
        if (go != null && go.activeSelf != state)
        {
            Undo.RecordObject(go, go.name + " active");
            go.SetActive(state);
        }
    }

    static GameObject Find(string n)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var r in scene.GetRootGameObjects())
        {
            var hit = FindRecursive(r.transform, n);
            if (hit != null) return hit;
        }
        return null;
    }

    static GameObject FindRecursive(Transform t, string n)
    {
        if (t.name == n) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindRecursive(t.GetChild(i), n);
            if (hit != null) return hit;
        }
        return null;
    }

    static Material FindMaterial(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Material"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null && m.name == name) return m;
        }
        return null;
    }
}
