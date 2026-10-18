// A1_BuildV6.cs
// Menue:  Tools > AED Prototype > Build V6
//
// Zwei Korrekturen:
//   1 Display am AED: Tafel und Schriftzug lagen in verschiedenen Ebenen,
//     deshalb stand der Text quer zur Tafel. Tafel steht jetzt hochkant
//     laengs zum Geraet, und der Text haengt beidseitig an ihr - damit ist
//     er von links wie von rechts lesbar, ohne mitzudrehen.
//   2 Notruf-Meldung: Info_Call haengt in der Button-Prefab und wurde mit
//     deren Teilen ausgeblendet. Die Meldung ist jetzt ein eigenes Objekt
//     ueber dem Telefon, das niemand mit abschaltet.

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

public static class A1_BuildV6
{
    const string MENU = "Tools/AED Prototype/Build V6";

    const string MSG_CALL = "HELP IS ON THE WAY. GET AED NOW.";

    static GameObject root;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V6");

        root = Find("====A1 CHANGES====");
        var unit = Find("AED_Unit");
        if (root == null || unit == null)
        {
            Debug.LogError("[A1] Bitte zuerst Build V4 und V5 ausfuehren."); return;
        }

        var matDark = FindMaterial("Mat_A1_Dark");

        // =============================================== 1 Display am Geraet
        // Tafel hochkant, duenn in x: ihre Flaechen zeigen nach +x und -x,
        // also zu beiden Seiten, an denen der Helfer stehen kann.
        var panel = Shape("AED_Panel", new Vector3(0f, 0.42f, 0f),
                          new Vector3(0.03f, 0.30f, 0.95f), matDark, unit);
        StripCollider(panel);

        // Alten, quer stehenden Text abschalten.
        var oldPivot = Find("AED_Panel_Pivot");
        if (oldPivot != null) SetActive(oldPivot, false);

        // Regel: die lesbare Seite eines TMP-Textes ist sein lokales -z.
        // Fuer einen Betrachter bei +x muss -z nach +x zeigen  -> yaw -90.
        // Fuer einen Betrachter bei -x muss -z nach -x zeigen  -> yaw +90.
        var tR = Label("AED_Panel_Text_R", unit.transform,
                       new Vector3(0.022f, 0.42f, 0f), -90f, "", 0.26f,
                       Color.white, new Vector2(0.90f, 0.28f));
        var tL = Label("AED_Panel_Text_L", unit.transform,
                       new Vector3(-0.022f, 0.42f, 0f), 90f, "", 0.26f,
                       Color.white, new Vector2(0.90f, 0.28f));

        var disp = Ensure<AEDDisplay>(unit);
        Undo.RecordObject(disp, "display");
        disp.instructionTexts = new[] { tR, tL };
        var clockGo = Find("Clock_Value");
        if (clockGo != null) disp.clockTexts = new[] { clockGo.GetComponent<TextMeshPro>() };
        EditorUtility.SetDirty(disp);

        SetActive(panel, false);
        SetActive(tR.gameObject, false);
        SetActive(tL.gameObject, false);

        // ========================================== 2 Meldung ueber dem Telefon
        // Eigenstaendig, nicht Teil der Button-Prefab.
        var plate = Shape("Call_Feedback_Plate", new Vector3(-1.6f, 1.38f, 1.12f),
                          new Vector3(1.25f, 0.30f, 0.03f), matDark, root);
        StripCollider(plate);

        // Spieler kommt von kleineren z-Werten  -> -z muss nach -z zeigen -> yaw 0.
        var fb = Label("Call_Feedback_Text", root.transform,
                       new Vector3(-1.6f, 1.38f, 1.095f), 0f, MSG_CALL, 0.26f,
                       Color.white, new Vector2(1.20f, 0.28f));

        SetActive(plate, false);
        SetActive(fb.gameObject, false);

        var info = Find("Info_Call");
        if (info != null) SetActive(info, false);

        // ====================================================== Verdrahtung
        var call = Find("Button_Call_000");
        if (call != null)
        {
            foreach (var ig in call.GetComponentsInChildren<InteractableGeneral>(true))
            {
                Undo.RecordObject(ig, "call");
                Clear(ig.onPrimaryInteract);
                UnityEventTools.AddVoidPersistentListener(ig.onPrimaryInteract,
                    new UnityAction(disp.StartClock));
                AddSetActive(ig.onPrimaryInteract, plate, true);
                AddSetActive(ig.onPrimaryInteract, fb.gameObject, true);
                AddString(ig.onPrimaryInteract, disp.Show, MSG_CALL);
                foreach (var part in new[] { "Graphics", "TMPCanvas", "InteractionBox", "Text (TMP)" })
                {
                    var child = FindRecursive(call.transform, part);
                    if (child != null) AddSetActive(ig.onPrimaryInteract, child, false);
                }
                Record(ig);
            }
        }

        // Display erscheint mit dem abgelegten Geraet.
        var zone = Find("Zone_AED_Placed");
        if (zone != null)
        {
            var it = zone.GetComponent<InteractableTrigger>();
            if (it != null)
            {
                Undo.RecordObject(it, "zone");
                AddSetActive(it.onTriggerEnter, panel, true);
                AddSetActive(it.onTriggerEnter, tR.gameObject, true);
                AddSetActive(it.onTriggerEnter, tL.gameObject, true);
                Record(it);
            }
        }

        Undo.CollapseUndoOperations(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[A1] Build V6 fertig.");
    }

    // ============================================================ Hilfsmittel

    static GameObject Shape(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
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
        bool local = parent != null && parent.name == "AED_Unit";
        go.transform.SetParent(parent != null ? parent.transform : null, !local);
        if (local) go.transform.localPosition = pos; else go.transform.position = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        if (go.GetComponent<Collider>() == null) Undo.AddComponent<BoxCollider>(go);
        return go;
    }

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

        var bb = go.GetComponent<A1Billboard>();
        if (bb != null) Undo.DestroyObjectImmediate(bb);

        Record(tmp);
        return tmp;
    }

    static void Clear(UnityEventBase evt)
    {
        if (evt == null) return;
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(evt, i);
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
}
