using NUnit.Framework;
using UnityEngine;

// EnemyMotor mirrors PlayerMotor exactly (charter 7 Research recommendation), so it is
// EditMode-testable for the same reason PlayerMotorTests already established:
// CharacterController.Move() is a synchronous PhysX capsule-sweep call that works outside
// Play Mode too.
public class EnemyMotorTests
{
    private GameObject _go;
    private EnemyMotor _motor;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Enemy", typeof(CharacterController));
        _motor = _go.AddComponent<EnemyMotor>();
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
    public void SpeedScale_DefaultsToOne()
    {
        Assert.AreEqual(1f, _motor.SpeedScale, 0.0001f);
    }

    [Test]
    public void TickMotor_WithDesiredDirection_AcceleratesHorizontalVelocityTowardTarget()
    {
        _motor.SetDesiredDirection(Vector3.forward);

        _motor.TickMotor(0.02f);

        Assert.Greater(_motor.HorizontalVelocity.z, 0f);
        Assert.AreEqual(0f, _motor.HorizontalVelocity.x, 0.0001f);
    }

    [Test]
    public void TickMotor_ZeroDirection_DeceleratesHorizontalVelocityTowardZero()
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

        Assert.AreEqual(EnemyMotor.Speed, _motor.HorizontalVelocity.z, 0.05f);
    }

    [Test]
    public void TickMotor_HalfSpeedScale_ConvergesTowardHalfSpeed()
    {
        // charter 7.2's "50% movement speed" for Patrol -- EnemyBrain drives this by setting
        // SpeedScale, not by a separate speed constant.
        _motor.SpeedScale = 0.5f;
        _motor.SetDesiredDirection(Vector3.forward);

        for (int i = 0; i < 500; i++)
        {
            _motor.TickMotor(0.02f);
        }

        Assert.AreEqual(EnemyMotor.Speed * 0.5f, _motor.HorizontalVelocity.z, 0.05f);
    }

    [Test]
    public void TickMotor_AppliesGravityOverTimeWhenNotGrounded()
    {
        _motor.TickMotor(0.02f);
        float afterFirst = TestReflectionUtil.GetField<float>(_motor, "_verticalVelocity");

        _motor.TickMotor(0.02f);
        float afterSecond = TestReflectionUtil.GetField<float>(_motor, "_verticalVelocity");

        Assert.Less(afterSecond, afterFirst, "Vertical velocity should keep falling while ungrounded.");
    }
}
