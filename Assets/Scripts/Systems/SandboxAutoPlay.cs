using UnityEngine;

// Sandbox test scenes (Assets/Scenes/Sandbox/*) are entered directly in Play Mode, skipping
// the Bootstrap scene's flow that would otherwise call GameState.SetState(Playing). Without
// this, GameState.IsPlayerInputLocked() stays true forever and every gated system (player
// input included) silently no-ops. Attach only to Sandbox test scenes, never to Bootstrap or
// real gameplay scenes — those own their own state transitions.
public class SandboxAutoPlay : MonoBehaviour
{
    private void Start()
    {
        GameState.SetState(GameState.State.Playing);
    }
}
