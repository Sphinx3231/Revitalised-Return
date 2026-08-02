using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainMenu.unity slot-select panel (charter Step 11, Research finding 7 / Approach ruling
/// (b)). Repoints the previously-dead PlayButton.onClick to Open() this panel instead of
/// MainMenuController -- deliberately NOT folded into MainMenuController.cs (single
/// responsibility: that class only owns the Settings panel toggle, confirmed by reading it
/// before this file was written).
///
/// Scope boundary (Director-approved, not an oversight): SelectSlot loads PlayerData into
/// memory and marks it as the active save via SaveSystem's static Current/CurrentPlayerData/
/// CurrentSlot holders, but does NOT trigger an actual scene transition -- MainMenu.unity
/// isn't registered in EditorBuildSettings and Prologue.unity has no FPS rig ported yet, both
/// pre-existing, separately logged gaps this task does not resolve.
/// </summary>
public class SaveSlotMenu : MonoBehaviour
{
    [SerializeField] private GameObject slotPanel;
    [SerializeField] private Text[] slotLabels;

    private SaveSystem _saveSystem;

    private void Awake()
    {
        _saveSystem = new SaveSystem();
    }

    public void Open()
    {
        if (slotPanel != null)
            slotPanel.SetActive(true);

        RefreshLabels();
    }

    public void Close()
    {
        if (slotPanel != null)
            slotPanel.SetActive(false);
    }

    private void RefreshLabels()
    {
        if (slotLabels == null)
            return;

        for (int i = 0; i < slotLabels.Length; i++)
        {
            if (slotLabels[i] == null)
                continue;

            SaveSlotInfo info = _saveSystem.PeekSlot(i);
            slotLabels[i].text = info.exists
                ? $"Slot {i + 1} - Lv.{info.level} - {info.regionId} - {info.lastPlayedUtc:yyyy-MM-dd}"
                : $"Slot {i + 1} - Empty Slot";
        }
    }

    /// <summary>
    /// Selects `slot` as the active save: loads its PlayerData (or creates a fresh one for an
    /// empty slot -- "new game") and publishes it via SaveSystem's static holders so gameplay
    /// systems (e.g. Shrine) can reach it. No ItemDatabase is wired into MainMenu.unity this
    /// task, so a loaded save's inventory entries silently fail to resolve here -- everything
    /// else on PlayerData still loads correctly.
    /// </summary>
    public void SelectSlot(int slot)
    {
        SaveSystem.Current = _saveSystem;
        SaveSystem.CurrentSlot = slot;
        SaveSystem.CurrentPlayerData = _saveSystem.Load(slot, null) ?? new PlayerData();

        // TODO: scene transition — see docs/Tasks/2026-08-02-step-11-hud-persistence.md
    }
}
