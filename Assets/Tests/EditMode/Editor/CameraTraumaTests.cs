using NUnit.Framework;
using UnityEngine;
using Unity.Cinemachine;

public class CameraTraumaTests
{
    private GameObject _go;
    private CameraTrauma _trauma;
    private CinemachineBasicMultiChannelPerlin _perlin;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("CameraTrauma");
        _trauma = _go.AddComponent<CameraTrauma>();
        _perlin = _go.AddComponent<CinemachineBasicMultiChannelPerlin>();

        TestReflectionUtil.SetField(_trauma, "perlin", _perlin);
        TestReflectionUtil.SetField(_trauma, "maxAmplitude", 2f);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    [Test]
    public void AddTrauma_ClampsToOne()
    {
        _trauma.AddTrauma(0.6f);
        _trauma.AddTrauma(0.6f);

        Assert.AreEqual(1f, _trauma.Trauma, 0.0001f);
    }

    [Test]
    public void AddTrauma_Accumulates_BelowClamp()
    {
        _trauma.AddTrauma(0.3f);
        _trauma.AddTrauma(0.2f);

        Assert.AreEqual(0.5f, _trauma.Trauma, 0.0001f);
    }

    [Test]
    public void Tick_DecaysAtLockedOnePointFivePerSecondRate()
    {
        _trauma.AddTrauma(1f);

        _trauma.Tick(0.1f);

        // Trauma(t) = clamp(Trauma(t-1) - 1.5 * deltaTime, 0, 1) => 1 - 1.5*0.1 = 0.85.
        Assert.AreEqual(0.85f, _trauma.Trauma, 0.0001f);
    }

    [Test]
    public void Tick_DecayNeverGoesBelowZero()
    {
        _trauma.AddTrauma(0.1f);

        _trauma.Tick(10f); // wildly overshoots the decay

        Assert.AreEqual(0f, _trauma.Trauma, 0.0001f);
    }

    [Test]
    public void Tick_DrivesAmplitudeGain_EqualToMaxAmplitudeTimesTraumaSquared()
    {
        _trauma.AddTrauma(1f);
        _trauma.Tick(0.2f);

        // Trauma after tick: 1 - 1.5*0.2 = 0.7. AmplitudeGain = maxAmplitude(2) * 0.7^2.
        float expectedTrauma = 0.7f;
        float expectedAmplitude = 2f * expectedTrauma * expectedTrauma;

        Assert.AreEqual(expectedTrauma, _trauma.Trauma, 0.0001f);
        Assert.AreEqual(expectedAmplitude, _perlin.AmplitudeGain, 0.0001f);
    }

    [Test]
    public void Tick_NullPerlinReference_StillDecaysTrauma_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_trauma, "perlin", null);
        _trauma.AddTrauma(1f);

        Assert.DoesNotThrow(() => _trauma.Tick(0.1f));
        Assert.AreEqual(0.85f, _trauma.Trauma, 0.0001f);
    }

    [Test]
    public void Tick_WithZeroTrauma_DrivesAmplitudeGainToZero()
    {
        _trauma.Tick(0.1f);

        Assert.AreEqual(0f, _trauma.Trauma, 0.0001f);
        Assert.AreEqual(0f, _perlin.AmplitudeGain, 0.0001f);
    }
}
