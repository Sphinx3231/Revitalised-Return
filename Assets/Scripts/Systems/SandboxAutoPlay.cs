using UnityEngine;

// Sandbox test scenes (Assets/Scenes/Sandbox/*) are entered directly in Play Mode, skipping
// the Bootstrap scene's flow that would otherwise call GameState.SetState(Playing). Without
// this, GameState.IsPlayerInputLocked() stays true forever and every gated system (player
// input included) silently no-ops. Attach only to Sandbox test scenes, never to Bootstrap or
// real gameplay scenes — those own their own state transitions.
//
// Also seeds a minimal save context (charter Step 11) the same way SaveSlotMenu would at the
// real main menu -- a Sandbox scene is entered directly, bypassing slot-select entirely, so
// without this SaveSystem.Current/CurrentPlayerData would stay null and Shrine's rest/save
// path (and MapScreen's discoveredShrines reveal) would have nothing to act on during a
// manual Play Mode pass. Only ever does this if nothing has already set a save context (e.g.
// a future scene-transition flow from MainMenu.unity that lands here), never overwrites one.
public class SandboxAutoPlay : MonoBehaviour
{
    private void Start()
    {
        GameState.SetState(GameState.State.Playing);

        if (SaveSystem.CurrentPlayerData == null)
        {
            SaveSystem.Current = new SaveSystem();
            SaveSystem.CurrentSlot = 0;
            SaveSystem.CurrentPlayerData = new PlayerData();
        }
    }
}
