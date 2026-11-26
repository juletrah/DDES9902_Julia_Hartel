// A2Flow.cs
// Steuert den kompletten Ablauf am Patienten. Eine Stelle, ein Zustand,
// eine Anzeige - statt der bisherigen verteilten Logik aus A1Sequence,
// A1SnapLock, A1PadTarget und zwei parallelen Displays.
//
// Ablauf:
//   1 Notruf absetzen        -> die Wanduhr laeuft los (bestehende Logik)
//   2 AED holen und ablegen  -> AED klappt auf: Anzeige und Pads erscheinen
//   3 Pads platzieren        -> richtig: gruen und fest
//                               falsch: rot, bleibt aufnehmbar,
//                                       Anzeige "WRONG LOCATION - RELOCATE"
//   4 Beide richtig          -> "SHOCK ADVISED", dann "STAND CLEAR"
//   5 Spieler geht heraus    -> Schock, Uhr stoppt
//
// Ohne Klick: der Ablauf haengt nur an Ablegen, Loslassen und Abstand.
// Unitys OnMouseDown ist in diesem Projekt unbrauchbar, weil der
// Spielercontroller den Mauszeiger in die Bildmitte sperrt - dann kommt
// kein Klick mehr beim Collider an. Jeder Schritt wird jetzt von einem
// Zustand ausgeloest, den der Spieler ohnehin herstellen muss.
//
// Technisch: EZPZ uebernimmt nur Aufnehmen und Tragen. Ob ein Pad gerade
// gehalten wird, steht in Holdable.myRayManipulator. Dieses Skript merkt
// sich den Zustand und wertet im Moment des Loslassens aus - deshalb
// braucht es keine Trigger-Volumen und keine unsichtbaren Collider, die
// sich gegenseitig den Strahl wegnehmen.

using UnityEngine;
using TMPro;

public class A2Flow : MonoBehaviour
{
    public enum Step { WaitCall, PlaceAED, TurnOn, Pads, ShockAdvised, StandClear, Done }

    [Header("Status")]
    public Step step = Step.WaitCall;

    [Header("AED")]
    public Transform aed;
    public Holdable aedHoldable;
    public Transform aedTarget;
    [Tooltip("Wie nah der AED am Zielpunkt liegen muss, damit er als abgelegt gilt.")]
    public float aedPlaceRadius = 1.00f;

    [Header("Anzeige")]
    public GameObject displayRoot;
    public TMP_Text displayText;

    [Header("Pads")]
    public GameObject padsRoot;
    public Holdable padA, padB;
    public Transform zoneA, zoneB;
    [Tooltip("Toleranz um den richtigen Punkt, waagerecht gemessen.")]
    public float zoneRadius = 0.15f;

    [Header("Materialien")]
    public Material matPad, matPadOk, matPadBad;

    [Header("Schock")]
    public AEDDisplay clock;
    public Transform patient;
    public Transform player;
    public float clearDistance = 2.00f;
    public GameObject clearZone;
    public Material matZoneBad, matZoneOk;

    [Header("Messwerte")]
    public float tCall = -1f, tAedPlaced = -1f, tTurnedOn = -1f;
    public float tPadsDone = -1f, tShock = -1f;
    public int wrongPlacements = 0;

    bool padAok, padBok;
    bool heldA, heldB;
    bool aedWasHeld;
    float t0 = -1f, stepStarted;

    // ------------------------------------------------------------------

    void Start()
    {
        if (displayRoot != null) displayRoot.SetActive(false);
        if (padsRoot != null) padsRoot.SetActive(false);
        if (clearZone != null) clearZone.SetActive(false);
        Paint(padA, matPad);
        Paint(padB, matPad);
        step = Step.WaitCall;
        // In der Szene steht noch der alte Wert 0,70 m. Grosszuegiger ist
        // hier besser: zu eng heisst, der AED liegt scheinbar richtig und
        // es passiert trotzdem nichts.
        if (aedPlaceRadius < 1.00f) aedPlaceRadius = 1.00f;
        Debug.Log("[A2] Ablauf bereit. Warte auf Notruf oder auf das Aufnehmen des AED.");
    }

