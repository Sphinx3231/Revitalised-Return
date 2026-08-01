using NUnit.Framework;
using UnityEngine;

// PlayerVitals' Awake()/Start() are private MonoBehaviour lifecycle methods that are not
// pumped by Unity's play loop in EditMode tests, so they're invoked explicitly via
// reflection (TestReflectionUtil) rather than relying on AddComponent's Editor-time Awake
// call, for determinism independent of any Unity Editor version behavior.
public class PlayerVitalsTests
{
    private GameObject _go;
    private PlayerVitals _vitals;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Vitals");
        _vitals = _go.AddComponent<PlayerVitals>();
        TestReflectionUtil.InvokeMethod(_vitals, "Awake");
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
    public void Start_FiresAllThreeVitalsEvents_WithInitialValues()
    {
        float healthCur = -1, healthMax = -1, staminaCur = -1, staminaMax = -1, postureCur = -1, postureMax = -1;
        System.Action<float, float> onHealth = (c, m) => { healthCur = c; healthMax = m; };
        System.Action<float, float> onStamina = (c, m) => { staminaCur = c; staminaMax = m; };
        System.Action<float, float> onPosture = (c, m) => { postureCur = c; postureMax = m; };

        EventBus.PlayerHealthChanged += onHealth;
        EventBus.PlayerStaminaChanged += onStamina;
        EventBus.PlayerPostureChanged += onPosture;

        TestReflectionUtil.InvokeMethod(_vitals, "Start");

        EventBus.PlayerHealthChanged -= onHealth;
        EventBus.PlayerStaminaChanged -= onStamina;
        EventBus.PlayerPostureChanged -= onPosture;

        Assert.AreEqual(100f, healthCur);
        Assert.AreEqual(100f, healthMax);
        Assert.AreEqual(100f, staminaCur);
        Assert.AreEqual(100f, staminaMax);
        Assert.AreEqual(100f, postureCur);
        Assert.AreEqual(100f, postureMax);
    }

    [Test]
    public void TrySpend_AmountGreaterThanCurrent_RejectsWithNoEventAndNoDeduction()
    {
        bool eventFired = false;
        System.Action<float, float> onStamina = (c, m) => eventFired = true;
        EventBus.PlayerStaminaChanged += onStamina;

        bool result = _vitals.TrySpend(150f);

        EventBus.PlayerStaminaChanged -= onStamina;

        Assert.IsFalse(result);
        Assert.IsFalse(eventFired);
    }

    [Test]
    public void TrySpend_ValidAmount_DeductsAndFiresEventAndPausesRegen()
    {
        float gotCurrent = -1, gotMax = -1;
        System.Action<float, float> onStamina = (c, m) => { gotCurrent = c; gotMax = m; };
        EventBus.PlayerStaminaChanged += onStamina;

        bool result = _vitals.TrySpend(20f);

        EventBus.PlayerStaminaChanged -= onStamina;

        Assert.IsTrue(result);
        Assert.AreEqual(80f, gotCurrent);
        Assert.AreEqual(100f, gotMax);

        // Regen pause timer should now be active (1.2s) — a TickRegen call immediately
        // after should be a no-op.
        bool tickFired = false;
        System.Action<float, float> onStaminaTick = (c, m) => tickFired = true;
        EventBus.PlayerStaminaChanged += onStaminaTick;
        _vitals.TickRegen(0.1f);
        EventBus.PlayerStaminaChanged -= onStaminaTick;

        Assert.IsFalse(tickFired, "TickRegen must do nothing while the post-spend pause timer is active.");
    }

    [Test]
    public void TickRegen_WhilePauseTimerActive_DoesNothing()
    {
        _vitals.TrySpend(10f); // arms the 1.2s pause timer

        float before = TestReflectionUtil.GetField<float>(_vitals, "_currentStamina");

        _vitals.TickRegen(0.5f); // still well within the 1.2s pause

        float after = TestReflectionUtil.GetField<float>(_vitals, "_currentStamina");

        Assert.AreEqual(before, after);
    }

