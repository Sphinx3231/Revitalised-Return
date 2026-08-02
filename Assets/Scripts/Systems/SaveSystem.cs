using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Persistence engine (charter Step 11). Instance class taking the save-root directory as a
/// constructor parameter (Dependency Inversion, not a test-only hack -- Research finding 3):
/// static default is Application.persistentDataPath/saves, EditMode tests inject a temp
/// directory instead so they never touch the real user save folder.
///
/// Atomic write (Research finding 4, verified live on this Windows machine): a `.tmp` file is
/// written first. First save for a slot has no live file yet to preserve/rotate to `.bak`, so
/// it falls back to a plain File.Move; every subsequent save uses
/// File.Replace(tmp, live, bak, ignoreMetadataErrors:true), which does live&lt;-tmp and old
/// live-&gt;.bak as a single filesystem rename operation. The 3-arg File.Move(src,dst,overwrite)
/// overload does NOT exist at this project's .NET Standard 2.0 API level -- do not
/// reintroduce it.
///
/// Static Current/CurrentPlayerData/CurrentSlot properties are a minimal cross-cutting-state
/// holder, matching the established GameState.Instance-style static-singleton convention
/// (CLAUDE.md Section 2), so gameplay systems like Shrine can reach "the active save" without
/// a direct reference chain. This does NOT replace the ctor-injectable instance API above --
/// tests construct their own SaveSystem against a temp root and never touch these statics.
/// </summary>
public sealed class SaveSystem
{
    public const int SlotCount = 3;

    public static string DefaultRoot => Path.Combine(Application.persistentDataPath, "saves");

    public static SaveSystem Current { get; set; }
    public static PlayerData CurrentPlayerData { get; set; }
    public static int CurrentSlot { get; set; }

    private readonly string _root;

    public SaveSystem(string root = null)
    {
        _root = string.IsNullOrEmpty(root) ? DefaultRoot : root;
        Directory.CreateDirectory(_root);
    }

    private string SlotPath(int slot) => Path.Combine(_root, $"slot{slot}.json");
    private string TmpPath(int slot) => Path.Combine(_root, $"slot{slot}.json.tmp");
    private string BakPath(int slot) => Path.Combine(_root, $"slot{slot}.json.bak");

    /// <summary>Serializes `data` (via PlayerSaveDto) and writes it atomically to `slot`.</summary>
    public void Save(int slot, PlayerData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        string json = JsonUtility.ToJson(PlayerSaveDto.FromPlayerData(data), true);

        string live = SlotPath(slot);
        string tmp = TmpPath(slot);
        string bak = BakPath(slot);

        File.WriteAllText(tmp, json);

        if (!File.Exists(live))
        {
            // First save for this slot -- nothing to preserve as a .bak yet.
            File.Move(tmp, live);
        }
        else
        {
            File.Replace(tmp, live, bak, ignoreMetadataErrors: true);
        }
    }

    /// <summary>
    /// Deserializes `slot`'s save file, resolving inventory itemIds via `itemDatabase` (may be
    /// null -- see PlayerSaveDto.ToPlayerData). Returns null if the slot is empty -- an empty
    /// slot is a normal, expected state at the main menu, not an error condition, so this
    /// deliberately does not throw (Director ruling, matches PeekSlot's own exists=false shape).
    /// </summary>
    public PlayerData Load(int slot, ItemDatabase itemDatabase)
    {
        string live = SlotPath(slot);
        if (!File.Exists(live))
            return null;

        string json = File.ReadAllText(live);
        PlayerSaveDto dto = JsonUtility.FromJson<PlayerSaveDto>(json);
        return dto.ToPlayerData(itemDatabase);
    }

    /// <summary>
    /// Cheap metadata projection for slot-select UI -- does not resolve inventory (no
    /// ItemDatabase dependency here on purpose, PeekSlot only reads scalar fields).
    /// lastPlayedUtc comes from filesystem metadata (File.GetLastWriteTimeUtc), NOT from save
    /// data -- Director ruling, no playtime/timestamp field was added to PlayerData itself.
    /// </summary>
    public SaveSlotInfo PeekSlot(int slot)
    {
        string live = SlotPath(slot);
        if (!File.Exists(live))
            return new SaveSlotInfo { exists = false };

        string json = File.ReadAllText(live);
        PlayerSaveDto dto = JsonUtility.FromJson<PlayerSaveDto>(json);

        return new SaveSlotInfo
        {
            exists = true,
            level = dto.level,
            regionId = dto.currentRegionId,
            lastPlayedUtc = File.GetLastWriteTimeUtc(live),
        };
    }
}

[Serializable]
public struct SaveSlotInfo
{
    public bool exists;
    public int level;
    public string regionId;
    public DateTime lastPlayedUtc;
}
