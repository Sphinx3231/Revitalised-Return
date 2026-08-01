using NUnit.Framework;
using UnityEngine;

// StanceData is a plain-field ScriptableObject (charter 5.1) — these tests assert its
// declared defaults so a future accidental edit to the field initializers (which would
// zero out combat math per the Step 5 task brief's own warning) fails a test immediately.
public class StanceDataTests
{
    private StanceData _stance;

    [SetUp]
    public void SetUp()
    {
        _stance = ScriptableObject.CreateInstance<StanceData>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_stance != null)
        {
            Object.DestroyImmediate(_stance);
        }
    }

    [Test]
    public void Defaults_AreNeutralNonZeroValues()
    {
        Assert.AreEqual(1.0f, _stance.baseDamageMultiplier);
        Assert.AreEqual(1.0f, _stance.postureDamageMultiplier);
        Assert.AreEqual(1.0f, _stance.attackSpeedScalar);
        Assert.AreEqual(0.12f, _stance.parryWindowDuration);
    }

    [Test]
    public void Fields_AreSettable()
    {
        _stance.stanceName = "Stone";
        _stance.baseDamageMultiplier = 1.2f;
        _stance.postureDamageMultiplier = 1.8f;
        _stance.attackSpeedScalar = 0.85f;
        _stance.parryWindowDuration = 0.12f;

        Assert.AreEqual("Stone", _stance.stanceName);
        Assert.AreEqual(1.2f, _stance.baseDamageMultiplier);
        Assert.AreEqual(1.8f, _stance.postureDamageMultiplier);
        Assert.AreEqual(0.85f, _stance.attackSpeedScalar);
        Assert.AreEqual(0.12f, _stance.parryWindowDuration);
    }
}
