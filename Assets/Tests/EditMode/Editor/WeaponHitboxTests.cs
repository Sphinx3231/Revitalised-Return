using NUnit.Framework;
using UnityEngine;

// WeaponHitbox's OnTriggerEnter is a private Unity physics callback never pumped by
// EditMode's non-running physics loop, so it's invoked directly via reflection with a
// hand-constructed "other" Collider — the same simulate-the-callback approach used
// elsewhere in this suite for engine-callback-shaped private methods (see
// TestReflectionUtil). This file is the most important one in the whole task per the
// Director's brief: the parry/block resolution order (charter 5.2) has no live opponent
// yet to manually Play-Mode-verify against, so it MUST be correctly proven here.
public class WeaponHitboxTests
{
    private class MockDamageable : MonoBehaviour, IDamageable
    {
        public int ApplyDamageCallCount;
        public float LastDamageAmount = -1f;
        public bool LastIsCritical;
        public int ApplyPostureDamageCallCount;
        public float LastPostureDamage = -1f;

        public void ApplyDamage(float amount, bool isCritical)
        {
            ApplyDamageCallCount++;
            LastDamageAmount = amount;
            LastIsCritical = isCritical;
        }

        public void ApplyPostureDamage(float amount)
        {
            ApplyPostureDamageCallCount++;
            LastPostureDamage = amount;
        }

        public Transform DamageTransform => transform;
    }

    private GameObject _ownerGo;
    private MockDamageable _ownerDamageable;
    private AttackController _ownerAttack;

    private GameObject _hitboxGo;
    private WeaponHitbox _hitbox;

    private GameObject _targetGo;
    private BoxCollider _targetCollider;
    private MockDamageable _targetDamageable;

    private StanceData _stance;

    [SetUp]
    public void SetUp()
    {
        _ownerGo = new GameObject("Owner");
        _ownerDamageable = _ownerGo.AddComponent<MockDamageable>();
        _ownerAttack = _ownerGo.AddComponent<AttackController>();

        _hitboxGo = new GameObject("WeaponPivot", typeof(BoxCollider));
        _hitboxGo.transform.SetParent(_ownerGo.transform);
        _hitbox = _hitboxGo.AddComponent<WeaponHitbox>();
        TestReflectionUtil.SetField(_hitbox, "baseDamage", 10f);

        TestReflectionUtil.SetField(_ownerAttack, "weaponHitbox", _hitbox);

        _targetGo = new GameObject("Target", typeof(BoxCollider));
        _targetCollider = _targetGo.GetComponent<BoxCollider>();
        _targetDamageable = _targetGo.AddComponent<MockDamageable>();

        _stance = ScriptableObject.CreateInstance<StanceData>();
        _stance.baseDamageMultiplier = 1.2f;
        _stance.postureDamageMultiplier = 1.8f;
    }

    [TearDown]
    public void TearDown()
    {
        if (_ownerGo != null) Object.DestroyImmediate(_ownerGo); // also destroys _hitboxGo (child)
        if (_targetGo != null) Object.DestroyImmediate(_targetGo);
        if (_stance != null) Object.DestroyImmediate(_stance);
    }

    private void Trigger(Collider other)
    {
        TestReflectionUtil.InvokeMethod(_hitbox, "OnTriggerEnter", other);
    }

    [Test]
    public void NormalHit_AppliesDamageAndPostureUsingStanceMultipliers_AndFiresEntityDamaged()
    {
        _hitbox.CurrentStance = _stance;

        Transform gotTarget = null;
        float gotAmount = -1f;
        bool gotCrit = true;
        System.Action<Transform, float, bool> handler = (t, a, c) => { gotTarget = t; gotAmount = a; gotCrit = c; };
        EventBus.EntityDamaged += handler;

        Trigger(_targetCollider);

        EventBus.EntityDamaged -= handler;

        // Damage = (Base) * stanceBaseMult * (1 - 0/(0+100)) = 10 * 1.2 * 1 = 12.
        Assert.AreEqual(1, _targetDamageable.ApplyDamageCallCount);
        Assert.AreEqual(12f, _targetDamageable.LastDamageAmount, 0.001f);
        Assert.IsFalse(_targetDamageable.LastIsCritical);

        // Posture damage = Base * stancePostureMult = 10 * 1.8 = 18.
        Assert.AreEqual(1, _targetDamageable.ApplyPostureDamageCallCount);
        Assert.AreEqual(18f, _targetDamageable.LastPostureDamage, 0.001f);

        Assert.AreSame(_targetGo.transform, gotTarget);
        Assert.AreEqual(12f, gotAmount, 0.001f);
        Assert.IsFalse(gotCrit);
    }

