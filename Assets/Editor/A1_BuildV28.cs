// A1_BuildV28.cs
// Menue:  Assets > AED Build V28   (laeuft einmalig auch von selbst)
//
// Die Pads lagen bisher bei x 0,85 / y 0,29 - also genau ueber dem
// abgelegten AED. Dessen Collider sind nach dem Ablegen abgeschaltet,
// damit er nicht wieder aufgenommen werden kann. Folge: die Pads fallen
// beim Erscheinen durch das Geraet hindurch und stecken darin. Man sieht
// sie nicht und trifft sie kaum.
//
// Sie liegen jetzt auf dem Boden davor, zwischen Spieler und AED:
// freistehend, nichts dazwischen, auf den ersten Blick sichtbar.
//
//   AED_Body belegt x 0,64..1,06 und z 3,18..3,52.
//   Die Pads kommen auf z 2,95, also klar davor.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV28
{
    const string MENU = "Assets/AED Build V28";

    static readonly Vector3 PAD_A_AT = new Vector3(0.70f, 0.03f, 2.95f);
    static readonly Vector3 PAD_B_AT = new Vector3(1.00f, 0.03f, 2.95f);

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V28_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 2)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V28");

        Move("Pad_1", PAD_A_AT);          // Sicherheitshalber beide
        Move("Pad_A", PAD_A_AT);          // Namensvarianten abdecken
        Move("Pad_2", PAD_B_AT);
        Move("Pad_B", PAD_B_AT);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V28] Fertig. Pads liegen jetzt vor dem AED auf dem Boden. " +
                  "Szene speichern mit Ctrl+S.");
    }

    static void Move(string name, Vector3 at)
    {
        GameObject g = Find(name);
        if (g == null) return;

        Undo.RecordObject(g.transform, "Pad");
        g.transform.position = at;
        g.transform.rotation = Quaternion.identity;
        Debug.Log("[V28] '" + name + "' liegt jetzt bei " + at.ToString("0.00") + ".");
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
