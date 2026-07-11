using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Falling Stalactite Trap.
///
/// Behaviour:
///   1. Waits a random interval (hidden).
///   2. Repositions above a random player (same X, ceiling Y).
///   3. Shakes briefly to warn.
///   4. Falls with gravity.
///   5. Kills any player it touches → debris + LoseGame.
///   6. If it falls past the scene without hitting a player → small debris + reset.
///
/// SETUP in Inspector:
///   - Player Targets: drag Fireboy_Player and Watergirl_Player (or leave empty to auto-find).
///   - Max Fall Time: how many seconds before auto-reset if no player was hit (default 6).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FallingStalactite : MonoBehaviour
{
    [Header("Player Targets")]
    [Tooltip("Drag Fireboy_Player and Watergirl_Player. Leave empty to auto-find by tag.")]
    public Transform[] playerTargets;

    [Header("Timing")]
    public float minWaitTime    = 3f;
    public float maxWaitTime    = 8f;

    [Header("Fall Settings — Floor Detection")]
    [Tooltip("Stalactite shatter + reset when its Y drops below this value.\nTip: click the floor tilemap in Scene, read its Y in Inspector, then subtract 1.")]
    public float destroyBelowY  = -5f;

    [Header("Warning Shake")]
    public float warningDuration = 0.6f;
    public float shakeIntensity  = 0.05f;

    [Header("Fall Settings")]
    public float fallGravityScale = 5f;

    [Header("Shatter Effect")]
    [Range(3, 8)] public int   debrisCount        = 5;
    public float               debrisFadeDuration = 0.6f;

    // -------------------------------------------------------------------------
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Collider2D     col;

    private float ceilingY;
    private bool  isFalling;
    private float triggerIgnoreTimer;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        ceilingY = transform.position.y;
    }

    private void Start()
    {
        if (playerTargets == null || playerTargets.Length == 0)
        {
            var list = new List<Transform>();
            var fb = GameObject.FindWithTag("Fireboy");
            var wg = GameObject.FindWithTag("Watergirl");
            if (fb != null) list.Add(fb.transform);
            if (wg != null) list.Add(wg.transform);
            playerTargets = list.ToArray();
        }

        StartCoroutine(StalactiteCycle());
    }

    private void Update()
    {
        if (triggerIgnoreTimer > 0f)
            triggerIgnoreTimer -= Time.deltaTime;
    }

    // =========================================================================
    //  MAIN CYCLE
    // =========================================================================

    private IEnumerator StalactiteCycle()
    {
        while (true)
        {
            // --- PHASE 1: IDLE ---
            SetVisible(false);
            isFalling = false;
            ResetRigidbody();

            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));

            // --- PHASE 2: AIM ---
            Transform target = PickRandomTarget();
            if (target == null) continue;

            Vector3 spawnPos = new Vector3(target.position.x, ceilingY, transform.position.z);
            transform.position = spawnPos;
            SetVisible(true);

            // --- PHASE 3: WARNING SHAKE ---
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                float shake = Mathf.Sin(elapsed * 50f) * shakeIntensity;
                transform.position = spawnPos + new Vector3(shake, 0f, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = spawnPos;

            // --- PHASE 4: FALL ---
            isFalling          = true;
            triggerIgnoreTimer = 0.2f;      // Brief cooldown so ceiling tiles don't fire OnTrigger
            rb.bodyType        = RigidbodyType2D.Dynamic;
            rb.gravityScale    = fallGravityScale;

            // Wait: OnTriggerEnter2D handles player hit.
            // Y-position check handles hitting the floor.
            while (isFalling)
            {
                if (transform.position.y < destroyBelowY)
                {
                    // Passed the floor level — shatter and reset
                    isFalling = false;
                    SpawnDebris(allDirections: false);
                }
                yield return null;
            }
        }
    }

    // =========================================================================
    //  COLLISION — only reacts to players, ignores everything else
    // =========================================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore if not falling, or during the initial cooldown (ceiling tiles)
        if (!isFalling || triggerIgnoreTimer > 0f) return;

        // ONLY kill on player contact — walls, platforms, tilemap → ignored completely
        if (other.CompareTag("Fireboy") || other.CompareTag("Watergirl"))
        {
            isFalling = false;
            SetVisible(false);
            ResetRigidbody();
            SpawnDebris(allDirections: true);

            // Trigger the same death animation as falling into water/lava:
            // PlayerHealth.Die() → plays "Die" animator trigger, hides head, then calls LoseGame
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.Die();
            else
                GameManager.Instance?.LoseGame(); // fallback

            StopAllCoroutines();
            StartCoroutine(StalactiteCycle());
        }
    }

    // =========================================================================
    //  SHATTER EFFECT
    // =========================================================================

    private void SpawnDebris(bool allDirections)
    {
        if (sr == null || sr.sprite == null) return;

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject piece = new GameObject("StalactiteDebris");
            piece.transform.position = transform.position;

            float scale = Random.Range(0.1f, 0.25f);
            piece.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer pieceSR     = piece.AddComponent<SpriteRenderer>();
            pieceSR.sprite             = sr.sprite;
            pieceSR.color              = sr.color;
            pieceSR.sortingLayerName   = sr.sortingLayerName;
            pieceSR.sortingOrder       = sr.sortingOrder + 1;

            Rigidbody2D pieceRb        = piece.AddComponent<Rigidbody2D>();
            pieceRb.gravityScale       = 1.5f;

            float angle = allDirections
                ? Random.Range(0f, 360f)
                : Random.Range(100f, 260f); // upward arc for ground hit

            float speed = Random.Range(2f, 5f);
            pieceRb.linearVelocity  = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * speed,
                Mathf.Sin(angle * Mathf.Deg2Rad) * speed
            );
            pieceRb.angularVelocity = Random.Range(-400f, 400f);

            StartCoroutine(FadeDebris(piece, pieceSR, debrisFadeDuration));
        }
    }

    private IEnumerator FadeDebris(GameObject obj, SpriteRenderer debrisSR, float duration)
    {
        float   elapsed    = 0f;
        Color   startColor = debrisSR.color;
        Vector3 startScale = obj.transform.localScale;

        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            debrisSR.color           = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    // =========================================================================
    //  HELPERS
    // =========================================================================

    private void ResetRigidbody()
    {
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = new Vector3(transform.position.x, ceilingY, transform.position.z);
    }

    private Transform PickRandomTarget()
    {
        if (playerTargets == null || playerTargets.Length == 0) return null;
        var valid = new List<Transform>();
        foreach (var t in playerTargets)
            if (t != null) valid.Add(t);
        return valid.Count == 0 ? null : valid[Random.Range(0, valid.Count)];
    }

    private void SetVisible(bool visible)
    {
        if (sr  != null) sr.enabled  = visible;
        if (col != null) col.enabled = visible;
    }
}
