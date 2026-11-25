// A1_BuildV22.cs
// Menue:  Tools > AED Prototype > Build V22
//
//   1 Holzton heller: von dunklem Nussbraun auf helles Birkenbeige.
//     Betrifft Kuechenzeile, Haengeschrank, Sideboard und die Bilderrahmen,
//     weil die sich das Material 'Mat_Wood' teilen.
//   2 Die beiden Bilder bekommen echte Motive: abstrakte Blumen, eins warm
//     und quer, eins kuehl und hoch. Die Seitenverhaeltnisse passen genau
//     zu den Rahmen, damit nichts gestaucht wird.
//   3 Die Fensterscheiben zeigen die Aussicht jetzt stark verschwommen,
//     wie durch Milchglas. Das Bild selbst wurde ausgetauscht, das Material
//     bleibt dasselbe.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV22
{
    const string MENU = "Tools/AED Prototype/Build V22";
    const string DIR  = "Assets/__My Project/";

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V22");

        AssetDatabase.Refresh();

        // ---------------------------------------------- 1 Holz aufhellen
        // Birke: warmes, helles Beige statt des bisherigen Nussbrauns.
        Material wood = Load("Mat_Wood");
        if (wood != null)
        {
            Color birch = new Color(0.82f, 0.73f, 0.57f);
            SetColor(wood, birch);
            Debug.Log("[V22] 'Mat_Wood' aufgehellt auf Birkenbeige " +
                      "(0,82 | 0,73 | 0,57). Betrifft Kueche, Sideboard und Bilderrahmen.");
        }
        else Debug.LogWarning("[V22] 'Mat_Wood' nicht gefunden.");

        // ---------------------------------------------- 2 Bildmotive
        Apply("Mat_Art_Ochre", "Art_Flowers_A", "warme abstrakte Blumen, quer");
        Apply("Mat_Art_Teal",  "Art_Flowers_B", "kuehle abstrakte Blumen, hoch");

        // ---------------------------------------------- 3 Milchglas
        Texture2D frosted = AssetDatabase.LoadAssetAtPath<Texture2D>(DIR + "Window_View.png");
        Material glass = Load("Mat_WindowView");
        if (glass != null && frosted != null)
        {
            SetColor(glass, Color.white);
            SetTex(glass, frosted);
            Debug.Log("[V22] 'Mat_WindowView' zeigt jetzt die stark weichgezeichnete " +
                      "Fassung - Milchglaswirkung, Gruen und Himmel bleiben erkennbar.");
        }
        else Debug.LogWarning("[V22] Material oder Bild fuer die Fenster nicht gefunden.");

        AssetDatabase.SaveAssets();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V22] Fertig. Szene speichern mit Ctrl+S.");
    }

    static void Apply(string matName, string texName, string what)
    {
        Material m = Load(matName);
        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(DIR + texName + ".png");

        if (m == null) { Debug.LogWarning("[V22] '" + matName + "' nicht gefunden."); return; }
        if (t == null) { Debug.LogWarning("[V22] '" + texName + ".png' nicht gefunden."); return; }

        // Weiss als Grundfarbe, sonst faerbt das Material das Motiv ein.
        SetColor(m, Color.white);
        SetTex(m, t);
        Debug.Log("[V22] '" + matName + "' zeigt jetzt '" + texName + "': " + what + ".");
    }

    static Material Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(DIR + name + ".mat");
    }

    static void SetColor(Material m, Color c)
    {
        Undo.RecordObject(m, "color");
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        EditorUtility.SetDirty(m);
    }

    static void SetTex(Material m, Texture2D t)
    {
        Undo.RecordObject(m, "texture");
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        m.mainTexture = t;
        EditorUtility.SetDirty(m);
    }
}
