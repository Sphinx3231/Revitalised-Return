using NUnit.Framework;
using UnityEngine;

public class MainMenuControllerTests
{
    private GameObject _controllerGo;
    private MainMenuController _controller;
    private GameObject _panelGo;

    [SetUp]
    public void SetUp()
    {
        _controllerGo = new GameObject("MainMenuController");
        _controller = _controllerGo.AddComponent<MainMenuController>();

        _panelGo = new GameObject("SettingsPanel");
        _panelGo.SetActive(true);

        TestReflectionUtil.SetField(_controller, "settingsPanel", _panelGo);
    }

    [TearDown]
    public void TearDown()
    {
        if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
        if (_panelGo != null) Object.DestroyImmediate(_panelGo);
    }

    [Test]
    public void ToggleSettings_TogglesPanelActiveState()
    {
        Assert.IsTrue(_panelGo.activeSelf);

        _controller.ToggleSettings();
        Assert.IsFalse(_panelGo.activeSelf);

        _controller.ToggleSettings();
        Assert.IsTrue(_panelGo.activeSelf);
    }

    [Test]
    public void ToggleSettings_NullPanel_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_controller, "settingsPanel", null);
        Assert.DoesNotThrow(() => _controller.ToggleSettings());
    }
}
