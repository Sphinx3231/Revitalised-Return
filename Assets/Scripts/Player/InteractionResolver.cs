using UnityEngine;

/// <summary>
/// Player component ranking/selecting Interactable candidates (charter Step 10). Only ranks
/// and selects -- it doesn't own interaction *behavior* (that's each Interactable subclass's
/// job, S.O.L.I.D.). Explicit-Tick pattern (no Update()), matching every other component in
/// this codebase -- driven by PlayerRoot's per-frame tick order.
///
/// Detection mechanism: manual Physics.OverlapSphere scan every tick (mirroring
/// EnemyPerception's exact pattern), not OnTriggerEnter/Exit tracking -- the ranking
/// formula's camera-dot term changes every frame with no collider event, so an enter/exit-
/// tracked candidate set would still need a full per-frame re-rank anyway.
///
/// Physics.autoSyncTransforms is false project-wide (confirmed) -- Physics.SyncTransforms()
/// is called before the OverlapSphere query, same as EnemyPerception's raycasts.
/// QueryTriggerInteraction.Collide is explicit (inverse of EnemyPerception's .Ignore --
/// Interactable colliders ARE triggers, on purpose, this time).
/// </summary>
public sealed class InteractionResolver : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private LayerMask interactableLayerMask;

    /// <summary>Highest-scoring currently-valid candidate, or null. Useful for a future Step 11 prompt UI.</summary>
    public Interactable CurrentCandidate { get; private set; }

    private readonly Collider[] _overlapBuffer = new Collider[16];

    /// <summary>
    /// Pure function implementing the locked ranking formula: 0.7 x camera-forward-dot +
    /// 0.3 x proximity. No Unity physics calls -- fully unit-testable in isolation. A
    /// candidate directly behind the camera scores a strongly negative dot term (dot is left
    /// unclamped on the low end so "behind" candidates rank clearly below "in front but far"
    /// candidates rather than being floored to the same 0).
    /// </summary>
    public static float ScoreCandidate(Vector3 cameraForward, Vector3 cameraPosition, Vector3 candidatePosition, float maxRange)
    {
        Vector3 toCandidate = candidatePosition - cameraPosition;
        float distance = toCandidate.magnitude;

        float dot;
        if (distance <= 0.0001f)
        {
            dot = 1f; // Degenerate case (candidate at the camera position) -- treat as maximally forward.
        }
        else
        {
            dot = Vector3.Dot(cameraForward.normalized, toCandidate / distance);
        }

        float proximity = maxRange > 0f
            ? 1f - Mathf.Clamp01(distance / maxRange)
            : 0f;

        return 0.7f * dot + 0.3f * proximity;
    }

    /// <summary>
    /// Scans for Interactable candidates in range, ranks them, and updates CurrentCandidate.
    /// Does NOT consume the input buffer or invoke Interact() itself -- PlayerRoot owns the
    /// single consume-and-act call site (see PlayerRoot's tick order), so double-fire
    /// prevention and interaction triggering both live in one place.
    /// </summary>
    public void Tick(float deltaTime)
    {
        CurrentCandidate = null;

        if (cameraTransform == null)
            return;

        Physics.SyncTransforms();

        int count = Physics.OverlapSphereNonAlloc(transform.position, interactionRadius, _overlapBuffer, interactableLayerMask, QueryTriggerInteraction.Collide);

        Interactable best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null)
                continue;

            Interactable interactable = col.GetComponent<Interactable>();
            if (interactable == null)
                interactable = col.GetComponentInParent<Interactable>();

            if (interactable == null || !interactable.CanInteract(transform))
                continue;

            float score = ScoreCandidate(cameraTransform.forward, cameraTransform.position, interactable.transform.position, interactionRadius);

            if (score > bestScore)
            {
                bestScore = score;
                best = interactable;
            }
        }

        CurrentCandidate = best;
    }
}
