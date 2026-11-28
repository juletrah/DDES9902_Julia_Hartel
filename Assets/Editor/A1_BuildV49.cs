// A1_BuildV49.cs
// Menue:  Assets > AED Build V49   (laeuft einmalig auch von selbst)
//
// Die <color>-Tags blieben wirkungslos, obwohl <size>, <b> und <i>
// verarbeitet wurden. Statt weiter an TMP-Schaltern zu drehen, wird die
// Farbe aus dem Text herausgenommen: drei getrennte Textfelder mit je
// eigener Grundfarbe. Das haengt an keinem Tag und kann nicht
// stillschweigend uebersteuert werden.
//
//   Titel   bernstein, wie das SHOW-HINT-Schild
//   Rumpf   gebrochenes Weiss
//   Start   ruhiges Gruen
//
// Die Abschnittsueberschriften bleiben weiss, dafuer fett - ohne Tags
// gibt es dort keine zweite Farbe, und drei Felder reichen.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV49
{
    const string MENU = "Assets/AED Build V49";

    static readonly Color AMBER = new Color(0.949f, 0.706f, 0.255f);
    static readonly Color BODY  = new Color(0.922f, 0.941f, 0.961f);
    static readonly Color MINT  = new Color(0.561f, 0.839f, 0.659f);
    static readonly Color MUTED = new Color(0.549f, 0.639f, 0.690f);

    const string BODY_TEXT =
        "<size=120%><b>Have you ever used a defibrillator?</b></size>\n\n" +
        "<align=left>" +
        "<b>The situation</b>\n" +
        "You are in an office. Someone has collapsed, and there is a " +
        "defibrillator (AED) somewhere in the room. Your task is to use it.\n\n" +
        "<b>What to do</b>\n" +
        "<b>1.</b>   Make an emergency call\n" +
        "<b>2.</b>   Find the AED and bring it to the person\n" +
        "<b>3.</b>   Follow the instructions the AED gives you\n" +
        "</align>\n" +
        "<i>You don't have to remember all of this. The steps are also on the " +
        "board in the room, and the AED guides you through them once you " +
        "reach it.</i>\n\n" +
        "A clock runs in the background, but this is practice, not a test.\n" +
        "Take your time, look around, figure out what to do.";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V49_done";
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
        if (panel == null) { Debug.LogError("[V49] Intro_Panel fehlt."); return; }

        Transform cardT = panel.transform.Find("Card");
        if (cardT == null) { Debug.LogError("[V49] Card fehlt - erst V47 laufen lassen."); return; }

        Undo.RegisterFullObjectHierarchyUndo(panel, "Intro-Text dreiteilig");

        // --- Rumpf: das vorhandene Feld weiterverwenden ----------------
        TextMeshProUGUI body = panel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (body == null) { Debug.LogError("[V49] Kein Textfeld gefunden."); return; }

        body.gameObject.name = "Text_Body";
        body.transform.SetParent(cardT, false);
        body.text = BODY_TEXT;
        body.color = BODY;
        body.fontSize = 29f;
        body.lineSpacing = 6f;
        body.alignment = TextAlignmentOptions.Top;
        body.richText = true;
        body.overrideColorTags = false;
        body.enableVertexGradient = false;

        RectTransform br = body.rectTransform;
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.pivot = new Vector2(0.5f, 0.5f);
        br.offsetMin = new Vector2(70f, 230f);    // links, unten
        br.offsetMax = new Vector2(-70f, -150f);  // rechts, oben
        br.localScale = Vector3.one;

        // --- Titel ----------------------------------------------------
        TextMeshProUGUI title = MakeText(cardT, "Text_Title", "AED PRACTICE", AMBER, 52f);
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Top;
        RectTransform tr = title.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -55f);
        tr.sizeDelta = new Vector2(-140f, 70f);

        // --- Startzeile -----------------------------------------------
        TextMeshProUGUI start = MakeText(cardT, "Text_Start", "Press SPACE to start", MINT, 40f);
        start.fontStyle = FontStyles.Bold;
        start.alignment = TextAlignmentOptions.Top;
        RectTransform sr = start.rectTransform;
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(1f, 0f);
        sr.pivot = new Vector2(0.5f, 0f);
        sr.anchoredPosition = new Vector2(0f, 110f);
        sr.sizeDelta = new Vector2(-140f, 55f);

        // --- Fusszeile: Steuerung und Uhrhinweis ----------------------
        TextMeshProUGUI foot = MakeText(cardT, "Text_Foot",
            "Move with W A S D or the arrow keys  ·  look with the mouse  ·  " +
            "left mouse button to pick things up and press things\n" +
            "The clock starts when you press space.", MUTED, 22f);
        foot.alignment = TextAlignmentOptions.Top;
        RectTransform fr = foot.rectTransform;
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(1f, 0f);
        fr.pivot = new Vector2(0.5f, 0f);
        fr.anchoredPosition = new Vector2(0f, 40f);
        fr.sizeDelta = new Vector2(-140f, 65f);

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V49] Panel auf vier Textfelder mit eigenen Farben umgestellt. " +
                  "Szene speichern mit Ctrl+S.");
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string content,
                                    Color c, float size)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject
                                         : new GameObject(name, typeof(RectTransform));
        if (existing == null) go.transform.SetParent(parent, false);

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (t == null) t = go.AddComponent<TextMeshProUGUI>();

        t.text = content;
        t.color = c;
        t.fontSize = size;
        t.richText = true;
        t.overrideColorTags = false;
        t.enableVertexGradient = false;
        t.raycastTarget = false;
        t.rectTransform.localScale = Vector3.one;
        return t;
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
