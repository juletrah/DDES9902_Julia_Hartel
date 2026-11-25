// A1_BuildV21.cs
// Menue:  Tools > AED Prototype > Build V21
//
//   1 Die Rueckmeldung zum Notruf wird gedreht. Sie zeigte nach Osten,
//     also in die Wand. Ein TextMeshPro ist von seiner MINUS-z-Seite aus
//     lesbar, nicht von der Plus-z-Seite - deshalb war die Drehung aus
//     V18 genau verkehrt herum.
//   2 Groessere Fenster, vier statt drei, mit einem Bild dahinter, sodass
//     man ins Gruene schaut.
//   3 Die Bilder an den Waenden bleiben unveraendert bestehen.
//
// Das Bild 'Window_View.png' liegt unter Assets/__My Project/ und wird
// hier in ein Material gelegt und auf die Scheiben gelegt.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV21
{
    const string MENU = "Tools/AED Prototype/Build V21";
    const string ROOT = "Room_Features";
    const string TEX  = "Assets/__My Project/Window_View.png";

    const float WALL_E =  7.90f;
    const float WALL_W = -7.90f;
    const float WALL_N =  5.90f;
    const float WALL_S = -5.90f;

    // Groesser als vorher: 2,60 x 1,80 statt 1,90 x 1,30
    const float WIN_W = 2.60f;
    const float WIN_H = 1.80f;
    const float WIN_Y = 1.70f;

    static Material matBase, matGlass, matFrame, matWood, matArtA, matArtB;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V21");

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null)
        {
            Debug.LogError("[V21] Kein Referenzmaterial gefunden - abgebrochen.");
            return;
        }

        // ------------------------------------------------- 1 Text drehen
        int turned = 0;
        foreach (string n in new[] { "call_feedback_text", "Info_Call" })
        {
            GameObject t = Find(n);
            if (t == null) { Debug.LogWarning("[V21] '" + n + "' nicht gefunden."); continue; }

            Undo.RecordObject(t.transform, n);
            float before = t.transform.eulerAngles.y;
            t.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            turned++;
            Debug.Log(string.Format(
                "[V21] '{0}': Drehung {1:0} -> 90 Grad. Jetzt vom Raum aus lesbar, " +
                "vorher zur Wand hin.", n, before));
        }
        if (turned == 0) Debug.LogWarning("[V21] Kein Rueckmeldungstext gefunden.");

        // ------------------------------------------------- Materialien
        AssetDatabase.Refresh();
        Texture2D view = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX);
        if (view == null)
            Debug.LogWarning("[V21] '" + TEX + "' nicht gefunden - Scheiben bleiben einfarbig.");

        matFrame = Mat("Mat_Frame", new Color(0.88f, 0.88f, 0.86f), null);
        matWood  = Mat("Mat_Wood",  new Color(0.52f, 0.36f, 0.21f), null);
        matArtA  = Mat("Mat_Art_Ochre", new Color(0.78f, 0.56f, 0.24f), null);
        matArtB  = Mat("Mat_Art_Teal",  new Color(0.22f, 0.45f, 0.48f), null);
        // Weisse Grundfarbe, damit das Bild unverfaelscht durchkommt.
        matGlass = Mat("Mat_WindowView", Color.white, view);

        // ------------------------------------------------- 2 Fenster neu
        GameObject root = Find(ROOT);
        if (root != null) Undo.DestroyObjectImmediate(root);
        root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, ROOT);

        Window(root, "Window_S1", new Vector3(-3.30f, WIN_Y, WALL_S + 0.03f),   0f);
        Window(root, "Window_S2", new Vector3( 3.30f, WIN_Y, WALL_S + 0.03f),   0f);
        Window(root, "Window_E1", new Vector3(WALL_E - 0.03f, WIN_Y, -3.40f), 270f);
        Window(root, "Window_E2", new Vector3(WALL_E - 0.03f, WIN_Y,  3.40f), 270f);

        Picture(root, "Picture_W", new Vector3(WALL_W + 0.03f, 1.80f, 4.00f),  90f, 1.10f, 0.80f, matArtA);
        Picture(root, "Picture_N", new Vector3(-3.60f, 1.80f, WALL_N - 0.03f), 180f, 0.90f, 1.10f, matArtB);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V21] Fertig. 4 Fenster " + WIN_W.ToString("0.0") + " x " +
                  WIN_H.ToString("0.0") + " m mit Aussicht, 2 Bilder. Szene speichern mit Ctrl+S.");
    }

    static void Window(GameObject root, string name, Vector3 at, float yaw)
    {
        GameObject g = Group(root, name, at, yaw);
        float w = WIN_W, h = WIN_H;

        // Die Scheibe zeigt mit ihrer Vorderseite in den Raum.
        Box(g, "Glass",        new Vector3(0f, 0f, 0f), new Vector3(w, h, 0.03f), matGlass);
        Box(g, "Frame_Top",    new Vector3(0f,  h / 2f + 0.06f, 0f), new Vector3(w + 0.24f, 0.12f, 0.08f), matFrame);
        Box(g, "Frame_Bottom", new Vector3(0f, -h / 2f - 0.06f, 0f), new Vector3(w + 0.24f, 0.12f, 0.08f), matFrame);
        Box(g, "Frame_Left",   new Vector3(-w / 2f - 0.06f, 0f, 0f), new Vector3(0.12f, h + 0.24f, 0.08f), matFrame);
        Box(g, "Frame_Right",  new Vector3( w / 2f + 0.06f, 0f, 0f), new Vector3(0.12f, h + 0.24f, 0.08f), matFrame);
        Box(g, "Mullion",      new Vector3(0f, 0f, 0.012f), new Vector3(0.07f, h, 0.04f), matFrame);
        Box(g, "Sill",         new Vector3(0f, -h / 2f - 0.15f, 0.08f), new Vector3(w + 0.36f, 0.06f, 0.24f), matFrame);

        Debug.Log("[V21] '" + name + "' bei (" + at.x.ToString("0.00") + " | " +
                  at.z.ToString("0.00") + "), Unterkante y " + (WIN_Y - h / 2f).ToString("0.00") + " m.");
    }

    static void Picture(GameObject root, string name, Vector3 at, float yaw, float w, float h, Material art)
    {
        GameObject g = Group(root, name, at, yaw);
        Box(g, "Canvas",       new Vector3(0f, 0f, 0f), new Vector3(w, h, 0.02f), art);
        Box(g, "Frame_Top",    new Vector3(0f,  h / 2f + 0.03f, 0f), new Vector3(w + 0.12f, 0.06f, 0.05f), matWood);
        Box(g, "Frame_Bottom", new Vector3(0f, -h / 2f - 0.03f, 0f), new Vector3(w + 0.12f, 0.06f, 0.05f), matWood);
        Box(g, "Frame_Left",   new Vector3(-w / 2f - 0.03f, 0f, 0f), new Vector3(0.06f, h + 0.12f, 0.05f), matWood);
        Box(g, "Frame_Right",  new Vector3( w / 2f + 0.03f, 0f, 0f), new Vector3(0.06f, h + 0.12f, 0.05f), matWood);
    }

    static GameObject Group(GameObject root, string name, Vector3 at, float yaw)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(root.transform, false);
        g.transform.position = at;
        g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return g;
    }

    static void Box(GameObject parent, string name, Vector3 localPos, Vector3 scale, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = scale;
        g.isStatic = true;

        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);

        if (m != null)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }

    static Material Mat(string assetName, Color c, Texture2D tex)
    {
        string path = "Assets/__My Project/" + assetName + ".mat";

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(matBase);
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[V21] Material '" + path + "' angelegt.");
        }

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);

        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            m.mainTexture = tex;
            Debug.Log("[V21] '" + assetName + "' hat jetzt die Aussicht als Textur.");
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
