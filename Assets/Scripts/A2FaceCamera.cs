using UnityEngine;

// Dreht ein Objekt so, dass es zur Kamera zeigt.
//
// TextMeshPro ist von seiner -z-Seite lesbar. Die Blickrichtung wird
// deshalb von der Kamera WEG angesetzt: zeigt +z vom Betrachter fort,
// liegt die lesbare Seite ihm zugewandt.
public class A2FaceCamera : MonoBehaviour
{
    [Tooltip("Nur um die Hochachse drehen. Verhindert, dass die Schrift " +
             "kippt, wenn die Person nach oben oder unten schaut.")]
    public bool keepUpright = true;

    Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = transform.position - cam.transform.position;
        if (keepUpright) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
