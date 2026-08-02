using NUnit.Framework;
using UnityEngine;

// Inventory's locked structure (charter Step 10): List<ItemStack> + a non-serialized runtime
// index Dictionary rebuilt on load/construction. Covers AddItem's documented overflow policy
// (cap at maxStack, silently drop the rest -- no second-stack splitting), HasItem's index
// lookup, and Rebuild() re-populating the index from a stacks list mutated externally.
public class InventoryTests
{
    private ItemData _ore;
    private ItemData _sprig;

    [SetUp]
    public void SetUp()
    {
        _ore = ScriptableObject.CreateInstance<ItemData>();
        _ore.itemId = "tamahagane_ore";
        _ore.maxStack = 10;

        _sprig = ScriptableObject.CreateInstance<ItemData>();
        _sprig.itemId = "ashroot_sprig";
        _sprig.maxStack = 20;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_ore);
        Object.DestroyImmediate(_sprig);
    }

    [Test]
    public void NewInventory_HasEmptyStacksList()
    {
        var inventory = new Inventory();
        Assert.AreEqual(0, inventory.stacks.Count);
    }

    [Test]
    public void AddItem_NewItem_CreatesStackWithQuantity()
    {
        var inventory = new Inventory();

        inventory.AddItem(_ore, 3);

        Assert.AreEqual(1, inventory.stacks.Count);
        Assert.AreSame(_ore, inventory.stacks[0].item);
        Assert.AreEqual(3, inventory.stacks[0].quantity);
    }

    [Test]
    public void AddItem_ExistingItem_IncrementsSameStack_NotANewOne()
    {
        var inventory = new Inventory();

        inventory.AddItem(_ore, 3);
        inventory.AddItem(_ore, 2);

        Assert.AreEqual(1, inventory.stacks.Count);
        Assert.AreEqual(5, inventory.stacks[0].quantity);
    }

    [Test]
    public void AddItem_OverflowBeyondMaxStack_IsCappedAndSilentlyDropped()
    {
        var inventory = new Inventory();

        inventory.AddItem(_ore, 8);
        inventory.AddItem(_ore, 8); // 8+8=16, but maxStack=10 -> capped at 10, 6 dropped.

        Assert.AreEqual(1, inventory.stacks.Count);
        Assert.AreEqual(10, inventory.stacks[0].quantity);
    }

    [Test]
    public void AddItem_QuantityExceedingMaxStackOnFirstAdd_IsCappedAtMaxStack()
    {
        var inventory = new Inventory();

        inventory.AddItem(_ore, 999);

        Assert.AreEqual(10, inventory.stacks[0].quantity);
    }

    [Test]
    public void AddItem_DifferentItems_CreateSeparateStacks()
    {
        var inventory = new Inventory();

        inventory.AddItem(_ore, 1);
        inventory.AddItem(_sprig, 1);

        Assert.AreEqual(2, inventory.stacks.Count);
    }

    [Test]
    public void AddItem_NullItem_IsNoOp()
    {
        var inventory = new Inventory();

        inventory.AddItem(null, 5);

        Assert.AreEqual(0, inventory.stacks.Count);
    }

    [Test]
    public void AddItem_ZeroOrNegativeQuantity_IsNoOp()
    {
        var inventory = new Inventory();

        inventory.AddItem(_ore, 0);
        inventory.AddItem(_ore, -5);

        Assert.AreEqual(0, inventory.stacks.Count);
    }

    [Test]
    public void HasItem_AfterAdd_ReturnsTrueForThatItemId_FalseForOthers()
    {
        var inventory = new Inventory();
        inventory.AddItem(_ore, 1);

        Assert.IsTrue(inventory.HasItem("tamahagane_ore"));
        Assert.IsFalse(inventory.HasItem("ashroot_sprig"));
    }

    [Test]
    public void HasItem_NullOrEmptyId_ReturnsFalse()
    {
        var inventory = new Inventory();
        inventory.AddItem(_ore, 1);

        Assert.IsFalse(inventory.HasItem(null));
        Assert.IsFalse(inventory.HasItem(string.Empty));
    }

    [Test]
    public void Rebuild_RepopulatesIndexFromExternallyMutatedStacksList()
    {
        var inventory = new Inventory();
        // Externally mutate stacks directly (bypassing AddItem), simulating a future
        // deserialize-from-disk load per the class's own doc comment.
        inventory.stacks.Add(new ItemStack { item = _sprig, quantity = 4 });

        // Before Rebuild(), the index hasn't seen this stack.
        Assert.IsFalse(inventory.HasItem("ashroot_sprig"));

        inventory.Rebuild();

        Assert.IsTrue(inventory.HasItem("ashroot_sprig"));
    }

    [Test]
    public void Rebuild_SkipsMalformedStacks_NullItemOrEmptyId()
    {
        var inventory = new Inventory();
        inventory.stacks.Add(new ItemStack { item = null, quantity = 1 });

        Assert.DoesNotThrow(() => inventory.Rebuild());
    }
}
