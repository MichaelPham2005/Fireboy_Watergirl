using UnityEngine;

namespace Network
{
    /// <summary>
    /// Follows the local player's character in online multiplayer mode.
    /// Preserves the static camera behavior in local co-op mode.
    /// </summary>
    public class NetworkCameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("The target transform to follow (automatically set to the local player).")]
        public Transform target;
        
        [Header("Follow Settings")]
        [Tooltip("How fast the camera follows the target.")]
        public float smoothSpeed = 5f;
        [Tooltip("The camera will only move if the target moves further than this distance from the center.")]
        public float deadZone = 0.5f;

        [Header("Bounds Settings")]
        [Tooltip("If true, the camera will be clamped within the defined level bounds.")]
        public bool useBounds = true;
        [Tooltip("The bounds to clamp the camera within. Can be auto-populated by a BoxCollider2D tagged 'LevelBounds'.")]
        public Bounds levelBounds;

        private Vector3 targetPosition;
        private Camera cam;

        private void Start()
        {
            cam = GetComponent<Camera>();

            // Attempt to find a BoxCollider2D tagged "LevelBounds" if bounds are enabled but not set
            if (useBounds && levelBounds.size == Vector3.zero)
            {
                GameObject boundsObj = GameObject.Find("LevelBounds");
                if (boundsObj != null)
                {
                    BoxCollider2D boundsCollider = boundsObj.GetComponent<BoxCollider2D>();
                    if (boundsCollider != null)
                    {
                        levelBounds = boundsCollider.bounds;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            // Do not follow in local co-op mode (preserve static camera)
            if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
                return;

            if (target == null)
                return;

            // Calculate the desired position
            targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);

            // Apply dead zone
            Vector2 delta = target.position - transform.position;
            if (delta.magnitude < deadZone)
            {
                targetPosition = transform.position;
            }

            // Apply level bounds clamping
            if (useBounds && cam != null)
            {
                float camHeight = cam.orthographicSize;
                float camWidth = cam.orthographicSize * cam.aspect;

                float minX = levelBounds.min.x + camWidth;
                float maxX = levelBounds.max.x - camWidth;
                float minY = levelBounds.min.y + camHeight;
                float maxY = levelBounds.max.y - camHeight;

                // Ensure bounds are large enough to clamp, otherwise just clamp to the center of the bounds
                if (maxX < minX)
                {
                    minX = maxX = levelBounds.center.x;
                }
                if (maxY < minY)
                {
                    minY = maxY = levelBounds.center.y;
                }

                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            }

            // Smoothly move the camera
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Sets the target for the camera to follow. Called by the local player's network script upon spawning.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            // Instantly snap to target when initially set
            if (target != null)
            {
                Vector3 startPos = new Vector3(target.position.x, target.position.y, transform.position.z);
                transform.position = startPos;
            }
        }
    }
}
