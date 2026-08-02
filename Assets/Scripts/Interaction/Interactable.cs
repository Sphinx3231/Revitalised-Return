using UnityEngine;

/// <summary>
/// Abstract base for all world interactables (charter Step 10). Requires a Collider so every
/// concrete subclass is guaranteed to sit on a trigger Collider (layer=Interactable,
/// physics layer 12) for InteractionResolver's OverlapSphere scan to find. CanInteract is the
/// Open/Closed/Liskov hook that lets Chest/HarvestNode self-exclude once depleted without
/// InteractionResolver branching on concrete types.
///
/// promptText is inert data with zero dependencies -- included now even though nothing
/// displays it this task (Step 11's HUD interaction prompt will consume it) so subclasses and
/// scene instances don't need re-touching later.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] private string promptText = "Interact"; // TODO(Step 11): consumed by the HUD interaction prompt.

    public string PromptText => promptText;

    public virtual bool CanInteract(Transform interactor) => true;

    public abstract void Interact(Transform interactor);
}
