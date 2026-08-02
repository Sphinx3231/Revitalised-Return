using NUnit.Framework;
using UnityEngine;
using Unity.Cinemachine;

// BossCameraFraming (charter 8.2's camera midpoint tracking) -- REWRITTEN for the 2026-08-02
// first-person camera pivot (docs/Tasks/2026-08-02-first-person-camera-and-weapon.md). The
// original Follow/LookAt-repoint behavior (StartEncounter/EndEncounter swapping
// PlayerFollowCam onto a CinemachineTargetGroup midpoint) was removed as incompatible with
// the new hard-locked FPS camera body -- see BossCameraFraming.cs's class doc comment for
// the full rationale. This file now asserts the explicit no-op boundary instead of the old
// Follow/LookAt swap, per the task's requirement to keep the scope boundary test-visible
// rather than just deleting the test file. EnsureTargetGroup/TargetGroup coverage (the part
// of this class that is UNCHANGED -- still a real, reusable midpoint calculation) is kept
// verbatim from the original suite.
public class BossCameraFramingTests
{
    private GameObject _framingGo;
    private BossCameraFraming _framing;

    private GameObject _playerGo;
    private GameObject _bossGo;

    [SetUp]
    public void SetUp()
    {
        _playerGo = new GameObject("Player");
        _playerGo.transform.position = new Vector3(0f, 0f, 0f);

        _bossGo = new GameObject("Boss");
        _bossGo.transform.position = new Vector3(10f, 0f, 0f);

        _framingGo = new GameObject("BossEncounterTargetGroup");
        _framing = _framingGo.AddComponent<BossCameraFraming>();

        TestReflectionUtil.SetField(_framing, "playerTransform", _playerGo.transform);
        TestReflectionUtil.SetField(_framing, "bossTransform", _bossGo.transform);

        _framing.EnsureTargetGroup();
    }

    [TearDown]
    public void TearDown()
    {
        if (_framingGo != null) Object.DestroyImmediate(_framingGo);
        if (_playerGo != null) Object.DestroyImmediate(_playerGo);
        if (_bossGo != null) Object.DestroyImmediate(_bossGo);
    }

    [Test]
    public void EnsureTargetGroup_CreatesTargetGroupComponent()
    {
        Assert.IsNotNull(_framing.TargetGroup);
    }

    [Test]
    public void EnsureTargetGroup_AddsBothMembers_WithWeightOneRadiusZero()
    {
        var group = _framing.TargetGroup;

        Assert.AreEqual(2, group.Targets.Count);

        int playerIndex = group.FindMember(_playerGo.transform);
        int bossIndex = group.FindMember(_bossGo.transform);

        Assert.GreaterOrEqual(playerIndex, 0);
        Assert.GreaterOrEqual(bossIndex, 0);

        Assert.AreEqual(1f, group.Targets[playerIndex].Weight);
        Assert.AreEqual(0f, group.Targets[playerIndex].Radius);
        Assert.AreEqual(1f, group.Targets[bossIndex].Weight);
        Assert.AreEqual(0f, group.Targets[bossIndex].Radius);
    }

    [Test]
    public void EnsureTargetGroup_CalledTwice_DoesNotDuplicateMembers()
    {
        _framing.EnsureTargetGroup();
        _framing.EnsureTargetGroup();

        Assert.AreEqual(2, _framing.TargetGroup.Targets.Count);
    }

    [Test]
    public void TargetGroup_SpherePosition_IsLiteralMidpoint_NoPlayModeNeeded()
    {
        // Research's live-verified finding: readable immediately in EditMode, no DoUpdate()
        // call needed from production code, no PlayMode required. Still true post-pivot --
        // this calculation is retained as a future recenter-system building block even
        // though nothing currently repoints a camera to it.
        Vector3 expectedMidpoint = (_playerGo.transform.position + _bossGo.transform.position) / 2f;
        Vector3 actual = _framing.TargetGroup.Sphere.position;

        Assert.AreEqual(expectedMidpoint.x, actual.x, 0.01f);
        Assert.AreEqual(expectedMidpoint.y, actual.y, 0.01f);
        Assert.AreEqual(expectedMidpoint.z, actual.z, 0.01f);
    }

    // --- Post-pivot boundary: StartEncounter/EndEncounter are explicit no-ops ---

    [Test]
    public void StartEncounter_DoesNotThrow_AndIsANoOp()
    {
        Assert.DoesNotThrow(() => _framing.StartEncounter());

        // No-op means the target group's members/positions are unaffected -- nothing about
        // player/boss transforms or the target group changes as a result of calling this.
        Assert.AreEqual(2, _framing.TargetGroup.Targets.Count);
    }

    [Test]
    public void EndEncounter_DoesNotThrow_AndIsANoOp()
    {
        _framing.StartEncounter();

        Assert.DoesNotThrow(() => _framing.EndEncounter());

        Assert.AreEqual(2, _framing.TargetGroup.Targets.Count);
    }

    [Test]
    public void StartEncounter_WithoutAnyWiring_DoesNotThrow()
    {
        var bareGo = new GameObject("BareFraming");
        var bareFraming = bareGo.AddComponent<BossCameraFraming>();

        Assert.DoesNotThrow(() => bareFraming.StartEncounter());
        Assert.DoesNotThrow(() => bareFraming.EndEncounter());

        Object.DestroyImmediate(bareGo);
    }
}
