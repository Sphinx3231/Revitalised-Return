using UnityEngine;

// Top-level control over global game loops, menu states, pausing, and cursor lock.
// See CLAUDE.md Step 2.2. Minimal stub shell only — full transition logic (cursor lock,
// Time.timeScale handling) lands in the real Step 2 implementation task.
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

    // TODO(Step 2): Playing -> Cursor.lockState = Locked, Time.timeScale = 1f.
    // TODO(Step 2): Paused/MainMenu -> Cursor.lockState = None, Time.timeScale = 0f.
    // TODO(Step 2): Dialogue/Cutscene -> Cursor.lockState = Locked, timeScale stays 1f,
    // player input locked via IsPlayerInputLocked().
    public static void SetState(State newState)
    {
        CurrentState = newState;
    }

    // TODO(Step 2): this stub definition (CurrentState != Playing) is the intended long-term
    // behavior per CLAUDE.md 2.2, but gameplay scripts should still call this helper rather
    // than reading CurrentState directly so future refinements (e.g. Cutscene nuances) stay
    // centralized here.
    public static bool IsPlayerInputLocked()
    {
        return CurrentState != State.Playing;
    }
}
