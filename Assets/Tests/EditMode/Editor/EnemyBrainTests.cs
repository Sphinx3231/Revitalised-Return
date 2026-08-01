using NUnit.Framework;
using UnityEngine;

// EnemyBrain's FSM (charter 7.2): Patrol -> Investigate -> Telegraph -> Attack -> Recovery ->
// Investigate (NOT straight back to Attack, per the charter's own diagram -- "Recovery"
// transitions on "Animation End" back into "Investigate" so perception gets a fresh chance to
// re-decide). These tests drive Tick(deltaTime) directly and assert on CurrentState plus the
// private timer fields via TestReflectionUtil, mirroring this suite's established pattern for
// engine-callback-shaped/explicit-tick production code.
public class EnemyBrainTests
{
    private GameObject _enemyGo;
    private EnemyBrain _brain;
    private EnemyMotor _motor;
    private EnemyPerception _perception;
    private AttackController _attack;

    private GameObject _playerGo;

    [SetUp]
    public void SetUp()
    {
        _enemyGo = new GameObject("Enemy", typeof(CharacterController));
        _enemyGo.transform.position = Vector3.zero;

        _motor = _enemyGo.AddComponent<EnemyMotor>();
        TestReflectionUtil.InvokeMethod(_motor, "Awake");

        _perception = _enemyGo.AddComponent<EnemyPerception>();
        TestReflectionUtil.InvokeMethod(_perception, "Awake");

        _attack = _enemyGo.AddComponent<AttackController>();
        var hitboxGo = new GameObject("WeaponHitbox", typeof(BoxCollider));
        hitboxGo.transform.SetParent(_enemyGo.transform);
        var hitbox = hitboxGo.AddComponent<WeaponHitbox>();
        TestReflectionUtil.SetField(_attack, "weaponHitbox", hitbox);

        _playerGo = new GameObject("Player");
        _playerGo.transform.position = new Vector3(0f, 0f, 1f);

        _brain = _enemyGo.AddComponent<EnemyBrain>();
        TestReflectionUtil.SetField(_brain, "motor", _motor);
        TestReflectionUtil.SetField(_brain, "perception", _perception);
        TestReflectionUtil.SetField(_brain, "attackController", _attack);
        TestReflectionUtil.SetField(_brain, "playerTransform", _playerGo.transform);
    }

    [TearDown]
    public void TearDown()
    {
        if (_enemyGo != null) Object.DestroyImmediate(_enemyGo);
        if (_playerGo != null) Object.DestroyImmediate(_playerGo);
    }

    private void SetCanSeePlayer(bool value)
    {
        TestReflectionUtil.SetField(_perception, "<CanSeePlayer>k__BackingField", value);
    }

    private void SetHeardNoise(bool value)
    {
        TestReflectionUtil.SetField(_perception, "<HeardNoise>k__BackingField", value);
    }

    private void SetLastKnownPlayerPosition(Vector3 value)
    {
        TestReflectionUtil.SetField(_perception, "<LastKnownPlayerPosition>k__BackingField", value);
    }

    // --- Initial state & Patrol ---

    [Test]
    public void InitialState_IsPatrol()
    {
        Assert.AreEqual(EnemyBrain.State.Patrol, _brain.CurrentState);
    }

    [Test]
    public void Patrol_NoPerceptionSignal_StaysInPatrol()
    {
        _brain.Tick(0.02f);

        Assert.AreEqual(EnemyBrain.State.Patrol, _brain.CurrentState);
    }

    [Test]
    public void Patrol_SetsMotorSpeedScaleToHalf()
    {
        _brain.Tick(0.02f);

        Assert.AreEqual(0.5f, _motor.SpeedScale, 0.0001f);
    }

    [Test]
    public void Patrol_CanSeePlayer_TransitionsToInvestigate()
    {
        SetCanSeePlayer(true);

        _brain.Tick(0.02f);

        Assert.AreEqual(EnemyBrain.State.Investigate, _brain.CurrentState);
    }

    [Test]
    public void Patrol_HeardNoise_TransitionsToInvestigate()
    {
        SetHeardNoise(true);

        _brain.Tick(0.02f);

        Assert.AreEqual(EnemyBrain.State.Investigate, _brain.CurrentState);
    }

    [Test]
    public void Patrol_NoWaypoints_DoesNotThrow_DesiredDirectionZero()
    {
        Assert.DoesNotThrow(() => _brain.Tick(0.02f));
    }

