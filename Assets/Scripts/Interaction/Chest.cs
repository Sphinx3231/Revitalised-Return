using UnityEngine;

/// <summary>
/// One-time item grant on interact (charter Step 10). CanInteract=false once looted is
/// sufficient self-exclusion for this task's scope -- no visual "looted" feedback is built
/// (explicitly out of scope per the approved approach).
/// </summary>
public sealed class Chest : Interactable
{
    [SerializeField] private ItemData itemToGrant;
    [SerializeField] private int quantity = 1;
    [SerializeField] private InventoryHolder targetInventory;

    private bool _looted;

    public override bool CanInteract(Transform interactor) => !_looted;

    public override void Interact(Transform interactor)
    {
        if (_looted)
            return;

        if (targetInventory != null && itemToGrant != null)
        {
            targetInventory.Inventory.AddItem(itemToGrant, quantity);
        }

        _looted = true;

        string itemLabel = itemToGrant != null ? itemToGrant.itemId : "item";
        EventBus.RaiseShowNotice($"Obtained {itemLabel} x{quantity}", 2f);
    }
}
