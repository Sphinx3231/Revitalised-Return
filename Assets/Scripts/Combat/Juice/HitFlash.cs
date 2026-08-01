using UnityEngine;

/// <summary>
/// Per-renderer hit-flash driver (charter 6.3). Uses a MaterialPropertyBlock (not
/// material.SetFloat, which would leak a per-instance material copy) to set the HitFlash
/// shader's _FlashIntensity on this Renderer. Flash() snaps to full intensity; Tick(deltaTime)
/// decays it linearly to 0 over 0.08s. Explicit-Tick pattern (no Update()) per Step 6's
/// approved approach.
/// </summary>
[RequireComponent(typeof(Renderer))]
public sealed class HitFlash : MonoBehaviour
{
    private const float DecayDuration = 0.08f;
    private static readonly int FlashIntensityId = Shader.PropertyToID("_FlashIntensity");

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private float _intensity;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>Snaps flash intensity to 1.0 (pure white albedo lerp target).</summary>
    public void Flash()
    {
        _intensity = 1f;
        ApplyPropertyBlock();
    }

    /// <summary>
    /// Decays the active flash (if any) toward 0 over 0.08s. No-op once intensity has
    /// reached 0 — still safe to call every frame regardless of flash state.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (_intensity <= 0f)
            return;

        _intensity = Mathf.Max(0f, _intensity - deltaTime / DecayDuration);
        ApplyPropertyBlock();
    }

    private void ApplyPropertyBlock()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<Renderer>();
        }

        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(FlashIntensityId, _intensity);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
}
