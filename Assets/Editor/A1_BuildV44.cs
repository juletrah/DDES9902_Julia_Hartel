// A1_BuildV44.cs
// Menue:  Assets > AED Build V44   (laeuft einmalig auch von selbst)
//
// Schild und Schrift werden ein Stueck.
//
// Bisher drehte sich nur das Label, die gelbe Platte blieb stehen - die
// Schrift schwebte daneben. Jetzt dreht sich der ganze Traeger, und das
// Label liegt flach auf der Platte.
//
// Dazu muss die Platte anders herum stehen: ihre grosse Flaeche lag auf
// der x-Achse, beim Drehen zur Person haette man nur die schmale Kante
// gesehen. Sie wird deshalb breit und duenn in der z-Achse aufgebaut -
// das ist die Seite, die A2FaceCamera zur Person dreht.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV44
{
    const string MENU = "Assets/AED Build V44";

    // Breit und flach: grosse Flaeche auf der z-Achse.
    static readonly Vector3 PLATE_SIZE = new Vector3(0.78f, 0.24f, 0.03f);

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V44_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 7)]
    public static void Build()
    {
        GameObject root = Find("Hint_Button");
        if (root == null) { Debug.LogError("[V44] Hint_Button fehlt."); return; }

        Undo.RegisterFullObjectHierarchyUndo(root, "Hint-Schild");

        // Der ganze Traeger dreht sich, nicht mehr nur die Schrift.
        A2FaceCamera rootFace = root.GetComponent<A2FaceCamera>();
        if (rootFace == null) rootFace = root.AddComponent<A2FaceCamera>();
        rootFace.keepUpright = true;

        Transform plateT = root.transform.Find("Plate");
        if (plateT != null)
        {
            plateT.localPosition = Vector3.zero;
            plateT.localRotation = Quaternion.identity;
            plateT.localScale = PLATE_SIZE;
        }
        else Debug.LogWarning("[V44] Plate nicht gefunden.");

        Transform labelT = root.transform.Find("Label");
        if (labelT != null)
        {
            // Das Label dreht sich nicht mehr selbst, sonst wuerde es
            // gegen die Drehung des Traegers arbeiten.
            A2FaceCamera old = labelT.GetComponent<A2FaceCamera>();
            if (old != null) Undo.DestroyObjectImmediate(old);

            // Knapp vor der Platte auf der Seite, die zur Person zeigt.
            // TMP ist von seiner -z-Seite lesbar, und der Traeger richtet
            // genau diese Seite auf die Person aus.
            labelT.localPosition = new Vector3(0f, 0f, -0.025f);
            labelT.localRotation = Quaternion.identity;
            labelT.localScale = Vector3.one;

            TextMeshPro t = labelT.GetComponent<TextMeshPro>();
            if (t != null)
            {
                t.text = "SHOW HINT";
                t.fontSize = 0.72f;
                t.color = Color.white;
                t.alignment = TextAlignmentOptions.Center;
                t.enableWordWrapping = false;
                RectTransform rt = t.rectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(0.74f, 0.22f);
            }
        }
        else Debug.LogWarning("[V44] Label nicht gefunden.");

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V44] Schild und Schrift sind ein Stueck und drehen sich gemeinsam zur Person. " +
                  "Szene speichern mit Ctrl+S.");
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
