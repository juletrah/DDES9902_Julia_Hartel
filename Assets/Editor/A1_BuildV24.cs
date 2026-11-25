// A1_BuildV24.cs
// Menue:  Tools > AED Prototype > Build V24
//
// Zwei durchgehende Bandfenster auf Bruestungshoehe, wie auf dem vierten
// Referenzfoto: eine ungeteilte Scheibe von 0,85 bis 2,70 m, schmale
// helle, schmale Profile. Kein Querriegel, kein Sims, kein Eckpfosten.
//
//   Suedseite: von x -4,60 (dort endet die Kuechenzeile) bis x 7,55
//   Ostseite:  von z -5,55 bis z 5,55
//
// Beide Baender bekommen EINE durchgehende Scheibe mit einem breiten
// Panoramabild, davor die Profile. Das ist der Grund fuer das eigene
// Bild 'Window_View_Pano.png': haette jede Scheibe das alte Hochformat
// bekommen, wuerde sich die Skyline in jedem Feld wiederholen.
//
// Die drei Bilder an den Waenden bleiben unveraendert.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV24
{
    const string MENU = "Tools/AED Prototype/Build V24";
    const string ROOT = "Room_Features";
    const string DIR  = "Assets/__My Project/";

    const float WALL_E =  7.90f;
    const float WALL_W = -7.90f;
    const float WALL_N =  5.90f;
    const float WALL_S = -5.90f;
    const float CEIL   =  3.00f;

    // Bandfenster auf Bruestungshoehe, wie auf dem vierten Referenzfoto:
    // eine durchgehende Scheibe ohne horizontale Unterteilung.
    const float GLASS_LOW  = 0.85f;
    const float GLASS_HIGH = 2.70f;
    const float PROFILE    = 0.055f;  // Breite der senkrechten Profile
    const float PANEL      = 1.55f;   // angestrebte Feldbreite

    static Material matBase, matGlass, matMull, matWood, matArtA, matArtB, matArtC;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V24");

        AssetDatabase.Refresh();

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null) { Debug.LogError("[V24] Kein Referenzmaterial - abgebrochen."); return; }

        matGlass = Mat("Mat_Glazing", Color.white, Tex("Window_View_Pano"));
        matMull  = Mat("Mat_Mullion", new Color(0.94f, 0.94f, 0.92f), null);
        matWood  = Mat("Mat_Wood", new Color(0.82f, 0.73f, 0.57f), null);
        matArtA  = Mat("Mat_Art_Ochre", Color.white, Tex("Art_Flowers_A"));
        matArtB  = Mat("Mat_Art_Teal",  Color.white, Tex("Art_Flowers_B"));
        matArtC  = Mat("Mat_Art_Sage",  Color.white, Tex("Art_Flowers_C"));

        GameObject root = Find(ROOT);
        if (root != null)
        {
            Undo.DestroyObjectImmediate(root);
            Debug.Log("[V24] Die vier Einzelfenster sind entfallen, es kommen zwei Baender.");
        }
        root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, ROOT);

        // Suedwand: beginnt dort, wo die Kuechenzeile aufhoert.
        Glazing(root, "Glazing_South", -4.60f, 7.55f, WALL_S + 0.05f, 0f);
        // Ostwand: fast die ganze Laenge.
        Glazing(root, "Glazing_East", -5.55f, 5.55f, WALL_E - 0.05f, 270f);

        Picture(root, "Picture_W",  new Vector3(WALL_W + 0.03f, 1.80f, 4.00f),  90f, 1.10f, 0.80f, matArtA);
        Picture(root, "Picture_N",  new Vector3(-3.60f, 1.80f, WALL_N - 0.03f), 180f, 0.90f, 1.10f, matArtB);
        Picture(root, "Picture_W2", new Vector3(WALL_W + 0.03f, 1.75f, 0.60f),  90f, 0.85f, 1.15f, matArtC);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V24] Fertig. Zwei Bandfenster auf Bruestungshoehe, 3 Bilder. " +
                  "Szene speichern mit Ctrl+S.");
    }

    // from/to sind die Enden des Bandes entlang der Wand.
    // wallCoord ist die feste Achse, yaw dreht das Band an seine Wand.
    static void Glazing(GameObject root, string name, float from, float to, float wallCoord, float yaw)
    {
        float width = to - from;
        float mid   = (from + to) / 2f;
        float h     = GLASS_HIGH - GLASS_LOW;
        float cy    = (GLASS_LOW + GLASS_HIGH) / 2f;

        Vector3 at = (yaw == 0f)
            ? new Vector3(mid, cy, wallCoord)          // Suedwand
            : new Vector3(wallCoord, cy, mid);         // Ostwand

        GameObject g = Group(root, name, at, yaw);

        // Eine durchgehende Scheibe, damit die Skyline ueber die ganze
        // Fassade laeuft und sich nicht je Feld wiederholt.
        Box(g, "Glass", new Vector3(0f, 0f, 0f), new Vector3(width, h, 0.02f), matGlass);

        // Profile liegen vor der Scheibe, Richtung Raum.
        const float z = 0.035f;
        Box(g, "Rail_Bottom", new Vector3(0f, -h / 2f + 0.035f, z), new Vector3(width, 0.07f, 0.05f), matMull);
        Box(g, "Rail_Top",    new Vector3(0f,  h / 2f - 0.035f, z), new Vector3(width, 0.07f, 0.05f), matMull);
        // Kein Querriegel, kein Sims, kein Eckpfosten: nur Scheibe und Profile.
        Box(g, "Post_Start",  new Vector3(-width / 2f + 0.035f, 0f, z), new Vector3(0.07f, h, 0.05f), matMull);
        Box(g, "Post_End",    new Vector3( width / 2f - 0.035f, 0f, z), new Vector3(0.07f, h, 0.05f), matMull);

        int fields = Mathf.Max(2, Mathf.RoundToInt(width / PANEL));
        float step = width / fields;
        for (int i = 1; i < fields; i++)
        {
            float x = -width / 2f + i * step;
            Box(g, "Mullion_" + i, new Vector3(x, 0f, z), new Vector3(PROFILE, h, 0.045f), matMull);
        }

        Debug.Log(string.Format(
            "[V24] '{0}': {1:0.00} m breit, {2:0.00} m hoch, {3} Felder je {4:0.00} m. " +
            "Bruestung bis {5:0.00}, Oberkante {6:0.00} bei {7:0.00} m Raumhoehe.",
            name, width, h, fields, step, GLASS_LOW, GLASS_HIGH, CEIL));
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

    // ---------------------------------------------------------- Bausteine

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

    static Texture2D Tex(string name)
    {
        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(DIR + name + ".png");
        if (t == null) Debug.LogWarning("[V24] Bild '" + name + ".png' nicht gefunden.");
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
            Debug.Log("[V24] Material '" + path + "' angelegt.");
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
