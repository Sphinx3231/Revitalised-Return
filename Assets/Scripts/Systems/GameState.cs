using UnityEngine;

// Top-level control over global game loops, menu states, pausing, and cursor lock.
// See CLAUDE.md Step 2.2.
public class GameState : MonoBehaviour
{
    public enum State { Initializing, MainMenu, Playing, Paused, Dialogue, Cutscene, GameOver }

    public static GameState Instance { get; private set; }

    public static State CurrentState { get; private set; } = State.Initializing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Per-state cursor-lock/Time.timeScale transition table (CLAUDE.md 2.2). Note the
    // Unity-vs-Godot divergence: Time.timeScale = 0 does NOT stop Update()/FixedUpdate() the
    // way Godot's get_tree().paused did (it only zeroes Time.deltaTime), so gameplay scripts
    // must gate on IsPlayerInputLocked() rather than assume timeScale enforces the pause.
    public static void SetState(State newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case State.Playing:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f;
                break;

            case State.Paused:
            case State.MainMenu:
            case State.GameOver:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Time.timeScale = 0f;
                break;

            case State.Dialogue:
            case State.Cutscene:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f;
                break;

            case State.Initializing:
                // Transient pre-boot state occupied for at most one frame by the Bootstrap
                // scene before it calls SetState(MainMenu) — no cursor/timeScale side effects
                // here, they'd just be immediately overwritten (Director ruling, see task log).
                break;
        }
    }

    // Gameplay scripts should call this helper rather than reading CurrentState directly so
    // future refinements (e.g. Cutscene nuances) stay centralized here.
    public static bool IsPlayerInputLocked()
    {
        return CurrentState != State.Playing;
    }

    // Safety net: entering a scene directly (e.g. a Scenes/Sandbox/* test scene) without going
    // through Bootstrap first would otherwise leave GameState.Instance null. Runs before any
    // scene's Awake() via SubsystemRegistration. Also resets the static CurrentState field to
    // Initializing here: with domain reload disabled in Editor settings, static fields survive
    // across Play sessions instead of resetting automatically, so this call is what re-arms it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void EnsureInstanceExists()
    {
        CurrentState = State.Initializing;

        if (Instance != null)
            return;

        var gameStateObject = new GameObject(nameof(GameState));
        gameStateObject.AddComponent<GameState>();
    }
}
