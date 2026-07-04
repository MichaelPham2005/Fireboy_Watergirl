using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The tag of the player that can open this door (e.g., 'Fireboy' or 'Watergirl')")]
    [SerializeField] public string requiredPlayerTag = "Player"; 
    
    private Animator animator;
    public bool IsPlayerReady { get; private set; } = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(requiredPlayerTag))
        {
            // Player entered their correct door
            IsPlayerReady = true;
            if (animator != null)
            {
                animator.SetBool("IsOpen", true);
            }
            if (GameManager.Instance != null) GameManager.Instance.CheckWinCondition();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(requiredPlayerTag))
        {
            // Player left the door
            IsPlayerReady = false;
            if (animator != null)
            {
                animator.SetBool("IsOpen", false);
            }
        }
    }
}