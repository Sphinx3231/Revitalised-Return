using UnityEngine;

/// <summary>
/// Charter Step 4.1 camera-relative movement vector derivation, with the charter's
/// own explicit amendment applied: the raw camera-basis transform is FLATTENED
/// (zero out Y) BEFORE normalizing, not after — normalizing first under any camera
/// pitch would inject a vertical component and shorten the resulting horizontal
/// magnitude.
/// </summary>
public static class CameraRelativeInput
{
    public static Vector3 ToCameraRelative(Vector2 rawInput, Transform cameraTransform)
    {
        Vector3 rawInput3D = cameraTransform.TransformDirection(new Vector3(rawInput.x, 0f, rawInput.y));
        Vector3 flattened = new Vector3(rawInput3D.x, 0f, rawInput3D.z).normalized;
        return flattened;
    }
}
