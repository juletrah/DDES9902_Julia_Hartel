// A1RunMetrics.cs
// Erhebt Verhaltensdaten waehrend eines Durchlaufs und zeigt sie am Ende
// auf einem Abschluss-Panel an, das die Testperson in die Umfrage abtippt.
//
// Warum so: In einem WebGL-Build kann der Prototyp keine Datei schreiben,
// die bei der Forscherin ankommt - der Browser laesst ihn nicht aus seiner
// Sandbox heraus. Statt eines Backends uebernimmt deshalb die Testperson
// den Transport: sie kopiert eine Zeile in das Umfrageformular.
//
// Die Messwerte kommen aus A2Flow, der den ganzen Ablauf steuert. Fruehere
// Fassungen lasen aus A1Sequence und A1Reveal - die treiben den Ablauf
// nicht mehr, weshalb das Panel zuletzt nur Striche und Nullen zeigte.
//
// Erhoben wird:
//   Zeit bis zum Notruf, bis der AED liegt, bis zum Schock (= Gesamtzeit),
//   ob beide Pads richtig sassen, Zahl der Fehlversuche, ob sich die Person
//   im Raum bewegt hat, ob der Durchlauf vollstaendig war.
//
// Alle Zeiten zaehlen ab Spielbeginn, wie die Wanduhr.

using UnityEngine;
using TMPro;

public class A1RunMetrics : MonoBehaviour
{
    // Bleibt fuer alte Aufrufer bestehen; gezaehlt wird jetzt in A2Flow.
    public static int wrongPlacements = 0;
    public static void WrongPad() { wrongPlacements++; }

    [Header("Links")]
    public A2Flow flow;

    // Werden nicht mehr gelesen. Sie bleiben nur, weil aeltere
    // Editor-Skripte (A1_BuildV10 und Geschwister) sie noch zuweisen -
    // ohne die Felder kompiliert das Projekt nicht mehr.
    [HideInInspector] public A1Sequence sequence;
    [HideInInspector] public AEDDisplay display;
    [HideInInspector] public A1Reveal reveal;
    public GameObject panel;
    public TMP_Text panelText;

    [Header("Einstellungen")]
    [Tooltip("Ab welcher zurueckgelegten Strecke die Person als 'hat sich bewegt' gilt.")]
    public float movedThreshold = 1.5f;
    [Tooltip("Taste, mit der das Panel auch ohne Abschluss erscheint.")]
    public KeyCode manualKey = KeyCode.F9;

    [Header("Status")]
    public int runCode = 0;
    public bool shown = false;

    float distance;
    Vector3 lastPos;
    Transform player;

    void Start()
    {
        wrongPlacements = 0;
        runCode = Random.Range(1000, 10000);

        if (panel != null) panel.SetActive(false);

        if (Camera.main != null)
        {
            player = Camera.main.transform;
            lastPos = Flat(player.position);
        }

        if (flow == null) flow = Object.FindFirstObjectByType<A2Flow>();
        if (flow == null)
            Debug.LogWarning("[A1RunMetrics] Kein A2Flow gefunden - das Panel bliebe leer.");
    }

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

        // ------------------------------------------------ fertig?
        if (!shown && flow != null && flow.IsDone)
            Show();

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

        float tCall  = flow != null ? flow.tCall : -1f;
        float tAed   = flow != null ? flow.tAedPlaced : -1f;
        float tShock = flow != null ? flow.tShock : -1f;
        int   wrong  = flow != null ? flow.wrongPlacements : wrongPlacements;
        int   padsOk = (flow != null && flow.PadsOk) ? 1 : 0;
        int   done   = (flow != null && flow.AllTasks) ? 1 : 0;
        int   moved  = distance >= movedThreshold ? 1 : 0;

        string line = runCode + " | " + S(tCall) + " | " + S(tAed) + " | " + S(tShock) +
                      " | " + padsOk + " | " + wrong + " | " + moved + " | " + done;

        string body =
            "<b>RUN FINISHED</b>\n\n" +
            "Your run code: <b>" + runCode + "</b>\n\n" +
            "time to emergency call: " + S(tCall) + " s\n" +
            "time to AED placed:     " + S(tAed) + " s\n" +
            "total time to shock:    " + S(tShock) + " s\n" +
            "pads placed correctly:  " + padsOk + "\n" +
            "wrong placements:       " + wrong + "\n" +
            "moved around:           " + moved + "\n" +
            "all tasks completed:    " + done + "\n\n" +
            "<b>Please copy this line into the survey:</b>\n" +
            "<size=120%><b>" + line + "</b></size>";

        if (panelText != null) panelText.text = body;
        if (panel != null) panel.SetActive(true);

        Debug.Log("[A1RunMetrics] " + line);
    }

    static string S(float t)
    {
        return t < 0f ? "-" : Mathf.RoundToInt(t).ToString();
    }
}
