using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// ItemDatabase's itemId -> ItemData lookup (charter Step 11, Research finding 2a). Matches
// Inventory.cs's own _index/Rebuild() pattern -- tests cover lookup hit/miss, the lazy-rebuild
// fallback (ScriptableObject.CreateInstance bypasses OnEnable timing in some editor contexts),
// and malformed-entry tolerance (null item / empty itemId are skipped, not thrown on).
public class ItemDatabaseTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    private ItemData _ore;
    private ItemData _sprig;
    private ItemDatabase _db;

    [SetUp]
    public void SetUp()
    {
        _ore = ScriptableObject.CreateInstance<ItemData>();
        _ore.itemId = "tamahagane_ore";

        _sprig = ScriptableObject.CreateInstance<ItemData>();
        _sprig.itemId = "ashroot_sprig";

        _db = ScriptableObject.CreateInstance<ItemDatabase>();
        typeof(ItemDatabase).GetField("items", NonPublicInstance).SetValue(_db, new List<ItemData> { _ore, _sprig });
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_ore);
        Object.DestroyImmediate(_sprig);
        Object.DestroyImmediate(_db);
    }

    [Test]
    public void Lookup_KnownItemId_ReturnsCorrectItemData()
    {
        _db.Rebuild();

        Assert.AreSame(_ore, _db.Lookup("tamahagane_ore"));
        Assert.AreSame(_sprig, _db.Lookup("ashroot_sprig"));
    }

    [Test]
    public void Lookup_UnknownItemId_ReturnsNull()
    {
        _db.Rebuild();

        Assert.IsNull(_db.Lookup("does_not_exist"));
    }

    [Test]
    public void Lookup_NullOrEmptyId_ReturnsNull()
    {
        _db.Rebuild();

        Assert.IsNull(_db.Lookup(null));
        Assert.IsNull(_db.Lookup(string.Empty));
    }

    [Test]
    public void Lookup_IndexNull_LazilyRebuildsFromCurrentItemsList()
    {
        // ScriptableObject.CreateInstance() runs OnEnable() synchronously (verified live --
        // by the time SetUp's reflection assigns `items`, an empty index already exists from
        // that early OnEnable call), so this test forces the lazy-rebuild branch directly by
        // nulling the private index field, rather than relying on OnEnable timing.
        typeof(ItemDatabase).GetField("_index", NonPublicInstance).SetValue(_db, null);

        Assert.AreSame(_ore, _db.Lookup("tamahagane_ore"), "Lookup must rebuild the index on demand when it is null, not return null forever.");
    }

    [Test]
    public void Rebuild_SkipsNullOrEmptyIdEntries_DoesNotThrow()
    {
        typeof(ItemDatabase).GetField("items", NonPublicInstance).SetValue(_db, new List<ItemData> { _ore, null });

        Assert.DoesNotThrow(() => _db.Rebuild());
        Assert.AreSame(_ore, _db.Lookup("tamahagane_ore"));
    }

    [Test]
    public void Rebuild_CalledTwice_LatestListWins()
    {
        _db.Rebuild();
        Assert.AreSame(_sprig, _db.Lookup("ashroot_sprig"));

        typeof(ItemDatabase).GetField("items", NonPublicInstance).SetValue(_db, new List<ItemData> { _ore });
        _db.Rebuild();

        Assert.IsNull(_db.Lookup("ashroot_sprig"), "After rebuilding with a smaller list, stale entries must not remain looked-up-able.");
        Assert.AreSame(_ore, _db.Lookup("tamahagane_ore"));
    }
}
