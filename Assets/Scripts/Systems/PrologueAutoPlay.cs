using UnityEngine;

// Prologue.unity is entered directly in Play Mode -- it isn't registered in
// EditorBuildSettings and there is no MainMenu->gameplay scene transition yet (a named,
// already-logged Step 11 scope boundary), so nothing ever calls GameState.SetState(Playing)
// here and GameState.IsPlayerInputLocked() stays true forever, silently no-oping all player
// input. Same fix pattern as Assets/Scripts/Systems/SandboxAutoPlay.cs (Sandbox scenes) and
// Assets/Scripts/UI/MainMenuAutoState.cs (MainMenu.unity). Remove once a real scene-transition
// flow from MainMenu lands and Prologue is reached through it instead of opened directly.
//
// Also seeds a minimal save context, exactly like SandboxAutoPlay, so Shrine's rest/save path
// and the NPC's quest-mutation path have PlayerData to act on during a direct Play Mode entry.
public class PrologueAutoPlay : MonoBehaviour
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
