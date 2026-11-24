// A1RunMetrics.cs
// Erhebt Verhaltensdaten waehrend eines Durchlaufs und zeigt sie am Ende
// auf einem Abschluss-Panel an, das die Testperson in die Umfrage abtippt.
//
// Warum so: In einem WebGL-Build kann der Prototyp keine Datei schreiben,
// die bei der Forscherin ankommt - der Browser laesst ihn nicht aus seiner
// Sandbox heraus. Statt eines Backends uebernimmt deshalb die Testperson
// den Transport: sie kopiert eine Zeile in das Umfrageformular. Das Panel
// ist zugleich die Rueckmeldung am Ende des Ablaufs, die der Prototyp
// vorher nicht hatte.
//
// Zaehlt: Zeit bis AED abgelegt, bis beide Pads sitzen, bis Schock;
// Platzierungsversuche; Fehlversuche; ob sich die Person im Raum bewegt
// hat; ob der Durchlauf abgeschlossen wurde.

using UnityEngine;
using TMPro;

public class A1RunMetrics : MonoBehaviour
{
    // Von A1PadTarget und A1SnapLock aus einer Zeile heraus hochgezaehlt.
    public static int wrongPlacements = 0;
    public static void WrongPad() { wrongPlacements++; }

    [Header("Links")]
    public A1Sequence sequence;
    public AEDDisplay display;
    public A1Reveal reveal;
    public GameObject panel;
    public TMP_Text panelText;

    [Header("Einstellungen")]
    [Tooltip("Ab welcher zurueckgelegten Strecke die Person als 'hat sich bewegt' gilt.")]
    public float movedThreshold = 1.5f;
    [Tooltip("Taste, mit der das Panel auch ohne Abschluss erscheint.")]
    public KeyCode manualKey = KeyCode.F9;

    [Header("Status")]
    public int runCode = 0;
    public float tAedPlaced = -1f;
    public float tPadsDone = -1f;
    public float tShock = -1f;
    public bool completed = false;
    public bool shown = false;

    float t0;
    float distance;
    Vector3 lastPos;
    Transform player;
    bool clockWasRunning;

    void Start()
    {
        wrongPlacements = 0;
        runCode = Random.Range(1000, 10000);
        t0 = Time.time;

        if (panel != null) panel.SetActive(false);

        if (Camera.main != null)
        {
            player = Camera.main.transform;
            lastPos = Flat(player.position);
        }
    }

    float Now() { return Time.time - t0; }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    void Update()
    {
        // ------------------------------------------------ zurueckgelegte Strecke
        if (player != null)
        {
            Vector3 p = Flat(player.position);
            distance += Vector3.Distance(p, lastPos);
            lastPos = p;
        }

        // ------------------------------------------------ Meilensteine
        if (tAedPlaced < 0f && reveal != null && reveal.revealed)
            tAedPlaced = Now();

        if (tPadsDone < 0f && sequence != null && sequence.padsReady)
            tPadsDone = Now();

        // Die Uhr laeuft ab dem Notruf und wird beim Schock gestoppt -
        // dieser Wechsel ist das verlaesslichste Signal fuer "fertig".
        if (display != null)
        {
            if (display.clockRunning) clockWasRunning = true;
            else if (clockWasRunning && tShock < 0f)
            {
                tShock = Now();
                completed = true;
                Show();
            }
        }

        // Das Projekt nutzt das neue Input System; je nach Projekteinstellung
        // wirft die alte Input-Klasse dann eine Ausnahme. Deshalb abgesichert:
        // der Tastendruck ist nur eine Bequemlichkeit, kein tragender Teil.
        if (!shown)
        {
            try { if (Input.GetKeyDown(manualKey)) Show(); }
            catch (System.Exception) { manualKey = KeyCode.None; }
        }
    }

    public void Show()
    {
        shown = true;

        string line = runCode + " | " + S(tAedPlaced) + " | " + S(tPadsDone) + " | " +
                      S(tShock) + " | " + Pads() + " | " + wrongPlacements + " | " +
                      (Moved() ? 1 : 0) + " | " + (completed ? 1 : 0);

        string body =
            "<b>RUN FINISHED</b>\n\n" +
            "Your run code: <b>" + runCode + "</b>\n\n" +
            "time to AED placed:  " + S(tAedPlaced) + " s\n" +
            "time to pads placed: " + S(tPadsDone) + " s\n" +
            "time to shock:       " + S(tShock) + " s\n" +
            "pads placed:         " + Pads() + "\n" +
            "wrong placements:    " + wrongPlacements + "\n" +
            "moved around:        " + (Moved() ? "yes" : "no") + "\n" +
            "completed:           " + (completed ? "yes" : "no") + "\n\n" +
            "<b>Please copy this line into the survey:</b>\n" +
            "<size=120%><b>" + line + "</b></size>";

        if (panelText != null) panelText.text = body;
        if (panel != null) panel.SetActive(true);

        Debug.Log("[A1RunMetrics] " + line);
    }

    bool Moved() { return distance >= movedThreshold; }

    int Pads() { return sequence != null ? sequence.padsAttached : 0; }

    static string S(float t)
    {
        return t < 0f ? "-" : Mathf.RoundToInt(t).ToString();
    }
}
