using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// MainMenu.unity slot-select panel (charter Step 11). Targets RefreshLabels()'s
// empty-vs-populated label formatting and SelectSlot()'s load/new-game branch logic (QA
// finding, docs/Tasks/2026-08-02-step-11-hud-persistence.md) -- the real logic-bearing pieces,
// not just Open()/Close() constructor coverage. Uses a temp-directory-backed SaveSystem
// instance (same hermetic pattern as SaveSystemTests), injected over the field Awake() would
// otherwise populate with the real Application.persistentDataPath-backed default.
public class SaveSlotMenuTests
{
    private GameObject _go;
    private SaveSlotMenu _menu;

    private GameObject _panelGo;
    private GameObject[] _labelGos;
    private Text[] _labels;

    private string _tempRoot;
    private SaveSystem _saveSystem;

    private SaveSystem _previousCurrent;
    private PlayerData _previousPlayerData;
    private int _previousSlot;

    [SetUp]
    public void SetUp()
    {
        _previousCurrent = SaveSystem.Current;
        _previousPlayerData = SaveSystem.CurrentPlayerData;
        _previousSlot = SaveSystem.CurrentSlot;

        _tempRoot = Path.Combine(Path.GetTempPath(), "ReturnSaveSlotMenuTests_" + Guid.NewGuid().ToString("N"));
        _saveSystem = new SaveSystem(_tempRoot);

        _go = new GameObject("SaveSlotMenu");
        _menu = _go.AddComponent<SaveSlotMenu>();
        // Bypass Awake()'s real `new SaveSystem()` (Application.persistentDataPath-backed) --
        // inject the hermetic temp-directory instance instead, same as every other field in
        // this suite.
        TestReflectionUtil.SetField(_menu, "_saveSystem", _saveSystem);

        _panelGo = new GameObject("SlotPanel");
        _panelGo.SetActive(false);
        TestReflectionUtil.SetField(_menu, "slotPanel", _panelGo);

        _labelGos = new GameObject[3];
        _labels = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            _labelGos[i] = new GameObject("Label" + i, typeof(RectTransform), typeof(Text));
            _labels[i] = _labelGos[i].GetComponent<Text>();
        }
        TestReflectionUtil.SetField(_menu, "slotLabels", _labels);
    }

    [TearDown]
    public void TearDown()
    {
        SaveSystem.Current = _previousCurrent;
        SaveSystem.CurrentPlayerData = _previousPlayerData;
        SaveSystem.CurrentSlot = _previousSlot;

        if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        if (_panelGo != null) UnityEngine.Object.DestroyImmediate(_panelGo);
        foreach (var g in _labelGos) if (g != null) UnityEngine.Object.DestroyImmediate(g);

        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    [Test]
    public void Awake_CreatesSaveSystemInstance()
    {
        var go = new GameObject("SaveSlotMenuAwake");
        var menu = go.AddComponent<SaveSlotMenu>();

        TestReflectionUtil.InvokeMethod(menu, "Awake");

        Assert.IsNotNull(TestReflectionUtil.GetField<SaveSystem>(menu, "_saveSystem"));

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Open_ActivatesPanel()
    {
        _menu.Open();

        Assert.IsTrue(_panelGo.activeSelf);
    }

    [Test]
    public void Open_NullPanel_DoesNotThrow_StillRefreshesLabels()
    {
        TestReflectionUtil.SetField(_menu, "slotPanel", null);

        Assert.DoesNotThrow(() => _menu.Open());
        StringAssert.Contains("Empty Slot", _labels[0].text);
    }

    [Test]
    public void Close_DeactivatesPanel()
    {
        _panelGo.SetActive(true);

        _menu.Close();

        Assert.IsFalse(_panelGo.activeSelf);
    }

    [Test]
    public void Close_NullPanel_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_menu, "slotPanel", null);

        Assert.DoesNotThrow(() => _menu.Close());
    }

    [Test]
    public void RefreshLabels_EmptySlots_ShowsEmptySlotText()
    {
        _menu.Open();

        Assert.AreEqual("Slot 1 - Empty Slot", _labels[0].text);
        Assert.AreEqual("Slot 2 - Empty Slot", _labels[1].text);
        Assert.AreEqual("Slot 3 - Empty Slot", _labels[2].text);
    }

    [Test]
    public void RefreshLabels_PopulatedSlot_ShowsLevelRegionAndDate()
    {
        _saveSystem.Save(1, new PlayerData { level = 5, currentRegionId = "ashlands" });
        SaveSlotInfo info = _saveSystem.PeekSlot(1);
        string expected = $"Slot 2 - Lv.5 - ashlands - {info.lastPlayedUtc:yyyy-MM-dd}";

        _menu.Open();

        Assert.AreEqual(expected, _labels[1].text);
    }

    [Test]
    public void RefreshLabels_NullSlotLabelsArray_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_menu, "slotLabels", null);

        Assert.DoesNotThrow(() => _menu.Open());
    }

    [Test]
    public void RefreshLabels_NullEntryInSlotLabelsArray_IsSkipped()
    {
        _labels[1] = null;
        TestReflectionUtil.SetField(_menu, "slotLabels", _labels);

        Assert.DoesNotThrow(() => _menu.Open());
        Assert.AreEqual("Slot 1 - Empty Slot", _labels[0].text);
        Assert.AreEqual("Slot 3 - Empty Slot", _labels[2].text);
    }

    [Test]
    public void SelectSlot_EmptySlot_PublishesFreshPlayerData()
    {
        _menu.SelectSlot(0);

        Assert.AreSame(_saveSystem, SaveSystem.Current);
        Assert.AreEqual(0, SaveSystem.CurrentSlot);
        Assert.IsNotNull(SaveSystem.CurrentPlayerData);
        Assert.AreEqual(1, SaveSystem.CurrentPlayerData.level, "An empty slot must publish a fresh (level 1, default) PlayerData -- new game.");
    }

    [Test]
    public void SelectSlot_PopulatedSlot_LoadsExistingPlayerData()
    {
        _saveSystem.Save(2, new PlayerData { level = 7, currentRegionId = "mount_shindai" });

        _menu.SelectSlot(2);

        Assert.AreSame(_saveSystem, SaveSystem.Current);
        Assert.AreEqual(2, SaveSystem.CurrentSlot);
        Assert.AreEqual(7, SaveSystem.CurrentPlayerData.level);
        Assert.AreEqual("mount_shindai", SaveSystem.CurrentPlayerData.currentRegionId);
    }

    [Test]
    public void SelectSlot_DifferentSlots_AreIndependent()
    {
        _saveSystem.Save(0, new PlayerData { level = 3, currentRegionId = "prologue" });
        _saveSystem.Save(1, new PlayerData { level = 9, currentRegionId = "sunken_pines" });

        _menu.SelectSlot(0);
        Assert.AreEqual(3, SaveSystem.CurrentPlayerData.level);

        _menu.SelectSlot(1);
        Assert.AreEqual(9, SaveSystem.CurrentPlayerData.level);
    }
}
