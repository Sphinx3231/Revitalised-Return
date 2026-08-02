using UnityEngine;

/// <summary>
/// Thin MonoBehaviour wrapper so a plain-C#-class Inventory can be referenced from the
/// Inspector (e.g. Chest.targetInventory -> the Player's InventoryHolder) and exist on a
/// live GameObject. Owns no logic itself -- purely a scene-wiring adapter, per the same
/// "MonoBehaviour adapter over plain data" pattern PlayerInputReader uses for InputBuffer.
/// </summary>
public sealed class InventoryHolder : MonoBehaviour
{
    public Inventory Inventory = new Inventory();
}
