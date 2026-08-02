using NUnit.Framework;
using UnityEngine;

// Shrine's Interact is a placeholder rest behavior (charter Step 10 -- real rest/save is
// Step 11's territory): it fires EventBus.ShowNotice and nothing else. Verified via the
// EventBus's own event, matching the project's established EventBus-subscriber test pattern.
public class ShrineTests
{
    private GameObject _go;
    private Shrine _shrine;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Shrine", typeof(BoxCollider));
        _shrine = _go.AddComponent<Shrine>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    [Test]
    public void Interact_RaisesShowNotice()
    {
        string receivedText = null;
        float receivedDuration = -1f;

        void Handler(string text, float duration)
        {
            receivedText = text;
            receivedDuration = duration;
        }

        EventBus.ShowNotice += Handler;
        try
        {
            _shrine.Interact(_go.transform);
        }
        finally
        {
            EventBus.ShowNotice -= Handler;
        }

        Assert.AreEqual("Rested at the shrine.", receivedText);
        Assert.AreEqual(3f, receivedDuration);
    }

    [Test]
    public void Interact_IsRepeatable_NoOneShotGate()
    {
        // Unlike Chest/HarvestNode, Shrine has no one-shot depletion -- CanInteract must
        // remain true across repeated interacts (this task's scope: real rest/cooldown
        // behavior is Step 11's job).
        Assert.DoesNotThrow(() => _shrine.Interact(_go.transform));
        Assert.IsTrue(_shrine.CanInteract(_go.transform));
        Assert.DoesNotThrow(() => _shrine.Interact(_go.transform));
        Assert.IsTrue(_shrine.CanInteract(_go.transform));
    }
}
