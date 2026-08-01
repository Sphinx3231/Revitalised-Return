using NUnit.Framework;
using UnityEngine;

public class TrailActivatorTests
{
    private GameObject _go;
    private TrailActivator _activator;
    private TrailRenderer _trailRenderer;
    private GameObject _attackGo;
    private AttackController _attackController;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TrailActivatorHost", typeof(TrailRenderer));
        _trailRenderer = _go.GetComponent<TrailRenderer>();
        _trailRenderer.emitting = false;
        _activator = _go.AddComponent<TrailActivator>();

        _attackGo = new GameObject("Attacker");
        _attackController = _attackGo.AddComponent<AttackController>();

        TestReflectionUtil.SetField(_activator, "trailRenderer", _trailRenderer);
        TestReflectionUtil.SetField(_activator, "attackController", _attackController);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        if (_attackGo != null) Object.DestroyImmediate(_attackGo);
    }

    [Test]
    public void Tick_WhenAttacking_SetsEmittingTrue()
    {
        TestReflectionUtil.SetField(_attackController, "_isAttacking", true);

        _activator.Tick(0.016f);

        Assert.IsTrue(_trailRenderer.emitting);
    }

    [Test]
    public void Tick_WhenNotAttacking_SetsEmittingFalse()
    {
        _trailRenderer.emitting = true;
        TestReflectionUtil.SetField(_attackController, "_isAttacking", false);

        _activator.Tick(0.016f);

        Assert.IsFalse(_trailRenderer.emitting);
    }

    [Test]
    public void Tick_MirrorsStateChangesAcrossMultipleTicks()
    {
        TestReflectionUtil.SetField(_attackController, "_isAttacking", true);
        _activator.Tick(0.016f);
        Assert.IsTrue(_trailRenderer.emitting);

        TestReflectionUtil.SetField(_attackController, "_isAttacking", false);
        _activator.Tick(0.016f);
        Assert.IsFalse(_trailRenderer.emitting);
    }

    [Test]
    public void Tick_NullTrailRenderer_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_activator, "trailRenderer", null);

        Assert.DoesNotThrow(() => _activator.Tick(0.016f));
    }

    [Test]
    public void Tick_NullAttackController_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_activator, "attackController", null);

        Assert.DoesNotThrow(() => _activator.Tick(0.016f));
    }

    // Step 8: exposed so BossPhaseController can tint the trail's colors for the Phase-2
    // enrage visual cue without knowing anything about boss phases itself.
    [Test]
    public void TrailRenderer_ExposesWiredTrailRenderer()
    {
        Assert.AreSame(_trailRenderer, _activator.TrailRenderer);
    }
}
