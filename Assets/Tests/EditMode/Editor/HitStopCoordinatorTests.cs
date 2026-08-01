using NUnit.Framework;
using UnityEngine;

// HitStopCoordinator mutates the global Time.timeScale, same class of global state
// GameStateTests/MainMenuAutoStateTests/SandboxAutoPlayTests already have to guard against
// (Test Coverage Pass 1 precedent) -- TearDown always restores it to avoid order-dependent
// flakiness across the rest of the suite.
public class HitStopCoordinatorTests
{
    private GameObject _go;
    private HitStopCoordinator _hitStop;
    private float _previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        _previousTimeScale = Time.timeScale;
        _go = new GameObject("HitStop");
        _hitStop = _go.AddComponent<HitStopCoordinator>();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = _previousTimeScale;
        if (_go != null) Object.DestroyImmediate(_go);
    }

    [Test]
    public void Request_SetsTimeScaleToZero_AndBecomesActive()
    {
        _hitStop.Request(0.045f);

        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(_hitStop.IsActive);
    }

    [Test]
    public void Request_DurationBelowRange_IsClampedToMinimum()
    {
        _hitStop.Request(0.001f);

        // Ticking for exactly the charter minimum (0.03s) must fully drain a clamped request.
        _hitStop.Tick(0.03f);

        Assert.IsFalse(_hitStop.IsActive);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void Request_DurationAboveRange_IsClampedToMaximum()
    {
        _hitStop.Request(1f);

        // A tick short of the charter maximum (0.06s) must still leave it active.
        _hitStop.Tick(0.05f);

        Assert.IsTrue(_hitStop.IsActive);
        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void Request_NoArgument_UsesDefaultOfFortyFiveMs()
    {
        _hitStop.Request();

        _hitStop.Tick(0.0449f);
        Assert.IsTrue(_hitStop.IsActive, "0.045s default should not have drained yet at t=0.0449.");

        _hitStop.Tick(0.0002f);
        Assert.IsFalse(_hitStop.IsActive, "0.045s default should have drained by t=0.0451.");
    }

    [Test]
    public void Tick_CountsDownAndRestoresTimeScale_WhenReachingZero()
    {
        _hitStop.Request(0.06f);

        _hitStop.Tick(0.03f);
        Assert.IsTrue(_hitStop.IsActive, "Halfway through, should still be active.");
        Assert.AreEqual(0f, Time.timeScale, "Time.timeScale must stay 0 while active.");

        _hitStop.Tick(0.03f);
        Assert.IsFalse(_hitStop.IsActive, "Countdown should have fully drained.");
        Assert.AreEqual(1f, Time.timeScale, "Time.timeScale must be restored to 1 exactly when the countdown hits zero.");
    }

    [Test]
    public void Tick_Overshoot_DoesNotGoNegative_AndRestoresTimeScale()
    {
        _hitStop.Request(0.03f);

        _hitStop.Tick(10f); // wildly overshoots the countdown

        Assert.IsFalse(_hitStop.IsActive);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void Tick_WhileNotActive_IsANoOp_DoesNotForceTimeScaleTo1()
    {
        // Never requested -- Tick must not touch Time.timeScale at all (e.g. some other
        // system may have legitimately paused the game via GameState for an unrelated
        // reason; HitStopCoordinator must not fight that when it has nothing active).
        Time.timeScale = 0.5f;

        _hitStop.Tick(0.02f);

        Assert.IsFalse(_hitStop.IsActive);
        Assert.AreEqual(0.5f, Time.timeScale);
    }

    [Test]
    public void Request_WhileActive_WithLongerDuration_ExtendsCountdown()
    {
        _hitStop.Request(0.03f);

        // Drain most of it, then request a longer freeze -- must extend, not ignore.
        _hitStop.Tick(0.02f); // 0.01s remaining

        _hitStop.Request(0.06f); // longer than the 0.01s remaining -> should extend to 0.06s

        _hitStop.Tick(0.05f);
        Assert.IsTrue(_hitStop.IsActive, "Extended request should still be active after 0.05s (0.06s total).");

        _hitStop.Tick(0.01f);
        Assert.IsFalse(_hitStop.IsActive);
    }

    [Test]
    public void Request_WhileActive_WithShorterDuration_IsIgnored()
    {
        _hitStop.Request(0.06f);

        _hitStop.Request(0.03f); // shorter than the remaining 0.06s -> must be ignored

        _hitStop.Tick(0.05f);
        Assert.IsTrue(_hitStop.IsActive, "Original longer countdown must not have been shortened.");
    }
}
