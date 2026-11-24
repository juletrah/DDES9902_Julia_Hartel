// A1_BuildV10.cs
// Menue:  Tools > AED Prototype > Build V10
//
// Baut das Abschluss-Panel: eine Bildschirmflaeche, die am Ende des
// Durchlaufs die erhobenen Verhaltensdaten zeigt, samt einer Zeile, die
// die Testperson in das Umfrageformular kopiert.
//
// Hintergrund: Ein WebGL-Build kann keine Datei bei der Forscherin
// ablegen. Die Testperson uebernimmt deshalb den Transport der Daten.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV10
{
    const string MENU = "Tools/AED Prototype/Build V10";
    const string CANVAS_NAME = "A1_EndPanel_Canvas";

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V10");

        GameObject canvasGO = Find(CANVAS_NAME);
        if (canvasGO == null)
        {
            canvasGO = new GameObject(CANVAS_NAME);
            Undo.RegisterCreatedObjectUndo(canvasGO, "End panel");

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler sc = canvasGO.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("[V10] Canvas '" + CANVAS_NAME + "' angelegt.");
        }

        // ------------------------------------------------------- Panel
        GameObject panel = FindChild(canvasGO.transform, "Panel");
        if (panel == null)
        {
            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGO.transform, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1000f, 760f);

            Image img = panel.GetComponent<Image>();
            img.color = new Color(0.05f, 0.07f, 0.09f, 0.94f);
        }

        // ------------------------------------------------------- Text
        GameObject txtGO = FindChild(panel.transform, "Text");
        TextMeshProUGUI tmp;
        if (txtGO == null)
        {
            txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(panel.transform, false);

            RectTransform rt = txtGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(48f, 48f);
            rt.offsetMax = new Vector2(-48f, -48f);

            tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 34f;
            tmp.enableAutoSizing = false;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = Color.white;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = "RUN FINISHED";
        }
        else
        {
            tmp = txtGO.GetComponent<TextMeshProUGUI>();
        }

        // ------------------------------------------------------- Messung
        A1RunMetrics m = canvasGO.GetComponent<A1RunMetrics>();
        if (m == null) m = Undo.AddComponent<A1RunMetrics>(canvasGO);

        Undo.RecordObject(m, "metrics");
        m.sequence  = Object.FindAnyObjectByType<A1Sequence>();
        m.display   = Object.FindAnyObjectByType<AEDDisplay>();
        m.reveal    = Object.FindAnyObjectByType<A1Reveal>();
        m.panel     = panel;
        m.panelText = tmp;
        EditorUtility.SetDirty(m);

        panel.SetActive(false);

        Report("A1Sequence", m.sequence);
        Report("AEDDisplay", m.display);
        Report("A1Reveal",   m.reveal);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V10] Fertig. Das Panel erscheint, sobald der Schock ausgeloest " +
                  "wurde (Uhr stoppt), oder auf Taste F9. Szene speichern mit Ctrl+S.");
    }

    static void Report(string what, Object found)
    {
        if (found == null) Debug.LogWarning("[V10] " + what + " nicht gefunden - bitte im Inspector nachtragen.");
        else Debug.Log("[V10] " + what + " verbunden.");
    }

    static GameObject FindChild(Transform parent, string name)
    {
        foreach (Transform t in parent)
            if (t.name == name) return t.gameObject;
        return null;
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
