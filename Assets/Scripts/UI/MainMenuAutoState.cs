using UnityEngine;

// MainMenu.unity is entered directly in Play Mode (not yet wired into the Bootstrap flow —
// see docs/Tasks/2026-07-31-ui-systems-phase2.md) and needs GameState.CurrentState to reach
// MainMenu for the cursor to unlock/show (GameState.SetState already handles the
// lockState/visible transition correctly for MainMenu — nothing was calling it here).
// Same fix pattern as Assets/Scripts/Systems/SandboxAutoPlay.cs for Sandbox scenes.
public class MainMenuAutoState : MonoBehaviour
{
    private void Start()
    {
        GameState.SetState(GameState.State.MainMenu);
    }
}
