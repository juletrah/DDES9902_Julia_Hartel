// A1_BuildV50.cs
// Menue:  Assets > AED Build V50   (laeuft einmalig auch von selbst)
//
// Text gekuerzt: "You will be guided." wandert in die Situation, der
// Absatz "You don't have to remember all of this ..." und der
// Uhr-Absatz fallen weg. Weniger Text heisst, dass er auch gelesen wird.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV50
{
    const string MENU = "Assets/AED Build V50";

    const string BODY_TEXT =
        "<size=120%><b>Have you ever used a defibrillator?</b></size>\n\n" +
        "<align=left>" +
        "<b>The situation</b>\n" +
        "You are in an office. Someone has collapsed, and there is a " +
        "defibrillator (AED) somewhere in the room. Your task is to use it. " +
        "You will be guided.\n\n" +
        "<b>What to do</b>\n" +
        "<b>1.</b>   Make an emergency call\n" +
        "<b>2.</b>   Find the AED and bring it to the person\n" +
        "<b>3.</b>   Follow the instructions the AED gives you" +
        "</align>";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V50_done";
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
        GameObject panel = Find("Intro_Panel");
        if (panel == null) { Debug.LogError("[V50] Intro_Panel fehlt."); return; }

        Transform cardT = panel.transform.Find("Card");
        if (cardT == null) { Debug.LogError("[V50] Card fehlt."); return; }

        Transform bodyT = cardT.Find("Text_Body");
        if (bodyT == null) { Debug.LogError("[V50] Text_Body fehlt - erst V49 laufen lassen."); return; }

        TextMeshProUGUI body = bodyT.GetComponent<TextMeshProUGUI>();
        if (body == null) { Debug.LogError("[V50] Kein TextMeshProUGUI an Text_Body."); return; }

        Undo.RecordObject(body, "Intro-Text kuerzen");
        body.text = BODY_TEXT;
        body.ForceMeshUpdate();
        EditorUtility.SetDirty(body);

        // Die Karte darf schrumpfen, sonst steht der kuerzere Text in
        // einer halbleeren Flaeche.
        RectTransform cr = cardT.GetComponent<RectTransform>();
        if (cr != null)
        {
            Undo.RecordObject(cr, "Karte anpassen");
            cr.sizeDelta = new Vector2(1340f, 760f);
            EditorUtility.SetDirty(cr);
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V50] Text gekuerzt, Karte auf 1340x760 verkleinert. " +
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
