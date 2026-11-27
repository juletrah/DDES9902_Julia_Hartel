// A1_BuildV35.cs
// Menue:  Assets > AED Build V35   (laeuft einmalig auch von selbst)
//
// Korrigiert die Beschriftung aus V34. Dort hing sie als Kind im
// Klickwuerfel, der ungleichmaessig skaliert ist (0,10 x 0,45 x 0,55).
// Die Gegenskalierung im Kind kann das nicht ausgleichen, sobald das
// Kind gedreht ist: die Achsen zeigen dann woanders hin als die
// Skalierung des Elternteils, und die Schrift wird geschert.
//
// Jetzt haengt sie unskaliert unter A2_Patient_Area und steht frei vor
// dem Schrank. Kleiner als vorher.
//
//   AED_Cabinet: Mitte (-7,63 | 1,30 | -4,00), Groesse 0,55 x 0,75 x 0,65
//   -> Vorderkante x -7,355, Unterkante y 0,925
//   Die Schrift sitzt knapp darunter und knapp davor.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV35
{
    const string MENU = "Assets/AED Build V35";

    static readonly Vector3 LABEL_AT = new Vector3(-7.32f, 0.82f, -4.00f);
    const float LABEL_SIZE = 0.6f;

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V35_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 9)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V35");

        GameObject old = Find("AED_Grab_Label");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject area = Find("A2_Patient_Area");
        if (area == null) { Debug.LogError("[V35] A2_Patient_Area fehlt - abgebrochen."); return; }

        GameObject label = new GameObject("AED_Grab_Label");
        Undo.RegisterCreatedObjectUndo(label, "AED_Grab_Label");
        label.transform.SetParent(area.transform, true);
        label.transform.position = LABEL_AT;
        label.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
        label.transform.localScale = Vector3.one;      // keine Verzerrung mehr

        TextMeshPro t = label.AddComponent<TextMeshPro>();
        t.text = "TAKE AED";
        t.fontSize = LABEL_SIZE;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rt = label.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(1.2f, 0.3f);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V35] Fertig. 'TAKE AED' unverzerrt bei " + LABEL_AT.ToString("0.00") +
                  ", Schriftgroesse " + LABEL_SIZE + ". Szene speichern mit Ctrl+S.");
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
