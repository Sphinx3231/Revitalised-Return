using UnityEngine;

// Shared idle-fade timer for a group of vitals bars (Health/Stamina/Posture). Lives on the
// CanvasGroup parenting the bars; each bar calls Notify() on change instead of owning its own
// fade logic (single responsibility — see docs/Tasks/2026-07-31-ui-systems-phase2.md).
// Uses unscaled time throughout per CLAUDE.md's Paused/Time.timeScale=0 divergence note.
[RequireComponent(typeof(CanvasGroup))]
public class VitalsFader : MonoBehaviour
{
    [SerializeField] private float idleDelaySeconds = 5f;
    [SerializeField] private float idleAlpha = 0.25f;
    [SerializeField] private float fadeDurationSeconds = 0.5f;

    private CanvasGroup _canvasGroup;
    private float _lastActivityTime;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _lastActivityTime = Time.unscaledTime;
    }

    public void Notify()
    {
        _lastActivityTime = Time.unscaledTime;
    }

    private void Update()
    {
        bool idle = Time.unscaledTime - _lastActivityTime >= idleDelaySeconds;
        float target = idle ? idleAlpha : 1f;
        float t = fadeDurationSeconds > 0f ? Time.unscaledDeltaTime / fadeDurationSeconds : 1f;
        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, target, t);
    }
}