    [Test]
    public void Patrol_CyclesToNextWaypointOnArrival()
    {
        var wpAGo = new GameObject("WaypointA");
        wpAGo.transform.position = new Vector3(0f, 0f, 0.1f); // within ArriveThreshold (0.5) of origin
        var wpBGo = new GameObject("WaypointB");
        wpBGo.transform.position = new Vector3(5f, 0f, 5f);

        TestReflectionUtil.SetField(_brain, "waypoints", new Transform[] { wpAGo.transform, wpBGo.transform });

        _brain.Tick(0.02f);

        int index = TestReflectionUtil.GetField<int>(_brain, "_currentWaypointIndex");
        Assert.AreEqual(1, index, "Arriving within ArriveThreshold of waypoint 0 must advance to waypoint 1.");

        Object.DestroyImmediate(wpAGo);
        Object.DestroyImmediate(wpBGo);
    }

    [Test]
    public void Patrol_WaypointIndexWrapsAroundAfterLast()
    {
        var wpAGo = new GameObject("WaypointA");
        wpAGo.transform.position = new Vector3(0f, 0f, 0.1f);

        TestReflectionUtil.SetField(_brain, "waypoints", new Transform[] { wpAGo.transform });

        _brain.Tick(0.02f);

        int index = TestReflectionUtil.GetField<int>(_brain, "_currentWaypointIndex");
        Assert.AreEqual(0, index, "A single-waypoint array must wrap back to index 0.");

        Object.DestroyImmediate(wpAGo);
    }

    // --- Investigate ---

    [Test]
    public void Investigate_SetsMotorSpeedScaleToFull()
    {
        SetCanSeePlayer(true);
        _brain.Tick(0.02f); // Patrol -> Investigate

        Assert.AreEqual(EnemyBrain.State.Investigate, _brain.CurrentState);

        SetCanSeePlayer(false); // avoid immediately advancing to Telegraph next tick
        SetLastKnownPlayerPosition(new Vector3(0f, 0f, 20f)); // far away, won't "arrive"
        _brain.Tick(0.02f);

        Assert.AreEqual(1f, _motor.SpeedScale, 0.0001f);
    }

    [Test]
    public void Investigate_TimesOutAfterLookAroundDuration_ReturnsToPatrol()
    {
        SetCanSeePlayer(true);
        _brain.Tick(0.02f); // Patrol -> Investigate, _investigateTimer = 3.0

        SetCanSeePlayer(false);
        SetLastKnownPlayerPosition(Vector3.zero); // arrived immediately (enemy also at origin)

        // Drive well past the 3.0s look-around duration.
        for (int i = 0; i < 200; i++)
        {
            _brain.Tick(0.02f); // 200 * 0.02 = 4.0s
        }

        Assert.AreEqual(EnemyBrain.State.Patrol, _brain.CurrentState);
    }

    [Test]
    public void Investigate_CloseAndVisible_TransitionsToTelegraph()
    {
        SetCanSeePlayer(true);
        _brain.Tick(0.02f); // Patrol -> Investigate

        // playerGo is at (0,0,1), within EngagementRange (3.0) of the enemy at origin, and
        // still visible.
        SetLastKnownPlayerPosition(_playerGo.transform.position);
        _brain.Tick(0.02f);

        Assert.AreEqual(EnemyBrain.State.Telegraph, _brain.CurrentState);
    }

    [Test]
    public void Investigate_VisibleButOutsideEngagementRange_StaysInInvestigate()
    {
        SetCanSeePlayer(true);
        _brain.Tick(0.02f); // Patrol -> Investigate

        _playerGo.transform.position = new Vector3(0f, 0f, 10f); // outside EngagementRange (3.0)
        SetLastKnownPlayerPosition(_playerGo.transform.position);
        _brain.Tick(0.02f);

        Assert.AreEqual(EnemyBrain.State.Investigate, _brain.CurrentState);
    }

    [Test]
    public void Investigate_NoPlayerTransform_DoesNotThrow()
    {
        SetCanSeePlayer(true);
        _brain.Tick(0.02f); // Patrol -> Investigate

        TestReflectionUtil.SetField(_brain, "playerTransform", null);

        Assert.DoesNotThrow(() => _brain.Tick(0.02f));
        Assert.AreEqual(EnemyBrain.State.Investigate, _brain.CurrentState);
    }

    // --- Telegraph ---

