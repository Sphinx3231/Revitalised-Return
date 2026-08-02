using System;
using System.Collections.Generic;

/// <summary>
/// JsonUtility-safe wire format for PlayerData (charter Step 11, Research finding 1 /
/// Approach ruling). JsonUtility cannot round-trip Dictionary&lt;K,V&gt; fields (silently
/// omitted, not "{}" -- verified live) and Inventory's ItemStack.item is a ScriptableObject
/// reference whose instanceID is session-local garbage across runs regardless of serializer.
/// This DTO flattens every Dictionary-shaped field into a List of small [Serializable]
/// key/value entry structs, and Inventory into a List of (itemId, quantity) entries.
///
/// ToPlayerData needs an ItemDatabase to resolve a saved itemId back to a real ItemData
/// asset -- nothing else in the project can do that resolution (Research finding 2a).
/// </summary>
[Serializable]
public class PlayerSaveDto
{
    public int saveVersion;
    public int ngCycle;

    public int level;
    public float expTotal;
    public float expUnbanked;
    public int mon;
    public int statPointsUnspent;
    public List<StatEntry> stats = new List<StatEntry>();

    public string currentRegionId;
    public List<string> discoveredShrines = new List<string>();
    public List<string> bossesDefeated = new List<string>();
    public List<string> lootedContainers = new List<string>();
    public List<WorldFlagEntry> worldFlags = new List<WorldFlagEntry>();

    public List<ItemStackEntry> inventory = new List<ItemStackEntry>();

    public List<string> equippedCharms = new List<string>();

    public List<QuestStateEntry> questStates = new List<QuestStateEntry>();
    public List<string> dialogueSeen = new List<string>();
    public List<NpcStateEntry> npcStates = new List<NpcStateEntry>();

    [Serializable]
    public struct StatEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public struct WorldFlagEntry
    {
        public string key;
        public bool value;
    }

    [Serializable]
    public struct ItemStackEntry
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public struct QuestStateEntry
    {
        public string questId;
        public int state;
    }

    [Serializable]
    public struct NpcStateEntry
    {
        public string npcId;
        public string state;
    }

    public static PlayerSaveDto FromPlayerData(PlayerData data)
    {
        var dto = new PlayerSaveDto
        {
            saveVersion = data.saveVersion,
            ngCycle = data.ngCycle,
            level = data.level,
            expTotal = data.expTotal,
            expUnbanked = data.expUnbanked,
            mon = data.mon,
            statPointsUnspent = data.statPointsUnspent,
            currentRegionId = data.currentRegionId,
        };

        if (data.stats != null)
        {
            foreach (var kvp in data.stats)
                dto.stats.Add(new StatEntry { key = kvp.Key, value = kvp.Value });
        }

        if (data.discoveredShrines != null)
            dto.discoveredShrines.AddRange(data.discoveredShrines);

        if (data.bossesDefeated != null)
            dto.bossesDefeated.AddRange(data.bossesDefeated);

        if (data.lootedContainers != null)
            dto.lootedContainers.AddRange(data.lootedContainers);

        if (data.worldFlags != null)
        {
            foreach (var kvp in data.worldFlags)
                dto.worldFlags.Add(new WorldFlagEntry { key = kvp.Key, value = kvp.Value });
        }

        if (data.inventory != null && data.inventory.stacks != null)
        {
            foreach (var stack in data.inventory.stacks)
            {
                if (stack == null || stack.item == null || string.IsNullOrEmpty(stack.item.itemId))
                    continue;

                dto.inventory.Add(new ItemStackEntry { itemId = stack.item.itemId, quantity = stack.quantity });
            }
        }

        if (data.equippedCharms != null)
            dto.equippedCharms.AddRange(data.equippedCharms);

        if (data.questStates != null)
        {
            foreach (var kvp in data.questStates)
                dto.questStates.Add(new QuestStateEntry { questId = kvp.Key, state = kvp.Value });
        }

        if (data.dialogueSeen != null)
            dto.dialogueSeen.AddRange(data.dialogueSeen);

        if (data.npcStates != null)
        {
            foreach (var kvp in data.npcStates)
                dto.npcStates.Add(new NpcStateEntry { npcId = kvp.Key, state = kvp.Value });
        }

        return dto;
    }

    /// <summary>
    /// Reconstructs a PlayerData from this DTO. `itemDatabase` may be null -- in that case
    /// saved inventory entries silently fail to resolve (documented, not a throw: a caller
    /// with no database available, e.g. a menu-only context, still gets a valid PlayerData
    /// back for everything except inventory contents).
    /// </summary>
    public PlayerData ToPlayerData(ItemDatabase itemDatabase)
    {
        var data = new PlayerData
        {
            saveVersion = saveVersion,
            ngCycle = ngCycle,
            level = level,
            expTotal = expTotal,
            expUnbanked = expUnbanked,
            mon = mon,
            statPointsUnspent = statPointsUnspent,
            currentRegionId = currentRegionId,
        };

        data.stats.Clear();
        foreach (var entry in stats)
            data.stats[entry.key] = entry.value;

        data.discoveredShrines = new HashSet<string>(discoveredShrines);
        data.bossesDefeated = new List<string>(bossesDefeated);
        data.lootedContainers = new List<string>(lootedContainers);

        data.worldFlags.Clear();
        foreach (var entry in worldFlags)
            data.worldFlags[entry.key] = entry.value;

        data.inventory = new Inventory();
        if (itemDatabase != null)
        {
            foreach (var entry in inventory)
            {
                ItemData item = itemDatabase.Lookup(entry.itemId);
                if (item != null)
                    data.inventory.AddItem(item, entry.quantity);
            }
        }
        data.inventory.Rebuild();

        data.equippedCharms = new List<string>(equippedCharms);

        data.questStates.Clear();
        foreach (var entry in questStates)
            data.questStates[entry.questId] = entry.state;

        data.dialogueSeen = new List<string>(dialogueSeen);

        data.npcStates.Clear();
        foreach (var entry in npcStates)
            data.npcStates[entry.npcId] = entry.state;

        return data;
    }
}
