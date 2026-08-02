using NUnit.Framework;
using UnityEngine;

// ItemStack is a plain [System.Serializable] data pair (charter Step 10 -- "no behavior,
// Inventory manages ItemStacks"). Tests only prove the field pair round-trips correctly.
public class ItemStackTests
{
    [Test]
    public void FieldsRoundTrip()
    {
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = "ashroot_sprig";

        var stack = new ItemStack { item = item, quantity = 5 };

        Assert.AreSame(item, stack.item);
        Assert.AreEqual(5, stack.quantity);

        Object.DestroyImmediate(item);
    }

    [Test]
    public void DefaultConstructed_HasNullItemAndZeroQuantity()
    {
        var stack = new ItemStack();

        Assert.IsNull(stack.item);
        Assert.AreEqual(0, stack.quantity);
    }
}
