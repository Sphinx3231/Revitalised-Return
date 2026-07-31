using NUnit.Framework;
using UnityEngine;

public class MeshLeanTests
{
    private GameObject _motorGo;
    private PlayerMotor _motor;
    private GameObject _meshGo;
    private MeshLean _lean;

    [SetUp]
    public void SetUp()
    {
        _motorGo = new GameObject("Player", typeof(CharacterController));
        _motor = _motorGo.AddComponent<PlayerMotor>();

        _meshGo = new GameObject("Mesh");
        _meshGo.transform.SetParent(_motorGo.transform);

        _lean = _motorGo.AddComponent<MeshLean>();
        TestReflectionUtil.SetField(_lean, "motor", _motor);
        TestReflectionUtil.SetField(_lean, "meshRoot", _meshGo.transform);
    }

    [TearDown]
    public void TearDown()
    {
        if (_motorGo != null)
        {
            Object.DestroyImmediate(_motorGo);
        }
    }

    private void SetMotorVelocity(Vector3 velocity)
    {
        TestReflectionUtil.SetField(_motor, "_horizontalVelocity", velocity);
    }

    [Test]
    public void TickLean_FirstCall_EstablishesBaselineWithoutRotating()
    {
        SetMotorVelocity(new Vector3(0f, 0f, 1f));

        _lean.TickLean(0.1f);

        Assert.AreEqual(0f, _meshGo.transform.localEulerAngles.z, 0.001f);
    }

    [Test]
    public void TickLean_NearZeroVelocity_NoOp()
    {
        SetMotorVelocity(Vector3.zero);

        Assert.DoesNotThrow(() => _lean.TickLean(0.1f));
        Assert.AreEqual(0f, _meshGo.transform.localEulerAngles.z, 0.001f);
    }

    [Test]
    public void TickLean_SharpTurn_ClampsToMaxLeanAngle()
    {
        // Baseline forward.
        SetMotorVelocity(new Vector3(0f, 0f, 1f));
        _lean.TickLean(0.1f);

        // Sharp 90-degree turn to the right over a small deltaTime -> large omega,
        // clamped to the +-5 degree max.
        SetMotorVelocity(new Vector3(1f, 0f, 0f));
        _lean.TickLean(0.1f);

        float signedZ = Mathf.DeltaAngle(0f, _meshGo.transform.localEulerAngles.z);
        Assert.AreEqual(-5f, signedZ, 0.01f);
    }

    [Test]
    public void TickLean_SmallTurn_ProducesUnclampedProportionalAngle()
    {
        SetMotorVelocity(new Vector3(0f, 0f, 1f));
        _lean.TickLean(1f); // establishes baseline yaw = atan2(0,1) = 0deg

        // A small yaw change over a full second -> small omega, well under the clamp.
        // velocity (sin(1deg), 0, cos(1deg)) approximates a 1-degree turn.
        float oneDegRad = 1f * Mathf.Deg2Rad;
        SetMotorVelocity(new Vector3(Mathf.Sin(oneDegRad), 0f, Mathf.Cos(oneDegRad)));
        _lean.TickLean(1f);

        // deltaAngle = 1deg, omega = 1deg/1s = 1, leanAngle = -clamp(1*0.1, -5, 5) = -0.1
        float signedZ = Mathf.DeltaAngle(0f, _meshGo.transform.localEulerAngles.z);
        Assert.AreEqual(-0.1f, signedZ, 0.01f);
    }

    [Test]
    public void Awake_MotorAlreadyAssigned_DoesNotOverwrite()
    {
        TestReflectionUtil.InvokeMethod(_lean, "Awake");
        Assert.AreSame(_motor, TestReflectionUtil.GetField<PlayerMotor>(_lean, "motor"));
    }

    [Test]
    public void Awake_MotorNull_ResolvesViaGetComponent()
    {
        TestReflectionUtil.SetField(_lean, "motor", null);

        TestReflectionUtil.InvokeMethod(_lean, "Awake");

        Assert.AreSame(_motor, TestReflectionUtil.GetField<PlayerMotor>(_lean, "motor"));
    }

    [Test]
    public void TickLean_NullMotorOrMeshRoot_NoOp()
    {
        TestReflectionUtil.SetField(_lean, "motor", null);
        Assert.DoesNotThrow(() => _lean.TickLean(0.1f));

        TestReflectionUtil.SetField(_lean, "motor", _motor);
        TestReflectionUtil.SetField(_lean, "meshRoot", null);
        Assert.DoesNotThrow(() => _lean.TickLean(0.1f));
    }
}
