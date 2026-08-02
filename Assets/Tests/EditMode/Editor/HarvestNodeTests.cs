using NUnit.Framework;
using UnityEngine;

// HarvestNode mirrors Chest's one-shot grant contract (charter Step 10) with a stubbed
// respawn-on-shrine-rest hook (Step 11's territory, not functional yet -- not tested here
// since there's nothing to exercise). Same TestReflectionUtil pattern as ChestTests.
public class HarvestNodeTests
{
    private GameObject _nodeGo;
    private HarvestNode _node;

    private GameObject _holderGo;
    private InventoryHolder _holder;

    private ItemData _item;

    [SetUp]
    public void SetUp()
    {
        _item = ScriptableObject.CreateInstance<ItemData>();
        _item.itemId = "ashroot_sprig";
        _item.maxStack = 20;

        _holderGo = new GameObject("InventoryHolder");
        _holder = _holderGo.AddComponent<InventoryHolder>();

        _nodeGo = new GameObject("HarvestNode", typeof(SphereCollider));
        _node = _nodeGo.AddComponent<HarvestNode>();

        TestReflectionUtil.SetField(_node, "itemToGrant", _item);
        TestReflectionUtil.SetField(_node, "quantity", 1);
        TestReflectionUtil.SetField(_node, "targetInventory", _holder);
    }

    [TearDown]
    public void TearDown()
    {
        if (_nodeGo != null) Object.DestroyImmediate(_nodeGo);
        if (_holderGo != null) Object.DestroyImmediate(_holderGo);
        if (_item != null) Object.DestroyImmediate(_item);
    }

    [Test]
    public void CanInteract_BeforeHarvesting_IsTrue()
    {
        Assert.IsTrue(_node.CanInteract(_nodeGo.transform));
    }

    [Test]
    public void Interact_GrantsItemIntoTargetInventory()
    {
        _node.Interact(_nodeGo.transform);

        Assert.AreEqual(1, _holder.Inventory.stacks.Count);
        Assert.AreSame(_item, _holder.Inventory.stacks[0].item);
        Assert.AreEqual(1, _holder.Inventory.stacks[0].quantity);
    }

    [Test]
    public void Interact_SetsCanInteractFalse_SelfExcludesAfterHarvesting()
    {
        _node.Interact(_nodeGo.transform);

        Assert.IsFalse(_node.CanInteract(_nodeGo.transform));
    }

    [Test]
    public void Interact_CalledTwiceThisSession_OnlyGrantsOnce()
    {
        _node.Interact(_nodeGo.transform);
        _node.Interact(_nodeGo.transform);

        Assert.AreEqual(1, _holder.Inventory.stacks.Count);
        Assert.AreEqual(1, _holder.Inventory.stacks[0].quantity);
    }

    [Test]
    public void Interact_RaisesShowNoticeWithItemLabel()
    {
        string receivedText = null;
        void Handler(string text, float duration) => receivedText = text;

        EventBus.ShowNotice += Handler;
        try
        {
            _node.Interact(_nodeGo.transform);
        }
        finally
        {
            EventBus.ShowNotice -= Handler;
        }

        Assert.AreEqual("Gathered ashroot_sprig x1", receivedText);
    }

    [Test]
    public void Interact_NoTargetInventoryWired_DoesNotThrow_StillMarksHarvested()
    {
        TestReflectionUtil.SetField(_node, "targetInventory", null);

        Assert.DoesNotThrow(() => _node.Interact(_nodeGo.transform));
        Assert.IsFalse(_node.CanInteract(_nodeGo.transform));
    }
}
