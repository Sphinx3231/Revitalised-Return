using NUnit.Framework;
using UnityEngine;

// KnockbackAbility mirrors DodgeAbility's VelocityOverride mechanism (charter 8.1's Phase-2
// AoE knockback), but restarts rather than no-ops on a re-trigger while already active
// (externally boss-triggered, not player-initiated -- see the class doc comment).
public class KnockbackAbilityTests
{
    private GameObject _go;
    private PlayerMotor _motor;
    private KnockbackAbility _knockback;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Player", typeof(CharacterController));
        _motor = _go.AddComponent<PlayerMotor>();
        TestReflectionUtil.InvokeMethod(_motor, "Awake");

        _knockback = _go.AddComponent<KnockbackAbility>();
        TestReflectionUtil.InvokeMethod(_knockback, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    [Test]
    public void ApplyKnockback_SetsActiveState()
    {
        _knockback.ApplyKnockback(Vector3.forward, 15f, 0.4f);
        Assert.IsTrue(_knockback.IsActive);
    }

    [Test]
    public void TickKnockback_WhileInactive_IsANoOp()
    {
        Assert.DoesNotThrow(() => _knockback.TickKnockback(0.1f));
        Assert.IsFalse(_knockback.IsActive);
        Assert.IsNull(_motor.VelocityOverride);
    }

    [Test]
    public void TickKnockback_AtStart_SetsFullForceVelocityOverride()
    {
        _knockback.ApplyKnockback(Vector3.forward, 15f, 0.4f);

        _knockback.TickKnockback(0f); // sample at elapsed=0 before any advance

        Assert.IsNotNull(_motor.VelocityOverride);
        Assert.AreEqual(15f, _motor.VelocityOverride.Value.magnitude, 0.01f);

        Vector3 direction = _motor.VelocityOverride.Value.normalized;
        Assert.AreEqual(Vector3.forward.x, direction.x, 0.001f);
        Assert.AreEqual(Vector3.forward.y, direction.y, 0.001f);
        Assert.AreEqual(Vector3.forward.z, direction.z, 0.001f);
    }

    [Test]
    public void TickKnockback_TapersLinearlyToZero()
    {
        _knockback.ApplyKnockback(Vector3.forward, 20f, 1.0f);

        _knockback.TickKnockback(0.5f); // checks elapsed=0 (full force), advances to 0.5

        // Now checks elapsed=0.5 (halfway through a 1.0s duration) -> half force.
        _knockback.TickKnockback(0.0001f);
        Assert.AreEqual(10f, _motor.VelocityOverride.Value.magnitude, 0.5f);
    }

    [Test]
    public void TickKnockback_PastDuration_ClearsOverrideAndDeactivates()
    {
        _knockback.ApplyKnockback(Vector3.forward, 15f, 0.4f);

        _knockback.TickKnockback(0.5f); // past the 0.4s duration in one tick

        Assert.IsFalse(_knockback.IsActive);
        Assert.IsNull(_motor.VelocityOverride);
    }

    [Test]
    public void ApplyKnockback_WhileAlreadyActive_Restarts()
    {
        _knockback.ApplyKnockback(Vector3.forward, 15f, 0.4f);
        _knockback.TickKnockback(0.3f); // most of the way through, force tapered down

        _knockback.ApplyKnockback(Vector3.right, 30f, 0.4f); // re-trigger takes over immediately
        _knockback.TickKnockback(0f); // sample at the fresh elapsed=0

        Assert.IsTrue(_knockback.IsActive);
        Assert.AreEqual(30f, _motor.VelocityOverride.Value.magnitude, 0.01f);

        Vector3 direction = _motor.VelocityOverride.Value.normalized;
        Assert.AreEqual(Vector3.right.x, direction.x, 0.001f);
        Assert.AreEqual(Vector3.right.z, direction.z, 0.001f);
    }

    [Test]
    public void ApplyKnockback_ZeroDirection_ResultsInZeroVector()
    {
        _knockback.ApplyKnockback(Vector3.zero, 15f, 0.4f);
        _knockback.TickKnockback(0f);

        Assert.AreEqual(Vector3.zero, _motor.VelocityOverride.Value);
    }

    [Test]
    public void ApplyKnockback_NullMotor_DoesNotThrowAndStaysInactive()
    {
        TestReflectionUtil.SetField(_knockback, "motor", null);

        Assert.DoesNotThrow(() => _knockback.ApplyKnockback(Vector3.forward, 15f, 0.4f));
        Assert.IsFalse(_knockback.IsActive);
    }

    [Test]
    public void ApplyKnockback_ZeroOrNegativeDuration_DoesNotDivideByZero()
    {
        Assert.DoesNotThrow(() => _knockback.ApplyKnockback(Vector3.forward, 15f, 0f));
        Assert.DoesNotThrow(() => _knockback.TickKnockback(0.001f));
    }
}
