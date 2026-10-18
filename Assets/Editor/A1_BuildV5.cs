// A1_BuildV5.cs
// Menue:  Tools > AED Prototype > Build V5
//
// Aenderungen gegenueber V4:
//   - Telefon schwarz
//   - Notruf: Feedbacksatz, Knopf verschwindet komplett
//   - Pads werden nicht mehr in ein Volumen getragen, sondern an
//     anklickbaren Stellen platziert (das war mit Maus kaum zu treffen)
//   - Display wieder AM Geraet, mit fester Ausrichtung statt mitdrehend
//   - alle mitdrehenden Billboards raus; Ausrichtung nach der belegten Regel:
//     die LESBARE Seite eines TMP-Textes ist seine lokale -z-Seite

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

public static class A1_BuildV5
{
    const string MENU = "Tools/AED Prototype/Build V5";

    static readonly Vector3 AED_ANCHOR = new Vector3(1.45f, 0.18f, 3.0f);

    // Stellen auf der Brust. Patient: x -0.25..0.25, Oberseite y = 0.30,
    // Kopf bei z = 4. Patientenrechts = +x.
    struct Spot
    {
        public string name; public Vector3 pos; public bool ok; public string msg;
        public Spot(string n, Vector3 p, bool o, string m) { name = n; pos = p; ok = o; msg = m; }
    }

    static readonly Spot[] SPOTS =
    {
        new Spot("Spot_Correct_A", new Vector3( 0.15f, 0.305f, 3.60f), true,
                 "PAD CORRECT - BELOW THE RIGHT COLLARBONE"),
        new Spot("Spot_Correct_B", new Vector3(-0.17f, 0.305f, 3.15f), true,
                 "PAD CORRECT - BELOW THE LEFT ARMPIT"),
        new Spot("Spot_Wrong_Low", new Vector3( 0.00f, 0.305f, 2.70f), false,
                 "TOO LOW - THAT IS THE BELLY, NOT THE CHEST"),
        new Spot("Spot_Wrong_Mid", new Vector3( 0.00f, 0.305f, 3.40f), false,
                 "TOO CENTRAL - THE PADS MUST NOT TOUCH"),
        new Spot("Spot_Wrong_High",new Vector3( 0.00f, 0.305f, 3.90f), false,
                 "TOO HIGH - PLACE IT BELOW THE COLLARBONE"),
    };

    static GameObject root;
    static Material matDark, matMarker, matGreen;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V5");

        root = Find("====A1 CHANGES====");
        if (root == null) { Debug.LogError("[A1] Bitte zuerst Build V4 ausfuehren."); return; }

        matDark = FindMaterial("Mat_A1_Dark");
        matMarker = FindMaterial("Mat_Marker");
        matGreen = FindMaterial("Mat_A1_Good");
        var matBlack = MakeMaterial("Mat_A1_Black", new Color(0.09f, 0.09f, 0.10f), matDark);

        // ---------------------------------------------------- 1 Telefon schwarz
        foreach (var n in new[] { "Phone", "Table_Phone" })
        {
            var go = Find(n);
            if (go == null) continue;
            var r = go.GetComponent<Renderer>();
            if (r == null) continue;
            Undo.RecordObject(r, n + " schwarz");
            r.sharedMaterial = n == "Phone" ? matBlack : matDark;
        }

        // ------------------------------------- 2 mitdrehende Schilder abschalten
        foreach (var bb in Object.FindObjectsByType<A1Billboard>(FindObjectsSortMode.None))
            Undo.DestroyObjectImmediate(bb);

        // Feste Ausrichtung nach der Regel: lesbare Seite = lokales -z.
        SetYaw("Clock_Texts", 0f);          // Nordwand, Spieler davor
        SetYaw("Sign_Cabinet_Pivot", -90f); // Westwand, Spieler oestlich davon

        // Kopftafel entfaellt - das Display sitzt wieder am Geraet.
        var head = Find("HeadPanel");
        if (head != null) SetActive(head, false);

