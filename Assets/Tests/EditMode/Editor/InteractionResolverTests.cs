using NUnit.Framework;
using UnityEngine;

// InteractionResolver's ranking formula (0.7 x camera-forward-dot + 0.3 x proximity, charter
// Step 10 locked spec) is exposed as a static pure function per Research's recommendation --
// covered directly with hand-constructed inputs, no physics required. Tick()'s OverlapSphere
// scan is covered separately following the exact EnemyPerceptionTests.cs pattern this codebase
// already established for EditMode physics-query testing: real GameObjects/Colliders in
// [SetUp], TestReflectionUtil for the private serialized fields, and an explicit reliance on
// InteractionResolver's own production-code Physics.SyncTransforms() call (not test scaffolding)
// to make the just-moved transforms visible to the query.
public class InteractionResolverTests
{
    // --- ScoreCandidate: pure function, no physics ---

    [Test]
    public void ScoreCandidate_DirectlyInFrontAtCamera_ScoresMaximum()
    {
        float score = InteractionResolver.ScoreCandidate(
            cameraForward: Vector3.forward,
            cameraPosition: Vector3.zero,
            candidatePosition: new Vector3(0f, 0f, 5f),
            maxRange: 10f);

        // dot = 1 (straight ahead), proximity = 1 - 5/10 = 0.5 -> 0.7*1 + 0.3*0.5 = 0.85
        Assert.AreEqual(0.85f, score, 0.0001f);
    }

    [Test]
    public void ScoreCandidate_DirectlyBehindCamera_ScoresNegativeDotTerm()
    {
        float score = InteractionResolver.ScoreCandidate(
            cameraForward: Vector3.forward,
            cameraPosition: Vector3.zero,
            candidatePosition: new Vector3(0f, 0f, -5f),
            maxRange: 10f);

        // dot = -1, proximity = 1 - 5/10 = 0.5 -> 0.7*-1 + 0.3*0.5 = -0.55
        Assert.AreEqual(-0.55f, score, 0.0001f);
    }

    [Test]
    public void ScoreCandidate_CloserCandidate_ScoresHigherThanFartherAtSameAngle()
    {
        float closeScore = InteractionResolver.ScoreCandidate(Vector3.forward, Vector3.zero, new Vector3(0f, 0f, 1f), 10f);
        float farScore = InteractionResolver.ScoreCandidate(Vector3.forward, Vector3.zero, new Vector3(0f, 0f, 9f), 10f);

        Assert.Greater(closeScore, farScore);
    }

    [Test]
    public void ScoreCandidate_CandidateAtCameraPosition_DegenerateCase_TreatsDotAsMaximallyForward()
    {
        float score = InteractionResolver.ScoreCandidate(Vector3.forward, Vector3.zero, Vector3.zero, 10f);

        // distance = 0 -> dot forced to 1, proximity = 1 - 0/10 = 1 -> 0.7 + 0.3 = 1.0
        Assert.AreEqual(1f, score, 0.0001f);
    }

    [Test]
    public void ScoreCandidate_ZeroMaxRange_ProximityTermIsZero_NoDivideByZero()
    {
        Assert.DoesNotThrow(() =>
        {
            float score = InteractionResolver.ScoreCandidate(Vector3.forward, Vector3.zero, new Vector3(0f, 0f, 5f), 0f);
            Assert.AreEqual(0.7f, score, 0.0001f); // dot=1, proximity=0
        });
    }

    [Test]
    public void ScoreCandidate_BeyondMaxRange_ProximityClampedToZero_NotNegative()
    {
        float score = InteractionResolver.ScoreCandidate(Vector3.forward, Vector3.zero, new Vector3(0f, 0f, 50f), 10f);

        // dot = 1, proximity clamped to 0 (not negative) -> 0.7*1 + 0.3*0 = 0.7
        Assert.AreEqual(0.7f, score, 0.0001f);
    }

    // --- Tick(): real OverlapSphere scan, mirrors EnemyPerceptionTests.cs's pattern ---

    private GameObject _resolverGo;
    private InteractionResolver _resolver;
    private GameObject _cameraGo;

