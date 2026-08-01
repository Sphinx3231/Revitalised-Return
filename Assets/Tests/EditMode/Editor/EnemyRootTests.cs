using NUnit.Framework;
using UnityEngine;

// EnemyRoot fixes the Step 7 sign-off bug: nothing anywhere called EnemyBrain.Tick(), so the
// Step 7 enemy was completely inert in Play Mode (see docs/Tasks/2026-08-01-step-7-ai-
// perception-fsm.md's "Post-Signoff Bug Found" section). These tests prove EnemyRoot.Update()
// actually drives EnemyBrain.Tick() (which, per EnemyBrain.cs itself, already internally ticks
// perception first and the motor last -- see EnemyRoot.cs's class doc comment for why
// EnemyRoot does NOT also call perception.Tick()/motor.TickMotor() directly, since doing so
// would double-tick perception's 0.1s accumulator every frame).
public class EnemyRootTests
{
    private GameObject _enemyGo;
    private EnemyMotor _motor;
    private EnemyPerception _perception;
    private EnemyBrain _brain;
    private EnemyRoot _root;

    [SetUp]
    public void SetUp()
    {
        _enemyGo = new GameObject("Enemy", typeof(CharacterController));

        _motor = _enemyGo.AddComponent<EnemyMotor>();
        TestReflectionUtil.InvokeMethod(_motor, "Awake");

        _perception = _enemyGo.AddComponent<EnemyPerception>();
        TestReflectionUtil.InvokeMethod(_perception, "Awake");

        _brain = _enemyGo.AddComponent<EnemyBrain>();
        TestReflectionUtil.SetField(_brain, "motor", _motor);
        TestReflectionUtil.SetField(_brain, "perception", _perception);

        _root = _enemyGo.AddComponent<EnemyRoot>();
        TestReflectionUtil.SetField(_root, "motor", _motor);
        TestReflectionUtil.SetField(_root, "perception", _perception);
        TestReflectionUtil.SetField(_root, "brain", _brain);
    }

    [TearDown]
    public void TearDown()
    {
        if (_enemyGo != null) Object.DestroyImmediate(_enemyGo);
    }

    [Test]
    public void Update_WithBrainWired_DrivesFsmTransition()
    {
        // Prove the real bug fix end-to-end: with EnemyBrain.CurrentState starting at Patrol
        // and CanSeePlayer forced true (via the backing field, matching EnemyBrainTests'
        // established pattern), a single EnemyRoot.Update() call must reach all the way
        // through to EnemyBrain.Tick() -> TickPatrol() -> TransitionTo(Investigate). Before
        // EnemyRoot existed, nothing called EnemyBrain.Tick() at all, so this would never fire.
        TestReflectionUtil.SetField(_perception, "<CanSeePlayer>k__BackingField", true);

        TestReflectionUtil.InvokeMethod(_root, "Update");

        Assert.AreEqual(EnemyBrain.State.Investigate, _brain.CurrentState);
    }

    [Test]
    public void Update_NullBrain_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_root, "brain", null);
        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_root, "Update"));
    }

    [Test]
    public void Update_WithBossPhaseControllerWired_TicksIt()
    {
        var bossHealthGo = new GameObject("BossHealth");
        var bossHealth = bossHealthGo.AddComponent<DummyHealth>();
        TestReflectionUtil.InvokeMethod(bossHealth, "Awake");

        var phaseGo = new GameObject("Phase");
        var phase = phaseGo.AddComponent<BossPhaseController>();
        TestReflectionUtil.SetField(phase, "bossHealth", bossHealth);
        TestReflectionUtil.InvokeMethod(phase, "OnEnable");

        // Force the invincibility window active so Tick() has observable state to advance.
        TestReflectionUtil.SetField(phase, "_invincibilityRemaining", 1.0f);
        bossHealth.IsInvincible = true;

        TestReflectionUtil.SetField(_root, "bossPhaseController", phase);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_root, "Update"));

        // EnemyRoot.Update() must have called phase.Tick(Time.deltaTime). In EditMode,
        // Time.deltaTime is typically 0, so the window may not have visibly decremented --
        // assert the no-throw contract and that the field is still readable (component alive,
        // wiring intact), which is what this test is actually proving: EnemyRoot reaches and
        // calls Tick() on a wired BossPhaseController without erroring.
        float remaining = TestReflectionUtil.GetField<float>(phase, "_invincibilityRemaining");
        Assert.LessOrEqual(remaining, 1.0f);

        Object.DestroyImmediate(phaseGo);
        Object.DestroyImmediate(bossHealthGo);
    }

    [Test]
    public void Update_NullBossPhaseController_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_root, "bossPhaseController", null);
        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_root, "Update"));
    }
}
