/// <summary>
/// Placeholder rest behavior (charter Step 10 -- real rest/save/map-reveal is Step 11's
/// territory). This proves the interaction pipeline works end-to-end -- the Prologue scene's
/// ShrineMarker gets a real Shrine component here for the first time -- without inventing
/// save/rest behavior prematurely.
/// </summary>
public sealed class Shrine : Interactable
{
    public override void Interact(UnityEngine.Transform interactor)
    {
        EventBus.RaiseShowNotice("Rested at the shrine.", 3f);
    }
}
