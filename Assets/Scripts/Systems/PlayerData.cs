using System.Collections.Generic;

/// <summary>
/// Runtime save-state shape (charter Step 11's locked field list). Plain C# class, NOT a
/// ScriptableObject -- this is per-playthrough runtime state, not asset-time authored data
/// (matches the charter's own explicit "PlayerData is a plain C# class... not a
/// ScriptableObject, those are asset-time data" ruling).
///
/// This class is never serialized directly -- JsonUtility cannot round-trip Dictionary&lt;K,V&gt;
/// fields or the ItemData ScriptableObject reference inside Inventory's ItemStacks (verified
/// live, see docs/Tasks/2026-08-02-step-11-hud-persistence.md Research finding 1).
/// PlayerSaveDto.FromPlayerData/ToPlayerData is the only supported (de)serialization path.
///
/// discoveredShrines is a HashSet&lt;string&gt; (not List) -- discovery is a pure membership
/// question (visited or not), never ordered/indexed, so a HashSet documents that intent
/// directly and makes double-adds a no-op for free.
///
/// Fields with no backing system yet -- equippedCharms (Step 10 charm-equip / Step 12
/// territory), questStates/dialogueSeen/npcStates (Step 12 territory) -- get their locked
/// types now per the charter so the save format never needs a breaking migration later, but
/// they serialize/round-trip empty until those systems exist. Not silently omitted.
/// </summary>
public class PlayerData
{
    public int saveVersion = 1;
    public int ngCycle = 0;

    // Progression
    public int level = 1;
    public float expTotal;
    public float expUnbanked;
    public int mon;
    public int statPointsUnspent;
    public Dictionary<string, int> stats = new Dictionary<string, int>
    {
        { "body", 0 },
        { "breath", 0 },
        { "blade", 0 },
        { "spirit", 0 },
    };

    // World state
    public string currentRegionId;
    public HashSet<string> discoveredShrines = new HashSet<string>();
    public List<string> bossesDefeated = new List<string>();
    public List<string> lootedContainers = new List<string>();
    public Dictionary<string, bool> worldFlags = new Dictionary<string, bool>();

    public Inventory inventory = new Inventory();

    // No charm system exists yet (Step 10 charm-equip / Step 12 territory) -- locked type
    // now, serializes empty.
    public List<string> equippedCharms = new List<string>();

    // No quest/dialogue/npc systems exist yet (Step 12 territory) -- locked types now,
    // serialize empty.
    public Dictionary<string, int> questStates = new Dictionary<string, int>();
    public List<string> dialogueSeen = new List<string>();
    public Dictionary<string, string> npcStates = new Dictionary<string, string>();
}
