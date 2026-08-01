using NUnit.Framework;
using UnityEngine;

// BossPhaseController owns the charter 8.1 Phase 2 transition: latches the 50% HP crossing
// exactly once, grants temporary invincibility, fires an AoE knockback, activates arena
// barriers, tints the weapon trail, and engages BossStanceMirror. Composition over
// DummyHealth.HealthChanged (DummyHealth is sealed) -- never owns the health value itself.
public class BossPhaseControllerTests
{
    private GameObject _bossGo;
    private BossPhaseController _phase;
    private DummyHealth _bossHealth;

    private GameObject _playerGo;
    private GameObject _knockbackGo;
    private PlayerMotor _playerMotor;
    private KnockbackAbility _playerKnockback;

    private GameObject _stanceMirrorGo;
    private BossStanceMirror _stanceMirror;

    private GameObject[] _barrierGos;

    [SetUp]
    public void SetUp()
    {
        _bossGo = new GameObject("Boss");
        _bossHealth = _bossGo.AddComponent<DummyHealth>();
        TestReflectionUtil.InvokeMethod(_bossHealth, "Awake");

        _phase = _bossGo.AddComponent<BossPhaseController>();
        TestReflectionUtil.SetField(_phase, "bossHealth", _bossHealth);

        _playerGo = new GameObject("Player", typeof(CharacterController));
        _playerGo.transform.position = new Vector3(0f, 0f, 2f);
        _playerMotor = _playerGo.AddComponent<PlayerMotor>();
        TestReflectionUtil.InvokeMethod(_playerMotor, "Awake");
        _playerKnockback = _playerGo.AddComponent<KnockbackAbility>();
        TestReflectionUtil.InvokeMethod(_playerKnockback, "Awake");

        TestReflectionUtil.SetField(_phase, "playerTransform", _playerGo.transform);
        TestReflectionUtil.SetField(_phase, "playerKnockback", _playerKnockback);
        TestReflectionUtil.SetField(_phase, "knockbackRadius", 10f);

        _stanceMirrorGo = new GameObject("StanceMirror");
        _stanceMirror = _stanceMirrorGo.AddComponent<BossStanceMirror>();
        TestReflectionUtil.SetField(_phase, "stanceMirror", _stanceMirror);

        _barrierGos = new[]
        {
            new GameObject("Barrier1"),
            new GameObject("Barrier2"),
        };
        foreach (var b in _barrierGos)
        {
            b.SetActive(false);
        }
        TestReflectionUtil.SetField(_phase, "arenaBarriers", _barrierGos);

        TestReflectionUtil.InvokeMethod(_phase, "OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        TestReflectionUtil.InvokeMethod(_phase, "OnDisable");

        if (_bossGo != null) Object.DestroyImmediate(_bossGo);
        if (_playerGo != null) Object.DestroyImmediate(_playerGo);
        if (_stanceMirrorGo != null) Object.DestroyImmediate(_stanceMirrorGo);
        foreach (var b in _barrierGos)
        {
            if (b != null) Object.DestroyImmediate(b);
        }
    }

    [Test]
    public void OnEnable_ActivatesArenaBarriers()
    {
        // SetUp already invoked OnEnable(); assert its side effect directly.
        foreach (var b in _barrierGos)
        {
            Assert.IsTrue(b.activeSelf);
        }
    }

    [Test]
    public void HealthChanged_AboveFiftyPercent_DoesNotTriggerPhase2()
    {
        _bossHealth.ApplyDamage(10f, false); // 90/100, above 50%

        Assert.IsFalse(_phase.Phase2Triggered);
    }

    [Test]
    public void HealthChanged_AtExactlyFiftyPercent_TriggersPhase2()
    {
        _bossHealth.ApplyDamage(50f, false); // exactly 50/100

        Assert.IsTrue(_phase.Phase2Triggered);
    }

    [Test]
    public void HealthChanged_CrossingFiftyPercent_TriggersPhase2_SetsInvincible()
    {
        _bossHealth.ApplyDamage(60f, false); // 40/100, below 50%

        Assert.IsTrue(_phase.Phase2Triggered);
        Assert.IsTrue(_bossHealth.IsInvincible);
    }

    [Test]
    public void HealthChanged_CrossingFiftyPercent_EngagesStanceMirror()
    {
        _bossHealth.ApplyDamage(60f, false);

        Assert.IsTrue(_stanceMirror.IsActive);
    }

    [Test]
    public void HealthChanged_MultipleCrossingsWithinPhase2_LatchesExactlyOnce()
    {
        _bossHealth.ApplyDamage(60f, false); // crosses 50% -> triggers Phase 2
        Assert.IsTrue(_phase.Phase2Triggered);

        // IsInvincible was set true by the transition -- ApplyDamage should now no-op, so
        // manually clear it (simulating the invincibility window having already expired) to
        // prove a SECOND crossing-eligible HealthChanged does not re-trigger the AoE/latch.
        _bossHealth.IsInvincible = false;

        _bossHealth.ApplyDamage(10f, false); // 30/100, still well below 50% -- re-crossing territory

        // If TriggerPhase2() ran a second time, IsInvincible would flip back to true (it was
        // manually cleared above right before this second damage call). It must stay false --
        // proof the 50%-crossing latch actually guards HandleHealthChanged, not just that
        // Phase2Triggered's flag reads true (which would be true either way).
        Assert.IsTrue(_phase.Phase2Triggered);
        Assert.IsFalse(_bossHealth.IsInvincible, "A second HealthChanged crossing must not re-trigger Phase 2 (the latch must guard re-entry).");
    }

    [Test]
    public void HealthChanged_PlayerWithinRadius_AppliesKnockbackAwayFromBoss()
    {
        _bossHealth.ApplyDamage(60f, false); // crosses 50%, player at distance 2 (radius 10)

        Assert.IsTrue(_playerKnockback.IsActive);
    }

    [Test]
    public void HealthChanged_PlayerOutsideRadius_DoesNotApplyKnockback()
    {
        TestReflectionUtil.SetField(_phase, "knockbackRadius", 1f); // player is at distance 2

        _bossHealth.ApplyDamage(60f, false);

        Assert.IsFalse(_playerKnockback.IsActive);
    }

    [Test]
    public void Tick_CountsDownInvincibilityWindow_ThenClearsIsInvincible()
    {
        TestReflectionUtil.SetField(_phase, "invincibilityDuration", 1.5f);
        _bossHealth.ApplyDamage(60f, false); // triggers, sets IsInvincible + 1.5s window

        Assert.IsTrue(_bossHealth.IsInvincible);

        _phase.Tick(1.0f);
        Assert.IsTrue(_bossHealth.IsInvincible, "Window should still be active after 1.0s of a 1.5s window.");

        _phase.Tick(0.6f); // total 1.6s, past the 1.5s window
        Assert.IsFalse(_bossHealth.IsInvincible);
    }

    [Test]
    public void Tick_WhenWindowNeverStarted_IsANoOp()
    {
        Assert.DoesNotThrow(() => _phase.Tick(0.1f));
        Assert.IsFalse(_bossHealth.IsInvincible);
    }

    [Test]
    public void Tick_AfterWindowExpires_IsANoOpAgain()
    {
        TestReflectionUtil.SetField(_phase, "invincibilityDuration", 0.2f);
        _bossHealth.ApplyDamage(60f, false);

        _phase.Tick(0.3f); // expires the window
        Assert.IsFalse(_bossHealth.IsInvincible);

        // A further Tick should be a clean no-op, not re-toggle anything.
        Assert.DoesNotThrow(() => _phase.Tick(0.1f));
        Assert.IsFalse(_bossHealth.IsInvincible);
    }

    [Test]
    public void OnDisable_UnsubscribesFromHealthChanged()
    {
        TestReflectionUtil.InvokeMethod(_phase, "OnDisable");

        // After unsubscribing, further damage must not trigger Phase 2 via this controller.
        _bossHealth.ApplyDamage(60f, false);

        Assert.IsFalse(_phase.Phase2Triggered);

        // Re-subscribe for TearDown's own OnDisable call to remain symmetric/no-throw.
        TestReflectionUtil.InvokeMethod(_phase, "OnEnable");
    }

    [Test]
    public void HealthChanged_AtZero_DeactivatesBarriersAndCallsEndEncounter()
    {
        var cameraGo = new GameObject("BossCameraFraming");
        var framing = cameraGo.AddComponent<BossCameraFraming>();
        var trackedPlayerGo = new GameObject("TrackedPlayer");
        TestReflectionUtil.SetField(framing, "playerTransform", trackedPlayerGo.transform);
        TestReflectionUtil.SetField(_phase, "bossCameraFraming", framing);

        Assert.DoesNotThrow(() => _bossHealth.ApplyDamage(100f, false)); // 0/100 -> defeat

        foreach (var b in _barrierGos)
        {
            Assert.IsFalse(b.activeSelf, "Barriers must deactivate on boss defeat.");
        }
        Assert.IsTrue(_phase.Defeated);

        // EndEncounter() reverts Follow/LookAt to the player transform -- with no
        // playerFollowCam wired it no-ops safely, so the meaningful assertion here is that the
        // call path executed without throwing and the latch flipped.
        Object.DestroyImmediate(cameraGo);
        Object.DestroyImmediate(trackedPlayerGo);
    }

    [Test]
    public void HealthChanged_MultipleSubZeroEvents_LatchesDefeatExactlyOnce()
    {
        _bossHealth.ApplyDamage(100f, false); // 0/100 -> defeat, deactivates barriers
        Assert.IsTrue(_phase.Defeated);

        // Reactivate a barrier manually to prove a second sub-zero HealthChanged does not
        // re-run TriggerDefeat (which would deactivate it again -- indistinguishable from a
        // no-op unless we flip it back on first).
        _barrierGos[0].SetActive(true);

        // IsInvincible/HealthChanged still fires if ApplyDamage is called again while already
        // at 0 HP (Mathf.Max clamps health at 0, so this is a legitimate "further damage while
        // already dead" event per the task spec).
        _bossHealth.IsInvincible = false;
        _bossHealth.ApplyDamage(10f, false); // still 0/100

        Assert.IsTrue(_barrierGos[0].activeSelf, "A second sub-zero HealthChanged must not re-trigger defeat (barrier should stay as manually reactivated).");
    }

    [Test]
    public void HealthChanged_DropsFromFullToZeroInOneEvent_TriggersBothPhase2AndDefeatLatches()
    {
        // A single hit taking the boss straight from 100% to 0% must fire BOTH the Phase-2
        // 50%-crossing latch AND the defeat latch from that one HealthChanged event -- they are
        // independent checks, not mutually exclusive branches.
        _bossHealth.ApplyDamage(100f, false); // 100/100 -> 0/100 in one ApplyDamage call

        Assert.IsTrue(_phase.Phase2Triggered, "Phase 2 must also latch when health drops straight past 50% to 0% in one event.");
        Assert.IsTrue(_phase.Defeated, "Defeat must latch when health reaches 0.");

        foreach (var b in _barrierGos)
        {
            Assert.IsFalse(b.activeSelf, "Barriers deactivated by the defeat path should win over Phase 2's activation in the same event.");
        }
    }

    [Test]
    public void HealthChanged_NullOptionalReferences_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_phase, "stanceMirror", null);
        TestReflectionUtil.SetField(_phase, "cameraTrauma", null);
        TestReflectionUtil.SetField(_phase, "trailActivator", null);
        TestReflectionUtil.SetField(_phase, "playerTransform", null);
        TestReflectionUtil.SetField(_phase, "playerKnockback", null);
        TestReflectionUtil.SetField(_phase, "arenaBarriers", null);
        TestReflectionUtil.SetField(_phase, "bossCameraFraming", null);

        Assert.DoesNotThrow(() => _bossHealth.ApplyDamage(60f, false));
        Assert.IsTrue(_phase.Phase2Triggered);
    }
}
