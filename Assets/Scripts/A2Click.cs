// A2Click.cs
// Meldet einen Mausklick auf dieses Objekt an A2Flow.
//
// Bewusst Unitys eigenes OnMouseDown statt der EZPZ-Interaktion: der AED
// ist ein Holdable, ein Klick darueber wuerde ihn aufnehmen statt
// einzuschalten. Sobald der AED abgelegt ist, schaltet A2Flow sein
// Holdable ab - dann bleibt der Klick hier haengen und schaltet das
// Geraet ein.

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class A2Click : MonoBehaviour
{
    public A2Flow flow;

    [Tooltip("Nur zum Nachsehen: zaehlt die erkannten Klicks.")]
    public int clicks = 0;

    void OnMouseDown()
    {
        clicks++;

        if (flow == null)
        {
            Debug.LogWarning("[A2Click] Kein A2Flow verbunden.");
            return;
        }

        flow.OnAedClicked();
        Debug.Log("[A2Click] Klick auf '" + name + "' (" + clicks + ").");
    }
}
