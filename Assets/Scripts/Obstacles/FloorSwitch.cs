using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class FloorSwitch : MonoBehaviour
{
    [Header("Switch Connections")]
    [Tooltip("Drag the GateController script (on the Gate object) into this slot")]
    public GateController linkedGate;

    [Header("Switch Visuals")]
    [Tooltip("How far the button presses down visually")]
    public float pressDownDistance = 0.2f;
    public float pressSpeed = 8f;

    private Vector3 unpressedPosition;
    private Vector3 pressedPosition;
    private bool isPressed = false;

    // Keep track of how many objects are currently on the switch
    private HashSet<Collider2D> objectsOnSwitch = new HashSet<Collider2D>();

    void Start()
    {
        unpressedPosition = transform.localPosition;
        pressedPosition = unpressedPosition + new Vector3(0, -pressDownDistance, 0);

        // Ensure the collider is set as a trigger
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        // Smoothly press down or pop up the button visually
        Vector3 targetPos = isPressed ? pressedPosition : unpressedPosition;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, pressSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if a player or a box stepped on the switch
        if (collision.CompareTag("Fireboy") || collision.CompareTag("Watergirl") || collision.CompareTag("Box"))
        {
            objectsOnSwitch.Add(collision);
            UpdateSwitchState();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (objectsOnSwitch.Contains(collision))
        {
            objectsOnSwitch.Remove(collision);
            UpdateSwitchState();
        }
    }

    private void UpdateSwitchState()
    {
        bool wasPressed = isPressed;
        // If at least one valid object is on the switch, it is pressed
        isPressed = objectsOnSwitch.Count > 0;

        // If the state changed, notify the gate
        if (isPressed && !wasPressed)
        {
            if (linkedGate != null) linkedGate.OpenGate();
        }
        else if (!isPressed && wasPressed)
        {
            if (linkedGate != null) linkedGate.CloseGate();
        }
    }
}