    [SetUp]
    public void SetUp()
    {
        _resolverGo = new GameObject("Player");
        _resolverGo.transform.position = Vector3.zero;
        _resolver = _resolverGo.AddComponent<InteractionResolver>();

        _cameraGo = new GameObject("Camera");
        _cameraGo.transform.position = Vector3.zero;
        _cameraGo.transform.rotation = Quaternion.identity; // forward = +Z

        TestReflectionUtil.SetField(_resolver, "cameraTransform", _cameraGo.transform);
        TestReflectionUtil.SetField(_resolver, "interactionRadius", 10f);
        TestReflectionUtil.SetField(_resolver, "interactableLayerMask", (LayerMask)(1 << 12));
    }

    [TearDown]
    public void TearDown()
    {
        if (_resolverGo != null) Object.DestroyImmediate(_resolverGo);
        if (_cameraGo != null) Object.DestroyImmediate(_cameraGo);
    }

    private GameObject CreateInteractableCandidate(string name, Vector3 position)
    {
        var go = new GameObject(name, typeof(SphereCollider));
        go.layer = 12; // Interactable
        go.transform.position = position;
        go.GetComponent<SphereCollider>().isTrigger = true;
        go.AddComponent<Shrine>();
        return go;
    }

    [Test]
    public void Tick_SingleCandidateInRangeOnInteractableLayer_BecomesCurrentCandidate()
    {
        var candidateGo = CreateInteractableCandidate("Shrine", new Vector3(0f, 0f, 3f));

        _resolver.Tick(0.016f);

        Assert.IsNotNull(_resolver.CurrentCandidate);
        Assert.AreEqual(candidateGo.GetComponent<Shrine>(), _resolver.CurrentCandidate);

        Object.DestroyImmediate(candidateGo);
    }

    [Test]
    public void Tick_NoCandidatesInRange_CurrentCandidateIsNull()
    {
        _resolver.Tick(0.016f);

        Assert.IsNull(_resolver.CurrentCandidate);
    }

    [Test]
    public void Tick_MultipleCandidates_PicksHighestScoring()
    {
        // Directly in front, close -- should win.
        var best = CreateInteractableCandidate("Best", new Vector3(0f, 0f, 2f));
        // Off to the side, far -- should lose.
        var worse = CreateInteractableCandidate("Worse", new Vector3(9f, 0f, 1f));

        _resolver.Tick(0.016f);

        Assert.AreEqual(best.GetComponent<Shrine>(), _resolver.CurrentCandidate);

        Object.DestroyImmediate(best);
        Object.DestroyImmediate(worse);
    }

    [Test]
    public void Tick_CandidateOnWrongLayer_IsIgnored()
    {
        var go = new GameObject("NotInteractable", typeof(SphereCollider));
        go.layer = 0; // Default, not the Interactable layer
        go.transform.position = new Vector3(0f, 0f, 3f);
        go.GetComponent<SphereCollider>().isTrigger = true;
        go.AddComponent<Shrine>();

        _resolver.Tick(0.016f);

        Assert.IsNull(_resolver.CurrentCandidate);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Tick_CandidateThatCannotInteract_IsExcluded()
    {
        var go = new GameObject("LootedChest", typeof(SphereCollider));
        go.layer = 12;
        go.transform.position = new Vector3(0f, 0f, 3f);
        go.GetComponent<SphereCollider>().isTrigger = true;
        var chest = go.AddComponent<Chest>();
        TestReflectionUtil.SetField(chest, "_looted", true);

        _resolver.Tick(0.016f);

        Assert.IsNull(_resolver.CurrentCandidate, "A depleted Chest (CanInteract=false) must be excluded from candidacy.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Tick_CandidateBeyondInteractionRadius_IsExcluded()
    {
        TestReflectionUtil.SetField(_resolver, "interactionRadius", 2f);
        var go = CreateInteractableCandidate("TooFar", new Vector3(0f, 0f, 5f));

        _resolver.Tick(0.016f);

        Assert.IsNull(_resolver.CurrentCandidate);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Tick_NoCameraTransformWired_DoesNotThrow_NoCandidate()
    {
        TestReflectionUtil.SetField(_resolver, "cameraTransform", null);
        var go = CreateInteractableCandidate("Shrine", new Vector3(0f, 0f, 3f));

        Assert.DoesNotThrow(() => _resolver.Tick(0.016f));
        Assert.IsNull(_resolver.CurrentCandidate);

        Object.DestroyImmediate(go);
    }
}
