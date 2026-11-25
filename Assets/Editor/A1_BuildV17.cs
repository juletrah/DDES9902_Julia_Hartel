// A1_BuildV17.cs
// Menue:  Tools > AED Prototype > Build V17
//
// Reine Deko: Arbeitsplaetze und Zimmerpflanzen, damit der Raum nach
// Buero aussieht statt nach Behandlungszimmer.
//
// Alles liegt unter 'Office_Decor' und laesst sich dort mit einem Haken
// komplett aus- und einschalten. Das Skript ist wiederholbar: es sucht
// seine Objekte am Namen und aendert sie, statt neue anzulegen.
//
// Keine Interaktion: weder Holdable noch InteractableTrigger. Die Collider
// bleiben, damit man nicht durch Moebel hindurchlaeuft.
//
// Aufteilung: drei Einzelplaetze in den freien Ecken (Nordwest, Nordost,
// Suedost) und drei frei im Raum stehende Doppelarbeitsplaetze. Belegt und
// deshalb gemieden: Westwand bei z -4 (AED), Trennwand bei z -2 von x -8
// bis -4, Nordwand (Tafel, Tuer, Uhr), Telefontisch im Osten bei z 0,6 bis
// 1,4, sowie Mitte mit Patient und Spielerstart.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV17
{
    const string MENU = "Tools/AED Prototype/Build V17";
    const string ROOT = "Office_Decor";

    // Buerohoehen, bewusst an realen Massen orientiert, damit die Moebel
    // neben der 1,98 m langen Figur stimmig wirken.
    const float DESK_TOP  = 0.76f;   // Oberkante Tischplatte
    const float TOP_T     = 0.05f;   // Plattenstaerke
    const float SEAT_H    = 0.47f;   // Sitzhoehe

    static Material matFurniture;
    static Material matBlack;
    static Material matPlant;

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V17");

        GameObject refTable = Find("Table_Phone");
        if (refTable != null)
        {
            Renderer r = refTable.GetComponent<Renderer>();
            if (r != null) matFurniture = r.sharedMaterial;
        }
        if (matFurniture == null)
            Debug.LogWarning("[V17] Kein Moebel-Material gefunden, Standardmaterial wird benutzt.");

        matBlack = GetOrCreate("Mat_OfficeBlack", new Color(0.11f, 0.11f, 0.12f));
        matPlant = GetOrCreate("Mat_Plant",       new Color(0.24f, 0.45f, 0.22f));

        GameObject root = Find(ROOT);
        if (root == null)
        {
            root = new GameObject(ROOT);
            Undo.RegisterCreatedObjectUndo(root, ROOT);
            Debug.Log("[V17] Sammelobjekt '" + ROOT + "' angelegt.");
        }
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        // ---------------------------------------- Einzelplaetze in den Ecken
        Desk(root, "Workstation_NW", new Vector2(-6.10f,  4.85f), -1f);
        Desk(root, "Workstation_NE", new Vector2( 5.90f,  4.85f), -1f);
        Desk(root, "Workstation_SE", new Vector2( 5.90f, -4.85f), +1f);

        // ---------------------------------------- Doppelplaetze im Raum
        SharedDesk(root, "Shared_W", new Vector2(-5.20f,  1.60f));
        SharedDesk(root, "Shared_S", new Vector2( 0.40f, -4.70f));
        SharedDesk(root, "Shared_E", new Vector2( 4.90f, -2.40f));

        // ---------------------------------------- Pflanzen
        Plant(root, "Plant_NW", new Vector2(-7.30f,  2.60f), 1.00f);
        Plant(root, "Plant_E",  new Vector2( 7.30f,  3.10f), 0.88f);
        Plant(root, "Plant_N",  new Vector2( 1.90f,  5.25f), 0.95f);

        CleanUpObsolete(root);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V17] Fertig. 3 Einzelplaetze, 3 Doppelplaetze, 3 Pflanzen unter '" +
                  ROOT + "'. Szene speichern mit Ctrl+S.");
    }

    // ------------------------------------------------------------ Einzelplatz
    // chairSide: -1 = Stuhl Richtung -z, +1 = Richtung +z
    static void Desk(GameObject root, string name, Vector2 at, float chairSide)
    {
        GameObject d = Child(root.transform, name);
        float x = at.x, z = at.y;

        float w = 1.80f;   // Breite
        float dp = 0.90f;  // Tiefe

        Box(d, "Desk_Top",    new Vector3(x, DESK_TOP - TOP_T / 2f, z), new Vector3(w, TOP_T, dp), matFurniture);
        Box(d, "Desk_Side_L", new Vector3(x - w / 2f + 0.04f, (DESK_TOP - TOP_T) / 2f, z), new Vector3(0.06f, DESK_TOP - TOP_T, dp - 0.08f), matFurniture);
        Box(d, "Desk_Side_R", new Vector3(x + w / 2f - 0.04f, (DESK_TOP - TOP_T) / 2f, z), new Vector3(0.06f, DESK_TOP - TOP_T, dp - 0.08f), matFurniture);
        Box(d, "Desk_Back",   new Vector3(x, 0.52f, z - chairSide * (dp / 2f - 0.03f)), new Vector3(w - 0.12f, 0.42f, 0.03f), matFurniture);

        Screen(d, x, z - chairSide * 0.26f, 1.0f);
        Box(d, "Keyboard", new Vector3(x, DESK_TOP + 0.012f, z + chairSide * 0.16f), new Vector3(0.48f, 0.025f, 0.17f), matBlack);

        Chair(d, "Chair", new Vector3(x, 0f, z + chairSide * 0.90f), chairSide);

        Debug.Log("[V17] '" + name + "' bei x " + x.ToString("0.0") + " / z " + z.ToString("0.0") + ".");
    }

    // ------------------------------------------------------------ Doppelplatz
    // Zwei Plaetze Ruecken an Ruecken, Sichtblende in der Mitte, Stuehle aussen.
    static void SharedDesk(GameObject root, string name, Vector2 at)
    {
        GameObject d = Child(root.transform, name);
        float x = at.x, z = at.y;

        float w = 2.00f;   // Breite in x
        float dp = 1.60f;  // Tiefe in z, beide Seiten zusammen

        Box(d, "Desk_Top",    new Vector3(x, DESK_TOP - TOP_T / 2f, z), new Vector3(w, TOP_T, dp), matFurniture);
        Box(d, "Desk_Side_L", new Vector3(x - w / 2f + 0.04f, (DESK_TOP - TOP_T) / 2f, z), new Vector3(0.06f, DESK_TOP - TOP_T, dp - 0.14f), matFurniture);
        Box(d, "Desk_Side_R", new Vector3(x + w / 2f - 0.04f, (DESK_TOP - TOP_T) / 2f, z), new Vector3(0.06f, DESK_TOP - TOP_T, dp - 0.14f), matFurniture);
        // Sichtblende auf der Platte
        Box(d, "Divider", new Vector3(x, DESK_TOP + 0.20f, z), new Vector3(w - 0.10f, 0.40f, 0.04f), matFurniture);

        // Arbeitsplatz Nord und Sued
        Screen(d, x, z - 0.30f, -1.0f, "_N");
        Box(d, "Keyboard_N", new Vector3(x, DESK_TOP + 0.012f, z - 0.58f), new Vector3(0.48f, 0.025f, 0.17f), matBlack);
        Chair(d, "Chair_N", new Vector3(x, 0f, z - 1.28f), -1f);

        Screen(d, x, z + 0.30f, 1.0f, "_S");
        Box(d, "Keyboard_S", new Vector3(x, DESK_TOP + 0.012f, z + 0.58f), new Vector3(0.48f, 0.025f, 0.17f), matBlack);
        Chair(d, "Chair_S", new Vector3(x, 0f, z + 1.28f), 1f);

        Debug.Log("[V17] '" + name + "' bei x " + x.ToString("0.0") + " / z " + z.ToString("0.0") + ".");
    }

    // Monitor mit Fuss. facing gibt an, zu welcher Seite der Bildschirm zeigt.
    static void Screen(GameObject parent, float x, float z, float facing, string suffix = "")
    {
        Box(parent, "Monitor_Foot" + suffix, new Vector3(x, DESK_TOP + 0.015f, z), new Vector3(0.26f, 0.03f, 0.16f), matBlack);
        Box(parent, "Monitor_Neck" + suffix, new Vector3(x, DESK_TOP + 0.13f, z), new Vector3(0.07f, 0.22f, 0.07f), matBlack);
        Box(parent, "Monitor" + suffix,      new Vector3(x, DESK_TOP + 0.44f, z), new Vector3(0.66f, 0.40f, 0.035f), matBlack);
    }

    // Buerostuhl, schwarz. facing: Rueckenlehne zeigt von der Platte weg.
    static void Chair(GameObject parent, string name, Vector3 at, float facing)
    {
        float x = at.x, z = at.z;
        Box(parent, name + "_Base",   new Vector3(x, 0.035f, z), new Vector3(0.58f, 0.06f, 0.58f), matBlack);
        Box(parent, name + "_Column", new Vector3(x, SEAT_H / 2f, z), new Vector3(0.10f, SEAT_H - 0.03f, 0.10f), matBlack);
        Box(parent, name + "_Seat",   new Vector3(x, SEAT_H, z), new Vector3(0.52f, 0.08f, 0.52f), matBlack);
        Box(parent, name + "_Back",   new Vector3(x, SEAT_H + 0.33f, z + facing * 0.24f), new Vector3(0.52f, 0.60f, 0.06f), matBlack);
    }

    // ------------------------------------------------------------ Pflanze
    // Topf, Stamm, drei versetzte Blattkugeln. s skaliert die ganze Pflanze.
    static void Plant(GameObject root, string name, Vector2 at, float s)
    {
        GameObject p = Child(root.transform, name);
        float x = at.x, z = at.y;

        Cyl(p, "Pot",     new Vector3(x, 0.23f * s, z), new Vector3(0.46f * s, 0.23f * s, 0.46f * s), matFurniture);
        Cyl(p, "Pot_Rim", new Vector3(x, 0.46f * s, z), new Vector3(0.52f * s, 0.03f * s, 0.52f * s), matFurniture);
        Cyl(p, "Stem",    new Vector3(x, 0.72f * s, z), new Vector3(0.07f * s, 0.26f * s, 0.07f * s), matPlant);

        Sphere(p, "Foliage_Main", new Vector3(x, 1.26f * s, z),
               new Vector3(0.88f * s, 1.00f * s, 0.88f * s), matPlant);
        Sphere(p, "Foliage_A", new Vector3(x + 0.30f * s, 1.00f * s, z - 0.20f * s),
               new Vector3(0.56f * s, 0.62f * s, 0.56f * s), matPlant);
        Sphere(p, "Foliage_B", new Vector3(x - 0.24f * s, 1.52f * s, z + 0.22f * s),
               new Vector3(0.46f * s, 0.52f * s, 0.46f * s), matPlant);

        Debug.Log("[V17] '" + name + "' bei x " + x.ToString("0.0") + " / z " + z.ToString("0.0") +
                  ", Hoehe etwa " + (1.80f * s).ToString("0.00") + " m.");
    }

    // ------------------------------------------------------------ Bausteine

    // Teile aus einem frueheren Lauf dieses Skripts, die es nicht mehr gibt.
    // Ohne das bleiben sie als Reste an ihrer alten Stelle stehen.
    static readonly string[] OBSOLETE = { "Monitor_Stand", "Foliage", "Foliage_Side" };

    static void CleanUpObsolete(GameObject root)
    {
        int n = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == null || t == root.transform) continue;
            foreach (string o in OBSOLETE)
            {
                if (t.name != o) continue;
                Debug.Log("[V17] Rest aus frueherem Lauf entfernt: " +
                          t.parent.name + "/" + t.name);
                Undo.DestroyObjectImmediate(t.gameObject);
                n++;
                break;
            }
        }
        if (n > 0) Debug.Log("[V17] " + n + " Rest(e) entfernt.");
    }

    static GameObject Child(Transform parent, string name)
    {
        foreach (Transform t in parent)
            if (t.name == name) return t.gameObject;

        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(g, name);
        return g;
    }

    static void Box(GameObject p, string n, Vector3 pos, Vector3 sc, Material m)
    { Prim(PrimitiveType.Cube, p, n, pos, sc, m); }

    static void Cyl(GameObject p, string n, Vector3 pos, Vector3 sc, Material m)
    { Prim(PrimitiveType.Cylinder, p, n, pos, sc, m); }

    static void Sphere(GameObject p, string n, Vector3 pos, Vector3 sc, Material m)
    { Prim(PrimitiveType.Sphere, p, n, pos, sc, m); }

    static void Prim(PrimitiveType type, GameObject parent, string name,
                     Vector3 pos, Vector3 scale, Material m)
    {
        GameObject g = null;
        foreach (Transform t in parent.transform)
            if (t.name == name) { g = t.gameObject; break; }

        if (g == null)
        {
            g = GameObject.CreatePrimitive(type);
            g.name = name;
            g.transform.SetParent(parent.transform, false);
            g.isStatic = true;
            Undo.RegisterCreatedObjectUndo(g, name);
        }

        Undo.RecordObject(g.transform, name);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.identity;
        g.transform.localScale = scale;

        if (m != null)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r != null) { Undo.RecordObject(r, "mat"); r.sharedMaterial = m; }
        }
    }

    // Material als Kopie eines vorhandenen anlegen, damit der Shader der des
    // Projekts bleibt und nichts per Namen geraten werden muss.
    static Material GetOrCreate(string assetName, Color c)
    {
        string path = "Assets/__My Project/" + assetName + ".mat";

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            if (matFurniture == null) return null;
            m = new Material(matFurniture);
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[V17] Material '" + path + "' angelegt.");
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
