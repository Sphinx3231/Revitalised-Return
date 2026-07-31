using NUnit.Framework;
using UnityEngine;

public class DodgeAbilityTests
{
    private class MockStaminaSource : IStaminaSource
    {
        public bool ShouldSucceed;
        public int TrySpendCallCount;

        public bool TrySpend(float amount)
        {
            TrySpendCallCount++;
            return ShouldSucceed;
        }
    }

    private GameObject _go;
    private PlayerMotor _motor;
    private DodgeAbility _dodge;
    private MockStaminaSource _mockStamina;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Player", typeof(CharacterController));
        _motor = _go.AddComponent<PlayerMotor>();
        _dodge = _go.AddComponent<DodgeAbility>();

        // Awake wires _staminaSource = vitals (null, since no PlayerVitals field set) and
        // resolves motor via GetComponent. Invoke it, then override _staminaSource with our
        // mock directly (the serialized `vitals` field is typed PlayerVitals, not the
        // IStaminaSource interface, so a mock can't be assigned through it).
        TestReflectionUtil.InvokeMethod(_dodge, "Awake");

        _mockStamina = new MockStaminaSource { ShouldSucceed = true };
        TestReflectionUtil.SetField(_dodge, "_staminaSource", _mockStamina);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
        {
            Object.DestroyImmediate(_go);
        }
    }

    [Test]
    public void TryDodge_StaminaSourceRejects_Fails()
    {
        _mockStamina.ShouldSucceed = false;

        _dodge.TryDodge(Vector3.forward);

        Assert.IsFalse(_dodge.IsDodging);
    }

    [Test]
    public void TryDodge_StaminaSourceAccepts_SucceedsAndSetsDodgingState()
    {
        _dodge.TryDodge(Vector3.forward);

        Assert.IsTrue(_dodge.IsDodging);
        Assert.AreEqual(1, _mockStamina.TrySpendCallCount);
    }

    [Test]
    public void TryDodge_WhileAlreadyDodging_IsANoOp()
    {
        _dodge.TryDodge(Vector3.forward);
        Assert.AreEqual(1, _mockStamina.TrySpendCallCount);

        _dodge.TryDodge(Vector3.right);

        // Second call must not spend stamina again (guarded by the _isDodging check
        // before the stamina source is even consulted).
        Assert.AreEqual(1, _mockStamina.TrySpendCallCount);
    }

    [Test]
    public void TickDodge_IsInvulnerableOnlyWithinTheIFrameWindow()
    {
        _dodge.TryDodge(Vector3.forward);

        // Advance in small steps and sample IsInvulnerable at each point.
        float elapsed = 0f;
        const float dt = 0.01f;
        bool sawTrueBelowWindow = false;
        bool sawTrueAboveWindow = false;
        bool sawTrueWithinWindow = false;

        while (elapsed < 0.5f)
        {
            _dodge.TickDodge(dt);
            elapsed += dt;

            if (_dodge.IsInvulnerable)
            {
                if (elapsed < 0.15f) sawTrueBelowWindow = true;
                else if (elapsed > 0.35f + dt) sawTrueAboveWindow = true;
                else sawTrueWithinWindow = true;
            }
        }

        Assert.IsFalse(sawTrueBelowWindow, "Must not be invulnerable before t=0.15s.");
        Assert.IsTrue(sawTrueWithinWindow, "Must be invulnerable within [0.15, 0.35]s.");
        Assert.IsFalse(sawTrueAboveWindow, "Must not be invulnerable after t=0.35s.");
    }

    [Test]
    public void TickDodge_AtExactWindowBoundaries_IsInvulnerable()
    {
        _dodge.TryDodge(Vector3.forward);

        // Drive elapsed to exactly 0.15 via a single tick (elapsed starts at 0, checked
        // against the window BEFORE elapsed advances, i.e. the check uses pre-tick elapsed).
        // First tick with dt=0.15 checks elapsed=0 (not in window), then advances to 0.15.
        _dodge.TickDodge(0.15f);
        Assert.IsFalse(_dodge.IsInvulnerable);

        // Next tick checks elapsed=0.15 (in window: 0.15 <= 0.15 <= 0.35).
        _dodge.TickDodge(0.0001f);
        Assert.IsTrue(_dodge.IsInvulnerable);
    }

    [Test]
    public void TickDodge_PastTotalDuration_EndsDodgeAndClearsInvulnerabilityAndOverride()
    {
        _dodge.TryDodge(Vector3.forward);

        // Drive well past the 0.5s total duration in one tick.
        _dodge.TickDodge(0.6f);

        Assert.IsFalse(_dodge.IsDodging);
        Assert.IsFalse(_dodge.IsInvulnerable);
        Assert.IsNull(_motor.VelocityOverride);
    }

    [Test]
    public void TickDodge_WhileNotDodging_IsANoOp()
    {
        Assert.DoesNotThrow(() => _dodge.TickDodge(0.1f));
        Assert.IsFalse(_dodge.IsDodging);
    }

    [Test]
    public void TryDodge_NullMotor_Fails()
    {
        TestReflectionUtil.SetField(_dodge, "motor", null);

        _dodge.TryDodge(Vector3.forward);

        Assert.IsFalse(_dodge.IsDodging);
        Assert.AreEqual(1, _mockStamina.TrySpendCallCount, "Stamina should still be spent before the motor-null guard is reached.");
    }

    [Test]
    public void TickDodge_WithHurtboxCollider_TogglesEnabledWithInvulnerability()
    {
        var colliderGo = new GameObject("Hurtbox", typeof(BoxCollider));
        var collider = colliderGo.GetComponent<BoxCollider>();
        TestReflectionUtil.SetField(_dodge, "hurtboxCollider", collider);

        _dodge.TryDodge(Vector3.forward);

        // Tick 1: checks elapsed=0 (outside window) -> not invulnerable, collider stays enabled.
        _dodge.TickDodge(0.2f);
        Assert.IsFalse(_dodge.IsInvulnerable);
        Assert.IsTrue(collider.enabled);

        // Tick 2: checks elapsed=0.2 (inside [0.15, 0.35]) -> invulnerable, collider disabled.
        _dodge.TickDodge(0.2f);
        Assert.IsTrue(_dodge.IsInvulnerable);
        Assert.IsFalse(collider.enabled);

        // Tick 3: checks elapsed=0.4 (past the window) -> not invulnerable, collider re-enabled.
        _dodge.TickDodge(0.2f);
        Assert.IsFalse(_dodge.IsInvulnerable);
        Assert.IsTrue(collider.enabled);

        Object.DestroyImmediate(colliderGo);
    }

    [Test]
    public void TryDodge_ZeroDirection_UsesTransformForward()
    {
        _go.transform.rotation = Quaternion.identity;

        _dodge.TryDodge(Vector3.zero);
        _dodge.TickDodge(0.01f);

        Assert.IsTrue(_dodge.IsDodging);
        Assert.IsNotNull(_motor.VelocityOverride);

        Vector3 overrideVel = _motor.VelocityOverride.Value.normalized;
        AssertVector3Approximately(_go.transform.forward, overrideVel, 0.001f);
    }

    [Test]
    public void TryDodge_SetsInitialBurstVelocityMultiplier()
    {
        _dodge.TryDodge(Vector3.forward);
        _dodge.TickDodge(0f); // sample at elapsed=0 before any advance

        Assert.IsNotNull(_motor.VelocityOverride);
        float speed = _motor.VelocityOverride.Value.magnitude;
        Assert.AreEqual(PlayerMotor.Speed * 1.8f, speed, 0.01f);
    }

    private static void AssertVector3Approximately(Vector3 expected, Vector3 actual, float tolerance)
    {
        Assert.AreEqual(expected.x, actual.x, tolerance);
        Assert.AreEqual(expected.y, actual.y, tolerance);
        Assert.AreEqual(expected.z, actual.z, tolerance);
    }
}
