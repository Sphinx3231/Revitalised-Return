using UnityEngine;

/// <summary>
/// Owns dialogue playback state and the GameState.Dialogue transition (charter Step 12).
/// static Instance set in Awake, matching GameState's own MonoBehaviour+Instance convention
/// (Research finding 6 -- needs a real per-frame lifecycle, unlike the static-class QuestSystem).
///
/// SOFTLOCK-RISK CONTRACT (Research finding 5, the single highest-risk item in this task):
/// PlayerRoot.Update() early-returns whenever GameState.IsPlayerInputLocked() is true, which
/// includes Dialogue -- so once Begin() flips state to Dialogue, PlayerRoot's own input
/// handling (movement, interact-consume, etc.) stops entirely for as long as CurrentState stays
/// Dialogue. Advance/choice-select input is therefore handled here, in this component's own
/// Update(), NOT routed through InputBuffer/PlayerRoot. End() is the ONLY path that calls
/// GameState.SetState(Playing) and it is reachable from every branch of Update() below (a
/// terminal node with no choices, or an explicit "end conversation" choice) -- there is no path
/// where dialogue starts (Begin) without a corresponding path back to Playing (End) being
/// reachable from the resulting node graph, short of the DialogueTree asset itself being
/// authored with an unreachable dead-end (a content bug, not a code bug -- flagged for the
/// mandatory Play Mode pass, not something this class can defend against without a real
/// authoring validator, out of scope this task).
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    [SerializeField] private DialogueDisplay display;

    public static DialogueRunner Instance { get; private set; }

    public DialogueTree CurrentTree { get; private set; }
    public DialogueNode CurrentNode { get; private set; }
    public bool IsActive => CurrentTree != null && CurrentNode != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!IsActive)
            return;

        // Advance-on-no-choices input: any of interact/light-attack acts as "continue" when
        // the current node has no branching choices. Deliberately reads Unity's low-level
        // Input directly (not InputBuffer/PlayerInputReader) -- per this class's own contract
        // above, dialogue input must never depend on PlayerRoot's Update(), which is exactly
        // what GameState.Dialogue blocks.
        if ((CurrentNode.choices == null || CurrentNode.choices.Count == 0))
        {
            if (ShouldAdvance())
            {
                End();
            }
        }
    }

    /// <summary>
    /// Polls the real advance-on-no-choices input (E / left mouse / Return). Factored out of
    /// Update() as `protected virtual` purely so EditMode tests can exercise Update()'s branching
    /// (IsActive guard, choices-present guard, the End() call itself) without depending on
    /// UnityEngine.Input's real keyboard/mouse state, which EditMode tests cannot drive -- a test
    /// subclass overrides this one seam instead of the whole Update() method.
    /// </summary>
    protected virtual bool ShouldAdvance()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Return);
    }

    /// <summary>
    /// Starts playback of `tree` for `npcId`. Sets GameState to Dialogue, jumps to the tree's
    /// startNodeId, marks it seen, and applies its mutations -- all on DISPLAY, per the locked
    /// charter rule (an early quit doesn't lose a visibly-triggered state change).
    /// </summary>
    public void Begin(DialogueTree tree, string npcId)
    {
        if (tree == null)
            return;

        CurrentTree = tree;
        GameState.SetState(GameState.State.Dialogue);

        ShowNode(tree.startNodeId);
    }

    /// <summary>
    /// Selects `choice` out of the current node -- applies to the choice's targetNodeId (which
    /// re-triggers the on-display mark-seen/mutation sequence for that node), or ends dialogue
    /// if targetNodeId is empty (an explicit "end conversation" branch).
    /// </summary>
    public void SelectChoice(DialogueChoice choice)
    {
        if (!IsActive || choice == null)
            return;

        if (string.IsNullOrEmpty(choice.targetNodeId))
        {
            End();
            return;
        }

        ShowNode(choice.targetNodeId);
    }

    /// <summary>
    /// Ends dialogue -- unconditionally restores GameState to Playing. This is the ONLY method
    /// that does so; every code path that can start dialogue (Begin) has a reachable path to
    /// this method (see class doc comment).
    /// </summary>
    public void End()
    {
        CurrentTree = null;
        CurrentNode = null;

        display?.Hide();

        GameState.SetState(GameState.State.Playing);
    }

    private void ShowNode(string nodeId)
    {
        DialogueNode node = CurrentTree != null ? CurrentTree.Lookup(nodeId) : null;

        if (node == null)
        {
            // Unknown/missing node id (authoring error) -- fail safe by ending dialogue rather
            // than leaving the player stuck in GameState.Dialogue with nothing displayed.
            End();
            return;
        }

        CurrentNode = node;

        PlayerData data = SaveSystem.CurrentPlayerData;
        if (data != null)
        {
            if (data.dialogueSeen == null)
                data.dialogueSeen = new System.Collections.Generic.HashSet<string>();
            data.dialogueSeen.Add(node.id);

            ApplyMutations(node, data);
        }

        display?.Show(node, this);
    }

    private static void ApplyMutations(DialogueNode node, PlayerData data)
    {
        if (node.mutations == null)
            return;

        foreach (DialogueMutation mutation in node.mutations)
        {
            if (mutation == null)
                continue;

            switch (mutation.source)
            {
                case DialogueCondition.Source.QuestState:
                    if (!string.IsNullOrEmpty(mutation.key))
                        QuestSystem.SetState(data, mutation.key, (QuestState)mutation.intValue);
                    break;

                case DialogueCondition.Source.WorldFlag:
                    if (!string.IsNullOrEmpty(mutation.key))
                    {
                        if (data.worldFlags == null)
                            data.worldFlags = new System.Collections.Generic.Dictionary<string, bool>();
                        data.worldFlags[mutation.key] = mutation.intValue != 0;
                    }
                    break;

                case DialogueCondition.Source.NpcState:
                    if (!string.IsNullOrEmpty(mutation.key))
                    {
                        if (data.npcStates == null)
                            data.npcStates = new System.Collections.Generic.Dictionary<string, string>();
                        data.npcStates[mutation.key] = mutation.stringValue;
                    }
                    break;

                // ItemOwned/DialogueSeen/PlayerLevel are read-only condition sources -- not
                // valid mutation targets (no sensible "write" semantics: DialogueSeen is
                // already written by ShowNode itself, ItemOwned/PlayerLevel belong to other
                // systems' authority). Silently ignored rather than thrown, matching this
                // project's established "unknown/invalid data degrades gracefully" convention.
                default:
                    break;
            }
        }
    }

    /// <summary>Returns choices out of `node` whose visibilityConditions all evaluate true against the active PlayerData (or all choices, unfiltered, if no save context is active).</summary>
    public static System.Collections.Generic.List<DialogueChoice> VisibleChoices(DialogueNode node)
    {
        var result = new System.Collections.Generic.List<DialogueChoice>();
        if (node == null || node.choices == null)
            return result;

        PlayerData data = SaveSystem.CurrentPlayerData;

        foreach (DialogueChoice choice in node.choices)
        {
            if (choice == null)
                continue;

            if (data == null || choice.visibilityConditions == null || choice.visibilityConditions.Count == 0)
            {
                result.Add(choice);
                continue;
            }

            bool allMet = true;
            foreach (DialogueCondition condition in choice.visibilityConditions)
            {
                if (!DialogueConditionEvaluator.Evaluate(condition, data))
                {
                    allMet = false;
                    break;
                }
            }

            if (allMet)
                result.Add(choice);
        }

        return result;
    }
}
