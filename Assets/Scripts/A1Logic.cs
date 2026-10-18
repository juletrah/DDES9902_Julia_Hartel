// A1Logic.cs
// Runtime logic for the AED prototype.
//
// A1Billboard - keeps a text panel turned towards the player, upright.
//               Fixes mirrored text: a world-space label is only readable
//               from its front side, so it has to follow the camera.
// A1SnapLock  - a target zone that either LOCKS a placed object onto an
//               anchor (correct position) or REJECTS it back to where it
//               came from (wrong position), with feedback either way.
// A1Sequence  - counts the attached pads and only arms the shock button
//               once both pads are on AND the player has stepped clear.

using UnityEngine;
using UnityEngine.Events;

public class A1Billboard : MonoBehaviour
{
    public bool yawOnly = true;
    Transform cam;

    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        // Die Vorderseite eines TMP-Textes ist seine lokale +z-Achse.
        // Sie muss ZUM Spieler zeigen, sonst sieht man die Rueckseite und
        // der Text steht spiegelverkehrt.
        Vector3 dir = cam.position - transform.position;
        if (yawOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}

public class A1SnapLock : MonoBehaviour
{
    [Header("Links")]
    public InteractableTrigger source;
    public Transform anchor;
    public AEDDisplay display;
    public A1Sequence sequence;

    [Header("Behaviour")]
    [Tooltip("True = wrong spot: the pad is pushed back instead of locked.")]
    public bool reject = false;
    public string message = "";
    public bool countsAsPad = false;
    public UnityEvent onSnapped;

    [Header("State")]
    public bool done = false;

    public void Snap()
    {
        if (source == null) return;
        if (done && !reject) return;

        GameObject subject = source.subject;
        if (subject == null) return;

        Holdable hold = subject.GetComponentInParent<Holdable>();
        GameObject target = hold != null ? hold.gameObject : subject;

        // A pad that is already locked must not be moved again.
        if (hold == null && !reject) return;

        if (hold != null) hold.ForceDrop();

        Rigidbody rb = target.GetComponent<Rigidbody>();

        if (reject)
        {
            // Wrong spot: send it back to its starting place and let the
            // player try again. Nothing is locked, nothing is counted.
            if (hold != null)
            {
                target.transform.position = hold.startingPosition;
                target.transform.rotation = hold.startingRotation;
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            if (display != null && !string.IsNullOrEmpty(message))
                display.Show(message);
            return;
        }

        if (anchor != null)
        {
            target.transform.position = anchor.position;
            target.transform.rotation = anchor.rotation;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        foreach (Collider c in target.GetComponentsInChildren<Collider>())
        {
            InteractableTrigger it = c.GetComponent<InteractableTrigger>();
            if (it == null) c.isTrigger = false;
        }

        if (hold != null) Destroy(hold);

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
        if (padsAttached >= padsNeeded) padsReady = true;
        Refresh();
    }

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
