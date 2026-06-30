using UnityEngine;

public class LiquidAnimation : MonoBehaviour
{
    public float waveSpeed = 2f;     // Speed of the wave animation
    public float waveAmount = 0.03f; // Amplitude of the wave (keep small to prevent texture distortion)

    private Vector3 startScale;

    void Start()
    {
        // Store the initial scale to use as a baseline
        startScale = transform.localScale;
    }

    void Update()
    {
        // Use Sine wave to create a smooth bouncing/breathing effect on the Y axis
        float newY = startScale.y + Mathf.Sin(Time.time * waveSpeed) * waveAmount;
        transform.localScale = new Vector3(startScale.x, newY, startScale.z);
    }
}