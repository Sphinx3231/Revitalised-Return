using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored, asset-time quest definition (charter Step 12). ScriptableObject matching
/// ItemData/StanceData's [CreateAssetMenu] precedent -- questId/state itself lives on
/// PlayerData.questStates (the save-file source of truth, see QuestSystem's own doc comment),
/// this asset is only the static definition. `journalHint` is the charter-required
/// journal-hint-tracked field for optional NPC threads (not GPS-waypoint-tracked).
///
/// Self-registers into QuestSystem's static registry on OnEnable/OnDisable (see
/// QuestSystem.cs's own doc comment) so QuestSystem.TickOnRest(data)'s locked single-arg
/// signature -- the exact call Shrine.Interact makes -- has real quest content to tick against
/// as soon as this asset is loaded, without Shrine needing a direct reference to it.
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "Return/Quest")]
public class Quest : ScriptableObject
{
    public string questId;
    public string displayName;

    [TextArea] public string journalHint;
    [TextArea] public string objectiveDescription;

    /// <summary>
    /// Conditions that, once all true, let QuestSystem.TickOnRest auto-advance this quest from
    /// Active to ObjectiveComplete. Empty/null list means this quest never auto-advances via
    /// TickOnRest (e.g. it's advanced explicitly by a dialogue mutation instead).
    /// </summary>
    public List<DialogueCondition> advancementConditions = new List<DialogueCondition>();

    private void OnEnable()
    {
        QuestSystem.RegisterQuest(this);
    }

    private void OnDisable()
    {
        QuestSystem.UnregisterQuest(this);
    }
}
