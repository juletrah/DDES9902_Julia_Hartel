// A1_BuildV4.cs
// Menue:  Tools > AED Prototype > Build V4
//
// Behebt die Punkte aus dem zweiten Playtest:
//   - Texte drehen sich zum Spieler, nie mehr spiegelverkehrt
//   - Reste aus V2 (AED_Screen, AED_Text_*) werden abgeschaltet
//   - Rueckmeldung beim Notruf ist wieder da
//   - AED-Display kompakt statt sperrige schwarze Platten
//   - Hinweistafel steht am KOPF des Patienten, nicht auf der Brust
//   - Pad-Platzierung: zwei richtige und drei falsche Felder, fein abgesteckt,
//     falsche Platzierung schiebt das Pad zurueck statt es zu fixieren
//   - mehr Platz rechts neben dem Patienten

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

public static class A1_BuildV4
{
    const string MENU = "Tools/AED Prototype/Build V4";

    const string P_AREA =
        "Assets/EZPZ Interaction Toolkit/Prefabs/_General Interactables/EZPZ Area Trigger.prefab";
    const string P_BUTTON =
        "Assets/EZPZ Interaction Toolkit/Prefabs/_General Interactables/EZPZ Button Interactable.prefab";

    // Geraet weiter nach rechts, damit neben dem Patienten Platz bleibt.
    static readonly Vector3 AED_ANCHOR = new Vector3(1.45f, 0.18f, 3.0f);
    static readonly Vector3 LID_POS    = new Vector3(2.35f, 0.09f, 3.0f);
    static readonly Vector3 PAD1_POS   = new Vector3(2.35f, 0.23f, 3.25f);
    static readonly Vector3 PAD2_POS   = new Vector3(2.35f, 0.23f, 2.75f);

    // Brust: Koerper x -0.25..0.25, Oberseite y = 0.30, Kopf bei z = 4.
    static readonly Vector3 PAD_A_ANCHOR = new Vector3(0.15f, 0.315f, 3.60f);
    // Seitlich unter der Achsel: das Pad steht hochkant an der Koerperflanke.
    static readonly Vector3 PAD_B_ANCHOR = new Vector3(-0.265f, 0.22f, 3.26f);
    static readonly Vector3 PAD_B_EULER  = new Vector3(0f, 0f, 90f);

    static GameObject root;
    static Material matRoom, matMarker, matGreen, matRed, matDark, matDevice;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V4");

