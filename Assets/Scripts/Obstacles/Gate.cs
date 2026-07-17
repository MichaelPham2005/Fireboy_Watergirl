using UnityEngine;

public class Gate : MonoBehaviour
{
    [Header("Positions (Optional)")]
    [Tooltip("If left empty, the gate's starting position in the scene will be the CLOSED position.")]
    public Transform closedPoint;
    
    [Tooltip("If left empty, the OPEN position will be 2 units ABOVE the starting position.")]
    public Transform openPoint;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Define exact closed and open positions
        closedPos = closedPoint != null ? closedPoint.position : transform.position;
        openPos = openPoint != null ? openPoint.position : transform.position + new Vector3(0, 2f, 0);
    }

    private int openSignals = 0;

    /// <summary>
    /// Changes the state of the gate. Uses reference counting so multiple buttons can control it.
    /// </summary>
    /// <param name="open">True to add an open signal, false to remove one.</param>
    /// <param name="instant">If true, snaps instantly (used for initialization).</param>
    public void SetState(bool open, bool instant = false)
    {
        if (open) openSignals++;
        else openSignals--;

        if (openSignals < 0) openSignals = 0;

        isOpen = (openSignals > 0);

        if (instant)
        {
            if (rb != null) rb.position = isOpen ? openPos : closedPos;
            else transform.position = isOpen ? openPos : closedPos;
        }
    }

    void FixedUpdate()
    {
        // Smoothly move towards the target position at physics framerate
        Vector3 target = isOpen ? openPos : closedPos;
        Vector3 newPos = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.fixedDeltaTime);

        if (rb != null)
        {
            // Use physics MovePosition so players standing on it move with it!
            rb.MovePosition(newPos);
        }
        else
        {
            transform.position = newPos;
        }
    }
}
