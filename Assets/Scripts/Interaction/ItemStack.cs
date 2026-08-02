/// <summary>
/// A quantity of one ItemData. Plain serializable data, no behavior (charter Step 10 --
/// Inventory manages ItemStacks, ItemStacks don't know about Inventory or UI).
/// </summary>
[System.Serializable]
public class ItemStack
{
    public ItemData item;
    public int quantity;
}
