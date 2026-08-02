using NUnit.Framework;
using UnityEngine;
using Unity.Cinemachine;

// CameraPitchDriver (charter first-person camera pivot, 2026-08-02 --
// docs/Tasks/2026-08-02-first-person-camera-and-weapon.md). Single responsibility: relay
// PlayerLook.PitchDegrees into CinemachinePanTilt.TiltAxis.Value every Update(). Lives on the
// CinemachineCamera GameObject, not the Player root, so it is not driven through
// TestReflectionUtil the way PlayerRoot's other Tick methods are wired -- Update() itself is
// invoked directly via reflection here, mirroring how PlayerRootTests reaches PlayerRoot's
// own private Update().
public class CameraPitchDriverTests
{
    private GameObject _camGo;
    private CinemachinePanTilt _panTilt;
    private CameraPitchDriver _driver;

    private GameObject _lookGo;
    private PlayerLook _look;

    [SetUp]
    public void SetUp()
    {
        // CinemachinePanTilt is a CinemachineComponentBase and expects to live alongside a
        // CinemachineCamera on the same GameObject (mirrors this project's own production
        // setup, wired via the cinemachine-set-aim MCP tool onto PlayerFollowCam).
        _camGo = new GameObject("PlayerFollowCam");
        _camGo.AddComponent<CinemachineCamera>();
        _panTilt = _camGo.AddComponent<CinemachinePanTilt>();
        _driver = _camGo.AddComponent<CameraPitchDriver>();

        _lookGo = new GameObject("PlayerRoot");
        _look = _lookGo.AddComponent<PlayerLook>();

        TestReflectionUtil.SetField(_driver, "playerLook", _look);
        TestReflectionUtil.SetField(_driver, "panTilt", _panTilt);
    }

    [TearDown]
    public void TearDown()
    {
        if (_camGo != null) Object.DestroyImmediate(_camGo);
        if (_lookGo != null) Object.DestroyImmediate(_lookGo);
    }

    [Test]
    public void Update_RelaysPlayerLookPitch_IntoPanTiltTiltAxisValue()
    {
        // Drive PlayerLook's real Tick() (not a hand-set field) so this test also proves the
        // two components compose correctly end-to-end, not just that reflection can poke a
        // number into a struct field.
        _look.Tick(new Vector2(0f, 10f)); // pitchSensitivity default 0.02 -> +0.2deg (lowered from 0.15 per 2026-08-02 user Play Mode feedback: sensitivity was too high)

        TestReflectionUtil.InvokeMethod(_driver, "Update");

        Assert.AreEqual(_look.PitchDegrees, _panTilt.TiltAxis.Value, 0.0001f);
        Assert.AreEqual(0.2f, _panTilt.TiltAxis.Value, 0.001f);
    }

    [Test]
    public void Update_CalledAfterFurtherLookTicks_RelaysUpdatedPitchEachTime()
    {
        _look.Tick(new Vector2(0f, 10f));
        TestReflectionUtil.InvokeMethod(_driver, "Update");
        float firstValue = _panTilt.TiltAxis.Value;

        _look.Tick(new Vector2(0f, 10f));
        TestReflectionUtil.InvokeMethod(_driver, "Update");
        float secondValue = _panTilt.TiltAxis.Value;

        Assert.Greater(secondValue, firstValue, "A second look tick with further +Y input should raise pitch further, and Update() should relay the new value, not a stale one.");
        Assert.AreEqual(_look.PitchDegrees, secondValue, 0.0001f);
    }

    [Test]
    public void Update_DoesNotOverwriteOtherTiltAxisFields()
    {
        // Guard against a regression where the whole InputAxis struct gets clobbered instead
        // of just its Value field (e.g. accidentally reassigning a default InputAxis).
        float rangeMinBefore = _panTilt.TiltAxis.Range.x;
        float rangeMaxBefore = _panTilt.TiltAxis.Range.y;

        _look.Tick(new Vector2(0f, 20f));
        TestReflectionUtil.InvokeMethod(_driver, "Update");

        Assert.AreEqual(rangeMinBefore, _panTilt.TiltAxis.Range.x, 0.0001f);
        Assert.AreEqual(rangeMaxBefore, _panTilt.TiltAxis.Range.y, 0.0001f);
    }

    [Test]
    public void Update_NullPlayerLook_DoesNotThrow_AndLeavesTiltAxisUnchanged()
    {
        TestReflectionUtil.SetField(_driver, "playerLook", null);
        float before = _panTilt.TiltAxis.Value;

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_driver, "Update"));

        Assert.AreEqual(before, _panTilt.TiltAxis.Value, 0.0001f);
    }

    [Test]
    public void Update_NullPanTilt_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_driver, "panTilt", null);
        _look.Tick(new Vector2(0f, 10f));

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_driver, "Update"));
    }

    [Test]
    public void Update_BothReferencesNull_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_driver, "playerLook", null);
        TestReflectionUtil.SetField(_driver, "panTilt", null);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_driver, "Update"));
    }
}
