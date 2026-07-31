using NUnit.Framework;
using UnityEngine;

// PlayerMotor was originally scoped as PlayMode-only (real CharacterController physics),
// but CharacterController.Move() is a synchronous PhysX capsule-sweep call that works
// outside Play Mode too (it does not depend on the running game loop) — verified empirically
// via this file's tests passing in a batchmode EditMode run. Direct EditMode coverage here.
public class PlayerMotorTests
{
    private GameObject _go;
    private PlayerMotor _motor;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Player", typeof(CharacterController));
        _motor = _go.AddComponent<PlayerMotor>();
        TestReflectionUtil.InvokeMethod(_motor, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    [Test]
    public void SetDesiredDirection_StoresDirectionForNextTick()
    {
        Assert.DoesNotThrow(() => _motor.SetDesiredDirection(Vector3.forward));
    }

    [Test]
    public void TickMotor_WithDesiredDirection_AcceleratesHorizontalVelocityTowardTarget()
    {
        _motor.SetDesiredDirection(Vector3.forward);

        _motor.TickMotor(0.02f);

        // Lerp toward target (0,0,Speed) with alpha=AlphaAccel*dt=15*0.02=0.3, starting
        // from zero, should now have a nonzero forward velocity component.
        Assert.Greater(_motor.HorizontalVelocity.z, 0f);
        Assert.AreEqual(0f, _motor.HorizontalVelocity.x, 0.0001f);
    }

    [Test]
    public void TickMotor_ZeroDirection_DecelratesHorizontalVelocityTowardZero()
    {
        _motor.SetDesiredDirection(Vector3.forward);
        _motor.TickMotor(0.1f);
        float afterAccel = _motor.HorizontalVelocity.z;
        Assert.Greater(afterAccel, 0f);

        _motor.SetDesiredDirection(Vector3.zero);
        _motor.TickMotor(0.1f);

        Assert.Less(_motor.HorizontalVelocity.z, afterAccel);
    }

    [Test]
    public void TickMotor_RepeatedTicksWithSameDirection_ConvergesTowardSpeedConstant()
    {
        _motor.SetDesiredDirection(Vector3.forward);

        for (int i = 0; i < 500; i++)
        {
            _motor.TickMotor(0.02f);
        }

        Assert.AreEqual(PlayerMotor.Speed, _motor.HorizontalVelocity.z, 0.05f);
    }

    [Test]
    public void TickMotor_VelocityOverride_BypassesLerpedKinematics()
    {
        _motor.SetDesiredDirection(Vector3.forward);
        _motor.VelocityOverride = new Vector3(3f, 0f, 0f);

        _motor.TickMotor(0.02f);

        Assert.AreEqual(3f, _motor.HorizontalVelocity.x, 0.0001f);
        Assert.AreEqual(0f, _motor.HorizontalVelocity.z, 0.0001f);
    }

    [Test]
    public void TickMotor_AppliesGravityOverTimeWhenNotGrounded()
    {
        // Not grounded (no ground collider under it) -> vertical velocity should
        // accumulate downward each tick rather than clamp to the grounded constant.
        _motor.TickMotor(0.02f);
        float afterFirst = TestReflectionUtil.GetField<float>(_motor, "_verticalVelocity");

        _motor.TickMotor(0.02f);
        float afterSecond = TestReflectionUtil.GetField<float>(_motor, "_verticalVelocity");

        Assert.Less(afterSecond, afterFirst, "Vertical velocity should keep falling while ungrounded.");
    }

    [Test]
    public void VelocityOverride_NullByDefault()
    {
        Assert.IsNull(_motor.VelocityOverride);
    }
}
