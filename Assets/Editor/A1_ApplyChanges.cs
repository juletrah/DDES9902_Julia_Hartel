// A1_ApplyChanges.cs
// Baut die vier ueberarbeiteten Interaktionen in A1_Prototype ein.
// Menue:  Tools > AED Prototype > Apply A1 Changes
//
// Das Skript ist idempotent: zweimal ausfuehren aendert nichts zusaetzlich.
// Alles laeuft ueber Undo, also ist Strg+Z nach dem Ausfuehren moeglich.
//
// Legt an / veraendert:
//   1  AED_Cabinet + AED_Sign an der Suedwand, AED + Pads ziehen dorthin um
//      -> der AED muss gesucht und geholt werden
//   2  Zone_AED_Placed neben dem Patienten  -> schaltet Button_AED_On frei
//   3  Target_Pad_A / Target_Pad_B / Zone_Pad_TooLow -> Qualitaet statt richtig/falsch
//   4  Obstacle_Chair + Zone_Access -> Zugang schaffen
//   5  Zone_Safety + Button_Shock -> Schock nur von ausserhalb der Zone

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;

public static class A1_ApplyChanges
{
    const string MENU = "Tools/AED Prototype/Apply A1 Changes";

    // ------------------------------------------------------------------ Pfade
    const string P_AREA_TRIGGER =
        "Assets/EZPZ Interaction Toolkit/Prefabs/_General Interactables/EZPZ Area Trigger.prefab";
    const string P_BUTTON =
        "Assets/EZPZ Interaction Toolkit/Prefabs/_General Interactables/EZPZ Button Interactable.prefab";

    // --------------------------------------------------------------- Geometrie
    // Patient liegt mit dem Kopf bei z = 4, Koerperoberseite bei y = 0.3.
    // Patientenrechts = +x, Patientenlinks = -x.
    static readonly Vector3 CABINET_POS = new Vector3(-2.2f, 1.20f, -4.78f);
    static readonly Vector3 SIGN_POS    = new Vector3(-2.2f, 1.72f, -4.76f);
    static readonly Vector3 AED_POS     = new Vector3(-2.2f, 1.20f, -4.55f);
    static readonly Vector3 PAD1_POS    = new Vector3(-2.48f, 0.90f, -4.60f);
    static readonly Vector3 PAD2_POS    = new Vector3(-1.92f, 0.90f, -4.60f);

    [MenuItem(MENU)]
    public static void Apply()
    {
        var root = GameObject.Find("====A1 CHANGES====");
        if (root == null)
        {
            root = new GameObject("====A1 CHANGES====");
            Undo.RegisterCreatedObjectUndo(root, "A1 root");
        }

        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply A1 Changes");

        var areaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P_AREA_TRIGGER);
        var btnPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(P_BUTTON);
        if (areaPrefab == null || btnPrefab == null)
        {
            Debug.LogError("[A1] EZPZ-Prefabs nicht gefunden. Pfade im Skript pruefen.");
            return;
        }

        var matMarker = FindMaterial("Mat_Marker");
        var matGreen  = MakeMaterial("Mat_A1_Good",   new Color(0.25f, 0.80f, 0.35f), matMarker);
        var matRed    = MakeMaterial("Mat_A1_TooLow", new Color(0.85f, 0.25f, 0.25f), matMarker);
        var matBox    = FindMaterial("Mat_Furniture");

        // ============================================================== 1 AED
        var cabinet = Box(root, "AED_Cabinet", CABINET_POS,
                          new Vector3(0.75f, 0.65f, 0.22f), matBox);
        Box(root, "AED_Sign", SIGN_POS,
            new Vector3(0.40f, 0.30f, 0.03f), matGreen);

        // Leerer Trager, damit beim Tragen nichts verzerrt wird.
        var carry = Find("AED_Carry");
        if (carry == null)
        {
            carry = new GameObject("AED_Carry");
            Undo.RegisterCreatedObjectUndo(carry, "AED_Carry");
            carry.transform.SetParent(root.transform, true);
        }
        Undo.RecordObject(carry.transform, "AED_Carry pos");
        carry.transform.position = AED_POS;
        carry.transform.localScale = Vector3.one;

        var aed     = Find("AED_Device");
        var display = Find("AED_Display");

        if (aed != null)
        {
            Reparent(aed, carry, new Vector3(0f, 0f, 0f));
            EnsureFilter(aed, "AED");
        }
        if (display != null)
            Reparent(display, carry, new Vector3(0f, 0.17f, 0f));

        // Holdable sitzt auf dem Traeger, nicht auf dem Mesh.
        EnsureCollider(carry, new Vector3(0.5f, 0.45f, 0.4f));
        var aedHoldable = Ensure<Holdable>(carry);
        Undo.RecordObject(aedHoldable, "AED holdable");
        aedHoldable.groundPlace = true;
        aedHoldable.dropOnKeyLift = false;
        EnsureFilter(carry, "AED");

        // Pads werden Holdables und wandern in den Schrank.
        MakePadHoldable("Pad_1", PAD1_POS);
        MakePadHoldable("Pad_2", PAD2_POS);

