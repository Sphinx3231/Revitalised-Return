using NUnit.Framework;
using UnityEngine;

public class EventBusTests
{
    [Test]
    public void RaisePlayerHealthChanged_InvokesSubscriberWithExactArgs()
    {
        float gotCurrent = -1f, gotMax = -1f;
        System.Action<float, float> handler = (c, m) => { gotCurrent = c; gotMax = m; };

        EventBus.PlayerHealthChanged += handler;
        EventBus.RaisePlayerHealthChanged(42f, 100f);
        EventBus.PlayerHealthChanged -= handler;

        Assert.AreEqual(42f, gotCurrent);
        Assert.AreEqual(100f, gotMax);
    }

    [Test]
    public void RaisePlayerHealthChanged_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaisePlayerHealthChanged(10f, 100f));
    }

    [Test]
    public void RaisePlayerStaminaChanged_InvokesSubscriberWithExactArgs()
    {
        float gotCurrent = -1f, gotMax = -1f;
        System.Action<float, float> handler = (c, m) => { gotCurrent = c; gotMax = m; };

        EventBus.PlayerStaminaChanged += handler;
        EventBus.RaisePlayerStaminaChanged(30f, 80f);
        EventBus.PlayerStaminaChanged -= handler;

        Assert.AreEqual(30f, gotCurrent);
        Assert.AreEqual(80f, gotMax);
    }

    [Test]
    public void RaisePlayerStaminaChanged_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaisePlayerStaminaChanged(10f, 100f));
    }

    [Test]
    public void RaisePlayerPostureChanged_InvokesSubscriberWithExactArgs()
    {
        float gotCurrent = -1f, gotMax = -1f;
        System.Action<float, float> handler = (c, m) => { gotCurrent = c; gotMax = m; };

        EventBus.PlayerPostureChanged += handler;
        EventBus.RaisePlayerPostureChanged(15f, 60f);
        EventBus.PlayerPostureChanged -= handler;

        Assert.AreEqual(15f, gotCurrent);
        Assert.AreEqual(60f, gotMax);
    }

    [Test]
    public void RaisePlayerPostureChanged_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaisePlayerPostureChanged(10f, 100f));
    }

    [Test]
    public void RaiseStanceSwapped_InvokesSubscriberWithExactArg()
    {
        var stance = ScriptableObject.CreateInstance<StanceData>();
        StanceData got = null;
        System.Action<StanceData> handler = s => got = s;

        EventBus.StanceSwapped += handler;
        EventBus.RaiseStanceSwapped(stance);
        EventBus.StanceSwapped -= handler;

        Assert.AreSame(stance, got);
        Object_DestroyImmediate(stance);
    }

    [Test]
    public void RaiseStanceSwapped_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaiseStanceSwapped(null));
    }

    [Test]
    public void RaisePlayerDied_InvokesSubscriber()
    {
        bool called = false;
        System.Action handler = () => called = true;

        EventBus.PlayerDied += handler;
        EventBus.RaisePlayerDied();
        EventBus.PlayerDied -= handler;

        Assert.IsTrue(called);
    }

    [Test]
    public void RaisePlayerDied_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaisePlayerDied());
    }

    [Test]
    public void RaiseEntityDamaged_InvokesSubscriberWithExactArgs()
    {
        var go = new GameObject("Target");
        Transform gotTarget = null;
        float gotAmount = -1f;
        bool gotCrit = false;
        System.Action<Transform, float, bool> handler = (t, a, c) => { gotTarget = t; gotAmount = a; gotCrit = c; };

        EventBus.EntityDamaged += handler;
        EventBus.RaiseEntityDamaged(go.transform, 25f, true);
        EventBus.EntityDamaged -= handler;

        Assert.AreSame(go.transform, gotTarget);
        Assert.AreEqual(25f, gotAmount);
        Assert.IsTrue(gotCrit);
        Object_DestroyImmediate(go);
    }

    [Test]
    public void RaiseEntityDamaged_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaiseEntityDamaged(null, 1f, false));
    }

    [Test]
    public void RaisePostureBroken_InvokesSubscriberWithExactArg()
    {
        var go = new GameObject("Target");
        Transform got = null;
        System.Action<Transform> handler = t => got = t;

        EventBus.PostureBroken += handler;
        EventBus.RaisePostureBroken(go.transform);
        EventBus.PostureBroken -= handler;

        Assert.AreSame(go.transform, got);
        Object_DestroyImmediate(go);
    }

    [Test]
    public void RaisePostureBroken_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaisePostureBroken(null));
    }

    [Test]
    public void RaiseParryExecuted_InvokesSubscriberWithExactArgs()
    {
        var attacker = new GameObject("Attacker");
        var defender = new GameObject("Defender");
        Transform gotAttacker = null, gotDefender = null;
        System.Action<Transform, Transform> handler = (a, d) => { gotAttacker = a; gotDefender = d; };

        EventBus.ParryExecuted += handler;
        EventBus.RaiseParryExecuted(attacker.transform, defender.transform);
        EventBus.ParryExecuted -= handler;

        Assert.AreSame(attacker.transform, gotAttacker);
        Assert.AreSame(defender.transform, gotDefender);
        Object_DestroyImmediate(attacker);
        Object_DestroyImmediate(defender);
    }

    [Test]
    public void RaiseParryExecuted_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaiseParryExecuted(null, null));
    }

    [Test]
    public void RaiseEnemyKilled_InvokesSubscriberWithExactArgs()
    {
        var go = new GameObject("Enemy");
        Transform gotEnemy = null;
        int gotExp = -1;
        System.Action<Transform, int> handler = (e, x) => { gotEnemy = e; gotExp = x; };

        EventBus.EnemyKilled += handler;
        EventBus.RaiseEnemyKilled(go.transform, 250);
        EventBus.EnemyKilled -= handler;

        Assert.AreSame(go.transform, gotEnemy);
        Assert.AreEqual(250, gotExp);
        Object_DestroyImmediate(go);
    }

    [Test]
    public void RaiseEnemyKilled_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaiseEnemyKilled(null, 0));
    }

    [Test]
    public void RaiseQuestStateUpdated_InvokesSubscriberWithExactArgs()
    {
        string gotId = null;
        int gotState = -1;
        System.Action<string, int> handler = (id, s) => { gotId = id; gotState = s; };

        EventBus.QuestStateUpdated += handler;
        EventBus.RaiseQuestStateUpdated("quest_1", 2);
        EventBus.QuestStateUpdated -= handler;

        Assert.AreEqual("quest_1", gotId);
        Assert.AreEqual(2, gotState);
    }

    [Test]
    public void RaiseQuestStateUpdated_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaiseQuestStateUpdated("x", 0));
    }

    [Test]
    public void RaiseInteractionTriggered_InvokesSubscriberWithExactArg()
    {
        var go = new GameObject("Interactable");
        Transform got = null;
        System.Action<Transform> handler = t => got = t;

        EventBus.InteractionTriggered += handler;
        EventBus.RaiseInteractionTriggered(go.transform);
        EventBus.InteractionTriggered -= handler;

        Assert.AreSame(go.transform, got);
        Object_DestroyImmediate(go);
    }

    [Test]
    public void RaiseInteractionTriggered_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaiseInteractionTriggered(null));
    }

    [Test]
    public void RaiseShowNotice_InvokesSubscriberWithExactArgs()
    {
        string gotText = null;
        float gotDuration = -1f;
        System.Action<string, float> handler = (t, d) => { gotText = t; gotDuration = d; };

        EventBus.ShowNotice += handler;
        EventBus.RaiseShowNotice("Hello", 3f);
        EventBus.ShowNotice -= handler;

        Assert.AreEqual("Hello", gotText);
        Assert.AreEqual(3f, gotDuration);
    }

    [Test]
    public void RaiseShowNotice_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventBus.RaiseShowNotice("x", 1f));
    }

    private static void Object_DestroyImmediate(Object obj)
    {
        Object.DestroyImmediate(obj);
    }
}
