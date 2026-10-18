// A1Logic.cs
// Runtime logic for the AED prototype.
//
// A1SnapLock  - when an object enters a target zone it is released from the
//               player's hand, snapped onto an anchor and locked there.
//               This is what makes "place the AED" and "attach the pad" feel
//               like real placement instead of dropping loose physics boxes.
//
// A1Sequence  - keeps track of how many pads are attached and only arms the
//               shock button once both pads are on AND the player has stepped
//               out of the safety zone.

using UnityEngine;
using UnityEngine.Events;

public class A1SnapLock : MonoBehaviour
{
    [Header("Links")]
    public InteractableTrigger source;
    public Transform anchor;
    public AEDDisplay display;
    public A1Sequence sequence;

    [Header("Settings")]
    public string message = "";
    public bool countsAsPad = false;
    public UnityEvent onSnapped;

    [Header("State")]
    public bool done = false;

    // Called from the zone's onTriggerEnter.
    public void Snap()
    {
        if (done) return;
        if (source == null) return;

        GameObject subject = source.subject;
        if (subject == null) return;

        // The Holdable may sit on a parent (the AED carrier).
        Holdable hold = subject.GetComponentInParent<Holdable>();
        GameObject target = hold != null ? hold.gameObject : subject;

        // Get it out of the player's hand before moving it.
        if (hold != null)
            hold.ForceDrop();

        if (anchor != null)
        {
            target.transform.position = anchor.position;
            target.transform.rotation = anchor.rotation;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Colliders back to solid - ForceDrop may have left them as triggers.
        foreach (Collider c in target.GetComponentsInChildren<Collider>())
        {
            InteractableTrigger it = c.GetComponent<InteractableTrigger>();
            if (it == null) c.isTrigger = false;
        }

        // Remove the Holdable so the object cannot be picked up again.
        if (hold != null)
            Destroy(hold);

        done = true;

        if (display != null && !string.IsNullOrEmpty(message))
            display.Show(message);

        if (sequence != null && countsAsPad)
            sequence.PadAttached();

        onSnapped.Invoke();
    }
}

public class A1Sequence : MonoBehaviour
{
    [Header("Links")]
    public AEDDisplay display;
    public GameObject shockButton;

    [Header("Settings")]
    public int padsNeeded = 2;

    [Header("State")]
    public int padsAttached = 0;
    public bool padsReady = false;
    public bool playerClear = false;

    public void PadAttached()
    {
        padsAttached++;

        if (padsAttached >= padsNeeded)
            padsReady = true;

        Refresh();
    }

    // Called from the safety zone.
    public void PlayerClear()
    {
        playerClear = true;
        Refresh();
    }

    public void PlayerBack()
    {
        playerClear = false;
        Refresh();
    }

    void Refresh()
    {
        bool armed = padsReady && playerClear;

        if (shockButton != null)
            shockButton.SetActive(armed);

        if (display == null) return;

        if (armed)
            display.Show("CLEAR - PRESS SHOCK");
        else if (padsReady)
            display.Show("STAND CLEAR OF THE PATIENT");
    }
}
