using NUnit.Framework;
using UnityEngine;
using Unity.Cinemachine;

// BossCameraFraming (charter 8.2's camera midpoint tracking). Research's live-verified finding
// (logged in docs/Tasks/2026-08-01-step-8-boss-mechanics.md): a CinemachineTargetGroup built
// in EditMode returns correct Sphere.position immediately after AddMember, no PlayMode needed
// -- CinemachineTargetGroup.Sphere's getter itself calls DoUpdate() when its cached frame is
// stale, confirmed by reading the package source (Library/PackageCache/com.unity.cinemachine.../
// Runtime/Behaviours/CinemachineTargetGroup.cs). Member weight=1/radius=0 per Research's
// verified numeric finding (a nonzero radius biases Sphere toward the larger-radius member).
public class BossCameraFramingTests
{
    private GameObject _framingGo;
    private BossCameraFraming _framing;

    private GameObject _camGo;
    private CinemachineCamera _cam;

    private GameObject _playerGo;
    private GameObject _bossGo;

    [SetUp]
    public void SetUp()
    {
        _camGo = new GameObject("PlayerFollowCam");
        _cam = _camGo.AddComponent<CinemachineCamera>();

        _playerGo = new GameObject("Player");
        _playerGo.transform.position = new Vector3(0f, 0f, 0f);

        _bossGo = new GameObject("Boss");
        _bossGo.transform.position = new Vector3(10f, 0f, 0f);

        _framingGo = new GameObject("BossEncounterTargetGroup");
        _framing = _framingGo.AddComponent<BossCameraFraming>();

        TestReflectionUtil.SetField(_framing, "playerFollowCam", _cam);
        TestReflectionUtil.SetField(_framing, "playerTransform", _playerGo.transform);
        TestReflectionUtil.SetField(_framing, "bossTransform", _bossGo.transform);

        _framing.EnsureTargetGroup();
    }

    [TearDown]
    public void TearDown()
    {
        if (_framingGo != null) Object.DestroyImmediate(_framingGo);
        if (_camGo != null) Object.DestroyImmediate(_camGo);
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
        // call needed from production code, no PlayMode required.
        Vector3 expectedMidpoint = (_playerGo.transform.position + _bossGo.transform.position) / 2f;
        Vector3 actual = _framing.TargetGroup.Sphere.position;

        Assert.AreEqual(expectedMidpoint.x, actual.x, 0.01f);
        Assert.AreEqual(expectedMidpoint.y, actual.y, 0.01f);
        Assert.AreEqual(expectedMidpoint.z, actual.z, 0.01f);
    }

    [Test]
    public void StartEncounter_SwapsFollowAndLookAtToTargetGroup()
    {
        _framing.StartEncounter();

        Assert.AreSame(_framing.TargetGroup.transform, _cam.Follow);
        Assert.AreSame(_framing.TargetGroup.transform, _cam.LookAt);
    }

    [Test]
    public void EndEncounter_RestoresFollowAndLookAtToPlayer()
    {
        _framing.StartEncounter();
        _framing.EndEncounter();

        Assert.AreSame(_playerGo.transform, _cam.Follow);
        Assert.AreSame(_playerGo.transform, _cam.LookAt);
    }

    [Test]
    public void StartEncounter_NullPlayerFollowCam_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_framing, "playerFollowCam", null);
        Assert.DoesNotThrow(() => _framing.StartEncounter());
    }

    [Test]
    public void EndEncounter_NullPlayerFollowCam_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_framing, "playerFollowCam", null);
        Assert.DoesNotThrow(() => _framing.EndEncounter());
    }
}
