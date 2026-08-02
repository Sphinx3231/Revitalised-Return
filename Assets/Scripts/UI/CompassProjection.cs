using UnityEngine;

/// <summary>
/// Pure static projection math for the top-centre compass strip (charter Step 11, Research
/// finding 5). Deliberately a static class with no MonoBehaviour dependency -- cheap to unit
/// test exhaustively, including the wrap-around case a naive `bearing - yaw` subtraction gets
/// wrong (e.g. player yaw 10 deg, target bearing 350 deg -- the correct delta is -20 deg, not
/// -340 deg). Mathf.DeltaAngle handles that wrap for free.
/// </summary>
public static class CompassProjection
{
    /// <summary>
    /// Pixel X offset (relative to the strip's centre) for a marker at `targetBearingDegrees`,
    /// given the player's current `playerYawDegrees` facing, the strip's half field-of-view in
    /// degrees, and the strip's half-width in pixels. Linear projection: a marker exactly at
    /// the edge of the FOV lands exactly at the strip's edge.
    /// </summary>
    public static float MarkerOffsetX(float playerYawDegrees, float targetBearingDegrees, float halfFovDegrees, float halfStripWidthPixels)
    {
        float delta = Mathf.DeltaAngle(playerYawDegrees, targetBearingDegrees);

        if (halfFovDegrees <= 0f)
            return 0f;

        return (delta / halfFovDegrees) * halfStripWidthPixels;
    }

    /// <summary>True if `targetBearingDegrees` currently falls within the visible field of view.</summary>
    public static bool IsVisible(float playerYawDegrees, float targetBearingDegrees, float halfFovDegrees)
    {
        float delta = Mathf.DeltaAngle(playerYawDegrees, targetBearingDegrees);
        return Mathf.Abs(delta) <= halfFovDegrees;
    }
}