        // ---------------------------------------------- 3 Display zurueck ans AED
        var unit = Find("AED_Unit");
        if (unit == null) { Debug.LogError("[A1] AED_Unit fehlt."); return; }

        var panel = Shape("AED_Panel", new Vector3(0f, 0.42f, 0f),
                          new Vector3(0.95f, 0.30f, 0.03f), matDark, unit, true);
        StripCollider(panel);

        var pivot = Child("AED_Panel_Pivot", unit.transform, new Vector3(0f, 0.42f, -0.025f), -90f);
        var text = Label("AED_Panel_Text", pivot, Vector3.zero,
                         "", 0.28f, Color.white, new Vector2(0.90f, 0.28f));

        var disp = Ensure<AEDDisplay>(unit);
        Undo.RecordObject(disp, "display");
        disp.instructionTexts = new[] { text };
        var clock = Find("Clock_Value");
        if (clock != null) disp.clockTexts = new[] { clock.GetComponent<TextMeshPro>() };
        EditorUtility.SetDirty(disp);

        // Panel erscheint erst, wenn das Geraet abgelegt ist.
        SetActive(panel, false);
        SetActive(pivot.gameObject, false);

        // ------------------------------------------- 4 anklickbare Pad-Stellen
        var seq = Find("A1_Sequence").GetComponent<A1Sequence>();
        var spots = BuildSpots(disp, seq);

        // Alte Pad-Trigger abschalten, sonst greifen zwei Mechaniken ineinander.
        foreach (var n in new[] { "Target_Pad_A", "Target_Pad_B", "Wrong_TooLow",
                                  "Wrong_TooCentral", "Wrong_TooHigh",
                                  "Marker_Pad_A", "Marker_Pad_B" })
        {
            var go = Find(n);
            if (go != null) SetActive(go, false);
        }

        // ------------------------------------------------------- 5 Verdrahtung
        Wire(disp, seq, panel, pivot.gameObject, spots);

        Undo.CollapseUndoOperations(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[A1] Build V5 fertig.");
    }

    // ============================================================== Stellen

    static GameObject[] BuildSpots(AEDDisplay disp, A1Sequence seq)
    {
        var made = new System.Collections.Generic.List<GameObject>();
        int correctIndex = 0;

        foreach (var s in SPOTS)
        {
            // Flache, gut anklickbare Platte auf der Brust.
            var go = Shape(s.name, s.pos, new Vector3(0.20f, 0.012f, 0.22f),
                           s.ok ? matMarker : matMarker, root);

            // Collider deutlich hoeher als die Platte, damit der Strahl trifft.
            var bc = go.GetComponent<BoxCollider>();
            if (bc != null) { Undo.RecordObject(bc, s.name); bc.size = new Vector3(1f, 8f, 1f); }

            var t = Ensure<A1PadTarget>(go);
            Undo.RecordObject(t, s.name);
            t.display = disp;
            t.sequence = s.ok ? seq : null;
            t.correct = s.ok;
            t.message = s.msg;
            t.used = false;
            t.markerWhenPlaced = go;

            if (s.ok)
            {
                var anchorPos = s.pos + new Vector3(0f, 0.02f, 0f);
                t.anchor = Anchor("Anchor_" + s.name, anchorPos);
                correctIndex++;
            }

            var ig = Ensure<InteractableGeneral>(go);
            Undo.RecordObject(ig, s.name + " click");
            ig.hoverText = "Place pad here";
            Clear(ig.onPrimaryInteract);
            UnityEventTools.AddVoidPersistentListener(ig.onPrimaryInteract, new UnityAction(t.Place));
            EditorUtility.SetDirty(ig);
            EditorUtility.SetDirty(t);

            SetActive(go, false);
            made.Add(go);
        }
        return made.ToArray();
    }

    // =========================================================== Verdrahtung