    float Now() { return t0 < 0f ? 0f : Time.time - t0; }

    void Say(string s)
    {
        if (displayText != null) displayText.text = s;
    }

    void Go(Step s)
    {
        step = s;
        stepStarted = Time.time;
    }

    // A2Click ruft das noch auf. Der Ablauf braucht es nicht mehr, die
    // Methode bleibt nur, damit das alte Skript weiter kompiliert.
    public void OnAedClicked()
    {
        Debug.Log("[A2] Klick auf den AED - wird im Ablauf nicht mehr gebraucht.");
    }

    void Update()
    {
        switch (step)
        {
            case Step.WaitCall:     DoWaitCall();     break;
            case Step.PlaceAED:     DoPlaceAED();     break;
            case Step.TurnOn:       DoTurnOn();       break;
            case Step.Pads:         DoPads();         break;
            case Step.ShockAdvised: DoShockAdvised(); break;
            case Step.StandClear:   DoStandClear();   break;
        }
    }

    // 1 -----------------------------------------------------------------
    void DoWaitCall()
    {
        // Zwei Starts, damit der Ablauf nicht haengen bleibt:
        // regulaer der Notruf (Uhr laeuft), ersatzweise das Aufnehmen des
        // AED. Haengt die Telefonlogik, laeuft der Rest trotzdem weiter.
        bool called = clock != null && clock.clockRunning;
        bool grabbed = aedHoldable != null && aedHoldable.myRayManipulator != null;

        if (!called && !grabbed) return;

        t0 = Time.time;
        tCall = 0f;
        if (called) Debug.Log("[A2] Notruf abgesetzt, Uhr laeuft.");
        else        Debug.LogWarning("[A2] AED aufgenommen, ohne dass die Uhr lief - " +
                                     "Ablauf startet trotzdem. Notruf pruefen.");
        if (!called && clock != null) clock.StartClock();
        Go(Step.PlaceAED);
    }

    // 2 -----------------------------------------------------------------
    void DoPlaceAED()
    {
        if (aed == null || aedTarget == null) return;

        bool held = aedHoldable != null && aedHoldable.myRayManipulator != null;
        if (held) { aedWasHeld = true; return; }

        Vector3 a = aed.position, b = aedTarget.position;
        a.y = b.y = 0f;
        float d = Vector3.Distance(a, b);

        // Nur beim Loslassen melden, sonst waere das Log pro Bild voll.
        if (aedWasHeld)
        {
            aedWasHeld = false;
            Debug.Log("[A2] AED losgelassen, " + d.ToString("0.00") +
                      " m vom Zielpunkt (erlaubt " + aedPlaceRadius.ToString("0.00") + " m).");
        }

        if (d > aedPlaceRadius) return;

        aed.position = aedTarget.position;
        aed.rotation = aedTarget.rotation;

        Rigidbody rb = aed.GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; rb.useGravity = false; }
        // Holdable entfernen, nicht nur abschalten: der EZPZ-Interactor
        // findet eine bloss deaktivierte Komponente weiterhin und wuerde
        // den AED beim Einschaltklick wieder aufnehmen.
        if (aedHoldable != null) Destroy(aedHoldable);

