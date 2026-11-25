// A1_BuildV23.cs
// Menue:  Tools > AED Prototype > Build V23
//
//   1 Fenster groesser: 3,20 x 2,10 m statt 2,60 x 1,80. Die Positionen
//     wurden dabei leicht verschoben, damit die breiteren Rahmen nicht
//     mit Kuechenzeile, Wandtisch und Sideboard zusammenstossen.
//   2 Aussicht jetzt Stadt statt Wiese: verschwommene Skyline.
//   3 Ein drittes Bild an der Westwand, gleicher Stil wie die beiden
//     anderen, hochformat.
//   4 Fuenf Kaffeebecher auf den Schreibtischen, damit die Arbeitsplaetze
//     benutzt aussehen. Sie haengen als Kinder an den jeweiligen
//     Tischgruppen, drehen sich also mit.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV23
{
    const string MENU = "Tools/AED Prototype/Build V23";
    const string ROOT = "Room_Features";
    const string DIR  = "Assets/__My Project/";

    const float WALL_E =  7.90f;
    const float WALL_W = -7.90f;
    const float WALL_N =  5.90f;
    const float WALL_S = -5.90f;

    const float WIN_W = 3.20f;
    const float WIN_H = 2.10f;
    const float WIN_Y = 1.65f;   // Unterkante 0,60 - Oberkante 2,70

    const float DESK_TOP = 0.76f;

    static Material matBase, matGlass, matFrame, matWood, matArtA, matArtB, matArtC, matMugA, matMugB;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V23");

        AssetDatabase.Refresh();

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null) { Debug.LogError("[V23] Kein Referenzmaterial - abgebrochen."); return; }

        matFrame = Mat("Mat_Frame", new Color(0.88f, 0.88f, 0.86f), null);
        matWood  = Mat("Mat_Wood",  new Color(0.82f, 0.73f, 0.57f), null);
        matGlass = Mat("Mat_WindowView", Color.white, Tex("Window_View"));
        matArtA  = Mat("Mat_Art_Ochre", Color.white, Tex("Art_Flowers_A"));
        matArtB  = Mat("Mat_Art_Teal",  Color.white, Tex("Art_Flowers_B"));
        matArtC  = Mat("Mat_Art_Sage",  Color.white, Tex("Art_Flowers_C"));
        matMugA  = Mat("Mat_Mug_Cream", new Color(0.93f, 0.92f, 0.88f), null);
        matMugB  = Mat("Mat_Mug_Clay",  new Color(0.72f, 0.38f, 0.29f), null);

        // ---------------------------------------------- Fenster und Bilder
        GameObject root = Find(ROOT);
        if (root != null) Undo.DestroyObjectImmediate(root);
        root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, ROOT);

        // Suedwand. S1 bleibt oestlich der Kuechenzeile (endet bei x -4,65),
        // S2 westlich des Wandtischs (beginnt bei x 4,70).
        Window(root, "Window_S1", new Vector3(-3.00f, WIN_Y, WALL_S + 0.03f),   0f);
        Window(root, "Window_S2", new Vector3( 2.90f, WIN_Y, WALL_S + 0.03f),   0f);
        // Ostwand. E2 beginnt oberhalb des Sideboards (endet bei z 2,00).
        Window(root, "Window_E1", new Vector3(WALL_E - 0.03f, WIN_Y, -3.40f), 270f);
        Window(root, "Window_E2", new Vector3(WALL_E - 0.03f, WIN_Y,  3.70f), 270f);

        Picture(root, "Picture_W",  new Vector3(WALL_W + 0.03f, 1.80f, 4.00f),  90f, 1.10f, 0.80f, matArtA);
        Picture(root, "Picture_N",  new Vector3(-3.60f, 1.80f, WALL_N - 0.03f), 180f, 0.90f, 1.10f, matArtB);
        Picture(root, "Picture_W2", new Vector3(WALL_W + 0.03f, 1.75f, 0.60f),  90f, 0.85f, 1.15f, matArtC);

        // ---------------------------------------------- Kaffeebecher
        int mugs = 0;
        mugs += Mug("Desk_Wall_NW", "Mug", new Vector3( 0.58f, 0f,  0.08f), matMugA);
        mugs += Mug("Desk_Wall_S",  "Mug", new Vector3(-0.60f, 0f,  0.10f), matMugB);
        mugs += Mug("Shared_W",     "Mug_N", new Vector3(-0.62f, 0f, -0.46f), matMugA);
        mugs += Mug("Shared_E",     "Mug_S", new Vector3( 0.58f, 0f,  0.44f), matMugB);
        mugs += Mug("Shared_S",     "Mug_N", new Vector3( 0.60f, 0f, -0.44f), matMugA);
        Debug.Log("[V23] " + mugs + " Kaffeebecher auf den Tischen.");

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V23] Fertig. 4 Fenster " + WIN_W.ToString("0.0") + " x " + WIN_H.ToString("0.0") +
                  " m mit Skyline, 3 Bilder, " + mugs + " Becher. Szene speichern mit Ctrl+S.");
    }

    // Becher als Kind der Tischgruppe, damit er sich mitdreht.
    static int Mug(string deskName, string mugName, Vector3 local, Material m)
    {
        GameObject desk = Find(deskName);
        if (desk == null) { Debug.LogWarning("[V23] '" + deskName + "' nicht gefunden."); return 0; }

        foreach (Transform t in desk.transform)
            if (t.name == mugName) { Undo.DestroyObjectImmediate(t.gameObject); break; }

        GameObject g = new GameObject(mugName);
        g.transform.SetParent(desk.transform, false);
        g.transform.localPosition = local;
        g.transform.localRotation = Quaternion.identity;
        Undo.RegisterCreatedObjectUndo(g, mugName);

        // Becherkoerper: Zylinder ist 2 Einheiten hoch, scale.y 0,05 = 10 cm.
        Prim(PrimitiveType.Cylinder, g, "Body", new Vector3(0f, DESK_TOP + 0.05f, 0f),
             new Vector3(0.085f, 0.05f, 0.085f), m);
        Prim(PrimitiveType.Cube, g, "Handle", new Vector3(0.055f, DESK_TOP + 0.055f, 0f),
             new Vector3(0.022f, 0.045f, 0.05f), m);

        return 1;
    }

    static void Window(GameObject root, string name, Vector3 at, float yaw)
    {
        GameObject g = Group(root, name, at, yaw);
        float w = WIN_W, h = WIN_H;

        Box(g, "Glass",        new Vector3(0f, 0f, 0f), new Vector3(w, h, 0.03f), matGlass);
        Box(g, "Frame_Top",    new Vector3(0f,  h / 2f + 0.07f, 0f), new Vector3(w + 0.28f, 0.14f, 0.08f), matFrame);
        Box(g, "Frame_Bottom", new Vector3(0f, -h / 2f - 0.07f, 0f), new Vector3(w + 0.28f, 0.14f, 0.08f), matFrame);
        Box(g, "Frame_Left",   new Vector3(-w / 2f - 0.07f, 0f, 0f), new Vector3(0.14f, h + 0.28f, 0.08f), matFrame);
        Box(g, "Frame_Right",  new Vector3( w / 2f + 0.07f, 0f, 0f), new Vector3(0.14f, h + 0.28f, 0.08f), matFrame);
        Box(g, "Mullion",      new Vector3(0f, 0f, 0.012f), new Vector3(0.08f, h, 0.04f), matFrame);
        Box(g, "Sill",         new Vector3(0f, -h / 2f - 0.17f, 0.09f), new Vector3(w + 0.42f, 0.06f, 0.26f), matFrame);

        Debug.Log("[V23] '" + name + "' bei (" + at.x.ToString("0.00") + " | " + at.z.ToString("0.00") +
                  "), Unterkante y " + (WIN_Y - h / 2f).ToString("0.00") + " m.");
    }

    static void Picture(GameObject root, string name, Vector3 at, float yaw, float w, float h, Material art)
    {
        GameObject g = Group(root, name, at, yaw);
        Box(g, "Canvas",       new Vector3(0f, 0f, 0f), new Vector3(w, h, 0.02f), art);
        Box(g, "Frame_Top",    new Vector3(0f,  h / 2f + 0.03f, 0f), new Vector3(w + 0.12f, 0.06f, 0.05f), matWood);
        Box(g, "Frame_Bottom", new Vector3(0f, -h / 2f - 0.03f, 0f), new Vector3(w + 0.12f, 0.06f, 0.05f), matWood);
        Box(g, "Frame_Left",   new Vector3(-w / 2f - 0.03f, 0f, 0f), new Vector3(0.06f, h + 0.12f, 0.05f), matWood);
        Box(g, "Frame_Right",  new Vector3( w / 2f + 0.03f, 0f, 0f), new Vector3(0.06f, h + 0.12f, 0.05f), matWood);
        Debug.Log("[V23] '" + name + "' an der Wand bei (" + at.x.ToString("0.00") + " | " + at.z.ToString("0.00") + ").");
    }

    // ---------------------------------------------------------- Bausteine

    static GameObject Group(GameObject root, string name, Vector3 at, float yaw)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(root.transform, false);
        g.transform.position = at;
        g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return g;
    }

    static void Box(GameObject p, string n, Vector3 pos, Vector3 sc, Material m)
    { Prim(PrimitiveType.Cube, p, n, pos, sc, m); }

    static void Prim(PrimitiveType type, GameObject parent, string name,
                     Vector3 localPos, Vector3 scale, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(type);
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

    static Texture2D Tex(string name)
    {
        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(DIR + name + ".png");
        if (t == null) Debug.LogWarning("[V23] Bild '" + name + ".png' nicht gefunden.");
        return t;
    }

    static Material Mat(string assetName, Color c, Texture2D tex)
    {
        string path = DIR + assetName + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(matBase);
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[V23] Material '" + path + "' angelegt.");
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            m.mainTexture = tex;
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
