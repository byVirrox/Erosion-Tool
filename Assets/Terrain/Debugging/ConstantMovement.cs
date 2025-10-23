using UnityEngine;

/// <summary>
/// Bewegt das Transform-Objekt, an dem dieses Script hängt,
/// mit einer konstanten Geschwindigkeit in die angegebene Richtung.
/// </summary>
public class ConstantMovement : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]

    [Tooltip("Die Richtung, in die sich das Objekt bewegen soll. Wird automatisch normalisiert (auf Länge 1 gebracht).")]
    public Vector3 movementDirection = Vector3.forward;

    [Tooltip("Die Geschwindigkeit der Bewegung in Einheiten pro Sekunde.")]
    public float speed = 5.0f;

    /// <summary>
    /// Update wird einmal pro Frame aufgerufen
    /// </summary>
    void Update()
    {

        if (movementDirection == Vector3.zero)
        {
            return; 
        }

        Vector3 direction = movementDirection.normalized;


        Vector3 movement = direction * speed * Time.deltaTime;


        transform.position += movement;
    }
}