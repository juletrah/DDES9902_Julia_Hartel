// A1_BuildV37.cs
// Menue:  Assets > AED Build V37   (laeuft einmalig auch von selbst)
//
// Zwei Aenderungen am Zeitdruck:
//
// 1. Ein START-Schild rechts neben der Instruktionstafel. Vorher laeuft
//    nichts: keine Uhr, kein Schritt, keine Messung. Das trennt zwei
//    Dinge, die bisher zusammenfielen - wann das Szenario beginnt, und
//    wann gehandelt werden muss. Sich einzulesen und die Steuerung zu
//    begreifen gehoert nicht zur Aufgabe; gemessene Orientierungszeit
//    ist nur Rauschen. Nach dem Druck gibt es keine Pause.
//
// 2. "Don't lose too much time!" verschwindet von der Tafel. Die Uhr
//    bleibt - eine Testperson las sie als sinnvolle Dringlichkeit. Was
//    Angst erzeugte, war die Ermahnung in Verbindung damit, ohne
//    Rueckmeldung festzustecken. Eine laufende Uhr draengt, eine
//    Aufforderung beschuldigt.
//
//   Instructions_Plate: Mitte (-0,43 | 1,53 | 5,86), 4,0 x 2,3 breit
//   -> rechte Kante x 1,57; das Schild kommt daneben.

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV37
{
    const string MENU = "Assets/AED Build V37";
    const string DIR  = "Assets/__My Project/";

    static readonly Vector3 SIGN_AT    = new Vector3(2.35f, 1.53f, 5.84f);
    static readonly Vector3 PLATE_SIZE = new Vector3(1.10f, 0.52f, 0.05f);

    const string BOARD_TEXT =
        "AED PRACTICE\nfor emergencies\n\n" +
        "1. Make an emergency call\n" +
        "2. Fetch the AED and place it next to the patient\n" +
        "3. Follow AED instructions\n\n" +
        "-> Apply the pads correctly. Wait for feedback.\n" +
        "-> Deliver shock if needed.\n\n" +
        "When you are ready, press START.";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V37_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 11)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V37");

        GameObject area = Find("A2_Patient_Area");
        A2Flow flow = area != null ? area.GetComponent<A2Flow>() : null;
        if (flow == null) { Debug.LogError("[V37] A2Flow fehlt - abgebrochen."); return; }

        // -- 1 Tafeltext ohne Ermahnung ----------------------------------
        int boards = 0;
        foreach (TextMeshPro t in Resources.FindObjectsOfTypeAll<TextMeshPro>())
        {
            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded) continue;
            if (t.text == null || !t.text.StartsWith("AED PRACTICE")) continue;

            Undo.RecordObject(t, "Board text");
            t.text = BOARD_TEXT;
            EditorUtility.SetDirty(t);
            boards++;
        }
        Debug.Log("[V37] " + boards + " Tafeltext(e) ersetzt, Ermahnung entfernt.");

        // -- 2 START-Schild ----------------------------------------------
        GameObject old = Find("Start_Sign");
        if (old != null) Undo.DestroyObjectImmediate(old);

        Material matGrab = AssetDatabase.LoadAssetAtPath<Material>(DIR + "Mat_Grab.mat");

        GameObject root = new GameObject("Start_Sign");
        Undo.RegisterCreatedObjectUndo(root, "Start_Sign");
        root.transform.SetParent(area.transform, true);
        root.transform.position = SIGN_AT;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Plate";
        plate.transform.SetParent(root.transform, false);
        plate.transform.localPosition = Vector3.zero;
        plate.transform.localRotation = Quaternion.identity;
        plate.transform.localScale = PLATE_SIZE;

        Renderer pr = plate.GetComponent<Renderer>();
        if (pr != null && matGrab != null) pr.sharedMaterial = matGrab;

        InteractableGeneral inter = plate.AddComponent<InteractableGeneral>();
        inter.hoverText = "START";
        if (inter.onPrimaryInteract == null) inter.onPrimaryInteract = new UnityEvent();
        UnityEventTools.AddPersistentListener(inter.onPrimaryInteract, (UnityAction)flow.OnStartClicked);

        // Schrift als Geschwister, damit die Plattenskalierung sie nicht verzerrt.
        GameObject label = new GameObject("Label");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0f, 0f, -0.035f);
        label.transform.localRotation = Quaternion.identity;
        label.transform.localScale = Vector3.one;

        TextMeshPro t2 = label.AddComponent<TextMeshPro>();
        t2.text = "START";
        t2.fontSize = 1.1f;
        t2.color = Color.white;
        t2.alignment = TextAlignmentOptions.Center;
        t2.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rt = label.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(1.6f, 0.4f);

        Undo.RecordObject(flow, "A2Flow");
        flow.startButton = root;
        EditorUtility.SetDirty(flow);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V37] Fertig. START-Schild bei " + SIGN_AT.ToString("0.00") +
                  ". Vor dem Druck laeuft weder Uhr noch Ablauf. Szene speichern mit Ctrl+S.");
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
