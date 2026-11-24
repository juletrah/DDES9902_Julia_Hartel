// A1_BuildV12.cs
// Menue:  Tools > AED Prototype > Build V12
//
// Entfernt tote Doppel-Komponenten vom Typ A1SnapLock.
//
// Drei Objekte tragen die Komponente zweimal: 's', 'Snap_AED' und
// 'Snap_Pad_B'. Jeweils eine der beiden ist unkonfiguriert - kein Anker,
// kein reject, kein countsAsPad. Genau diese richtet Schaden an: ihr
// Snap() nimmt dem Pad das Holdable und schaltet die Physik ab, bewegt es
// aber nirgendwohin. Das Pad bleibt dort stehen, wo es losgelassen wurde,
// also in der Luft. Die richtig konfigurierte Komponente findet danach
// kein Holdable mehr und steigt sofort aus.
//
// Entfernt wird nur dort, wo auf demselben Objekt noch eine funktionierende
// A1SnapLock uebrig bleibt. Die Reject_*-Objekte haben nur eine Komponente
// und werden deshalb nicht angefasst.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class A1_BuildV12
{
    const string MENU = "Tools/AED Prototype/Build V12";

    [MenuItem(MENU)]
    public static void Build()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build V12");

        // Komponenten nach Objekt gruppieren.
        var byObject = new Dictionary<GameObject, List<A1SnapLock>>();

        foreach (A1SnapLock l in Resources.FindObjectsOfTypeAll<A1SnapLock>())
        {
            if (l == null) continue;
            if (!l.gameObject.scene.IsValid() || !l.gameObject.scene.isLoaded) continue;

            if (!byObject.ContainsKey(l.gameObject))
                byObject[l.gameObject] = new List<A1SnapLock>();
            byObject[l.gameObject].Add(l);
        }

        int removed = 0;

        foreach (var pair in byObject)
        {
            List<A1SnapLock> list = pair.Value;
            if (list.Count < 2) continue;

            var alive = new List<A1SnapLock>();
            var dead = new List<A1SnapLock>();

            foreach (A1SnapLock l in list)
            {
                bool configured = l.anchor != null || l.reject || l.countsAsPad;
                if (configured) alive.Add(l); else dead.Add(l);
            }

            if (alive.Count == 0)
            {
                Debug.LogWarning("[V12] '" + pair.Key.name + "' hat " + list.Count +
                                 " A1SnapLock, aber keine davon ist konfiguriert - " +
                                 "nichts entfernt, bitte von Hand ansehen.");
                continue;
            }

            foreach (A1SnapLock l in dead)
            {
                Debug.Log("[V12] '" + pair.Key.name + "': unkonfigurierte A1SnapLock " +
                          "entfernt (" + alive.Count + " funktionierende bleibt/bleiben).");
                Undo.DestroyObjectImmediate(l);
                removed++;
            }
        }

        // Zum Nachsehen: was danach noch da ist.
        foreach (A1SnapLock l in Resources.FindObjectsOfTypeAll<A1SnapLock>())
        {
            if (l == null) continue;
            if (!l.gameObject.scene.IsValid() || !l.gameObject.scene.isLoaded) continue;

            if (l.anchor == null && !l.reject && !l.countsAsPad)
                Debug.LogWarning("[V12] '" + l.name + "' traegt weiterhin eine " +
                                 "A1SnapLock ohne Anker, ohne reject und ohne " +
                                 "countsAsPad. Ein Pad, das hier abgelegt wird, " +
                                 "bleibt in der Luft stehen.");
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[V12] Fertig. " + removed + " tote Komponente(n) entfernt. " +
                  "Szene speichern mit Ctrl+S.");
    }
}
