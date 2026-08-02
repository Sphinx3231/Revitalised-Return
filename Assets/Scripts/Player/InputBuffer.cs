using System.Collections.Generic;

/// <summary>
/// Rolling action buffer (charter Step 3.2). Pure C#, no UnityEngine/MonoBehaviour
/// dependency — timestamps are supplied by the caller (typically Time.time) so this
/// class is unit-testable independent of the Unity runtime/play mode.
///
/// Semantics ported verbatim from the legacy Godot implementation's consume_action
/// (see docs/Tasks/2026-07-31-player-base-character.md Research Findings, item 6):
///   - Push appends an entry with its action + timestamp.
///   - TryConsume scans NEWEST-FIRST. The first entry whose action matches is removed
///     from the buffer regardless of whether it has expired; the method returns true
///     only if that removed entry was NOT expired (now - timestamp &lt;= expireSeconds).
///   - While scanning, any OTHER (non-matching) expired entries encountered are
///     opportunistically pruned from the buffer as a side effect.
/// </summary>
public sealed class InputBuffer
{
    public enum BufferedAction
    {
        LightAttack,
        HeavyAttack,
        Parry,
        Dodge,
        // Step 10 (Unity port) / Step 3.2 amendment: `interact` is bound in the Input
        // Actions asset since Phase 1 but was never wired past that. Buffered like the
        // other four discrete actions so InteractionResolver gets double-fire prevention
        // structurally via TryConsume rather than a hand-rolled guard.
        Interact
    }

    private struct Entry
    {
        public BufferedAction Action;
        public float Timestamp;
    }

    private readonly List<Entry> _entries = new List<Entry>();

    public void Push(BufferedAction action, float timestamp)
    {
        _entries.Add(new Entry { Action = action, Timestamp = timestamp });
    }

    public bool TryConsume(BufferedAction action, float now, float expireSeconds = 0.15f)
    {
        // Scan newest-first.
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i];
            bool expired = (now - entry.Timestamp) > expireSeconds;

            if (entry.Action == action)
            {
                // First match (scanning newest-first): remove it regardless of expiry,
                // and only report success if it was still valid.
                _entries.RemoveAt(i);
                return !expired;
            }

            if (expired)
            {
                // Opportunistic pruning of unrelated expired entries encountered
                // while scanning for the requested action.
                _entries.RemoveAt(i);
            }
        }

        return false;
    }
}
