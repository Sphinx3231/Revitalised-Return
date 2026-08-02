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
/// dialogueSeen is also a HashSet&lt;string&gt; for the exact same reason (Step 12 fix, closing a
/// defect this class's own doc comment predicted at Step 11: "seen or not" is a pure membership
/// question, never ordered/indexed -- a List meant unbounded duplicate growth + O(n) Contains
/// checks over a 20-hour campaign). PlayerSaveDto's on-disk format is unchanged -- the DTO field
/// stays List&lt;string&gt; (JsonUtility needs a List on the wire), only this runtime field's type
/// changed, same List&lt;-&gt;HashSet conversion idiom discoveredShrines already uses.
///
/// Fields with no backing system yet -- equippedCharms (Step 10 charm-equip territory) -- get
/// its locked type now per the charter so the save format never needs a breaking migration
/// later, but it serializes/round-trips empty until that system exists. Not silently omitted.
/// questStates/dialogueSeen/npcStates are Step 12's real, now-implemented systems.
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

    // Quest/dialogue/npc state (Step 12).
    public Dictionary<string, int> questStates = new Dictionary<string, int>();
    public HashSet<string> dialogueSeen = new HashSet<string>();
    public Dictionary<string, string> npcStates = new Dictionary<string, string>();
}
