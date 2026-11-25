// A1_BuildV18.cs
// Menue:  Tools > AED Prototype > Build V18
//
// Raeumt die Bueroeinrichtung auf und ergaenzt sie:
//
//   1 Nichts steckt mehr in einer Wand. Jedes Moebel wird als Gruppe mit
//     eigener Drehung gesetzt, sodass auch an Ost- und Westwand gearbeitet
//     werden kann. Wandtische stehen 15 cm vor der Wand, Stuehle zeigen in
//     den Raum.
//   2 Die frei stehenden Doppeltische haben rundherum Platz: mindestens
//     etwa 1 m hinter jedem Stuhl, und keiner steht im Weg zwischen
//     Spielerstart und Patient.
//   3 In der AED-Ecke hinter der Trennwand steht jetzt eine angedeutete
//     Kuechenzeile mit Kaffeeautomat.
//   4 Der Telefontisch wird zum laenglichen Sideboard an der Ostwand,
//     buendig statt in der Wand. Telefon und Rueckmeldung ziehen mit um.
//
// Der ganze Dekoteil liegt unter 'Office_Decor' und wird bei jedem Lauf
// neu aufgebaut, damit keine Reste aus frueheren Varianten stehenbleiben.
//
// Keine Interaktion: weder Holdable noch InteractableTrigger. Collider
// bleiben, damit man nicht durch Moebel laeuft.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV18
{
    const string MENU = "Tools/AED Prototype/Build V18";
    const string ROOT = "Office_Decor";

    const float DESK_TOP = 0.76f;
    const float TOP_T    = 0.05f;
    const float SEAT_H   = 0.47f;

    static Material matFurniture;
    static Material matBlack;
    static Material matPlant;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V18");

        GameObject refTable = Find("Table_Phone");
        if (refTable != null)
        {
            Renderer r = refTable.GetComponent<Renderer>();
            if (r != null) matFurniture = r.sharedMaterial;
        }

        matBlack = GetOrCreate("Mat_OfficeBlack", new Color(0.11f, 0.11f, 0.12f));
        matPlant = GetOrCreate("Mat_Plant",       new Color(0.24f, 0.45f, 0.22f));

        // Alten Dekobestand komplett verwerfen - die Teile haben sich
        // umbenannt und umgeordnet, Reste waeren sonst nicht aufzufinden.
        GameObject old = Find(ROOT);
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
            Debug.Log("[V18] Alten '" + ROOT + "' verworfen und neu aufgebaut.");
        }

        GameObject root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, ROOT);

        // ---------------------------------------------- Tische an der Wand
        // yaw 180 = Rueckseite zur Nordwand, Stuhl zeigt in den Raum
        Desk(root, "Desk_Wall_NW", new Vector3(-5.60f, 0f, 5.30f), 180f);
        Desk(root, "Desk_Wall_NE", new Vector3( 5.60f, 0f, 5.30f), 180f);
        Desk(root, "Desk_Wall_S",  new Vector3( 5.60f, 0f, -5.30f), 0f);

        // ---------------------------------------------- frei stehende Doppeltische
        Bench(root, "Shared_W", new Vector3(-5.00f, 0f,  1.00f), 0f);
        Bench(root, "Shared_E", new Vector3( 4.60f, 0f, -0.80f), 0f);
        Bench(root, "Shared_S", new Vector3(-1.40f, 0f, -4.35f), 90f);

        // ---------------------------------------------- Kuechenzeile
        Kitchen(root, "Kitchenette", new Vector3(-6.00f, 0f, -5.35f), 0f);

        // ---------------------------------------------- Pflanzen
        Plant(root, "Plant_NW", new Vector2(-7.20f,  2.40f), 1.00f);
        Plant(root, "Plant_E",  new Vector2( 7.35f, -1.60f), 0.90f);
        Plant(root, "Plant_N",  new Vector2( 7.30f,  5.20f), 0.95f);

        // ---------------------------------------------- Sideboard mit Telefon
        Sideboard();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V18] Fertig. 3 Wandtische, 3 Doppeltische, Kuechenzeile, " +
                  "3 Pflanzen, Sideboard. Szene speichern mit Ctrl+S.");
    }

    // ============================================================ Moebel
    // Alle Teile werden in lokalen Koordinaten gebaut: vorne ist +z, dort
    // sitzt man. Die Gruppe uebernimmt Position und Drehung.

    static void Desk(GameObject root, string name, Vector3 at, float yaw)
    {
        GameObject g = Group(root, name, at, yaw);

        Box(g, "Desk_Top",    new Vector3(0f, DESK_TOP - TOP_T / 2f, 0f), new Vector3(1.80f, TOP_T, 0.90f), matFurniture);
        Box(g, "Desk_Side_L", new Vector3(-0.86f, (DESK_TOP - TOP_T) / 2f, 0f), new Vector3(0.06f, DESK_TOP - TOP_T, 0.82f), matFurniture);
        Box(g, "Desk_Side_R", new Vector3( 0.86f, (DESK_TOP - TOP_T) / 2f, 0f), new Vector3(0.06f, DESK_TOP - TOP_T, 0.82f), matFurniture);
        Box(g, "Desk_Back",   new Vector3(0f, 0.52f, -0.42f), new Vector3(1.68f, 0.42f, 0.03f), matFurniture);

        Screen(g, "", 0f, -0.26f);
        Box(g, "Keyboard", new Vector3(0f, DESK_TOP + 0.013f, 0.16f), new Vector3(0.48f, 0.025f, 0.17f), matBlack);
        Chair(g, "Chair", 0f, 0.90f, +1f);

        Debug.Log("[V18] '" + name + "' bei x " + at.x.ToString("0.0") +
                  " / z " + at.z.ToString("0.0") + ", gedreht um " + yaw + " Grad.");
    }

    static void Bench(GameObject root, string name, Vector3 at, float yaw)
    {
        GameObject g = Group(root, name, at, yaw);

        Box(g, "Desk_Top",    new Vector3(0f, DESK_TOP - TOP_T / 2f, 0f), new Vector3(2.00f, TOP_T, 1.60f), matFurniture);
        Box(g, "Desk_Side_L", new Vector3(-0.97f, (DESK_TOP - TOP_T) / 2f, 0f), new Vector3(0.06f, DESK_TOP - TOP_T, 1.46f), matFurniture);
        Box(g, "Desk_Side_R", new Vector3( 0.97f, (DESK_TOP - TOP_T) / 2f, 0f), new Vector3(0.06f, DESK_TOP - TOP_T, 1.46f), matFurniture);
        Box(g, "Divider",     new Vector3(0f, DESK_TOP + 0.20f, 0f), new Vector3(1.90f, 0.40f, 0.04f), matFurniture);

        Screen(g, "_N", 0f, -0.30f);
        Box(g, "Keyboard_N", new Vector3(0f, DESK_TOP + 0.013f, -0.58f), new Vector3(0.48f, 0.025f, 0.17f), matBlack);
        Chair(g, "Chair_N", 0f, -1.28f, -1f);

        Screen(g, "_S", 0f, 0.30f);
        Box(g, "Keyboard_S", new Vector3(0f, DESK_TOP + 0.013f, 0.58f), new Vector3(0.48f, 0.025f, 0.17f), matBlack);
        Chair(g, "Chair_S", 0f, 1.28f, +1f);

        Debug.Log("[V18] '" + name + "' bei x " + at.x.ToString("0.0") +
                  " / z " + at.z.ToString("0.0") + ", gedreht um " + yaw + " Grad.");
    }

    static void Kitchen(GameObject root, string name, Vector3 at, float yaw)
    {
        GameObject g = Group(root, name, at, yaw);

        Box(g, "Counter_Base", new Vector3(0f, 0.44f, -0.22f), new Vector3(2.60f, 0.88f, 0.60f), matFurniture);
        Box(g, "Counter_Top",  new Vector3(0f, 0.90f, -0.20f), new Vector3(2.70f, 0.05f, 0.66f), matFurniture);
        Box(g, "Upper_Cabinet",new Vector3(0f, 1.75f, -0.30f), new Vector3(2.40f, 0.60f, 0.38f), matFurniture);
        Box(g, "Sink",         new Vector3(-0.60f, 0.925f, -0.20f), new Vector3(0.52f, 0.04f, 0.40f), matBlack);
        Box(g, "Tap",          new Vector3(-0.60f, 1.07f, -0.40f), new Vector3(0.04f, 0.26f, 0.04f), matBlack);

        Box(g, "Coffee_Body",  new Vector3(0.75f, 1.12f, -0.22f), new Vector3(0.34f, 0.40f, 0.32f), matBlack);
        Box(g, "Coffee_Top",   new Vector3(0.75f, 1.34f, -0.22f), new Vector3(0.38f, 0.06f, 0.36f), matBlack);
        Box(g, "Coffee_Tray",  new Vector3(0.75f, 0.95f, -0.04f), new Vector3(0.24f, 0.03f, 0.18f), matBlack);

        Debug.Log("[V18] '" + name + "' bei x " + at.x.ToString("0.0") +
                  " / z " + at.z.ToString("0.0") + " in der AED-Ecke.");
    }

    static void Screen(GameObject g, string suffix, float x, float z)
    {
        Box(g, "Monitor_Foot" + suffix, new Vector3(x, DESK_TOP + 0.015f, z), new Vector3(0.26f, 0.03f, 0.16f), matBlack);
        Box(g, "Monitor_Neck" + suffix, new Vector3(x, DESK_TOP + 0.13f, z),  new Vector3(0.07f, 0.22f, 0.07f), matBlack);
        Box(g, "Monitor" + suffix,      new Vector3(x, DESK_TOP + 0.44f, z),  new Vector3(0.66f, 0.40f, 0.035f), matBlack);
    }

    // facing: in welche Richtung die Rueckenlehne vom Tisch weg zeigt
    static void Chair(GameObject g, string name, float x, float z, float facing)
    {
        Box(g, name + "_Base",   new Vector3(x, 0.035f, z), new Vector3(0.58f, 0.06f, 0.58f), matBlack);
        Box(g, name + "_Column", new Vector3(x, SEAT_H / 2f, z), new Vector3(0.10f, SEAT_H - 0.03f, 0.10f), matBlack);
        Box(g, name + "_Seat",   new Vector3(x, SEAT_H, z), new Vector3(0.52f, 0.08f, 0.52f), matBlack);
        Box(g, name + "_Back",   new Vector3(x, SEAT_H + 0.33f, z + facing * 0.24f), new Vector3(0.52f, 0.60f, 0.06f), matBlack);
    }

    static void Plant(GameObject root, string name, Vector2 at, float s)
    {
        GameObject g = Group(root, name, new Vector3(at.x, 0f, at.y), 0f);

        Cyl(g, "Pot",     new Vector3(0f, 0.23f * s, 0f), new Vector3(0.46f * s, 0.23f * s, 0.46f * s), matFurniture);
        Cyl(g, "Pot_Rim", new Vector3(0f, 0.46f * s, 0f), new Vector3(0.52f * s, 0.03f * s, 0.52f * s), matFurniture);
        Cyl(g, "Stem",    new Vector3(0f, 0.72f * s, 0f), new Vector3(0.07f * s, 0.26f * s, 0.07f * s), matPlant);

        Sphere(g, "Foliage_Main", new Vector3(0f, 1.26f * s, 0f), new Vector3(0.88f * s, 1.00f * s, 0.88f * s), matPlant);
        Sphere(g, "Foliage_A", new Vector3(0.28f * s, 1.00f * s, -0.18f * s), new Vector3(0.54f * s, 0.60f * s, 0.54f * s), matPlant);
        Sphere(g, "Foliage_B", new Vector3(-0.22f * s, 1.52f * s, 0.20f * s), new Vector3(0.44f * s, 0.50f * s, 0.44f * s), matPlant);

        Debug.Log("[V18] '" + name + "', Hoehe etwa " + (1.80f * s).ToString("0.00") + " m.");
    }

    // ============================================================ Sideboard
    // Der Telefontisch stand 13 cm in der Ostwand. Er wird zu einem
    // laenglichen Sideboard, buendig vor der Wand; Telefon und die
    // Rueckmeldung zum Notruf ziehen mit.
    static void Sideboard()
    {
        const float WALL_X = 7.90f;   // Innenseite der Ostwand
        const float DEPTH  = 0.50f;
        const float HEIGHT = 0.75f;
        const float LENGTH = 2.00f;
        const float CZ     = 1.00f;

        float cx = WALL_X - DEPTH / 2f;

        GameObject table = Find("Table_Phone");
        if (table != null)
        {
            Undo.RecordObject(table.transform, "sideboard");
            table.transform.position = new Vector3(cx, HEIGHT / 2f, CZ);
            table.transform.rotation = Quaternion.identity;
            table.transform.localScale = new Vector3(DEPTH, HEIGHT, LENGTH);
            Debug.Log("[V18] 'Table_Phone' ist jetzt ein Sideboard: " +
                      DEPTH.ToString("0.00") + " x " + LENGTH.ToString("0.00") +
                      " m, Oberkante " + HEIGHT.ToString("0.00") +
                      " m, buendig an der Ostwand (vorher 13 cm in der Wand).");
        }
        else Debug.LogWarning("[V18] 'Table_Phone' nicht gefunden.");

        GameObject phone = Find("Phone");
        if (phone != null)
        {
            Undo.RecordObject(phone.transform, "phone");
            Vector3 s = phone.transform.localScale;
            phone.transform.position = new Vector3(cx, HEIGHT + s.y / 2f, CZ);
            Debug.Log("[V18] 'Phone' liegt jetzt mittig auf dem Sideboard.");
        }

        // Rueckmeldung an die Wand ueber das Sideboard, zum Raum gedreht.
        GameObject plate = Find("Call_Feedback_Plate");
        if (plate != null)
        {
            Undo.RecordObject(plate.transform, "feedback plate");
            plate.transform.position = new Vector3(WALL_X - 0.02f, 1.45f, CZ);
            plate.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Debug.Log("[V18] 'Call_Feedback_Plate' an die Ostwand ueber das Sideboard gesetzt " +
                      "(stand vorher bei x 5,40, also frei im Raum).");
        }

        foreach (string n in new[] { "call_feedback_text", "Info_Call" })
        {
            GameObject t = Find(n);
            if (t == null) continue;
            Undo.RecordObject(t.transform, n);
            t.transform.position = new Vector3(WALL_X - 0.05f, n == "Info_Call" ? 1.80f : 1.45f, CZ);
            t.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
            Debug.Log("[V18] '" + n + "' vor die Tafel gesetzt, lesbar vom Raum aus " +
                      "(lag vorher im Weltursprung).");
        }
    }

    // ============================================================ Bausteine

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

    static void Cyl(GameObject p, string n, Vector3 pos, Vector3 sc, Material m)
    { Prim(PrimitiveType.Cylinder, p, n, pos, sc, m); }

    static void Sphere(GameObject p, string n, Vector3 pos, Vector3 sc, Material m)
    { Prim(PrimitiveType.Sphere, p, n, pos, sc, m); }

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

        if (m != null)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }

    static Material GetOrCreate(string assetName, Color c)
    {
        string path = "Assets/__My Project/" + assetName + ".mat";

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            if (matFurniture == null) return null;
            m = new Material(matFurniture);
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[V18] Material '" + path + "' angelegt.");
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
