using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameStateTests
{
    [TearDown]
    public void TearDown()
    {
        // GameState.CurrentState is a static field; Cursor/Time.timeScale are real global
        // engine state. Reset both after every test so test order never causes flakiness.
        GameState.SetState(GameState.State.Playing);
        Time.timeScale = 1f;
    }

    [TestCase(GameState.State.Initializing)]
    [TestCase(GameState.State.MainMenu)]
    [TestCase(GameState.State.Playing)]
    [TestCase(GameState.State.Paused)]
    [TestCase(GameState.State.Dialogue)]
    [TestCase(GameState.State.Cutscene)]
    [TestCase(GameState.State.GameOver)]
    public void SetState_SetsCurrentStateForEveryEnumValue(GameState.State state)
    {
        GameState.SetState(state);
        Assert.AreEqual(state, GameState.CurrentState);
    }

    [Test]
    public void IsPlayerInputLocked_FalseOnlyWhenPlaying()
    {
        GameState.SetState(GameState.State.Playing);
        Assert.IsFalse(GameState.IsPlayerInputLocked());
    }

    [TestCase(GameState.State.Initializing)]
    [TestCase(GameState.State.MainMenu)]
    [TestCase(GameState.State.Paused)]
    [TestCase(GameState.State.Dialogue)]
    [TestCase(GameState.State.Cutscene)]
    [TestCase(GameState.State.GameOver)]
    public void IsPlayerInputLocked_TrueForAllNonPlayingStates(GameState.State state)
    {
        GameState.SetState(state);
        Assert.IsTrue(GameState.IsPlayerInputLocked());
    }

    // Note: Cursor.lockState/visible are NOT asserted here. In a -nographics batchmode
    // run there is no window for the OS cursor to actually lock to, so Cursor.lockState
    // silently stays CursorLockMode.None regardless of what SetState assigns to it —
    // verified empirically (these assertions failed under batchmode even though the
    // assignment line in GameState.SetState does execute). Time.timeScale has no such
    // headless-environment caveat and is asserted directly.
    [Test]
    public void SetState_Playing_UnpausesTime()
    {
        GameState.SetState(GameState.State.Playing);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [TestCase(GameState.State.Paused)]
    [TestCase(GameState.State.MainMenu)]
    [TestCase(GameState.State.GameOver)]
    public void SetState_PauseLikeStates_FreezesTime(GameState.State state)
    {
        GameState.SetState(state);
        Assert.AreEqual(0f, Time.timeScale);
    }

    [TestCase(GameState.State.Dialogue)]
    [TestCase(GameState.State.Cutscene)]
    public void SetState_DialogueLikeStates_KeepsTimeRunning(GameState.State state)
    {
        GameState.SetState(state);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void Awake_FirstInstance_SetsInstanceBeforePlayModeOnlyCallThrows()
    {
        // GameState.Awake() sets Instance = this BEFORE calling DontDestroyOnLoad, and
        // DontDestroyOnLoad is documented by Unity as unusable outside Play Mode — invoking
        // Awake() directly from an EditMode test throws for that specific line. Verified
        // empirically: the exception is real, and the preceding assignment still ran.
        var previousInstance = GameState.Instance;

        var go = new GameObject("GameStateTestA");
        var gs = go.AddComponent<GameState>();
        SetStaticInstance(null);

        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => TestReflectionUtil.InvokeMethod(gs, "Awake"));
        Assert.IsInstanceOf<System.InvalidOperationException>(ex.InnerException);
        Assert.AreSame(gs, GameState.Instance);

        Object.DestroyImmediate(go);
        SetStaticInstance(previousInstance);
    }

    [Test]
    public void Awake_SecondInstance_DestroysDuplicateGameObject()
    {
        var previousInstance = GameState.Instance;

        var first = new GameObject("GameStateTestFirst");
        var firstGs = first.AddComponent<GameState>();
        SetStaticInstance(null);
        Assert.Throws<System.Reflection.TargetInvocationException>(
            () => TestReflectionUtil.InvokeMethod(firstGs, "Awake"));
        Assert.AreSame(firstGs, GameState.Instance);

        var second = new GameObject("GameStateTestSecond");
        var secondGs = second.AddComponent<GameState>();
        // Second instance takes the early-return Destroy branch, which does not call
        // DontDestroyOnLoad, so this call does not throw. Note: Object.Destroy() in Edit
        // Mode logs an error-level message (Unity Test Framework fails the test on any
        // unexpected error log by default, hence the LogAssert.Expect below) and destroys
        // the object, but not synchronously within this call (verified empirically —
        // `second == null` is still false immediately after), so only the Instance
        // invariant is asserted below, not immediate nullity.
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Destroy may not be called from edit mode.*", System.Text.RegularExpressions.RegexOptions.Singleline));
        TestReflectionUtil.InvokeMethod(secondGs, "Awake");

        Assert.AreSame(firstGs, GameState.Instance);

        Object.DestroyImmediate(first);
        if (second != null)
        {
            Object.DestroyImmediate(second);
        }
        SetStaticInstance(previousInstance);
    }

    [Test]
    public void EnsureInstanceExists_NoInstance_CreatesGameObjectAndResetsCurrentState()
    {
        // EnsureInstanceExists() creates the GameObject via AddComponent<GameState>() but
        // does not itself call Awake() — in normal play that's fine (Unity's own
        // Play-Mode/scene-load bootstrap invokes Awake for it), but AddComponent alone does
        // NOT trigger Awake in an EditMode test context (verified empirically), so
        // GameState.Instance legitimately stays null here. What IS directly observable and
        // worth asserting: the CurrentState reset and that the GameObject got created.
        var previousInstance = GameState.Instance;
        SetStaticInstance(null);
        GameState.SetState(GameState.State.Playing); // sanity: not Initializing beforehand

        TestReflectionUtil.InvokeStaticMethod(typeof(GameState), "EnsureInstanceExists");

        Assert.AreEqual(GameState.State.Initializing, GameState.CurrentState);

        var created = GameObject.Find("GameState");
        Assert.IsNotNull(created, "EnsureInstanceExists should create a GameState GameObject when Instance is null.");
        Assert.IsNotNull(created.GetComponent<GameState>());

        Object.DestroyImmediate(created);
        SetStaticInstance(previousInstance);
    }

    [Test]
    public void EnsureInstanceExists_InstanceAlreadyExists_DoesNotCreateAnother()
    {
        var previousInstance = GameState.Instance;

        var go = new GameObject("GameStateTestExisting");
        var gs = go.AddComponent<GameState>();
        SetStaticInstance(gs);

        TestReflectionUtil.InvokeStaticMethod(typeof(GameState), "EnsureInstanceExists");

        Assert.AreSame(gs, GameState.Instance);

        Object.DestroyImmediate(go);
        SetStaticInstance(previousInstance);
    }

    private static void SetStaticInstance(GameState instance)
    {
        var field = typeof(GameState).GetField("<Instance>k__BackingField",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(null, instance);
    }
}
