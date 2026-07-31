using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class PostureBarTests
{
    private GameObject _go;
    private PostureBar _bar;
    private GameObject _imageGo;
    private Image _image;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("PostureBar");
        _bar = _go.AddComponent<PostureBar>();

        _imageGo = new GameObject("Fill");
        _image = _imageGo.AddComponent<Image>();

        TestReflectionUtil.SetField(_bar, "fillImage", _image);
        TestReflectionUtil.SetField(_bar, "fader", null);

        TestReflectionUtil.InvokeMethod(_bar, "OnDisable");
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        if (_imageGo != null) Object.DestroyImmediate(_imageGo);
    }

    [Test]
    public void Awake_SetsDefaultFillToFull()
    {
        TestReflectionUtil.InvokeMethod(_bar, "Awake");
        Assert.AreEqual(1f, _image.fillAmount);
    }

    [Test]
    public void HandleChanged_SetsFillAmountToRatio()
    {
        TestReflectionUtil.InvokeMethod(_bar, "HandleChanged", 40f, 100f);
        Assert.AreEqual(0.4f, _image.fillAmount, 0.0001f);
    }

    [Test]
    public void HandleChanged_MaxLessThanOrEqualZero_GuardsAgainstDivideByZero()
    {
        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_bar, "HandleChanged", 10f, 0f));
        Assert.AreEqual(0f, _image.fillAmount);
    }

    [Test]
    public void OnEnable_SubscribesToPlayerPostureChanged()
    {
        TestReflectionUtil.InvokeMethod(_bar, "OnEnable");

        EventBus.RaisePlayerPostureChanged(15f, 100f);

        Assert.AreEqual(0.15f, _image.fillAmount, 0.0001f);
    }

    [Test]
    public void OnDisable_UnsubscribesFromPlayerPostureChanged()
    {
        TestReflectionUtil.InvokeMethod(_bar, "OnEnable");
        TestReflectionUtil.InvokeMethod(_bar, "OnDisable");

        _image.fillAmount = 0.9f;
        EventBus.RaisePlayerPostureChanged(15f, 100f);

        Assert.AreEqual(0.9f, _image.fillAmount, 0.0001f);
    }
}
