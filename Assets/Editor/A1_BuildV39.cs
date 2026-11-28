// A1_BuildV39.cs
// Menue:  Assets > AED Build V39   (laeuft einmalig auch von selbst)
//
// Zwei Aenderungen fuer v4:
//
// [s1] Die blauen Zielflaechen auf dem Koerper erscheinen nicht mehr von
//      selbst. Der Transfer von der Abbildung auf den Koerper ist das
//      Lernziel - in v3 wurde er verschenkt. Ein Klickfeld "HINT" neben
//      dem Patienten blendet sie auf Anfrage ein. Wer den Hinweis
//      anfordert, erzeugt damit eine neue Messgroesse.
//
// [s2] Ein Overlay beim Start erklaert die Steuerung und nichts weiter.
//      Bisher floss die Zeit, in der sich jemand mit Maus und Tastatur
//      zurechtfand, in die Messung ein - das ist Rauschen, kein Befund
//      ueber die Aufgabe. Das Overlay verschwindet mit der Leertaste,
//      erst danach laeuft die Uhr. Das Start-Schild im Raum bleibt als
//      zweiter Weg erhalten.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV39
{
    const string MENU = "Assets/AED Build V39";
    const string DIR  = "Assets/__My Project/";

    // Seitlich neben dem Patienten (der liegt bei 0 | 0,15 | 3,4), auf
    // Huefthoehe, damit das Feld beim Pad-Setzen im Blick ist.
    static readonly Vector3 HINT_AT    = new Vector3(1.25f, 0.45f, 3.40f);
    static readonly Vector3 PLATE_SIZE = new Vector3(0.05f, 0.20f, 0.46f);

    static Material matBase;

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V39_done";
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
        Undo.SetCurrentGroupName("Build V39");

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null) { Debug.LogError("[V39] Kein Referenzmaterial - abgebrochen."); return; }

        GameObject area = Find("A2_Patient_Area");
        A2Flow flow = area != null ? area.GetComponent<A2Flow>() : null;
        if (flow == null) { Debug.LogError("[V39] A2Flow fehlt - abgebrochen."); return; }

        flow.hintButton = BuildHint(area, flow);
        flow.introPanel = BuildIntro();

        Undo.RecordObject(flow, "A2Flow");
        EditorUtility.SetDirty(flow);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V39] Fertig. HINT-Feld neben dem Patienten, Start-Overlay angelegt. " +
                  "Szene speichern mit Ctrl+S.");
    }

    // --- [s1] Klickfeld "HINT" ------------------------------------------
    // Aufbau wie beim AED-Klickfeld: ein Trager mit Skalierung 1, damit
    // die Schrift nicht verzerrt. Die Platte traegt den Collider, das
    // Label haengt daneben, nicht darin.
    static GameObject BuildHint(GameObject area, A2Flow flow)
    {
        GameObject old = Find("Hint_Button");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject root = new GameObject("Hint_Button");
        Undo.RegisterCreatedObjectUndo(root, "Hint_Button");
        root.transform.SetParent(area.transform, true);
        root.transform.position = HINT_AT;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Plate";
        plate.transform.SetParent(root.transform, false);
        plate.transform.localPosition = Vector3.zero;
        plate.transform.localRotation = Quaternion.identity;
        plate.transform.localScale = PLATE_SIZE;

        Renderer pr = plate.GetComponent<Renderer>();
        if (pr != null)
            pr.sharedMaterial = Mat("Mat_Hint",
                                    new Color(1.00f, 0.72f, 0.15f),
                                    new Color(0.45f, 0.28f, 0.03f));

        InteractableGeneral inter = plate.AddComponent<InteractableGeneral>();
        inter.hoverText = "SHOW ME WHERE";
        if (inter.onPrimaryInteract == null) inter.onPrimaryInteract = new UnityEvent();
        UnityEventTools.AddPersistentListener(inter.onPrimaryInteract, (UnityAction)flow.ShowHint);

        GameObject label = new GameObject("Label");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(-0.06f, 0f, 0f);
        label.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        label.transform.localScale = Vector3.one;

        TextMeshPro t = label.AddComponent<TextMeshPro>();
        t.text = "HINT";
        t.fontSize = 1.1f;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        RectTransform rt = label.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(0.9f, 0.3f);

        root.SetActive(false);
        Debug.Log("[V39] Hint_Button bei " + HINT_AT.ToString("0.00") + ".");
        return root;
    }

    // --- [s2] Start-Overlay ---------------------------------------------
    // Bildschirmfuellend, damit niemand daran vorbeilaeuft. Kein Knopf:
    // der Player-Controller sperrt den Mauszeiger, deshalb Leertaste.
    static GameObject BuildIntro()
    {
        GameObject old = Find("Intro_Panel");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject canvasGo = new GameObject("Intro_Panel");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Intro_Panel");

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasGo.transform, false);
        Image img = bg.AddComponent<Image>();
        img.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);
        RectTransform br = bg.GetComponent<RectTransform>();
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(bg.transform, false);
        TextMeshProUGUI t = textGo.AddComponent<TextMeshProUGUI>();
        t.text =
            "<size=150%><b>BEFORE YOU START</b></size>\n\n" +
            "This takes about five minutes.\n\n" +
            "<b>Move</b>   W A S D  or the arrow keys\n" +
            "<b>Look</b>   move the mouse\n" +
            "<b>Use</b>    left mouse button to pick things up and press things\n\n" +
            "That is all you need to know about the controls.\n" +
            "What to do once you are inside is for you to work out —\n" +
            "that is the part being tested, not you.\n\n" +
            "<size=130%><b>Press SPACE to begin</b></size>";
        t.fontSize = 34f;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;

        RectTransform tr = textGo.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.5f, 0.5f);
        tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.pivot     = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = Vector2.zero;
        tr.sizeDelta = new Vector2(1100f, 700f);

        Debug.Log("[V39] Intro_Panel angelegt (Leertaste zum Starten).");
        return canvasGo;
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
