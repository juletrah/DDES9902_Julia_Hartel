// AEDDisplay.cs
// Diegetic display for the AED prototype.
// Instructions appear on the device itself; the clock lives on the wall
// behind the patient so it stays in view while working.
//
// Callable from UnityEvents:
//   Show(string)   - show an instruction
//   StartClock()   - start counting at the emergency call
//   StopClock()    - stop counting when the shock is delivered

using UnityEngine;
using TMPro;

public class AEDDisplay : MonoBehaviour
{
    [Header("Text targets (several, so both sides stay readable)")]
    public TextMeshPro[] instructionTexts;
    public TextMeshPro[] clockTexts;

    [Header("State")]
    public string currentMessage = "";
    public bool clockRunning = false;
    public float elapsed = 0f;

    void Start()
    {
        Apply();
        ApplyClock();
    }

    public void Show(string message)
    {
        currentMessage = message;
        Apply();
    }

    public void StartClock()
    {
        clockRunning = true;
    }

    public void StopClock()
    {
        clockRunning = false;
    }

    void Update()
    {
        if (!clockRunning) return;

        elapsed += Time.deltaTime;
        ApplyClock();
    }

    void Apply()
    {
        if (instructionTexts == null) return;

        foreach (var t in instructionTexts)
            if (t != null) t.text = currentMessage;
    }

    void ApplyClock()
    {
        if (clockTexts == null) return;

        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        string s = minutes.ToString("0") + ":" + seconds.ToString("00");

        foreach (var t in clockTexts)
            if (t != null) t.text = s;
    }
}
