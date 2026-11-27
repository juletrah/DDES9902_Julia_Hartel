// A1_BuildV38.cs
// Menue:  Assets > AED Build V38   (laeuft einmalig auch von selbst)
//
// Das START-Schild stand rechts neben der Tafel und verschwand halb in
// der Tuer. Es wandert an die Tafel heran, auf Hoehe des Satzes "When
// you are ready, press START", und liegt leicht davor - dann steht der
// Knopf dort, wo die Aufforderung steht, statt irgendwo daneben.
//
//   Instructions_Plate: Mitte (-0,43 | 1,53 | 5,86), 4,0 x 2,3 x 0,06
//   -> Vorderflaeche z 5,83, rechte Kante x 1,57, Unterkante y 0,38
//   Das Schild (1,10 x 0,52 x 0,05) kommt auf z 5,80, also knapp davor.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV38
{
    const string MENU = "Assets/AED Build V38";

    static readonly Vector3 SIGN_AT = new Vector3(1.15f, 0.72f, 5.80f);

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        const string KEY = "A1_V38_done";
        if (EditorPrefs.GetBool(KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(KEY, false)) return;
            EditorPrefs.SetBool(KEY, true);
            Build();
        };
    }

    [MenuItem(MENU, false, 12)]
    public static void Build()
    {
        GameObject sign = Find("Start_Sign");
        if (sign == null) { Debug.LogWarning("[V38] Start_Sign nicht gefunden."); return; }

        Undo.RecordObject(sign.transform, "Start_Sign");
        sign.transform.position = SIGN_AT;
        sign.transform.rotation = Quaternion.identity;

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V38] START-Schild jetzt bei " + SIGN_AT.ToString("0.00") +
                  ", direkt an der Tafel. Szene speichern mit Ctrl+S.");
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
