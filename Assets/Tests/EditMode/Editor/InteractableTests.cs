using NUnit.Framework;
using UnityEngine;

// Interactable is abstract, so its base-class contract (default CanInteract=true, promptText
// default, RequireComponent<Collider>) is exercised through Shrine -- the simplest concrete
// subclass, which adds zero behavior of its own beyond the base defaults.
public class InteractableTests
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
    public void ConcreteSubclass_CoexistsWithACollider()
    {
        // Interactable is [RequireComponent(typeof(Collider))] -- every concrete subclass
        // must be able to sit alongside a Collider without conflict (InteractionResolver's
        // OverlapSphere scan depends on this).
        Assert.IsNotNull(_go.GetComponent<Collider>());
    }

    [Test]
    public void DefaultCanInteract_IsTrue()
    {
        Assert.IsTrue(_shrine.CanInteract(_go.transform));
    }

    [Test]
    public void DefaultPromptText_IsInteract()
    {
        Assert.AreEqual("Interact", _shrine.PromptText);
    }
}
