using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GateController : MonoBehaviour
{
    [Header("Gate Settings")]
    [Tooltip("How far the gate should move when open (e.g., X:0, Y:2 means move UP by 2 units)")]
    public Vector3 openOffset = new Vector3(0, 2.5f, 0);
    [Tooltip("How fast the gate opens and closes")]
    public float moveSpeed = 4f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen = false;

    void Start()
    {
        // Record the starting position as the "closed" position
        closedPosition = transform.localPosition;
        // Calculate the target "open" position
        openPosition = closedPosition + openOffset;
    }

    void Update()
    {
        // Move towards open or closed position smoothly
        Vector3 targetPosition = isOpen ? openPosition : closedPosition;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
    }

    public void OpenGate()
    {
        isOpen = true;
    }

    public void CloseGate()
    {
        isOpen = false;
    }
}
