using UnityEngine;
using UnityEngine.UI;

// Single-responsibility bar: displays EventBus.PlayerHealthChanged only. See
// docs/Tasks/2026-07-31-ui-systems-phase2.md for the S.O.L.I.D. component split rationale.
public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private VitalsFader fader;

    private void Awake()
    {
        // Sane default: no real emitter fires PlayerHealthChanged yet (Phase 3/Combat territory).
        if (fillImage != null)
            fillImage.fillAmount = 1f;
    }

    private void OnEnable()
    {
        EventBus.PlayerHealthChanged += HandleChanged;
    }

    private void OnDisable()
    {
        EventBus.PlayerHealthChanged -= HandleChanged;
    }

    private void HandleChanged(float current, float max)
    {
        if (fillImage != null)
            fillImage.fillAmount = max > 0f ? current / max : 0f;
        fader?.Notify();
    }
}
