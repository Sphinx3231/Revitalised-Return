using UnityEngine;

/// <summary>
/// Dependency-inversion seam: anything that needs raw 2D movement input depends on
/// this interface, not on a concrete input-reading MonoBehaviour.
/// </summary>
public interface IMovementInput
{
    Vector2 MoveRaw { get; }
}
