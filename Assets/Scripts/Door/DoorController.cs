using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The tag of the player that can open this door (e.g., 'Fireboy' or 'Watergirl')")]
    [SerializeField] private string requiredPlayerTag = "Player"; 
    
    private Animator animator;

    void Start()
    {
        // Automatically grab the Animator attached to this specific door
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object entering the trigger has the specific tag we set in the Inspector
        if (collision.gameObject.CompareTag(requiredPlayerTag)) 
        {
            animator.SetBool("isOpen", true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Check if the correct character is leaving the door area
        if (collision.gameObject.CompareTag(requiredPlayerTag)) 
        {
            animator.SetBool("isOpen", false);
        }
    }
}