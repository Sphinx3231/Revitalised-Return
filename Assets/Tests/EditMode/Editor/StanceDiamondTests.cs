using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class StanceDiamondTests
{
    private GameObject _go;
    private StanceDiamond _diamond;
    private StanceData[] _order;
    private Image[] _icons;
    private GameObject[] _iconGos;
    private Color _selected = Color.white;
    private Color _dimmed = new Color(1f, 1f, 1f, 0.4f);

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("StanceDiamond");
        _diamond = _go.AddComponent<StanceDiamond>();

        _order = new StanceData[4];
        _icons = new Image[4];
        _iconGos = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            _order[i] = ScriptableObject.CreateInstance<StanceData>();
            _iconGos[i] = new GameObject("Icon" + i);
            _icons[i] = _iconGos[i].AddComponent<Image>();
        }

        TestReflectionUtil.SetField(_diamond, "stanceOrder", _order);
        TestReflectionUtil.SetField(_diamond, "icons", _icons);
        TestReflectionUtil.SetField(_diamond, "selectedColor", _selected);
        TestReflectionUtil.SetField(_diamond, "dimmedColor", _dimmed);

        // AddComponent may or may not have already auto-invoked OnEnable depending on
        // Editor behavior; force a known unsubscribed baseline (unsubscribe is a safe
        // no-op if it wasn't subscribed) so subscription tests aren't order-dependent.
        TestReflectionUtil.InvokeMethod(_diamond, "OnDisable");
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        foreach (var s in _order) if (s != null) Object.DestroyImmediate(s);
        foreach (var g in _iconGos) if (g != null) Object.DestroyImmediate(g);
    }

    [Test]
    public void HandleStanceSwapped_KnownStance_HighlightsMatchingIconOnly()
    {
        TestReflectionUtil.InvokeMethod(_diamond, "HandleStanceSwapped", (object)_order[2]);

        for (int i = 0; i < 4; i++)
        {
            Color expected = i == 2 ? _selected : _dimmed;
            Assert.AreEqual(expected, _icons[i].color, "index " + i);
        }
    }

    [Test]
    public void HandleStanceSwapped_NullStance_DimsAllIcons()
    {
        TestReflectionUtil.InvokeMethod(_diamond, "HandleStanceSwapped", new object[] { null });

        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(_dimmed, _icons[i].color, "index " + i);
        }
    }

    [Test]
    public void HandleStanceSwapped_UnknownStance_DimsAllIcons()
    {
        var unknown = ScriptableObject.CreateInstance<StanceData>();

        TestReflectionUtil.InvokeMethod(_diamond, "HandleStanceSwapped", (object)unknown);

        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(_dimmed, _icons[i].color, "index " + i);
        }

        Object.DestroyImmediate(unknown);
    }

    [Test]
    public void OnEnable_SubscribesToStanceSwapped()
    {
        TestReflectionUtil.InvokeMethod(_diamond, "OnEnable");

        EventBus.RaiseStanceSwapped(_order[0]);

        Assert.AreEqual(_selected, _icons[0].color);
    }

    [Test]
    public void OnDisable_UnsubscribesFromStanceSwapped()
    {
        TestReflectionUtil.InvokeMethod(_diamond, "OnEnable");
        TestReflectionUtil.InvokeMethod(_diamond, "OnDisable");

        // Set a sentinel color, then fire — should remain unchanged since unsubscribed.
        _icons[0].color = Color.green;
        EventBus.RaiseStanceSwapped(_order[0]);

        Assert.AreEqual(Color.green, _icons[0].color);
    }
}
