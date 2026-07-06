using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach this to each Floor platform in the pulley system.
/// It detects when players stand on top and reports the count
/// to the main PulleySystem controller.
/// </summary>
public class PulleyPlatform : MonoBehaviour
{
    [HideInInspector] public int playersOnPlatform = 0;

    // Track which player GameObjects are currently on this platform
    // to prevent double-counting from multiple collision contacts.
    private HashSet<GameObject> trackedPlayers = new HashSet<GameObject>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;

        // Check if the object is a player or a pushable rock
        bool isPlayer = other.CompareTag("Fireboy") || other.CompareTag("Watergirl");
        bool isPushable = other.GetComponent<PushableRock>() != null || other.name.Contains("Rock");

        // Only react to valid heavy objects, and only if we haven't already counted them
        if ((isPlayer || isPushable) && !trackedPlayers.Contains(other))
        {
            // Make sure the object is ABOVE the platform (standing on top, not bumping the side)
            if (other.transform.position.y > transform.position.y)
            {
                trackedPlayers.Add(other);
                playersOnPlatform = trackedPlayers.Count;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;

        if (trackedPlayers.Contains(other))
        {
            trackedPlayers.Remove(other);
            playersOnPlatform = trackedPlayers.Count;
        }
    }
}
