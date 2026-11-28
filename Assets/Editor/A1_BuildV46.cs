// A1_BuildV46.cs
// Menue:  Assets > AED Build V46   (laeuft einmalig auch von selbst)
//
// Neuer Text fuer das Start-Overlay.
//
// Geaendert gegenueber V41:
// - Einstiegsfrage statt direktem Aufgabenstart. Sie steht ohne Antwort
//   da; der Leser beantwortet sie selbst und liest deshalb weiter.
// - Kurze Lagebeschreibung, damit klar ist, wo man gleich steht.
// - Nur noch die drei Schritte, keine Pad- und Schockhinweise mehr. Was
//   dort zu tun ist, sagt der AED im richtigen Moment - vorab gelesen
//   waere es ohnehin vergessen.
// - Hinweis auf die Tafel im Raum: die Person muss sich nichts merken
//   und erfaehrt nebenbei, wofuer die Tafel da ist.
// - Zeithinweis. Antwort auf die Rueckmeldung aus Iteration 0, dass die
//   laufende Uhr Druck erzeugt, wenn man feststeckt. "Figure out what to
//   do" sagt zugleich, dass Ausprobieren erwuenscht ist - genau das soll
//   beobachtet werden.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV46
{
    const string MENU = "Assets/AED Build V46";

    const string INTRO_TEXT =
        "<size=150%><b>AED PRACTICE</b></size>\n\n" +
        "<size=120%><b>Have you ever used a defibrillator?</b></size>\n\n" +
        "<align=left>" +
        "<b>The situation</b>\n" +
        "You are in an office. Someone has collapsed, and there is a " +
        "defibrillator (AED) somewhere in the room. Your task is to use it.\n\n" +
        "<b>What to do</b>\n" +
        "<b>1.</b>  Make an emergency call\n" +
        "<b>2.</b>  Find the AED and bring it to the person\n" +
        "<b>3.</b>  Follow the instructions the AED gives you\n\n" +
        "<i>You don't have to remember all of this. The steps are also on the " +
        "board in the room, and the AED guides you through them once you " +
        "reach it.</i>\n\n" +
        "A clock runs in the background, but this is practice, not a test. " +
        "Take your time, look around, figure out what to do." +
        "</align>\n\n" +
        "<size=85%>Move with W A S D or the arrow keys · look with the mouse · " +
        "left mouse button to pick things up and press things</size>\n\n" +
        "<size=130%><b>Press SPACE to start</b></size>\n" +
        "<size=85%>The clock starts when you press space.</size>";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V46_done";
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
        if (panel == null) { Debug.LogError("[V46] Intro_Panel fehlt."); return; }

        Undo.RegisterFullObjectHierarchyUndo(panel, "Intro-Text");

        TextMeshProUGUI txt = panel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (txt == null) { Debug.LogError("[V46] Kein Text im Intro_Panel."); return; }

        txt.text = INTRO_TEXT;
        txt.fontSize = 28f;
        txt.lineSpacing = 2f;
        txt.paragraphSpacing = 2f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = true;

        RectTransform tr = txt.rectTransform;
        tr.sizeDelta = new Vector2(1020f, 880f);
        tr.anchoredPosition = Vector2.zero;

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V46] Start-Overlay neu getextet. Szene speichern mit Ctrl+S.");
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
