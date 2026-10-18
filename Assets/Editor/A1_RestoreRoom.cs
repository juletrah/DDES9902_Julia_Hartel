// A1_RestoreRoom.cs
// Stellt fehlende Raumteile von A1_Prototype wieder her.
// Menue:  Tools > AED Prototype > Restore Room
//
// Legt Boden, Waende und Decke mit ihren urspruenglichen Transformwerten neu
// an, falls sie geloescht wurden. Vorhandene Objekte werden nur korrigiert,
// nicht ersetzt - Rotation auf Null, Skalierung auf den Sollwert.
// Idempotent, laeuft ueber Undo.

using UnityEngine;
using UnityEditor;

public static class A1_RestoreRoom
{
    const string MENU = "Tools/AED Prototype/Restore Room";

    struct Part
    {
        public string name;
        public Vector3 pos;
        public Vector3 scale;
        public string mat;
        public Part(string n, Vector3 p, Vector3 s, string m)
        { name = n; pos = p; scale = s; mat = m; }
    }

    static readonly Part[] PARTS =
    {
        new Part("Floor",      new Vector3( 0f, -0.1f,  0f), new Vector3(10f,  0.2f, 10f),   "Mat_Floor"),
        new Part("Wall_North", new Vector3( 0f,  1.5f,  5f), new Vector3(10f,  3f,    0.2f), "Mat_Room"),
        new Part("Wall_South", new Vector3( 0f,  1.5f, -5f), new Vector3(10f,  3f,    0.2f), "Mat_Room"),
        new Part("Wall_East",  new Vector3( 5f,  1.5f,  0f), new Vector3( 0.2f, 3f,  10f),   "Mat_Room"),
        new Part("Wall_West",  new Vector3(-5f,  1.5f,  0f), new Vector3( 0.2f, 3f,  10f),   "Mat_Room"),
        new Part("Ceiling",    new Vector3( 0f,  3.1f,  0f), new Vector3(10.2f, 0.2f, 10.2f),"Mat_Room"),
    };

    [MenuItem(MENU)]
    public static void Restore()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restore Room");

        int created = 0, fixedUp = 0;

        foreach (var part in PARTS)
        {
            var go = Find(part.name);
            bool isNew = false;

            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = part.name;
                Undo.RegisterCreatedObjectUndo(go, part.name);
                isNew = true;
                created++;
            }
            else
            {
                fixedUp++;
            }

            Undo.RecordObject(go.transform, part.name + " transform");
            go.transform.SetParent(null, true);
            go.transform.position = part.pos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = part.scale;

            if (!go.activeSelf)
            {
                Undo.RecordObject(go, part.name + " aktivieren");
                go.SetActive(true);
            }

            // Ein Boden ohne Collider laesst den Player durchfallen.
            if (go.GetComponent<Collider>() == null)
                Undo.AddComponent<BoxCollider>(go);

            if (isNew)
            {
                var mat = FindMaterial(part.mat);
                var r = go.GetComponent<Renderer>();
                if (mat != null && r != null) r.sharedMaterial = mat;
            }
        }

        Undo.CollapseUndoOperations(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[A1] Raum wiederhergestellt: " + created + " neu angelegt, " +
                  fixedUp + " geprueft und ausgerichtet.");
    }

    static GameObject Find(string n)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var r in scene.GetRootGameObjects())
        {
            var hit = FindRecursive(r.transform, n);
            if (hit != null) return hit;
        }
        return null;
    }

    static GameObject FindRecursive(Transform t, string n)
    {
        if (t.name == n) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindRecursive(t.GetChild(i), n);
            if (hit != null) return hit;
        }
        return null;
    }

    static Material FindMaterial(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Material"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null && m.name == name) return m;
        }
        return null;
    }
}
