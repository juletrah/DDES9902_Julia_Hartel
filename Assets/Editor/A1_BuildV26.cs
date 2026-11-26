// A1_BuildV26.cs  (Stand 2)
// Menue:  Tools > AED Prototype > Build V26
//
// Ersetzt die zwei durchgehenden Baender aus V24 durch Fenstergruppen:
// immer zwei Scheiben nebeneinander, dann ein Stueck Wand, dann wieder
// zwei - der Rhythmus alter Schulgebaeude.
//
//   Suedseite: von x -4,60 (dort endet die Kuechenzeile) bis x 7,55
//   Ostseite:  von z -5,55 bis z 5,55
//
// Hoehe und Profile bleiben wie in V24: eine Bruestung ab 0,85 m,
// Oberkante 2,70 m, schmale helle Rahmen.
//
// Das Panoramabild laeuft trotz der Luecken ueber die ganze Fassade
// weiter: jede Gruppe bekommt ihr eigenes Material, dessen UV-Ausschnitt
// genau dem Platz der Gruppe im Band entspricht. Sonst wuerde in jeder
// Gruppe dieselbe Skyline von vorn beginnen.
//
// Die drei Bilder an den Waenden bleiben unveraendert.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV26
{
    // Das " %#j" am Ende ist Unitys Schreibweise fuer Strg+Umschalt+J.
    const string MENU = "Assets/AED Build V26";
    const string ROOT = "Room_Features";
    const string DIR  = "Assets/__My Project/";

    const float WALL_E =  7.90f;
    const float WALL_W = -7.90f;
    const float WALL_N =  5.90f;
    const float WALL_S = -5.90f;

    const float GLASS_LOW  = 0.85f;
    const float GLASS_HIGH = 2.70f;

    const float PANE    = 1.40f;   // Breite einer einzelnen Scheibe
    const float PROFILE = 0.07f;   // Breite der senkrechten Profile
    const float GAP     = 1.00f;   // Wandstueck zwischen zwei Gruppen
    // Zwei Scheiben, drei Profile: aussen zwei Pfosten, in der Mitte einer.
    const float UNIT    = 2f * PANE + 3f * PROFILE;

    static Material matBase, matMull, matWood, matArtA, matArtB, matArtC;
    static Texture2D texPano;

    // Laeuft genau einmal von selbst, sobald Unity dieses Skript neu
    // kompiliert hat. Grund: das Menue ist hier gerade von einem anderen
    // Fenster verdeckt. Der Merker verhindert jede Wiederholung.
    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V26_done_b";
        Debug.Log("[V26] Skript geladen, Merker = " + EditorPrefs.GetBool(KEY, false));
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Debug.Log("[V26] Starte automatisch nach dem Kompilieren.");
            Build();
        };
    }

    [MenuItem(MENU, false, 0)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V26");

        AssetDatabase.Refresh();

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null) { Debug.LogError("[V26] Kein Referenzmaterial - abgebrochen."); return; }

        texPano = Tex("Window_View_Pano");
        matMull = Mat("Mat_Mullion", new Color(0.94f, 0.94f, 0.92f), null);
        matWood = Mat("Mat_Wood", new Color(0.82f, 0.73f, 0.57f), null);
        matArtA = Mat("Mat_Art_Ochre", Color.white, Tex("Art_Flowers_A"));
        matArtB = Mat("Mat_Art_Teal",  Color.white, Tex("Art_Flowers_B"));
        matArtC = Mat("Mat_Art_Sage",  Color.white, Tex("Art_Flowers_C"));

        GameObject root = Find(ROOT);
        if (root != null)
        {
            Undo.DestroyObjectImmediate(root);
            Debug.Log("[V26] Die zwei Baender aus V24 sind entfallen.");
        }
        root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, ROOT);

        Band(root, "Glazing_South", "S", -4.60f, 7.55f, WALL_S + 0.05f, 0f);
        Band(root, "Glazing_East",  "E", -5.55f, 5.55f, WALL_E - 0.05f, 270f);

        Picture(root, "Picture_W",  new Vector3(WALL_W + 0.03f, 1.80f, 4.00f),  90f, 1.10f, 0.80f, matArtA);
        Picture(root, "Picture_N",  new Vector3(-3.60f, 1.80f, WALL_N - 0.03f), 180f, 0.90f, 1.10f, matArtB);
        Picture(root, "Picture_W2", new Vector3(WALL_W + 0.03f, 1.75f, 0.60f),  90f, 0.85f, 1.15f, matArtC);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V26] Fertig. Fenster jetzt in Zweiergruppen mit " +
                  GAP.ToString("0.00") + " m Wand dazwischen. Szene speichern mit Ctrl+S.");
    }

    // Verteilt moeglichst viele Zweiergruppen gleichmaessig auf das Band.
    static void Band(GameObject root, string name, string tag,
                     float from, float to, float wallCoord, float yaw)
    {
        float span = to - from;
        float h    = GLASS_HIGH - GLASS_LOW;
        float cy   = (GLASS_LOW + GLASS_HIGH) / 2f;

        int units = Mathf.Max(1, Mathf.FloorToInt((span + GAP) / (UNIT + GAP)));
        float used   = units * UNIT + (units - 1) * GAP;
        float margin = (span - used) / 2f;

        GameObject band = new GameObject(name);
        band.transform.SetParent(root.transform, false);

        for (int i = 0; i < units; i++)
        {
            float start = from + margin + i * (UNIT + GAP);
            float mid   = start + UNIT / 2f;

            Vector3 at = (yaw == 0f)
                ? new Vector3(mid, cy, wallCoord)      // Suedwand
                : new Vector3(wallCoord, cy, mid);     // Ostwand

            GameObject g = Group(band, name + "_" + (i + 1), at, yaw);

            // Eigenes Material je Gruppe, damit die Skyline weiterlaeuft:
            // Ausschnitt = Platz dieser Gruppe im gesamten Band.
            Material glass = Mat("Mat_Glazing_" + tag + (i + 1), Color.white, texPano);
            if (texPano != null)
            {
                Vector2 scale  = new Vector2(UNIT / span, 1f);
                Vector2 offset = new Vector2((start - from) / span, 0f);
                SetUV(glass, scale, offset);
            }

            Box(g, "Glass", Vector3.zero, new Vector3(UNIT, h, 0.02f), glass);

            const float z = 0.035f;
            Box(g, "Rail_Bottom", new Vector3(0f, -h / 2f + 0.035f, z), new Vector3(UNIT, 0.07f, 0.05f), matMull);
            Box(g, "Rail_Top",    new Vector3(0f,  h / 2f - 0.035f, z), new Vector3(UNIT, 0.07f, 0.05f), matMull);
            Box(g, "Post_Left",   new Vector3(-UNIT / 2f + PROFILE / 2f, 0f, z), new Vector3(PROFILE, h, 0.05f), matMull);
            Box(g, "Post_Right",  new Vector3( UNIT / 2f - PROFILE / 2f, 0f, z), new Vector3(PROFILE, h, 0.05f), matMull);
            Box(g, "Mullion",     new Vector3(0f, 0f, z), new Vector3(PROFILE, h, 0.05f), matMull);
        }

        Debug.Log(string.Format(
            "[V26] '{0}': {1:0.00} m Wand, {2} Zweiergruppen je {3:0.00} m, " +
            "{4:0.00} m Luecke dazwischen, {5:0.00} m Rand aussen.",
            name, span, units, UNIT, GAP, margin));
    }

    static void Picture(GameObject root, string name, Vector3 at, float yaw, float w, float h, Material art)
    {
        GameObject g = Group(root, name, at, yaw);
        Box(g, "Canvas",       Vector3.zero, new Vector3(w, h, 0.02f), art);
        Box(g, "Frame_Top",    new Vector3(0f,  h / 2f + 0.03f, 0f), new Vector3(w + 0.12f, 0.06f, 0.05f), matWood);
        Box(g, "Frame_Bottom", new Vector3(0f, -h / 2f - 0.03f, 0f), new Vector3(w + 0.12f, 0.06f, 0.05f), matWood);
        Box(g, "Frame_Left",   new Vector3(-w / 2f - 0.03f, 0f, 0f), new Vector3(0.06f, h + 0.12f, 0.05f), matWood);
        Box(g, "Frame_Right",  new Vector3( w / 2f + 0.03f, 0f, 0f), new Vector3(0.06f, h + 0.12f, 0.05f), matWood);
    }

    // ---------------------------------------------------------- Bausteine

    static GameObject Group(GameObject parent, string name, Vector3 at, float yaw)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
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

    static void SetUV(Material m, Vector2 scale, Vector2 offset)
    {
        if (m.HasProperty("_BaseMap"))
        {
            m.SetTextureScale("_BaseMap", scale);
            m.SetTextureOffset("_BaseMap", offset);
        }
        if (m.HasProperty("_MainTex"))
        {
            m.SetTextureScale("_MainTex", scale);
            m.SetTextureOffset("_MainTex", offset);
        }
        m.mainTextureScale  = scale;
        m.mainTextureOffset = offset;
        EditorUtility.SetDirty(m);
    }

    static Texture2D Tex(string name)
    {
        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(DIR + name + ".png");
        if (t == null) Debug.LogWarning("[V26] Bild '" + name + ".png' nicht gefunden.");
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
            Debug.Log("[V26] Material '" + path + "' angelegt.");
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
