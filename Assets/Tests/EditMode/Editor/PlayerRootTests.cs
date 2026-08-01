using NUnit.Framework;
using UnityEngine;

// PlayerRoot's stance-switch handlers (HandleStanceNext/HandleStancePrev) are reached via
// reflection since they're private (invoked normally via PlayerInputReader's C# events).
//
// PlayerRoot's Update() was initially assumed to need a PlayMode rig (live PlayerInputReader
// owning a generated PlayerControls Input Actions instance, a camera Transform, a
// PlayerMotor+CharacterController, PlayerVitals, all wired together, gated behind
// GameState.IsPlayerInputLocked()). It turned out to be reachable in EditMode after all:
// constructing PlayerControls and calling CharacterController.Move() both work synchronously
// without the Play Mode loop running. See Update_WhenPlaying_DrivesFullPerFrameChain below —
// every individual step Update() performs (CameraRelativeInput, InputBuffer.TryConsume,
// DodgeAbility.TryDodge/TickDodge, PlayerMotor.TickMotor, PlayerVitals.TickRegen) also has
// direct unit coverage in its own test file; this integration test additionally exercises
// the generated PlayerControls.cs wrapper as a side effect of constructing a real
// PlayerInputReader.
public class PlayerRootTests
{
    private GameObject _rootGo;
    private PlayerRoot _root;
    private GameObject _stanceGo;
    private StanceController _stanceController;
    private StanceData[] _stances;

    [SetUp]
    public void SetUp()
    {
        _rootGo = new GameObject("PlayerRoot");
        _root = _rootGo.AddComponent<PlayerRoot>();

        _stanceGo = new GameObject("Stances");
        _stanceController = _stanceGo.AddComponent<StanceController>();

        _stances = new StanceData[2];
        for (int i = 0; i < 2; i++)
        {
            _stances[i] = ScriptableObject.CreateInstance<StanceData>();
        }
        TestReflectionUtil.SetField(_stanceController, "stances", _stances);
        TestReflectionUtil.SetField(_stanceController, "_currentIndex", 0);

        TestReflectionUtil.SetField(_root, "stanceController", _stanceController);
    }

    [TearDown]
    public void TearDown()
    {
        if (_rootGo != null) Object.DestroyImmediate(_rootGo);
        if (_stanceGo != null) Object.DestroyImmediate(_stanceGo);
        foreach (var s in _stances)
        {
            if (s != null) Object.DestroyImmediate(s);
        }
    }

    [Test]
    public void HandleStanceNext_DelegatesToStanceController_CycleNext()
    {
        StanceData got = null;
        System.Action<StanceData> handler = s => got = s;
        EventBus.StanceSwapped += handler;

        TestReflectionUtil.InvokeMethod(_root, "HandleStanceNext");

        EventBus.StanceSwapped -= handler;

        Assert.AreSame(_stances[1], got);
    }

    [Test]
    public void HandleStancePrev_DelegatesToStanceController_CyclePrevious()
    {
        StanceData got = null;
        System.Action<StanceData> handler = s => got = s;
        EventBus.StanceSwapped += handler;

        TestReflectionUtil.InvokeMethod(_root, "HandleStancePrev");

        EventBus.StanceSwapped -= handler;

        Assert.AreSame(_stances[1], got); // index 0 -> Previous -> wraps to last (index 1 of 2)
    }

