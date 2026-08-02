using NUnit.Framework;
using UnityEngine;

// PlayerLook (charter first-person camera pivot, 2026-08-02 --
// docs/Tasks/2026-08-02-first-person-camera-and-weapon.md). Yaw is applied directly to
// this component's own Transform; pitch is only accumulated/exposed, never applied to a
// Transform here (that's the Cinemachine PanTilt Aim component's job on the camera child).
public class PlayerLookTests
{
    private GameObject _rootGo;
    private PlayerLook _look;

    [SetUp]
    public void SetUp()
    {
        _rootGo = new GameObject("PlayerRoot");
        _look = _rootGo.AddComponent<PlayerLook>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_rootGo != null)
        {
            Object.DestroyImmediate(_rootGo);
        }
    }

    [Test]
    public void Tick_ZeroInput_NoYawOrPitchChange()
    {
        _look.Tick(Vector2.zero);

        Assert.AreEqual(0f, _look.YawDegrees, 0.0001f);
        Assert.AreEqual(0f, _look.PitchDegrees, 0.0001f);
        Assert.AreEqual(Quaternion.identity, _rootGo.transform.rotation);
    }

    [Test]
    public void Tick_PositiveXInput_AccumulatesYaw_AndAppliesToTransform()
    {
        TestReflectionUtil.SetField(_look, "yawSensitivity", 0.15f);

        _look.Tick(new Vector2(10f, 0f));

        Assert.AreEqual(1.5f, _look.YawDegrees, 0.001f);
        Assert.AreEqual(1.5f, _rootGo.transform.eulerAngles.y, 0.001f);
    }

    [Test]
    public void Tick_CalledTwice_YawAccumulatesAcrossTicks()
    {
        TestReflectionUtil.SetField(_look, "yawSensitivity", 1f);

        _look.Tick(new Vector2(5f, 0f));
        _look.Tick(new Vector2(3f, 0f));

        Assert.AreEqual(8f, _look.YawDegrees, 0.001f);
        Assert.AreEqual(8f, _rootGo.transform.eulerAngles.y, 0.001f);
    }

    [Test]
    public void Tick_PositiveYInput_AccumulatesPitch_WithoutTouchingTransformRotationDirectly()
    {
        TestReflectionUtil.SetField(_look, "pitchSensitivity", 0.2f);

        _look.Tick(new Vector2(0f, 10f));

        Assert.AreEqual(2f, _look.PitchDegrees, 0.001f);
        // Yaw-only transform: pitch must never leak into the root's own rotation.
        Assert.AreEqual(0f, _rootGo.transform.eulerAngles.y, 0.001f);
        Assert.AreEqual(0f, _rootGo.transform.eulerAngles.x, 0.001f);
    }

    [Test]
    public void Tick_PitchExceedsMax_ClampsTo80()
    {
        TestReflectionUtil.SetField(_look, "pitchSensitivity", 1f);

        _look.Tick(new Vector2(0f, 500f));

        Assert.AreEqual(80f, _look.PitchDegrees, 0.001f);
    }

    [Test]
    public void Tick_PitchBelowMin_ClampsToNegative80()
    {
        TestReflectionUtil.SetField(_look, "pitchSensitivity", 1f);

        _look.Tick(new Vector2(0f, -500f));

        Assert.AreEqual(-80f, _look.PitchDegrees, 0.001f);
    }

    [Test]
    public void Tick_InvertPitchTrue_FlipsPitchSign()
    {
        TestReflectionUtil.SetField(_look, "pitchSensitivity", 0.2f);
        TestReflectionUtil.SetField(_look, "invertPitch", true);

        _look.Tick(new Vector2(0f, 10f));

        Assert.AreEqual(-2f, _look.PitchDegrees, 0.001f);
    }

    [Test]
    public void Tick_NegativeXInput_YawsNegative()
    {
        TestReflectionUtil.SetField(_look, "yawSensitivity", 0.5f);

        _look.Tick(new Vector2(-4f, 0f));

        Assert.AreEqual(-2f, _look.YawDegrees, 0.001f);
    }
}
