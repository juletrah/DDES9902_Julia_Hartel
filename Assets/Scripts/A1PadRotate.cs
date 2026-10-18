using UnityEngine;

// Erlaubt dem Spieler, ein gehaltenes Pad zu kippen. Holdable.RotateY dreht
// nur um die Hochachse - fuer das seitliche Pad muss es sich aber auf die
// Kante stellen, also um die eigene Laengsachse.
public class A1PadRotate : MonoBehaviour
{
    public float step = 90f;

    public void Roll()
    {
        transform.Rotate(0f, 0f, step, Space.Self);
    }

    public void Yaw()
    {
        transform.Rotate(0f, step, 0f, Space.Self);
    }
}
