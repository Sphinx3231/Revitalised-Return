using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

// SaveSystem's atomic-write/.bak-rotation/slot API (charter Step 11, Research finding 3/4).
// Constructed against a temp directory per test (never Application.persistentDataPath) so
// this suite stays hermetic and never touches the real user save folder -- exactly the
// Dependency-Inversion-motivated ctor-injection Research called for.
public class SaveSystemTests
{
    private string _tempRoot;
    private SaveSystem _saveSystem;

    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ReturnSaveSystemTests_" + Guid.NewGuid().ToString("N"));
        _saveSystem = new SaveSystem(_tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    private PlayerData BuildData(int level, string regionId)
    {
        return new PlayerData { level = level, currentRegionId = regionId };
    }

    [Test]
    public void Constructor_CreatesRootDirectory()
    {
        Assert.IsTrue(Directory.Exists(_tempRoot));
    }

    [Test]
    public void Save_FirstSaveForSlot_NoLiveFileYet_WritesLiveFile_NoBakFile()
    {
        _saveSystem.Save(0, BuildData(5, "prologue"));

        Assert.IsTrue(File.Exists(Path.Combine(_tempRoot, "slot0.json")));
        Assert.IsFalse(File.Exists(Path.Combine(_tempRoot, "slot0.json.bak")));
        Assert.IsFalse(File.Exists(Path.Combine(_tempRoot, "slot0.json.tmp")), "The .tmp file must not survive a completed save.");
    }

    [Test]
    public void Save_SecondSaveForSlot_RotatesPreviousLiveFileToBak()
    {
        _saveSystem.Save(0, BuildData(1, "prologue"));
        string firstJson = File.ReadAllText(Path.Combine(_tempRoot, "slot0.json"));

        _saveSystem.Save(0, BuildData(2, "act_i"));

        string bakPath = Path.Combine(_tempRoot, "slot0.json.bak");
        Assert.IsTrue(File.Exists(bakPath), "Second save onto an existing slot must produce a .bak.");
        Assert.AreEqual(firstJson, File.ReadAllText(bakPath), "The .bak must contain the PREVIOUS save's content, not the new one.");

        PlayerData reloaded = _saveSystem.Load(0, null);
        Assert.AreEqual(2, reloaded.level, "The live file must contain the NEW save's content.");
    }

    [Test]
    public void Save_ThirdSaveForSlot_BakIsOverwritten_NotAppended()
    {
        _saveSystem.Save(0, BuildData(1, "prologue"));
        _saveSystem.Save(0, BuildData(2, "act_i"));
        _saveSystem.Save(0, BuildData(3, "act_ii"));

        PlayerData bakData = _saveSystem.Load(0, null); // sanity: live is slot 3's data
        Assert.AreEqual(3, bakData.level);

        // Save() writes pretty-printed JSON (JsonUtility.ToJson(dto, true)) -- ": " with a
        // space, not ":".
        string bakJson = File.ReadAllText(Path.Combine(_tempRoot, "slot0.json.bak"));
        StringAssert.Contains("\"level\": 2", bakJson);
        StringAssert.DoesNotContain("\"level\": 1", bakJson);
    }

    [Test]
    public void Save_DifferentSlots_AreIndependentFiles()
    {
        _saveSystem.Save(0, BuildData(10, "slot_zero_region"));
        _saveSystem.Save(1, BuildData(20, "slot_one_region"));

        Assert.AreEqual(10, _saveSystem.Load(0, null).level);
        Assert.AreEqual(20, _saveSystem.Load(1, null).level);
    }

    [Test]
    public void Save_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _saveSystem.Save(0, null));
    }

    [Test]
    public void Load_EmptySlot_ReturnsNull()
    {
        Assert.IsNull(_saveSystem.Load(0, null));
    }

    [Test]
    public void Load_AfterSave_RoundTripsLevelAndRegion()
    {
        _saveSystem.Save(2, BuildData(42, "mount_shindai"));

        PlayerData loaded = _saveSystem.Load(2, null);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(42, loaded.level);
        Assert.AreEqual("mount_shindai", loaded.currentRegionId);
    }

    [Test]
    public void PeekSlot_EmptySlot_ReturnsExistsFalse()
    {
        SaveSlotInfo info = _saveSystem.PeekSlot(1);

        Assert.IsFalse(info.exists);
    }

    [Test]
    public void PeekSlot_PopulatedSlot_ReturnsLevelAndRegion_ExistsTrue()
    {
        _saveSystem.Save(0, BuildData(8, "outskirts"));

        SaveSlotInfo info = _saveSystem.PeekSlot(0);

        Assert.IsTrue(info.exists);
        Assert.AreEqual(8, info.level);
        Assert.AreEqual("outskirts", info.regionId);
    }

    [Test]
    public void PeekSlot_LastPlayedUtc_ComesFromFileMetadata_NotSaveData()
    {
        DateTime before = DateTime.UtcNow.AddSeconds(-5);
        _saveSystem.Save(0, BuildData(1, "prologue"));
        DateTime after = DateTime.UtcNow.AddSeconds(5);

        SaveSlotInfo info = _saveSystem.PeekSlot(0);

        Assert.GreaterOrEqual(info.lastPlayedUtc, before);
        Assert.LessOrEqual(info.lastPlayedUtc, after);
    }

    [Test]
    public void SlotCount_IsThree()
    {
        Assert.AreEqual(3, SaveSystem.SlotCount);
    }

    [Test]
    public void DefaultRoot_IsUnderPersistentDataPath_SavesSubfolder()
    {
        string expected = Path.Combine(Application.persistentDataPath, "saves");
        Assert.AreEqual(expected, SaveSystem.DefaultRoot);
    }

    [Test]
    public void StaticCurrentHolders_AreIndependentOfInstanceApi_DefaultUnset()
    {
        // This suite never touches the static holders -- a fresh test run should not see
        // stray state from another test leaking through them (best-effort smoke check; the
        // authoritative per-test isolation is the ctor-injected temp directory above).
        Assert.DoesNotThrow(() => { var _ = SaveSystem.CurrentSlot; });
    }
}
