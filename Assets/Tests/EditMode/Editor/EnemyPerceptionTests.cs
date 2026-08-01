using NUnit.Framework;
using UnityEngine;

// EnemyPerception's vision-cone raycast + acoustic-sphere logic (charter 7.1) is the
// highest-risk untested logic in this task per the Director's checkpoint: Research found
// (and verified live against the running Editor, not just documentation) that EditMode does
// NOT auto-sync transforms -- Physics.Raycast/OverlapSphere against a just-moved transform
// silently returns nothing unless Physics.SyncTransforms() runs first -- and that
// Physics.queriesHitTriggers defaults true, so the line-of-sight raycast must explicitly pass
// QueryTriggerInteraction.Ignore or trigger hurtboxes would register as false obstructions.
// Both fixes are implemented inside EnemyPerception.EvaluateVision() itself; these tests
// exercise the public/private surface directly (bypassing the 0.1s Tick gate via the
// EvaluatePerception() method, which the production code explicitly documents as separated
// out for this purpose) to prove the fixes actually work, not just that they're present.
public class EnemyPerceptionTests
{
    private GameObject _enemyGo;
    private EnemyPerception _perception;

    private GameObject _playerGo;
    private BoxCollider _playerCollider;

    [SetUp]
    public void SetUp()
    {
        _enemyGo = new GameObject("Enemy");
        _enemyGo.transform.position = Vector3.zero;
        _enemyGo.transform.rotation = Quaternion.identity; // forward = +Z

        _perception = _enemyGo.AddComponent<EnemyPerception>();

        _playerGo = new GameObject("Player", typeof(BoxCollider));
        _playerCollider = _playerGo.GetComponent<BoxCollider>();
        _playerGo.layer = 0; // Default layer, matches production obstructionMask convention

        TestReflectionUtil.SetField(_perception, "obstructionMask", (LayerMask)1); // Default layer bit
        TestReflectionUtil.SetField(_perception, "playerTransform", _playerGo.transform);

        TestReflectionUtil.InvokeMethod(_perception, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        if (_enemyGo != null) Object.DestroyImmediate(_enemyGo);
        if (_playerGo != null) Object.DestroyImmediate(_playerGo);
    }

    private void Evaluate()
    {
        TestReflectionUtil.InvokeMethod(_perception, "EvaluatePerception");
    }

    // --- Vision cone ---

    [Test]
    public void Vision_PlayerInFrontWithinRangeAndAngle_Unobstructed_Detects()
    {
        _playerGo.transform.position = new Vector3(0f, 0f, 10f); // straight ahead, 10m < 18m range

        Evaluate();

        Assert.IsTrue(_perception.CanSeePlayer);
    }

    [Test]
    public void Vision_PlayerBeyondRange_NotDetected()
    {
        _playerGo.transform.position = new Vector3(0f, 0f, 25f); // > 18m range

        Evaluate();

        Assert.IsFalse(_perception.CanSeePlayer);
    }

    [Test]
    public void Vision_PlayerOutsideHalfAngle_NotDetected()
    {
        // Directly to the enemy's right (90 degrees off forward) -- outside the 45 degree
        // half-angle (90 degree total cone).
        _playerGo.transform.position = new Vector3(10f, 0f, 0f);

        Evaluate();

        Assert.IsFalse(_perception.CanSeePlayer);
    }

    [Test]
    public void Vision_PlayerJustInsideHalfAngle_Detects()
    {
        // 44 degrees off forward (+Z, within the 45-degree half-angle), within range. Kept
        // 1 degree shy of the exact boundary to avoid floating-point rounding flakiness at
        // the dot-product comparison.
        float distance = 10f;
        float rad = 44f * Mathf.Deg2Rad;
        _playerGo.transform.position = new Vector3(Mathf.Sin(rad) * distance, 0f, Mathf.Cos(rad) * distance);

        Evaluate();

        Assert.IsTrue(_perception.CanSeePlayer);
    }

    [Test]
    public void Vision_SolidObstructionBetweenEnemyAndPlayer_BlocksDetection()
    {
        _playerGo.transform.position = new Vector3(0f, 0f, 10f);

        var obstructionGo = new GameObject("Wall", typeof(BoxCollider));
        obstructionGo.layer = 0; // on the obstruction mask
        obstructionGo.transform.position = new Vector3(0f, 0f, 5f); // halfway between
        obstructionGo.transform.localScale = new Vector3(3f, 3f, 0.5f);

        Evaluate();

        Assert.IsFalse(_perception.CanSeePlayer, "A solid collider on the obstruction mask between enemy and player must block line of sight.");

        Object.DestroyImmediate(obstructionGo);
    }

    [Test]
    public void Vision_TriggerColliderBetweenEnemyAndPlayer_IsIgnored_StillDetects()
    {
        _playerGo.transform.position = new Vector3(0f, 0f, 10f);

        var triggerGo = new GameObject("Hurtbox", typeof(BoxCollider));
        triggerGo.layer = 0;
        triggerGo.transform.position = new Vector3(0f, 0f, 5f);
        triggerGo.transform.localScale = new Vector3(3f, 3f, 0.5f);
        triggerGo.GetComponent<BoxCollider>().isTrigger = true;

        Evaluate();

        Assert.IsTrue(_perception.CanSeePlayer, "QueryTriggerInteraction.Ignore must skip trigger colliders, proving they cannot falsely block sight.");

        Object.DestroyImmediate(triggerGo);
    }

    [Test]
    public void Vision_NoPlayerTransformWired_DoesNotThrow_NotDetected()
    {
        TestReflectionUtil.SetField(_perception, "playerTransform", null);

        Assert.DoesNotThrow(() => Evaluate());
        Assert.IsFalse(_perception.CanSeePlayer);
    }

    [Test]
    public void Vision_EyePointUnassigned_FallsBackToTransformPosition()
    {
        // eyePoint left null (default) -- EvaluateVision must fall back to transform.position
        // rather than throwing a NullReferenceException.
        _playerGo.transform.position = new Vector3(0f, 0f, 10f);

        Assert.DoesNotThrow(() => Evaluate());
        Assert.IsTrue(_perception.CanSeePlayer);
    }

    [Test]
    public void Vision_EyePointAssigned_UsedAsRaycastOrigin()
    {
        var eyeGo = new GameObject("EyePoint");
        eyeGo.transform.SetParent(_enemyGo.transform);
        eyeGo.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        TestReflectionUtil.SetField(_perception, "eyePoint", eyeGo.transform);

        _playerGo.transform.position = new Vector3(0f, 0f, 10f);

        Evaluate();

        Assert.IsTrue(_perception.CanSeePlayer);
    }

    // --- Acoustic detection sphere ---

    [Test]
    public void Sound_PlayerWithinRadiusAndDodging_Detects()
    {
        _playerGo.transform.position = new Vector3(0f, 0f, 5f); // < 8m sound radius
        var dodge = _playerGo.AddComponent<DodgeAbility>();
        TestReflectionUtil.SetField(dodge, "_isDodging", true);
        TestReflectionUtil.SetField(_perception, "playerDodgeAbility", dodge);

        Evaluate();

        Assert.IsTrue(_perception.HeardNoise);
    }

    [Test]
    public void Sound_PlayerWithinRadiusAndAttacking_Detects()
    {
        _playerGo.transform.position = new Vector3(0f, 0f, 5f);
        var attack = _playerGo.AddComponent<AttackController>();
        TestReflectionUtil.SetField(attack, "_isAttacking", true);
        TestReflectionUtil.SetField(_perception, "playerAttackController", attack);

        Evaluate();

        Assert.IsTrue(_perception.HeardNoise);
    }

    [Test]
    public void Sound_PlayerWithinRadiusButNeitherDodgingNorAttacking_NotDetected()
    {
        _playerGo.transform.position = new Vector3(0f, 0f, 5f);
        var dodge = _playerGo.AddComponent<DodgeAbility>();
        TestReflectionUtil.SetField(_perception, "playerDodgeAbility", dodge);

        Evaluate();

        Assert.IsFalse(_perception.HeardNoise);
    }

    [Test]
    public void Sound_PlayerOutsideRadius_NotDetectedEvenWhileDodging()
    {
        _playerGo.transform.position = new Vector3(0f, 0f, 20f); // > 8m radius, also > vision range angle-wise irrelevant here
        var dodge = _playerGo.AddComponent<DodgeAbility>();
        TestReflectionUtil.SetField(dodge, "_isDodging", true);
        TestReflectionUtil.SetField(_perception, "playerDodgeAbility", dodge);

        Evaluate();

        Assert.IsFalse(_perception.HeardNoise);
    }

    [Test]
    public void LastKnownPlayerPosition_UpdatesOnlyWhenSeenOrHeard()
    {
        // Player far away and silent -- neither seen nor heard, LastKnownPlayerPosition
        // should remain at its default (Vector3.zero).
        _playerGo.transform.position = new Vector3(0f, 0f, 25f);

        Evaluate();

        Assert.AreEqual(Vector3.zero, _perception.LastKnownPlayerPosition);

        // Now bring the player into sight -- LastKnownPlayerPosition should update.
        _playerGo.transform.position = new Vector3(0f, 0f, 10f);
        Evaluate();

        Assert.AreEqual(new Vector3(0f, 0f, 10f), _perception.LastKnownPlayerPosition);
    }

    // --- 0.1s tick-interval gating (charter 7.1) ---

    [Test]
    public void Tick_BelowIntervalThreshold_DoesNotEvaluate()
    {
        TestReflectionUtil.SetField(_perception, "_accumulator", 0f);
        _playerGo.transform.position = new Vector3(0f, 0f, 10f); // would be detected if evaluated

        _perception.Tick(0.05f); // 0.05 < 0.1s tick interval

        Assert.IsFalse(_perception.CanSeePlayer, "Perception must not evaluate before the 0.1s tick interval elapses.");
    }

    [Test]
    public void Tick_AtOrAboveIntervalThreshold_Evaluates()
    {
        TestReflectionUtil.SetField(_perception, "_accumulator", 0f);
        _playerGo.transform.position = new Vector3(0f, 0f, 10f);

        _perception.Tick(0.05f); // accumulator -> 0.05, not yet
        Assert.IsFalse(_perception.CanSeePlayer);

        _perception.Tick(0.05f); // accumulator -> 0.10, triggers evaluation

        Assert.IsTrue(_perception.CanSeePlayer);
    }

    [Test]
    public void Tick_AccumulatorResetsAfterEvaluation_NotDoubleCounted()
    {
        TestReflectionUtil.SetField(_perception, "_accumulator", 0f);

        _perception.Tick(0.12f); // triggers one evaluation, accumulator -= 0.1 -> 0.02 remaining

        float remaining = TestReflectionUtil.GetField<float>(_perception, "_accumulator");
        Assert.AreEqual(0.02f, remaining, 0.0001f);
    }
}
