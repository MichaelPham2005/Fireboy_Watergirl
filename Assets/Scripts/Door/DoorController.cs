using UnityEngine;
using Fusion;
using Network;

public class DoorController : NetworkBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The tag of the player that can open this door (e.g., 'Fireboy' or 'Watergirl')")]
    [SerializeField] public string requiredPlayerTag = "Player"; 
    
    private Animator animator;
    public bool IsPlayerReady { get; private set; } = false;

    [Networked] public NetworkBool NetworkIsOpen { get; set; }

    private ChangeDetector _changes;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(NetworkIsOpen):
                        if (animator != null)
                        {
                            animator.SetBool("isOpen", NetworkIsOpen);
                        }
                        break;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && !HasStateAuthority)
            return;

        if (other.CompareTag(requiredPlayerTag))
        {
            IsPlayerReady = true;
            if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
            {
                if (animator != null) animator.SetBool("isOpen", true);
            }
            else
            {
                NetworkIsOpen = true;
            }

            if (GameManager.Instance != null) GameManager.Instance.CheckWinCondition();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && !HasStateAuthority)
            return;

        if (other.CompareTag(requiredPlayerTag))
        {
            IsPlayerReady = false;
            if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
            {
                if (animator != null) animator.SetBool("isOpen", false);
            }
            else
            {
                NetworkIsOpen = false;
            }
        }
    }
}