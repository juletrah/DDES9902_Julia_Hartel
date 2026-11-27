// A1_BuildV31.cs
// Menue:  Assets > AED Build V31   (laeuft einmalig auch von selbst)
//
// Haengt die Abbildung der richtigen Pad-Stellen neben die Anzeigetafel.
// Sie erscheint zusammen mit den Pads und verschwindet, sobald beide
// sitzen - A2Flow schaltet sie ueber das Feld 'padGuide'.
//
// Zur Ausrichtung: die Tafel dreht sich ueber A1Billboard zum Spieler.
// TextMeshPro ist von seiner MINUS-z-Seite lesbar, und die Beschriftung
// steht ohne Eigendrehung unter der Tafel - der Spieler sieht die Tafel
// also von ihrer -z-Seite. Ein Wuerfel zeigt seine Textur auf der
// +z-Flaeche seitenrichtig, auf der Rueckseite gespiegelt. Die
// Abbildung wird deshalb um 180 Grad gedreht, damit ihre +z-Flaeche zum
// Spieler zeigt. Ohne das waeren links und rechts vertauscht - und
// damit die gelehrten Klebestellen falsch.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV31
{
    const string MENU = "Assets/AED Build V31";
    const string DIR  = "Assets/__My Project/";

    // Bild ist 394 x 451, Seitenverhaeltnis 0,874.
    const float GUIDE_H = 0.52f;                    // so hoch wie die Tafel
    const float GUIDE_W = GUIDE_H * 0.874f;         // 0,455

    // Tafel ist 1,05 breit -> halbe Breite 0,525, plus Luft, plus halbe
    // Bildbreite.
    static readonly Vector3 GUIDE_LOCAL = new Vector3(0.525f + 0.04f + GUIDE_W / 2f, 0f, 0f);

    static Material matBase;

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V31_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 5)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V31");

        AssetDatabase.Refresh();

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null) { Debug.LogError("[V31] Kein Referenzmaterial - abgebrochen."); return; }

        GameObject display = Find("AED_Display");
        if (display == null) { Debug.LogError("[V31] AED_Display fehlt - abgebrochen."); return; }

        GameObject area = Find("A2_Patient_Area");
        A2Flow flow = area != null ? area.GetComponent<A2Flow>() : null;
        if (flow == null) { Debug.LogError("[V31] A2Flow fehlt - abgebrochen."); return; }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(DIR + "Pad_Positions.jpg");
        if (tex == null) { Debug.LogError("[V31] Pad_Positions.jpg nicht gefunden - abgebrochen."); return; }

        GameObject old = Find("Pad_Guide");
        if (old != null) Undo.DestroyObjectImmediate(old);

        Material m = Mat("Mat_Pad_Guide", tex);

        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "Pad_Guide";
        Undo.RegisterCreatedObjectUndo(g, "Pad_Guide");
        g.transform.SetParent(display.transform, false);
        g.transform.localPosition = GUIDE_LOCAL;
        g.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        g.transform.localScale = new Vector3(GUIDE_W, GUIDE_H, 0.02f);

        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);   // darf den Klickstrahl nicht stoeren

        Renderer gr = g.GetComponent<Renderer>();
        if (gr != null) gr.sharedMaterial = m;

        g.SetActive(false);

        Undo.RecordObject(flow, "A2Flow");
        flow.padGuide = g;
        EditorUtility.SetDirty(flow);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V31] Fertig. Abbildung " + GUIDE_W.ToString("0.00") + " x " +
                  GUIDE_H.ToString("0.00") + " m neben der Tafel, erscheint mit den Pads. " +
                  "Szene speichern mit Ctrl+S.");
    }

    static Material Mat(string assetName, Texture2D tex)
    {
        string path = DIR + assetName + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(matBase);
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[V31] Material '" + path + "' angelegt.");
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        m.mainTexture = tex;
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