    [Test]
    public void NormalHit_NullStance_UsesMultiplierOfOne()
    {
        _hitbox.CurrentStance = null;

        Trigger(_targetCollider);

        Assert.AreEqual(10f, _targetDamageable.LastDamageAmount, 0.001f);
        Assert.AreEqual(10f, _targetDamageable.LastPostureDamage, 0.001f);
    }

    [Test]
    public void SameCollider_SecondOverlap_DoesNotDoubleHit()
    {
        Trigger(_targetCollider);
        Trigger(_targetCollider);

        Assert.AreEqual(1, _targetDamageable.ApplyDamageCallCount);
    }

    [Test]
    public void ResetHitTracking_AllowsReHittingSameCollider()
    {
        Trigger(_targetCollider);
        _hitbox.ResetHitTracking();
        Trigger(_targetCollider);

        Assert.AreEqual(2, _targetDamageable.ApplyDamageCallCount);
    }

    [Test]
    public void NoIDamageableOnTarget_DoesNotThrowAndNoOp()
    {
        var plainGo = new GameObject("Plain", typeof(BoxCollider));
        var plainCollider = plainGo.GetComponent<BoxCollider>();

        Assert.DoesNotThrow(() => Trigger(plainCollider));

        Object.DestroyImmediate(plainGo);
    }

    [Test]
    public void IDamageableOnParent_IsResolved()
    {
        var childGo = new GameObject("Child", typeof(BoxCollider));
        childGo.transform.SetParent(_targetGo.transform);
        var childCollider = childGo.GetComponent<BoxCollider>();

        Trigger(childCollider);

        Assert.AreEqual(1, _targetDamageable.ApplyDamageCallCount);
    }

    // --- Parry resolution (charter 5.2 step 1) ---

    [Test]
    public void ParryingTarget_NoDamageToDefender_AppliesFortyPercentPostureToAttacker_AndFiresParryExecuted()
    {
        // ParryController lives directly on the struck GameObject here (mirroring the
        // production hierarchy where the Hurtbox collider's own GameObject or one of its
        // ancestors carries ParryController) — GetComponent/GetComponentInParent both
        // resolve upward/on-self, never onto children.
        var parryController = _targetGo.AddComponent<ParryController>();
        TestReflectionUtil.SetField(parryController, "_isParrying", true);

        Transform gotAttacker = null, gotDefender = null;
        System.Action<Transform, Transform> handler = (a, d) => { gotAttacker = a; gotDefender = d; };
        EventBus.ParryExecuted += handler;

        Trigger(_targetCollider);

        EventBus.ParryExecuted -= handler;

        // No damage/posture applied to the defender.
        Assert.AreEqual(0, _targetDamageable.ApplyDamageCallCount);
        Assert.AreEqual(0, _targetDamageable.ApplyPostureDamageCallCount);

        // 40% of baseDamage (10) = 4 posture damage applied to the attacker (owner).
        Assert.AreEqual(1, _ownerDamageable.ApplyPostureDamageCallCount);
        Assert.AreEqual(4f, _ownerDamageable.LastPostureDamage, 0.001f);
        Assert.AreEqual(0, _ownerDamageable.ApplyDamageCallCount);

        Assert.AreSame(_hitboxGo.transform, gotAttacker);
        Assert.AreSame(_targetGo.transform, gotDefender);
    }

    [Test]
    public void ParryingTarget_InterruptsAttackersActiveAttack()
    {
        var parryController = _targetGo.AddComponent<ParryController>();
        TestReflectionUtil.SetField(parryController, "_isParrying", true);

        TestReflectionUtil.SetField(_ownerAttack, "_isAttacking", true);
        _hitbox.GetComponent<Collider>().enabled = true;

        Trigger(_targetCollider);

        Assert.IsFalse(_ownerAttack.IsAttacking, "Parry must interrupt (force-close) the attacker's active attack.");
        Assert.IsFalse(_hitbox.GetComponent<Collider>().enabled);
    }

    [Test]
    public void ParryingTarget_NoAttackControllerOnOwner_DoesNotThrow()
    {
        // Rebuild the hitbox under an owner with no AttackController at all.
        var bareOwnerGo = new GameObject("BareOwner");
        var bareOwnerDamageable = bareOwnerGo.AddComponent<MockDamageable>();
        var bareHitboxGo = new GameObject("BareHitbox", typeof(BoxCollider));
        bareHitboxGo.transform.SetParent(bareOwnerGo.transform);
        var bareHitbox = bareHitboxGo.AddComponent<WeaponHitbox>();
        TestReflectionUtil.SetField(bareHitbox, "baseDamage", 10f);

        var parryController = _targetGo.AddComponent<ParryController>();
        TestReflectionUtil.SetField(parryController, "_isParrying", true);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(bareHitbox, "OnTriggerEnter", _targetCollider));
        Assert.AreEqual(1, bareOwnerDamageable.ApplyPostureDamageCallCount);

