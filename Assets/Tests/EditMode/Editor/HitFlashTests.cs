using NUnit.Framework;
using UnityEngine;

public class HitFlashTests
{
    private GameObject _go;
    private HitFlash _hitFlash;
    private Renderer _renderer;
    private static readonly int FlashIntensityId = Shader.PropertyToID("_FlashIntensity");

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("HitFlashTarget", typeof(MeshRenderer));
        _renderer = _go.GetComponent<Renderer>();
        _hitFlash = _go.AddComponent<HitFlash>();
        TestReflectionUtil.InvokeMethod(_hitFlash, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    private float ReadPropertyBlockIntensity()
    {
        var block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(block);
        return block.GetFloat(FlashIntensityId);
    }

    [Test]
    public void Flash_SetsIntensityToOne_AndAppliesToPropertyBlock()
    {
        _hitFlash.Flash();

        Assert.AreEqual(1f, TestReflectionUtil.GetField<float>(_hitFlash, "_intensity"), 0.0001f);
        Assert.AreEqual(1f, ReadPropertyBlockIntensity(), 0.0001f);
    }

    [Test]
    public void Tick_DecaysLinearlyOverPointZeroEightSeconds()
    {
        _hitFlash.Flash();

        _hitFlash.Tick(0.04f); // halfway through the 0.08s decay window

        Assert.AreEqual(0.5f, TestReflectionUtil.GetField<float>(_hitFlash, "_intensity"), 0.0001f);
        Assert.AreEqual(0.5f, ReadPropertyBlockIntensity(), 0.0001f);
    }

    [Test]
    public void Tick_FullyDecaysToZero_AfterPointZeroEightSeconds()
    {
        _hitFlash.Flash();

        _hitFlash.Tick(0.08f);

        Assert.AreEqual(0f, TestReflectionUtil.GetField<float>(_hitFlash, "_intensity"), 0.0001f);
        Assert.AreEqual(0f, ReadPropertyBlockIntensity(), 0.0001f);
    }

    [Test]
    public void Tick_Overshoot_NeverGoesNegative()
    {
        _hitFlash.Flash();

        _hitFlash.Tick(10f);

        Assert.AreEqual(0f, TestReflectionUtil.GetField<float>(_hitFlash, "_intensity"), 0.0001f);
    }

    [Test]
    public void Tick_WhileAlreadyZero_IsANoOp_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _hitFlash.Tick(0.02f));
        Assert.AreEqual(0f, TestReflectionUtil.GetField<float>(_hitFlash, "_intensity"), 0.0001f);
    }

    [Test]
    public void Flash_CalledAgainMidDecay_ResetsToFull()
    {
        _hitFlash.Flash();
        _hitFlash.Tick(0.06f); // 0.02 remaining -> intensity 0.25

        _hitFlash.Flash();

        Assert.AreEqual(1f, TestReflectionUtil.GetField<float>(_hitFlash, "_intensity"), 0.0001f);
    }
}
