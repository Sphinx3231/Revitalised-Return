using NUnit.Framework;
using UnityEngine;

public class StanceControllerTests
{
    private GameObject _go;
    private StanceController _controller;
    private StanceData[] _stances;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Stances");
        _controller = _go.AddComponent<StanceController>();

        _stances = new StanceData[4];
        for (int i = 0; i < 4; i++)
        {
            _stances[i] = ScriptableObject.CreateInstance<StanceData>();
            _stances[i].stanceName = "Stance" + i;
        }

        TestReflectionUtil.SetField(_controller, "stances", _stances);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
        {
            Object.DestroyImmediate(_go);
        }
        foreach (var s in _stances)
        {
            if (s != null)
                Object.DestroyImmediate(s);
        }
    }

    [Test]
    public void CycleNext_FromLastIndex_WrapsToZero()
    {
        TestReflectionUtil.SetField(_controller, "_currentIndex", 3);

        _controller.CycleNext();

        int index = TestReflectionUtil.GetField<int>(_controller, "_currentIndex");
        Assert.AreEqual(0, index);
    }

    [Test]
    public void CyclePrevious_FromZero_WrapsToLastIndex()
    {
        TestReflectionUtil.SetField(_controller, "_currentIndex", 0);

        _controller.CyclePrevious();

        int index = TestReflectionUtil.GetField<int>(_controller, "_currentIndex");
        Assert.AreEqual(3, index);
    }

    [Test]
    public void CycleNext_FiresStanceSwappedWithCorrectStanceData()
    {
        TestReflectionUtil.SetField(_controller, "_currentIndex", 0);

        StanceData got = null;
        System.Action<StanceData> handler = s => got = s;
        EventBus.StanceSwapped += handler;

        _controller.CycleNext();

        EventBus.StanceSwapped -= handler;

        Assert.AreSame(_stances[1], got);
    }

    [Test]
    public void CyclePrevious_FiresStanceSwappedWithCorrectStanceData()
    {
        TestReflectionUtil.SetField(_controller, "_currentIndex", 0);

        StanceData got = null;
        System.Action<StanceData> handler = s => got = s;
        EventBus.StanceSwapped += handler;

        _controller.CyclePrevious();

        EventBus.StanceSwapped -= handler;

        Assert.AreSame(_stances[3], got);
    }

    [Test]
    public void CycleNext_NullArray_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_controller, "stances", null);
        Assert.DoesNotThrow(() => _controller.CycleNext());
    }

    [Test]
    public void CyclePrevious_NullArray_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_controller, "stances", null);
        Assert.DoesNotThrow(() => _controller.CyclePrevious());
    }

    [Test]
    public void CycleNext_EmptyArray_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_controller, "stances", new StanceData[0]);
        Assert.DoesNotThrow(() => _controller.CycleNext());
    }

    [Test]
    public void CyclePrevious_EmptyArray_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_controller, "stances", new StanceData[0]);
        Assert.DoesNotThrow(() => _controller.CyclePrevious());
    }
}