        var areaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P_AREA);
        var btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P_BUTTON);
        if (areaPrefab == null || btnPrefab == null)
        {
            Debug.LogError("[A1] EZPZ-Prefabs nicht gefunden."); return;
        }

        root = Find("====A1 CHANGES====");
        if (root == null)
        {
            root = new GameObject("====A1 CHANGES====");
            Undo.RegisterCreatedObjectUndo(root, "A1 root");
        }

        matRoom = FindMaterial("Mat_Room");
        matMarker = FindMaterial("Mat_Marker");
        matGreen = FindMaterial("Mat_A1_Good");
        matRed = FindMaterial("Mat_A1_TooLow");
        matDark = FindMaterial("Mat_A1_Dark");
        matDevice = FindMaterial("Mat_A1_Device");

        KillLeftovers();
        BuildCabinetSign();
        RebuildDevice();
        var headText = BuildHeadPanel();
        var clockText = BuildWallClock();
        var disp = LinkDisplay(headText, clockText);
        BuildCaseAndPads();
        var anchors = BuildAnchors();
        var zones = BuildZones(areaPrefab);
        var buttons = BuildButtons(btnPrefab);
        var seq = BuildSequence(disp, buttons.shock);
        WireAll(disp, seq, zones, buttons, anchors);

        Undo.CollapseUndoOperations(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[A1] Build V4 fertig.");
    }

    // ================================================== Altlasten abschalten

    static void KillLeftovers()
    {
        string[] off = {
            // Reste aus V2, lagen doppelt auf dem Geraet:
            "AED_Screen", "AED_Text_Instruction", "AED_Text_Clock",
            // beide Displayseiten der V3 entfallen - die rechte war der Text,
            // der weiterhin ueber dem Geraet stand:
            "AED_Screen_L", "AED_Text_L", "AED_Screen_R", "AED_Text_R",
            // auch der Billboard-Text der V4 soll nicht am Geraet haengen:
            "AED_Text",
            // alte Versuche:
            "AED_Carry", "AED_Device", "AED_Display", "AED_Sign",
            "Lamp_Pad_Good", "Lamp_Pad_TooLow", "Lamp_Shock_Done",
            "Obstacle_Chair", "Zone_Access", "Table_AED", "Zone_Chest",
            "Zone_Patient", "Marker_Chest",
            // doppelte Buttonbeschriftungen, die Prefab traegt sie selbst:
            "A1_Button_AED_On_Front", "A1_Button_AED_On_Back",
            "A1_Button_Shock_Front", "A1_Button_Shock_Back",
        };
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

    // ============================================ Grosses AED ueber dem Schrank

    static void BuildCabinetSign()
    {
        // Der Schrank haengt an der Westwand (x = -8) bei z = -4.
        // Schild und Schrift zeigen in den Raum, also nach +x.
        var plate = Shape("Sign_Cabinet", new Vector3(-7.90f, 2.10f, -4f),
                          new Vector3(0.06f, 0.70f, 1.20f), matGreen, root);
        StripCollider(plate);

        var pivot = Find("Sign_Cabinet_Pivot");
        if (pivot == null)
        {
            pivot = new GameObject("Sign_Cabinet_Pivot");
            Undo.RegisterCreatedObjectUndo(pivot, "Sign_Cabinet_Pivot");
        }
        Undo.RecordObject(pivot.transform, "Sign_Cabinet_Pivot");
        pivot.transform.SetParent(root.transform, true);
        pivot.transform.position = new Vector3(-7.84f, 2.10f, -4f);
        pivot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        pivot.transform.localScale = Vector3.one;

        // Als Billboard, damit die Schrift garantiert von links nach rechts
        // lesbar ist, egal aus welcher Richtung man kommt.
        Billboard("Sign_Cabinet_Text", pivot.transform, Vector3.zero,
                  "AED", 2.6f, Color.white, new Vector2(1.15f, 0.65f));
    }

    // ============================================================== Geraet

    static void RebuildDevice()
    {
        var unit = Find("AED_Unit");
        if (unit == null)
        {
            unit = new GameObject("AED_Unit");
            Undo.RegisterCreatedObjectUndo(unit, "AED_Unit");
        }
        Undo.RecordObject(unit.transform, "AED_Unit");
        unit.transform.SetParent(root.transform, true);
        unit.transform.position = new Vector3(-7.45f, 1.2f, -4f);
        unit.transform.rotation = Quaternion.identity;
        unit.transform.localScale = Vector3.one;

        Shape("AED_Body", Vector3.zero, new Vector3(0.42f, 0.26f, 0.34f), matDevice, unit, true);

        // Kompaktes Display flach auf dem Deckel statt sperriger Seitenplatten.
        var screen = Shape("AED_Screen_Top", new Vector3(0f, 0.14f, 0.02f),
                           new Vector3(0.28f, 0.015f, 0.18f), matDark, unit, true);
        StripCollider(screen);

        // Kein schwebender Text ueber dem Geraet - das erledigt KillLeftovers.

        var col = unit.GetComponent<BoxCollider>();
        if (col == null) col = Undo.AddComponent<BoxCollider>(unit);
        Undo.RecordObject(col, "AED collider");
        col.size = new Vector3(0.46f, 0.3f, 0.4f);
        col.center = Vector3.zero;

        var rb = unit.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(unit);
        Undo.RecordObject(rb, "AED rb");
        rb.useGravity = true; rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var hold = Ensure<Holdable>(unit);
        Undo.RecordObject(hold, "AED holdable");
        hold.groundPlace = false; hold.dropOnKeyLift = false; hold.freezeRotation = true;

        EnsureFilter(unit, "AED");
    }

    // ===================================================== Tafel am Kopfende

    static TextMeshPro BuildHeadPanel()
    {
        // Steht am Kopf des Patienten (z = 4), damit die Brust frei bleibt.
        var pivot = Find("HeadPanel");
        if (pivot == null)
        {
            pivot = new GameObject("HeadPanel");
            Undo.RegisterCreatedObjectUndo(pivot, "HeadPanel");
        }
        Undo.RecordObject(pivot.transform, "HeadPanel");
        pivot.transform.SetParent(root.transform, true);
        pivot.transform.position = new Vector3(0f, 0.95f, 4.55f);
        pivot.transform.localScale = Vector3.one;
        Ensure<A1Billboard>(pivot);

        var plate = Shape("HeadPanel_Plate", new Vector3(0f, 0f, 0.03f),
                          new Vector3(1.30f, 0.42f, 0.03f), matDark, pivot, true);
        StripCollider(plate);

        return Label("HeadPanel_Text", pivot.transform, Vector3.zero, 0f,
                     "", 0.30f, Color.white, new Vector2(1.25f, 0.4f));
    }

    // ============================================================ Wanduhr

    static TextMeshPro BuildWallClock()
    {
        var plate = Shape("Clock_Plate", new Vector3(0f, 2.15f, 5.86f),
                          new Vector3(2.2f, 0.95f, 0.06f), matDark, root);
        StripCollider(plate);

        // Texte haengen NICHT an der skalierten Platte - genau das hat sie
        // vorher verzerrt und gespiegelt. Sie stehen frei davor.
        var pivot = Find("Clock_Texts");
        if (pivot == null)
        {
            pivot = new GameObject("Clock_Texts");
            Undo.RegisterCreatedObjectUndo(pivot, "Clock_Texts");
        }
        Undo.RecordObject(pivot.transform, "Clock_Texts");
        pivot.transform.SetParent(root.transform, true);
        pivot.transform.position = new Vector3(0f, 2.15f, 5.80f);
        pivot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        pivot.transform.localScale = Vector3.one;

        Label("Clock_Caption", pivot.transform, new Vector3(0f, 0.30f, 0f), 0f,
              "TIME SINCE COLLAPSE", 0.26f, new Color(0.78f, 0.80f, 0.84f),
              new Vector2(2.0f, 0.3f));

        return Label("Clock_Value", pivot.transform, new Vector3(0f, -0.08f, 0f), 0f,
                     "0:00", 0.9f, new Color(1f, 0.85f, 0.3f), new Vector2(2.0f, 0.6f));
    }

    static AEDDisplay LinkDisplay(TextMeshPro head, TextMeshPro clock)
    {
        var unit = Find("AED_Unit");
        var disp = Ensure<AEDDisplay>(unit);
        Undo.RecordObject(disp, "display");
        disp.instructionTexts = new[] { head };
        disp.clockTexts = new[] { clock };
        disp.currentMessage = "";
        EditorUtility.SetDirty(disp);
        return disp;
    }

    // ==================================================== Koffer, Pads, Marken

    static void BuildCaseAndPads()
    {
        var lid = Shape("AED_Case_Lid", LID_POS, new Vector3(0.7f, 0.18f, 1.1f), matDark, root);
        SetActiveStart(lid, false);

        MakePad("Pad_1", PAD1_POS);
        MakePad("Pad_2", PAD2_POS);

        // Flache Marken auf der Brustoberflaeche, nichts steht hoch.
        var mA = Shape("Marker_Pad_A", new Vector3(0.15f, 0.302f, 3.60f),
                       new Vector3(0.18f, 0.006f, 0.20f), matMarker, root);
        // Seitliche Marke steht hochkant an der Flanke, nicht auf der Brust.
        var mB = Shape("Marker_Pad_B", new Vector3(-0.256f, 0.22f, 3.26f),
                       new Vector3(0.006f, 0.17f, 0.20f), matMarker, root);
        StripCollider(mA); StripCollider(mB);
        SetActiveStart(mA, false);
        SetActiveStart(mB, false);
    }

    static void MakePad(string name, Vector3 pos)
    {
        var pad = Shape(name, pos, new Vector3(0.16f, 0.025f, 0.19f), matGreen, root);

        // Der Collider ist bewusst dicker als das Pad selbst. Eine 2,5 cm
        // flache Platte ist mit dem Mauszeiger kaum zu treffen; so laesst sie
        // sich greifen, ohne dass man die Ecke erwischen muss.
        var bc = pad.GetComponent<BoxCollider>();
        if (bc != null)
        {
            Undo.RecordObject(bc, name + " collider");
            bc.size = new Vector3(1.3f, 5f, 1.3f);
        }

        var rb = pad.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(pad);
        Undo.RecordObject(rb, name + " rb");
        rb.useGravity = true; rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var h = Ensure<Holdable>(pad);
        Undo.RecordObject(h, name + " holdable");
        h.groundPlace = false;
        // Muss drehbar bleiben: das seitliche Pad wird hochkant angelegt.
        // Die Rigidbody-Constraints verhindern trotzdem, dass es wegkippt -
        // gedreht wird nur per Rechtsklick ueber Holdable.RotateY.
        h.freezeRotation = false;

        // Rechtsklick kippt das Pad um 90 Grad auf die Kante - das braucht
        // man fuer das seitliche Pad unter der Achsel.
        var rot = Ensure<A1PadRotate>(pad);
        Undo.RecordObject(rot, name + " rotate");
        rot.step = 90f;

        foreach (var ig in pad.GetComponents<InteractableGeneral>())
        {
            Undo.RecordObject(ig, name + " rotate wiring");
            Clear(ig.onSecondaryInteract);
            AddVoid(ig.onSecondaryInteract, rot.Roll);
            Record(ig);
        }

        EnsureFilter(pad, "PAD");
        SetActiveStart(pad, false);
    }

    // ============================================================== Anker

    class Anchors { public Transform aed, padA, padB; }

    static Anchors BuildAnchors()
    {
        return new Anchors {
            aed = Anchor("Anchor_AED", AED_ANCHOR, Vector3.zero),
            padA = Anchor("Anchor_Pad_A", PAD_A_ANCHOR, Vector3.zero),
            padB = Anchor("Anchor_Pad_B", PAD_B_ANCHOR, PAD_B_EULER),
        };
    }

    static Transform Anchor(string name, Vector3 pos, Vector3 euler)
    {
        var go = Find(name);
        if (go == null) { go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, name); }
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(root.transform, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        return go.transform;
    }

    // ============================================================== Zonen

    class Zones
    {
        public InteractableTrigger aedPlaced, padA, padB, low, central, high, safety;
    }

    static Zones BuildZones(GameObject prefab)
    {
        var z = new Zones();
        z.aedPlaced = Area(prefab, "Zone_AED_Placed", new Vector3(1.45f, 0.3f, 3f),
                           new Vector3(1.1f, 0.8f, 1.5f), "AED");

        // Richtig: rechts unter dem Schluesselbein / links unter der Achsel.
        z.padA = Area(prefab, "Target_Pad_A", new Vector3(0.15f, 0.34f, 3.60f),
                      new Vector3(0.20f, 0.20f, 0.26f), "PAD");
        z.padB = Area(prefab, "Target_Pad_B", new Vector3(-0.29f, 0.24f, 3.26f),
                      new Vector3(0.26f, 0.26f, 0.28f), "PAD");

        // Falsch: zu tief, zu mittig, zu hoch. Feiner abgesteckt, ohne
        // die richtigen Felder zu ueberlappen.
        z.low = Area(prefab, "Wrong_TooLow", new Vector3(0f, 0.34f, 2.80f),
                     new Vector3(0.52f, 0.20f, 0.50f), "PAD");
        z.central = Area(prefab, "Wrong_TooCentral", new Vector3(0f, 0.34f, 3.42f),
                         new Vector3(0.09f, 0.20f, 0.42f), "PAD");
        z.high = Area(prefab, "Wrong_TooHigh", new Vector3(0f, 0.34f, 3.88f),
                      new Vector3(0.52f, 0.20f, 0.22f), "PAD");

        z.safety = Area(prefab, "Zone_Safety", new Vector3(0f, 1.0f, 3.1f),
                        new Vector3(2.4f, 2.0f, 3.0f), "PLAYER");

        var player = Find("EZPZ Player Flat Screen WASD");
        if (player != null) EnsureFilter(player, "PLAYER");
        return z;
    }

    // ============================================================ Buttons

    class Buttons { public GameObject aedOn, shock; }

    static Buttons BuildButtons(GameObject prefab)
    {
        var b = new Buttons();
        b.aedOn = Button(prefab, "Button_AED_On", new Vector3(1.45f, 0.62f, 3.0f), "TURN ON AED");
        b.shock = Button(prefab, "Button_Shock", new Vector3(2.95f, 0.75f, 3.0f), "DELIVER SHOCK");
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
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(root.transform, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        SetLabel(go, label);
        return go;
    }

    static void SetLabel(GameObject go, string label)
    {
        foreach (var t in go.GetComponentsInChildren<TextMeshPro>(true))
        {
            Undo.RecordObject(t, "label"); t.text = label; Record(t);
            // Beschriftung dreht sich mit, damit sie von jeder Seite lesbar ist.
            Ensure<A1Billboard>(t.gameObject);
        }
        foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(t, "label"); t.text = label; Record(t);
        }
    }

    // ============================================================= Ablauf

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
        seq.display = disp; seq.shockButton = shockButton; seq.padsNeeded = 2;
        seq.padsAttached = 0; seq.padsReady = false; seq.playerClear = false;
        EditorUtility.SetDirty(seq);
        return seq;
    }

    static A1SnapLock Snapper(string name, InteractableTrigger src, Transform anchor,
                              AEDDisplay disp, A1Sequence seq, string msg,
                              bool pad, bool reject)
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
        sl.source = src; sl.anchor = anchor; sl.display = disp; sl.sequence = seq;
        sl.message = msg; sl.countsAsPad = pad; sl.reject = reject; sl.done = false;
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
        var info = Find("Info_Call");

        var snapAed = Snapper("Snap_AED", z.aedPlaced, a.aed, disp, seq, "TURN ON THE AED", false, false);
        var snapA = Snapper("Snap_Pad_A", z.padA, a.padA, disp, seq, "PAD 1 CORRECT", true, false);
        var snapB = Snapper("Snap_Pad_B", z.padB, a.padB, disp, seq, "PAD 2 CORRECT", true, false);
        var rejLow = Snapper("Reject_TooLow", z.low, null, disp, seq, "TOO LOW - ON THE CHEST, NOT THE BELLY", false, true);
        var rejCen = Snapper("Reject_TooCentral", z.central, null, disp, seq, "TOO CENTRAL - PADS MUST NOT TOUCH", false, true);
        var rejHigh = Snapper("Reject_TooHigh", z.high, null, disp, seq, "TOO HIGH - BELOW THE COLLARBONE", false, true);

        // --- Notruf
        // Die Meldung steht beim Telefon. Dafuer muss Info_Call aus dem
        // Button heraus, sonst verschwindet sie mit ihm.
        if (info != null)
        {
            Undo.RecordObject(info.transform, "Info_Call");
            info.transform.SetParent(root.transform, true);
            info.transform.position = new Vector3(-1.6f, 1.35f, 1.0f);
            info.transform.localScale = Vector3.one;
            Ensure<A1Billboard>(info);

            foreach (var t in info.GetComponentsInChildren<TextMeshPro>(true))
            {
                Undo.RecordObject(t, "info text");
                t.text = "HELP IS ON THE WAY - GET THE AED";
                t.fontSize = 0.55f;
                Record(t);
            }
            SetActiveStart(info, false);
        }

        if (call != null)
        {
            SetLabel(call, "CALL 000");
            foreach (var ig in call.GetComponentsInChildren<InteractableGeneral>(true))
            {
                RevertOverrides(ig);
                Undo.RecordObject(ig, "call");
                Clear(ig.onPrimaryInteract);
                AddVoid(ig.onPrimaryInteract, disp.StartClock);
                if (info != null) AddSetActive(ig.onPrimaryInteract, info, true);
                AddString(ig.onPrimaryInteract, disp.Show,
                          "HELP IS ON THE WAY - GET THE AED");
                // Der Knopf verschwindet, sobald der Notruf raus ist.
                AddSetActive(ig.onPrimaryInteract, call, false);
                Record(ig);
            }
        }

        // --- AED abgelegt
        RevertOverrides(z.aedPlaced);
        Undo.RecordObject(z.aedPlaced, "aed zone");
        Clear(z.aedPlaced.onTriggerEnter); Clear(z.aedPlaced.onTriggerExit);
        AddVoid(z.aedPlaced.onTriggerEnter, snapAed.Snap);
        AddSetActive(z.aedPlaced.onTriggerEnter, lid, true);
        AddSetActive(z.aedPlaced.onTriggerEnter, pad1, true);
        AddSetActive(z.aedPlaced.onTriggerEnter, pad2, true);
        AddSetActive(z.aedPlaced.onTriggerEnter, b.aedOn, true);
        Record(z.aedPlaced);

        // --- Einschalten
        foreach (var ig in b.aedOn.GetComponentsInChildren<InteractableGeneral>(true))
        {
            RevertOverrides(ig);
            Undo.RecordObject(ig, "aedOn");
            Clear(ig.onPrimaryInteract);
            AddSetActive(ig.onPrimaryInteract, mA, true);
            AddSetActive(ig.onPrimaryInteract, mB, true);
            AddString(ig.onPrimaryInteract, disp.Show, "ATTACH BOTH PADS TO THE MARKS");
            AddSetActive(ig.onPrimaryInteract, b.aedOn, false);
            Record(ig);
        }

        // --- Pads: richtig und falsch
        HookZone(z.padA, snapA.Snap);
        HookZone(z.padB, snapB.Snap);
        HookZone(z.low, rejLow.Snap);
        HookZone(z.central, rejCen.Snap);
        HookZone(z.high, rejHigh.Snap);

        // --- Sicherheitszone
        RevertOverrides(z.safety);
        Undo.RecordObject(z.safety, "safety");
        Clear(z.safety.onTriggerEnter); Clear(z.safety.onTriggerExit);
        AddVoid(z.safety.onTriggerExit, seq.PlayerClear);
        AddVoid(z.safety.onTriggerEnter, seq.PlayerBack);
        Record(z.safety);

        // --- Schock
        foreach (var ig in b.shock.GetComponentsInChildren<InteractableGeneral>(true))
        {
            RevertOverrides(ig);
            Undo.RecordObject(ig, "shock");
            Clear(ig.onPrimaryInteract);
            AddVoid(ig.onPrimaryInteract, disp.StopClock);
            AddString(ig.onPrimaryInteract, disp.Show, "SHOCK DELIVERED - RESUME CPR");
            AddSetActive(ig.onPrimaryInteract, b.shock, false);
            Record(ig);
        }
    }

    static void HookZone(InteractableTrigger z, UnityAction call)
    {
        RevertOverrides(z);
        Undo.RecordObject(z, z.name);
        Clear(z.onTriggerEnter); Clear(z.onTriggerExit);
        AddVoid(z.onTriggerEnter, call);
        Record(z);
    }

    // ========================================================= Hilfsmittel

    static void Clear(UnityEventBase evt)
    {
        if (evt == null) return;
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(evt, i);
    }

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
        if (evt != null) UnityEventTools.AddStringPersistentListener(evt, call, arg);
    }

    static void AddVoid(UnityEvent evt, UnityAction call)
    {
        if (evt != null) UnityEventTools.AddVoidPersistentListener(evt, call);
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
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(parent != null ? parent.transform : null, !local);
        if (local) go.transform.localPosition = pos; else go.transform.position = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        if (!go.activeSelf) { Undo.RecordObject(go, name); go.SetActive(true); }
        if (go.GetComponent<Collider>() == null) Undo.AddComponent<BoxCollider>(go);
        return go;
    }

    // Text ohne skalierte Eltern - sonst wird er verzerrt oder gespiegelt.
    static TextMeshPro Label(string name, Transform parent, Vector3 localPos, float yaw,
                             string text, float size, Color col, Vector2 box)
    {
        var go = Find(name);
        if (go == null) { go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, name); }
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one;

        var tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null) tmp = Undo.AddComponent<TextMeshPro>(go);
        Undo.RecordObject(tmp, name);
        tmp.text = text; tmp.fontSize = size; tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = box;
        Record(tmp);
        return tmp;
    }

    static TextMeshPro Billboard(string name, Transform parent, Vector3 localPos,
                                 string text, float size, Color col, Vector2 box)
    {
        var tmp = Label(name, parent, localPos, 0f, text, size, col, box);
        Ensure<A1Billboard>(tmp.gameObject);
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
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(root.transform, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = scale;
        if (!go.activeSelf) { Undo.RecordObject(go, name); go.SetActive(true); }

        var it = Ensure<InteractableTrigger>(go);
        RevertOverrides(it);
        Undo.RecordObject(it, name);
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
        Undo.RecordObject(f, go.name);
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
            Undo.RecordObject(go, go.name);
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
