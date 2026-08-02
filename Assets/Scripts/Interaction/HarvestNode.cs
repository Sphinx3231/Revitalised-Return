using UnityEngine;

/// <summary>
/// Single-item gathering node (charter Step 10). Same one-shot pattern as Chest, simpler --
/// a single ItemData/quantity, harvested once per session. Respawn-on-shrine-rest is a
/// stubbed hook: no rest/save system exists yet (Step 11's territory).
/// </summary>
public sealed class HarvestNode : Interactable
{
    [SerializeField] private ItemData itemToGrant;
    [SerializeField] private int quantity = 1;
    [SerializeField] private InventoryHolder targetInventory;

    private bool _harvested;

    public override bool CanInteract(Transform interactor) => !_harvested;

    public override void Interact(Transform interactor)
    {
        if (_harvested)
            return;

        if (targetInventory != null && itemToGrant != null)
        {
            targetInventory.Inventory.AddItem(itemToGrant, quantity);
        }

        _harvested = true;
        // TODO(Step 11): respawn on shrine rest.

        string itemLabel = itemToGrant != null ? itemToGrant.itemId : "item";
        EventBus.RaiseShowNotice($"Gathered {itemLabel} x{quantity}", 2f);
    }
}
