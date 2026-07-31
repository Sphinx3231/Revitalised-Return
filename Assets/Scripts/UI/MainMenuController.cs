using UnityEngine;

// Minimal MainMenu wiring: toggles the Settings panel stub. No save/load, no rebind
// persistence, no scene-load logic yet — those are out of scope for this UI pass. See
// docs/Tasks/2026-07-31-ui-systems-phase2.md.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    public void ToggleSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}
