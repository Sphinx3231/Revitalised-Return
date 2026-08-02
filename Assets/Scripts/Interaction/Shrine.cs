using UnityEngine;

/// <summary>
/// Rest behavior (charter Step 10 placeholder, now wired up per Step 11's checkpoint-trigger
/// research finding 6: "extend Shrine.Interact" -- its own prior doc comment named this exact
/// seam). Of the charter's 7 checkpoint triggers, shrine-rest is the only one with a real
/// firing system today; this proves persistence + shrine discovery + map-reveal in one path.
///
/// Reaches the active save via SaveSystem's static Current/CurrentPlayerData holders (the
/// project's established GameState.Instance-style cross-cutting-state convention) rather than
/// a direct reference chain -- Shrine has no idea who set those up (SaveSlotMenu at the main
/// menu, or a Sandbox test-scene bootstrap), matching Dependency Inversion. If no save context
/// is active (SaveSystem.CurrentPlayerData is null -- e.g. a scene entered directly without
/// going through slot-select) this degrades to the original placeholder behavior: the notice
/// still fires, nothing throws, nothing is silently half-saved.
/// </summary>
public sealed class Shrine : Interactable
{
    [SerializeField] private string shrineId;

    public override void Interact(Transform interactor)
    {
        PlayerData data = SaveSystem.CurrentPlayerData;

        if (data != null && !string.IsNullOrEmpty(shrineId))
        {
            data.discoveredShrines.Add(shrineId);

            // Charter Step 12: shrine rest is "the single deterministic quest-tick point" --
            // this MUST run before SaveSystem.Save() below, or every rest persists the
            // pre-tick state and a reload loses one tick of progression (locked, tested
            // ordering, not a style preference).
            QuestSystem.TickOnRest(data);

            SaveSystem.Current?.Save(SaveSystem.CurrentSlot, data);
        }

        EventBus.RaiseShowNotice("Rested at the shrine.", 3f);
    }
}
