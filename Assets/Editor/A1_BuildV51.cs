// A1_BuildV51.cs
// Menue:  Assets > AED Build V51   (laeuft einmalig auch von selbst)
//
// Die Entlastung kommt zurueck, aber als eine Zeile statt zweier
// Absaetze. Sie ist die Antwort auf Xiaohans Rueckmeldung, dass die
// laufende Uhr Druck erzeugt hat - und damit eine ethische Entscheidung,
// die im Report belegbar sein soll.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV51
{
    const string MENU = "Assets/AED Build V51";

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
        "<b>3.</b>   Follow the instructions the AED gives you\n" +
        "</align>\n" +
        "<i>A clock runs in the background, but take your time. " +
        "This is practice, not a test.</i>";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V51_done";
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
        if (panel == null) { Debug.LogError("[V51] Intro_Panel fehlt."); return; }

        Transform cardT = panel.transform.Find("Card");
        if (cardT == null) { Debug.LogError("[V51] Card fehlt."); return; }

        Transform bodyT = cardT.Find("Text_Body");
        if (bodyT == null) { Debug.LogError("[V51] Text_Body fehlt."); return; }

        TextMeshProUGUI body = bodyT.GetComponent<TextMeshProUGUI>();
        if (body == null) { Debug.LogError("[V51] Kein TextMeshProUGUI an Text_Body."); return; }

        Undo.RecordObject(body, "Uhr-Hinweis zurueck");
        body.text = BODY_TEXT;
        body.ForceMeshUpdate();
        EditorUtility.SetDirty(body);

        RectTransform cr = cardT.GetComponent<RectTransform>();
        if (cr != null)
        {
            Undo.RecordObject(cr, "Karte anpassen");
            cr.sizeDelta = new Vector2(1340f, 830f);
            EditorUtility.SetDirty(cr);
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V51] Uhr-Hinweis als eine Zeile zurueck, Karte auf 1340x830. " +
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
