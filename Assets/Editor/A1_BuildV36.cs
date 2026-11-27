// A1_BuildV36.cs
// Menue:  Assets > AED Build V36   (laeuft einmalig auch von selbst)
//
// Das Klickfeld wandert von der Schrankvorderseite auf ein Schild
// darunter. Grund: die gruene Platte verdeckte den AED, und "das Geraet
// im Raum finden" ist eine der Lernaufgaben - ein Ziel, das das Objekt
// verdeckt, nimmt genau diese Aufgabe weg.
//
// Aufbau als Gruppe, nicht als ein Wuerfel:
//   AED_Grab        leer, Skalierung 1  <- A2Flow blendet dieses aus
//     Plate         die gruene Platte, traegt Collider + Interaktion
//     Label         die Schrift, unskaliert und damit unverzerrt
//
// Die Schrift muss ein Geschwister der Platte sein, kein Kind: die
// Platte ist ungleichmaessig skaliert, und eine gedrehte Schrift darin
// wird geschert. Die Interaktion wiederum muss auf demselben Objekt wie
// der Collider sitzen, weil der EZPZ-Strahl dort danach sucht.
//
//   AED_Cabinet: Mitte (-7,63 | 1,30 | -4,00), Unterkante y 0,925

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV36
{
    const string MENU = "Assets/AED Build V36";
    const string DIR  = "Assets/__My Project/";

    static readonly Vector3 SIGN_AT    = new Vector3(-7.62f, 0.78f, -4.00f);
    static readonly Vector3 PLATE_SIZE = new Vector3(0.04f, 0.26f, 0.66f);
    const float LABEL_SIZE = 0.8f;

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V36_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 10)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V36");

        GameObject area = Find("A2_Patient_Area");
        A2Flow flow = area != null ? area.GetComponent<A2Flow>() : null;
        if (flow == null) { Debug.LogError("[V36] A2Flow fehlt - abgebrochen."); return; }

        foreach (string n in new[] { "AED_Grab", "AED_Grab_Label" })
        {
            GameObject o = Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }

        Material matGrab = AssetDatabase.LoadAssetAtPath<Material>(DIR + "Mat_Grab.mat");

        // -- Gruppe ------------------------------------------------------
        GameObject root = new GameObject("AED_Grab");
        Undo.RegisterCreatedObjectUndo(root, "AED_Grab");
        root.transform.SetParent(area.transform, true);
        root.transform.position = SIGN_AT;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        // -- Platte mit Collider und Interaktion -------------------------
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Plate";
        plate.transform.SetParent(root.transform, false);
        plate.transform.localPosition = Vector3.zero;
        plate.transform.localRotation = Quaternion.identity;
        plate.transform.localScale = PLATE_SIZE;

        Renderer pr = plate.GetComponent<Renderer>();
        if (pr != null && matGrab != null) pr.sharedMaterial = matGrab;

        InteractableGeneral inter = plate.AddComponent<InteractableGeneral>();
        inter.hoverText = "TAKE AED";
        if (inter.onPrimaryInteract == null) inter.onPrimaryInteract = new UnityEvent();
        UnityEventTools.AddPersistentListener(inter.onPrimaryInteract, (UnityAction)flow.OnAedGrabClicked);

        // -- Schrift, Geschwister der Platte -----------------------------
        GameObject label = new GameObject("Label");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0.035f, 0f, 0f);
        label.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
        label.transform.localScale = Vector3.one;

        TextMeshPro t = label.AddComponent<TextMeshPro>();
        t.text = "TAKE AED";
        t.fontSize = LABEL_SIZE;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rt = label.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(1.4f, 0.35f);

        root.SetActive(true);

        Undo.RecordObject(flow, "A2Flow");
        flow.aedGrabButton = root;
        EditorUtility.SetDirty(flow);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V36] Fertig. Schild unter dem Schrank bei " + SIGN_AT.ToString("0.00") +
                  ", Schrankvorderseite wieder frei. Szene speichern mit Ctrl+S.");
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
