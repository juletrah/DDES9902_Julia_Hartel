// A1Reveal.cs
// Blendet Teile der Szene erst ein, wenn der AED neben dem Patienten
// abgelegt wurde.
//
// Warum: Ein Geraetedisplay, das von Anfang an in der Luft steht, hat keine
// Entsprechung in der Wirklichkeit und nimmt dem Ablegen des AED seine
// Bedeutung. Display und Pads erscheinen deshalb erst in dem Moment, in dem
// das Geraet dort liegt, wo es hingehoert - und genau dann sagt das Display,
// was als Naechstes zu tun ist.

using UnityEngine;

public class A1Reveal : MonoBehaviour
{
    [Header("Ausloeser")]
    [Tooltip("Objekt mit dem A1SnapLock des AED (Snap_AED).")]
    public GameObject trigger;

    [Header("Was erscheinen soll")]
    public GameObject[] revealObjects;

    [Header("Meldung beim Erscheinen")]
    public AEDDisplay display;
    [TextArea]
    public string messageOnReveal =
        "PLACE THE PADS ON THE RIGHT SPOTS ON THE BODY.\nTRY IT AND GET FEEDBACK.";

    [Header("Status")]
    public bool revealed = false;

    void Start()
    {
        revealed = false;
        SetAll(false);
    }

    void Update()
    {
        if (revealed || trigger == null) return;

        foreach (A1SnapLock l in trigger.GetComponents<A1SnapLock>())
        {
            if (l != null && l.done)
            {
                Reveal();
                return;
            }
        }
    }

    // Auch direkt aus einem UnityEvent aufrufbar.
    public void Reveal()
    {
        if (revealed) return;
        revealed = true;

        SetAll(true);

        if (display != null && !string.IsNullOrEmpty(messageOnReveal))
            display.Show(messageOnReveal);
    }

    void SetAll(bool on)
    {
        if (revealObjects == null) return;

        foreach (GameObject g in revealObjects)
            if (g != null) g.SetActive(on);
    }
}
