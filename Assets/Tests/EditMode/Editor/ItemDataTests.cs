using NUnit.Framework;
using UnityEngine;

// ItemData is a plain-field ScriptableObject (charter Step 10) -- no behavior beyond data
// storage, so coverage here is about proving the locked field set round-trips correctly and
// the documented default (maxStack = 1, per ItemData.cs) actually holds for a freshly-created
// instance, not about exercising any logic (there is none to exercise).
public class ItemDataTests
{
    [Test]
    public void FreshInstance_HasDocumentedDefaults()
    {
        var item = ScriptableObject.CreateInstance<ItemData>();

        Assert.AreEqual(1, item.maxStack);
        Assert.AreEqual(0, item.valueMon);
        Assert.IsNull(item.itemId);

        Object.DestroyImmediate(item);
    }

    [Test]
    public void AllLockedFields_RoundTrip()
    {
        var item = ScriptableObject.CreateInstance<ItemData>();

        item.itemId = "tamahagane_ore";
        item.category = ItemCategory.UpgradeMat;
        item.maxStack = 99;
        item.valueMon = 25;
        item.regionTag = "Prologue";
        item.description = "Raw folded steel.";

        Assert.AreEqual("tamahagane_ore", item.itemId);
        Assert.AreEqual(ItemCategory.UpgradeMat, item.category);
        Assert.AreEqual(99, item.maxStack);
        Assert.AreEqual(25, item.valueMon);
        Assert.AreEqual("Prologue", item.regionTag);
        Assert.AreEqual("Raw folded steel.", item.description);

        Object.DestroyImmediate(item);
    }

    [Test]
    public void AllCategoryValues_AreAssignable()
    {
        var item = ScriptableObject.CreateInstance<ItemData>();

        foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
        {
            item.category = category;
            Assert.AreEqual(category, item.category);
        }

        Object.DestroyImmediate(item);
    }
}
