using NUnit.Framework;
using UnityEngine;

public class ParryControllerTests
{
    private GameObject _go;
    private ParryController _parry;

    private GameObject _stanceGo;
    private StanceController _stanceController;
    private StanceData _stance;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Defender");
        _parry = _go.AddComponent<ParryController>();

        _stanceGo = new GameObject("Stances");
        _stanceController = _stanceGo.AddComponent<StanceController>();
        _stance = ScriptableObject.CreateInstance<StanceData>();
        _stance.parryWindowDuration = 0.18f;
        TestReflectionUtil.SetField(_stanceController, "stances", new[] { _stance });
        TestReflectionUtil.SetField(_stanceController, "_currentIndex", 0);

        TestReflectionUtil.SetField(_parry, "stanceController", _stanceController);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        if (_stanceGo != null) Object.DestroyImmediate(_stanceGo);
        if (_stance != null) Object.DestroyImmediate(_stance);
    }

    [Test]
    public void TryParry_SetsIsParryingTrue_UsingCurrentStancesWindow()
    {
        _parry.TryParry();

        Assert.IsTrue(_parry.IsParrying);

        float remaining = TestReflectionUtil.GetField<float>(_parry, "_windowRemaining");
        Assert.AreEqual(0.18f, remaining, 0.0001f);
    }

    [Test]
    public void TryParry_NoStanceController_UsesDefaultWindow()
    {
        TestReflectionUtil.SetField(_parry, "stanceController", null);

        _parry.TryParry();

        float remaining = TestReflectionUtil.GetField<float>(_parry, "_windowRemaining");
        Assert.AreEqual(0.12f, remaining, 0.0001f);
    }

    [Test]
    public void TryParry_NoStancesConfigured_UsesDefaultWindow()
    {
        TestReflectionUtil.SetField(_stanceController, "stances", new StanceData[0]);

        _parry.TryParry();

        float remaining = TestReflectionUtil.GetField<float>(_parry, "_windowRemaining");
        Assert.AreEqual(0.12f, remaining, 0.0001f);
    }

    [Test]
    public void TryParry_WhileAlreadyParrying_IsANoOp()
    {
        _parry.TryParry();
        _parry.TickParry(0.1f); // window now partially elapsed (0.08s remaining)

        _parry.TryParry(); // must not reset the window back to 0.18

        float remaining = TestReflectionUtil.GetField<float>(_parry, "_windowRemaining");
        Assert.AreEqual(0.08f, remaining, 0.0001f);
    }

    [Test]
    public void TickParry_WhileNotParrying_IsANoOp()
    {
        Assert.DoesNotThrow(() => _parry.TickParry(0.1f));
        Assert.IsFalse(_parry.IsParrying);
    }

    [Test]
    public void TickParry_ExpiresWindow_SetsIsParryingFalse()
    {
        _parry.TryParry(); // 0.18s window

        _parry.TickParry(0.1f);
        Assert.IsTrue(_parry.IsParrying);

        _parry.TickParry(0.09f); // total 0.19s, past the window
        Assert.IsFalse(_parry.IsParrying);
    }

    [Test]
    public void IsBlocking_DefaultsFalse_AndIsSettable()
    {
        Assert.IsFalse(_parry.IsBlocking);

        _parry.IsBlocking = true;

        Assert.IsTrue(_parry.IsBlocking);
    }
}
