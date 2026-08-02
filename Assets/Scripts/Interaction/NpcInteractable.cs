using UnityEngine;

/// <summary>
/// Talkable NPC (charter Step 12) -- subclasses Interactable exactly like Chest/Shrine.
/// Deliberately does NOT override CanInteract: NPCs stay talkable indefinitely, unlike Chest's
/// one-shot depletion. Kicks off dialogue via a direct call to DialogueRunner.Instance (Call
/// Down, matching Chest/Shrine's own direct-call style) rather than through EventBus -- per the
/// charter's own Call-Down/Signal-Up distinction, starting dialogue is a command to one specific
/// subsystem with a specific payload, not a fire-and-forget notification (Research finding 6).
/// </summary>
public sealed class NpcInteractable : Interactable
{
    [SerializeField] private DialogueTree dialogueTree;
    [SerializeField] private string npcId;

    // Not read by this class's own logic -- its only purpose is to hold a live reference to
    // the Quest asset this NPC's dialogue advances, so Unity loads (and therefore
    // OnEnable-registers, see Quest.cs/QuestSystem.cs) it as soon as this NpcInteractable is
    // loaded in a scene. Optional: leave null for NPCs whose dialogue only mutates
    // WorldFlag/NpcState, not QuestState.
    [SerializeField] private Quest relatedQuest;

    public override void Interact(Transform interactor)
    {
        DialogueRunner.Instance?.Begin(dialogueTree, npcId);
    }
}
