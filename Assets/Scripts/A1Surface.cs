// A1Surface.cs
// Gemeinsame Hilfe fuer beide Platzierungswege: ein Objekt von der
// Ankerhoehe auf die Koerperoberflaeche absenken, damit es aufliegt
// statt darueber zu schweben.
//
// Es gibt im Prototyp zwei Wege, auf denen ein Pad an seinen Platz
// kommt - den Klick auf eine Stelle (A1PadTarget) und das Ablegen in
// ein Volumen (A1SnapLock). Beide setzten das Pad bisher einfach auf
// die Ankerposition. Damit sie sich gleich verhalten, liegt die Logik
// hier an einer Stelle.

using UnityEngine;

public static class A1Surface
{
    /// <summary>
    /// Senkt <paramref name="obj"/> auf die Flaeche unter dem Anker ab.
    /// </summary>
    /// <param name="surface">
    /// Wenn gesetzt, zaehlen nur Treffer auf diesem Objekt oder seinen
    /// Kindern. Das ist der verlaessliche Weg: im Raum liegen mehrere
    /// unsichtbare Klickvolumen uebereinander, die ein Strahl sonst
    /// zuerst trifft.
    /// </param>
    public static bool Settle(
        Transform obj,
        Transform anchor,
        Transform surface,
        Transform ignoreRoot,
        float rayStartHeight,
        float rayLength,
        float skin,
        bool align)
    {
        if (obj == null || anchor == null) return false;

        Vector3 start = anchor.position + Vector3.up * rayStartHeight;

        RaycastHit[] hits = Physics.RaycastAll(
            start, Vector3.down, rayLength, ~0, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (h.collider == null) continue;

            Transform t = h.collider.transform;

            if (t.IsChildOf(obj)) continue;
            if (ignoreRoot != null && t.IsChildOf(ignoreRoot)) continue;

            // Nur die vorgesehene Oberflaeche zaehlt, wenn eine angegeben ist.
            if (surface != null && !t.IsChildOf(surface)) continue;

            float half = 0f;
            Renderer r = obj.GetComponentInChildren<Renderer>();
            if (r != null) half = r.bounds.extents.y;

            if (align)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(anchor.forward, h.normal);
                if (fwd.sqrMagnitude > 0.0001f)
                    obj.rotation = Quaternion.LookRotation(fwd, h.normal);
            }

            obj.position = h.point + h.normal * (half + skin);
            return true;
        }

        Debug.LogWarning("[A1Surface] Keine Oberflaeche unter dem Anker '" +
                         anchor.name + "' gefunden" +
                         (surface != null ? " (gesucht wurde nur auf '" + surface.name + "')" : "") +
                         ". '" + obj.name + "' bleibt auf Ankerhoehe.");
        return false;
    }
}
