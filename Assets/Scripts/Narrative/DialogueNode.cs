using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One node of a DialogueTree (charter Step 12). Plain serializable data -- no behavior,
/// matching RegionNode's "graph is the ScriptableObject, node is its serialized payload"
/// precedent. `mutations` apply on node DISPLAY, not exit (explicit locked charter rule -- an
/// early quit doesn't lose a visibly-triggered state change); DialogueRunner.Begin/Advance is
/// what actually applies them, this class only carries the data.
///
/// portrait/voiceClip/cameraShotMarker/animationCue are locked in now even though no portrait
/// art/VO/animation content exists yet -- inert data, zero dependencies, same "a data field
/// isn't a method" precedent Step 10 used for Interactable.promptText.
/// </summary>
[System.Serializable]
public class DialogueNode
{
    public string id;
    public string speaker;
    [TextArea] public string text;
    public Sprite portrait;
    public AudioClip voiceClip;
    public string cameraShotMarker;
    public string animationCue;

    public List<DialogueChoice> choices = new List<DialogueChoice>();
    public List<DialogueMutation> mutations = new List<DialogueMutation>();
}

/// <summary>One branch out of a DialogueNode. Hidden (not shown as an option) unless every visibilityCondition evaluates true.</summary>
[System.Serializable]
public class DialogueChoice
{
    public string text;
    public string targetNodeId;
    public List<DialogueCondition> visibilityConditions = new List<DialogueCondition>();
}

/// <summary>
/// A single state write, applied when its owning DialogueNode is displayed. Structurally
/// parallel to DialogueCondition (a mutation writes a value at `source`+`key`, a condition
/// reads one) so the same Source vocabulary covers both read and write sides.
/// </summary>
[System.Serializable]
public class DialogueMutation
{
    public DialogueCondition.Source source;
    public string key;
    public int intValue;
    public string stringValue;
}