    [Test]
    public void TickRegen_AfterPauseExpires_RegeneratesAtCorrectRateClampedToMax()
    {
        _vitals.TrySpend(50f); // stamina now 50, pause timer 1.2s

        // Expire the pause timer.
        _vitals.TickRegen(1.2f);

        float beforeRegenTick = TestReflectionUtil.GetField<float>(_vitals, "_currentStamina");
        Assert.AreEqual(50f, beforeRegenTick);

        float gotCurrent = -1, gotMax = -1;
        bool fired = false;
        System.Action<float, float> onStamina = (c, m) => { gotCurrent = c; gotMax = m; fired = true; };
        EventBus.PlayerStaminaChanged += onStamina;

        // StaminaRegenRate = 10.0f/sec; 1 second of regen => +10.
        _vitals.TickRegen(1f);

        EventBus.PlayerStaminaChanged -= onStamina;

        Assert.IsTrue(fired);
        Assert.AreEqual(60f, gotCurrent, 0.001f);
        Assert.AreEqual(100f, gotMax);
    }

    [Test]
    public void TickRegen_ClampsToMaxAndDoesNotFireWhenValueUnchanged()
    {
        _vitals.TrySpend(5f); // stamina 95, pause armed
        _vitals.TickRegen(1.2f); // expire pause, no-op tick (returns early on pause check)

        // Regen well past max — should clamp to 100 and fire once.
        bool firedOnce = false;
        int fireCount = 0;
        System.Action<float, float> onStamina = (c, m) => { firedOnce = true; fireCount++; };
        EventBus.PlayerStaminaChanged += onStamina;

        _vitals.TickRegen(10f); // would overshoot to 195 without the clamp
        EventBus.PlayerStaminaChanged -= onStamina;

        Assert.IsTrue(firedOnce);
        Assert.AreEqual(1, fireCount);

        float current = TestReflectionUtil.GetField<float>(_vitals, "_currentStamina");
        Assert.AreEqual(100f, current);

        // Already at max: a further tick must not fire the event again.
        bool firedAgain = false;
        System.Action<float, float> onStaminaAgain = (c, m) => firedAgain = true;
        EventBus.PlayerStaminaChanged += onStaminaAgain;
        _vitals.TickRegen(1f);
        EventBus.PlayerStaminaChanged -= onStaminaAgain;

        Assert.IsFalse(firedAgain);
    }

    // IDamageable tests (Step 5).

    [Test]
    public void ApplyDamage_DeductsHealthAndFiresEvent()
    {
        float gotCurrent = -1, gotMax = -1;
        System.Action<float, float> onHealth = (c, m) => { gotCurrent = c; gotMax = m; };
        EventBus.PlayerHealthChanged += onHealth;

        ((IDamageable)_vitals).ApplyDamage(30f, false);

        EventBus.PlayerHealthChanged -= onHealth;

        Assert.AreEqual(70f, gotCurrent);
        Assert.AreEqual(100f, gotMax);
    }

    [Test]
    public void ApplyDamage_ClampsAtZero()
    {
        float gotCurrent = -1;
        System.Action<float, float> onHealth = (c, m) => gotCurrent = c;
        EventBus.PlayerHealthChanged += onHealth;

        ((IDamageable)_vitals).ApplyDamage(500f, false);

        EventBus.PlayerHealthChanged -= onHealth;

        Assert.AreEqual(0f, gotCurrent);
    }

    [Test]
    public void ApplyPostureDamage_DeductsPostureAndFiresEvent()
    {
        float gotCurrent = -1, gotMax = -1;
        System.Action<float, float> onPosture = (c, m) => { gotCurrent = c; gotMax = m; };
        EventBus.PlayerPostureChanged += onPosture;

        ((IDamageable)_vitals).ApplyPostureDamage(40f);

        EventBus.PlayerPostureChanged -= onPosture;

        Assert.AreEqual(60f, gotCurrent);
        Assert.AreEqual(100f, gotMax);
    }

    [Test]
    public void ApplyPostureDamage_ClampsAtZero()
    {
        float gotCurrent = -1;
        System.Action<float, float> onPosture = (c, m) => gotCurrent = c;
        EventBus.PlayerPostureChanged += onPosture;

        ((IDamageable)_vitals).ApplyPostureDamage(500f);

        EventBus.PlayerPostureChanged -= onPosture;

        Assert.AreEqual(0f, gotCurrent);
    }

    [Test]
    public void DamageTransform_ReturnsOwnTransform()
    {
        Assert.AreSame(_vitals.transform, ((IDamageable)_vitals).DamageTransform);
    }
}
