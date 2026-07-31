using NUnit.Framework;

// Pure C#, no Unity deps — highest-value coverage per Research. Semantics verified against
// InputBuffer.cs's own doc comment (ported verbatim from the legacy Godot consume_action,
// see docs/Tasks/2026-07-31-player-base-character.md Research Findings item 6).
public class InputBufferTests
{
    [Test]
    public void PushThenTryConsume_BasicRoundTrip_Succeeds()
    {
        var buffer = new InputBuffer();
        buffer.Push(InputBuffer.BufferedAction.Dodge, 0f);

        bool result = buffer.TryConsume(InputBuffer.BufferedAction.Dodge, 0.05f);

        Assert.IsTrue(result);
    }

    [Test]
    public void TryConsume_PastExpiryWindow_FailsAndRemovesEntry()
    {
        var buffer = new InputBuffer();
        buffer.Push(InputBuffer.BufferedAction.Dodge, 0f);

        // Default expire is 0.15s; 0.2s elapsed is past it.
        bool firstAttempt = buffer.TryConsume(InputBuffer.BufferedAction.Dodge, 0.2f);
        Assert.IsFalse(firstAttempt);

        // The expired-but-matched entry was still removed on the first scan — a second
        // consume attempt for the same action must also fail since it's gone.
        bool secondAttempt = buffer.TryConsume(InputBuffer.BufferedAction.Dodge, 0.2f);
        Assert.IsFalse(secondAttempt);
    }

    [Test]
    public void TryConsume_NoMatchingEntry_ReturnsFalse()
    {
        var buffer = new InputBuffer();
        Assert.IsFalse(buffer.TryConsume(InputBuffer.BufferedAction.Parry, 0f));
    }

    [Test]
    public void TryConsume_ScansNewestFirst()
    {
        var buffer = new InputBuffer();
        // Two Dodge entries: an old (now-expired) one and a fresh one pushed after it.
        buffer.Push(InputBuffer.BufferedAction.Dodge, 0f);
        buffer.Push(InputBuffer.BufferedAction.Dodge, 0.5f);

        // now = 0.55: the entry at t=0.5 is still valid (0.05s old); the one at t=0 is
        // long expired. Newest-first scanning must find and succeed on the t=0.5 entry.
        bool result = buffer.TryConsume(InputBuffer.BufferedAction.Dodge, 0.55f);
        Assert.IsTrue(result);

        // Only one Dodge entry should remain consumed; the old expired one is still in
        // the buffer only if it wasn't reached in the newest-first scan (it wasn't,
        // since the match returns immediately) — so a second consume should still find
        // the old, expired entry and fail (but remove it).
        bool second = buffer.TryConsume(InputBuffer.BufferedAction.Dodge, 0.55f);
        Assert.IsFalse(second);

        bool third = buffer.TryConsume(InputBuffer.BufferedAction.Dodge, 0.55f);
        Assert.IsFalse(third);
    }

    [Test]
    public void TryConsume_OpportunisticallyPrunesOtherExpiredEntriesDuringScan()
    {
        var buffer = new InputBuffer();
        // "Newest-first" scanning is by insertion (list) order, not by timestamp value —
        // Push() lets a caller record any timestamp regardless of call order. Pushed FIRST
        // (older list position, scanned LAST) but given a fresh-enough timestamp to still
        // be valid when consumed:
        buffer.Push(InputBuffer.BufferedAction.HeavyAttack, 0.9f);
        // Pushed SECOND (newer list position, scanned FIRST), but given an old timestamp
        // so it's already expired by consume time — this is the entry that must be
        // opportunistically pruned while the scan passes over it looking for HeavyAttack.
        buffer.Push(InputBuffer.BufferedAction.Parry, 0f);

        bool heavyResult = buffer.TryConsume(InputBuffer.BufferedAction.HeavyAttack, 1f);
        Assert.IsTrue(heavyResult);

        // Confirm the Parry entry is gone: consuming it "in the past" (now=0.05, which
        // would have made its original t=0 timestamp still valid) finds nothing, since it
        // no longer exists in the buffer.
        bool parryResult = buffer.TryConsume(InputBuffer.BufferedAction.Parry, 0.05f);
        Assert.IsFalse(parryResult);
    }

    [Test]
    public void DifferentActionTypes_DoNotInterfereWithEachOther()
    {
        var buffer = new InputBuffer();
        buffer.Push(InputBuffer.BufferedAction.LightAttack, 0f);
        buffer.Push(InputBuffer.BufferedAction.HeavyAttack, 0f);
        buffer.Push(InputBuffer.BufferedAction.Parry, 0f);
        buffer.Push(InputBuffer.BufferedAction.Dodge, 0f);

        Assert.IsTrue(buffer.TryConsume(InputBuffer.BufferedAction.HeavyAttack, 0.01f));
        Assert.IsTrue(buffer.TryConsume(InputBuffer.BufferedAction.LightAttack, 0.01f));
        Assert.IsTrue(buffer.TryConsume(InputBuffer.BufferedAction.Dodge, 0.01f));
        Assert.IsTrue(buffer.TryConsume(InputBuffer.BufferedAction.Parry, 0.01f));
    }

    [Test]
    public void TryConsume_CustomExpireSeconds_Respected()
    {
        var buffer = new InputBuffer();
        buffer.Push(InputBuffer.BufferedAction.LightAttack, 0f);

        // Well within a generous custom expiry window.
        bool result = buffer.TryConsume(InputBuffer.BufferedAction.LightAttack, 1f, expireSeconds: 5f);
        Assert.IsTrue(result);
    }
}
