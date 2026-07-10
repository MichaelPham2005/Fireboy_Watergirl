using UnityEngine;

public class Gem : MonoBehaviour
{
    [Header("Gem Settings")]
    [Tooltip("The tag of the player that can collect this gem (e.g., 'Fireboy' or 'Watergirl')")]
    public string requiredPlayerTag = "Fireboy";
    
    [Tooltip("Is this the red gem?")]
    public bool isRedGem = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the correct character touched the gem
        if (collision.gameObject.CompareTag(requiredPlayerTag))
        {
            if (GameManager.Instance != null)
            {
                if (isRedGem)
                {
                    GameManager.Instance.CollectRedGem();
                }
                else
                {
                    GameManager.Instance.CollectBlueGem();
                }
            }

            // Destroy the gem after collection
            Destroy(gameObject);
        }
    }
}
