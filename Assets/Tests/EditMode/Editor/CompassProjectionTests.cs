using NUnit.Framework;

// CompassProjection's pure static math (charter Step 11, Research finding 5). Covers the
// wrap-around case a naive `bearing - yaw` subtraction gets wrong (player yaw 10 deg, target
// bearing 350 deg -- correct delta is -20 deg, not -340 deg), off-screen exclusion, and the
// 4 fixed cardinal marker positions the CompassStrip component places every frame.
public class CompassProjectionTests
{
    private const float HalfFov = 60f;
    private const float HalfStripWidth = 200f;

    [Test]
    public void IsVisible_TargetDirectlyAhead_ReturnsTrue()
    {
        Assert.IsTrue(CompassProjection.IsVisible(playerYawDegrees: 0f, targetBearingDegrees: 0f, HalfFov));
    }

    [Test]
    public void IsVisible_TargetAtExactHalfFovEdge_ReturnsTrue()
    {
        Assert.IsTrue(CompassProjection.IsVisible(playerYawDegrees: 0f, targetBearingDegrees: 60f, HalfFov));
    }

    [Test]
    public void IsVisible_TargetJustBeyondHalfFov_ReturnsFalse()
    {
        Assert.IsFalse(CompassProjection.IsVisible(playerYawDegrees: 0f, targetBearingDegrees: 61f, HalfFov));
    }

    [Test]
    public void IsVisible_TargetBehindPlayer_ReturnsFalse()
    {
        Assert.IsFalse(CompassProjection.IsVisible(playerYawDegrees: 0f, targetBearingDegrees: 180f, HalfFov));
    }

    [Test]
    public void IsVisible_WrapAroundCase_PlayerYaw10_TargetBearing350_IsWithinFov()
    {
        // Naive (bearing - yaw) gives -340 deg (way outside FOV); the correct wrapped delta
        // is -20 deg, which IS within a 60-degree half-FOV.
        Assert.IsTrue(CompassProjection.IsVisible(playerYawDegrees: 10f, targetBearingDegrees: 350f, HalfFov));
    }

    [Test]
    public void MarkerOffsetX_TargetDirectlyAhead_IsZero()
    {
        float x = CompassProjection.MarkerOffsetX(playerYawDegrees: 0f, targetBearingDegrees: 0f, HalfFov, HalfStripWidth);

        Assert.AreEqual(0f, x, 0.001f);
    }

    [Test]
    public void MarkerOffsetX_TargetAtHalfFovEdge_IsAtStripEdge()
    {
        float x = CompassProjection.MarkerOffsetX(playerYawDegrees: 0f, targetBearingDegrees: HalfFov, HalfFov, HalfStripWidth);

        Assert.AreEqual(HalfStripWidth, x, 0.01f);
    }

    [Test]
    public void MarkerOffsetX_TargetAtNegativeHalfFovEdge_IsAtNegativeStripEdge()
    {
        float x = CompassProjection.MarkerOffsetX(playerYawDegrees: 0f, targetBearingDegrees: -HalfFov, HalfFov, HalfStripWidth);

        Assert.AreEqual(-HalfStripWidth, x, 0.01f);
    }

    [Test]
    public void MarkerOffsetX_WrapAroundCase_PlayerYaw10_TargetBearing350_ProjectsNegative()
    {
        // Delta is -20 deg (target is slightly to the player's left), so the marker must
        // project to a negative (left-of-centre) offset, not a huge positive one from a
        // naive unwrapped subtraction.
        float x = CompassProjection.MarkerOffsetX(playerYawDegrees: 10f, targetBearingDegrees: 350f, HalfFov, HalfStripWidth);

        Assert.Less(x, 0f);
        Assert.Greater(x, -HalfStripWidth);
    }

    [Test]
    public void MarkerOffsetX_ZeroHalfFov_ReturnsZero_DoesNotDivideByZero()
    {
        float x = 0f;
        Assert.DoesNotThrow(() => x = CompassProjection.MarkerOffsetX(0f, 45f, 0f, HalfStripWidth));
        Assert.AreEqual(0f, x);
    }

    // --- Cardinal markers, as placed by CompassStrip every frame ---

    [Test]
    public void Cardinals_PlayerFacingNorth_NorthIsVisibleAtCentre_SouthIsNotVisible()
    {
        Assert.IsTrue(CompassProjection.IsVisible(0f, 0f, HalfFov));
        Assert.AreEqual(0f, CompassProjection.MarkerOffsetX(0f, 0f, HalfFov, HalfStripWidth), 0.001f);

        Assert.IsFalse(CompassProjection.IsVisible(0f, 180f, HalfFov));
    }

    [Test]
    public void Cardinals_PlayerFacingEast_EastIsVisibleAtCentre()
    {
        Assert.IsTrue(CompassProjection.IsVisible(90f, 90f, HalfFov));
        Assert.AreEqual(0f, CompassProjection.MarkerOffsetX(90f, 90f, HalfFov, HalfStripWidth), 0.001f);
    }

    [Test]
    public void Cardinals_PlayerFacingNorth_EastIsJustOutsideFov()
    {
        // East bearing (90) is 90 degrees off a north-facing (0) player's forward -- outside
        // the default 60-degree half-FOV.
        Assert.IsFalse(CompassProjection.IsVisible(0f, 90f, HalfFov));
    }
}
