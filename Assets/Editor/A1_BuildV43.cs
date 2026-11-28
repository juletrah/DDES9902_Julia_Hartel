// A1_BuildV43.cs
// Menue:  Assets > AED Build V43   (laeuft einmalig auch von selbst)
//
// 1) Beschriftung "HINT" -> "SHOW HINT".
// 2) Sicherung fuer das Aufnehmen der Pads: dropOnKeyLift und groundPlace
//    muessen aus sein, sonst legt der Interactor das Pad schon beim
//    Loslassen der Maustaste ab. Die eigentliche Ursache lag woanders
//    (stehengebliebenes throwFlag, behoben in A2Flow.Freeze), diese
//    Werte werden hier nur geprueft und protokolliert.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV43
{
    const string MENU = "Assets/AED Build V43";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V43_done";
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
        // --- 1) Beschriftung -------------------------------------------
        GameObject root = Find("Hint_Button");
        if (root != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Hint-Beschriftung");

            Transform labelT = root.transform.Find("Label");
            if (labelT != null)
            {
                TextMeshPro t = labelT.GetComponent<TextMeshPro>();
                if (t != null)
                {
                    t.text = "SHOW HINT";
                    // Breiter als "HINT", damit nichts umbricht.
                    t.fontSize = 1.1f;
                    RectTransform rt = t.rectTransform;
                    if (rt != null) rt.sizeDelta = new Vector2(1.8f, 0.35f);
                    Debug.Log("[V43] Beschriftung jetzt 'SHOW HINT'.");
                }
            }

            Transform plateT = root.transform.Find("Plate");
            if (plateT != null)
            {
                InteractableGeneral ig = plateT.GetComponent<InteractableGeneral>();
                if (ig != null) ig.hoverText = "SHOW ME WHERE THE PADS GO";
            }

            EditorUtility.SetDirty(root);
        }
        else Debug.LogWarning("[V43] Hint_Button nicht gefunden.");

        // --- 2) Pad-Einstellungen pruefen ------------------------------
        CheckPad("Pad_A");
        CheckPad("Pad_B");

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[V43] Fertig. Szene speichern mit Ctrl+S.");
    }

    static void CheckPad(string name)
    {
        GameObject go = Find(name);
        if (go == null) { Debug.LogWarning("[V43] " + name + " nicht gefunden."); return; }

        Holdable h = go.GetComponent<Holdable>();
        if (h == null) { Debug.LogWarning("[V43] " + name + " hat kein Holdable."); return; }

        Undo.RecordObject(h, "Pad-Einstellungen");

        Rigidbody rb = go.GetComponent<Rigidbody>();
        Debug.Log("[V43] " + name +
                  " vorher: dropOnKeyLift=" + h.dropOnKeyLift +
                  ", groundPlace=" + h.groundPlace +
                  ", noCollideOnHold=" + h.noCollideOnHold +
                  ", freezeRotation=" + h.freezeRotation +
                  ", magnetSnapper=" + (h.myMagnetSnapper != null) +
                  ", rigidbody=" + (rb != null ? ("useGravity=" + rb.useGravity +
                                                  ", isKinematic=" + rb.isKinematic) : "keiner"));

        h.dropOnKeyLift = false;   // sonst faellt das Pad beim Loslassen
        h.groundPlace   = false;   // sonst klebt es sofort am Boden

        EditorUtility.SetDirty(h);
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