        // Alter AED-Tisch wird nicht mehr gebraucht.
        var tableAed = Find("Table_AED");
        if (tableAed != null && tableAed.activeSelf)
        {
            Undo.RecordObject(tableAed, "Table_AED aus");
            tableAed.SetActive(false);
        }

        // ================================================= 2 AED abgelegt
        var zAed = Area(areaPrefab, root, "Zone_AED_Placed",
                        new Vector3(0.78f, 0.30f, 3.00f),
                        new Vector3(0.80f, 0.60f, 1.00f),
                        "AED", false);

        var btnOn = Find("Button_AED_On");
        if (btnOn != null)
        {
            Undo.RecordObject(btnOn.transform, "Button_AED_On pos");
            btnOn.transform.position = new Vector3(0.78f, 0.62f, 2.55f);
            SetActiveStart(btnOn, false);
            WireSetActive(zAed.onTriggerEnter, btnOn, true);
        }

        // =============================================== 3 Pad-Qualitaet
        var padA = Area(areaPrefab, root, "Target_Pad_A",
                        new Vector3(0.14f, 0.33f, 3.62f),
                        new Vector3(0.22f, 0.16f, 0.22f),
                        "PAD", false);

        var padB = Area(areaPrefab, root, "Target_Pad_B",
                        new Vector3(-0.24f, 0.28f, 3.30f),
                        new Vector3(0.18f, 0.18f, 0.28f),
                        "PAD", false);

        var padLow = Area(areaPrefab, root, "Zone_Pad_TooLow",
                          new Vector3(0f, 0.33f, 3.02f),
                          new Vector3(0.52f, 0.16f, 0.44f),
                          "PAD", false);

        // Sichtbare Zielmarken auf der Brust (flache Platten).
        var mA = Box(root, "Marker_Pad_A", new Vector3(0.14f, 0.305f, 3.62f),
                     new Vector3(0.18f, 0.01f, 0.18f), matMarker);
        var mB = Box(root, "Marker_Pad_B", new Vector3(-0.235f, 0.28f, 3.30f),
                     new Vector3(0.01f, 0.14f, 0.24f), matMarker);
        StripCollider(mA);
        StripCollider(mB);

        // Rueckmeldungslampen am AED-Display.
        var lampGood = Box(root, "Lamp_Pad_Good", new Vector3(-0.12f, 0.45f, 2.55f),
                           new Vector3(0.12f, 0.12f, 0.04f), matGreen);
        var lampLow  = Box(root, "Lamp_Pad_TooLow", new Vector3(0.12f, 0.45f, 2.55f),
                           new Vector3(0.12f, 0.12f, 0.04f), matRed);
        StripCollider(lampGood);
        StripCollider(lampLow);
        SetActiveStart(lampGood, false);
        SetActiveStart(lampLow, false);

        WireSetActive(padA.onTriggerEnter, lampGood, true);
        WireSetActive(padB.onTriggerEnter, lampGood, true);
        WireSetActive(padLow.onTriggerEnter, lampLow, true);
        WireSetActive(padLow.onTriggerExit, lampLow, false);

        // Ihre alte, binaere Brustzone wird abgeschaltet.
        var zChest = Find("Zone_Chest");
        if (zChest != null && zChest.activeSelf)
        {
            Undo.RecordObject(zChest, "Zone_Chest aus");
            zChest.SetActive(false);
        }

        // ================================================= 4 Zugang schaffen
        var chair = Box(root, "Obstacle_Chair", new Vector3(-0.85f, 0.26f, 3.20f),
                        new Vector3(0.52f, 0.52f, 0.52f), matBox);
        var chairHold = Ensure<Holdable>(chair);
        Undo.RecordObject(chairHold, "chair holdable");
        chairHold.groundPlace = true;
        EnsureFilter(chair, "OBSTACLE");

        var zAccess = Area(areaPrefab, root, "Zone_Access",
                           new Vector3(-0.85f, 0.32f, 3.20f),
                           new Vector3(0.85f, 0.75f, 0.85f),
                           "OBSTACLE", false);

        // Brustmarken erscheinen erst, wenn der Zugang frei ist.
        SetActiveStart(mA, false);
        SetActiveStart(mB, false);
        WireSetActive(zAccess.onTriggerExit, mA, true);
        WireSetActive(zAccess.onTriggerExit, mB, true);

        // ================================================ 5 Sicherheitszone
        var zSafety = Area(areaPrefab, root, "Zone_Safety",
                           new Vector3(0f, 1.00f, 3.10f),
                           new Vector3(2.60f, 2.00f, 2.90f),
                           "PLAYER", false);

        var player = Find("EZPZ Player Flat Screen WASD");
        if (player != null)
            EnsureFilter(player, "PLAYER");
        else
            Debug.LogWarning("[A1] Player nicht gefunden - TriggerFilter 'PLAYER' bitte manuell setzen.");

