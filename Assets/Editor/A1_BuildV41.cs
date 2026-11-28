// A1_BuildV41.cs
// Menue:  Assets > AED Build V41   (laeuft einmalig auch von selbst)
//
// Das Start-Overlay zeigt jetzt den Inhalt der Wandtafel.
//
// Begruendung: Die Anweisungen haengen ohnehin im Raum, jede Testperson
// muss sie lesen. Bisher lief dabei die Uhr mit, und gemessen wurde zum
// Teil, wie schnell jemand ein Schild an der Wand findet und liest -
// nicht, wie gut der Ablauf verstanden wird. Vorgezogen verraet der Text
// nichts Neues, er nimmt nur die Orientierungszeit aus der Messung.
//
// Die richtigen Pad-Positionen stehen bewusst NICHT hier. Die zeigt
// weiterhin erst die Abbildung am AED, und der Transfer von der
// Abbildung auf den Koerper bleibt das Lernziel.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV41
{
    const string MENU = "Assets/AED Build V41";

    const string INTRO_TEXT =
        "<size=160%><b>AED PRACTICE</b></size>\n" +
        "<size=110%>for emergencies</size>\n\n" +
        "<align=left>" +
        "<b>1.</b>  Make an emergency call\n" +
        "<b>2.</b>  Fetch the AED and place it next to the patient\n" +
        "<b>3.</b>  Follow the AED instructions\n\n" +
        "     →  Apply the pads correctly. Wait for feedback.\n" +
        "     →  Deliver the shock if needed.\n" +
        "</align>\n" +
        "<i>You don't have to remember all of this. The AED will guide you\n" +
        "through the process once you reach it.</i>\n\n" +
        "<size=90%>Move with W A S D or the arrow keys · look with the mouse · " +
        "left mouse button to pick things up and press things</size>\n\n" +
        "<size=130%><b>Press SPACE to start</b></size>\n" +
        "<size=85%>The clock starts then, not before.</size>";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V41_done";
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
        if (panel == null) { Debug.LogError("[V41] Intro_Panel fehlt - erst V39 laufen lassen."); return; }

        Undo.RegisterFullObjectHierarchyUndo(panel, "Intro-Text");

        TextMeshProUGUI txt = panel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (txt == null) { Debug.LogError("[V41] Kein Text im Intro_Panel."); return; }

        txt.text = INTRO_TEXT;
        txt.fontSize = 30f;
        txt.lineSpacing = 4f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = true;

        RectTransform tr = txt.rectTransform;
        tr.sizeDelta = new Vector2(1250f, 820f);
        tr.anchoredPosition = Vector2.zero;

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V41] Intro-Text ersetzt: Wandtafel-Inhalt plus Hinweis, dass der AED fuehrt. " +
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
