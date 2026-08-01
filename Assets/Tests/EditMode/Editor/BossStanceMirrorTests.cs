using NUnit.Framework;
using UnityEngine;

// BossStanceMirror is intentionally trivial (Approach's explicit note): it returns null
// (stance-neutral) until BossPhaseController engages it at the Phase 2 transition (charter
// 8.1's stance-mirroring enrage detail), then returns the player's real StanceController.
public class BossStanceMirrorTests
{
    private GameObject _mirrorGo;
    private BossStanceMirror _mirror;

    private GameObject _stanceGo;
    private StanceController _stanceController;
    private StanceData _stance;

    [SetUp]
    public void SetUp()
    {
        _mirrorGo = new GameObject("BossStanceMirror");
        _mirror = _mirrorGo.AddComponent<BossStanceMirror>();

        _stanceGo = new GameObject("PlayerStances");
        _stanceController = _stanceGo.AddComponent<StanceController>();
        _stance = ScriptableObject.CreateInstance<StanceData>();
        TestReflectionUtil.SetField(_stanceController, "stances", new[] { _stance });
        TestReflectionUtil.SetField(_stanceController, "_currentIndex", 0);

        TestReflectionUtil.SetField(_mirror, "playerStanceController", _stanceController);
    }

    [TearDown]
    public void TearDown()
    {
        if (_mirrorGo != null) Object.DestroyImmediate(_mirrorGo);
        if (_stanceGo != null) Object.DestroyImmediate(_stanceGo);
        if (_stance != null) Object.DestroyImmediate(_stance);
    }

    [Test]
    public void CurrentStance_WhenInactive_ReturnsNull()
    {
        Assert.IsFalse(_mirror.IsActive);
        Assert.IsNull(_mirror.CurrentStance);
    }

    [Test]
    public void CurrentStance_WhenActive_ReturnsPlayerStance()
    {
        _mirror.SetActive(true);

        Assert.IsTrue(_mirror.IsActive);
        Assert.AreSame(_stance, _mirror.CurrentStance);
    }

    [Test]
    public void CurrentStance_WhenActiveButNoPlayerStanceControllerWired_ReturnsNull()
    {
        TestReflectionUtil.SetField(_mirror, "playerStanceController", null);
        _mirror.SetActive(true);

        Assert.IsNull(_mirror.CurrentStance);
    }

    [Test]
    public void SetActive_False_RevertsToNull()
    {
        _mirror.SetActive(true);
        Assert.AreSame(_stance, _mirror.CurrentStance);

        _mirror.SetActive(false);

        Assert.IsNull(_mirror.CurrentStance);
    }

    [Test]
    public void BossStanceMirror_ImplementsIStanceSource()
    {
        IStanceSource source = _mirror;
        Assert.IsNotNull(source);

        _mirror.SetActive(true);
        Assert.AreSame(_stance, source.CurrentStance);
    }
}