        tAedPlaced = Now();
        Debug.Log("[A2] AED abgelegt nach " + tAedPlaced.ToString("0.0") + " s.");
        Go(Step.TurnOn);
    }

    // 3 -----------------------------------------------------------------
    // Das Aufklappen passiert jetzt direkt beim Ablegen. Eine kurze Pause
    // bleibt, damit man sieht, dass es eine Folge des Ablegens ist.
    void DoTurnOn()
    {
        if (Time.time - stepStarted < 0.4f) return;

        if (displayRoot != null) displayRoot.SetActive(true);
        if (padsRoot != null) padsRoot.SetActive(true);
        Say("PLACE BOTH PADS\nON THE PATIENT");

        tTurnedOn = Now();
        Debug.Log("[A2] AED aufgeklappt nach " + tTurnedOn.ToString("0.0") + " s.");
        Go(Step.Pads);
    }

    // 4 -----------------------------------------------------------------
    void DoPads()
    {
        CheckPad(padA, zoneA, ref padAok, ref heldA, "A");
        CheckPad(padB, zoneB, ref padBok, ref heldB, "B");

        if (padAok && padBok)
        {
            tPadsDone = Now();
            Say("SHOCK ADVISED");
            Debug.Log("[A2] Beide Pads richtig nach " + tPadsDone.ToString("0.0") +
                      " s, " + wrongPlacements + " Fehlversuch(e).");
            Go(Step.ShockAdvised);
        }
    }

    void CheckPad(Holdable pad, Transform zone, ref bool ok, ref bool wasHeld, string label)
    {
        if (pad == null || zone == null || ok) return;

        bool held = pad.myRayManipulator != null;

        // Ausgewertet wird im Moment des Loslassens, nicht beim Aufnehmen.
        if (wasHeld && !held)
        {
            Vector3 p = pad.transform.position, z = zone.position;
            p.y = z.y = 0f;
            float d = Vector3.Distance(p, z);

            if (d <= zoneRadius)
            {
                ok = true;
                pad.transform.position = zone.position;
                pad.transform.rotation = zone.rotation;

                Rigidbody rb = pad.GetComponent<Rigidbody>();
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; rb.useGravity = false; }
                Paint(pad, matPadOk);
                Destroy(pad);   // festgeklebt, nicht mehr aufnehmbar
                Say("PAD " + label + " CORRECT");
                Debug.Log("[A2] Pad " + label + " richtig, Abweichung " + d.ToString("0.00") + " m.");
            }
            else
            {
                wrongPlacements++;
                Paint(pad, matPadBad);
                Say("WRONG LOCATION\nRELOCATE");
                Debug.Log("[A2] Pad " + label + " falsch, " + d.ToString("0.00") +
                          " m daneben (erlaubt " + zoneRadius.ToString("0.00") + " m).");
            }
        }

        wasHeld = held;
    }

    void Paint(Holdable pad, Material m)
    {
        if (pad == null || m == null) return;
        foreach (Renderer r in pad.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = m;
    }

    // 5 -----------------------------------------------------------------
    void DoShockAdvised()
    {
        // "SHOCK ADVISED" kurz stehenlassen, damit es gelesen werden kann,
        // dann von selbst weiter zur Warnung.
        if (Time.time - stepStarted < 2.5f) return;

        if (clearZone != null)
        {
            clearZone.SetActive(true);
            PaintZone(matZoneBad);
        }
        Say("STAND CLEAR\nMOVE AWAY FROM THE PATIENT");
        Debug.Log("[A2] Warnung ausgegeben, Spieler muss " + clearDistance.ToString("0.0") +
                  " m Abstand nehmen.");
        Go(Step.StandClear);
    }

    // 6 + 7 ---------------------------------------------------------------
    void DoStandClear()
    {
        if (player == null || patient == null) return;

        Vector3 a = player.position, b = patient.position;
        a.y = b.y = 0f;
        bool clear = Vector3.Distance(a, b) >= clearDistance;

        if (!clear) return;

        PaintZone(matZoneOk);
        Say("SHOCK DELIVERED");

        tShock = Now();
        if (clock != null) clock.StopClock();

        Debug.Log("[A2] Schock abgegeben nach " + tShock.ToString("0.0") + " s. Uhr gestoppt.");
        Go(Step.Done);
    }

    void PaintZone(Material m)
    {
        if (clearZone == null || m == null) return;
        foreach (Renderer r in clearZone.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = m;
    }
}