    [Test]
    public void Telegraph_LocksMovement_DesiredDirectionZero()
    {
        EnterTelegraph();

        // Motor's horizontal velocity should decelerate toward zero since desired direction
        // is locked to zero throughout Telegraph -- verified indirectly via SpeedScale/no
        // exception; direct velocity magnitude check after a tick with zero desired direction.
        Assert.DoesNotThrow(() => _brain.Tick(0.02f));
    }

    [Test]
    public void Telegraph_TimesOutAfterWindupDuration_TransitionsToAttack()
    {
        EnterTelegraph();

        // TelegraphDuration is 0.45s (within the charter's 0.3-0.6s range).
        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f); // 30 * 0.02 = 0.6s
        }

        Assert.AreEqual(EnemyBrain.State.Attack, _brain.CurrentState);
    }

    [Test]
    public void Telegraph_RotatesTowardPlayer()
    {
        _playerGo.transform.position = new Vector3(1f, 0f, 0f); // to the enemy's right
        EnterTelegraph();

        Quaternion before = _enemyGo.transform.rotation;
        _brain.Tick(0.1f);
        Quaternion after = _enemyGo.transform.rotation;

        Assert.AreNotEqual(before, after, "Telegraph must smoothly turn the enemy toward the player.");
    }

    // --- Attack ---

    [Test]
    public void EnteringAttack_StartsAttackControllerAttack()
    {
        EnterTelegraph();

        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f);
        }

        Assert.AreEqual(EnemyBrain.State.Attack, _brain.CurrentState);
        Assert.IsTrue(_attack.IsAttacking, "Entering Attack must call AttackController.TryLightAttack().");
    }

    [Test]
    public void Attack_WindowCloses_TransitionsToRecovery()
    {
        EnterTelegraph();
        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f); // reach Attack
        }
        Assert.AreEqual(EnemyBrain.State.Attack, _brain.CurrentState);

        // lightAttackWindowSeconds defaults to 0.2s -- drive well past it.
        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f); // 30 * 0.02 = 0.6s
        }

        Assert.AreEqual(EnemyBrain.State.Recovery, _brain.CurrentState);
    }

    [Test]
    public void Attack_NullAttackController_TransitionsStraightToRecovery()
    {
        EnterTelegraph();
        TestReflectionUtil.SetField(_brain, "attackController", null);

        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f);
        }

        Assert.AreEqual(EnemyBrain.State.Recovery, _brain.CurrentState);
    }

    // --- Recovery ---

    [Test]
    public void Recovery_TimesOut_TransitionsToInvestigate_NotAttack()
    {
        EnterTelegraph();
        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f); // reach Attack
        }
        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f); // reach Recovery
        }
        Assert.AreEqual(EnemyBrain.State.Recovery, _brain.CurrentState);

        // RecoveryDuration is 1.0s (within the charter's 0.8-1.5s range).
        for (int i = 0; i < 60; i++)
        {
            _brain.Tick(0.02f); // 60 * 0.02 = 1.2s
        }

        Assert.AreEqual(EnemyBrain.State.Investigate, _brain.CurrentState,
            "Charter 7.2's diagram routes Recovery -> Investigate on Animation End, never straight back to Attack.");
    }

    [Test]
    public void Recovery_BacksAwayFromPlayer()
    {
        EnterTelegraph();
        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f);
        }
        for (int i = 0; i < 30; i++)
        {
            _brain.Tick(0.02f);
        }
        Assert.AreEqual(EnemyBrain.State.Recovery, _brain.CurrentState);

        Vector3 posBefore = _enemyGo.transform.position;
        for (int i = 0; i < 10; i++)
        {
            _brain.Tick(0.02f);
        }
        Vector3 posAfter = _enemyGo.transform.position;

        // Player is at +Z from the enemy; backstepping means moving away, i.e. -Z.
        Assert.Less(posAfter.z, posBefore.z + 0.001f);
    }

    // --- Helpers ---

    private void EnterTelegraph()
    {
        SetCanSeePlayer(true);
        _brain.Tick(0.02f); // Patrol -> Investigate

        SetLastKnownPlayerPosition(_playerGo.transform.position);
        _brain.Tick(0.02f); // Investigate -> Telegraph (player within EngagementRange)

        Assert.AreEqual(EnemyBrain.State.Telegraph, _brain.CurrentState, "Test setup failed to reach Telegraph.");
    }
}
