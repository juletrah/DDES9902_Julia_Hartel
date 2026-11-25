// A1_BuildV14.cs
// Menue:  Tools > AED Prototype > Build V14
//
// Macht den Patienten als Mensch lesbar, ohne die Greybox zu verlassen:
// aus einem einzelnen Quader werden Rumpf, zwei Arme und zwei Beine.
//
// Die Gesamtlaenge bleibt gleich (z 2,20 bis 3,80, Kopf schliesst bei
// 4,175 an). Der Rumpf wird auf die obere Haelfte verkuerzt, die untere
// Haelfte uebernehmen zwei Beinquader mit sichtbarem Spalt dazwischen.
// Die Arme liegen aussen neben dem Rumpf.
//
// Wichtig fuer die Pads: beide Anker liegen weiterhin ueber dem Rumpf
// (A bei z 3,60 / x 0,15 - B bei z 3,15 / x -0,17), die Oberseite bleibt
// bei y 0,30. Das Auflegen aus V11 funktioniert unveraendert.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV14
{
    const string MENU = "Tools/AED Prototype/Build V14";
    const string ROOT = "Patient_Limbs";

    // Rumpf: obere Haelfte des bisherigen Koerpers
    static readonly Vector3 TORSO_POS   = new Vector3(0f, 0.15f, 3.40f);
    static readonly Vector3 TORSO_SCALE = new Vector3(0.50f, 0.30f, 0.80f);

    struct Limb
    {
        public string name;
        public Vector3 pos;
        public Vector3 scale;
        public Limb(string n, Vector3 p, Vector3 s) { name = n; pos = p; scale = s; }
    }

    static readonly Limb[] LIMBS =
    {
        // Beine: untere Haelfte, zwei Quader mit Spalt in der Mitte
        new Limb("Patient_Leg_L", new Vector3( 0.13f, 0.13f, 2.60f), new Vector3(0.19f, 0.26f, 0.80f)),
        new Limb("Patient_Leg_R", new Vector3(-0.13f, 0.13f, 2.60f), new Vector3(0.19f, 0.26f, 0.80f)),
        // Arme: aussen laengs neben dem Rumpf
        new Limb("Patient_Arm_L", new Vector3( 0.315f, 0.08f, 3.45f), new Vector3(0.13f, 0.16f, 0.70f)),
        new Limb("Patient_Arm_R", new Vector3(-0.315f, 0.08f, 3.45f), new Vector3(0.13f, 0.16f, 0.70f)),
    };

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V14");

        GameObject body = Find("Patient_Body");
        if (body == null)
        {
            Debug.LogError("[V14] 'Patient_Body' nicht gefunden - abgebrochen.");
            return;
        }

        // Material vom Rumpf uebernehmen, damit alles gleich aussieht.
        Material mat = null;
        Renderer bodyRend = body.GetComponent<Renderer>();
        if (bodyRend != null) mat = bodyRend.sharedMaterial;

        // ------------------------------------------------- Rumpf kuerzen
        Undo.RecordObject(body.transform, "torso");
        Vector3 oldPos = body.transform.position;
        Vector3 oldScale = body.transform.localScale;
        body.transform.position = TORSO_POS;
        body.transform.localScale = TORSO_SCALE;
        Debug.Log(string.Format(
            "[V14] Rumpf: Laenge {0:0.00} -> {1:0.00} m, Mitte z {2:0.00} -> {3:0.00}. " +
            "Oberseite bleibt bei y {4:0.00}.",
            oldScale.z, TORSO_SCALE.z, oldPos.z, TORSO_POS.z,
            TORSO_POS.y + TORSO_SCALE.y / 2f));

        // ------------------------------------------------- Sammelobjekt
        GameObject root = Find(ROOT);
        if (root == null)
        {
            root = new GameObject(ROOT);
            Undo.RegisterCreatedObjectUndo(root, ROOT);
            Debug.Log("[V14] Objekt '" + ROOT + "' angelegt.");
        }
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        // ------------------------------------------------- Gliedmassen
        foreach (Limb l in LIMBS)
        {
            GameObject g = FindChild(root.transform, l.name);
            if (g == null)
            {
                g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = l.name;
                g.transform.SetParent(root.transform, false);
                Undo.RegisterCreatedObjectUndo(g, l.name);
                Debug.Log("[V14] '" + l.name + "' angelegt.");
            }

            Undo.RecordObject(g.transform, l.name);
            g.transform.position = l.pos;
            g.transform.rotation = Quaternion.identity;
            g.transform.localScale = l.scale;

            if (mat != null)
            {
                Renderer r = g.GetComponent<Renderer>();
                if (r != null)
                {
                    Undo.RecordObject(r, "material");
                    r.sharedMaterial = mat;
                }
            }
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V14] Fertig. Rumpf, 2 Beine, 2 Arme. Szene speichern mit Ctrl+S.");
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