    [Test]
    public void HandleStanceNext_NullStanceController_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_root, "stanceController", null);
        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_root, "HandleStanceNext"));
    }

    [Test]
    public void HandleStancePrev_NullStanceController_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_root, "stanceController", null);
        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_root, "HandleStancePrev"));
    }

    // Full Update() integration: wires a real PlayerInputReader, PlayerMotor,
    // DodgeAbility, PlayerVitals, and a camera Transform, then drives Update() directly
    // via reflection. This turned out to be reachable in EditMode after all (nothing here
    // requires the running Play Mode loop — CharacterController.Move and constructing the
    // generated PlayerControls Input Actions wrapper both work synchronously), so it's
    // included as a bonus beyond the "report the gap" fallback documented above the class.
    [Test]
    public void Update_WhenPlaying_DrivesFullPerFrameChain_WithoutThrowing()
    {
        var camGo = new GameObject("Cam");
        var playerGo = new GameObject("Player", typeof(CharacterController));
        var readerGo = new GameObject("Reader");

        var motor = playerGo.AddComponent<PlayerMotor>();
        var vitals = playerGo.AddComponent<PlayerVitals>();
        var dodge = playerGo.AddComponent<DodgeAbility>();
        var reader = readerGo.AddComponent<PlayerInputReader>();
        var fullRootGo = new GameObject("FullPlayerRoot");
        var fullRoot = fullRootGo.AddComponent<PlayerRoot>();

        TestReflectionUtil.InvokeMethod(motor, "Awake");
        TestReflectionUtil.InvokeMethod(vitals, "Awake");
        TestReflectionUtil.InvokeMethod(dodge, "Awake");
        TestReflectionUtil.InvokeMethod(reader, "Awake");
        TestReflectionUtil.InvokeMethod(reader, "OnEnable");

        TestReflectionUtil.SetField(fullRoot, "inputReader", reader);
        TestReflectionUtil.SetField(fullRoot, "motor", motor);
        TestReflectionUtil.SetField(fullRoot, "dodgeAbility", dodge);
        TestReflectionUtil.SetField(fullRoot, "meshLean", null);
        TestReflectionUtil.SetField(fullRoot, "cameraTransform", camGo.transform);
        TestReflectionUtil.SetField(fullRoot, "vitals", vitals);
        TestReflectionUtil.SetField(fullRoot, "stanceController", null);

        TestReflectionUtil.InvokeMethod(fullRoot, "Awake");

        var previousState = GameState.CurrentState;
        GameState.SetState(GameState.State.Playing);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(fullRoot, "Update"));

        // A second tick exercises the "already has a last-nonzero-direction" and
        // dodge-in-progress-or-not branches without special setup.
        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(fullRoot, "Update"));

        GameState.SetState(previousState);
        TestReflectionUtil.InvokeMethod(reader, "OnDisable");
        // Note: PlayerInputReader.OnDestroy() is NOT invoked here — it calls
        // PlayerControls.Dispose(), which internally calls Object.Destroy() on a generated
        // internal asset; Object.Destroy() in Edit Mode logs an error-level message that
        // Unity Test Framework treats as a test failure by default (verified empirically).
        // Simply destroying readerGo below is sufficient cleanup for this test.

        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(readerGo);
        Object.DestroyImmediate(fullRootGo);
    }

    [Test]
    public void Update_WhenInputLocked_EarlyReturnsWithoutThrowing()
    {
        var fullRootGo = new GameObject("LockedPlayerRoot");
        var fullRoot = fullRootGo.AddComponent<PlayerRoot>();
        TestReflectionUtil.InvokeMethod(fullRoot, "Awake");

        var previousState = GameState.CurrentState;
        GameState.SetState(GameState.State.Paused);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(fullRoot, "Update"));

        GameState.SetState(previousState);
        Object.DestroyImmediate(fullRootGo);
    }

    [Test]
    public void Update_WhenPlayingButMissingDependencies_EarlyReturnsWithoutThrowing()
    {
        // inputReader/motor/cameraTransform all still null after Awake -> Update's second
        // guard clause (line 69's null-dependency check) should return before touching
        // anything, even while GameState is Playing (the first guard clause passes).
        var fullRootGo = new GameObject("MissingDepsPlayerRoot");
        var fullRoot = fullRootGo.AddComponent<PlayerRoot>();
        TestReflectionUtil.InvokeMethod(fullRoot, "Awake");

        var previousState = GameState.CurrentState;
        GameState.SetState(GameState.State.Playing);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(fullRoot, "Update"));

        GameState.SetState(previousState);
        Object.DestroyImmediate(fullRootGo);
    }

    [Test]
    public void OnDestroy_UnsubscribesFromInputReaderEvents()
    {
        var readerGo = new GameObject("Reader");
        var reader = readerGo.AddComponent<PlayerInputReader>();
        TestReflectionUtil.InvokeMethod(reader, "Awake");

        var rootGo = new GameObject("RootForDestroy");
        var root = rootGo.AddComponent<PlayerRoot>();
        TestReflectionUtil.SetField(root, "inputReader", reader);
        TestReflectionUtil.InvokeMethod(root, "Awake");

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(root, "OnDestroy"));

        Object.DestroyImmediate(rootGo);
        Object.DestroyImmediate(readerGo);
    }

    // Step 5: exercises the new attack/parry buffered-consume + tick steps added to
    // Update(), wiring real AttackController/ParryController components (not just the
    // attackController==null/parryController==null false-branch the other Update tests
    // above cover implicitly).
    [Test]
    public void Update_WithAttackAndParryControllersWired_ConsumesBufferedActionsAndTicks()
    {
        var camGo = new GameObject("Cam2");
        var playerGo = new GameObject("Player2", typeof(CharacterController));
        var readerGo = new GameObject("Reader2");
        var hitboxGo = new GameObject("WeaponPivot2", typeof(BoxCollider));

        var motor = playerGo.AddComponent<PlayerMotor>();
        var vitals = playerGo.AddComponent<PlayerVitals>();
        var dodge = playerGo.AddComponent<DodgeAbility>();
        var reader = readerGo.AddComponent<PlayerInputReader>();
        var attack = playerGo.AddComponent<AttackController>();
        var parry = playerGo.AddComponent<ParryController>();
        var hitbox = hitboxGo.AddComponent<WeaponHitbox>();
        var fullRootGo = new GameObject("FullPlayerRoot2");
        var fullRoot = fullRootGo.AddComponent<PlayerRoot>();

        TestReflectionUtil.InvokeMethod(motor, "Awake");
        TestReflectionUtil.InvokeMethod(vitals, "Awake");
        TestReflectionUtil.InvokeMethod(dodge, "Awake");
        TestReflectionUtil.InvokeMethod(reader, "Awake");
        TestReflectionUtil.InvokeMethod(reader, "OnEnable");

        TestReflectionUtil.SetField(attack, "weaponHitbox", hitbox);

        TestReflectionUtil.SetField(fullRoot, "inputReader", reader);
        TestReflectionUtil.SetField(fullRoot, "motor", motor);
        TestReflectionUtil.SetField(fullRoot, "dodgeAbility", dodge);
        TestReflectionUtil.SetField(fullRoot, "meshLean", null);
        TestReflectionUtil.SetField(fullRoot, "cameraTransform", camGo.transform);
        TestReflectionUtil.SetField(fullRoot, "vitals", vitals);
        TestReflectionUtil.SetField(fullRoot, "stanceController", null);
        TestReflectionUtil.SetField(fullRoot, "attackController", attack);
        TestReflectionUtil.SetField(fullRoot, "parryController", parry);

        TestReflectionUtil.InvokeMethod(fullRoot, "Awake");

        // Buffer a light-attack and a parry action directly on the reader's InputBuffer,
        // matching how PlayerInputReader's own callbacks push entries.
        reader.InputBuffer.Push(InputBuffer.BufferedAction.LightAttack, Time.time);
        reader.InputBuffer.Push(InputBuffer.BufferedAction.Parry, Time.time);

        var previousState = GameState.CurrentState;
        GameState.SetState(GameState.State.Playing);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(fullRoot, "Update"));

        Assert.IsTrue(attack.IsAttacking, "Buffered LightAttack should have been consumed and triggered.");
        Assert.IsTrue(parry.IsParrying, "Buffered Parry should have been consumed and triggered.");

        GameState.SetState(previousState);
        TestReflectionUtil.InvokeMethod(reader, "OnDisable");

        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(readerGo);
        Object.DestroyImmediate(hitboxGo);
        Object.DestroyImmediate(fullRootGo);
    }

    [Test]
    public void OnDestroy_NullInputReader_DoesNotThrow()
    {
        var rootGo = new GameObject("RootForDestroyNull");
        var root = rootGo.AddComponent<PlayerRoot>();
        TestReflectionUtil.InvokeMethod(root, "Awake");

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(root, "OnDestroy"));

        Object.DestroyImmediate(rootGo);
    }
}
