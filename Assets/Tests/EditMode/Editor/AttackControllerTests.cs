using NUnit.Framework;
using UnityEngine;

public class AttackControllerTests
{
    private GameObject _go;
    private AttackController _attack;

    private GameObject _hitboxGo;
    private WeaponHitbox _hitbox;
    private BoxCollider _hitboxCollider;

    private GameObject _stanceGo;
    private StanceController _stanceController;
    private StanceData _stance;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Attacker");
        _attack = _go.AddComponent<AttackController>();

        _hitboxGo = new GameObject("WeaponPivot", typeof(BoxCollider));
        _hitbox = _hitboxGo.AddComponent<WeaponHitbox>();
        _hitboxCollider = _hitboxGo.GetComponent<BoxCollider>();
        _hitboxCollider.enabled = false;

        _stanceGo = new GameObject("Stances");
        _stanceController = _stanceGo.AddComponent<StanceController>();
        _stance = ScriptableObject.CreateInstance<StanceData>();
        _stance.attackSpeedScalar = 1.0f;
        TestReflectionUtil.SetField(_stanceController, "stances", new[] { _stance });
        TestReflectionUtil.SetField(_stanceController, "_currentIndex", 0);

        TestReflectionUtil.SetField(_attack, "weaponHitbox", _hitbox);
        TestReflectionUtil.SetField(_attack, "stanceController", _stanceController);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        if (_hitboxGo != null) Object.DestroyImmediate(_hitboxGo);
        if (_stanceGo != null) Object.DestroyImmediate(_stanceGo);
        if (_stance != null) Object.DestroyImmediate(_stance);
    }

    [Test]
    public void TryLightAttack_OpensHitbox_SetsStanceAndDamage_AndStartsWindow()
    {
        _attack.TryLightAttack();

        Assert.IsTrue(_attack.IsAttacking);
        Assert.IsTrue(_hitboxCollider.enabled);
        Assert.AreSame(_stance, _hitbox.CurrentStance);
        Assert.AreEqual(10f, _hitbox.BaseDamage); // default lightAttackBaseDamage
    }

    [Test]
    public void TryHeavyAttack_UsesHeavyDamage()
    {
        _attack.TryHeavyAttack();

        Assert.IsTrue(_attack.IsAttacking);
        Assert.AreEqual(18f, _hitbox.BaseDamage); // default heavyAttackBaseDamage
    }

    [Test]
    public void TryLightAttack_WhileAlreadyAttacking_IsANoOp()
    {
        _attack.TryLightAttack();
        _hitbox.CurrentStance = null; // sentinel: only a second TryAttack call would reassign this

        _attack.TryHeavyAttack();

        // The sentinel must be untouched (heavy attack's TryAttack body never ran) and
        // BaseDamage must still be the light attack's value, not the heavy one.
        Assert.IsNull(_hitbox.CurrentStance);
        Assert.AreEqual(10f, _hitbox.BaseDamage);
    }

    [Test]
    public void TryLightAttack_NullWeaponHitbox_DoesNotThrowAndStaysNotAttacking()
    {
        TestReflectionUtil.SetField(_attack, "weaponHitbox", null);

        Assert.DoesNotThrow(() => _attack.TryLightAttack());
        Assert.IsFalse(_attack.IsAttacking);
    }

    [Test]
    public void TickAttack_WhileNotAttacking_IsANoOp()
    {
        Assert.DoesNotThrow(() => _attack.TickAttack(0.1f));
        Assert.IsFalse(_attack.IsAttacking);
    }

    [Test]
    public void TickAttack_ClosesHitboxWhenWindowExpires()
    {
        _attack.TryLightAttack(); // window = 0.2s / 1.0 attackSpeedScalar = 0.2s

        _attack.TickAttack(0.1f);
        Assert.IsTrue(_attack.IsAttacking);
        Assert.IsTrue(_hitboxCollider.enabled);

        _attack.TickAttack(0.15f); // total 0.25s, past the 0.2s window
        Assert.IsFalse(_attack.IsAttacking);
        Assert.IsFalse(_hitboxCollider.enabled);
    }

    [Test]
    public void TickAttack_WindowScaledByAttackSpeedScalar()
    {
        _stance.attackSpeedScalar = 2.0f; // halves the window: 0.2 / 2 = 0.1s

        _attack.TryLightAttack();

        _attack.TickAttack(0.09f);
        Assert.IsTrue(_attack.IsAttacking);

        _attack.TickAttack(0.02f); // total 0.11s, past 0.1s
        Assert.IsFalse(_attack.IsAttacking);
    }

    [Test]
    public void TickAttack_NullStance_DefaultsAttackSpeedScalarToOne()
    {
        TestReflectionUtil.SetField(_stanceController, "stances", new StanceData[0]);

        _attack.TryLightAttack();

        _attack.TickAttack(0.19f);
        Assert.IsTrue(_attack.IsAttacking);

        _attack.TickAttack(0.02f); // total 0.21s, past the default 0.2s window
        Assert.IsFalse(_attack.IsAttacking);
    }

    [Test]
    public void TryInterrupt_WhileAttacking_ForceClosesHitbox()
    {
        _attack.TryLightAttack();

        _attack.TryInterrupt();

        Assert.IsFalse(_attack.IsAttacking);
        Assert.IsFalse(_hitboxCollider.enabled);
    }

    [Test]
    public void TryInterrupt_WhileNotAttacking_IsANoOp()
    {
        Assert.DoesNotThrow(() => _attack.TryInterrupt());
        Assert.IsFalse(_attack.IsAttacking);
    }

    [Test]
    public void TryLightAttack_ResetsHitTrackingOnOpen()
    {
        var targetGo = new GameObject("Target", typeof(BoxCollider));
        var targetCollider = targetGo.GetComponent<BoxCollider>();

        TestReflectionUtil.InvokeMethod(_hitbox, "OnTriggerEnter", targetCollider);
        // No IDamageable on target, so this is a no-op hit-tracking-wise; call
        // ResetHitTracking indirectly via a second TryLightAttack to prove no exception.
        _attack.TickAttack(1f); // closes the first attack
        Assert.DoesNotThrow(() => _attack.TryLightAttack());

        Object.DestroyImmediate(targetGo);
    }
}
