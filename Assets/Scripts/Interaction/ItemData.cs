using UnityEngine;

/// <summary>
/// Authored, asset-time item definition (charter Step 10). ScriptableObject per
/// StanceData/RegionGraph's [CreateAssetMenu] precedent. `description` is called out by the
/// charter as "a first-class narrative surface" -- not an afterthought field. `Mon` is a
/// scalar int on the future PlayerData, never modeled as an ItemStack -- do not add a
/// currency ItemData.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "Return/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public ItemCategory category;
    public int maxStack = 1;
    public int valueMon;
    public string regionTag;

    [TextArea]
    public string description;
}
