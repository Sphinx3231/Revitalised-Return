using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// DummyHealth's Awake() is a private MonoBehaviour lifecycle method not pumped by Unity's
// play loop in EditMode tests, so it's invoked explicitly via reflection, matching the
// established PlayerVitalsTests convention.
public class DummyHealthTests
{
    private GameObject _go;
    private DummyHealth _dummy;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Dummy");
        _dummy = _go.AddComponent<DummyHealth>();
        TestReflectionUtil.InvokeMethod(_dummy, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
        {
            Object.DestroyImmediate(_go);
        }
    }

    [Test]
    public void Awake_InitializesHealthAndPostureToMax()
    {
        float health = TestReflectionUtil.GetField<float>(_dummy, "_currentHealth");
        float posture = TestReflectionUtil.GetField<float>(_dummy, "_currentPosture");

        Assert.AreEqual(100f, health);
        Assert.AreEqual(100f, posture);
    }

    [Test]
    public void ApplyDamage_DeductsHealthAndLogs()
    {
        LogAssert.Expect(LogType.Log, "Dummy took 30 damage, 70/100 HP remaining");

        ((IDamageable)_dummy).ApplyDamage(30f, false);

        float health = TestReflectionUtil.GetField<float>(_dummy, "_currentHealth");
        Assert.AreEqual(70f, health);
    }

    [Test]
    public void ApplyDamage_ClampsAtZero()
    {
        LogAssert.ignoreFailingMessages = true;

        ((IDamageable)_dummy).ApplyDamage(500f, false);

        float health = TestReflectionUtil.GetField<float>(_dummy, "_currentHealth");
        Assert.AreEqual(0f, health);

        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void ApplyDamage_LethalHit_AlsoLogsDefeated()
    {
        LogAssert.Expect(LogType.Log, "Dummy took 100 damage, 0/100 HP remaining");
        LogAssert.Expect(LogType.Log, "Dummy defeated");

        ((IDamageable)_dummy).ApplyDamage(100f, false);
    }

    [Test]
    public void ApplyDamage_NonLethalHit_DoesNotLogDefeated()
    {
        LogAssert.Expect(LogType.Log, "Dummy took 10 damage, 90/100 HP remaining");

        ((IDamageable)_dummy).ApplyDamage(10f, false);

        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void ApplyPostureDamage_DeductsPostureAndLogs()
    {
        LogAssert.Expect(LogType.Log, "Dummy took 25 posture damage, 75/100 posture remaining");

        ((IDamageable)_dummy).ApplyPostureDamage(25f);

        float posture = TestReflectionUtil.GetField<float>(_dummy, "_currentPosture");
        Assert.AreEqual(75f, posture);
    }

    [Test]
    public void ApplyPostureDamage_ClampsAtZero()
    {
        LogAssert.ignoreFailingMessages = true;

        ((IDamageable)_dummy).ApplyPostureDamage(500f);

        float posture = TestReflectionUtil.GetField<float>(_dummy, "_currentPosture");
        Assert.AreEqual(0f, posture);

        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void DamageTransform_ReturnsOwnTransform()
    {
        Assert.AreSame(_dummy.transform, ((IDamageable)_dummy).DamageTransform);
    }

    // Step 8 additions: HealthChanged event, HealthFraction, IsInvincible.

    [Test]
    public void ApplyDamage_FiresHealthChanged_WithCurrentAndMax()
    {
        LogAssert.Expect(LogType.Log, "Dummy took 30 damage, 70/100 HP remaining");

        float gotCurrent = -1f;
        float gotMax = -1f;
        _dummy.HealthChanged += (current, max) => { gotCurrent = current; gotMax = max; };

        ((IDamageable)_dummy).ApplyDamage(30f, false);

        Assert.AreEqual(70f, gotCurrent);
        Assert.AreEqual(100f, gotMax);
    }

    [Test]
    public void ApplyDamage_NoSubscribers_DoesNotThrow()
    {
        LogAssert.Expect(LogType.Log, "Dummy took 10 damage, 90/100 HP remaining");
        Assert.DoesNotThrow(() => ((IDamageable)_dummy).ApplyDamage(10f, false));
    }

    [Test]
    public void HealthFraction_StartsAtOne()
    {
        Assert.AreEqual(1f, _dummy.HealthFraction, 0.0001f);
    }

    [Test]
    public void HealthFraction_ReflectsDamageTaken()
    {
        LogAssert.Expect(LogType.Log, "Dummy took 25 damage, 75/100 HP remaining");
        ((IDamageable)_dummy).ApplyDamage(25f, false);

        Assert.AreEqual(0.75f, _dummy.HealthFraction, 0.0001f);
    }

    [Test]
    public void IsInvincible_DefaultsFalse()
    {
        Assert.IsFalse(_dummy.IsInvincible);
    }

    [Test]
    public void ApplyDamage_WhileInvincible_IsANoOp_NoLogNoEvent()
    {
        _dummy.IsInvincible = true;

        bool eventFired = false;
        _dummy.HealthChanged += (current, max) => eventFired = true;

        ((IDamageable)_dummy).ApplyDamage(50f, false);

        LogAssert.NoUnexpectedReceived();
        Assert.IsFalse(eventFired);
        float health = TestReflectionUtil.GetField<float>(_dummy, "_currentHealth");
        Assert.AreEqual(100f, health);
    }

    [Test]
    public void ApplyDamage_AfterInvincibilityCleared_DamagesNormally()
    {
        _dummy.IsInvincible = true;
        ((IDamageable)_dummy).ApplyDamage(50f, false); // no-op, no log expected

        _dummy.IsInvincible = false;
        LogAssert.Expect(LogType.Log, "Dummy took 50 damage, 50/100 HP remaining");
        ((IDamageable)_dummy).ApplyDamage(50f, false);

        float health = TestReflectionUtil.GetField<float>(_dummy, "_currentHealth");
        Assert.AreEqual(50f, health);
    }
}
