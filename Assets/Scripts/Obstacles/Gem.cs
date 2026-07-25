using UnityEngine;
using Fusion;
using Network;

public class Gem : NetworkBehaviour
{
    [Header("Gem Settings")]
    [Tooltip("The tag of the player that can collect this gem (e.g., 'Fireboy' or 'Watergirl')")]
    public string requiredPlayerTag = "Fireboy";
    
    [Tooltip("Is this the red gem?")]
    public bool isRedGem = true;

    private bool isCollectedLocal = false;
    [Networked] private NetworkBool IsCollectedNetwork { get; set; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the correct character touched the gem
        if (collision.gameObject.CompareTag(requiredPlayerTag))
        {
            if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
            {
                if (isCollectedLocal) return;
                isCollectedLocal = true;

                if (GameManager.Instance != null)
                {
                    if (isRedGem) GameManager.Instance.CollectRedGem();
                    else GameManager.Instance.CollectBlueGem();
                }
                Destroy(gameObject);
            }
            else
            {
                // In Online mode, the player with local control detects the collision and requests collection
                NetworkObject no = collision.gameObject.GetComponent<NetworkObject>();
                if (no != null && no.HasInputAuthority)
                {
                    RPC_RequestCollectGem();
                }
                // Host also requests if they have state authority and the player touched it
                else if (HasStateAuthority)
                {
                    RPC_RequestCollectGem();
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestCollectGem()
    {
        if (IsCollectedNetwork) return;
        
        IsCollectedNetwork = true;
        if (GameManager.Instance != null)
        {
            if (isRedGem) GameManager.Instance.CollectRedGem();
            else GameManager.Instance.CollectBlueGem();
        }
        
        // Despawn network object safely
        Runner.Despawn(Object);
    }
}
