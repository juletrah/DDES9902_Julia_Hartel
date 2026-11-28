// A1_BuildV42.cs
// Menue:  Assets > AED Build V42   (laeuft einmalig auch von selbst)
//
// Das HINT-Label stand spiegelverkehrt: TextMeshPro ist von seiner
// -z-Seite lesbar, die feste Drehung aus V39 zeigte die falsche Seite
// zum Raum. Statt die Drehung nur einmal zu korrigieren, bekommt das
// Label A2FaceCamera - dann steht es aus jeder Position lesbar, egal
// von wo die Person an den Patienten herantritt.
//
// Das Label haengt etwas hoeher ueber der Platte, damit es sich beim
// Drehen nicht in ihr verfaengt.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV42
{
    const string MENU = "Assets/AED Build V42";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V42_done";
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
        if (root == null) { Debug.LogError("[V42] Hint_Button fehlt - erst V39 laufen lassen."); return; }

        Undo.RegisterFullObjectHierarchyUndo(root, "Hint-Label drehen");

        Transform labelT = root.transform.Find("Label");
        if (labelT == null) { Debug.LogError("[V42] Label fehlt unter Hint_Button."); return; }

        GameObject label = labelT.gameObject;

        // Ueber die Platte heben und frei stellen.
        label.transform.localPosition = new Vector3(0f, 0.17f, 0f);
        label.transform.localRotation = Quaternion.identity;
        label.transform.localScale = Vector3.one;

        TextMeshPro t = label.GetComponent<TextMeshPro>();
        if (t != null)
        {
            t.text = "HINT";
            t.fontSize = 1.3f;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            RectTransform rt = t.rectTransform;
            if (rt != null) rt.sizeDelta = new Vector2(1.0f, 0.35f);
        }
        else Debug.LogWarning("[V42] Kein TextMeshPro am Label.");

        if (label.GetComponent<A2FaceCamera>() == null)
            label.AddComponent<A2FaceCamera>();

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V42] HINT-Label dreht sich jetzt zur Person und steht lesbar. " +
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
