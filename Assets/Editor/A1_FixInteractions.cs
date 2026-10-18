// A1_FixInteractions.cs
// Repariert die Interaktionskette in A1_Prototype.
// Menue:  Tools > AED Prototype > Fix Interactions
//
// Problem, das hier behoben wird:
// In Unity feuert OnTriggerEnter nur, wenn mindestens einer der beiden
// beteiligten Collider einen Rigidbody hat. Die EZPZ Area Trigger Prefab hat
// keinen, und "Holdable Basic - Carry" hat auch keinen. Zwei statische
// Collider loesen also nichts aus - deshalb passierte nach dem Aufnehmen des
// AED nichts mehr. Die EZPZ-Beispielszenen benutzen fuer genau diesen Fall
// "Holdable Gravity", das einen Rigidbody mitbringt.
//
// Ausserdem bekommen die beiden Buttons ihre eigenen Aktionen:
//   Button_AED_On   -> schaltet das AED-Display ein
//   Button_Shock    -> schaltet die Schock-Anzeige ein
//
// Idempotent, laeuft ueber Undo.

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;

public static class A1_FixInteractions
{
    const string MENU = "Tools/AED Prototype/Fix Interactions";

    [MenuItem(MENU)]
    public static void Fix()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fix A1 Interactions");

        // ------------------------------------------------- 1 Rigidbodies
        // Ohne diese feuert keine einzige Zone.
        AddBody("AED_Carry", freezeRotation: true);
        AddBody("Pad_1", freezeRotation: true);
        AddBody("Pad_2", freezeRotation: true);
        AddBody("Obstacle_Chair", freezeRotation: true);

        // ------------------------------------------------- 2 Button-Aktionen
        var display = Find("AED_Display");
        var btnOn = Find("Button_AED_On");
        if (btnOn != null && display != null)
            WireButton(btnOn, display);
        else
            Debug.LogWarning("[A1] Button_AED_On oder AED_Display fehlt.");

        var btnShock = Find("Button_Shock");
        if (btnShock != null)
        {
            var lamp = Find("Lamp_Shock_Done");
            if (lamp == null)
            {
                lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lamp.name = "Lamp_Shock_Done";
                Undo.RegisterCreatedObjectUndo(lamp, "Lamp_Shock_Done");
                var root = Find("====A1 CHANGES====");
                if (root != null) lamp.transform.SetParent(root.transform, true);

                var mat = FindMaterial("Mat_A1_Good");
                var r = lamp.GetComponent<Renderer>();
                if (mat != null && r != null) r.sharedMaterial = mat;
            }
            Undo.RecordObject(lamp.transform, "Lamp_Shock_Done pos");
            lamp.transform.position = new Vector3(0f, 0.45f, 2.55f);
            lamp.transform.localScale = new Vector3(0.12f, 0.12f, 0.04f);

            var col = lamp.GetComponent<Collider>();
            if (col != null) Undo.DestroyObjectImmediate(col);

            if (lamp.activeSelf)
            {
                Undo.RecordObject(lamp, "Lamp aus");
                lamp.SetActive(false);
            }

            WireButton(btnShock, lamp);
        }
        else
        {
            Debug.LogWarning("[A1] Button_Shock nicht gefunden.");
        }

        Undo.CollapseUndoOperations(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[A1] Interaktionen repariert: Rigidbodies gesetzt, Buttons verdrahtet.");
    }

    // ----------------------------------------------------------- Hilfsmittel

    // Die Button-Prefab traegt mehrere InteractableGeneral-Komponenten
    // (Wurzel und InteractionBox). Welche den Klick annimmt, haengt vom
    // Aufbau ab, also werden alle verdrahtet und protokolliert.
    static void WireButton(GameObject button, GameObject target)
    {
        var all = button.GetComponentsInChildren<InteractableGeneral>(true);
        Debug.Log("[A1] " + button.name + ": " + all.Length +
                  " InteractableGeneral gefunden.");

        if (all.Length == 0)
        {
            Debug.LogWarning("[A1] " + button.name +
                             " hat keine InteractableGeneral-Komponente.");
            return;
        }

        foreach (var ig in all)
        {
            Undo.RecordObject(ig, button.name + " event");
            WireSetActive(ig.onPrimaryInteract, target, true);
            EditorUtility.SetDirty(ig);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ig);
            Debug.Log("[A1]   -> " + ig.gameObject.name + " verdrahtet auf " +
                      target.name + " (" +
                      ig.onPrimaryInteract.GetPersistentEventCount() +
                      " Listener).");
        }
    }

    static void AddBody(string name, bool freezeRotation)
    {
        var go = Find(name);
        if (go == null)
        {
            Debug.LogWarning("[A1] " + name + " nicht gefunden.");
            return;
        }

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(go);

        Undo.RecordObject(rb, name + " rigidbody");
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 1f;
        rb.constraints = freezeRotation
            ? RigidbodyConstraints.FreezeRotation
            : RigidbodyConstraints.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Holdable kennt eine eigene Rotationssperre.
        var h = go.GetComponent<Holdable>();
        if (h != null)
        {
            Undo.RecordObject(h, name + " holdable");
            h.freezeRotation = freezeRotation;
        }
    }

    static GameObject Find(string n)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var r in scene.GetRootGameObjects())
        {
            var hit = FindRecursive(r.transform, n);
            if (hit != null) return hit;
        }
        return null;
    }

    static GameObject FindRecursive(Transform t, string n)
    {
        if (t.name == n) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindRecursive(t.GetChild(i), n);
            if (hit != null) return hit;
        }
        return null;
    }

    static void WireSetActive(UnityEvent evt, GameObject go, bool state)
    {
        if (evt == null || go == null) return;

        for (int i = 0; i < evt.GetPersistentEventCount(); i++)
        {
            if (evt.GetPersistentTarget(i) == go &&
                evt.GetPersistentMethodName(i) == "SetActive")
                return;
        }
        UnityEventTools.AddBoolPersistentListener(
            evt, new UnityAction<bool>(go.SetActive), state);
    }

    static Material FindMaterial(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Material"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null && m.name == name) return m;
        }
        return null;
    }
}
