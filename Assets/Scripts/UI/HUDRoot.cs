using UnityEngine;

// Thin marker/anchor component on the root HUD Canvas GameObject. No EventBus subscriptions of
// its own — sub-elements (HealthBar, StaminaBar, PostureBar, StanceDiamond, NoticeDisplay) each
// own their own single responsibility. See docs/Tasks/2026-07-31-ui-systems-phase2.md.
public class HUDRoot : MonoBehaviour
{
}
