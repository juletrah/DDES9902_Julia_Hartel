// A1_BuildV47.cs
// Menue:  Assets > AED Build V47   (laeuft einmalig auch von selbst)
//
// Die Farben aus V46 kamen nicht an. Dieses Skript protokolliert den
// tatsaechlichen Zustand des Panels und setzt die Farben hart.
//
// Verdaechtig sind zwei Dinge: ein aktiver Vertex-Farbverlauf am
// Textfeld ueberschreibt die <color>-Tags, und der Unterschied zwischen
// Hintergrund und Karte war in V46 so klein, dass er kaum zu sehen war.
// Beides wird hier behandelt.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV47
{
    const string MENU = "Assets/AED Build V47";

    // Deutlicher Unterschied, damit die Karte sichtbar ist.
    static readonly Color BACKDROP = new Color(0.05f, 0.07f, 0.10f, 1.00f);
    static readonly Color CARD     = new Color(0.16f, 0.21f, 0.27f, 1.00f);

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V47_done";
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
        if (panel == null) { Debug.LogError("[V47] Intro_Panel fehlt."); return; }

        Undo.RegisterFullObjectHierarchyUndo(panel, "Intro-Farben");

        // --- Was haengt ueberhaupt unter dem Panel? --------------------
        string kinder = "";
        for (int i = 0; i < panel.transform.childCount; i++)
        {
            Transform c = panel.transform.GetChild(i);
            Image im = c.GetComponent<Image>();
            kinder += "\n   [" + i + "] " + c.name +
                      (im != null ? ("  Image, Farbe " + ColorUtility.ToHtmlStringRGBA(im.color)) : "  (kein Image)");
        }
        Debug.Log("[V47] Kinder von Intro_Panel:" + kinder);

        // --- Hintergrund ----------------------------------------------
        Transform bgT = panel.transform.Find("Background");
        if (bgT != null)
        {
            Image bgImg = bgT.GetComponent<Image>();
            if (bgImg == null) bgImg = bgT.gameObject.AddComponent<Image>();
            bgImg.color = BACKDROP;
            bgImg.enabled = true;
            bgT.SetAsFirstSibling();
            Debug.Log("[V47] Hintergrund gesetzt auf #" + ColorUtility.ToHtmlStringRGBA(BACKDROP));
        }
        else Debug.LogWarning("[V47] Kein Kind namens 'Background'.");

        // --- Karte ----------------------------------------------------
        Transform cardT = panel.transform.Find("Card");
        GameObject card;
        if (cardT == null)
        {
            card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(panel.transform, false);
            Debug.Log("[V47] Karte neu angelegt - V46 hatte keine erzeugt.");
        }
        else { card = cardT.gameObject; Debug.Log("[V47] Karte war vorhanden."); }

        Image cardImg = card.GetComponent<Image>();
        if (cardImg == null) cardImg = card.AddComponent<Image>();
        cardImg.color = CARD;
        cardImg.enabled = true;
        cardImg.raycastTarget = false;

        RectTransform cr = card.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.5f);
        cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot     = new Vector2(0.5f, 0.5f);
        cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = new Vector2(1340f, 980f);
        cr.localScale = Vector3.one;
        card.transform.SetSiblingIndex(1);

        // --- Text -----------------------------------------------------
        TextMeshProUGUI t = panel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (t != null)
        {
            Debug.Log("[V47] Text vorher: richText=" + t.richText +
                      ", Verlauf aktiv=" + t.enableVertexGradient +
                      ", Grundfarbe #" + ColorUtility.ToHtmlStringRGBA(t.color) +
                      ", Material '" + (t.fontSharedMaterial != null ? t.fontSharedMaterial.name : "keins") + "'");

            // Ein aktiver Verlauf faerbt den gesamten Text ein und macht
            // jeden <color>-Tag wirkungslos.
            t.enableVertexGradient = false;
            t.richText = true;
            t.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            t.transform.SetAsLastSibling();

            t.ForceMeshUpdate();
        }
        else Debug.LogError("[V47] Kein TextMeshProUGUI gefunden.");

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V47] Fertig. Szene speichern mit Ctrl+S.");
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
