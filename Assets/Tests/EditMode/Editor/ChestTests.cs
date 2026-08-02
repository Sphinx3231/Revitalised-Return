using NUnit.Framework;
using UnityEngine;

// Chest's one-time-grant contract (charter Step 10 approach doc): grants exactly one
// ItemStack into a serialized-reference Inventory (via InventoryHolder) and self-excludes via
// CanInteract=false once looted -- no visual "looted" feedback is in scope. Private serialized
// fields are set via TestReflectionUtil, matching this codebase's established pattern for
// wiring a MonoBehaviour's Inspector-only fields from EditMode tests.
public class ChestTests
{
    private GameObject _chestGo;
    private Chest _chest;

    private GameObject _holderGo;
    private InventoryHolder _holder;

    private ItemData _item;

    [SetUp]
    public void SetUp()
    {
        _item = ScriptableObject.CreateInstance<ItemData>();
        _item.itemId = "tamahagane_ore";
        _item.maxStack = 99;

        _holderGo = new GameObject("InventoryHolder");
        _holder = _holderGo.AddComponent<InventoryHolder>();

        _chestGo = new GameObject("Chest", typeof(BoxCollider));
        _chest = _chestGo.AddComponent<Chest>();

        TestReflectionUtil.SetField(_chest, "itemToGrant", _item);
        TestReflectionUtil.SetField(_chest, "quantity", 3);
        TestReflectionUtil.SetField(_chest, "targetInventory", _holder);
    }

    [TearDown]
    public void TearDown()
    {
        if (_chestGo != null) Object.DestroyImmediate(_chestGo);
        if (_holderGo != null) Object.DestroyImmediate(_holderGo);
        if (_item != null) Object.DestroyImmediate(_item);
    }

    [Test]
    public void CanInteract_BeforeLooting_IsTrue()
    {
        Assert.IsTrue(_chest.CanInteract(_chestGo.transform));
    }

    [Test]
    public void Interact_GrantsItemIntoTargetInventory()
    {
        _chest.Interact(_chestGo.transform);

        Assert.AreEqual(1, _holder.Inventory.stacks.Count);
        Assert.AreSame(_item, _holder.Inventory.stacks[0].item);
        Assert.AreEqual(3, _holder.Inventory.stacks[0].quantity);
    }

    [Test]
    public void Interact_SetsCanInteractFalse_SelfExcludesAfterLooting()
    {
        _chest.Interact(_chestGo.transform);

        Assert.IsFalse(_chest.CanInteract(_chestGo.transform));
    }

    [Test]
    public void Interact_CalledTwice_OnlyGrantsOnce()
    {
        _chest.Interact(_chestGo.transform);
        _chest.Interact(_chestGo.transform);

        Assert.AreEqual(1, _holder.Inventory.stacks.Count);
        Assert.AreEqual(3, _holder.Inventory.stacks[0].quantity);
    }

    [Test]
    public void Interact_RaisesShowNoticeWithItemLabel()
    {
        string receivedText = null;
        void Handler(string text, float duration) => receivedText = text;

        EventBus.ShowNotice += Handler;
        try
        {
            _chest.Interact(_chestGo.transform);
        }
        finally
        {
            EventBus.ShowNotice -= Handler;
        }

        Assert.AreEqual("Obtained tamahagane_ore x3", receivedText);
    }

    [Test]
    public void Interact_NoTargetInventoryWired_DoesNotThrow_StillMarksLooted()
    {
        TestReflectionUtil.SetField(_chest, "targetInventory", null);

        Assert.DoesNotThrow(() => _chest.Interact(_chestGo.transform));
        Assert.IsFalse(_chest.CanInteract(_chestGo.transform));
    }
}