    static void Wire(AEDDisplay disp, A1Sequence seq, GameObject panel,
                     GameObject pivot, GameObject[] spots)
    {
        var call = Find("Button_Call_000");
        var info = Find("Info_Call");
        var zone = Find("Zone_AED_Placed");
        var btnOn = Find("Button_AED_On");

        // --- Notruf
        if (info != null)
        {
            foreach (var t in info.GetComponentsInChildren<TextMeshPro>(true))
            {
                Undo.RecordObject(t, "info");
                t.text = "HELP IS ON THE WAY. GET AED NOW.";
                Record(t);
            }
        }

        if (call != null)
        {
            foreach (var ig in call.GetComponentsInChildren<InteractableGeneral>(true))
            {
                RevertOverrides(ig);
                Undo.RecordObject(ig, "call");
                Clear(ig.onPrimaryInteract);
                UnityEventTools.AddVoidPersistentListener(ig.onPrimaryInteract,
                    new UnityAction(disp.StartClock));
                if (info != null) AddSetActive(ig.onPrimaryInteract, info, true);
                AddString(ig.onPrimaryInteract, disp.Show, "HELP IS ON THE WAY. GET AED NOW.");
                // Der ganze Knopf verschwindet. Info_Call haengt daran, wird
                // aber von ItemActiveToggle wieder sichtbar gehalten - deshalb
                // haengen wir es vorher um, soweit Unity das zulaesst.
                foreach (var part in new[] { "Graphics", "TMPCanvas", "InteractionBox", "Text (TMP)" })
                {
                    var child = FindRecursive(call.transform, part);
                    if (child != null && child != info)
                        AddSetActive(ig.onPrimaryInteract, child, false);
                }
                Record(ig);
            }
        }

        // --- AED abgelegt: Display einschalten
        if (zone != null)
        {
            var it = zone.GetComponent<InteractableTrigger>();
            if (it != null)
            {
                Undo.RecordObject(it, "zone");
                AddSetActive(it.onTriggerEnter, panel, true);
                AddSetActive(it.onTriggerEnter, pivot, true);
                Record(it);
            }
        }

        // --- Geraet einschalten: Stellen zeigen
        if (btnOn != null)
        {
            foreach (var ig in btnOn.GetComponentsInChildren<InteractableGeneral>(true))
            {
                Undo.RecordObject(ig, "aedOn");
                Clear(ig.onPrimaryInteract);
                foreach (var sp in spots) AddSetActive(ig.onPrimaryInteract, sp, true);
                AddString(ig.onPrimaryInteract, disp.Show,
                          "HOLD A PAD AND CLICK WHERE IT GOES");
                AddSetActive(ig.onPrimaryInteract, btnOn, false);
                Record(ig);
            }
        }
    }

    // ============================================================ Hilfsmittel

    static void SetYaw(string name, float yaw)
    {
        var go = Find(name);
        if (go == null) return;
        Undo.RecordObject(go.transform, name);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    static Transform Child(string name, Transform parent, Vector3 localPos, float yaw)
    {
        var go = Find(name);
        if (go == null) { go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, name); }
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    static Transform Anchor(string name, Vector3 pos)
    {
        var go = Find(name);
        if (go == null) { go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, name); }
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(root.transform, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        return go.transform;
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
        if (go.GetComponent<Collider>() == null) Undo.AddComponent<BoxCollider>(go);
        return go;
    }

    static TextMeshPro Label(string name, Transform parent, Vector3 localPos,
                             string text, float size, Color col, Vector2 box)
    {
        var go = Find(name);
        if (go == null) { go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, name); }
        Undo.RecordObject(go.transform, name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
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

    static void Record(Object o)
    {
        EditorUtility.SetDirty(o);
        PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }

    static void StripCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Undo.DestroyObjectImmediate(c);
    }

    static void SetActive(GameObject go, bool state)
    {
        if (go != null && go.activeSelf != state)
        {
            Undo.RecordObject(go, go.name);
            go.SetActive(state);
        }
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
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

    static Material MakeMaterial(string name, Color c, Material template)
    {
        var existing = FindMaterial(name);
        if (existing != null) return existing;
        Shader sh = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { name = name };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        AssetDatabase.CreateAsset(m, "Assets/__My Project/" + name + ".mat");
        AssetDatabase.SaveAssets();
        return m;
    }
}
