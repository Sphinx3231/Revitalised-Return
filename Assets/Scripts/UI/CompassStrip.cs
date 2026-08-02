using UnityEngine;

/// <summary>
/// Top-centre compass strip (charter Step 11 -- sightline navigation, no minimap). Places
/// fixed N/E/S/W cardinal markers (bearings 0/90/180/270) via the pure CompassProjection
/// functions. Reads playerTransform.eulerAngles.y directly rather than a PlayerLook
/// reference (Research finding 5 -- keeps the HUD decoupled from the specific look-input
/// implementation, same reasoning HealthBar etc. only ever depend on EventBus).
///
/// HUDRoot is confirmed thin (no shared tick pattern to hook into -- it owns zero Update()
/// logic and no per-frame orchestration of its children), so this ticks itself every frame
/// like a standalone HUD element, no different in spirit from StanceDiamond/NoticeDisplay's
/// own event-driven per-instance behavior.
/// </summary>
public class CompassStrip : MonoBehaviour
{
    private const float NorthBearing = 0f;
    private const float EastBearing = 90f;
    private const float SouthBearing = 180f;
    private const float WestBearing = 270f;

    [SerializeField] private Transform playerTransform;
    [SerializeField] private RectTransform stripRect;
    [SerializeField] private RectTransform northMarker;
    [SerializeField] private RectTransform eastMarker;
    [SerializeField] private RectTransform southMarker;
    [SerializeField] private RectTransform westMarker;
    [SerializeField] private float halfFovDegrees = 60f;

    private void Update()
    {
        if (playerTransform == null || stripRect == null)
            return;

        float yaw = playerTransform.eulerAngles.y;
        float halfWidth = stripRect.rect.width * 0.5f;

        PlaceMarker(northMarker, yaw, NorthBearing, halfWidth);
        PlaceMarker(eastMarker, yaw, EastBearing, halfWidth);
        PlaceMarker(southMarker, yaw, SouthBearing, halfWidth);
        PlaceMarker(westMarker, yaw, WestBearing, halfWidth);
    }

    private void PlaceMarker(RectTransform marker, float yawDegrees, float bearingDegrees, float halfWidthPixels)
    {
        if (marker == null)
            return;

        bool visible = CompassProjection.IsVisible(yawDegrees, bearingDegrees, halfFovDegrees);
        marker.gameObject.SetActive(visible);

        if (!visible)
            return;

        float x = CompassProjection.MarkerOffsetX(yawDegrees, bearingDegrees, halfFovDegrees, halfWidthPixels);
        Vector2 pos = marker.anchoredPosition;
        pos.x = x;
        marker.anchoredPosition = pos;
    }
}
