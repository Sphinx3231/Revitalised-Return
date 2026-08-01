using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Camera trauma-decay shake (charter 6.2), driving a CinemachineBasicMultiChannelPerlin's
/// AmplitudeGain rather than hand-writing a rotation offset (Step 6 Ruling 1: Cinemachine's
/// own Noise stage composes after Body/Aim and can't be fought/overwritten by the Brain the
/// way a hand-rolled transform write would be). The locked pieces of the charter formula
/// stay locked here: Trauma(t) = clamp(Trauma(t-1) - 1.5 * deltaTime, 0, 1) decay, and
/// AmplitudeGain = maxAmplitude * Trauma^2 driving Cinemachine's noise amplitude every frame.
/// Explicit-Tick pattern (no Update()) per Step 6's approved approach.
/// </summary>
public sealed class CameraTrauma : MonoBehaviour
{
    private const float DecayPerSecond = 1.5f;

    [SerializeField] private CinemachineBasicMultiChannelPerlin perlin;
    [SerializeField] private float maxAmplitude = 2f;

    private float _trauma;

    public float Trauma => _trauma;

    /// <summary>Adds trauma, clamped to [0,1].</summary>
    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    /// <summary>
    /// Advances trauma decay by one explicit tick and drives the wired Perlin noise
    /// component's AmplitudeGain. No-op on the Perlin write if no reference is wired (trauma
    /// still decays either way, so a missing reference doesn't stall the state).
    /// </summary>
    public void Tick(float deltaTime)
    {
        _trauma = Mathf.Clamp01(_trauma - DecayPerSecond * deltaTime);

        if (perlin != null)
        {
            perlin.AmplitudeGain = maxAmplitude * _trauma * _trauma;
        }
    }
}
