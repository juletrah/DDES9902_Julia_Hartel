// A1_BuildV20.cs
// Menue:  Tools > AED Prototype > Build V20
//
//   1 Doppeltisch Sued weiter nach Osten, direkt hinter die Spielfigur,
//     damit der Weg zur Kuechenzeile frei ist.
//   2 Kuechenzeile, Haengeschrank und Sideboard in Holzton.
//   3 Zwei Fenster und zwei Bilder an den bisher leeren Waenden.
//
// Fenster und Bilder liegen unter einem eigenen Objekt 'Room_Features'
// und nicht unter 'Office_Decor' - sonst wuerden sie bei einem erneuten
// Lauf von V18 mitgeloescht, das seinen Dekobestand jedes Mal neu baut.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV20
{
    const string MENU = "Tools/AED Prototype/Build V20";
    const string ROOT = "Room_Features";

    // Innenseiten der Waende
    const float WALL_E =  7.90f;
    const float WALL_W = -7.90f;
    const float WALL_N =  5.90f;
    const float WALL_S = -5.90f;

    static Material matWood, matGlass, matFrame, matArtA, matArtB, matBase;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V20");

        GameObject refObj = Find("Table_Phone");
        if (refObj != null)
        {
            Renderer r = refObj.GetComponent<Renderer>();
            if (r != null) matBase = r.sharedMaterial;
        }
        if (matBase == null)
        {
            Debug.LogError("[V20] Kein Referenzmaterial gefunden - abgebrochen.");
            return;
        }

        matWood  = Mat("Mat_Wood",      new Color(0.52f, 0.36f, 0.21f));
        matGlass = Mat("Mat_Glass",     new Color(0.62f, 0.78f, 0.88f));
        matFrame = Mat("Mat_Frame",     new Color(0.88f, 0.88f, 0.86f));
        matArtA  = Mat("Mat_Art_Ochre", new Color(0.78f, 0.56f, 0.24f));
        matArtB  = Mat("Mat_Art_Teal",  new Color(0.22f, 0.45f, 0.48f));

        // ------------------------------------------------- 1 Tisch verschieben
        GameObject bench = Find("Shared_S");
        if (bench != null)
        {
            Vector3 from = bench.transform.position;
            Undo.RecordObject(bench.transform, "Shared_S");
            bench.transform.position = new Vector3(0.35f, 0f, -4.35f);
            Debug.Log(string.Format(
                "[V20] 'Shared_S': x {0:0.00} -> 0,35, also direkt hinter der Spielfigur " +
                "bei x 0,35. Vor der Kuechenzeile sind damit rund 3,4 m frei.", from.x));
        }
        else Debug.LogWarning("[V20] 'Shared_S' nicht gefunden.");

        // ------------------------------------------------- 2 Holzton
        int painted = 0;
        painted += Paint("Counter_Base", matWood);
        painted += Paint("Counter_Top", matWood);
        painted += Paint("Upper_Cabinet", matWood);
        painted += Paint("Table_Phone", matWood);
        Debug.Log("[V20] " + painted + " Teil(e) in Holzton: Kuechenzeile, Haengeschrank, Sideboard. " +
                  "Spuele, Armatur und Kaffeeautomat bleiben schwarz.");

        // ------------------------------------------------- 3 Fenster und Bilder
        GameObject root = Find(ROOT);
        if (root != null) Undo.DestroyObjectImmediate(root);
        root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, ROOT);

        // Suedwand: zwei Fenster, hoch genug, dass die Moebel nicht davorstehen
        Window(root, "Window_S1", new Vector3(-3.20f, 1.75f, WALL_S + 0.03f),   0f, 1.90f, 1.30f);
        Window(root, "Window_S2", new Vector3( 3.20f, 1.75f, WALL_S + 0.03f),   0f, 1.90f, 1.30f);
        // Ostwand: ein Fenster
        Window(root, "Window_E1", new Vector3(WALL_E - 0.03f, 1.75f, -3.40f), 270f, 1.90f, 1.30f);

        // Bilder an den freien Wandstuecken
        Picture(root, "Picture_W", new Vector3(WALL_W + 0.03f, 1.80f, 4.00f),  90f, 1.10f, 0.80f, matArtA);
        Picture(root, "Picture_N", new Vector3(-3.60f, 1.80f, WALL_N - 0.03f), 180f, 0.90f, 1.10f, matArtB);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V20] Fertig. 3 Fenster, 2 Bilder unter '" + ROOT +
                  "'. Szene speichern mit Ctrl+S.");
    }

    // Fenster: Scheibe mit Rahmen und Mittelsprosse. Die Gruppe sitzt auf der
    // Wandinnenseite, lokal zeigt die Scheibe nach +z.
    static void Window(GameObject root, string name, Vector3 at, float yaw, float w, float h)
    {
        GameObject g = Group(root, name, at, yaw);

        Box(g, "Glass",        new Vector3(0f, 0f, 0f), new Vector3(w, h, 0.03f), matGlass);
        Box(g, "Frame_Top",    new Vector3(0f,  h / 2f + 0.05f, 0f), new Vector3(w + 0.20f, 0.10f, 0.07f), matFrame);
        Box(g, "Frame_Bottom", new Vector3(0f, -h / 2f - 0.05f, 0f), new Vector3(w + 0.20f, 0.10f, 0.07f), matFrame);
        Box(g, "Frame_Left",   new Vector3(-w / 2f - 0.05f, 0f, 0f), new Vector3(0.10f, h + 0.20f, 0.07f), matFrame);
        Box(g, "Frame_Right",  new Vector3( w / 2f + 0.05f, 0f, 0f), new Vector3(0.10f, h + 0.20f, 0.07f), matFrame);
        Box(g, "Mullion",      new Vector3(0f, 0f, 0.005f), new Vector3(0.07f, h, 0.05f), matFrame);
        Box(g, "Sill",         new Vector3(0f, -h / 2f - 0.12f, 0.06f), new Vector3(w + 0.30f, 0.05f, 0.20f), matFrame);

        Debug.Log("[V20] '" + name + "' bei (" + at.x.ToString("0.00") + " | " +
                  at.z.ToString("0.00") + "), " + w.ToString("0.0") + " x " + h.ToString("0.0") + " m.");
    }

    static void Picture(GameObject root, string name, Vector3 at, float yaw, float w, float h, Material art)
    {
        GameObject g = Group(root, name, at, yaw);

        Box(g, "Canvas",      new Vector3(0f, 0f, 0f), new Vector3(w, h, 0.02f), art);
        Box(g, "Frame_Top",   new Vector3(0f,  h / 2f + 0.03f, 0f), new Vector3(w + 0.12f, 0.06f, 0.05f), matWood);
        Box(g, "Frame_Bottom",new Vector3(0f, -h / 2f - 0.03f, 0f), new Vector3(w + 0.12f, 0.06f, 0.05f), matWood);
        Box(g, "Frame_Left",  new Vector3(-w / 2f - 0.03f, 0f, 0f), new Vector3(0.06f, h + 0.12f, 0.05f), matWood);
        Box(g, "Frame_Right", new Vector3( w / 2f + 0.03f, 0f, 0f), new Vector3(0.06f, h + 0.12f, 0.05f), matWood);

        Debug.Log("[V20] '" + name + "' bei (" + at.x.ToString("0.00") + " | " +
                  at.z.ToString("0.00") + ").");
    }

    // ---------------------------------------------------------- Bausteine

    static int Paint(string objectName, Material m)
    {
        int n = 0;
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name != objectName) continue;
            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded) continue;

            Renderer r = t.GetComponent<Renderer>();
            if (r == null) continue;

            Undo.RecordObject(r, "paint");
            r.sharedMaterial = m;
            n++;
        }
        if (n == 0) Debug.LogWarning("[V20] '" + objectName + "' nicht gefunden.");
        return n;
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

        // Wanddeko braucht keine Collider - sie wuerden nur Klicks abfangen.
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);

        if (m != null)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }

    static Material Mat(string assetName, Color c)
    {
        string path = "Assets/__My Project/" + assetName + ".mat";

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(matBase);
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[V20] Material '" + path + "' angelegt.");
        }

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
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
