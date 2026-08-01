using NUnit.Framework;
using UnityEngine;

// JuiceCoordinator's own private Update() is invoked via reflection, same precedent as
// PlayerRootTests -- this is the "single per-frame driver" role for the Juice-engine's
// Tick-based components, mirroring PlayerRoot's own established pattern.
public class JuiceCoordinatorTests
{
    private GameObject _go;
    private JuiceCoordinator _coordinator;

    private GameObject _hitStopGo;
    private HitStopCoordinator _hitStopCoordinator;

    private GameObject _cameraTraumaGo;
    private CameraTrauma _cameraTrauma;

    private float _previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        _previousTimeScale = Time.timeScale;

        _go = new GameObject("JuiceCoordinator");
        _coordinator = _go.AddComponent<JuiceCoordinator>();

        _hitStopGo = new GameObject("HitStop");
        _hitStopCoordinator = _hitStopGo.AddComponent<HitStopCoordinator>();

        _cameraTraumaGo = new GameObject("CameraTrauma");
        _cameraTrauma = _cameraTraumaGo.AddComponent<CameraTrauma>();

        TestReflectionUtil.SetField(_coordinator, "hitStopCoordinator", _hitStopCoordinator);
        TestReflectionUtil.SetField(_coordinator, "cameraTrauma", _cameraTrauma);
        TestReflectionUtil.SetField(_coordinator, "hitFlash", null);
        TestReflectionUtil.SetField(_coordinator, "trailActivator", null);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = _previousTimeScale;
        if (_go != null) Object.DestroyImmediate(_go);
        if (_hitStopGo != null) Object.DestroyImmediate(_hitStopGo);
        if (_cameraTraumaGo != null) Object.DestroyImmediate(_cameraTraumaGo);
    }

    [Test]
    public void OnEnable_EntityDamaged_RequestsHitStopAndAddsTrauma()
    {
        TestReflectionUtil.InvokeMethod(_coordinator, "OnEnable");

        EventBus.RaiseEntityDamaged(_go.transform, 10f, false);

        Assert.IsTrue(_hitStopCoordinator.IsActive, "EntityDamaged must trigger a hit-stop request.");
        Assert.AreEqual(0.3f, _cameraTrauma.Trauma, 0.0001f, "EntityDamaged must add 0.3 trauma.");

        TestReflectionUtil.InvokeMethod(_coordinator, "OnDisable");
    }

    [Test]
    public void OnEnable_ParryExecuted_RequestsHitStopAndAddsMoreTrauma()
    {
        TestReflectionUtil.InvokeMethod(_coordinator, "OnEnable");

        EventBus.RaiseParryExecuted(_go.transform, _go.transform);

        Assert.IsTrue(_hitStopCoordinator.IsActive, "ParryExecuted must trigger a hit-stop request.");
        Assert.AreEqual(0.5f, _cameraTrauma.Trauma, 0.0001f, "ParryExecuted must add 0.5 trauma.");

        TestReflectionUtil.InvokeMethod(_coordinator, "OnDisable");
    }

    [Test]
    public void OnDisable_UnsubscribesFromEventBus_NoLongerReacts()
    {
        TestReflectionUtil.InvokeMethod(_coordinator, "OnEnable");
        TestReflectionUtil.InvokeMethod(_coordinator, "OnDisable");

        EventBus.RaiseEntityDamaged(_go.transform, 10f, false);

        Assert.IsFalse(_hitStopCoordinator.IsActive, "After OnDisable, EntityDamaged must no longer reach the coordinator.");
        Assert.AreEqual(0f, _cameraTrauma.Trauma, 0.0001f);
    }

    [Test]
    public void Update_TicksHitStopAndCameraTrauma_WithoutThrowing()
    {
        TestReflectionUtil.InvokeMethod(_coordinator, "OnEnable");
        EventBus.RaiseEntityDamaged(_go.transform, 10f, false);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_coordinator, "Update"));

        TestReflectionUtil.InvokeMethod(_coordinator, "OnDisable");
    }

    [Test]
    public void Update_WithAllReferencesNull_DoesNotThrow()
    {
        var bareGo = new GameObject("BareCoordinator");
        var bareCoordinator = bareGo.AddComponent<JuiceCoordinator>();

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(bareCoordinator, "Update"));

        Object.DestroyImmediate(bareGo);
    }
}
