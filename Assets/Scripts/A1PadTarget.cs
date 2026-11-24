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

    [Header("Auflegen")]
    [Tooltip("Pad nach dem Platzieren auf die Koerperoberflaeche absenken, " +
             "statt es an der Ankerhoehe schweben zu lassen.")]
    public bool settleOnSurface = true;
    [Tooltip("Die Flaeche, auf die abgesenkt wird - normalerweise Patient_Body. " +
             "Leer heisst: erste Flaeche unter dem Anker, was wegen der " +
             "unsichtbaren Klickvolumen im Raum unzuverlaessig ist.")]
    public Transform surface;
    [Tooltip("Wenn gesetzt, legt sich das Pad zusaetzlich flach auf die " +
             "getroffene Flaeche. Aus, wenn der Anker die Neigung schon vorgibt.")]
    public bool alignToSurface = false;
    [Tooltip("Wie weit ueber dem Anker der Suchstrahl beginnt.")]
    public float rayStartHeight = 0.35f;
    [Tooltip("Wie weit der Suchstrahl insgesamt reicht.")]
    public float rayLength = 1.2f;
    [Tooltip("Luftspalt zwischen Pad und Oberflaeche, gegen Z-Fighting.")]
    public float surfaceSkin = 0.002f;

    [Header("State")]
    public bool used = false;

    public void Place()
    {
        if (used) return;

        // So nachsichtig wie moeglich: bevorzugt das Pad in der Hand, sonst
        // einfach das naechste noch nicht platzierte. Ein Klick genuegt.
        Holdable held = FindHeldPad();
        if (held == null) held = FindFreePad();

        if (held == null)
        {
            if (display != null) display.Show("NO PAD LEFT");
            return;
        }

        if (!correct)
        {
            // Falsche Stelle: Pad bleibt in der Hand, nur Rueckmeldung.
            A1RunMetrics.WrongPad();
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

            if (settleOnSurface) SettleOnSurface(pad.transform);
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

    // Senkt das Pad von der Ankerhoehe auf die Koerperoberflaeche ab.
    // Die eigentliche Arbeit macht A1Surface, damit der Klickweg und der
    // Ablegeweg (A1SnapLock) sich gleich verhalten.
    void SettleOnSurface(Transform pad)
    {
        A1Surface.Settle(pad, anchor, surface, transform,
                         rayStartHeight, rayLength, surfaceSkin, alignToSurface);
    }

    // Ein Pad, das noch nicht angebracht ist: platzierte Pads verlieren ihr
    // Holdable, tauchen hier also nicht mehr auf.
    static Holdable FindFreePad()
    {
        foreach (Holdable h in Object.FindObjectsByType<Holdable>(FindObjectsSortMode.None))
        {
            if (h == null || !h.gameObject.activeInHierarchy) continue;
            TriggerFilter f = h.GetComponent<TriggerFilter>();
            if (f != null && f.filterString == "PAD") return h;
        }
        return null;
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
