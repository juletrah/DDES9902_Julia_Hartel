// A1_BuildV40.cs
// Menue:  Assets > AED Build V40   (laeuft einmalig auch von selbst)
//
// Nachbesserung zu V39: Im Game-View schien der Raum durch das
// Start-Overlay durch. Dieses Skript prueft den Hintergrund des Panels,
// legt ihn notfalls neu an, macht ihn deckend und sortiert die Kinder
// so, dass der Text ueber dem Hintergrund liegt. Es protokolliert
// ausserdem, was es vorgefunden hat.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class A1_BuildV40
{
    const string MENU = "Assets/AED Build V40";

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V40_done";
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
        if (panel == null) { Debug.LogError("[V40] Intro_Panel fehlt - erst V39 laufen lassen."); return; }

        Undo.RegisterFullObjectHierarchyUndo(panel, "Intro_Panel reparieren");

        // Was ist da?
        Transform bgT = panel.transform.Find("Background");
        Debug.Log("[V40] Background-Kind gefunden: " + (bgT != null));

        GameObject bg;
        if (bgT == null)
        {
            bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(panel.transform, false);
        }
        else bg = bgT.gameObject;

        Image img = bg.GetComponent<Image>();
        Debug.Log("[V40] Image-Komponente vorhanden: " + (img != null));
        if (img == null) img = bg.AddComponent<Image>();

        img.color = new Color(0.04f, 0.05f, 0.07f, 1.00f);
        img.raycastTarget = false;
        img.enabled = true;

        RectTransform br = bg.GetComponent<RectTransform>();
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.pivot     = new Vector2(0.5f, 0.5f);
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;
        br.localScale = Vector3.one;
        Debug.Log("[V40] Background-Rect jetzt " + br.rect.size.ToString("0"));

        // Der Text haengt bisher unter dem Hintergrund. Als Geschwister
        // nach dem Hintergrund ist die Zeichenreihenfolge eindeutig.
        TextMeshProUGUI txt = panel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (txt != null)
        {
            txt.transform.SetParent(panel.transform, false);
            txt.transform.SetAsLastSibling();
            RectTransform tr = txt.rectTransform;
            tr.anchorMin = new Vector2(0.5f, 0.5f);
            tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot     = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = Vector2.zero;
            tr.sizeDelta = new Vector2(1200f, 760f);
            tr.localScale = Vector3.one;
            Debug.Log("[V40] Text neu einsortiert, Index " + txt.transform.GetSiblingIndex() + ".");
        }
        else Debug.LogWarning("[V40] Kein TextMeshProUGUI im Panel gefunden.");

        bg.transform.SetAsFirstSibling();

        Canvas c = panel.GetComponent<Canvas>();
        if (c != null)
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 500;
            c.enabled = true;
        }
        if (panel.GetComponent<GraphicRaycaster>() == null)
            panel.AddComponent<GraphicRaycaster>();

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V40] Fertig. Overlay deckend. Szene speichern mit Ctrl+S.");
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
