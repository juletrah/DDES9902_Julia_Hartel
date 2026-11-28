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
// gehalten wird, steht in Holdable.moving. Dieses Skript merkt
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
    public float aedPlaceRadius = 3.00f;
    [Tooltip("Ab dieser Naehe nimmt der Spieler den AED ohne Klick auf.")]
    public float aedPickupRadius = 2.00f;

    [Header("Anzeige")]
    public GameObject displayRoot;
    public TMP_Text displayText;

    [Header("Pads")]
    [Tooltip("Abbildung der richtigen Pad-Stellen, haengt an der Anzeige.")]
    public GameObject padGuide;
    public GameObject padsRoot;
    public Holdable padA, padB;
    public Transform zoneA, zoneB;
    [Tooltip("Toleranz um den richtigen Punkt, waagerecht gemessen.")]
    public float zoneRadius = 0.15f;

    [Header("Materialien")]
    public Material matPad, matPadOk, matPadBad;

    [Header("Zielflaechen am Patienten")]
    [Tooltip("Sichtbare Markierungen der beiden richtigen Stellen.")]
    public GameObject markerA, markerB;

    [Header("Startknopf")]
    public GameObject startButton;

    // v4 [s1] Die Markierungen auf dem Koerper erscheinen nicht mehr von
    // selbst. Der Transfer von der Abbildung auf den Koerper IST die
    // Aufgabe - wer ihn geschenkt bekommt, uebt ihn nicht. Wer nicht
    // weiterkommt, fordert die Hilfe an; dass jemand sie anfordert, ist
    // selbst eine Messgroesse.
    [Header("Hinweis auf Anfrage (v4)")]
    public GameObject hintButton;
    public bool hintUsed = false;

    // v4 [s2] Erklaert ausschliesslich die Steuerung, nicht die Aufgabe.
    // Sonst wird genau das verschenkt, was gemessen werden soll.
    [Header("Start-Overlay (v4)")]
    public GameObject introPanel;

    [Header("AED greifen per Klick")]
    public GameObject aedGrabButton;

    [Header("Schock-Knopf am AED")]
    public GameObject aedButton;
    public Renderer aedButtonRenderer;
    public Material matButtonOff, matButtonOn;

    [Header("Schock")]
    public AEDDisplay clock;
    public Transform patient;
    public Transform player;
    public float clearDistance = 2.00f;
    public GameObject clearZone;
    public Material matZoneBad, matZoneOk;

    [Header("Wanduhr")]
    [Tooltip("Die Anzeigen der Wanduhr. A2Flow schreibt sie selbst, damit " +
             "die Uhr ab Spielbeginn laeuft und nicht erst ab dem Notruf.")]
    public TMP_Text[] clockTexts;

    [Header("Messwerte")]
    public float tCall = -1f, tAedPlaced = -1f, tTurnedOn = -1f;
    public float tPadsDone = -1f, tShock = -1f;
    public int wrongPlacements = 0;

    bool padAok, padBok;
    bool zoneAused, zoneBused;
    bool heldA, heldB;
    bool aedWasHeld;
    bool aedCarried;
    bool buttonPressed;
    bool shockArmed;   // v4: Knopf erst scharf, wenn der Abstand stimmt
    bool clockFrozen;
    bool grabQueued;
    bool startPressed;
    bool started;
    Vector3 playerStart;
    float t0 = -1f, stepStarted;

    // ------------------------------------------------------------------

    void Start()
    {
        if (displayRoot != null) displayRoot.SetActive(false);
        if (padsRoot != null) padsRoot.SetActive(false);
        if (clearZone != null) clearZone.SetActive(false);
        if (aedButton != null) aedButton.SetActive(false);
        if (padGuide != null) padGuide.SetActive(false);
        if (markerA != null) markerA.SetActive(false);
        if (markerB != null) markerB.SetActive(false);
        if (startButton != null) startButton.SetActive(true);
        if (hintButton != null) hintButton.SetActive(false);
        if (introPanel != null) introPanel.SetActive(true);
        hintUsed = false;
        if (zoneRadius < 0.25f) zoneRadius = 0.25f;
        Paint(padA, matPad);
        Paint(padB, matPad);
        step = Step.WaitCall;
        // Die Uhr laeuft erst ab der ersten Eingabe. Startet sie beim Laden
        // der Szene, ist der Durchlauf schon getaktet, bevor die Testperson
        // bereit ist - genau das wurde an v2 bemaengelt.
        t0 = -1f;
        if (player != null) playerStart = player.position;
        // In der Szene steht noch der alte Wert 0,70 m. Grosszuegiger ist
        // hier besser: zu eng heisst, der AED liegt scheinbar richtig und
        // es passiert trotzdem nichts.
        if (aedPlaceRadius < 3.00f) aedPlaceRadius = 3.00f;
        if (aedPickupRadius < 2.00f) aedPickupRadius = 2.00f;
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

    // Haengt am onPrimaryInteract des Knopfes auf dem AED. Das alte
    // A2Click ruft dieselbe Methode auf, schadet also nicht.
    // Haengt am Klickfeld vor dem AED-Schrank. Notruf und AED laufen damit
    // ueber dieselbe Geste - die Uneinheitlichkeit war ein Befund aus v1.
    // Haengt am Startknopf an der Instruktionstafel.
    public void OnStartClicked()
    {
        startPressed = true;
        Debug.Log("[A2] START gedrueckt.");
    }

    public void OnAedGrabClicked()
    {
        grabQueued = true;
        Debug.Log("[A2] AED angeklickt.");
    }

    public void OnAedClicked()
    {
        buttonPressed = true;
        Debug.Log("[A2] Knopf am AED gedrueckt.");
    }

    void Update()
    {
        CheckStart();
        UpdateClock();

        // Vor dem Start laeuft nichts weiter - kein Schritt, keine Messung.
        if (!started) return;

        // Der Notruf zaehlt genau dann, wenn das Telefon betaetigt wurde -
        // unabhaengig davon, wo der Ablauf gerade steht.
        if (tCall < 0f && clock != null && clock.clockRunning)
        {
            tCall = Now();
            Debug.Log("[A2] Notruf nach " + tCall.ToString("0.0") + " s.");
        }

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

    // Die Wanduhr. AEDDisplay zaehlt erst ab dem Notruf - deshalb schreibt
    // dieses Skript die Anzeige selbst, und AEDDisplay.clockTexts ist
    // geleert, damit sich beide nicht gegenseitig ueberschreiben.
    // clockRunning bleibt als reines Signal "Notruf abgesetzt" erhalten.
    // Der Durchlauf beginnt, wenn die Person START drueckt - nicht beim
    // Laden der Szene und nicht bei der ersten Bewegung. Sich umsehen und
    // die Steuerung begreifen gehoert nicht zur Aufgabe, und gemessene
    // Orientierungszeit ist nur Rauschen in den Daten. Wer stattdessen
    // sofort zum Telefon geht, startet damit ebenfalls.
    void CheckStart()
    {
        if (started) return;

        bool called = clock != null && clock.clockRunning;

        bool space = SpacePressed();

        // v4: Solange das Overlay steht, laeuft nichts. Es erklaert die
        // Steuerung - und gemessene Einarbeitungszeit ist Rauschen, kein
        // Befund ueber die Aufgabe.
        if (introPanel != null && introPanel.activeSelf)
        {
            if (!space && !startPressed && !called) return;
            introPanel.SetActive(false);
        }

        if (!startPressed && !called && !space) return;

        started = true;
        t0 = Time.time;
        if (startButton != null) startButton.SetActive(false);
        Debug.Log("[A2] Durchlauf gestartet, Uhr laeuft.");
    }

    void UpdateClock()
    {
        if (clockFrozen || clockTexts == null) return;

        float t = started ? Now() : 0f;
        string txt = Mathf.FloorToInt(t / 60f).ToString("0") + ":" +
                     Mathf.FloorToInt(t % 60f).ToString("00");

        foreach (TMP_Text c in clockTexts)
            if (c != null) c.text = txt;
    }

    // Das Projekt laeuft auf dem neuen Input System; UnityEngine.Input
    // wirft dort eine Ausnahme. Deshalb ueber Keyboard.current - und
    // abgesichert, falls keine Tastatur angemeldet ist.
    static bool SpacePressed()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
    }

    // v4: Vom Klickfeld "HINT" aufgerufen. Zeigt nur die Stellen, die
    // noch frei sind - Hilfe genau so weit, wie sie gebraucht wird.
    public void ShowHint()
    {
        if (step != Step.Pads) return;
        hintUsed = true;
        if (markerA != null && !zoneAused) markerA.SetActive(true);
        if (markerB != null && !zoneBused) markerB.SetActive(true);
        if (hintButton != null) hintButton.SetActive(false);
        Debug.Log("[A2] Hinweis angefordert nach " + Now().ToString("0.0") + " s.");
    }

    public bool PadsOk { get { return padAok && padBok; } }
    public bool IsDone { get { return step == Step.Done; } }

    // Alle vier Teilaufgaben: Notruf, AED abgelegt, Pads richtig, Schock.
    // Ohne Notruf ist der Durchlauf nicht vollstaendig, auch wenn der
    // Schock ausgeloest wurde.
    public bool AllTasks { get { return IsDone && tCall >= 0f; } }

    // 1 -----------------------------------------------------------------
    void DoWaitCall()
    {
        // Zwei Starts, damit der Ablauf nicht haengen bleibt:
        // regulaer der Notruf, ersatzweise das Aufnehmen des AED. Haengt
        // die Telefonlogik, laeuft der Rest trotzdem weiter.
        //
        // Die Zeit bis zum Notruf wird hier NICHT gesetzt. Sie gehoert
        // allein an den Druck auf das Telefon und wird in Update()
        // erfasst - sonst stuende dort die Zeit bis zum AED-Griff, und
        // der gemessene Wert hiesse etwas anderes als sein Name.
        bool called = clock != null && clock.clockRunning;
        bool grabbed = IsHeld(aedHoldable);

        if (!called && !grabbed) return;

        if (called) Debug.Log("[A2] Notruf abgesetzt.");
        else        Debug.LogWarning("[A2] AED aufgenommen, ohne Notruf - Ablauf startet " +
                                     "trotzdem, der Notruf fehlt dann in der Messung.");
        Go(Step.PlaceAED);
    }

    // 2 -----------------------------------------------------------------
    void DoPlaceAED()
    {
        if (aed == null || aedTarget == null) return;

        Vector3 a = aed.position, b = aedTarget.position;
        a.y = b.y = 0f;
        float d = Vector3.Distance(a, b);

        bool held = IsHeld(aedHoldable);
        Vector3 p = player != null ? player.position : Vector3.positiveInfinity;
        p.y = 0f;

        // -- Aufnehmen ---------------------------------------------------
        // Der AED steckt in einem Schrank; der Klickstrahl trifft dort oft
        // den Rahmen statt das Geraet, und dann bleibt der ganze Ablauf
        // stehen. Deshalb genuegt Hingehen. Wer ihn per Klick nimmt, wird
        // ueber 'held' genauso erkannt.
        if (!aedCarried && !held && player != null)
        {
            if (Vector3.Distance(p, a) <= aedPickupRadius || (grabQueued && Vector3.Distance(p, a) <= 4f))
            {
                grabQueued = false;
                aedCarried = true;
                Freeze(aedHoldable);

                Rigidbody rb0 = aed.GetComponent<Rigidbody>();
                if (rb0 != null) { rb0.isKinematic = true; rb0.useGravity = false; }
                foreach (Collider c in aed.GetComponentsInChildren<Collider>(true))
                    c.enabled = false;

                aed.SetParent(player, true);
                aed.localPosition = new Vector3(0.35f, 0.15f, 0.65f);
                aed.localRotation = Quaternion.identity;
                if (aedGrabButton != null) aedGrabButton.SetActive(false);
                Debug.Log("[A2] AED aufgenommen. Jetzt zum Patienten gehen.");
            }
            else return;
        }

        // -- Hintragen ---------------------------------------------------
        if (aedCarried || held)
        {
            if (!aedWasHeld && held) { aedWasHeld = true; Debug.Log("[A2] AED aufgenommen (Klick)."); }
            if (player == null) return;
            if (Vector3.Distance(p, b) > aedPlaceRadius) return;

            if (aedCarried) aed.SetParent(null, true);
            Debug.Log("[A2] Spieler mit AED am Patienten - Geraet legt sich ab.");
        }
        else
        {
            if (aedWasHeld)
            {
                aedWasHeld = false;
                Debug.Log("[A2] AED losgelassen, " + d.ToString("0.00") +
                          " m vom Zielpunkt (erlaubt " + aedPlaceRadius.ToString("0.00") + " m).");
            }
            if (d > aedPlaceRadius) return;
        }

        Freeze(aedHoldable);
        StartCoroutine(SnapInto(aed, aedTarget, 0.15f));

        Rigidbody rb = aed.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Geschwindigkeiten nur setzen, solange der Koerper nicht schon
            // kinematisch ist - sonst warnt Unity bei jedem Durchlauf.
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true; rb.useGravity = false;
        }
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
        if (padGuide != null) padGuide.SetActive(true);
        // v4: Die Markierungen bleiben aus. Sichtbar ist nur das Angebot,
        // sie anzufordern.
        if (hintButton != null) hintButton.SetActive(true);
        Say("PLACE BOTH PADS\nON THE PATIENT");

        tTurnedOn = Now();
        Debug.Log("[A2] AED aufgeklappt nach " + tTurnedOn.ToString("0.0") + " s.");
        if (padA != null) Debug.Log("[A2] Pad A liegt bei " + padA.transform.position.ToString("0.00") +
                                    ", Ziel " + (zoneA != null ? zoneA.position.ToString("0.00") : "?"));
        if (padB != null) Debug.Log("[A2] Pad B liegt bei " + padB.transform.position.ToString("0.00") +
                                    ", Ziel " + (zoneB != null ? zoneB.position.ToString("0.00") : "?"));
        Go(Step.Pads);
    }

    // 4 -----------------------------------------------------------------
    void DoPads()
    {
        CheckPad(padA, ref padAok, ref heldA, "A");
        CheckPad(padB, ref padBok, ref heldB, "B");

        if (padAok && padBok)
        {
            tPadsDone = Now();
            if (padGuide != null) padGuide.SetActive(false);
            if (hintButton != null) hintButton.SetActive(false);
            Say("BOTH PADS SIT WELL");
            Debug.Log("[A2] Beide Pads richtig nach " + tPadsDone.ToString("0.0") +
                      " s, " + wrongPlacements + " Fehlversuch(e).");
            Go(Step.ShockAdvised);
        }
    }

    // Beide Pads sind gleichwertig: jedes darf an jede der beiden
    // Stellen. Gewertet wird gegen die naechstgelegene noch freie Zone.
    // In Wirklichkeit sind AED-Pads zwar beschriftet, aber die
    // Reihenfolge ist fuer das Lernziel ohne Belang - und eine
    // Zuordnung, die der Spieler nicht sehen kann, waere nur eine
    // willkuerliche Huerde.
    void CheckPad(Holdable pad, ref bool ok, ref bool wasHeld, string label)
    {
        if (pad == null || ok) return;

        bool held = IsHeld(pad);

        if (held && !wasHeld)
            Debug.Log("[A2] Pad " + label + " aufgenommen.");

        // Ausgewertet wird im Moment des Loslassens, nicht beim Aufnehmen.
        if (wasHeld && !held)
        {
            Vector3 p = pad.transform.position;
            p.y = 0f;

            Transform best = null;
            float bestD = float.MaxValue;
            bool bestIsA = false;

            if (zoneA != null && !zoneAused)
            {
                Vector3 z = zoneA.position; z.y = 0f;
                float d = Vector3.Distance(p, z);
                if (d < bestD) { bestD = d; best = zoneA; bestIsA = true; }
            }
            if (zoneB != null && !zoneBused)
            {
                Vector3 z = zoneB.position; z.y = 0f;
                float d = Vector3.Distance(p, z);
                if (d < bestD) { bestD = d; best = zoneB; bestIsA = false; }
            }

            if (best != null && bestD <= zoneRadius)
            {
                ok = true;
                if (bestIsA) { zoneAused = true; if (markerA != null) markerA.SetActive(false); }
                else         { zoneBused = true; if (markerB != null) markerB.SetActive(false); }

                Freeze(pad);    // festgeklebt, nicht mehr aufnehmbar

                Rigidbody rb = pad.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                    rb.isKinematic = true; rb.useGravity = false;
                }
                // Gleitet in die Endlage, statt dorthin zu springen - das
                // ist der Moment, der sich wie Magnet anfuehlt.
                StartCoroutine(SnapInto(pad.transform, best, 0.18f));
                Paint(pad, matPadOk);
                Say("PAD SITS WELL");
                Debug.Log("[A2] Pad " + label + " richtig an Zone " + (bestIsA ? "A" : "B") +
                          ", Abweichung " + bestD.ToString("0.00") + " m.");
            }
            else
            {
                wrongPlacements++;
                Paint(pad, matPadBad);
                Say("PAD NOT IN PLACE\nTRY AGAIN");
                Debug.Log("[A2] Pad " + label + " falsch, " +
                          (best == null ? "keine freie Zone" : bestD.ToString("0.00") + " m daneben") +
                          " (erlaubt " + zoneRadius.ToString("0.00") + " m).");
            }
        }

        wasHeld = held;
    }

    // Ob ein Objekt gerade getragen wird.
    //
    // NICHT ueber myRayManipulator pruefen: der EZPZ-Interactor raeumt das
    // Feld in DropHoldable() nicht auf, nur ForceDrop() tut das. Nach dem
    // ersten Griff bliebe ein Objekt damit fuer immer "in der Hand" und
    // das Ablegen wuerde nie erkannt. 'moving' wird in beiden Wegen
    // korrekt gesetzt.
    static bool IsHeld(Holdable h)
    {
        return h != null && h.moving;
    }

    // Legt ein Objekt still, ohne die Holdable-Komponente zu loeschen.
    // Der EZPZ-Interactor merkt sich, was er zuletzt angefasst hat. Wird
    // die Komponente zerstoert, wirft er beim naechsten Loslassen eine
    // NullReferenceException und nimmt danach ueberhaupt nichts mehr auf.
    // Abgeschaltete Collider reichen: der Strahl trifft das Objekt dann
    // gar nicht erst.
    void Freeze(Holdable h)
    {
        if (h == null) return;

        // ForceDrop raeumt den Interactor nur halb auf: throwFlag bleibt
        // stehen. Darin merkt sich der Interactor, dass der naechste
        // Loslass-Klick ablegen soll. Blieb es nach dem AED auf true,
        // fiel das erste Pad schon beim allerersten Loslassen zu Boden,
        // und nach Pad A genauso das zweite.
        RaycastInteractor ri = h.myRayManipulator;
        if (ri != null) ri.throwFlag = false;

        if (h.myRayManipulator != null || h.moving) h.ForceDrop();
        foreach (Collider c in h.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        h.enabled = false;
    }

    // Faehrt ein Objekt weich in seine Endlage. Ein harter Sprung liest
    // sich als Fehler, eine kurze Bewegung als Einrasten.
    System.Collections.IEnumerator SnapInto(Transform t, Transform target, float dur)
    {
        if (t == null || target == null) yield break;

        Vector3 p0 = t.position;
        Quaternion r0 = t.rotation;

        for (float e = 0f; e < dur; e += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
            t.position = Vector3.Lerp(p0, target.position, k);
            t.rotation = Quaternion.Slerp(r0, target.rotation, k);
            yield return null;
        }

        t.position = target.position;
        t.rotation = target.rotation;
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
        // v4: Die Warnung kommt VOR dem Knopf.
        //
        // Vorher war es umgekehrt: erst ausloesen, dann zuruecktreten.
        // Das ist die Reihenfolge, die im Ernstfall den Helfer trifft -
        // ein echtes Geraet warnt, bevor es ausloesen laesst, und wer den
        // Patienten dabei beruehrt, bekommt den Schock mit ab. Eine
        // Uebung, die das falsch herum einuebt, ist schlechter als keine.
        if (Time.time - stepStarted < 1.5f) return;

        if (aedButton != null) aedButton.SetActive(false);
        shockArmed = false;
        buttonPressed = false;

        if (clearZone != null)
        {
            clearZone.SetActive(true);
            PaintZone(matZoneBad);
        }

        Say("SHOCK ADVISED\nSTAND CLEAR OF THE PATIENT");
        Debug.Log("[A2] Warnung ausgegeben, Spieler muss " + clearDistance.ToString("0.0") +
                  " m Abstand nehmen, bevor der Knopf scharf wird.");
        Go(Step.StandClear);
    }

    // 6 + 7 ---------------------------------------------------------------
    void DoStandClear()
    {
        if (player == null || patient == null) return;

        Vector3 a = player.position, b = patient.position;
        a.y = b.y = 0f;
        bool clear = Vector3.Distance(a, b) >= clearDistance;

        // Der Ring ist die Rueckmeldung: rot heisst zu nah, gruen heisst
        // bereit. Der Knopf folgt dem Ring, nicht umgekehrt.
        if (clear && !shockArmed)
        {
            shockArmed = true;
            buttonPressed = false;
            PaintZone(matZoneOk);
            if (aedButton != null)
            {
                aedButton.SetActive(true);
                if (aedButtonRenderer != null && matButtonOn != null)
                    aedButtonRenderer.sharedMaterial = matButtonOn;
            }
            Say("YOU ARE CLEAR\nPRESS THE SHOCK BUTTON");
            Debug.Log("[A2] Abstand erreicht, Schockknopf freigegeben.");
        }
        else if (!clear && shockArmed)
        {
            // Wer wieder herantritt, verliert den Knopf. Sonst liesse
            // sich die Warnung umgehen, indem man kurz zuruecktritt.
            shockArmed = false;
            PaintZone(matZoneBad);
            if (aedButton != null) aedButton.SetActive(false);
            Say("TOO CLOSE\nMOVE AWAY FROM THE PATIENT");
            Debug.Log("[A2] Spieler wieder zu nah, Schockknopf gesperrt.");
        }

        if (!shockArmed || !buttonPressed) return;
        buttonPressed = false;

        if (aedButton != null) aedButton.SetActive(false);
        Say("SHOCK DELIVERED");

        tShock = Now();
        clockFrozen = true;      // Endzeit stehen lassen
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
