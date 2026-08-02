using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// PlayerSaveDto round-trip (charter Step 11). Per Research finding 9's explicit warning:
// JsonUtility.FromJson<T>("{}") runs field initializers, so a round-trip test asserting only
// e.g. stats.Count == 4 can pass against total data loss (a fresh PlayerData already has that
// count via its own field initializer). Every assertion below targets a VALUE that provably
// differs from PlayerData's own constructor defaults, and one test asserts on the raw JSON
// string for a Dictionary-backed field (stats) to prove it is actually being written, not
// just silently dropped the way JsonUtility drops bare Dictionary fields.
public class PlayerSaveDtoTests
{
    private ItemData _ore;

    [SetUp]
    public void SetUp()
    {
        _ore = ScriptableObject.CreateInstance<ItemData>();
        _ore.itemId = "tamahagane_ore";
        _ore.maxStack = 99;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_ore);
    }

    private PlayerData BuildNonDefaultPlayerData()
    {
        var data = new PlayerData
        {
            saveVersion = 1,
            ngCycle = 2,
            level = 17,
            expTotal = 543.5f,
            expUnbanked = 12.25f,
            mon = 999,
            statPointsUnspent = 3,
            currentRegionId = "act_ii_sunken_pines",
        };

        // Defaults are all 0 -- overwrite with values that prove the flatten/reconstruct
        // path actually moved data, not just preserved a fresh object's own defaults.
        data.stats["body"] = 7;
        data.stats["breath"] = 4;
        data.stats["blade"] = 9;
        data.stats["spirit"] = 2;

        data.discoveredShrines.Add("prologue_entrance");
        data.discoveredShrines.Add("act_i_shrine");

        data.bossesDefeated.Add("captain_renzo");
        data.lootedContainers.Add("chest_001");

        data.worldFlags["met_soren"] = true;
        data.worldFlags["gate_open"] = false;

        data.inventory.AddItem(_ore, 5);

        data.equippedCharms.Add("omamori_wind");

        data.questStates["main_quest"] = 3;
        data.dialogueSeen.Add("intro_line_1");
        data.npcStates["blacksmith"] = "relocated";

        return data;
    }

    [Test]
    public void RoundTrip_ScalarFields_PreserveNonDefaultValues()
    {
        PlayerData original = BuildNonDefaultPlayerData();

        PlayerSaveDto dto = PlayerSaveDto.FromPlayerData(original);
        string json = JsonUtility.ToJson(dto);
        PlayerSaveDto reloadedDto = JsonUtility.FromJson<PlayerSaveDto>(json);
        PlayerData reloaded = reloadedDto.ToPlayerData(null);

        Assert.AreEqual(1, reloaded.saveVersion);
        Assert.AreEqual(2, reloaded.ngCycle);
        Assert.AreEqual(17, reloaded.level);
        Assert.AreEqual(543.5f, reloaded.expTotal);
        Assert.AreEqual(12.25f, reloaded.expUnbanked);
        Assert.AreEqual(999, reloaded.mon);
        Assert.AreEqual(3, reloaded.statPointsUnspent);
        Assert.AreEqual("act_ii_sunken_pines", reloaded.currentRegionId);
    }

    [Test]
    public void RoundTrip_StatsDictionary_PreservesNonDefaultValue()
    {
        PlayerData original = BuildNonDefaultPlayerData();

        PlayerSaveDto dto = PlayerSaveDto.FromPlayerData(original);
        string json = JsonUtility.ToJson(dto);
        PlayerData reloaded = JsonUtility.FromJson<PlayerSaveDto>(json).ToPlayerData(null);

        // A fresh PlayerData already has stats["body"] == 0 -- asserting == 7 (not just
        // Count == 4) is what actually proves the flattened List<StatEntry> round-tripped
        // real data rather than a default-initialized dictionary.
        Assert.AreEqual(7, reloaded.stats["body"]);
        Assert.AreEqual(4, reloaded.stats["breath"]);
        Assert.AreEqual(9, reloaded.stats["blade"]);
        Assert.AreEqual(2, reloaded.stats["spirit"]);
    }

    [Test]
    public void ToJson_StatsList_ActuallyAppearsInRawJson()
    {
        PlayerData original = BuildNonDefaultPlayerData();
        PlayerSaveDto dto = PlayerSaveDto.FromPlayerData(original);

        string json = JsonUtility.ToJson(dto);

        // Direct proof the Dictionary->List<StatEntry> flatten actually serialized -- not just
        // that a post-round-trip C# object happens to hold the right value.
        StringAssert.Contains("\"key\":\"body\"", json);
        StringAssert.Contains("\"value\":7", json);
    }

    [Test]
    public void RoundTrip_WorldFlagsDictionary_PreservesBothTrueAndFalse()
    {
        PlayerData original = BuildNonDefaultPlayerData();
        PlayerData reloaded = JsonUtility.FromJson<PlayerSaveDto>(JsonUtility.ToJson(PlayerSaveDto.FromPlayerData(original))).ToPlayerData(null);

        Assert.IsTrue(reloaded.worldFlags["met_soren"]);
        Assert.IsFalse(reloaded.worldFlags["gate_open"]);
    }

    [Test]
    public void RoundTrip_ListFields_PreserveContents()
    {
        PlayerData original = BuildNonDefaultPlayerData();
        PlayerData reloaded = JsonUtility.FromJson<PlayerSaveDto>(JsonUtility.ToJson(PlayerSaveDto.FromPlayerData(original))).ToPlayerData(null);

        CollectionAssert.AreEquivalent(new[] { "prologue_entrance", "act_i_shrine" }, reloaded.discoveredShrines);
        CollectionAssert.AreEqual(new[] { "captain_renzo" }, reloaded.bossesDefeated);
        CollectionAssert.AreEqual(new[] { "chest_001" }, reloaded.lootedContainers);
        CollectionAssert.AreEqual(new[] { "omamori_wind" }, reloaded.equippedCharms);
        CollectionAssert.AreEqual(new[] { "intro_line_1" }, reloaded.dialogueSeen);
    }

    [Test]
    public void RoundTrip_QuestAndNpcState_PreserveValues()
    {
        PlayerData original = BuildNonDefaultPlayerData();
        PlayerData reloaded = JsonUtility.FromJson<PlayerSaveDto>(JsonUtility.ToJson(PlayerSaveDto.FromPlayerData(original))).ToPlayerData(null);

        Assert.AreEqual(3, reloaded.questStates["main_quest"]);
        Assert.AreEqual("relocated", reloaded.npcStates["blacksmith"]);
    }

    [Test]
    public void ToPlayerData_WithItemDatabase_ResolvesInventoryItemId()
    {
        var db = ScriptableObject.CreateInstance<ItemDatabase>();
        var field = typeof(ItemDatabase).GetField("items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(db, new List<ItemData> { _ore });
        db.Rebuild();

        PlayerData original = BuildNonDefaultPlayerData();
        PlayerSaveDto dto = PlayerSaveDto.FromPlayerData(original);

        PlayerData reloaded = dto.ToPlayerData(db);

        Assert.IsTrue(reloaded.inventory.HasItem("tamahagane_ore"));
        Assert.AreEqual(5, reloaded.inventory.stacks[0].quantity);

        Object.DestroyImmediate(db);
    }

    [Test]
    public void ToPlayerData_WithNullItemDatabase_InventoryIsEmptyButDoesNotThrow()
    {
        PlayerData original = BuildNonDefaultPlayerData();
        PlayerSaveDto dto = PlayerSaveDto.FromPlayerData(original);

        PlayerData reloaded = null;
        Assert.DoesNotThrow(() => reloaded = dto.ToPlayerData(null));

        Assert.AreEqual(0, reloaded.inventory.stacks.Count);
    }

    [Test]
    public void FromPlayerData_FreshPlayerData_DoesNotThrow_AndProducesEmptyCollections()
    {
        var fresh = new PlayerData();

        PlayerSaveDto dto = null;
        Assert.DoesNotThrow(() => dto = PlayerSaveDto.FromPlayerData(fresh));

        Assert.AreEqual(0, dto.discoveredShrines.Count);
        Assert.AreEqual(0, dto.inventory.Count);
        Assert.AreEqual(0, dto.equippedCharms.Count);
    }
}
