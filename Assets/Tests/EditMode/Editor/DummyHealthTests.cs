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
}
