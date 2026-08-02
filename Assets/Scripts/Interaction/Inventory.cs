using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plain C# data container (charter Step 10 -- NOT a MonoBehaviour; "List&lt;ItemStack&gt; + a
/// non-serialized runtime index Dictionary rebuilt on load"). No save system exists yet, so
/// "on load" currently means "on construction/Rebuild()" -- the real load-from-disk hook is
/// Step 11's territory. Knows nothing about UI or specific item behaviors (S.O.L.I.D. --
/// that's each Interactable subclass's / the future HUD's job).
///
/// Overflow policy (documented, not left ambiguous): AddItem caps a stack at maxStack and
/// silently drops the overflow beyond that cap for this task's scope -- no partial-stack
/// splitting into a second ItemStack of the same item, since Step 10 doesn't require full
/// inventory-grid semantics yet. Logged here rather than in a comment buried elsewhere.
/// </summary>
public class Inventory
{
    public List<ItemStack> stacks = new List<ItemStack>();

    private Dictionary<string, ItemStack> _index = new Dictionary<string, ItemStack>();

    public Inventory()
    {
        Rebuild();
    }

    /// <summary>Clears and repopulates the runtime index from `stacks`, keyed by item.itemId.</summary>
    public void Rebuild()
    {
        _index.Clear();

        foreach (ItemStack stack in stacks)
        {
            if (stack == null || stack.item == null || string.IsNullOrEmpty(stack.item.itemId))
                continue;

            _index[stack.item.itemId] = stack;
        }
    }

    /// <summary>
    /// Adds `quantity` of `item`, incrementing an existing stack (capped at maxStack, overflow
    /// silently dropped per this class's documented policy) or creating a new stack.
    /// </summary>
    public void AddItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0)
            return;

        if (_index.TryGetValue(item.itemId, out ItemStack existing))
        {
            int room = Mathf.Max(0, item.maxStack - existing.quantity);
            existing.quantity += Mathf.Min(quantity, room);
            return;
        }

        var newStack = new ItemStack
        {
            item = item,
            quantity = Mathf.Min(quantity, item.maxStack)
        };

        stacks.Add(newStack);
        _index[item.itemId] = newStack;
    }

    /// <summary>Convenience lookup via the runtime index -- true if any stack of this itemId exists.</summary>
    public bool HasItem(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && _index.ContainsKey(itemId);
    }
}