        var btnShock = Find("Button_Shock");
        if (btnShock == null)
        {
            btnShock = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab);
            btnShock.name = "Button_Shock";
            Undo.RegisterCreatedObjectUndo(btnShock, "Button_Shock");
            btnShock.transform.SetParent(root.transform, true);
        }
        Undo.RecordObject(btnShock.transform, "Button_Shock pos");
        btnShock.transform.position = new Vector3(0.78f, 0.95f, 3.00f);
        SetActiveStart(btnShock, false);

        WireSetActive(zSafety.onTriggerExit, btnShock, true);
        WireSetActive(zSafety.onTriggerEnter, btnShock, false);

        Undo.CollapseUndoOperations(group);
        EditorSceneManagerMarkDirty();

        Debug.Log("[A1] Fertig. Siehe '====A1 CHANGES====' in der Hierarchie. " +
                  "Offen bleibt: Button_AED_On und Button_Shock mit ihren eigenen Aktionen " +
                  "verdrahten (Display-Text, Zaehler).");
    }

    // ------------------------------------------------------------- Hilfsmittel

    static void EditorSceneManagerMarkDirty()
    {
        var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
    }

    // Durchsucht die aktive Szene inklusive deaktivierter Objekte.
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

    static GameObject FindUnder(GameObject parent, string n)
    {
        var t = parent.transform.Find(n);
        return t == null ? null : t.gameObject;
    }

    static GameObject Box(GameObject parent, string name, Vector3 pos,
                          Vector3 scale, Material mat)
    {
        var go = FindUnder(parent, name) ?? Find(name);
        if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, name);
        }
        Undo.RecordObject(go.transform, name + " transform");
        go.transform.SetParent(parent.transform, true);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.transform.rotation = Quaternion.identity;

        if (mat != null)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                Undo.RecordObject(r, name + " mat");
                r.sharedMaterial = mat;
            }
        }
        return go;
    }

    static void StripCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Undo.DestroyObjectImmediate(c);
    }

    static InteractableTrigger Area(GameObject prefab, GameObject parent, string name,
                                    Vector3 pos, Vector3 scale,
                                    string filter, bool allowUnfiltered)
    {
        var go = FindUnder(parent, name) ?? Find(name);
        if (go == null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, name);
        }
        Undo.RecordObject(go.transform, name + " transform");
        go.transform.SetParent(parent.transform, true);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.transform.rotation = Quaternion.identity;

        var it = Ensure<InteractableTrigger>(go);
        Undo.RecordObject(it, name + " trigger");
        it.filterString = filter;
        it.allowUnfiltered = allowUnfiltered;
        it.excludeMode = false;
        it.deleteOnEnter = false;
        it.disableOnEnter = false;
        it.visibleAtRuntime = false;
        return it;
    }

    static void MakePadHoldable(string padName, Vector3 pos)
    {
        var pad = Find(padName);
        if (pad == null)
        {
            Debug.LogWarning("[A1] " + padName + " nicht gefunden.");
            return;
        }
        Undo.RecordObject(pad.transform, padName + " pos");
        pad.transform.position = pos;

        EnsureCollider(pad, Vector3.one);
        var h = Ensure<Holdable>(pad);
        Undo.RecordObject(h, padName + " holdable");
        h.groundPlace = true;
        h.dropOnKeyLift = false;
        EnsureFilter(pad, "PAD");
    }

    static void Reparent(GameObject go, GameObject parent, Vector3 localPos)
    {
        Undo.RecordObject(go.transform, go.name + " reparent");
        var scale = go.transform.localScale;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
    }

    static Collider EnsureCollider(GameObject go, Vector3 size)
    {
        var c = go.GetComponent<Collider>();
        if (c == null)
        {
            var b = Undo.AddComponent<BoxCollider>(go);
            b.size = size;
            c = b;
        }
        return c;
    }

    static void EnsureFilter(GameObject go, string s)
    {
        var f = Ensure<TriggerFilter>(go);
        Undo.RecordObject(f, go.name + " filter");
        f.filterString = s;
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    static void SetActiveStart(GameObject go, bool state)
    {
        if (go.activeSelf != state)
        {
            Undo.RecordObject(go, go.name + " active");
            go.SetActive(state);
        }
    }

    // Haengt go.SetActive(state) als persistenten Listener an das Event,
    // ohne Duplikate bei mehrfachem Ausfuehren.
    static void WireSetActive(UnityEvent evt, GameObject go, bool state)
    {
        if (evt == null || go == null) return;

        for (int i = 0; i < evt.GetPersistentEventCount(); i++)
        {
            if (evt.GetPersistentTarget(i) == go &&
                evt.GetPersistentMethodName(i) == "SetActive")
                return;
        }
        UnityEventTools.AddBoolPersistentListener(
            evt, new UnityAction<bool>(go.SetActive), state);
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

    static Material MakeMaterial(string name, Color c, Material template)
    {
        var existing = FindMaterial(name);
        if (existing != null) return existing;

        Shader sh = template != null ? template.shader
                                     : Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        m.name = name;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);

        const string dir = "Assets/__My Project";
        AssetDatabase.CreateAsset(m, dir + "/" + name + ".mat");
        AssetDatabase.SaveAssets();
        return m;
    }
}
