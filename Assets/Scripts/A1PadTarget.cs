// A1PadTarget.cs
// Eine anklickbare Stelle auf der Brust des Patienten.
//
// Warum: Ein getragenes Pad in ein kleines Trigger-Volumen zu manoevrieren
// ist mit Maus und Tastatur kaum zu treffen. Stattdessen haelt der Spieler
// ein Pad und KLICKT die Stelle an, an die es soll. Das Pad springt dorthin.
// Richtige Stellen rasten ein, falsche geben Rueckmeldung und das Pad bleibt
// in der Hand.

using UnityEngine;

public class A1PadTarget : MonoBehaviour
{
    [Header("Links")]
    public Transform anchor;
    public AEDDisplay display;
    public A1Sequence sequence;

    [Header("Settings")]
    public bool correct = false;
    public string message = "";
    public GameObject markerWhenPlaced;

    [Header("State")]
    public bool used = false;

    public void Place()
    {
        if (used) return;

        Holdable held = FindHeldPad();

        if (held == null)
        {
            if (display != null) display.Show("PICK UP A PAD FIRST");
            return;
        }

        if (!correct)
        {
            // Falsche Stelle: Pad bleibt in der Hand, nur Rueckmeldung.
            if (display != null && !string.IsNullOrEmpty(message))
                display.Show(message);
            return;
        }

        GameObject pad = held.gameObject;
        held.ForceDrop();

        if (anchor != null)
        {
            pad.transform.position = anchor.position;
            pad.transform.rotation = anchor.rotation;
        }

        Rigidbody rb = pad.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        foreach (Collider c in pad.GetComponentsInChildren<Collider>())
            c.isTrigger = false;

        Destroy(held);

        used = true;

        if (markerWhenPlaced != null)
            markerWhenPlaced.SetActive(false);

        if (display != null && !string.IsNullOrEmpty(message))
            display.Show(message);

        if (sequence != null)
            sequence.PadAttached();
    }

    static Holdable FindHeldPad()
    {
        foreach (Holdable h in Object.FindObjectsByType<Holdable>(FindObjectsSortMode.None))
        {
            if (h == null) continue;
            TriggerFilter f = h.GetComponent<TriggerFilter>();
            if (f == null || f.filterString != "PAD") continue;
            if (h.myRayManipulator != null) return h;
        }
        return null;
    }
}