        Object.DestroyImmediate(bareOwnerGo);
    }

    // --- Block resolution (charter 5.2 step 2) ---

    [Test]
    public void BlockingTarget_ReducesDamageByEightyPercent_ButPostureDamageBleedsThroughUnreduced()
    {
        var parryController = _targetGo.AddComponent<ParryController>();
        parryController.IsBlocking = true; // not parrying

        _hitbox.CurrentStance = _stance;

        Trigger(_targetCollider);

        // Damage = 12 (from stance) * 0.2 = 2.4.
        Assert.AreEqual(1, _targetDamageable.ApplyDamageCallCount);
        Assert.AreEqual(2.4f, _targetDamageable.LastDamageAmount, 0.001f);

        // Posture damage bleeds through fully unreduced: 10 * 1.8 = 18.
        Assert.AreEqual(1, _targetDamageable.ApplyPostureDamageCallCount);
        Assert.AreEqual(18f, _targetDamageable.LastPostureDamage, 0.001f);
    }

    [Test]
    public void BlockingAndParrying_ParryTakesPrecedence()
    {
        var parryController = _targetGo.AddComponent<ParryController>();
        TestReflectionUtil.SetField(parryController, "_isParrying", true);
        parryController.IsBlocking = true;

        Trigger(_targetCollider);

        // Parry resolution wins: no damage/posture to the defender at all.
        Assert.AreEqual(0, _targetDamageable.ApplyDamageCallCount);
        Assert.AreEqual(0, _targetDamageable.ApplyPostureDamageCallCount);
    }

    [Test]
    public void NeitherParryingNorBlocking_FullDamageApplied()
    {
        _targetGo.AddComponent<ParryController>(); // IsParrying=false, IsBlocking=false by default

        Trigger(_targetCollider);

        Assert.AreEqual(10f, _targetDamageable.LastDamageAmount, 0.001f);
    }

    // --- Step 6 juice VFX direct calls (Ruling 2) ---

    [Test]
    public void NormalHit_NoSparkPoolWired_DoesNotThrow()
    {
        // sparkPool defaults to null (unwired) -- a WeaponHitbox without one must still
        // resolve the hit correctly.
        Assert.DoesNotThrow(() => Trigger(_targetCollider));
        Assert.AreEqual(1, _targetDamageable.ApplyDamageCallCount);
    }

    [Test]
    public void NormalHit_WithSparkPoolWired_DoesNotThrow_AndStillAppliesDamage()
    {
        var sparkGo = new GameObject("SparkPool");
        var sparkPool = sparkGo.AddComponent<SparkPool>();
        TestReflectionUtil.SetField(_hitbox, "sparkPool", sparkPool);

        Assert.DoesNotThrow(() => Trigger(_targetCollider));
        Assert.AreEqual(1, _targetDamageable.ApplyDamageCallCount);

        Object.DestroyImmediate(sparkGo);
    }

    [Test]
    public void NormalHit_TargetHasHitFlash_CallsFlash()
    {
        _targetGo.AddComponent<MeshRenderer>();
        var hitFlash = _targetGo.AddComponent<HitFlash>();
        TestReflectionUtil.InvokeMethod(hitFlash, "Awake");

        Trigger(_targetCollider);

        Assert.AreEqual(1f, TestReflectionUtil.GetField<float>(hitFlash, "_intensity"), 0.0001f);
    }

    [Test]
    public void NormalHit_TargetHasHitFlashOnParent_CallsFlash()
    {
        var childGo = new GameObject("Child", typeof(BoxCollider));
        childGo.transform.SetParent(_targetGo.transform);
        var childCollider = childGo.GetComponent<BoxCollider>();

        _targetGo.AddComponent<MeshRenderer>();
        var hitFlash = _targetGo.AddComponent<HitFlash>();
        TestReflectionUtil.InvokeMethod(hitFlash, "Awake");

        Trigger(childCollider);

        Assert.AreEqual(1f, TestReflectionUtil.GetField<float>(hitFlash, "_intensity"), 0.0001f);
    }

    [Test]
    public void NormalHit_TargetHasNoHitFlash_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Trigger(_targetCollider));
    }
}
