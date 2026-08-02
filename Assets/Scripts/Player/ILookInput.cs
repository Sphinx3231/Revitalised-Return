using UnityEngine;

/// <summary>
/// Dependency-inversion seam: anything that needs raw 2D look input (mouse delta /
/// gamepad right-stick) depends on this interface, not on a concrete input-reading
/// MonoBehaviour. Kept separate from IMovementInput (Interface Segregation) — a
/// consumer that only needs movement input shouldn't be forced to also depend on look.
/// </summary>
public interface ILookInput
{
    Vector2 LookRaw { get; }
}
