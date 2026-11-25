// A1_BuildV19.cs
// Menue:  Tools > AED Prototype > Build V19
//
// Drei Feinkorrekturen an der Bueroeinrichtung. Bewusst als eigenes
// Skript, das nur vorhandene Objekte verschiebt: so bleibt alles
// unangetastet, was du zwischendurch von Hand justiert hast - etwa das
// Sideboard, das jetzt 10 cm weiter westlich steht als von V18 gesetzt.
//
//   1 Doppeltisch Ost weiter nach Norden: der Abstand zum Wandtisch im
//     Sueden war mit knapp 1 m zu eng.
//   2 Doppeltisch Sued weiter nach Osten: er stand direkt vor der
//     Kuechenzeile.
//   3 Die Pflanze vor der Anweisungstafel in die Nordostecke.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV19
{
    const string MENU = "Tools/AED Prototype/Build V19";

    struct Move
    {
        public string name;
        public Vector3 to;
        public string why;
        public Move(string n, Vector3 t, string w) { name = n; to = t; why = w; }
    }

    static readonly Move[] MOVES =
    {
        new Move("Shared_E", new Vector3( 4.60f, 0f, -0.80f),
                 "Abstand zum Wandtisch Sued von 0,98 m auf 1,78 m"),
        new Move("Shared_S", new Vector3(-1.40f, 0f, -4.35f),
                 "1,70 m frei vor der Kuechenzeile statt 0 m"),
        new Move("Plant_N",  new Vector3( 7.30f, 0f,  5.20f),
                 "aus dem Weg vor der Anweisungstafel in die Nordostecke"),
    };

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V19");

        int n = 0;

        foreach (Move m in MOVES)
        {
            GameObject g = Find(m.name);
            if (g == null)
            {
                Debug.LogWarning("[V19] '" + m.name + "' nicht gefunden - uebersprungen.");
                continue;
            }

            Vector3 from = g.transform.position;
            Undo.RecordObject(g.transform, m.name);
            g.transform.position = m.to;
            n++;

            Debug.Log(string.Format(
                "[V19] '{0}': ({1:0.00} | {2:0.00}) -> ({3:0.00} | {4:0.00}). {5}.",
                m.name, from.x, from.z, m.to.x, m.to.z, m.why));
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V19] Fertig. " + n + " Objekt(e) verschoben. Szene speichern mit Ctrl+S.");
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
